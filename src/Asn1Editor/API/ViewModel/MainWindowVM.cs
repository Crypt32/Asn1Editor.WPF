using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.SessionState;
using SysadminsLV.Asn1Editor.API.Utils;
using SysadminsLV.Asn1Editor.API.Utils.WPF;
using SysadminsLV.Asn1Editor.Core.Tree;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

class MainWindowVM : ViewModelBase, IMainWindowVM, IHasAsnDocumentTabs {
    readonly IWindowFactory _windowFactory;
    readonly IUIMessenger _uiMessenger;
    readonly AsnDocumentFileService _documentFileService;
    readonly SessionDocumentSource _sessionDocumentSource;

    public MainWindowVM(
        IWindowFactory windowFactory,
        IAppCommands appCommands,
        UserSettings userSettings) {
        _windowFactory = windowFactory;
        _uiMessenger = windowFactory.GetUIMessenger();
        UserSettings = userSettings;
        UserSettings.RequireTreeRefresh += OnUserSettingsChanged;
        TreeCommands = new TreeViewCommands(windowFactory, this);
        DocumentHostManager = new AsnDocumentHostManager(userSettings);
        _documentFileService = new AsnDocumentFileService(_uiMessenger, DocumentHostManager, TreeCommands, requestFileSave);
        GlobalData = new GlobalData();
        AppCommands = appCommands;
        
        NewCommand = new RelayCommand(_ => DocumentHostManager.AddNewTab(TreeCommands));
        CloseTabCommand = new RelayCommand(closeTab, canCloseTab);
        CloseAllTabsCommand = new RelayCommand(closeAllTabs);
        CloseAllButThisTabCommand = new RelayCommand(closeAllButThisTab, canCloseAllButThisTab);
        OpenCommand = new AsyncCommand((_, _) => _documentFileService.OpenFileAsync());
        SaveCommand = new RelayCommand(o => _documentFileService.SaveFile(o as String), canPrintSave);
        ReloadDocumentCommand = new AsyncCommand((_, _) => _documentFileService.ReloadActiveDocumentAsync());
        DropFileCommand = new AsyncCommand((o, _) => _documentFileService.DropFileAsync(o as String));
        appCommands.ShowConverterWindow = new RelayCommand(showConverter);
        _sessionDocumentSource = new SessionDocumentSource(DocumentHostManager, UserSettings);
        DocumentHostManager.AddTab(new AsnDocumentHostVM(UserSettings, TreeCommands));
    }

    async void OnUserSettingsChanged(Object sender, RequireTreeRefreshEventArgs args) {
        await RefreshTabs(args.Filter);
    }

    public ICommand NewCommand { get; }
    public ICommand CloseTabCommand { get; }
    public ICommand CloseAllTabsCommand { get; }
    public ICommand CloseAllButThisTabCommand { get; }
    public IAsyncCommand OpenCommand { get; }
    public ICommand SaveCommand { get; }
    public IAsyncCommand ReloadDocumentCommand { get; }
    public ICommand PrintCommand { get; }
    public ICommand SettingsCommand { get; }
    public IAsyncCommand DropFileCommand { get; }
    
    public IAppCommands AppCommands { get; }
    public ITreeCommands TreeCommands { get; }

    public GlobalData GlobalData { get; }
    public UserSettings UserSettings { get; }
    public AsnDocumentHostManager DocumentHostManager { get; }
    public AsnDocumentHostVM? SelectedTab => DocumentHostManager.SelectedTab;

    /// <summary>
    /// Shows Binary Converter dialog and renders converted ASN data if requested.
    /// </summary>
    /// <param name="o"></param>
    void showConverter(Object o) {
        if (SelectedTab is null) {
            _windowFactory.ShowConverterWindow([], _documentFileService.OpenRawAsync);
        } else {
            _windowFactory.ShowConverterWindow(SelectedTab.GetPrimaryDocument().AsnDocContext.RawData, _documentFileService.OpenRawAsync);
        }
    }

    #region Write tab to file
    Boolean canPrintSave(Object obj) {
        return SelectedTab?.Left.AsnDocContext.RawData.Count > 0;
    }

    Boolean requestFileSave(AsnDocumentHostVM tab) {
        Boolean? result = _uiMessenger.YesNoCancel("Current file was modified. Save changes?", "Unsaved Data");
        return result switch {
            false => true,
            true => _documentFileService.WriteFile(tab),
            _ => false
        };
    }
    #endregion

    #region Close Tab(s)

    void closeTab(Object? o) {
        if (o is AsnDocumentHostVM vm) {
            closeTab(vm);
        } else {
            closeTab(SelectedTab);
        }
    }
    Boolean canCloseTab(Object? o) {
        return o is AsnDocumentHostVM or null;
    }
    void closeAllTabs(Object? o) {
        CloseAllTabs();
    }
    void closeAllButThisTab(Object? o) {
        if (o is AsnDocumentHostVM vm) {
            closeTabsWithPreservation(vm);
        } else {
            closeTabsWithPreservation(SelectedTab);
        }
    }
    Boolean canCloseAllButThisTab(Object? o) {
        if (DocumentHostManager.Tabs.Count == 0) {
            return false;
        }
        if (o is null) {
            return SelectedTab is not null;
        }

        return true;
    }

    void closeTab(AsnDocumentHostVM tab) {
        Asn1DocumentVM doc = tab.GetPrimaryDocument();
        if (!doc.IsModified) {
            DocumentHostManager.RemoveTab(tab);
        }
        if (doc.IsModified && requestFileSave(tab)) {
            DocumentHostManager.RemoveTab(tab);
        }
    }
    Boolean closeTabsWithPreservation(AsnDocumentHostVM? preservedTab = null) {
        // loop over a copy of tabs since we are going to update source collection in a loop
        var tabs = DocumentHostManager.Tabs.ToList();
        foreach (AsnDocumentHostVM tab in tabs) {
            Asn1DocumentVM doc = tab.GetPrimaryDocument();
            if (preservedTab is not null && Equals(tab, preservedTab)) {
                continue;
            }
            if (!doc.IsModified) {
                DocumentHostManager.RemoveTab(tab);

                continue;
            }
            DocumentHostManager.SelectedTab = tab;
            if (!requestFileSave(tab)) {
                return false;
            }
            DocumentHostManager.RemoveTab(tab);
        }

        return true;
    }
    public Boolean CloseAllTabs() {
        return closeTabsWithPreservation();
    }

    /// <inheritdoc />
    public void Shutdown() {
        _sessionDocumentSource.Shutdown();
    }

    #endregion

    public Task OpenExistingAsync(String filePath) {
        return _documentFileService.OpenExistingAsync(filePath);
    }
    public async Task OpenRawAsync(String base64String) {
        try {
            await _documentFileService.OpenRawAsync(Convert.FromBase64String(base64String));
        } catch (Exception ex) {
            _uiMessenger.ShowError(ex.Message, "Read Error");
        }
    }

    public Task RefreshTabs(Func<AsnTreeNode, Boolean>? filter = null) {
        return Task.WhenAll(DocumentHostManager.Tabs.Select(x => x.GetPrimaryDocument().RefreshTreeView(filter)));
    }
    public async Task RestoreSessionAsync(SessionRecoveryDto recoveryData) {
        if (recoveryData.Tabs.Count > 0) {
            DocumentHostManager.Clear();
        }
        var compareDictionary = new Dictionary<String, AsnDocumentHostVM>();
        foreach (SessionTabRecoveryDto recoveryTab in recoveryData.Tabs) {
            var tab = new AsnDocumentHostVM(UserSettings, TreeCommands);
            Asn1DocumentVM doc = tab.GetPrimaryDocument();
            if (recoveryTab.RecoveryData is not null) {
                try {
                    await doc.Decode(recoveryTab.RecoveryData, false);
                } catch (Exception ex) {
                    App.Write(ex);
                    _uiMessenger.ShowError($"Failed to restore session tab with source path '{recoveryTab.SourcePath}'. Error: {ex.Message}", "Session Restore Warning");
                    continue;
                }
            } else if (!String.IsNullOrEmpty(recoveryTab.SourcePath) && File.Exists(recoveryTab.SourcePath)) {
                try {
                    IEnumerable<Byte> bytes = await FileUtility.FileToBinaryAsync(recoveryTab.SourcePath!);
                    await doc.Decode(bytes, true);
                } catch (Exception ex) {
                    App.Write(ex);
                    _uiMessenger.ShowError($"Failed to restore session tab with source path '{recoveryTab.SourcePath}'. Error: {ex.Message}", "Session Restore Warning");
                    continue;
                }
            } else {
                // if there is no recovery data and source path is invalid, skip restoring this tab.
                // normally, you don't hit this branch it may happen if the recovery data is corrupted, edited manually.
                continue;
            }

            doc.ID = recoveryTab.ID;
            doc.Path = recoveryTab.SourcePath;
            DocumentHostManager.AddTab(tab);
            compareDictionary[recoveryTab.ID] = tab;
        }

        foreach (SessionTabRecoveryDto recoveryTab in recoveryData.Tabs.Where(x => x.CompareID is not null)) {
            if (compareDictionary.TryGetValue(recoveryTab.CompareID!, out AsnDocumentHostVM compareTab)) {
                AsnDocumentHostVM left = compareDictionary[recoveryTab.ID];
                var tabParam = new TabCompareParam(left, compareTab);
                left.StartCompareModeCommand.Execute(tabParam);
            }
        }
        if (recoveryData.SelectedTabID is not null) {
            DocumentHostManager.SelectedTab = DocumentHostManager.Tabs.FirstOrDefault(x => x.GetPrimaryDocument().ID == recoveryData.SelectedTabID);
        }
    }
}