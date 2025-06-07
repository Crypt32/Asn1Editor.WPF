using SysadminsLV.Asn1Editor.API.Abstractions;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.Utils;
using SysadminsLV.Asn1Editor.Controls;
using SysadminsLV.Asn1Parser;
using SysadminsLV.WPF.OfficeTheme.Controls;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

public class MainWindowVM : ViewModelBase, IMainWindowVM, IHasAsnDocumentTabs {
    readonly IWindowFactory _windowFactory;
    readonly IUIMessenger _uiMessenger;
    Asn1DocumentVM selectedLeftTab;
    Asn1DocumentVM selectedRightTab;
    Asn1DocumentVM selectedTab;
    private GridLength separatorWidth = new GridLength(0, GridUnitType.Pixel);
    private GridLength rightColumnWidth = new GridLength(0, GridUnitType.Pixel);

    public MainWindowVM(
        IWindowFactory windowFactory,
        IAppCommands appCommands,
        NodeViewOptions nodeViewOptions)
    {
        _windowFactory = windowFactory;
        _uiMessenger = windowFactory.GetUIMessenger();
        GlobalData = new GlobalData();
        AppCommands = appCommands;
        TreeCommands = new TreeViewCommands(windowFactory, this);
        NodeViewOptions = nodeViewOptions;
        NodeViewOptions.RequireTreeRefresh += onNodeViewOptionsChanged;

        NewCommand = new RelayCommand(newTab);
        CloseTabCommand = new RelayCommand<Asn1DocumentVM>(closeTab);
        CloseAllTabsCommand = new RelayCommand(_ => CloseAllTabs());
        CloseAllButThisTabCommand = new RelayCommand<Asn1DocumentVM>(closeAllButThisTab);
        OpenCommand = new AsyncCommand(openFileAsync);
        SaveCommand = new RelayCommand(saveFile, canPrintSave);
        DropFileCommand = new AsyncCommand(dropFileAsync);
        MoveTabLeftCommand = new RelayCommand(moveTabLeft);
        MoveTabRightCommand = new RelayCommand(moveTabRight);
        appCommands.ShowConverterWindow = new RelayCommand(showConverter);

        // Handle dynamic updates
        Tabs.CollectionChanged += (_, _) => attachPropertyChangedHandlers();

        // Start with one tab
        addTabToList(new Asn1DocumentVM(NodeViewOptions, TreeCommands, this));

        LeftTabsView = new ListCollectionView(Tabs);
        LeftTabsView.Filter = o => ((Asn1DocumentVM)o).ActivePanel == ActivePanel.Left;

        RightTabsView = new ListCollectionView(Tabs);
        RightTabsView.Filter = o => ((Asn1DocumentVM)o).ActivePanel == ActivePanel.Right;
    }

    async void onNodeViewOptionsChanged(Object sender, RequireTreeRefreshEventArgs args) {
        await RefreshTabs(args.Filter);
    }
    
    private void attachPropertyChangedHandlers()
    {
        foreach (var tab in Tabs)
        {
            tab.PropertyChanged -= onTabPropertyChanged;
            tab.PropertyChanged += onTabPropertyChanged;
        }
    }
    
    private void onTabPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Asn1DocumentVM.ActivePanel))
        {
            LeftTabsView.Refresh();
            RightTabsView.Refresh();
        }
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

    /// <summary>
    /// Gets the command that moves the currently selected tab to the left.
    /// </summary>
    public ICommand MoveTabLeftCommand { get; }

    /// <summary>
    /// Gets the command that moves the currently selected tab to the right.
    /// </summary>
    public ICommand MoveTabRightCommand { get; }

    public GlobalData GlobalData { get; }
    public NodeViewOptions NodeViewOptions { get; }

    public ObservableCollection<Asn1DocumentVM> Tabs { get; } = [];

    public ICollectionView LeftTabsView { get; }
    public ICollectionView RightTabsView { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the view is split into multiple panels.
    /// </summary>
    public bool IsSplitView => Tabs.Any(x => x.ActivePanel == ActivePanel.Right);

    /// <summary>
    /// Gets a value indicating whether the current view is not a split view
    /// </summary>
    public bool IsNotSplitView => !IsSplitView;

    /// <summary>
    /// Gets or sets the width of the separator between panels in the grid layout.
    /// </summary>
    public GridLength SeparatorWidth
    {
        get
        {
            return separatorWidth;
        }
        set
        {
            separatorWidth = value;
            OnPropertyChanged();
        }
    } 

    /// <summary>
    /// Gets or sets the width of the right panel in the grid layout.
    /// </summary>
    public GridLength RightColumnWidth
    {
        get
        {
            return rightColumnWidth;
        }
        set
        {
            rightColumnWidth = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Selected tab of both panels
    /// </summary>
    public Asn1DocumentVM SelectedTab => selectedTab;

    public Asn1DocumentVM SelectedLeftTab
    {
        get => selectedLeftTab;
        set
        {
            selectedLeftTab = value;
            selectedTab = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedTab));
        }
    }

    public Asn1DocumentVM SelectedRightTab
    {
        get => selectedRightTab;
        set
        {
            selectedRightTab = value;
            selectedTab = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedTab));
        }
    }

    /// <summary>
    /// Shows Binary Converter dialog and renders converted ASN data if requested.
    /// </summary>
    /// <param name="o"></param>
    void showConverter(Object o) {
        if (SelectedTab == null) {
            _windowFactory.ShowConverterWindow(Array.Empty<Byte>(), openRawAsync);
        } else {
            _windowFactory.ShowConverterWindow(SelectedTab.DataSource.RawData, openRawAsync);
        }
    }
    void newTab(Object o) {
        var tab = new Asn1DocumentVM(NodeViewOptions, TreeCommands, this);
        addTabToList(tab);
    }

    private void removeTab(Asn1DocumentVM tab) 
    {
        Tabs.Remove(tab);
    }

    /// <summary>
    /// Adds tab specified by <strong>tab</strong> parameter to <see cref="LeftTabs"/> list and optionally makes it
    /// active (sets to <see cref="SelectedTab"/> property).
    /// </summary>
    /// <param name="tab">Tab document to add.</param>
    private void addTabToList(Asn1DocumentVM tab)
    {
        Tabs.Add(tab);
        selectedTab = tab;
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
        var tab = new Asn1DocumentVM(NodeViewOptions, TreeCommands, this);
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
                removeTab(tab);
            }
        }
    }

    /// <summary>
    /// Moves the selected tab from the left panel to the right panel.
    /// </summary>
    private void moveTabRight(Object o)
    {
        if (o is not Asn1DocumentVM tab)
        {
            return;
        }

        int leftTabCount = Tabs.Count(x => x.ActivePanel == ActivePanel.Left);
        if (leftTabCount > 1)
        {
            tab.ActivePanel = ActivePanel.Right;
            SelectedRightTab = tab;

            if (Tabs.Count(x => x.ActivePanel == ActivePanel.Right) == 1)
            {
                RightColumnWidth = new GridLength(1, GridUnitType.Star);
                SeparatorWidth = new GridLength(5, GridUnitType.Pixel);
            }

            OnPropertyChanged(nameof(IsSplitView));
        }
    }

    private void moveTabLeft(Object o)
    {
        if (o is not Asn1DocumentVM tab)
        {
            return;
        }

        tab.ActivePanel = ActivePanel.Left;
        SelectedLeftTab = tab;

        int rightTabCount = Tabs.Count(x => x.ActivePanel == ActivePanel.Right);
        if (rightTabCount == 0)
        {
            RightColumnWidth = new GridLength(0, GridUnitType.Pixel);
            SeparatorWidth = new GridLength(0, GridUnitType.Pixel);
            SelectedRightTab = null;
        }

        OnPropertyChanged(nameof(IsSplitView));
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
        if (obj == null) {
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

    Boolean canCloseTab(Object o) {
        // TODO: need to eliminate explicit reference to UI elements
        return o is null or ClosableTabItem;
    }
    void closeAllTabs(Object o) {
        CloseAllTabs();
    }
    
    private void closeAllButThisTab(Asn1DocumentVM preservedTab)
    {
        closeTabsWithPreservation(SelectedTab);
    }

    Boolean canCloseAllButThisTab(Object o) {
        if (Tabs.Count == 0) {
            return false;
        }
        if (o == null) {
            return SelectedTab != null;
        }

        return true;
    }

    void closeTab(Asn1DocumentVM tab) {
        if (!tab.IsModified) {
            removeTab(tab);
        }
        if (tab.IsModified && RequestFileSave(tab)) {
            removeTab(tab);
        }
    }
    Boolean closeTabsWithPreservation(Asn1DocumentVM preservedTab = null) {
        // loop over a copy of tabs since we are going to update source collection in a loop
        var tabs = Tabs.ToList();
        foreach (Asn1DocumentVM tab in tabs)
        {
            if (preservedTab != null && Equals(tab, preservedTab))
            {
                continue;
            }
            if (!tab.IsModified)
            {
                removeTab(tab);

                continue;
            }
            selectedTab = tab;
            if (!RequestFileSave(tab))
            {
                return false;
            }
            removeTab(tab);
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

    public Task RefreshTabs(Func<Asn1TreeNode, Boolean>? filter = null)
    {
        return Task.WhenAll(Tabs.Select(x => x.RefreshTreeView(filter)));
    }
}