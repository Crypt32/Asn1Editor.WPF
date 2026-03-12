using System;
using System.Windows;
using Microsoft.Win32;
using SysadminsLV.Asn1Editor.API.Abstractions;

namespace SysadminsLV.Asn1Editor.API.Utils;

class UIMessenger : IUIMessenger {
    public void ShowInformation(String message, String header = "Information") {
        MsgBox(header, message, MessageBoxImage.Information);
    }
    public void ShowWarning(String message, String header = "Warning") {
        MsgBox(header, message, MessageBoxImage.Warning);
    }
    public void ShowError(String message, String header = "Error") {
        MsgBox(header, message);
    }
    public Boolean YesNo(String question, String header) {
        return MsgBox(header, question, MessageBoxImage.Question, MessageBoxButton.YesNo) == MessageBoxResult.Yes;
    }
    public Boolean? YesNoCancel(String question, String header = "Question") {
        MessageBoxResult result = MsgBox(
            header,
            question,
            MessageBoxImage.Warning,
            MessageBoxButton.YesNoCancel);

        return result switch {
            MessageBoxResult.Yes => true,
            MessageBoxResult.No  => false,
            _                    => null
        };
    }
    public Boolean TryGetSaveFileName(out String filePath) {
        filePath = null;
        var dlg = new SaveFileDialog {
            FileName = "",
            Filter = "All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() == true) {
            filePath = dlg.FileName.Trim();
            return true;
        }

        return false;
    }
    public Boolean TryGetOpenFileName(out String filePath) {
        filePath = null;
        var dlg = new OpenFileDialog {
            FileName = "",
            DefaultExt = ".*",
            Filter = "All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() == true) {
            filePath = dlg.FileName.Trim();
            return true;
        }

        return false;
    }

    static MessageBoxResult MsgBox(String header, String message, MessageBoxImage image = MessageBoxImage.Error, MessageBoxButton button = MessageBoxButton.OK) {
        WindowCollection windows = Application.Current.Windows;
        Window hwnd = null;
        if (windows.Count > 0) {
            hwnd = windows[windows.Count - 1];
        }
        return hwnd is null
            ? MessageBox.Show(message, header, button, image)
            : MessageBox.Show(hwnd, message, header, button, image);
    }
}