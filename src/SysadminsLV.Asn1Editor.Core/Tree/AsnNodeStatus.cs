namespace SysadminsLV.Asn1Editor.Core.Tree;

/// <summary>
/// Represents the status of a node in the ASN.1 tree structure.
/// </summary>
public enum AsnNodeStatus {
    /// <summary>
    /// The node has not been modified since it was last loaded or saved.
    /// </summary>
    Unchanged = 0,
    /// <summary>
    /// The node has been modified.
    /// </summary>
    Modified  = 1,
    /// <summary>
    /// The node has been newly added.
    /// </summary>
    Added     = 2,
    /// <summary>
    /// The child node has been deleted.
    /// </summary>
    Deleted   = 3
}