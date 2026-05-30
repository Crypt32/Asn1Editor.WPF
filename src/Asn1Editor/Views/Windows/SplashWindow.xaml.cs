using SysadminsLV.Asn1Editor.API.Interfaces;

namespace SysadminsLV.Asn1Editor.Views.Windows;
/// <summary>
/// Interaction logic for SplashWindow.xaml
/// </summary>
public partial class SplashWindow {
    public SplashWindow(ISplashScreenVM viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }
}
