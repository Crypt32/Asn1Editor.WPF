using System;
using System.Reflection;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.WPF.Mvvm;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

class SplashScreenVM : ViewModelBase, ISplashScreenVM {
    public String? CurrentAction {
        get;
        set => SetField(ref field, value);
    } = String.Empty;
    public Double Progress {
        get;
        set => SetField(ref field, value);
    } = 0;
    public String Version { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? String.Empty;
}
