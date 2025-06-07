using System;
using System.Globalization;
using System.Windows.Data;

namespace SysadminsLV.Asn1Editor.Controls.Converters
{
    /// <summary>
    /// Converts an <see cref="ActivePanel"/> value to a string representing the direction of movement for a tab header.
    /// </summary>
    public class MoveTabHeaderConverter : IValueConverter
    {
        /// <summary>
        /// Converts an <see cref="ActivePanel"/> value to a corresponding string representation indicating the
        /// direction of movement.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is ActivePanel panel
                ? (panel == ActivePanel.Left ? "Move Right" : "Move Left")
                : "Move";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}