using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SysadminsLV.Asn1Editor.API.ViewModel;
using SysadminsLV.Asn1Editor.Core.ASN;
using SysadminsLV.Asn1Parser;

namespace SysadminsLV.Asn1Editor.API.Utils.WPF;

/// <summary>
/// Selects the appropriate DataTemplate for editing ASN.1 node values based on tag type and context.
/// </summary>
public class AsnValueEditorTemplateSelector : DataTemplateSelector {
    public DataTemplate ReadOnlyTemplate { get; set; }
    public DataTemplate HexTemplate { get; set; }
    public DataTemplate BooleanTemplate { get; set; }
    public DataTemplate OidTemplate { get; set; }
    public DataTemplate StringTemplate { get; set; }
    public DataTemplate BitStringTemplate { get; set; }

    public override DataTemplate? SelectTemplate(Object? item, DependencyObject container) {
        if (item is not AsnValueEditorContext context) {
            return base.SelectTemplate(item, container);
        }

        DataTemplate template = selectTemplate(context);
        // Enable hex switch if in hex mode or if the selected template is not the default hex editor
        context.IsHexSwitchEnabled = context.IsHexMode || (!ReferenceEquals(HexTemplate, template) && !ReferenceEquals(ReadOnlyTemplate, template));

        return template;
    }
    
    DataTemplate selectTemplate(AsnValueEditorContext context) {
        // Rule 1: Read-only nodes (containers, NULL)
        if (context.IsReadOnly) {
            return ReadOnlyTemplate;
        }

        // Rule 2: Force Hex Mode
        if (context.IsHexMode) {
            return HexTemplate;
        }

        // Rule 3: BIT_STRING with no nested content
        if (context is { Tag: (Byte)Asn1Type.BIT_STRING, IsContainer: false }) {
            return BitStringTemplate;
        }

        // Rule 4: Tag-specific editors
        DataTemplate? tagTemplate = selectByTag(context);
        if (tagTemplate is not null) {
            return tagTemplate;
        }

        // Rule 5: CONTEXT_SPECIFIC heuristics
        if (context.IsContextSpecific) {
            return selectForContextSpecific(context);
        }

        // Rule 6: Default fallback
        return HexTemplate;
    }

    DataTemplate? selectByTag(AsnValueEditorContext context) {
        return (Asn1Type)context.Tag switch {
            Asn1Type.OBJECT_IDENTIFIER or
            Asn1Type.RELATIVE_OID => OidTemplate,

            Asn1Type.BOOLEAN => BooleanTemplate,
            Asn1Type.INTEGER or
            Asn1Type.UTF8String or
            Asn1Type.NumericString or
            Asn1Type.PrintableString or
            Asn1Type.TeletexString or
            Asn1Type.VideotexString or
            Asn1Type.IA5String or
            Asn1Type.VisibleString or
            Asn1Type.GeneralString or
            Asn1Type.UniversalString or
            Asn1Type.BMPString or 
            Asn1Type.UTCTime or
            Asn1Type.GeneralizedTime => StringTemplate,

            Asn1Type.OCTET_STRING => HexTemplate,

            _ => null
        };
    }

    DataTemplate selectForContextSpecific(AsnValueEditorContext context) {
        // If container, read-only
        if (context.IsContainer) {
            return ReadOnlyTemplate;
        }

        // Use existing heuristic from AsnDecoder via GetEditValue
        // Construct temporary Asn1Reader to check if it's text-like
        Byte[] fullTag = buildFullTag(context);
        var asn = new Asn1Reader(fullTag);
        AsnViewValue viewValue = AsnDecoder.GetEditValue(asn);

        // Check if raw data looks like a printable text
        if ((viewValue.Options & AsnViewValueOptions.SupportsPrintableText) != 0) {
            return StringTemplate;
        }

        // Otherwise, hex only
        return HexTemplate;
    }

    static Byte[] buildFullTag(AsnValueEditorContext context) {
        // Build temporary ASN.1 structure for heuristic check
        return Asn1Utils.Encode(context.OriginalEncodedValue.ToArray(), context.Tag);
    }
}