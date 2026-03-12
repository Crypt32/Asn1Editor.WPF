using System;
using System.Windows;

namespace SysadminsLV.Asn1Editor.Views.Windows;

/// <summary>
/// Interaction logic for Converter.xaml
/// </summary>
public partial class BinaryConverterWindow {
    public BinaryConverterWindow() {
        InitializeComponent();
    }
    void onCloseClick(Object sender, RoutedEventArgs e) {
        e.Handled = true;
        Close();
    }
}