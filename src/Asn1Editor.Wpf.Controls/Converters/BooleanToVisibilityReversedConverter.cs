using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SysadminsLV.Asn1Editor.Controls.Converters;

/// <summary>
/// Provides a value converter that reverses the standard Boolean-to-Visibility conversion logic.
/// </summary>
/// <remarks>
/// This converter returns <see cref="Visibility.Collapsed"/> when the input value is <c>false</c>,
/// and <see cref="Visibility.Visible"/> when the input value is <c>true</c>.
/// </remarks>
public class BooleanToVisibilityReversedConverter : IValueConverter {
    public Object? Convert(Object? value, Type targetType, Object? parameter, CultureInfo culture) {
        return value is false
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
    public Object? ConvertBack(Object? value, Type targetType, Object? parameter, CultureInfo culture) {
        throw new NotImplementedException();
    }
}
