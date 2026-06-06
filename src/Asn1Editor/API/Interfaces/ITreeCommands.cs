using System.Windows.Input;

namespace SysadminsLV.Asn1Editor.API.Interfaces;

public interface ITreeCommands {
    CommandBindingCollection Bindings { get; }
}