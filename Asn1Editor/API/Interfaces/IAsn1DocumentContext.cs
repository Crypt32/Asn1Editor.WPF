using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.Core.Tree;

namespace SysadminsLV.Asn1Editor.API.Interfaces;

public interface IAsn1DocumentContext : INotifyCollectionChanged {
    /// <summary>
    /// Gets the raw binary data associated with the data source.
    /// </summary>
    /// <remarks>
    /// This property provides a read-only collection of bytes representing the raw data.
    /// It is commonly used for operations such as parsing, updating, or manipulating ASN.1 structures.
    /// </remarks>
    IReadOnlyList<Byte> RawData { get; }
    /// <summary>
    /// Gets or sets active node.
    /// </summary>
    AsnTreeNode? SelectedNode { get; set; }
    /// <summary>
    /// Gets tree node view options.
    /// </summary>
    NodeViewOptions NodeViewOptions { get; }
    /// <summary>
    /// Gets current ASN.1 node tree.
    /// </summary>
    ReadOnlyObservableCollection<AsnTreeNode> Tree { get; }

    /// <summary>
    /// Initializes the data source with the provided raw binary data.
    /// </summary>
    /// <param name="rawData">
    /// A collection of bytes representing the raw binary data to initialize the data source.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation of initializing the data source.
    /// </returns>
    Task InitializeFromRawData(IEnumerable<Byte> rawData);
    /// <summary>
    /// Appends new node to the end of selected node's children list.
    /// </summary>
    /// <param name="nodeRawData">Node binary data.</param>
    /// <param name="parent">Parent node to add child to.</param>
    /// <returns>Inserted node.</returns>
    Task<AsnTreeNode> AddNode(Byte[] nodeRawData, AsnTreeNode? parent);
    /// <summary>
    /// Inserts a new node under currently selected node.
    /// </summary>
    /// <param name="nodeRawData">Node binary data.</param>
    /// <param name="node"></param>
    /// <param name="option">Insertion option.</param>
    Task InsertNode(Byte[] nodeRawData, AsnTreeNode node, NodeAddOption option);
    /// <summary>
    /// Removes specified node from the tree.
    /// </summary>
    /// <param name="nodeToRemove">Node to remove.</param>
    void RemoveNode(AsnTreeNode nodeToRemove);

    void UpdateNodeBinaryCopy(IReadOnlyCollection<Byte> newBytes, AsnNodeValue nodeValue);
    void UpdateNodeLength(AsnTreeNode node, IReadOnlyCollection<Byte> newLenBytes);
    void FinishBinaryUpdate();
    /// <summary>
    /// Resets current data source, which clears tree, backing binary source and sets <see cref="SelectedNode"/> to <c>null</c>.
    /// </summary>
    void Reset();
}