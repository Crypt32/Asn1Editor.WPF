using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using SysadminsLV.Asn1Editor.API.ViewModel;
using SysadminsLV.Asn1Editor.Controls;
using SysadminsLV.Asn1Editor.Core.ASN;
using SysadminsLV.Asn1Parser;

namespace SysadminsLV.Asn1Editor.API.ModelObjects;

public class Asn1Lite : ViewModelBase, IHexAsnNode {
    const String METADATA_TEMPLATE = """
                                     Tag    : {0} (0x{0:X2}) : {1}
                                     Offset : {2} (0x{2:X2})
                                     Length : {3} (0x{3:X2})
                                     Depth  : {4}
                                     Path   : {5}
                                     
                                     """;

    Byte tag;
    Boolean invalidData;
    Int32 offset, offsetChange;
    String path, header, toolTip;

    public Asn1Lite(Asn1Reader asn) {
        initialize(asn);
        Depth = 0;
        Path = String.Empty;
    }
    public Asn1Lite(Asn1Reader root, Asn1TreeNode tree, Int32 index) {
        initialize(root);
        Depth = tree.Value.Depth + 1;
        Path = $"{tree.Value.Path}/{index}";
        if (Tag == (Byte)Asn1Type.BIT_STRING) {
            if (root.PayloadLength > 0) {
                UnusedBits = root[root.PayloadStartOffset];
            }
        }
    }

    public String Header {
        get => header;
        private set {
            header = value;
            OnPropertyChanged();
        }
    }
    public String ToolTip {
        get => toolTip;
        private set {
            toolTip = value;
            OnPropertyChanged();
        }
    }
    public Byte Tag {
        get => tag;
        private set {
            tag = value;
            if ((tag & (Byte)Asn1Class.CONTEXT_SPECIFIC) > 0) {
                IsContextSpecific = true;
            }
            if ((tag & (Byte)Asn1Class.CONSTRUCTED) > 0) {
                IsContainer = true;
            }
            OnPropertyChanged();
        }
    }
    public Byte UnusedBits { get; set; }
    public String TagName { get; private set; }
    public Int32 Offset {
        get => offset;
        set {
            Int32 diff = value - offset;
            offset = value;
            PayloadStartOffset += diff;
        }
    }
    public Int32 OffsetChange {
        get => offsetChange;
        set {
            if (offsetChange == value) { return; }
            offsetChange = value;
            OnPropertyChanged();
        }
    }

    public Int32 PayloadStartOffset { get; set; }
    public Int32 HeaderLength => PayloadStartOffset - Offset;
    public Int32 PayloadLength { get; set; }
    public Int32 TagLength => HeaderLength + PayloadLength;
    /// <summary>
    /// Gets or sets a value indicating whether the current ASN.1 node is a container.
    /// </summary>
    /// <remarks>
    /// A container node typically has child nodes and represents a structured ASN.1 element,
    /// such as a SEQUENCE or SET, or BIT_STRING and OCTET_STRING in certain cases.
    /// This property is used to determine if the node can hold other nodes as children.
    /// <para>Note, this property does <strong>not</strong> directly reflect the CONSTRUCTED bit in tag.</para>
    /// </remarks>
    public Boolean IsContainer { get; set; }
    public Boolean IsContextSpecific { get; private set; }
    public Boolean InvalidData {
        get => invalidData;
        private set {
            invalidData = value;
            OnPropertyChanged();
        }
    } //TODO
    public Int32 Depth { get; private set; }
    public String Path {
        get => path;
        set {
            path = value ?? String.Empty;
            Depth = Path.Split(['/'], StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
    public String ExplicitValue { get; set; }

    void initialize(Asn1Reader asn) {
        Offset = asn.Offset;
        Tag = asn.Tag;
        TagName = asn.TagName;
        PayloadLength = asn.PayloadLength;
        PayloadStartOffset = asn.PayloadStartOffset;
        IsContainer = asn.IsConstructed;
        if (!asn.IsConstructed) {
            try {
                ExplicitValue = AsnDecoder.GetViewValue(asn);
            } catch {
                InvalidData = true;
            }
        }
    }

    /// <summary>
    /// Retrieves a formatted metadata string for the current ASN.1 node.
    /// </summary>
    /// <returns>
    /// A string containing metadata information about the node, including its tag, offset, length,
    /// depth, and path, formatted according to a predefined template.
    /// </returns>
    public String GetFormattedMetadata() {
        return String.Format(METADATA_TEMPLATE,
            Tag,
            TagName,
            Offset,
            TagLength,
            Depth,
            Path);
    }
    /// <summary>
    /// Performs node header update. This method does not perform expensive display value (except for
    /// <strong>INTEGER</strong> and <strong>OBJECT_IDENTIFIER</strong> tags) or tooltip (all tags)
    /// re-calculation and do not raise <see cref="DataChanged"/> event.
    /// </summary>
    /// <param name="rawData">Node raw data.</param>
    /// <param name="options">Node view options.</param>
    /// <remarks></remarks>
    public void UpdateNodeHeader(IReadOnlyList<Byte> rawData, NodeViewOptions options) {
        Header = getNodeHeader(rawData, options);
    }
    /// <summary>
    /// Performs node value update, which includes update for <see cref="Header"/>, <see cref="ToolTip"/>
    /// and raises <see cref="DataChanged"/> event.
    /// </summary>
    /// <param name="rawData">Node raw data.</param>
    /// <param name="options">Node view options.</param>
    public void UpdateNode(IReadOnlyList<Byte> rawData, NodeViewOptions options) {
        Header = getNodeHeader(rawData, options);
        ToolTip = getToolTip(rawData);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }
    String getNodeHeader(IReadOnlyList<Byte> rawData, NodeViewOptions options) {
        if (Tag == (Byte)Asn1Type.INTEGER) {
            updateIntValue(rawData, options.IntegerAsInteger);
        }
        if (Tag == (Byte)Asn1Type.OBJECT_IDENTIFIER) {
            updateOidValue(rawData);
        }

        // contains only node location information, such as offset, length, path. Everything what is displayed in parentheses.
        var innerList = new List<String>();
        // contains full node header, including inner list (see above), tag name and optional tag display value.
        var outerList = new List<String>();
        if (options.ShowNodePath) {
            outerList.Add($"({Path})");
        }
        if (options.ShowTagNumber) {
            innerList.Add(options.ShowInHex ? $"T:{Tag:x2}" : $"T:{Tag}");
        }
        if (options.ShowNodeOffset) {
            innerList.Add(options.ShowInHex ? $"O:{Offset:x4}" : $"O:{Offset}");
        }
        if (options.ShowNodeLength) {
            innerList.Add(options.ShowInHex ? $"L:{PayloadLength:x4}" : $"L:{PayloadLength}");
        }
        if (innerList.Count > 0) {
            outerList.Add("(" + String.Join(", ", innerList) + ")");
        }
        outerList.Add(TagName);
        if (options.ShowContent) {
            if (!String.IsNullOrEmpty(ExplicitValue)) {
                outerList.Add(":");
                outerList.Add(ExplicitValue);
            }

        }

        return String.Join(" ", outerList);
    }
    void updateIntValue(IEnumerable<Byte> rawData, Boolean forceInteger) {
        if (forceInteger) {
            Byte[] raw = rawData.Skip(PayloadStartOffset).Take(PayloadLength).ToArray();
            ExplicitValue = new BigInteger(raw.Reverse().ToArray()).ToString();
        } else {
            Byte[] raw = rawData.Skip(PayloadStartOffset).Take(PayloadLength).ToArray();
            ExplicitValue = AsnFormatter.BinaryToString(
                raw,
                EncodingType.HexRaw,
                EncodingFormat.NOCRLF
            );
        }
    }
    void updateOidValue(IEnumerable<Byte> rawData) {
        Byte[] raw = rawData.Skip(Offset).Take(TagLength).ToArray();
        ExplicitValue = AsnDecoder.GetViewValue(new Asn1Reader(raw));
    }
    String getToolTip(IEnumerable<Byte> rawData) {
        var sb = new StringBuilder();
        sb.AppendLine(GetFormattedMetadata());
        if (!IsContainer) {
            sb.Append("Value:");
            if (PayloadLength == 0) {
                sb.AppendLine(" NULL");
            } else {
                sb.AppendLine();
                Int32 skip = PayloadStartOffset;
                Int32 take = PayloadLength;
                Boolean writeUnusedBits = false;
                if (Tag == (Byte)Asn1Type.BIT_STRING) {
                    skip++;
                    take--;
                    writeUnusedBits = true;
                }
                if (writeUnusedBits) {
                    sb.AppendLine($"Unused Bits: {UnusedBits}");
                }
                Byte[] binData = rawData.Skip(skip).Take(take).ToArray();
                sb.Append(binData.Length == 0
                    ? "EMPTY"
                    : AsnFormatter.BinaryToString(binData, EncodingType.Hex).TrimEnd());
            }
        }

        return sb.ToString();
    }

    #region Equals
    public override Boolean Equals(Object obj) {
        if (ReferenceEquals(null, obj)) { return false; }
        if (ReferenceEquals(this, obj)) { return true; }
        return obj.GetType() == typeof(Asn1Lite) && Equals((Asn1Lite)obj);
    }
    protected Boolean Equals(Asn1Lite other) {
        return offset == other.offset && tag == other.tag;
    }
    public override Int32 GetHashCode() {
        unchecked {
            return (offset * 397) ^ tag.GetHashCode();
        }
    }
    #endregion

    /// <summary>
    /// Raised when node value changes. It is used by Hex Viewer to update node coloring boundaries.
    /// </summary>
    public event EventHandler DataChanged;
}