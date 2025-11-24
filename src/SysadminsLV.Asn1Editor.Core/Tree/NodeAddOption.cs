namespace SysadminsLV.Asn1Editor.Core.Tree; 

/// <summary>
/// Specifies the options for adding a new node relative to the currently selected node.
/// </summary>
public enum NodeAddOption {
    /// <summary>
    /// Adds the new node before the currently selected node.
    /// </summary>
    Before,
    /// <summary>
    /// Adds the new node after the currently selected node.
    /// </summary>
    After,
    /// <summary>
    /// Adds the new node as the last child of the currently selected node.
    /// </summary>
    Last
}