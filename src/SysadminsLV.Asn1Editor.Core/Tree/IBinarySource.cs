using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace SysadminsLV.Asn1Editor.Core.Tree;

/// <summary>
/// Represents a binary data source that provides raw binary data and supports initialization
/// from a collection of bytes. This interface also notifies clients of dynamic changes, such as
/// when items are added or removed, or the entire list is refreshed.
/// </summary>
public interface IBinarySource : INotifyCollectionChanged {
    /// <summary>
    /// Gets the raw binary data represented as a read-only list of bytes.
    /// This property provides access to the underlying binary data source,
    /// which can be used for operations such as decoding or processing.
    /// </summary>
    IReadOnlyList<Byte> RawData { get; }

    /// <summary>
    /// Asynchronously initializes the binary data source with the provided raw binary data.
    /// </summary>
    /// <param name="rawData">
    /// A collection of bytes representing the raw binary data to initialize the source with.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation of initializing the binary data source.
    /// </returns>
    Task InitializeFromRawData(IEnumerable<Byte> rawData);
}