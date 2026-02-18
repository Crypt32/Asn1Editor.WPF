using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.Core.Tree;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

/// <summary>
/// Represents the ASN.1 document context for a WPF tab.
/// Wraps TreeCoordinator and provides UI-specific bindings.
/// </summary>
class Asn1DocumentContext : ViewModelBase, IAsn1DocumentContext {
    readonly TreeCoordinator _coordinator;
    readonly ObservableCollection<AsnTreeNode> _treeCollection = [];

    public Asn1DocumentContext(NodeViewOptions viewOptions) {
        NodeViewOptions = viewOptions;
        Tree = new ReadOnlyObservableCollection<AsnTreeNode>(_treeCollection);
        // TreeCoordinator creates and owns BinaryDataSource internally
        _coordinator = new TreeCoordinator(viewOptions);
        // Forward notifications from BinarySource
        _coordinator.RawData.CollectionChanged += onBinarySourceChanged;
    }

    void onBinarySourceChanged(Object? sender, NotifyCollectionChangedEventArgs e) {
        CollectionChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Synchronizes the observable collection with coordinator's root node.
    /// </summary>
    void syncTreeCollection() {
        _treeCollection.Clear();
        if (_coordinator.Root is not null) {
            _treeCollection.Add(_coordinator.Root);
        }
    }

    /// <summary>
    /// Gets or sets the currently selected tree node (UI state).
    /// </summary>
    public AsnTreeNode? SelectedNode {
        get;
        set {
            field = value;
            OnPropertyChanged();
        }
    }

    public NodeViewOptions NodeViewOptions { get; }
    /// <summary>
    /// Gets the tree collection for WPF TreeView binding.
    /// This is a single-item collection containing the root node.
    /// </summary>
    public ReadOnlyObservableCollection<AsnTreeNode> Tree { get; }
    public IReadOnlyList<Byte> RawData => _coordinator.RawData;

    #region Tree operations (delegated to TreeCoordinator)

    public async Task<AsnTreeNode> AddNode(Byte[] nodeRawData, AsnTreeNode? parent) {
        AsnTreeNode result = await _coordinator.AddNode(nodeRawData, parent);
        syncTreeCollection();

        return result;
    }
    public async Task InsertNode(AsnTreeNode node, NodeAddOption option, Byte[] nodeRawData) {
        await _coordinator.InsertNode(nodeRawData, node, option);
        syncTreeCollection();
    }
    public void RemoveNode(AsnTreeNode nodeToRemove) {
        _coordinator.RemoveNode(nodeToRemove);
        syncTreeCollection();
    }
    public void UpdateNode(AsnTreeNode nodeValue, Byte[] newBytes) {
        _coordinator.UpdateNode(nodeValue, newBytes);
    }
    public async Task InitializeFromRawData(IEnumerable<Byte> rawData) {
        await _coordinator.InitializeFromRawData(rawData);
        syncTreeCollection();
    }
    public void Reset() {
        _coordinator.Reset();
        syncTreeCollection();
        SelectedNode = null;
    }

    #endregion

    #region Events

    /// <inheritdoc />
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    #endregion
}