﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace SysadminsLV.Asn1Editor.API.Utils {
    static class Tools {
        public static MessageBoxResult MsgBox(String header, String message, MessageBoxImage image = MessageBoxImage.Error, MessageBoxButton button = MessageBoxButton.OK) {
            WindowCollection windows = Application.Current.Windows;
            Window hwnd = null;
            if (windows.Count > 0) {
                hwnd = windows[windows.Count - 1];
            }
            return hwnd == null
                ? MessageBox.Show(message, header, button, image)
                : MessageBox.Show(hwnd, message, header, button, image);
        }
        public static String GetSaveFileName() {
            var dlg = new SaveFileDialog {
                FileName = "",
                DefaultExt = ".*",
                Filter = "All files (*.*)|*.*"
            };
            return dlg.ShowDialog() == true ? dlg.FileName : String.Empty;
        }
        public static String GetOpenFileName() {
            var dlg = new OpenFileDialog {
                FileName = "",
                DefaultExt = ".*",
                Filter = "All files (*.*)|*.*"
            };
            return dlg.ShowDialog() == true ? dlg.FileName : String.Empty;
        }
        public static Double MeasureStringWidth(String str, Int32 size, Boolean includeScrollbars) {
            var formattedText = new FormattedText(
                str,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                size,
                Brushes.Black);
            return includeScrollbars
                ? formattedText.Width + SystemParameters.VerticalScrollBarWidth + 12
                : formattedText.Width + 12;
        }
    }
}
