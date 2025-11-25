using System;

namespace SysadminsLV.Asn1Editor.Core.Tree;

/// <summary>
/// Represents ASN node value members necessary for built-in hex viewer.
/// </summary>
public interface IHexAsnNode {
    /// <summary>
    /// Gets node start binary offset.
    /// </summary>
    Int32 Offset { get; }
    /// <summary>
    /// Gets node value start offset.
    /// </summary>
    Int32 PayloadStartOffset { get; }
    /// <summary>
    /// Gets node value length in bytes (excluding tag and length identifier).
    /// </summary>
    Int32 PayloadLength { get; }

    /// <summary>
    /// Event triggered when node's binary value is changed. This event is used to
    /// re-color hex viewer to reflect current state.
    /// </summary>
    event EventHandler DataChanged;
}