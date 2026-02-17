using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.Core.Data;
using SysadminsLV.Asn1Parser;

namespace SysadminsLV.Asn1Editor.Core.Tree;

public class TreeCoordinator(INodeViewOptions viewOptions) {
    readonly BinaryDataSource _binarySource = [];

    public AsnTreeNode? Root { get; private set; }
    public IBinarySource RawData => _binarySource;

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

    public AsnTreeNode AddNode(Byte[] nodeRawData, AsnTreeNode parent) {
        if (Root is null) {
            var asn = new Asn1Reader(nodeRawData);
            var rootValue = new AsnNodeValue(asn);
            Root = new AsnTreeNode(rootValue, _binarySource, viewOptions);
            _binarySource.InsertRange(0, nodeRawData);
            return Root;
        }

        var nodeValue = new AsnNodeValue(new Asn1Reader(nodeRawData)) {
            Offset = parent.Value.Offset + parent.Value.TagLength
        };
        _binarySource.InsertRange(nodeValue.Offset, nodeRawData);

        var node = new AsnTreeNode(nodeValue, _binarySource, viewOptions);
        parent.AddChildNode(node, parent.Children.Count);

        propagateSizeChange(parent, node, nodeRawData.Length);
        updatePathsFrom(parent, parent.Children.Count - 1);

        return node;
    }

    public async Task InsertNode(Byte[] nodeRawData, AsnTreeNode targetNode, NodeAddOption option) {
        (AsnTreeNode parent, Int32 insertIndex, Int32 binaryOffset) = calculateInsertPosition(targetNode, option);
        AsnTreeNode childNode = await AsnTreeBuilder.BuildTreeAsync(nodeRawData, _binarySource, viewOptions);

        childNode.Value.Offset = binaryOffset;
        _binarySource.InsertRange(binaryOffset, nodeRawData);
        parent.AddChildNode(childNode, insertIndex);

        propagateSizeChange(parent, childNode, nodeRawData.Length);
        updatePathsFrom(parent, insertIndex);
        updateOffsetsFrom(parent, insertIndex + 1, nodeRawData.Length);
    }

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

        propagateSizeChange(parent, nodeToRemove, -nodeLength);
        updatePathsFrom(parent, nodeIndex);
        updateOffsetsFrom(parent, nodeIndex, -nodeLength);
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
            NodeAddOption.After => (targetNode.Parent!, targetNode.Parent!.Children.IndexOf(targetNode) + 1, targetNode.Value.Offset + targetNode.Value.TagLength),
            NodeAddOption.Last => (targetNode, targetNode.Children.Count, targetNode.Value.Offset + targetNode.Value.TagLength),
            _ => throw new ArgumentOutOfRangeException(nameof(option))
        };
    }
}