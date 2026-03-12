using SysadminsLV.Asn1Editor.API.Interfaces;

namespace SysadminsLV.Asn1Editor.Views.Windows;
/// <summary>
/// Interaction logic for AsnValueEditorWindow.xaml
/// </summary>
public partial class AsnValueEditorWindow : IAsnValueEditorWindow {
    public AsnValueEditorWindow(IAsnValueEditorVM vm) {
        InitializeComponent();
        DataContext = vm;
    }
}
