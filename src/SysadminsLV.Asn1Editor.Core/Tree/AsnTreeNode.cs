using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.Core.Data;
using SysadminsLV.Asn1Parser;

namespace SysadminsLV.Asn1Editor.Core.Tree;

/// <summary>
/// Represents a node in an ASN.1 tree structure. This class provides functionality for managing
/// hierarchical relationships, navigating sibling nodes, and performing operations on the tree.
/// </summary>
/// <remarks>
/// The <see cref="AsnTreeNode"/> class is designed to work with ASN.1 data structures, allowing
/// for manipulation and traversal of the tree. Each node contains a value, a reference to its
/// parent, and a collection of child nodes. The class also supports operations such as adding
/// and removing child nodes, updating offsets and paths, and retrieving encoded values.
/// </remarks>
public class AsnTreeNode {
    readonly IBinarySource _binarySource;
    readonly INodeViewOptions _viewOptions;
    readonly ObservableCollection<AsnTreeNode> _children = [];

    public AsnTreeNode(AsnNodeValue value, IBinarySource binarySource, INodeViewOptions viewOptions) {
        _binarySource = binarySource;
        Children = new ReadOnlyObservableCollection<AsnTreeNode>(_children);
        Value = value;
        _viewOptions = viewOptions;
    }

    /// <summary>
    /// Gets or sets the parent node of the current <see cref="AsnTreeNode"/>.
    /// </summary>
    /// <remarks>
    /// This property is used to establish the hierarchical relationship between nodes in the ASN.1 tree structure.
    /// A value of <c>null</c> indicates that the current node is the root node.
    /// </remarks>
    public AsnTreeNode? Parent { get; internal set; }
    /// <summary>
    /// Gets a read-only collection of child nodes associated with the current ASN.1 tree node.
    /// </summary>
    /// <remarks>
    /// This property provides access to the child nodes of the current <see cref="AsnTreeNode"/> instance.
    /// The collection is read-only and reflects the hierarchical structure of the ASN.1 data.
    /// </remarks>
    /// <value>
    /// A <see cref="ReadOnlyObservableCollection{T}"/> containing the child nodes of the current node.
    /// </value>
    public ReadOnlyObservableCollection<AsnTreeNode> Children { get; }
    /// <summary>
    /// Gets the value associated with the current ASN.1 tree node.
    /// </summary>
    /// <remarks>
    /// The <see cref="AsnNodeValue"/> object encapsulates metadata and data
    /// related to the ASN.1 structure represented by this node, such as its tag,
    /// payload, offsets, and path within the tree.
    /// </remarks>
    public AsnNodeValue Value { get; }
    /// <summary>
    /// Gets or sets the hierarchy path to the node in form: /0/1/4/3/..., where values represent zero-based index of the node in subtree.
    /// </summary>
    public String Path => Value.Path;
    /// <summary>
    /// Gets or sets the zero-based index of the current node within its parent's collection of child nodes.
    /// </summary>
    /// <remarks>
    /// This property is used to determine the position of the current node relative to its siblings.
    /// It is updated automatically when the node is added to or removed from its parent's collection.
    /// </remarks>
    public Int32 MyIndex { get; internal set; }
    /// <summary>
    /// Gets a value indicating whether this node is the root of the tree.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this node is the root of the tree; otherwise, <see langword="false"/>.
    /// </value>
    public Boolean IsRoot => Parent is null;
    /// <summary>
    /// Gets the previous sibling of the current <see cref="AsnTreeNode"/> in the parent's child collection.
    /// </summary>
    /// <remarks>
    /// If the current node is the root node or the first child in the parent's child collection,
    /// this property returns <c>null</c>.
    /// </remarks>
    /// <value>
    /// The previous sibling node, or <c>null</c> if there is no previous sibling.
    /// </value>
    public AsnTreeNode? PreviousSibling {
        get {
            // there are no siblings for root node
            if (IsRoot) {
                return null;
            }

            // this node is the first element in parent node. No previous sibling.
            return MyIndex == 0
                ? null
                : Parent!.Children[MyIndex - 1];
        }
    }
    /// <summary>
    /// Gets the next sibling of the current <see cref="AsnTreeNode"/> in the parent's child collection.
    /// </summary>
    /// <remarks>
    /// If the current node is the root node or the last child in the parent's collection, this property returns <c>null</c>.
    /// </remarks>
    /// <value>
    /// The next sibling node, or <c>null</c> if there is no next sibling.
    /// </value>
    public AsnTreeNode? NextSibling {
        get {
            // there are no siblings for root node
            if (IsRoot) {
                return null;
            }

            // this node is the last element in parent node. No next sibling.
            return MyIndex + 1 > Parent!.Children.Count
                ? null
                : Parent.Children[MyIndex + 1];
        }
    }

    /// <summary>
    /// Flattens the hierarchical structure of the current ASN.1 tree node and its descendants
    /// into a single enumerable collection of nodes.
    /// </summary>
    /// <remarks>
    /// This method performs a depth-first traversal of the tree, starting from the current node.
    /// It includes the current node and all its child nodes in the resulting collection.
    /// </remarks>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> containing the current node and all its descendant nodes
    /// in a flattened structure.
    /// </returns>
    public IEnumerable<AsnTreeNode> Flatten() {
        return new[] { this }.Union(Children.SelectMany(x => x.Flatten()));
    }

    internal void AddChildNode(AsnTreeNode child, Int32 index) {
        child.Parent = this;
        _children.Insert(index, child);
        Value.IsContainer = true;
    }

    internal void RemoveChildNode(AsnTreeNode node) {
        _children.RemoveAt(node.MyIndex);
        if (Children.Count == 0 && !IsRoot) {
            Value.IsContainer = false;
        }
    }

    // ITreeOperations implementation
    public void UpdateOffset(Int32 difference) {
        Value.Offset += difference;
        foreach (AsnTreeNode child in Children) {
            child.UpdateOffset(difference);
        }
    }

    public void UpdatePath(String parentPath, Int32 index) {
        Value.Path = parentPath + "/" + index;
        MyIndex = index;
        for (Int32 i = 0; i < Children.Count; i++) {
            Children[i].UpdatePath(Path, i);
        }
    }

    // View updates
    public void UpdateNodeView(Func<AsnTreeNode, Boolean>? filter = null) {
        updateNodeView(this, filter);
    }
    public Task UpdateNodeViewAsync(Func<AsnTreeNode, Boolean>? filter = null) {
        return Task.Run(() => UpdateNodeView(filter));
    }

    public void UpdateNodeHeader(Func<AsnTreeNode, Boolean>? filter = null) {
        updateNodeHeader(this, filter);
    }
    public Task UpdateNodeHeaderAsync(Func<AsnTreeNode, Boolean>? filter = null) {
        return Task.Run(() => UpdateNodeView(filter));
    }

    void updateNodeView(AsnTreeNode node, Func<AsnTreeNode, Boolean>? filter) {
        if (filter is null || filter(node)) {
            node.Value.UpdateNode(_binarySource, _viewOptions);
        }
        foreach (AsnTreeNode child in node.Children) {
            updateNodeView(child, filter);
        }
    }
    void updateNodeHeader(AsnTreeNode node, Func<AsnTreeNode, Boolean>? filter) {
        if (filter is null || filter(node)) {
            node.Value.UpdateNodeHeader(_binarySource, _viewOptions);
        }
        foreach (AsnTreeNode child in node.Children) {
            updateNodeView(child, filter);
        }
    }


    public Byte[] GetEncodedValue() {
        Int32 skip = Value.Tag == (Byte)Asn1Type.BIT_STRING
            ? Value.PayloadStartOffset + 1
            : Value.PayloadStartOffset;
        Int32 take = Value.Tag == (Byte)Asn1Type.BIT_STRING
            ? Value.PayloadLength - 1
            : Value.PayloadLength;

        return _binarySource.Skip(skip).Take(take).ToArray();
    }

    #region Equals
    protected Boolean Equals(AsnTreeNode other) {
        return String.Equals(Path, other.Path);
    }
    public override Int32 GetHashCode() {
        return Path.GetHashCode();
    }
    public override Boolean Equals(Object? obj) {
        if (ReferenceEquals(null, obj)) { return false; }
        if (ReferenceEquals(this, obj)) { return true; }
        return obj.GetType() == GetType() && Equals((AsnTreeNode)obj);
    }
    #endregion
}