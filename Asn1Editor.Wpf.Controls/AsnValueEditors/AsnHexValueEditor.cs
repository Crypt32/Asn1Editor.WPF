using System;
using System.Windows;
using SysadminsLV.Asn1Parser;

namespace SysadminsLV.Asn1Editor.Controls;

public class AsnHexValueEditor : AsnVariantValueEditor {
    AsnValueValidator? validator;

    protected override AsnValueValidationResult PerformValidation() {
        try {
            return validator!.Validate(Value);
        } catch (Exception ex) {
            return AsnValueValidationResult.Fail(ex.Message);
        }
    }
    protected override void OnInputValueChanged(Byte[]? oldValue, Byte[]? newValue) {
        if (newValue is not null) {
            validator = new AsnValueValidator(newValue[0]);
            var reader = new Asn1Reader(newValue);
            Value = AsnFormatter.BinaryToString(reader.GetPayload(), EncodingType.Hex);

            SetOutOfBandValidationResult();
        }
    }

    static AsnHexValueEditor() {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(AsnHexValueEditor),
            new FrameworkPropertyMetadata(typeof(AsnHexValueEditor)));
    }
}
