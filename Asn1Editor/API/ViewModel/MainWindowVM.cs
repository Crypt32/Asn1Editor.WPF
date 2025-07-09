using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using SysadminsLV.Asn1Editor.API.Abstractions;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.Utils;
using SysadminsLV.Asn1Parser;
using SysadminsLV.WPF.OfficeTheme.Controls;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

class MainWindowVM : ViewModelBase, IMainWindowVM, IHasAsnDocumentTabs {
    readonly IWindowFactory _windowFactory;
    readonly IUIMessenger _uiMessenger;
    Asn1DocumentVM selectedTab;

    public MainWindowVM(
        IWindowFactory windowFactory,
        IAppCommands appCommands,
        NodeViewOptions nodeViewOptions) {
        _windowFactory = windowFactory;
        _uiMessenger = windowFactory.GetUIMessenger();
        GlobalData = new GlobalData();
        AppCommands = appCommands;
        TreeCommands = new TreeViewCommands(windowFactory, this);
        NodeViewOptions = nodeViewOptions;
        NodeViewOptions.RequireTreeRefresh += onNodeViewOptionsChanged;
        NewCommand = new RelayCommand(newTab);
        CloseTabCommand = new RelayCommand(closeTab, canCloseTab);
        CloseAllTabsCommand = new RelayCommand(closeAllTabs);
        CloseAllButThisTabCommand = new RelayCommand(closeAllButThisTab, canCloseAllButThisTab);
        OpenCommand = new AsyncCommand(openFileAsync);
        SaveCommand = new RelayCommand(saveFile, canPrintSave);
        DropFileCommand = new AsyncCommand(dropFileAsync);
        appCommands.ShowConverterWindow = new RelayCommand(showConverter);
        addTabToList(new Asn1DocumentVM(NodeViewOptions, TreeCommands));
    }

    async void onNodeViewOptionsChanged(Object sender, RequireTreeRefreshEventArgs args) {
        await RefreshTabs(args.Filter);
    }

    public ICommand NewCommand { get; }
    public ICommand CloseTabCommand { get; }
    public ICommand CloseAllTabsCommand { get; }
    public ICommand CloseAllButThisTabCommand { get; }
    public IAsyncCommand OpenCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand PrintCommand { get; }
    public ICommand SettingsCommand { get; }
    public IAsyncCommand DropFileCommand { get; }

    public IAppCommands AppCommands { get; }
    public ITreeCommands TreeCommands { get; }

    public GlobalData GlobalData { get; }
    public NodeViewOptions NodeViewOptions { get; }
    public ObservableCollection<Asn1DocumentVM> Tabs { get; } = [];
    public Asn1DocumentVM SelectedTab {
        get => selectedTab;
        set {
            selectedTab = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Shows Binary Converter dialog and renders converted ASN data if requested.
    /// </summary>
    /// <param name="o"></param>
    void showConverter(Object o) {
        if (SelectedTab is null) {
            _windowFactory.ShowConverterWindow(Array.Empty<Byte>(), openRawAsync);
        } else {
            _windowFactory.ShowConverterWindow(SelectedTab.DataSource.RawData, openRawAsync);
        }
    }
    void newTab(Object o) {
        var tab = new Asn1DocumentVM(NodeViewOptions, TreeCommands);
        addTabToList(tab);
    }
    /// <summary>
    /// Adds tab specified by <strong>tab</strong> parameter to <see cref="Tabs"/> list and optionally makes it
    /// active (sets to <see cref="SelectedTab"/> property).
    /// </summary>
    /// <param name="tab">Tab document to add.</param>
    void addTabToList(Asn1DocumentVM tab) {
        Tabs.Add(tab);
        SelectedTab = tab;
    }
    /// <summary>
    /// Returns a blank tab instance. Either, it is a current value of <see cref="SelectedTab"/> property
    /// or new tab document instance.
    /// </summary>
    /// <param name="isNew">
    ///     Specifies if method created new tab document instance. This parameter can be used to determine if
    ///     created tab can be closed should decode fail.
    /// </param>
    /// <returns>Blank tab document instance.</returns>
    Asn1DocumentVM getAvailableTab(out Boolean isNew) {
        isNew = false;
        Boolean useExistingTab = SelectedTab is not null && SelectedTab.CanReuse;
        if (useExistingTab && Tabs.Any()) {
            return SelectedTab;
        }

        isNew = true;
        var tab = new Asn1DocumentVM(NodeViewOptions, TreeCommands);
        addTabToList(tab);

        return tab;
    }
    /// <summary>
    /// Creates tab document from file.
    /// </summary>
    /// <param name="file">Path to a file that contains valid ASN.1-encoded data.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>If new tab was created, but file decoding fails, this temporary tab document will be closed.</remarks>
    async Task createTabFromFile(String file) {
        var tab = getAvailableTab(out Boolean useExistingTab);
        tab.Path = file;
        try {
            IEnumerable<Byte> bytes = await FileUtility.FileToBinaryAsync(file);
            await tab.Decode(bytes, true);
        } catch (Exception ex) {
            _uiMessenger.ShowError(ex.Message, "Read Error");
            if (!useExistingTab) {
                Tabs.Remove(tab);
            }
        }
    }

    #region Read content to tab
    Task openFileAsync(Object obj, CancellationToken token = default) {
        _uiMessenger.TryGetOpenFileName(out String filePath);
        if (String.IsNullOrWhiteSpace(filePath)) {
            return Task.CompletedTask;
        }

        return createTabFromFile(filePath);
    }
    #endregion

    #region Write tab to file
    // 'o' parameter can receive:
    // null - use current tab with default name
    // 1    - use current tab with custom name
    // 2    - save all tabs with default name.
    void saveFile(Object obj) {
        if (obj is null) {
            writeFile(SelectedTab);
        } else {
            switch (obj.ToString()) {
                case "1": {
                    if (getSaveFilePath(out String filePath)) {
                        writeFile(SelectedTab, filePath);
                    }

                    break;
                }
                case "2":
                    // do something with save all tabs
                    break;
            }
        }
    }
    Boolean canPrintSave(Object obj) {
        return SelectedTab?.DataSource.RawData.Count > 0;
    }

    // general method to write arbitrary tab to a file.
    Boolean writeFile(Asn1DocumentVM tab, String filePath = null) {
        // use default path if no custom file path specified
        filePath ??= tab.Path;
        // if file path is still null, then it came from "untitled" tab with default file path
        // so prompt for file to save and abort if cancelled.
        if (String.IsNullOrEmpty(filePath) && !getSaveFilePath(out filePath)) {
            return false;
        }
        try {
            File.WriteAllBytes(filePath, tab.DataSource.RawData.ToArray());
            tab.Path = filePath;
            tab.IsModified = false;

            return true;
        } catch (Exception e) {
            _uiMessenger.ShowError(e.Message, "Save Error");
        }
        return false;
    }
    Boolean getSaveFilePath(out String saveFilePath) {
        return _uiMessenger.TryGetSaveFileName(out saveFilePath);
    }

    public Boolean RequestFileSave(Asn1DocumentVM tab) {
        Boolean? result = _uiMessenger.YesNoCancel("Current file was modified. Save changes?", "Unsaved Data");
        return result switch {
            false => true,
            true  => writeFile(tab),
            _     => false
        };
    }
    #endregion

    #region Close Tab(s)

    void closeTab(Object o) {
        if (o is null) {
            closeTab(SelectedTab);
        } else if (o is ClosableTabItem tabItem) { // TODO: need to eliminate explicit reference to UI elements
            var vm = (Asn1DocumentVM)tabItem.Content;
            closeTab(vm);
        }
    }
    Boolean canCloseTab(Object o) {
        // TODO: need to eliminate explicit reference to UI elements
        return o is null or ClosableTabItem;
    }
    void closeAllTabs(Object o) {
        CloseAllTabs();
    }
    void closeAllButThisTab(Object o) {
        if (o is null) {
            closeTabsWithPreservation(SelectedTab);
        } else if (o is ClosableTabItem tabItem) { // TODO: need to eliminate explicit reference to UI elements
            var vm = (Asn1DocumentVM)tabItem.Content;
            closeTabsWithPreservation(vm);
        }
    }
    Boolean canCloseAllButThisTab(Object o) {
        if (Tabs.Count == 0) {
            return false;
        }
        if (o is null) {
            return SelectedTab is not null;
        }

        return true;
    }

    void closeTab(Asn1DocumentVM tab) {
        if (!tab.IsModified) {
            Tabs.Remove(tab);
        }
        if (tab.IsModified && RequestFileSave(tab)) {
            Tabs.Remove(tab);
        }
    }
    Boolean closeTabsWithPreservation(Asn1DocumentVM preservedTab = null) {
        // loop over a copy of tabs since we are going to update source collection in a loop
        var tabs = Tabs.ToList();
        foreach (Asn1DocumentVM tab in tabs) {
            if (preservedTab is not null && Equals(tab, preservedTab)) {
                continue;
            }
            if (!tab.IsModified) {
                Tabs.Remove(tab);

                continue;
            }
            SelectedTab = tab;
            if (!RequestFileSave(tab)) {
                return false;
            }
            Tabs.Remove(tab);
        }

        return true;
    }
    public Boolean CloseAllTabs() {
        return closeTabsWithPreservation();
    }

    #endregion

    Task dropFileAsync(Object o, CancellationToken token = default) {
        if (o is not String filePath || !File.Exists(filePath)) {
            return Task.CompletedTask;
        }

        return createTabFromFile(filePath);
    }
    public Task OpenExistingAsync(String filePath) {
        return createTabFromFile(filePath);
    }
    public async Task OpenRawAsync(String base64String) {
        try {
            await openRawAsync(Convert.FromBase64String(base64String));
        } catch (Exception ex) {
            _uiMessenger.ShowError(ex.Message, "Read Error");
        }
    }
    Task openRawAsync(Byte[] rawBytes) {
        var asn = new Asn1Reader(rawBytes);
        try
        {
            asn.BuildOffsetMap();
            // at this point, raw data is granted to be valid DER encoding and should not fail.
            Asn1DocumentVM tab = getAvailableTab(out _);

            return tab.Decode(rawBytes, false);
        }
        catch (Exception ex)
        {
            _uiMessenger.ShowError(ex.Message, "Read Error");

            return Task.CompletedTask;
        }
    }

    public Task RefreshTabs(Func<Asn1TreeNode, Boolean>? filter = null) {
        return Task.WhenAll(Tabs.Select(x => x.RefreshTreeView(filter)));
    }
}