using System;
using System.Linq;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.Core.Data;
using SysadminsLV.Asn1Parser;

namespace SysadminsLV.Asn1Editor.Core.Tree;

/// <summary>
/// Provides methods to construct an Abstract Syntax Notation One (ASN.1) tree structure 
/// from raw binary data. This class includes both synchronous and asynchronous methods 
/// for building the tree, which represents the hierarchical structure of ASN.1 data.
/// </summary>
/// <remarks>
/// The <see cref="AsnTreeBuilder"/> class is designed to parse binary ASN.1 data and 
/// create a tree of <see cref="AsnTreeNode"/> objects. Each node in the tree corresponds 
/// to a specific ASN.1 structure, and the tree can be used for further processing, 
/// visualization, or manipulation of the ASN.1 data.
/// </remarks>
public static class AsnTreeBuilder {
    public static AsnTreeNode BuildTree(Byte[] rawData, IBinarySource binaryOps, INodeViewOptions viewOptions) {
        var asn = new Asn1Reader(rawData);
        asn.BuildOffsetMap();
        var rootValue = new AsnNodeValue(asn);
        var rootNode = new AsnTreeNode(rootValue, binaryOps, viewOptions);

        if (asn.NextOffset == 0) {
            return rootNode;
        }

        buildTree(asn, rootNode, binaryOps, viewOptions);
        return rootNode;
    }

    /// <summary>
    /// Asynchronously constructs an Abstract Syntax Notation One (ASN.1) tree structure 
    /// from raw binary data.
    /// </summary>
    /// <param name="rawData">
    /// The raw binary data to be parsed into an ASN.1 tree structure.
    /// </param>
    /// <param name="binaryOps">
    /// An implementation of <see cref="IBinarySource"/> that provides methods for 
    /// manipulating the binary data.
    /// </param>
    /// <param name="viewOptions">
    /// An implementation of <see cref="INodeViewOptions"/> that specifies options for 
    /// customizing the view or behavior of the resulting ASN.1 tree.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the 
    /// root node of the constructed <see cref="AsnTreeNode"/> tree.
    /// </returns>
    /// <remarks>
    /// This method is designed for scenarios where the tree-building process might be 
    /// computationally intensive, allowing it to run asynchronously to avoid blocking 
    /// the calling thread. The resulting tree represents the hierarchical structure of 
    /// the provided ASN.1 data.
    /// </remarks>
    public static Task<AsnTreeNode> BuildTreeAsync(Byte[] rawData, IBinarySource binaryOps, INodeViewOptions viewOptions) {
        return Task.Run(() => BuildTree(rawData, binaryOps, viewOptions));
    }

    static void buildTree(Asn1Reader root, AsnTreeNode tree, IBinarySource binaryOps, INodeViewOptions viewOptions) {
        root.MoveNext();
        Int32 index = 0;
        do {
            var childValue = new AsnNodeValue(root, tree.Value.Depth, tree.Value.Path, index);
            var childNode = new AsnTreeNode(childValue, binaryOps, viewOptions);
            tree.AddChildNode(childNode, index);
            index++;
        } while (root.MoveNextSibling());

        root.Reset();
        foreach (AsnTreeNode node in tree.Children.Where(node => node.Value is { IsContainer: true, PayloadLength: > 0 })) {
            root.Seek(node.Value.Offset);
            buildTree(root, node, binaryOps, viewOptions);
        }
    }
}
