using System;
using System.Windows;

namespace SysadminsLV.Asn1Editor.Views.UserControls;
/// <summary>
/// Interaction logic for ClassicToolbarUC.xaml
/// </summary>
public partial class ClassicToolbarUC {
    public ClassicToolbarUC() {
        InitializeComponent();
    }
    void onCloseClick(Object sender, RoutedEventArgs args) {
        Window? myWindow = Window.GetWindow(this);
        myWindow?.Close();
    }
}
