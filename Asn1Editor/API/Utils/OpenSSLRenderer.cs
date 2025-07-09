using System;
using System.Linq;
using System.Text;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.Utils.Extensions;

namespace SysadminsLV.Asn1Editor.API.Utils;

class OpenSSLRenderer(Asn1TreeNode rootNode) : ITextRenderer {
    const String delimiter = "      |      |       |";
    readonly String nl = Environment.NewLine;
    Int32 width = 80;

    public String RenderText(Int32 textWidth) {
        width = textWidth;
        var sb = new StringBuilder("Offset|Length|LenByte|" + nl);
        if (rootNode is null) {
            return sb.ToString();
        }
            
        sb.AppendLine("======+======+=======+" + new String('=', width + 10));
        foreach (Asn1Lite node in rootNode.Flatten().Select(x => x.Value)) {
            String padding = new(' ', (node.Depth - rootNode.Value.Depth + 1) * 3);
            String str = String.Format("{0,6}|{1,6}|{2,7}|{3}{4} : ",
                node.Offset,
                node.PayloadLength,
                node.HeaderLength - 1,
                padding,
                node.TagName);
            sb.Append(str + calculateValue(node, padding.Length));
        }
        return sb.ToString();
    }
    String calculateValue(Asn1Lite node, Int32 padding) {
        if (String.IsNullOrEmpty(node.ExplicitValue)) { return nl; }
        if (24 + padding + node.ExplicitValue.Length <= width) {
            return $"'{node.ExplicitValue.Trim()}'{nl}";
        }
        Int32 remaining = width - 22 - padding;
        if (node.ExplicitValue.Length <= remaining - 2) {
            return String.Format(
                "{0}{1}{4}{2}{3}{2}{0}",
                nl,
                delimiter,
                "'",
                node.ExplicitValue.Trim(),
                new String(' ', padding + 3)
            );
        }
        return node.ExplicitValue
            .SplitByLength(width - padding)
            .Aggregate(nl, (current, line) =>
                               current + $"{delimiter}{new String(' ', padding + 3)}{line.Trim()}{nl}");
    }
}