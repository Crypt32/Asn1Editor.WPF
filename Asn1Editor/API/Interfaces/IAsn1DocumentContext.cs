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
    /// Inserts a new ASN.1 node into the document tree at a specified position relative to an existing node.
    /// </summary>
    /// <param name="node">
    /// The existing <see cref="AsnTreeNode"/> that serves as the reference point for the insertion.
    /// </param>
    /// <param name="option">
    /// Specifies the position where the new node will be inserted relative to the <paramref name="node"/>.
    /// This can be <see cref="NodeAddOption.Before"/>, <see cref="NodeAddOption.After"/>, or <see cref="NodeAddOption.Last"/>.
    /// </param>
    /// <param name="nodeRawData">
    /// The raw byte array representing the data of the new node to be inserted.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation. The task result contains the newly inserted <see cref="AsnTreeNode"/>.
    /// </returns>
    Task InsertNode(AsnTreeNode node, NodeAddOption option, Byte[] nodeRawData);
    /// <summary>
    /// Updates the specified ASN.1 tree node with new raw data.
    /// </summary>
    /// <param name="nodeValue">
    /// The <see cref="AsnTreeNode"/> instance representing the node to be updated.
    /// </param>
    /// <param name="newBytes">
    /// A byte array containing the new raw data to update the node with.
    /// </param>
    /// <remarks>
    /// This method modifies the content of the specified node by replacing its current data
    /// with the provided <paramref name="newBytes"/>. The update may affect the structure
    /// or representation of the node within the ASN.1 tree.
    /// </remarks>
    void UpdateNode(AsnTreeNode nodeValue, Byte[] newBytes);
    /// <summary>
    /// Removes the specified node from the ASN.1 tree structure.
    /// </summary>
    /// <param name="nodeToRemove">
    /// The <see cref="AsnTreeNode"/> instance to be removed from the tree. 
    /// This node must exist within the current tree structure.
    /// </param>
    /// <remarks>
    /// This method updates the tree structure by removing the specified node and its associated data.
    /// If the node has child nodes, they will also be removed. Ensure that the node to be removed
    /// is not null and is part of the current tree.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="nodeToRemove"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the specified node cannot be removed due to constraints in the tree structure.
    /// </exception>
    void RemoveNode(AsnTreeNode nodeToRemove);
    /// <summary>
    /// Resets current data source, which clears tree, backing binary source and sets <see cref="SelectedNode"/> to <c>null</c>.
    /// </summary>
    void Reset();
}