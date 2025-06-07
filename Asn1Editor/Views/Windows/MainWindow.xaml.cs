using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ViewModel;
using SysadminsLV.Asn1Editor.Controls;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

    void onCloseClick(Object sender, RoutedEventArgs e) {
        Close();
    }
    void onTabHeaderContextMenuOpening(Object sender, ContextMenuEventArgs e) {
        var vm = (Asn1DocumentVM)((FrameworkElement)sender).DataContext;
        e.Handled = _vm.SelectedTab != vm;
    }

    private void onLeftTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowVM vm && e.AddedItems.Count > 0)
        {
            vm.SelectedLeftTab = (Asn1DocumentVM)e.AddedItems[0];
        }
    }

    private void onRightTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowVM vm && e.AddedItems.Count > 0)
        {
            vm.SelectedRightTab = (Asn1DocumentVM)e.AddedItems[0];
        }
    }

    private void asnTreeViewPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Asn1DocumentVM vm)
        {
            if (DataContext is MainWindowVM mainVm)
            {
                if (vm.ActivePanel == ActivePanel.Left)
                    mainVm.SelectedLeftTab = vm;
                else
                    mainVm.SelectedRightTab = vm;
            }
        }
    }
}