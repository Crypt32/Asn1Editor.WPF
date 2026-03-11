using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using SysadminsLV.Asn1Editor.Core.ASN;
using SysadminsLV.Asn1Parser;
using SysadminsLV.Asn1Parser.Universal;

namespace SysadminsLV.Asn1Editor.Core.Tree;

public class AsnNodeValue : NotifyPropertyChanged, IHexAsnNode {
    const String METADATA_TEMPLATE = """
                                     Tag    : {0} (0x{0:X2}) : {1}
                                     Offset : {2} (0x{2:X2})
                                     Length : {3} (0x{3:X2})
                                     Depth  : {4}
                                     Path   : {5}
                                     
                                     """;

    Int32 offset;

    public AsnNodeValue(Asn1Reader asnReader) {
        Offset = asnReader.Offset;
        Tag = asnReader.Tag;
        TagName = asnReader.TagName;
        PayloadLength = asnReader.PayloadLength;
        PayloadStartOffset = asnReader.PayloadStartOffset;
        IsContainer = asnReader.IsConstructed;
        if (!asnReader.IsConstructed) {
            try {
                ExplicitValue = AsnDecoder.GetViewValue(asnReader);
            } catch {
                InvalidData = true;
            }
        }
        Depth = 0;
        Path = String.Empty;
    }
    public AsnNodeValue(Asn1Reader asnReader, Int32 parentDepth, String parentPath, Int32 index) : this(asnReader) {
        Depth = parentDepth + 1;
        Path = $"{parentPath}/{index}";
        if (Tag == (Byte)Asn1Type.BIT_STRING) {
            if (asnReader.PayloadLength > 0) {
                UnusedBits = asnReader[asnReader.PayloadStartOffset];
            }
        }
    }

    public String Header {
        get;
        private set {
            field = value;
            OnPropertyChanged();
        }
    }
    public String ToolTip {
        get;
        private set {
            field = value;
            OnPropertyChanged();
        }
    }
    public Byte Tag {
        get;
        private init {
            field = value;
            if ((field & (Byte)Asn1Class.CONTEXT_SPECIFIC) > 0) {
                IsContextSpecific = true;
            }
            if ((field & (Byte)Asn1Class.CONSTRUCTED) > 0) {
                IsContainer = true;
            }
            OnPropertyChanged();
        }
    }
    public Byte UnusedBits { get; set; }
    public String TagName { get; }
    public Int32 Offset {
        get => offset;
        set {
            Int32 diff = value - offset;
            offset = value;
            PayloadStartOffset += diff;
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
        get;
        private set {
            field = value;
            OnPropertyChanged();
        }
    } //TODO
    public Int32 Depth { get; private set; }
    public String Path {
        get;
        set {
            field = value ?? String.Empty;
            Depth = Path.Split(['/'], StringSplitOptions.RemoveEmptyEntries)
                .Length;
        }
    }
    public String ExplicitValue { get; set; }

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
    public void UpdateNodeHeader(IReadOnlyList<Byte> rawData, INodeViewOptions options) {
        Header = getNodeHeader(rawData, options);
    }
    /// <summary>
    /// Performs node value update, which includes update for <see cref="Header"/>, <see cref="ToolTip"/>
    /// and raises <see cref="DataChanged"/> event.
    /// </summary>
    /// <param name="rawData">Node raw data.</param>
    /// <param name="options">Node view options.</param>
    public void UpdateNode(IReadOnlyList<Byte> rawData, INodeViewOptions options) {
        Header = getNodeHeader(rawData, options);
        ToolTip = getToolTip(rawData);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }
    String getNodeHeader(IReadOnlyList<Byte> rawData, INodeViewOptions options) {
        if (Tag == (Byte)Asn1Type.INTEGER) {
            updateIntValue(rawData, options.GetIntegerViewOptions().IntegerAsInteger);
        }
        if (Tag == (Byte)Asn1Type.OBJECT_IDENTIFIER) {
            updateOidValue(rawData);
        }
        if (Tag is (Byte)Asn1Type.UTCTime or (Byte)Asn1Type.GeneralizedTime) {
            updateDateTimeValue(rawData, options.GetDateTimeViewOptions().UseISO8601Format);
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
    void updateDateTimeValue(IEnumerable<Byte> rawData, Boolean useIsoFormat) {
        Byte[] raw = rawData.Skip(offset).Take(TagLength).ToArray();
        Asn1DateTime dateTime = Asn1DateTime.DecodeAnyDateTime(new Asn1Reader(raw));
        ExplicitValue = useIsoFormat
            ? dateTime.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            : AsnDecoder.GetViewValue(new Asn1Reader(raw));
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
    public override Boolean Equals(Object? obj) {
        if (ReferenceEquals(null, obj)) { return false; }
        if (ReferenceEquals(this, obj)) { return true; }
        return obj.GetType() == typeof(AsnNodeValue) && Equals((AsnNodeValue)obj);
    }
    protected Boolean Equals(AsnNodeValue other) {
        return offset == other.offset && Tag == other.Tag;
    }
    public override Int32 GetHashCode() {
        unchecked {
            return (offset * 397) ^ Tag.GetHashCode();
        }
    }
    #endregion

    /// <summary>
    /// Raised when node value changes. It is used by Hex Viewer to update node coloring boundaries.
    /// </summary>
    public event EventHandler DataChanged;
}