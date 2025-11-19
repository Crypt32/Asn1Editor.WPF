#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Parser;

namespace SysadminsLV.Asn1Editor.API.ModelObjects;

public class Asn1TreeNode : INotifyPropertyChanged {
    readonly IDataSource _dataSource;
    readonly ObservableCollection<Asn1TreeNode> _children = [];

    Boolean isSelected, isExpanded = true;
    Byte tag, unusedBits;
    Int32 offset, offsetChange, payloadLength, depth;

    public Asn1TreeNode(Asn1Lite value, IDataSource dataSource) {
        _dataSource = dataSource;
        Children = new ReadOnlyObservableCollection<Asn1TreeNode>(_children);
        setNodeValues(value);

        Value = value;
        Path = value.Path;
        String[] tokens = Path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        MyIndex = String.IsNullOrEmpty(Path)
            ? 0
            : Convert.ToInt32(tokens[tokens.Length - 1]);
        Value.PropertyChanged += valuePropertyChanged;
    }
    public Asn1TreeNode this[Int32 index] => Children[index];

    /// <summary>
    /// Gets or sets the indexed path to the node in form: /0/1/4/3/..., where values represent zero-based index of the node in subtree.
    /// </summary>
    public String Path { get; private set; }
    /// <summary>
    /// Gets the index of current node in parent's children collection.
    /// </summary>
    public Int32 MyIndex { get; private set; }
    /// <summary>
    /// Gets the value that indicates whether the current node is root node. Root node is a node with no parent.
    /// </summary>
    public Boolean IsRoot => Parent is null;
    /// <summary>
    /// Gets or sets a tree node value.
    /// </summary>
    public Asn1Lite Value { get; }
    /// <summary>
    /// Gets a collection of child nodes.
    /// </summary>
    public ReadOnlyObservableCollection<Asn1TreeNode> Children { get; }

    public void InsertChildNode(Asn1TreeNode nodeToInsert, Asn1TreeNode caller, NodeAddOption option) {
        Int32 indexToInsert, newOffset;
        nodeToInsert.Parent = this;
        switch (option) {
            case NodeAddOption.Before:
                indexToInsert = Children.IndexOf(caller);
                newOffset = caller.Value.Offset;
                break;
            case NodeAddOption.After:
                indexToInsert = Children.IndexOf(caller) + 1;
                newOffset = caller.Value.Offset + caller.Value.TagLength;
                break;
            case NodeAddOption.Last:
                indexToInsert = Children.Count;
                newOffset = Value.Offset + Value.TagLength;
                break;
            default:
                return;
        }
        Value.IsContainer = true;
        if (indexToInsert < 0) { return; }
        nodeToInsert.Value.Offset = newOffset;
        _children.Insert(indexToInsert, nodeToInsert);
        notifySizeChanged(nodeToInsert, nodeToInsert.Value.TagLength);
        for (Int32 index = indexToInsert; index < Children.Count; index++) {
            updatePath(Children[index], Path, index);
        }
        foreach (Asn1TreeNode child in Children[indexToInsert].Children) {
            child.updateOffset(newOffset);
        }
    }
    public Asn1TreeNode AddChild(Asn1Lite value, Boolean forcePathUpdate = false) {
        var node = new Asn1TreeNode(value, _dataSource) { Parent = this };
        _children.Add(node);
        if (forcePathUpdate) {
            notifySizeChanged(node, node.Value.TagLength);
            updatePath(node, Path, Children.Count - 1);
        }
        Value.IsContainer = true;

        return node;
    }
    public void RemoveChild(Asn1TreeNode node) {
        notifySizeChanged(node, -node.Value.TagLength);
        _children.RemoveAt(node.MyIndex);
        // update path only below removed node
        for (Int32 childIndex = node.MyIndex; childIndex < Children.Count; childIndex++) {
            updatePath(Children[childIndex], Path, childIndex);
        }
        if (Children.Count == 0 && !IsRoot) {
            Value.IsContainer = false;
        }
    }
    public IEnumerable<Asn1TreeNode> Flatten() {
        return new[] { this }.Union(Children.SelectMany(x => x.Flatten()));
    }
    public IDataSource GetDataSource() {
        return _dataSource;
    }

    public void UpdateNodeView(Func<Asn1TreeNode, Boolean>? filter) {
        updateNodeView(this, filter);
    }
    public Task UpdateNodeViewAsync(Func<Asn1TreeNode, Boolean>? filter) {
        return Task.Run(() => UpdateNodeView(filter));
    }
    public void UpdateNodeHeader(Func<Asn1TreeNode, Boolean>? filter) {
        updateNodeHeader(this, filter);
    }
    public Task UpdateNodeHeaderAsync(Func<Asn1TreeNode, Boolean>? filter) {
        return Task.Run(() => UpdateNodeView(filter));
    }

    void updateNodeView(Asn1TreeNode node, Func<Asn1TreeNode, Boolean>? filter) {
        if (filter is null || filter(node)) {
            node.Value.UpdateNode(_dataSource.RawData, _dataSource.NodeViewOptions);
        }
        foreach (Asn1TreeNode child in node.Children) {
            updateNodeView(child, filter);
        }
    }
    void updateNodeHeader(Asn1TreeNode node, Func<Asn1TreeNode, Boolean>? filter) {
        if (filter is null || filter(node)) {
            node.Value.UpdateNodeHeader(_dataSource.RawData, _dataSource.NodeViewOptions);
        }
        foreach (Asn1TreeNode child in node.Children) {
            updateNodeView(child, filter);
        }
    }
    void notifySizeChanged(Asn1TreeNode source, Int32 difference) {
        Asn1TreeNode t = this;
        do {
            if (t.Children.Count > 0) {
                // update offset of every node below modified (source) node
                Int32 callerIndex = t.Children.IndexOf(source);
                if (callerIndex < 0) { return; }
                for (Int32 index = callerIndex + 1; index < t.Children.Count; index++) {
                    t.Children[index].updateOffset(difference);
                }
            }
            Byte[] newLenBytes = Asn1Utils.GetLengthBytes(t.Value.PayloadLength + difference);
            _dataSource.UpdateNodeLength(t, newLenBytes);
            // check if extra length byte is added. If so, add this byte to difference variable
            Int32 diff = newLenBytes.Length - (t.Value.HeaderLength - 1);
            if (diff != 0) {
                // shift payload start offset to extra length bytes
                t.Value.PayloadStartOffset += diff;
                t.updateOffset(diff);
                t.Value.Offset -= diff; // TODO this: updateOffset method updates current node as well which is not necessary
            }
            source = t;
            t = t.Parent;
            source.Value.PayloadLength += difference;
            difference += diff;
        } while (t is not null);
    }
    void updateOffset(Int32 difference) {
        Value.Offset += difference;
        foreach (Asn1TreeNode child in Children) {
            child.updateOffset(difference);
        }
    }
    void valuePropertyChanged(Object sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(Asn1Lite.OffsetChange) && Parent is not null && Value.OffsetChange != 0) {
            Parent.notifySizeChanged(this, Value.OffsetChange);
        }
    }

    static void updatePath(Asn1TreeNode source, String path, Int32 index) {
        source.Value.Path = source.Path = path + "/" + index;
        source.MyIndex = index;
        Int32 deepness = source.Value.Path.Split(['/'], StringSplitOptions.RemoveEmptyEntries).Length;
        source.Value.Depth = deepness;
        for (Int32 i = 0; i < source.Children.Count; i++) {
            updatePath(source.Children[i], source.Path, i);
        }
    }

    #region Equals
    protected Boolean Equals(Asn1TreeNode other) {
        return String.Equals(Path, other.Path);
    }
    public override Int32 GetHashCode() {
        return Path is not null ? Path.GetHashCode() : 0;
    }
    public override Boolean Equals(Object obj) {
        if (ReferenceEquals(null, obj)) { return false; }
        if (ReferenceEquals(this, obj)) { return true; }
        return obj.GetType() == GetType() && Equals((Asn1TreeNode)obj);
    }
    #endregion

    #region V2 properties
    // value-specific properties
    public Byte Tag {
        get => tag;
        private set {
            tag = value;
            if ((tag & (Byte)Asn1Class.CONTEXT_SPECIFIC) > 0) {
                IsContextSpecific = true;
            }
            OnPropertyChanged();
        }
    }
    public String TagName { get; private set; }
    public Int32 Offset { get; private set; }
    public Int32 PayloadStartOffset { get; private set; }
    public Int32 HeaderLength => PayloadStartOffset - Offset;
    public Int32 PayloadLength { get; private set; }
    public Int32 TagLength => HeaderLength + PayloadLength;
    public Boolean IsContainer { get; set; }
    public Boolean IsContextSpecific { get; set; }

    public Boolean IsExpanded {
        get => isExpanded;
        set {
            isExpanded = value;
            OnPropertyChanged();
        }
    }

    // tree-specific properties
    public Int32 Depth { get; private set; }
    public Asn1TreeNode? Parent { get; private set; }
    public Asn1TreeNode? NextSibling {
        get {
            // there is no siblings for root node
            if (IsRoot) {
                return null;
            }

            // this node is last element in parent node. No next sibling.
            return MyIndex + 1 > Parent!.Children.Count
                ? null
                : Parent.Children[MyIndex + 1];
        }
    }
    public Asn1TreeNode? PreviousSibling {
        get {
            // there is no siblings for root node
            if (IsRoot) {
                return null;
            }

            // this node is first element in parent node. No previous sibling.
            return MyIndex == 0
                ? null
                : Parent!.Children[MyIndex - 1];
        }
    }

    #endregion

    #region V2 methods

    public void AppendNode(Asn1TreeNode node) {

    }
    public void AppendNodes(IEnumerable<Asn1TreeNode> collection) {

    }
    public void InsertNode(Int32 index, Asn1TreeNode node) {

    }
    public void RemoveNode(Asn1TreeNode node) {

    }
    public void RemoveSelf() {

    }
    public void Clear() {

    }
    public Asn1TreeNode Clone() {
        return null;
    }

    #endregion

    #region private methods

    void setNodeValues(Asn1Lite asn) {
        Offset = asn.Offset;
        Tag = asn.Tag;
        TagName = asn.TagName;
        PayloadLength = asn.PayloadLength;
        PayloadStartOffset = asn.PayloadStartOffset;
        IsContainer = asn.IsContainer;
        Depth = 0;
        Path = String.Empty;
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] String? propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}