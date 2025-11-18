using System;
using System.Windows;
using System.Windows.Controls;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ViewModel;

namespace SysadminsLV.Asn1Editor.Views.UserControls;
/// <summary>
/// Interaction logic for DocumentTabsView.xaml
/// </summary>
public partial class DocumentTabsUC {
    public DocumentTabsUC() {
        InitializeComponent();
    }

    void onTabHeaderContextMenuOpening(Object sender, ContextMenuEventArgs e) {
        if (DataContext is not IMainWindowVM mwvm) {
            e.Handled = true;
            
            return;
        }

        var vm = (AsnDocumentHostVM)((FrameworkElement)sender).DataContext;
        e.Handled = mwvm.SelectedTab != vm;
    }
}
