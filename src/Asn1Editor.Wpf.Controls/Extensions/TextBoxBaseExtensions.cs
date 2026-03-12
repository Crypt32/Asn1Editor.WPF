using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SysadminsLV.Asn1Editor.Controls.Extensions;
static class TextBoxBaseExtensions {
    /// <summary>
    /// Sets <see cref="TextBoxBase"/> control's width to a value required to fit requested text string.
    /// </summary>
    /// <param name="textBoxBase">A <see cref="TextBoxBase"/> instance that defines font style.</param>
    /// <param name="str">String to fit.</param>
    /// <param name="fontSize">Optional custom font size. Default value is 0. Text box font size is used when this parameter is set to default.</param>
    /// <param name="includeScrollBar">Specifies whether to allocate extra space for vertical scrollbar. Default is <c>false</c>.</param>
    public static void SetWidthToFitString(this TextBoxBase textBoxBase, String str, Double fontSize = 0, Boolean includeScrollBar = false) {
        Double effectiveFontSize = fontSize == 0
            ? textBoxBase.FontSize
            : fontSize;
        FormattedText formattedText = new FormattedText(
            str,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(textBoxBase.FontFamily, textBoxBase.FontStyle, textBoxBase.FontWeight, textBoxBase.FontStretch),
            effectiveFontSize,
            textBoxBase.Foreground,
            VisualTreeHelper.GetDpi(textBoxBase).PixelsPerDip);

        textBoxBase.Width = includeScrollBar
            ? formattedText.Width + SystemParameters.VerticalScrollBarWidth + 12
            : formattedText.Width + 12;
    }
}