using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SysadminsLV.Asn1Editor.API.ViewModel;

namespace SysadminsLV.Asn1Editor.API.Utils.WPF;

sealed record TabCompareParam(AsnDocumentHostVM? Left, AsnDocumentHostVM? Right);

/// <summary>
/// Provides a mechanism for converting multiple input values into a single <see cref="TabCompareParam"/> object
/// and vice versa. This converter is specifically designed for use in WPF bindings where tab comparison
/// parameters are required.
/// </summary>
/// <remarks>
/// This class implements the <see cref="IMultiValueConverter"/> interface to handle the conversion of
/// multiple values, typically representing the left and right tabs, into a <see cref="TabCompareParam"/> object.
/// The conversion back operation is not supported and will throw a <see cref="NotSupportedException"/>.
/// </remarks>
public class CompareTabParamConverter : IMultiValueConverter {
    /// <inheritdoc />
    public Object Convert(Object?[] values, Type targetType, Object parameter, CultureInfo culture) {
        if (values[0] is null || values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue) {
            return new TabCompareParam(null, null);
        }
        
        return new TabCompareParam((AsnDocumentHostVM)values[0], (AsnDocumentHostVM)values[1]);
    }
    /// <inheritdoc />
    public Object[] ConvertBack(Object value, Type[] targetType, Object parameter, CultureInfo culture) {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Provides a mechanism for determining whether two tab objects are not the same.
/// This converter is specifically designed for use in WPF bindings where such comparisons
/// are required, for example, to enable or disable UI elements based on tab equality.
/// </summary>
/// <remarks>
/// This class implements the <see cref="IMultiValueConverter"/> interface to handle the comparison
/// of two input values, typically representing tabs, and returns a boolean indicating whether
/// they are not the same. The conversion back operation is not supported and will throw a
/// <see cref="NotSupportedException"/>.
/// </remarks>
public class NotSameTabConverter : IMultiValueConverter {
    /// <inheritdoc />
    public Object Convert(Object?[] values, Type targetTypet, Object parameter, CultureInfo culture) {
        return !ReferenceEquals(values[0], values[1]);
    }
    /// <inheritdoc />
    public Object[] ConvertBack(Object value, Type[] targetType, Object parameter, CultureInfo culture) {
        throw new NotSupportedException();
    }
}