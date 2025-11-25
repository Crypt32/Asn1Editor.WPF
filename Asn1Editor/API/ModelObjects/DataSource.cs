using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.Utils.ASN;
using SysadminsLV.Asn1Editor.API.ViewModel;
using SysadminsLV.Asn1Editor.Core.Tree;
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
    // TODO: this doesn't really belong here
    public Asn1TreeNode? SelectedNode {
        get => selectedNode;
        set {
            selectedNode = value;
            OnPropertyChanged();
        }
    }
    // TODO: this doesn't really belong here?
    public NodeViewOptions NodeViewOptions { get; } = viewOptions;
    public ReadOnlyObservableCollection<Asn1TreeNode> Tree => new(_tree);


    AsnNodeValue createNewNode(Byte[] nodeRawData, Asn1TreeNode? node) {
        var nodeValue = new AsnNodeValue(new Asn1Reader(nodeRawData));
        if (node is not null) {
            nodeValue.Offset = node.Value.Offset + node.Value.TagLength;
            //node.Depth += SelectedNode.Value.Depth;
        }
        _rawData.InsertRange(nodeValue.Offset, nodeRawData);

        return nodeValue;
    }
    void setRootNode(Asn1TreeNode node) {
        _tree.Clear();
        _tree.Add(node);
        FinishBinaryUpdate();
    }

    public Asn1TreeNode AddNode(Byte[] nodeRawData, Asn1TreeNode? parent) {
        var nodeValue = createNewNode(nodeRawData, parent);
        Asn1TreeNode node;
        if (Tree.Count == 0) {
            // add new root node
            node = new Asn1TreeNode(nodeValue, this);
            setRootNode(node);
        } else {
            node = parent!.AddChild(nodeValue, true);
            FinishBinaryUpdate();
        }

        return node;
    }
    public async Task InsertNode(Byte[] nodeRawData, Asn1TreeNode node, NodeAddOption option) {
        if (node is null) {
            throw new ArgumentNullException(nameof(node));
        }
        var childNode = await AsnTreeBuilder.BuildTreeAsync(nodeRawData, this);
        Int32 newOffset = node.Value.Offset;
        if (option != NodeAddOption.Before) {
            newOffset += node.Value.TagLength;
        }
        _rawData.InsertRange(newOffset, nodeRawData);
        switch (option) {
            case NodeAddOption.Before:
            case NodeAddOption.After:
                node.Parent!.InsertChildNode(childNode, node, option);
                break;
            case NodeAddOption.Last:
                node.InsertChildNode(
                    childNode,
                    node,
                    option
                );
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(option), option, null);
        }
        
        FinishBinaryUpdate();
    }
    public void RemoveNode(Asn1TreeNode nodeToRemove) {
        if (nodeToRemove!.Parent is null) {
            Reset();
        } else {
            _rawData.RemoveRange(nodeToRemove.Value.Offset, nodeToRemove.Value.TagLength);
            nodeToRemove.Parent.RemoveChild(nodeToRemove);
        }
        FinishBinaryUpdate();
    }
    public void UpdateNodeBinaryCopy(IEnumerable<Byte> newBytes, AsnNodeValue nodeValue) {
        _rawData.RemoveRange(nodeValue.Offset, nodeValue.TagLength);
        _rawData.InsertRange(nodeValue.Offset, newBytes);
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
        _rawData.Clear();
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event EventHandler? RequireTreeRefresh;
}