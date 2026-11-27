using System;
using SysadminsLV.WPF.Mvvm;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

public abstract class AsyncViewModel : ViewModelBase {
    public Boolean IsBusy
    {
        get;
        set => SetField(ref field, value);
    }
}
