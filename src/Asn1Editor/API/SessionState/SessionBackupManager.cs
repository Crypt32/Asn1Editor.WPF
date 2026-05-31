using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.ViewModel;

namespace SysadminsLV.Asn1Editor.API.SessionState;

/// <summary>
/// Manages the lifecycle and state of application sessions.
/// This class provides functionality to save, restore, and manage session-related data,
/// ensuring the persistence of session state across application runs.
/// </summary>
class SessionBackupManager {
    static readonly Object _lock = new();
    static readonly SessionManagerStorage _sessionStorage = new();
    static volatile Boolean isRunning;
    static volatile Boolean savesEnabled;  // controlled by Start/Shutdown

    readonly SessionDto _currentSession;

    SessionBackupManager() {
        _currentSession = new SessionDto {
            Version = 1,
            SessionID = Guid.NewGuid().ToString("N"),
            ProcessID = Process.GetCurrentProcess().Id,
            CreatedUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime(),
            UpdatedUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets the singleton instance of the <see cref="SessionBackupManager"/> class.
    /// This property ensures that only one instance of the <see cref="SessionBackupManager"/> exists
    /// throughout the application's lifecycle, providing a centralized point for managing session state.
    /// </summary>
    public static SessionBackupManager Instance { get; } = new();

    /// <summary>
    /// Asynchronously saves the current session state, including the state of all open tabs and their metadata.
    /// This method ensures that the session data is persisted and can be restored in future application runs.
    /// </summary>
    /// <param name="documentHosts">
    /// An instance of <see cref="ISessionTabHost"/> that provides access to the collection of open tabs
    /// and the currently selected tab within the session.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    /// <remarks>
    /// This method is thread-safe and ensures that only one save operation is executed at a time.
    /// If an exception occurs during the save process, it is logged using the application's logging mechanism.
    /// </remarks>
    public async Task SaveSessionAsync(ISessionTabHost documentHosts) {
        lock (_lock) {
            if (isRunning || !savesEnabled) {
                return;
            }
            isRunning = true;
        }

        try {
            await saveSessionAsync(documentHosts);
        } catch (Exception ex) {
            App.Write(ex);
        } finally {
            isRunning = false;
        }
    }
    
    public void Start() {
        savesEnabled = true;
    }
    public void Shutdown() {
        savesEnabled = false;
        foreach (SessionTabDto tab in _currentSession.OpenTabs) {
            _sessionStorage.DeleteRecoveryFile(tab.ID);
        }
        _sessionStorage.DeleteRecoverySession(_currentSession.SessionID);
    }

    async Task saveSessionAsync(ISessionTabHost documentHosts) {
        // Create a backup of the current tabs to avoid issues with collection modification during enumeration
        List<AsnDocumentHostVM> tabBackup = documentHosts.Tabs.ToList();
        String? selectedTabID = documentHosts.SelectedTab?.GetPrimaryDocument().ID;
        // update session metadata
        _currentSession.UpdatedUtc = DateTime.UtcNow;

        // convert active tabs to a dictionary for easy lookup
        Dictionary<String, TabSource> activeTabs = [];
        for (Int32 i = 0; i < tabBackup.Count; i++) {
            if (tabBackup[i].GetPrimaryDocument().ID == selectedTabID) {
                _currentSession.SelectedTabID = tabBackup[i].GetPrimaryDocument().ID;
            }
            var dto = new SessionTabDto {
                ID = tabBackup[i].GetPrimaryDocument().ID,
                Order = i,
                Title = tabBackup[i].Header.TrimEnd('*'),
                SourcePath = tabBackup[i].GetPrimaryDocument().Path,
                CompareID = tabBackup[i].GetSecondaryDocument()?.ID
            };
            activeTabs[dto.ID] = new TabSource(tabBackup[i].GetPrimaryDocument(), dto);
        }

        // compare active tabs with last known state to determine changes
        Dictionary<String, SessionTabDto> lastKnownState = _currentSession.OpenTabs.ToDictionary(tab => tab.ID);
        List<TabChangeState> compareState = compareStateChange(activeTabs, lastKnownState);
        // process changes
        foreach (TabChangeState tabChangeState in compareState) {
            if (!savesEnabled) {
                return; // hard return if saves were disabled during processing
            }
            switch (tabChangeState.State) {
                case SessionTabState.Dirty:
                    tabChangeState.Tab.RecoveryFile = await _sessionStorage.WriteRecoveryFileAsync(activeTabs[tabChangeState.Tab.ID].Document);
                    break;
                case SessionTabState.Clean:
                case SessionTabState.Removed:
                    await _sessionStorage.DeleteRecoveryFileAsync(tabChangeState.Tab.ID);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        _currentSession.OpenTabs.Clear();
        foreach (TabSource tab in activeTabs.Values) {
            _currentSession.OpenTabs.Add(tab.Dto);
        }

        if (savesEnabled) {
            await _sessionStorage.WriteSessionAsync(_currentSession);
        }
    }

    static List<TabChangeState> compareStateChange(IDictionary<String, TabSource> activeTabs, IDictionary<String, SessionTabDto> lastKnownState) {
        List<TabChangeState> list = [];

        // process added tabs
        foreach (String addedTabKey in activeTabs.Keys.Except(lastKnownState.Keys)) {
            AddDocToList(addedTabKey);
        }

        // process removed tabs
        foreach (String removedTabKey in lastKnownState.Keys.Except(activeTabs.Keys)) {
            list.Add(new TabChangeState(SessionTabState.Removed, lastKnownState[removedTabKey]));
        }

        // process changed tabs
        foreach (String commonTabKey in activeTabs.Keys.Intersect(lastKnownState.Keys)) {
            AddDocToList(commonTabKey);
        }

        return list;

        // nested function to avoid code duplication when adding documents to the change list
        void AddDocToList(String key) {
            TabSource activeTab = activeTabs[key];
            if (activeTab.Document.IsModified) {
                list.Add(new TabChangeState(SessionTabState.Dirty, activeTab.Dto));
            } else {
                list.Add(new TabChangeState(SessionTabState.Clean, activeTab.Dto));
            }
        }
    }

    record TabSource(Asn1DocumentVM Document, SessionTabDto Dto);
    record TabChangeState(SessionTabState State, SessionTabDto Tab);
    enum SessionTabState {
        Clean   = 0,
        Dirty   = 1,
        Removed = 2
    }

    public async Task SaveSessionMetadataOnlyAsync(ISessionTabHost sessionTabHost) {
        lock (_lock) {
            if (isRunning || !savesEnabled) {
                return;
            }
            isRunning = true;
        }

        try {
            _currentSession.SelectedTabID = sessionTabHost.SelectedTab?.GetPrimaryDocument().ID;
            _currentSession.UpdatedUtc = DateTime.UtcNow;
            if (savesEnabled) {
                await _sessionStorage.WriteSessionAsync(_currentSession);
            }
        } catch (Exception ex) {
            App.Write(ex);
        } finally {
            isRunning = false;
        }
    }
}