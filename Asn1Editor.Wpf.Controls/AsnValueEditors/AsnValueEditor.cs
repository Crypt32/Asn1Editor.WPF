using System;
using System.Windows;
using System.Windows.Controls;

namespace SysadminsLV.Asn1Editor.Controls;

public abstract class AsnValueEditor : Control {
    #region BinaryValue

    public static readonly DependencyProperty BinaryValueProperty = DependencyProperty.Register(
        nameof(BinaryValue),
        typeof(Byte[]),
        typeof(AsnValueEditor),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            onBinaryValueChanged));

    static void onBinaryValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is AsnValueEditor editor) {
            editor.OnBinaryValueChanged((Byte[])e.OldValue, (Byte[])e.NewValue);
        }
    }

    public Byte[] BinaryValue {
        get => (Byte[])GetValue(BinaryValueProperty);
        set => SetValue(BinaryValueProperty, value);
    }

    #endregion

    #region IsValid

    static readonly DependencyPropertyKey IsValidPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(IsValid),
            typeof(Boolean),
            typeof(AsnValueEditor),
            new PropertyMetadata(true));


    public static readonly DependencyProperty IsValidProperty = IsValidPropertyKey.DependencyProperty;

    public Boolean IsValid {
        get => (Boolean)GetValue(IsValidProperty);
        private set => SetValue(IsValidPropertyKey, value);
    }

    #endregion

    #region ErrorText

    public static readonly DependencyPropertyKey ErrorTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ErrorText),
        typeof(String),
        typeof(AsnValueEditor),
        new PropertyMetadata(default(String)));

    public static readonly DependencyProperty ErrorTextProperty = ErrorTextPropertyKey.DependencyProperty;

    public String ErrorText {
        get => (String)GetValue(ErrorTextProperty);
        private set => SetValue(ErrorTextPropertyKey, value);
    }

    #endregion

    protected void SetValidationState(Boolean isValid, String? errorText = "") {
        IsValid = isValid;
        ErrorText = isValid
            ? String.Empty
            : errorText ?? "The value is not correct.";
    }
    protected abstract void OnBinaryValueChanged(Byte[] oldValue, Byte[] newValue);
}