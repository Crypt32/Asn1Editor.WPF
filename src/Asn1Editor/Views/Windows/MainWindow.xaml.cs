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
        CommandBindings.AddRange(_vm.TreeCommands.Bindings);
        InitializeComponent();
        DataContext = vm;
        Closing += onClosing;
    }
    void onClosing(Object sender, CancelEventArgs e) {
        Boolean result = _vm.CloseAllTabs();
        if (result) {
            _vm.Shutdown();
        }
        e.Cancel = !result;
    }
}