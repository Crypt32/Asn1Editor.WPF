using System;
using System.Windows;
using SysadminsLV.Asn1Editor.Core.ASN;
using SysadminsLV.Asn1Parser;

namespace SysadminsLV.Asn1Editor.Controls;

public class AsnVariantValueEditor : AsnValueEditor {
    AsnValueValidator? validator;

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(String),
        typeof(AsnVariantValueEditor),
        new PropertyMetadata(default(String)));

    public String Value {
        get => (String)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected override AsnValueValidationResult PerformValidation() {
        return validator!.Validate(Value);
    }
    protected override void OnInputValueChanged(Byte[]? oldValue, Byte[]? newValue) {
        if (newValue is not null) {
            validator = AsnValueValidator.Create(newValue[0]);
            var reader = new Asn1Reader(newValue);
            var editValue = AsnDecoder.GetEditValue(reader);
            Value = editValue.TextValue ?? String.Empty;

            SetOutOfBandValidationResult();
        }
    }

    static AsnVariantValueEditor() {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(AsnVariantValueEditor),
            new FrameworkPropertyMetadata(typeof(AsnVariantValueEditor)));
    }
}
