using System.Windows.Input;

namespace SysadminsLV.Asn1Editor.Controls;

/// <summary>
/// Provides a set of standard commands for interacting with an <see cref="AsnTreeView"/> control.
/// These commands follow the same pattern as <see cref="System.Windows.Input.ApplicationCommands"/>
/// and are intended to be handled by the host application via <see cref="System.Windows.Input.CommandBinding"/>.
/// </summary>
public static class AsnTreeViewCommands {
    /// <summary>
    /// Opens a text viewer for the selected node's decoded value.
    /// </summary>
    public static readonly RoutedUICommand ShowNodeTextViewer = new(
        "View node text...",
        nameof(ShowNodeTextViewer),
        typeof(AsnTreeViewCommands));

    /// <summary>
    /// Opens the binary converter window pre-populated with the selected node's raw bytes.
    /// </summary>
    public static readonly RoutedUICommand ShowNodeInConverter = new(
        "View encoded node...",
        nameof(ShowNodeInConverter),
        typeof(AsnTreeViewCommands));

    /// <summary>
    /// Displays hash values computed over the selected node's raw bytes.
    /// </summary>
    public static readonly RoutedUICommand ShowNodeHashCommand = new(
        "View node hash values",
        nameof(ShowNodeHashCommand),
        typeof(AsnTreeViewCommands));

    /// <summary>
    /// Saves the selected node's raw bytes to a file.
    /// </summary>
    public static readonly RoutedUICommand SaveNodeCommand = new(
        "Save selected node as...",
        nameof(SaveNodeCommand),
        typeof(AsnTreeViewCommands));

    /// <summary>
    /// Opens the node content editor for the selected node.
    /// The command parameter should be a <c>NodeEditMode</c> value; when <see langword="null"/>
    /// the host handler defaults to text-edit mode.
    /// </summary>
    public static readonly RoutedUICommand EditNodeCommand = new(
        "Edit node...",
        nameof(EditNodeCommand),
        typeof(AsnTreeViewCommands));

    /// <summary>
    /// Opens the OID mapping editor for the selected Object Identifier node.
    /// </summary>
    public static readonly RoutedUICommand RegisterOidCommand = new(
        "Edit Object Identifier mapping",
        nameof(RegisterOidCommand),
        typeof(AsnTreeViewCommands));

    /// <summary>
    /// Inserts a new child node after the selected node.
    /// </summary>
    public static readonly RoutedUICommand AddNewNodeCommand = new(
        "New node",
        nameof(AddNewNodeCommand),
        typeof(AsnTreeViewCommands));

    /// <summary>
    /// Deletes the selected node from the tree.
    /// </summary>
    public static readonly RoutedUICommand DeleteNodeCommand = new(
        "Remove",
        nameof(DeleteNodeCommand),
        typeof(AsnTreeViewCommands));

    /// <summary>
    /// Cuts the selected node to the internal clipboard.
    /// </summary>
    public static readonly RoutedUICommand CutNodeCommand = new(
        "Cut",
        nameof(CutNodeCommand),
        typeof(AsnTreeViewCommands));

    /// <summary>
    /// Copies the selected node to the internal clipboard.
    /// </summary>
    public static readonly RoutedUICommand CopyNodeCommand = new(
        "Copy",
        nameof(CopyNodeCommand),
        typeof(AsnTreeViewCommands));

    /// <summary>
    /// Inserts clipboard node content immediately before the selected node.
    /// </summary>
    public static readonly RoutedUICommand PasteBeforeCommand = new(
        "Paste before",
        nameof(PasteBeforeCommand),
        typeof(AsnTreeViewCommands));

    /// <summary>
    /// Inserts clipboard node content immediately after the selected node.
    /// </summary>
    public static readonly RoutedUICommand PasteAfterCommand = new(
        "Paste after",
        nameof(PasteAfterCommand),
        typeof(AsnTreeViewCommands));

    /// <summary>
    /// Inserts clipboard node content as the last child of the selected node.
    /// </summary>
    public static readonly RoutedUICommand PasteLastCommand = new(
        "Insert as last child node",
        nameof(PasteLastCommand),
        typeof(AsnTreeViewCommands));
}