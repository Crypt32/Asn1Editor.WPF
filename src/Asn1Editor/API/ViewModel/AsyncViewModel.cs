using System;

namespace SysadminsLV.Asn1Editor.API.ViewModel;
public abstract class AsyncViewModel : ViewModelBase {
    Boolean isBusy;

    public Boolean IsBusy {
        get => isBusy;
        set {
            isBusy = value;
            OnPropertyChanged();
        }
    }
}
