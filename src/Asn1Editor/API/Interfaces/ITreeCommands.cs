using System.Windows.Input;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.API.Interfaces;

public interface ITreeCommands {
    ICommand ShowNodeTextViewer { get; }
    ICommand ShowNodeInConverter { get; }
    ICommand ShowNodeHashCommand { get; }
    ICommand SaveNodeCommand { get; }
    ICommand EditNodeCommand { get; }
    ICommand RegisterOidCommand { get; }
    ICommand AddNewNodeCommand { get; }
    ICommand DeleteNodeCommand { get; }
    ICommand CutNodeCommand { get; }
    ICommand CopyNodeCommand { get; }
    IAsyncCommand PasteBeforeCommand { get; }
    IAsyncCommand PasteAfterCommand { get; }
    IAsyncCommand PasteLastCommand { get; }
}