using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SysadminsLV.Asn1Editor.Core;

/// <summary>
/// Represents the base implementation of <see cref="INotifyPropertyChanged"/> interface.
/// </summary>
public abstract class NotifyPropertyChanged : INotifyPropertyChanged {
    /// <summary>
    /// Raises property changed event.
    /// </summary>
    /// <param name="propertyName">Property name that changed its value.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] String? propertyName = null) {
        PropertyChangedEventHandler handler = PropertyChanged;
        handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
}
