using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using SysadminsLV.Asn1Editor.API.ModelObjects;

namespace SysadminsLV.Asn1Editor.API.SessionState;

/// <summary>
/// Represents a source for managing session documents within the application.
/// This class handles session recovery options, monitors tab changes, and provides
/// mechanisms to enable or disable automatic recovery for session documents.
/// </summary>
/// <remarks>
/// The <see cref="SessionDocumentSource"/> class interacts with the session tab host
/// and recovery options to ensure proper handling of session states and backups.
/// It implements the <see cref="IDisposable"/> interface to release resources when no longer needed.
/// </remarks>
class SessionDocumentSource : IDisposable {
    readonly ISessionTabHost _sessionTabHost;
    readonly INotifyCollectionChanged _tabsChangeSource;
    readonly SessionRecoveryOptions _recoveryOptions;
    readonly DispatcherTimer _timer = new();

    public SessionDocumentSource(ISessionTabHost sessionTabHost, UserSettings userSettings) {
        _sessionTabHost = sessionTabHost;
        _tabsChangeSource = sessionTabHost.Tabs;
        _recoveryOptions = userSettings.SessionRecovery;
        _recoveryOptions.PropertyChanged += RecoveryOptions_OnPropertyChanged;
        if (_recoveryOptions.EnableAutomaticRecovery) {
            enableRecovery();
        }
    }
    


    async Task saveSessionAsync() {
        _timer.Stop();
        try {
            await SessionBackupManager.Instance.SaveSessionAsync(_sessionTabHost);
        } catch (Exception ex) {
            App.Write(ex);
        } finally {
            _timer.Start();
        }
    }

    void enableRecovery() {
        SessionBackupManager.Instance.Start();
        _sessionTabHost.PropertyChanged += SessionTabHost_OnPropertyChanged;
        _tabsChangeSource.CollectionChanged += TabsChangeSource_OnCollectionChanged;
        _timer.Tick += Timer_OnTick;
        _timer.Interval = getBackupInterval();
        _timer.Start();
    }
    void disableRecovery() {
        _sessionTabHost.PropertyChanged -= SessionTabHost_OnPropertyChanged;
        _tabsChangeSource.CollectionChanged -= TabsChangeSource_OnCollectionChanged;
        _timer.Stop();
        _timer.Tick -= Timer_OnTick;
        SessionBackupManager.Instance.Shutdown();
    }
    TimeSpan getBackupInterval() {
        Int32 interval = Math.Max(20, _recoveryOptions.BackupIntervalInSeconds);    
        return TimeSpan.FromSeconds(interval);
    }

    void RecoveryOptions_OnPropertyChanged(Object sender, PropertyChangedEventArgs e) {
        switch (e.PropertyName) {
            case nameof(SessionRecoveryOptions.EnableAutomaticRecovery) when _recoveryOptions.EnableAutomaticRecovery:
                enableRecovery();
                break;
            case nameof(SessionRecoveryOptions.EnableAutomaticRecovery):
                disableRecovery();
                break;
            case nameof(SessionRecoveryOptions.BackupIntervalInSeconds):
                _timer.Interval = getBackupInterval();
                break;
        }
    }
    async void Timer_OnTick(Object sender, EventArgs e) {
        await saveSessionAsync();
    }
    async void TabsChangeSource_OnCollectionChanged(Object sender, NotifyCollectionChangedEventArgs args) {
        await saveSessionAsync();
    }
    async void SessionTabHost_OnPropertyChanged(Object sender, PropertyChangedEventArgs args) {
        if (args.PropertyName == nameof(ISessionTabHost.SelectedTab)) {
            await SessionBackupManager.Instance.SaveSessionMetadataOnlyAsync(_sessionTabHost);
        }
    }

    /// <summary>
    /// Shuts down the session document source and releases associated resources.
    /// </summary>
    /// <remarks>
    /// This method ensures that all session-related resources are properly cleaned up,
    /// including stopping any ongoing recovery processes and shutting down the
    /// <see cref="SessionBackupManager"/> instance.
    /// </remarks>
    public void Shutdown() {
        disableRecovery();
    }

    /// <inheritdoc />
    public void Dispose() {
        _recoveryOptions.PropertyChanged -= RecoveryOptions_OnPropertyChanged;
        Shutdown();
    }
}