using System;
using System.Linq;
using System.Windows.Input;
using SysadminsLV.Asn1Editor.Controls;
using SysadminsLV.Asn1Editor.Core.Tree;
using SysadminsLV.Asn1Parser;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

/// <summary>
/// Represents the editing context for an ASN.1 node value editor.
/// This context is used by DataTemplateSelector to determine which editor control to display
/// and by the editor controls to perform encoding/decoding operations.
/// </summary>
public class AsnValueEditorContext : ViewModelBase {
    public AsnValueEditorContext(AsnTreeNode node, Byte[] initialEncodedValue, Boolean isHexMode = false) {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        OriginalEncodedValue = initialEncodedValue;
        IsHexMode = isHexMode;
        IsHexSwitchEnabled = true;
    }

    /// <summary>
    /// Gets the underlying ASN.1 tree node.
    /// </summary>
    public AsnTreeNode Node { get; }
    /// <summary>
    /// Gets the ASN.1 tag byte.
    /// </summary>
    public Byte Tag => Node.Value.Tag;
    /// <summary>
    /// Gets the ASN.1 tag friendly name.
    /// </summary>
    public String TagName => Node.Value.TagName;
    /// <summary>
    /// Gets a value indicating whether the node is read-only (containers, NULL, etc.).
    /// </summary>
    public Boolean IsReadOnly => Node.Value.IsContainer || Node.Value.Tag == (Byte)Asn1Type.NULL;
    /// <summary>
    /// Gets a value indicating whether the node is a container with nested elements.
    /// </summary>
    public Boolean IsContainer => Node.Value.IsContainer;
    /// <summary>
    /// Gets a value indicating whether the node is a CONTEXT_SPECIFIC tag.
    /// </summary>
    public Boolean IsContextSpecific => Node.Value.IsContextSpecific;
    /// <summary>
    /// Gets or sets a value indicating whether the hex mode toggle is enabled.
    /// </summary>
    /// <value>
    /// <c>true</c> if the hex mode toggle is enabled; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// This property allows enabling or disabling the ability to switch between
    /// hex and non-hex modes in the ASN.1 value editor.
    /// </remarks>
    public Boolean IsHexSwitchEnabled {
        get;
        set {
            field = value;
            OnPropertyChanged();
        }
    }
    /// <summary>
    /// Gets or sets a value indicating whether hex mode is forced.
    /// When true, the hex editor will be used regardless of tag type.
    /// </summary>
    public Boolean IsHexMode { get; set; }

    /// <summary>
    /// Gets or sets the command that validates the ASN.1 node value.
    /// This command is used by client code to request controls to perform validation
    /// and update the <see cref="Result"/> property with the validation outcome.
    /// </summary>
    /// <remarks>
    /// This property is SHALL NOT be set by client code, it is set by tag editor control via <strong>OneWayToSource</strong> binding.
    /// Client code SHALL execute this command to trigger validation in the editor control, which will then update the <see cref="Result"/> property accordingly.
    /// </remarks>
    public ICommand? ValidateCommand {
        get;
        set {
            field = value;
            OnPropertyChanged();
        }
    }
    /// <summary>
    /// Gets or sets the result of the ASN.1 tag validation process.
    /// </summary>
    /// <value>
    /// An instance of <see cref="AsnValueValidationResult"/> representing the validation outcome.
    /// This includes information about whether the validation was successful, the encoded value,
    /// and any associated error message.
    /// </value>
    /// <remarks>
    /// <para>
    ///     This property is used to store the result of validating the ASN.1 tag, which can include
    ///     the encoded value if validation is successful or an error message if validation fails.
    /// </para>
    /// <para>
    ///     This property is set by tag editor control via <strong>OneWayToSource</strong> binding.
    ///     It SHALL NOT be set in user code.
    /// </para>
    /// </remarks>
    public AsnValueValidationResult? Result {
        get;
        set {
            field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the original encoded value for change detection.
    /// </summary>
    public Byte[] OriginalEncodedValue { get; }

    /// <summary>
    /// Gets a value indicating whether the current value differs from the original.
    /// </summary>
    public Boolean HasChanges => Result?.EncodedValue != null && !Result.EncodedValue.SequenceEqual(OriginalEncodedValue);
}