using System;
using System.Windows;
using SysadminsLV.Asn1Parser.Universal;

namespace SysadminsLV.Asn1Editor.Controls;

public class AsnBooleanValueEditor : AsnValueEditor {
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(Boolean?),
        typeof(AsnBooleanValueEditor),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            onValueChanged));
    static void onValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is AsnBooleanValueEditor editor) {
            editor.SetOutOfBandValidationResult();
        }
    }

    public Boolean? Value {
        get => (Boolean?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected override AsnValueValidationResult PerformValidation() {
        if (Value is null) {
            return AsnValueValidationResult.Fail("BOOLEAN value cannot be determined. It has to be either, True or False.");
        }

        return AsnValueValidationResult.Ok(new Asn1Boolean(Value ?? false).GetRawData());
    }
    protected override void OnInputValueChanged(Byte[]? oldValue, Byte[]? newValue) {
        if (newValue is not { Length: 3 }) {
            SetOutOfBandValidationResult();

            return;
        }

        Value = new Asn1Boolean(newValue).Value;
    }

    static AsnBooleanValueEditor() {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(AsnBooleanValueEditor),
            new FrameworkPropertyMetadata(typeof(AsnBooleanValueEditor)));
    }
}