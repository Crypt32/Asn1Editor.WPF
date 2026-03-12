using System;
using System.Windows;
using SysadminsLV.Asn1Editor.API.Interfaces;

namespace SysadminsLV.Asn1Editor.Views.Windows; 

/// <summary>
/// Interaction logic for TextViewer.xaml
/// </summary>
public partial class TextViewer {
    public TextViewer(ITextViewerVM vm) {
        InitializeComponent();
        DataContext = vm;
    }
    void Close(Object sender, RoutedEventArgs e) {
        Close();
    }
}