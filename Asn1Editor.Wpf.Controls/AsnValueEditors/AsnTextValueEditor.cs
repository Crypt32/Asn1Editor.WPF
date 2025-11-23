using System;
using System.Windows;

namespace SysadminsLV.Asn1Editor.Controls;

public class AsnTextValueEditor : AsnValueEditor {
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(String), typeof(AsnTextValueEditor), new PropertyMetadata(default(String)));

    public String Value
    {
        get => (String)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
    
    protected override void OnBinaryValueChanged(Byte[] oldValue, Byte[] newValue) {
        
    }
}
