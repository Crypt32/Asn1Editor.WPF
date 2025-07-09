using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SysadminsLV.Asn1Editor.API.ViewModel; 

public abstract class ViewModelBase : INotifyPropertyChanged {
    protected void OnPropertyChanged([CallerMemberName] String? propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}