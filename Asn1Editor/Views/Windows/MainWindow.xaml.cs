using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ViewModel;

namespace SysadminsLV.Asn1Editor.Views.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow {
    readonly IMainWindowVM _vm;
    public MainWindow(IMainWindowVM vm) {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
        Closing += onClosing;
    }
    void onClosing(Object sender, CancelEventArgs e) {
        e.Cancel = !_vm.CloseAllTabs();
    }
    void onTabHeaderContextMenuOpening(Object sender, ContextMenuEventArgs e) {
        var vm = (AsnDocumentHostVM)((FrameworkElement)sender).DataContext;
        e.Handled = _vm.SelectedTab != vm;
    }
}