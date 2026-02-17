using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SysadminsLV.Asn1Editor.Core.Tree;
using SysadminsLV.Asn1Parser;

namespace SysadminsLV.Asn1Editor.Core.AsnFormatters;

public class CertutilFormatter(AsnTreeNode baseNode) : IAsnDumpFormatter {
    readonly StringBuilder _sb = new();
    readonly StringBuilder _line = new();
    readonly String nl = Environment.NewLine;
    readonly List<Byte> _headList = new(4);
    readonly HashSet<Byte> _noAsciiTags = [
        (Byte)Asn1Type.BOOLEAN,
        (Byte)Asn1Type.INTEGER,
        (Byte)Asn1Type.OBJECT_IDENTIFIER,
        (Byte)Asn1Type.RELATIVE_OID
    ];
    readonly HashSet<Byte> _explicitTextTags = [
        (Byte)Asn1Type.OBJECT_IDENTIFIER,
        (Byte)Asn1Type.RELATIVE_OID,
        (Byte)Asn1Type.UTCTime,
        (Byte)Asn1Type.GeneralizedTime,
        (Byte)Asn1Type.PrintableString,
        (Byte)Asn1Type.IA5String,
        (Byte)Asn1Type.NumericString,
        (Byte)Asn1Type.TeletexString,
        (Byte)Asn1Type.VisibleString,
        (Byte)Asn1Type.VideotexString,
        (Byte)Asn1Type.UTF8String,
        (Byte)Asn1Type.UniversalString,
        (Byte)Asn1Type.BMPString
    ];

    public String RenderText(Int32 textWidth) {
        _sb.Clear();
        foreach (AsnTreeNode node in baseNode.Flatten()) {
            String leftPad = getLeftPad(node);
            writeTagHeader(node, leftPad);
            if (node.Value is { IsContainer: false, PayloadLength: > 0 }) {
                writeContent(node, leftPad);
            }
        }
        return _sb.ToString();
    }

    void writeTagHeader(AsnTreeNode node, String leftPadString) {
        AsnNodeValue value = node.Value;
        _line.Clear();
        _headList.Clear();
        _headList.Add(value.Tag);
        _headList.AddRange(Asn1Utils.GetLengthBytes(value.PayloadLength));
        // write tag address
        _line.AppendFormat("{0:x4}: ", value.Offset);
        // pad from
        _line.Append(leftPadString);
        _line.Append(AsnFormatter.BinaryToString(_headList.ToArray(), EncodingType.Hex, EncodingFormat.NOCRLF));
        if (_line.Length < 48) {
            Int32 padLeft = 48 - _line.Length;
            _line.Append(new String(' ', padLeft));
        }
        _line.AppendFormat("; {0} ({1:x} Bytes)", value.TagName, value.PayloadLength);
        _line.Append(nl);
        _sb.Append(_line);
        if (value.Tag == (Byte)Asn1Type.BIT_STRING && node.Value.IsContainer) {
            writeLeadingNumber(leftPadString, value.PayloadStartOffset, value.UnusedBits);
        }
    }
    void writeLeadingNumber(String leftPadString, Int32 offset, Byte leadingByte) {
        _line.Clear();
        _headList.Clear();
        _headList.Add(leadingByte);
        // write line address
        _line.AppendFormat("{0:x4}: ", offset);
        // pad from
        _line.Append(leftPadString + "   ");
        _line.Append(AsnFormatter.BinaryToString([leadingByte], EncodingType.Hex));
        _sb.Append(_line);
    }
    void writeContent(AsnTreeNode node, String leftPadString) {
        AsnNodeValue value = node.Value;
        _line.Clear();

        CertutilRenderLine hexTable = getHexTable(node);
        IList<String> lines = hexTable.Lines;
        String padLeftContent = String.Empty;
        if (node.Parent is not null) {
            padLeftContent = node.MyIndex < node.Parent.Children.Count - 1 ? "|  " : "   ";
        }

        for (Int32 i = 0; i < lines.Count; i++) {
            Int32 address;
            if (i == 0 && hexTable.Shift == 1) {
                address = value.PayloadStartOffset;
            } else if (hexTable.Shift == 1) {
                address = value.PayloadStartOffset + hexTable.Shift + (i - 1) * 16;
            } else {
                address = value.PayloadStartOffset + hexTable.Shift + i * 16;
            }
            _line.AppendFormat("{0:x4}: ", address);
            // shift right nested content
            _line.Append(leftPadString)
                .Append(padLeftContent);
            if (!_noAsciiTags.Contains(value.Tag)) {
                Int32 index = lines[i].LastIndexOf("  ", StringComparison.InvariantCulture);
                if (index >= 0) {
                    lines[i] = lines[i].Insert(50, ";");
                }
            }
            _line.AppendLine(lines[i]);
        }
        // write decoded content
        if (_explicitTextTags.Contains(value.Tag)) {
            _line.Append(new String(' ', 6)).Append(leftPadString).Append(padLeftContent);
            _line.AppendLine($"   ; \"{value.ExplicitValue}\"");
        }
        _sb.Append(_line);
    }
    CertutilRenderLine getHexTable(AsnTreeNode node) {
        AsnNodeValue value = node.Value;

        Byte[] binValue = node.GetEncodedValue();
        Byte? highByte = null;
        Byte shift = 0;
        if (node.Value.Tag == (Byte)Asn1Type.INTEGER && binValue[0] == 0) {
            shift = 1;
            highByte = 0;
            binValue = binValue.Skip(1).ToArray();
        }
        String strValue = AsnFormatter.BinaryToString(binValue, _noAsciiTags.Contains(value.Tag)
            ? EncodingType.Hex
            : EncodingType.HexAscii).TrimEnd();
        List<String> lines = strValue.Split([nl], StringSplitOptions.RemoveEmptyEntries).ToList();
        if (node.Value.Tag == (Byte)Asn1Type.BIT_STRING) {
            shift = 1;
            if (!node.Value.IsContainer) {
                lines.Insert(0, $"{node.Value.UnusedBits:x2} ; UNUSED BITS");
            }
        } else if (highByte.HasValue) {
            lines.Insert(0, $"{highByte.Value:x2}");
        }

        return new CertutilRenderLine(shift, lines);
    }
    String getLeftPad(AsnTreeNode node) {
        if (node.Parent is null) {
            return String.Empty;
        }
        var sb = new StringBuilder();
        List<Int32> l = getParents(node);
        for (Int32 i = baseNode.Value.Depth; i < node.Value.Depth; i++) {
            sb.Append(l.Contains(i) ? "|  " : "   ");
        }
        return sb.ToString();
    }

    static List<Int32> getParents(AsnTreeNode node) {
        var depths = new List<Int32>();
        AsnTreeNode n = node;

        while (n.Parent is not null) {
            if (n.MyIndex < n.Parent.Children.Count - 1) {
                depths.Add(n.Value.Depth);
            }
            n = n.Parent;
        }
        return depths;
    }
    record CertutilRenderLine(Int32 Shift, IList<String> Lines);
}