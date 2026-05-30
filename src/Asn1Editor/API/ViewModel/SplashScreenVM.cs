using System;
using System.Reflection;
using SysadminsLV.Asn1Editor.API.Interfaces;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

class SplashScreenVM : ViewModelBase, ISplashScreenVM {
    public String CurrentAction {
        get => field;
        set {
            field = value;
            OnPropertyChanged();
        }
    } = String.Empty;
    public Double Progress {
        get => field;
        set {
            field = value;
            OnPropertyChanged();
        }
    } = 0;
    public String Version { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? String.Empty;
}
