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
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

class MainWindowVM : ViewModelBase, IMainWindowVM {
    readonly IWindowFactory _windowFactory;
    readonly IUIMessenger _uiMessenger;
    readonly AsnDocumentFileService _documentFileService;
    readonly SessionDocumentSource _sessionDocumentSource;

    public MainWindowVM(
        IWindowFactory windowFactory,
        IAppCommands appCommands,
        AsnDocumentHostManager documentHostManager,
        AsnDocumentFileService documentFileService,
        ITreeCommands treeCommands,
        UserSettings userSettings,
        IUIMessenger uiMessenger) {
        UserSettings = userSettings;
        DocumentHostManager = documentHostManager;
        TreeCommands = treeCommands;
        _windowFactory = windowFactory;
        _uiMessenger = uiMessenger;
        _documentFileService = documentFileService;
        GlobalData = new GlobalData();
        AppCommands = appCommands;
        
        NewCommand = new RelayCommand(_ => DocumentHostManager.AddNewTab());
        OpenCommand = new AsyncCommand((_, _) => _documentFileService.OpenFileAsync());
        SaveCommand = new RelayCommand(o => _documentFileService.SaveFile(o as String), canPrintSave);
        ReloadDocumentCommand = new AsyncCommand((_, _) => _documentFileService.ReloadActiveDocumentAsync());
        DropFileCommand = new AsyncCommand((o, _) => _documentFileService.DropFileAsync(o as String));
        appCommands.ShowConverterWindow = new RelayCommand(showConverter);
        _sessionDocumentSource = new SessionDocumentSource(DocumentHostManager, UserSettings);
        DocumentHostManager.AddTab(new AsnDocumentHostVM(UserSettings));

        CloseTabCommand = new RelayCommand(closeTab, canCloseTab);
        CloseAllTabsCommand = new RelayCommand(_ => CloseAllTabs());
        CloseAllButThisTabCommand = new RelayCommand(closeAllButThis, canCloseAllButThisTab);
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
        if (DocumentHostManager.SelectedTab is null) {
            _windowFactory.ShowConverterWindow([], _documentFileService.OpenRawAsync);
        } else {
            _windowFactory.ShowConverterWindow(DocumentHostManager.SelectedTab.GetPrimaryDocument().AsnDocContext.RawData, _documentFileService.OpenRawAsync);
        }
    }

    #region Write tab to file
    Boolean canPrintSave(Object obj) {
        return DocumentHostManager.SelectedTab?.Left.AsnDocContext.RawData.Count > 0;
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

    #region Close Tab(s)/ Shutdown

    void closeTab(Object? o) {
        DocumentHostManager.CloseTab(o as AsnDocumentHostVM ?? DocumentHostManager.SelectedTab, requestFileSave);
    }
    void closeAllButThis(Object o) {
        DocumentHostManager.CloseTabsWithPreservation(requestFileSave, o as AsnDocumentHostVM ?? DocumentHostManager.SelectedTab);
    }
    public Boolean CloseAllTabs() {
        return DocumentHostManager.CloseTabsWithPreservation(requestFileSave);
    }

    Boolean canCloseTab(Object? o) {
        return o is AsnDocumentHostVM or null;
    }
    Boolean canCloseAllButThisTab(Object? o) {
        if (DocumentHostManager.Tabs.Count == 0) {
            return false;
        }
        if (o is null) {
            return DocumentHostManager.SelectedTab is not null;
        }

        return true;
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

    public async Task RestoreSessionAsync(SessionRecoveryDto recoveryData) {
        if (recoveryData.Tabs.Count > 0) {
            DocumentHostManager.Clear();
        }
        var compareDictionary = new Dictionary<String, AsnDocumentHostVM>();
        foreach (SessionTabRecoveryDto recoveryTab in recoveryData.Tabs) {
            var tab = new AsnDocumentHostVM(UserSettings);
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