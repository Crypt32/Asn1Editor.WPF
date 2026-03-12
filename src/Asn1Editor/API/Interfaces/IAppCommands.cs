using System.Windows.Input;

namespace SysadminsLV.Asn1Editor.API.Interfaces;

public interface IAppCommands {
    ICommand ShowLicenseCommand { get; }
    ICommand ShowAboutCommand { get; }
    ICommand ShowConverterWindow { get; set; }
    ICommand ShowOidEditor { get; set; }
}