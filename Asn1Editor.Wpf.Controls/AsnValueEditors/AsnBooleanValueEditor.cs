using System;
using System.Collections.Generic;
using System.Windows;

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
        var editor = (AsnBooleanValueEditor)d;
        editor.OnValueChanged((Boolean?)e.NewValue);
    }

    public Boolean? Value {
        get => (Boolean?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected override void OnBinaryValueChanged(IList<Byte>? oldValue, IList<Byte>? newValue) {
        if (newValue is not { Count: 1 }) {
            SetValidationState(false, "BOOLEAN must be a single octet.");
            Value = null;

            return;
        }

        Value = newValue[0] != 0;
        SetValidationState(true);
    }

    void OnValueChanged(Boolean? newValue) {
        if (newValue == null) {
            BinaryValue = [0x00];
            SetValidationState(true);
            return;
        }

        BinaryValue = [newValue.Value ? (Byte)0xFF : (Byte)0x00];
        SetValidationState(true);
    }
}