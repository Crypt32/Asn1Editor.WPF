using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.Core.ASN;
using SysadminsLV.Asn1Editor.Core.Data;
using SysadminsLV.Asn1Parser;
using SysadminsLV.Asn1Parser.Universal;

namespace SysadminsLV.Asn1Editor.Core.Tree;

/// <summary>
/// Provides functionality to manage and manipulate a hierarchical tree structure of ASN.1 data nodes.
/// </summary>
/// <remarks>
/// The <see cref="TreeCoordinator"/> class is responsible for coordinating operations on ASN.1 tree nodes,
/// such as adding, inserting, updating, and removing nodes. It also manages the underlying binary data source
/// and ensures that changes to the tree structure are propagated correctly.
/// </remarks>
/// <example>
/// Example usage:
/// <code>
/// var viewOptions = new NodeViewOptions();
/// var treeCoordinator = new TreeCoordinator(viewOptions);
/// await treeCoordinator.InitializeFromRawData(rawData);
/// var newNode = await treeCoordinator.AddNode(nodeRawData, parentNode);
/// </code>
/// </example>
public class TreeCoordinator(INodeViewOptions viewOptions) {
    readonly BinaryDataSource _binarySource = [];

    /// <summary>
    /// Gets the root node of the ASN.1 tree structure.
    /// </summary>
    /// <value>
    /// An instance of <see cref="AsnTreeNode"/> representing the root node of the tree, or <c>null</c> if the tree has not been initialized.
    /// </value>
    /// <remarks>
    /// The <see cref="Root"/> property serves as the entry point to the hierarchical ASN.1 tree structure. 
    /// It is populated during the initialization process via the <see cref="TreeCoordinator.InitializeFromRawData(IEnumerable{Byte})"/> method.
    /// </remarks>
    /// <example>
    /// Example usage:
    /// <code>
    /// var treeCoordinator = new TreeCoordinator(viewOptions);
    /// await treeCoordinator.InitializeFromRawData(rawData);
    /// var rootNode = treeCoordinator.Root;
    /// if (rootNode != null) {
    ///     Console.WriteLine($"Root node path: {rootNode.Path}");
    /// }
    /// </code>
    /// </example>
    public AsnTreeNode? Root { get; private set; }
    /// <summary>
    /// Gets the underlying binary data source associated with the ASN.1 tree structure.
    /// </summary>
    /// <remarks>
    /// The <see cref="RawData"/> property provides access to the binary data source that backs the tree structure.
    /// It implements the <see cref="IBinarySource"/> interface, allowing efficient manipulation of binary data
    /// and providing change notifications for seamless integration with data-binding frameworks.
    /// </remarks>
    /// <value>
    /// An object implementing the <see cref="IBinarySource"/> interface, representing the binary data source.
    /// </value>
    /// <example>
    /// Example usage:
    /// <code>
    /// var rawData = treeCoordinator.RawData;
    /// foreach (var byteValue in rawData) {
    ///     Console.WriteLine(byteValue);
    /// }
    /// </code>
    /// </example>
    public IBinarySource RawData => _binarySource;

    /// <summary>
    /// Initializes the ASN.1 tree structure from the provided raw binary data.
    /// </summary>
    /// <param name="rawData">
    /// A collection of bytes representing the raw binary data to be parsed into an ASN.1 tree structure.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation of initializing the tree structure.
    /// </returns>
    /// <remarks>
    /// This method processes the provided binary data, updates the internal binary data source, 
    /// and constructs the hierarchical ASN.1 tree structure. The root node of the tree can be accessed 
    /// via the <see cref="Root"/> property after the initialization is complete.
    /// </remarks>
    /// <example>
    /// Example usage:
    /// <code>
    /// var treeCoordinator = new TreeCoordinator(viewOptions);
    /// await treeCoordinator.InitializeFromRawData(rawData);
    /// var rootNode = treeCoordinator.Root;
    /// if (rootNode != null) {
    ///     Console.WriteLine($"Root node path: {rootNode.Path}");
    /// }
    /// </code>
    /// </example>
    public async Task InitializeFromRawData(IEnumerable<Byte> rawData) {
        Byte[] dataArray = rawData.ToArray();
        _binarySource.BeginUpdate();
        try {
            _binarySource.Clear();
            _binarySource.InsertRange(0, dataArray);
            Root = await AsnTreeBuilder.BuildTreeAsync(dataArray, _binarySource, viewOptions);
            await Root.UpdateNodeHeaderAsync();
        } finally {
            _binarySource.EndUpdate();
        }
    }
    /// <summary>
    /// Adds a new ASN.1 tree node to the tree structure. If the tree is empty, the new node becomes the root.
    /// </summary>
    /// <param name="nodeRawData">The raw binary data representing the ASN.1 node.</param>
    /// <param name="parent">
    /// The parent node to which the new node will be added. This parameter is required for non-root nodes.
    /// </param>
    /// <returns>The newly created <see cref="AsnTreeNode"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="parent"/> is <c>null</c> for a non-root node.
    /// </exception>
    public async Task<AsnTreeNode> AddNode(Byte[] nodeRawData, AsnTreeNode? parent) {
        if (Root is null) {
            var asn = new Asn1Reader(nodeRawData);
            var rootValue = new AsnNodeValue(asn);
            Root = new AsnTreeNode(rootValue, _binarySource, viewOptions);
            _binarySource.InsertRange(0, nodeRawData);
            await Root.UpdateNodeViewAsync();

            return Root;
        }

        if (parent is null) {
            throw new ArgumentNullException(nameof(parent), "Parent node cannot be null for non-root node.");
        }

        var nodeValue = new AsnNodeValue(new Asn1Reader(nodeRawData)) {
            Offset = parent.Value.Offset + parent.Value.TagLength
        };
        _binarySource.InsertRange(nodeValue.Offset, nodeRawData);

        var node = new AsnTreeNode(nodeValue, _binarySource, viewOptions);
        parent.AddChildNode(node, parent.Children.Count);
        // this has to be done before propagating size change, because the propagation relies
        // on the node's Path and MyIndex to update offsets of siblings
        updatePathsFrom(parent, parent.Children.Count - 1);

        propagateSizeChange(parent, node, nodeRawData.Length);
        await Root.UpdateNodeViewAsync();

        return node;
    }
    /// <summary>
    /// Inserts a new ASN.1 tree node into the specified target node at a position determined by the provided option.
    /// </summary>
    /// <param name="targetNode">
    /// The relative target <see cref="AsnTreeNode"/> where the new node will be inserted.
    /// </param>
    /// <param name="option">
    /// A <see cref="NodeAddOption"/> value that specifies the position relative to the target node where the new node will be inserted.
    /// </param>
    /// <param name="nodeRawData">
    /// A byte array containing the raw data of the new node to be inserted.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    /// <remarks>
    /// This method calculates the appropriate insertion position, builds the new node asynchronously,
    /// updates the binary data source, adjusts the paths and offsets of affected nodes, and propagates
    /// size changes to maintain the integrity of the tree structure.
    /// </remarks>
    public async Task InsertNode(AsnTreeNode targetNode, NodeAddOption option, Byte[] nodeRawData) {
        (AsnTreeNode parent, Int32 insertIndex, Int32 binaryOffset) = calculateInsertPosition(targetNode, option);
        AsnTreeNode childNode = await AsnTreeBuilder.BuildTreeAsync(nodeRawData, _binarySource, viewOptions);

        _binarySource.InsertRange(binaryOffset, nodeRawData);
        parent.AddChildNode(childNode, insertIndex);
        // this has to be done before propagating size change, because the propagation relies
        // on the node's Path and MyIndex to update offsets of siblings
        updatePathsFrom(parent, insertIndex);

        // shift the inserted node and its subtree to the correct offset in the binary structure
        // it will not be updated by the propagation of size change
        childNode.UpdateOffset(binaryOffset);
        propagateSizeChange(parent, childNode, nodeRawData.Length);
        await Root!.UpdateNodeViewAsync();
    }
    /// <summary>
    /// Removes the specified node from the tree structure. If the node is the root node, the tree is reset.
    /// Updates the binary data source and adjusts the offsets and paths of sibling nodes accordingly.
    /// </summary>
    /// <param name="nodeToRemove">
    /// The <see cref="AsnTreeNode"/> to be removed from the tree.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="nodeToRemove"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// This method ensures that the tree structure and binary data source remain consistent after the removal
    /// of the specified node. It propagates size changes and updates offsets and paths of affected nodes.
    /// </remarks>
    public void RemoveNode(AsnTreeNode nodeToRemove) {
        if (nodeToRemove.IsRoot) {
            Reset();

            return;
        }

        AsnTreeNode parent = nodeToRemove.Parent!;
        Int32 nodeLength = nodeToRemove.Value.TagLength;
        Int32 nodeIndex = nodeToRemove.MyIndex;

        _binarySource.RemoveRange(nodeToRemove.Value.Offset, nodeLength);
        parent.RemoveChildNode(nodeToRemove);
        // this has to be done before propagating size change, because the propagation relies
        // on the node's Path and MyIndex to update offsets of siblings
        updatePathsFrom(parent, nodeIndex);

        propagateSizeChange(parent, nodeToRemove, -nodeLength);
        // explicitly update offsets of next sibling after the removed node only. Other sibling
        // offsets will be updated during propagation of size change, which also updates offsets
        // in the subtree of the removed node.
        if (parent.Children.Count >= nodeIndex + 1) {
            parent.Children[nodeIndex].UpdateOffset(-nodeLength);
        }
        Root!.UpdateNodeHeader();
    }
    /// <summary>
    /// Updates the specified ASN.1 tree node with new data and recalculates its metadata.
    /// </summary>
    /// <param name="node">
    /// The <see cref="AsnTreeNode"/> to be updated. This parameter cannot be <c>null</c>.
    /// </param>
    /// <param name="newData">
    /// The new binary data to replace the existing data in the specified node.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="node"/> parameter is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// This method updates the binary data of the specified node, recalculates its metadata,
    /// and propagates any size changes up the tree if necessary. It also updates the node's view
    /// and handles specific cases such as BIT_STRING unused bits.
    /// </remarks>
    public void UpdateNode(AsnTreeNode node, Byte[] newData) {
        if (node is null) {
            throw new ArgumentNullException(nameof(node));
        }

        // Parse the new encoded data to extract metadata
        var asn = new Asn1Reader(newData);

        // Calculate size difference
        Int32 oldNodeSize = node.Value.TagLength;
        Int32 newNodeSize = newData.Length;
        Int32 sizeDifference = newNodeSize - oldNodeSize;

        // Replace binary data
        _binarySource.ReplaceRange(node.Value.Offset, oldNodeSize, newData);

        // Update node metadata
        node.Value.PayloadLength = asn.PayloadLength;
        node.Value.PayloadStartOffset = node.Value.Offset + asn.TagLength - asn.PayloadLength;
        node.Value.ExplicitValue = AsnDecoder.GetViewValue(asn);

        // Handle BIT_STRING unused bits
        if (asn.Tag == (Byte)Asn1Type.BIT_STRING) {
            var bitString = (Asn1BitString)asn.GetTagObject();
            node.Value.UnusedBits = bitString.UnusedBits;
        } else {
            node.Value.UnusedBits = 0;
        }

        // If the node size changed, propagate the change up the tree
        if (sizeDifference != 0 && node.Parent is not null) {
            propagateSizeChange(node.Parent, node, sizeDifference);
        }
        Root!.UpdateNodeView();
    }

    public void Reset() {
        Root = null;
        _binarySource.Clear();
    }

    /// <summary>
    /// Propagates a payload size change up the ASN.1 tree and updates the underlying
    /// binary representation accordingly.
    /// </summary>
    /// <remarks>
    /// This method handles the cascading effects caused by ASN.1 length encoding.
    /// <para>
    ///     When the payload size of a node changes (e.g. due to insertion or removal
    ///     of bytes in a descendant), the following must occur:
    /// </para>
    /// <list type="number">
    /// <item>
    ///     The payload length of each ancestor is increased by the cumulative difference
    ///     produced at lower levels.</item>
    /// <item>The encoded length field of each ancestor is recalculated.</item>
    /// <item>
    ///     If the ASN.1 length encoding crosses a boundary (e.g. 127 → 128, 255 → 256),
    ///     the number of bytes used to encode the length may change.
    /// </item>
    /// <item>
    ///     Any increase in the length field size shifts the binary layout of:
    ///     <list type="bullet">
    ///         <item>the node’s payload start offset,</item>
    ///         <item>all nodes in its subtree,</item>
    ///         <item>and all following siblings in the parent node.</item>
    ///     </list>
    /// </item>
    /// <item>
    ///     That header-length expansion becomes part of the cumulative size
    ///     difference and must be propagated further up the tree.
    /// </item>
    /// </list>
    /// <para>
    ///     This cascading behavior can repeat at multiple ancestor levels if
    ///     additional length-encoding boundaries are crossed.
    /// </para>
    /// <para>The method guarantees:</para>
    /// <list type="bullet">
    ///     <item>Consistent PayloadLength values for all affected ancestors.</item>
    ///     <item>Correct recalculation of encoded length bytes.</item>
    ///     <item>Proper offset adjustment for all impacted nodes in the binary structure.</item>
    ///     <item>A single batched binary update via BeginUpdate/EndUpdate to prevent intermediate inconsistent states.</item>
    /// </list>
    /// <para>
    ///     This algorithm is sensitive to ASN.1 length encoding rules and must be modified with extreme care.
    /// </para>
    /// </remarks>
    void propagateSizeChange(AsnTreeNode parent, AsnTreeNode source, Int32 difference) {
        _binarySource.BeginUpdate();
        try {
            AsnTreeNode? current = parent;
            Int32 cumulativeDiff = difference;

            while (current is not null) {
                current.Value.PayloadLength += cumulativeDiff;
                Byte[] newLenBytes = Asn1Utils.GetLengthBytes(current.Value.PayloadLength);
                Int32 oldLenBytesCount = current.Value.HeaderLength - 1;
                Int32 lenDiff = newLenBytes.Length - oldLenBytesCount;

                _binarySource.ReplaceRange(current.Value.Offset + 1, oldLenBytesCount, newLenBytes);

                if (lenDiff != 0) {
                    current.Value.PayloadStartOffset += lenDiff;
                    updateOffsetsInSubtree(current, lenDiff);
                    cumulativeDiff += lenDiff;
                }

                updateOffsetsFromSibling(source, cumulativeDiff);
                source = current;
                current = current.Parent;
            }
        } finally {
            _binarySource.EndUpdate();
        }
    }
    static void updateOffsetsFrom(AsnTreeNode parent, Int32 startIndex, Int32 difference) {
        for (Int32 i = startIndex; i < parent.Children.Count; i++) {
            parent.Children[i].UpdateOffset(difference);
        }
    }
    static void updateOffsetsInSubtree(AsnTreeNode node, Int32 difference) {
        foreach (AsnTreeNode child in node.Children) {
            child.UpdateOffset(difference);
        }
    }
    static void updateOffsetsFromSibling(AsnTreeNode node, Int32 difference) {
        if (node.Parent is not null) {
            updateOffsetsFrom(node.Parent, node.MyIndex + 1, difference);
        }
    }
    static void updatePathsFrom(AsnTreeNode parent, Int32 startIndex) {
        for (Int32 i = startIndex; i < parent.Children.Count; i++) {
            parent.Children[i].UpdatePath(parent.Path, i);
        }
    }

    static (AsnTreeNode parent, Int32 insertIndex, Int32 binaryOffset) calculateInsertPosition(AsnTreeNode targetNode, NodeAddOption option) {
        return option switch {
            NodeAddOption.Before => (targetNode.Parent!, targetNode.Parent!.Children.IndexOf(targetNode), targetNode.Value.Offset),
            NodeAddOption.After  => (targetNode.Parent!, targetNode.Parent!.Children.IndexOf(targetNode) + 1, targetNode.Value.Offset + targetNode.Value.TagLength),
            NodeAddOption.Last   => (targetNode, targetNode.Children.Count, targetNode.Value.Offset + targetNode.Value.TagLength),
            _                    => throw new ArgumentOutOfRangeException(nameof(option))
        };
    }
}