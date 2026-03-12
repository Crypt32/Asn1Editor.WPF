using System;
using System.Reflection;

namespace SysadminsLV.Asn1Editor.API.ViewModel;
class GlobalData {
    public GlobalData() {
        AppVersion = Assembly.GetEntryAssembly().GetName().Version.ToString(3);
    }
    public String AppVersion { get; }
}
