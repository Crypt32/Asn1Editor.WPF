using System;
using System.Windows.Input;

namespace SysadminsLV.Asn1Editor.API.Abstractions;

public interface IOidEditorVM {
    ICommand ReloadCommand { get; }
    String OidValue { get; set; }
    String FriendlyName { get; set; }
}