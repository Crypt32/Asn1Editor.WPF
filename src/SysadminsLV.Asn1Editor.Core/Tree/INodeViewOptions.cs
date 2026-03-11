using System;
using System.ComponentModel;

namespace SysadminsLV.Asn1Editor.Core.Tree;

/// <summary>
/// Represents the options for displaying and formatting nodes in the ASN.1 tree view.
/// </summary>
public interface INodeViewOptions : INotifyPropertyChanged {
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

    /// <summary>
    /// Retrieves the options for displaying and formatting integer values in ASN.1 tree nodes.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="IAsnIntegerViewOptions"/> that represents
    /// the current settings for integer display options.
    /// </returns>
    IAsnIntegerViewOptions GetIntegerViewOptions();
    /// <summary>
    /// Retrieves the options for displaying and formatting date and time values in ASN.1 tree nodes.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="IAsnDateTimeViewOptions"/> that represents
    /// the current settings for date and time display options.
    /// </returns>
    IAsnDateTimeViewOptions GetDateTimeViewOptions();
}

/// <summary>
/// Represents the options for displaying date and time values in ASN tree nodes in a specific format.
/// </summary>
public interface IAsnDateTimeViewOptions {
    /// <summary>
    /// Gets or sets a value indicating whether the date and time should be formatted using the ISO 8601 standard.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the ISO 8601 format should be used; otherwise, <see langword="false"/>.
    /// </value>
    Boolean UseISO8601Format { get; set; }
}
/// <summary>
/// Represents the options for displaying integer values in ASN tree nodes in a specific format.
/// </summary>
public interface IAsnIntegerViewOptions {
    /// <summary>
    /// Gets or sets a value indicating whether INTEGER values in the ASN.1 tree view
    /// should be displayed as <see cref="BigInteger"/> instead of Hex string.
    /// </summary>
    Boolean IntegerAsInteger { get; set; }
}