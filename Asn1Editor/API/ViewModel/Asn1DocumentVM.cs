using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.Controls;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

public class Asn1DocumentVM : AsyncViewModel {
    String path, fileName, pbHeaderText;
    Boolean isModified, suppressModified;

    ActivePanel activePanel { get; set; } = ActivePanel.Left;

    /// <summary>
    /// Gets or sets the currently active panel of the asn.1 document
    /// </summary>
    public ActivePanel ActivePanel
    {
        get => activePanel;
        set
        {
            if (activePanel != value)
            {
                activePanel = value;
                OnPropertyChanged();
            }
        }
    }

    public Asn1DocumentVM(NodeViewOptions nodeViewOptions, ITreeCommands treeCommands, MainWindowVM mainWindowVM)
    {
        DataSource = new DataSource(nodeViewOptions);
        DataSource.CollectionChanged += onDataSourceCollectionChanged;
        DataSource.RequireTreeRefresh += onTreeRefreshRequired;
        TreeCommands = treeCommands;
        MainWindowVM = mainWindowVM;
    }
    async void onTreeRefreshRequired(Object sender, EventArgs e) {
        await RefreshTreeView();
    }
    void onDataSourceCollectionChanged(Object sender, NotifyCollectionChangedEventArgs args) {
        if (!suppressModified) {
            IsModified = true;
        }
    }

    /// <summary>
    /// Reference to the view model for the main window of the application
    /// </summary>
    public MainWindowVM MainWindowVM { get; }

    /// <summary>
    /// Gets the current command to move a tab between panels
    /// </summary>
    public ICommand MoveTabCommand => activePanel == ActivePanel.Left
        ? MainWindowVM.MoveTabRightCommand
        : MainWindowVM.MoveTabLeftCommand;

    public IDataSource DataSource { get; }
    public ITreeCommands TreeCommands { get; }
    public NodeViewOptions NodeViewOptions => DataSource.NodeViewOptions;
    public ReadOnlyObservableCollection<Asn1TreeNode> Tree => DataSource.Tree;

    /// <summary>
    /// Determines if current ASN.1 document instance can be re-used.
    /// Returns <c>true</c> if <see cref="Tree"/> is empty, no file path is associated with current instance
    /// and there were no modifications. Otherwise <c>false</c>.
    /// </summary>
    public Boolean CanReuse => Tree.Count == 0 && String.IsNullOrWhiteSpace(Path) && !IsModified;
    public String Header {
        get {
            String template = fileName ?? "untitled";
            if (IsModified) {
                template += "*";
            }

            return template;
        }
    }
    public String ToolTipText {
        get {
            if (!String.IsNullOrWhiteSpace(Path)) {
                return Path;
            }

            return "untitled";
        }
    }
    public String Path {
        get => path;
        set {
            path = value;
            if (!String.IsNullOrWhiteSpace(path)) {
                fileName = new FileInfo(path).Name;
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(Header));
            OnPropertyChanged(nameof(ToolTipText));
        }
    }
    public Boolean IsModified {
        get => isModified;
        set {
            isModified = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Header));
        }
    }
    public String ProgressText {
        get => pbHeaderText;
        set {
            pbHeaderText = value;
            OnPropertyChanged();
        }
    }

    public Task RefreshTreeView(Func<Asn1TreeNode, Boolean>? filter = null) {
        if (Tree.Count == 0) {
            return Task.CompletedTask;
        }
        return refreshTree(Tree[0].UpdateNodeViewAsync, filter);
    }
    public Task RefreshTreeHeaders(Func<Asn1TreeNode, Boolean>? filter = null) {
        if (Tree.Count == 0) {
            return Task.CompletedTask;
        }
        return refreshTree(Tree[0].UpdateNodeHeaderAsync, filter);
    }

    async Task refreshTree(Func<Func<Asn1TreeNode, Boolean>?, Task> action, Func<Asn1TreeNode, Boolean>? filter = null) {
        
        ProgressText = "Refreshing view...";
        IsBusy = true;
        await action.Invoke(filter);
        IsBusy = false;
    }
    public async Task Decode(IEnumerable<Byte> bytes, Boolean doNotSetModifiedFlag) {
        ProgressText = "Decoding file...";
        IsBusy = true;
        if (doNotSetModifiedFlag) {
            suppressModified = true;
        }

        try {
            if (DataSource.RawData.Count > 0) {
                return;
            }
            await DataSource.InitializeFromRawData(bytes);
        } finally {
            suppressModified = false;
            IsBusy = false;
        }
    }
    public void Reset() {
        DataSource.Reset();
        Path = String.Empty;
        IsModified = false;
    }

    /// <inheritdoc />
    public override String ToString()
    {
        return $"Header: {Header}, Active panel: {ActivePanel}";
    }
}
