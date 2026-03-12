using System;
using System.ComponentModel;
using SysadminsLV.Asn1Editor.API.Interfaces;

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
}