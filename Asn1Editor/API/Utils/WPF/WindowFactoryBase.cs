using System;
using System.Linq;
using System.Windows;

namespace SysadminsLV.Asn1Editor.API.Utils.WPF;
abstract class WindowFactoryBase {
    protected Window? hwnd;
    void setParent(Boolean mainWindowAsParent = false) {
        Window[] windows = Application.Current.Windows
            .Cast<Window>()
            .Where(x => x.ActualWidth != 0)
            .ToArray();
        Window parent = null;
        if (windows.Any()) {
            parent = mainWindowAsParent
                ? windows[0]
                : windows[windows.Length - 1];
        }

        hwnd!.Owner = parent;
    }

    protected void ShowAsWindow(Boolean mainWindowAsParent = false) {
        setParent(mainWindowAsParent);
        hwnd!.Show();
    }
    protected void ShowAsDialog() {
        setParent();
        hwnd!.ShowDialog();
    }
}