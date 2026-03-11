using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.Core.Tree;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

public class Asn1DocumentVM : AsyncViewModel {
    String fileName;
    Boolean suppressModified;

    public Asn1DocumentVM(NodeViewOptions nodeViewOptions, ITreeCommands treeCommands) {
        AsnDocContext = new Asn1DocumentContext(nodeViewOptions);
        AsnDocContext.CollectionChanged += onAsnDocContextCollectionChanged;
        TreeCommands = treeCommands;
    }

    void onAsnDocContextCollectionChanged(Object sender, NotifyCollectionChangedEventArgs args) {
        if (!suppressModified) {
            IsModified = true;
        }
    }

    public IAsn1DocumentContext AsnDocContext { get; }
    public ITreeCommands TreeCommands { get; }
    public NodeViewOptions NodeViewOptions => AsnDocContext.NodeViewOptions;
    public ReadOnlyObservableCollection<AsnTreeNode> Tree => AsnDocContext.Tree;

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
        get;
        set {
            field = value;
            if (!String.IsNullOrWhiteSpace(field)) {
                fileName = new FileInfo(field).Name;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(Header));
            OnPropertyChanged(nameof(ToolTipText));
        }
    }
    public Boolean IsModified {
        get;
        set {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Header));
        }
    }
    public String ProgressText {
        get;
        set {
            field = value;
            OnPropertyChanged();
        }
    }
    public Boolean IsEnabled {
        get;
        set {
            field = value;
            OnPropertyChanged();
        }
    } = true;

    public Task RefreshTreeView(Func<AsnTreeNode, Boolean>? filter = null) {
        if (Tree.Count == 0) {
            return Task.CompletedTask;
        }
        return refreshTree(Tree[0].UpdateNodeViewAsync, filter);
    }
    public Task RefreshTreeHeaders(Func<AsnTreeNode, Boolean>? filter = null) {
        if (Tree.Count == 0) {
            return Task.CompletedTask;
        }
        return refreshTree(Tree[0].UpdateNodeHeaderAsync, filter);
    }

    async Task refreshTree(Func<Func<AsnTreeNode, Boolean>?, Task> action, Func<AsnTreeNode, Boolean>? filter = null) {

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
            if (AsnDocContext.RawData.Count > 0) {
                return;
            }
            await AsnDocContext.InitializeFromRawData(bytes);
        } finally {
            suppressModified = false;
            IsBusy = false;
        }
    }
    public void Reset() {
        AsnDocContext.Reset();
        IsModified = false;
    }
}
