using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.Utils.ASN;
using SysadminsLV.Asn1Editor.API.ViewModel;
using SysadminsLV.Asn1Parser;
using SysadminsLV.WPF.OfficeTheme.Toolkit.CLR;

namespace SysadminsLV.Asn1Editor.API.ModelObjects;

class DataSource(NodeViewOptions viewOptions) : ViewModelBase, IDataSource {
    readonly ObservableList<Byte> _rawData = new(true, false);
    readonly ObservableCollection<Asn1TreeNode> _tree = [];
    Asn1TreeNode? selectedNode;

    protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
        if (_rawData.IsNotifying && CollectionChanged is not null) {
            try {
                CollectionChanged(this, e);
            } catch (NotSupportedException) {
                var alternativeEventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                OnCollectionChanged(alternativeEventArgs);
            }
        }
    }
    public Asn1TreeNode? SelectedNode {
        get => selectedNode;
        set {
            selectedNode = value;
            OnPropertyChanged();
        }
    }
    public NodeViewOptions NodeViewOptions { get; } = viewOptions;
    public ReadOnlyObservableCollection<Asn1TreeNode> Tree => new(_tree);


    Asn1Lite createNewNode(Byte[] nodeRawData) {
        var node = new Asn1Lite(new Asn1Reader(nodeRawData));
        if (SelectedNode is not null) {
            node.Offset = SelectedNode.Value.Offset + SelectedNode.Value.TagLength;
            node.Depth += SelectedNode.Value.Depth;
        }
        _rawData.InsertRange(node.Offset, nodeRawData);

        return node;
    }
    void setRootNode(Asn1TreeNode node) {
        _tree.Clear();
        _tree.Add(node);
        FinishBinaryUpdate();
    }

    public Asn1TreeNode AddNode(Byte[] nodeRawData) {
        var nodeValue = createNewNode(nodeRawData);
        Asn1TreeNode node;
        if (Tree.Count == 0) {
            // add new root node
            node = new Asn1TreeNode(nodeValue, this);
            setRootNode(node);
        } else {
            node = SelectedNode!.AddChild(nodeValue, true);
            FinishBinaryUpdate();
        }

        return SelectedNode = node;
    }
    public async Task InsertNode(Byte[] nodeRawData, NodeAddOption option) {
        if (SelectedNode is null) {
            throw new ArgumentNullException(nameof(SelectedNode));
        }
        var childNode = await AsnTreeBuilder.BuildTreeAsync(nodeRawData, this);
        Int32 newOffset = SelectedNode.Value.Offset;
        if (option != NodeAddOption.Before) {
            newOffset += SelectedNode.Value.TagLength;
        }
        _rawData.InsertRange(newOffset, nodeRawData);
        switch (option) {
            case NodeAddOption.Before:
            case NodeAddOption.After:
                SelectedNode.Parent!.InsertChildNode(childNode, SelectedNode, option);
                break;
            case NodeAddOption.Last:
                SelectedNode.InsertChildNode(
                    childNode,
                    SelectedNode,
                    option
                );
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(option), option, null);
        }
        
        FinishBinaryUpdate();
    }
    public void RemoveSelectedNode() {
        if (SelectedNode!.Parent is null) {
            Reset();
        } else {
            _rawData.RemoveRange(SelectedNode.Value.Offset, SelectedNode.Value.TagLength);
            SelectedNode.Parent.RemoveChild(SelectedNode);
        }
        FinishBinaryUpdate();
    }
    public void UpdateNodeBinaryCopy(IEnumerable<Byte> newBytes) {
        _rawData.RemoveRange(SelectedNode!.Value.Offset, SelectedNode.Value.TagLength);
        _rawData.InsertRange(SelectedNode.Value.Offset, newBytes);
    }
    public void UpdateNodeLength(Asn1TreeNode node, Byte[] newLenBytes) {
        _rawData.RemoveRange(node.Value.Offset + 1, node.Value.HeaderLength - 1);
        _rawData.InsertRange(node.Value.Offset + 1, newLenBytes);
    }
    public void FinishBinaryUpdate() {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        RequireTreeRefresh?.Invoke(this, EventArgs.Empty);
    }
    public IReadOnlyList<Byte> RawData => _rawData;

    public async Task InitializeFromRawData(IEnumerable<Byte> rawData) {
        _rawData.Clear();
        _rawData.AddRange(rawData);
        Asn1TreeNode rootNode = await AsnTreeBuilder.BuildTreeAsync(this);
        setRootNode(rootNode);
    }

    public void Reset() {
        _tree.Clear();
        SelectedNode = null;
        _rawData.Clear();
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event EventHandler? RequireTreeRefresh;
}