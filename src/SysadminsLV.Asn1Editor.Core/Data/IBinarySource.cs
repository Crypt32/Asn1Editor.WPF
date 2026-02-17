using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SysadminsLV.Asn1Editor.Core.Data;

/// <summary>
/// Defines a contract for binary data operations with support for efficient range manipulation 
/// and full notification capabilities. This interface extends <see cref="IReadOnlyList{T}"/> 
/// for read-only access to binary data, and implements <see cref="INotifyCollectionChanged"/> 
/// and <see cref="INotifyPropertyChanged"/> for change notifications, enabling seamless 
/// integration with data-binding frameworks such as WPF.
/// </summary>
public interface IBinarySource : IReadOnlyList<Byte>, INotifyCollectionChanged, INotifyPropertyChanged {
    /// <summary>
    /// Replaces a range of bytes in the binary source with the specified new data.
    /// </summary>
    /// <param name="offset">
    /// The zero-based position in the binary source at which the replacement begins.
    /// </param>
    /// <param name="count">
    /// The number of bytes to be replaced starting from the specified <paramref name="offset"/>.
    /// </param>
    /// <param name="newData">
    /// A collection of bytes that will replace the specified range in the binary source.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="offset"/> or <paramref name="count"/> is out of the valid range.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="newData"/> is <c>null</c>.
    /// </exception>
    void ReplaceRange(Int32 offset, Int32 count, IReadOnlyCollection<Byte> newData);
}