using System;
using System.Windows;
using SysadminsLV.Asn1Editor.Core.ASN;
using SysadminsLV.Asn1Parser;
using SysadminsLV.Asn1Parser.Universal;

namespace SysadminsLV.Asn1Editor.Controls;

public class AsnBitStringValueEditor : AsnValueEditor {
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(String),
        typeof(AsnBitStringValueEditor),
        new PropertyMetadata(default(String)));

    public String Value {
        get => (String)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty UnusedBitsProperty = DependencyProperty.Register(
        nameof(UnusedBits),
        typeof(String),
        typeof(AsnBitStringValueEditor),
        new FrameworkPropertyMetadata(
            "0",
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            null,
            CoerceUnusedBits));

    public String UnusedBits {
        get => (String)GetValue(UnusedBitsProperty);
        set => SetValue(UnusedBitsProperty, value);
    }

    static Object CoerceUnusedBits(DependencyObject d, Object baseValue) {
        if (baseValue is String { Length: 1 } s &&
            s[0] >= '0' && s[0] <= '7') {
            return s;
        }

        return "0"; // default to "0" if the value is invalid
    }

    public static readonly DependencyProperty AutoCalculateUnusedBitsProperty = DependencyProperty.Register(
        nameof(AutoCalculateUnusedBits),
        typeof(Boolean),
        typeof(AsnBitStringValueEditor),
        new PropertyMetadata(false, OnCalculateUnusedBitsPropertyChanged));
    static void OnCalculateUnusedBitsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs args) {
        if (d is AsnBitStringValueEditor editor) {
            if ((Boolean)args.NewValue) {
                editor.calculateUnusedBits();
            }
        }
    }

    public Boolean AutoCalculateUnusedBits {
        get => (Boolean)GetValue(AutoCalculateUnusedBitsProperty);
        set => SetValue(AutoCalculateUnusedBitsProperty, value);
    }

    void calculateUnusedBits() {
        Byte[] value = AsnFormatter.StringToBinary(Value, EncodingType.Hex);
        if (value.Length == 0) {
            UnusedBits = "0";
        } else {
            var bitString = new Asn1BitString(value, true);
            UnusedBits = bitString.UnusedBits.ToString();
        }
    }

    protected override AsnValueValidationResult PerformValidation() {
        throw new NotImplementedException();
    }
    protected override void OnInputValueChanged(Byte[]? oldValue, Byte[]? newValue) {
        if (newValue is not null) {
            var reader = new Asn1Reader(newValue);
            var editValue = AsnDecoder.GetEditValue(reader);
            Value = editValue.TextValue ?? String.Empty;
            UnusedBits = editValue.UnusedBits.ToString();
        }
    }

    static AsnBitStringValueEditor() {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(AsnBitStringValueEditor),
            new FrameworkPropertyMetadata(typeof(AsnBitStringValueEditor)));
    }
}
