using System;
using System.Linq;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Parser;

namespace SysadminsLV.Asn1Editor.API.Utils.ASN;

public static class AsnTreeBuilder {
    /// <summary>
    /// Builds ASN.1 tree from ASN.1-encoded byte array.
    /// </summary>
    /// <param name="rawData">ASN.1-encoded byte array</param>
    /// <param name="dataSource">Data source to associate root node with.</param>
    /// <returns>Root node with decoded children nodes.</returns>
    /// <remarks>This method is a synchronous version of <see cref="BuildTreeAsync(Byte[], IDataSource)"/></remarks>
    public static Asn1TreeNode BuildTree(Byte[] rawData, IDataSource dataSource) {
        var asn = new Asn1Reader(rawData);
        asn.BuildOffsetMap();
        var rootValue = new Asn1Lite(asn);
        var rootNode = new Asn1TreeNode(rootValue, dataSource);
        if (asn.NextOffset == 0) {
            return rootNode;
        }
        buildTree(asn, rootNode);
        
        return rootNode;
    }
    /// <summary>
    /// Builds ASN.1 tree from ASN.1-encoded byte array.
    /// </summary>
    /// <param name="rawData">ASN.1-encoded byte array</param>
    /// <param name="dataSource">Data source to associate root node with.</param>
    /// <returns>Root node with decoded children nodes.</returns>
    /// <remarks>This method is an asynchronous version of <see cref="BuildTree(Byte[], IDataSource)"/></remarks>
    public static Task<Asn1TreeNode> BuildTreeAsync(Byte[] rawData, IDataSource dataSource) {
        return Task.Run(() => BuildTree(rawData, dataSource));
    }
    /// <summary>
    /// Gets a new ASN.1 tree from existing data source.
    /// </summary>
    /// <param name="dataSource">Data source.</param>
    /// <returns>Root node with decoded children nodes.</returns>
    public static Task<Asn1TreeNode> BuildTreeAsync(IDataSource dataSource) {
        return BuildTreeAsync(dataSource.RawData.ToArray(), dataSource);
    }

    static void buildTree(Asn1Reader root, Asn1TreeNode tree) {
        root.MoveNext();
        Int32 index = 0;
        do {
            tree.AddChild(new Asn1Lite(root, tree.Value.Depth, tree.Value.Path, index));
            index++;
        } while (root.MoveNextSibling());
        root.Reset();
        foreach (Asn1TreeNode node in tree.Children.Where(node => node.Value is { IsContainer: true, PayloadLength: > 0 })) {
            root.Seek(node.Value.Offset);
            buildTree(root, node);
        }
    }
}