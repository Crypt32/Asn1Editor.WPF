using System;
using System.ComponentModel;
using System.Numerics;

namespace SysadminsLV.Asn1Editor.Core.Tree;

/// <summary>
/// Represents the options for displaying and formatting nodes in the ASN.1 tree view.
/// </summary>
public interface INodeViewOptions : INotifyPropertyChanged {
    /// <summary>
    /// Gets or sets a value indicating whether INTEGER values in the ASN.1 tree view
    /// should be displayed as <see cref="BigInteger"/> instead of Hex string.
    /// </summary>
    Boolean IntegerAsInteger { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the content of the ASN.1 node should be displayed.
    /// </summary>
    Boolean ShowContent { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the node information, such as tag number, offset, and length, 
    /// should be displayed in hexadecimal format in the ASN.1 tree view.
    /// </summary>
    Boolean ShowInHex { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the length of the node should be displayed
    /// in the ASN.1 tree view.
    /// </summary>
    Boolean ShowNodeLength { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the node offset should be displayed in the ASN.1 tree view.
    /// </summary>
    /// <remarks>
    /// The node offset represents the position of the node within the ASN.1 structure. 
    /// This option is useful for debugging or analyzing the structure of ASN.1 data.
    /// </remarks>
    Boolean ShowNodeOffset { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the node path should be displayed in the ASN.1 tree view.
    /// </summary>
    /// <remarks>
    /// The node path represents the hierarchical location of the node within the ASN.1 tree structure.
    /// </remarks>
    Boolean ShowNodePath { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the tag number of an ASN.1 node should be displayed in the tree view.
    /// </summary>
    Boolean ShowTagNumber { get; set; }
}