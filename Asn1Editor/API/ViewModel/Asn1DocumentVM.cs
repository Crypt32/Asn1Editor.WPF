using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

public class Asn1DocumentVM : AsyncViewModel {
    String path, fileName, pbHeaderText;
    Boolean isModified, suppressModified, isEnabled = true;

    public Asn1DocumentVM(NodeViewOptions nodeViewOptions, ITreeCommands treeCommands) {
        DataSource = new DataSource(nodeViewOptions);
        DataSource.CollectionChanged += onDataSourceCollectionChanged;
        DataSource.RequireTreeRefresh += onTreeRefreshRequired;
        TreeCommands = treeCommands;
    }
    async void onTreeRefreshRequired(Object sender, EventArgs e) {
        await RefreshTreeView();
    }
    void onDataSourceCollectionChanged(Object sender, NotifyCollectionChangedEventArgs args) {
        if (!suppressModified) {
            IsModified = true;
        }
    }

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
    public Boolean IsEnabled {
        get => isEnabled;
        set {
            isEnabled = value;
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
}
