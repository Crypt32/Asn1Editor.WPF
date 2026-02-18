using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SysadminsLV.Asn1Parser;

namespace SysadminsLV.Asn1Editor.Controls;

public class AsnHexValueEditor : AsnValueEditor {

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(String),
        typeof(AsnHexValueEditor),
        new FrameworkPropertyMetadata(String.Empty,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            onValueChanged));
    static void onValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var editor = (AsnHexValueEditor)d;
        editor.OnValueChanged((String?)e.NewValue);
    }

    public String Value {
        get => (String)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected override void OnBinaryValueChanged(IList<Byte>? oldValue, IList<Byte>? newValue) {
        Value = newValue is null
            ? String.Empty
            : AsnFormatter.BinaryToString(newValue.ToArray(), EncodingType.Hex);

        SetValidationState(true);
    }
    void OnValueChanged(String? newValue) {
        if (String.IsNullOrWhiteSpace(newValue)) {
            BinaryValue = [];
            SetValidationState(true);

            return;
        }

        Byte[] binary = null;

        try {
            binary = AsnFormatter.StringToBinary(newValue, EncodingType.Hex);
        } catch (Exception ex) {
            SetValidationState(false, ex.Message);
        }

        // if we reached this far, binary is not null
        BinaryValue = binary!;
        SetValidationState(true);
    }
}
