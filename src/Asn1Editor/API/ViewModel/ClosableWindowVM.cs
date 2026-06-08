using System;
using System.Windows.Input;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

public abstract class ClosableWindowVM : ViewModelBase {
    protected ClosableWindowVM() {
        CloseCommand = new RelayCommand(_ => { DialogResult = true; });
    }

    public ICommand CloseCommand { get; }

    public Boolean? DialogResult {
        get;
        set {
            field = value;
            OnPropertyChanged();
        }
    }
}