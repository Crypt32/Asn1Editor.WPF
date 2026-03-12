using System;
using System.Windows;
using System.Windows.Input;
using SysadminsLV.Asn1Editor.API.ViewModel;

namespace SysadminsLV.Asn1Editor.Views.UserControls;
/// <summary>
/// Interaction logic for RibbonToolbarUC.xaml
/// </summary>
public partial class RibbonToolbarUC {
    public RibbonToolbarUC() {
        InitializeComponent();
    }
    void onCloseClick(Object sender, RoutedEventArgs args) {
        Window? myWindow = Window.GetWindow(this);
        myWindow?.Close();
    }
    void onRibbonExpandCollapseClick(Object sender, RoutedEventArgs args) {
        if (DataContext is MainWindowVM vm) {
            vm.NodeViewOptions.RibbonMinimized = !vm.NodeViewOptions.RibbonMinimized;
        }
        Keyboard.ClearFocus();
    }
}
