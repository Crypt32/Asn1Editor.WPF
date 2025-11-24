using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.Core.Tree;

namespace SysadminsLV.Asn1Editor.API.Interfaces;

public interface IDataSource : IBinarySource {
    /// <summary>
    /// Gets or sets active node.
    /// </summary>
    Asn1TreeNode? SelectedNode { get; set; }
    /// <summary>
    /// Gets tree node view options.
    /// </summary>
    NodeViewOptions NodeViewOptions { get; }
    /// <summary>
    /// Gets current ASN.1 node tree.
    /// </summary>
    ReadOnlyObservableCollection<Asn1TreeNode> Tree { get; }

    /// <summary>
    /// Appends new node to the end of selected node's children list.
    /// </summary>
    /// <param name="nodeRawData">Node binary data.</param>
    /// <param name="parent">Parent node to add child to.</param>
    /// <returns>Inserted node.</returns>
    Asn1TreeNode AddNode(Byte[] nodeRawData, Asn1TreeNode? parent);
    /// <summary>
    /// Inserts a new node under currently selected node.
    /// </summary>
    /// <param name="nodeRawData">Node binary data.</param>
    /// <param name="node"></param>
    /// <param name="option">Insertion option.</param>
    Task InsertNode(Byte[] nodeRawData, Asn1TreeNode node, NodeAddOption option);
    /// <summary>
    /// Removes specified node from the tree.
    /// </summary>
    /// <param name="nodeToRemove">Node to remove.</param>
    void RemoveNode(Asn1TreeNode nodeToRemove);

    void UpdateNodeBinaryCopy(IEnumerable<Byte> newBytes, Asn1Lite nodeValue);
    void UpdateNodeLength(Asn1TreeNode node, Byte[] newLenBytes);
    void FinishBinaryUpdate();
    /// <summary>
    /// Resets current data source, which clears tree, backing binary source and sets <see cref="SelectedNode"/> to <c>null</c>.
    /// </summary>
    void Reset();

    /// <summary>
    /// Raised when tree refresh required.
    /// </summary>
    event EventHandler RequireTreeRefresh;
}