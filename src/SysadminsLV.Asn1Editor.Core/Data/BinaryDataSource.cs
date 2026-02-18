using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace SysadminsLV.Asn1Editor.Core.Data;

/// <summary>
/// Binary data source with efficient range operations and full notification support.
/// Implements both INotifyCollectionChanged and INotifyPropertyChanged for complete WPF binding.
/// </summary>
class BinaryDataSource : IBinarySource, IList<Byte> {
    readonly List<Byte> _data = [];
    Boolean suppressNotifications;

    public BinaryDataSource() { }

    public BinaryDataSource(IEnumerable<Byte> collection) {
        _data.AddRange(collection);
    }

    /// <summary>
    /// Inserts a range of bytes into the binary data source at the specified offset.
    /// </summary>
    /// <param name="offset">
    /// The zero-based index at which the new range of bytes should be inserted.
    /// </param>
    /// <param name="data">
    /// The collection of bytes to insert. This cannot be <c>null</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="data"/> parameter is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// If the <paramref name="data"/> collection is empty, the method performs no operation.
    /// The method ensures that the offset is validated and supports inserting at the end of the collection.
    /// </remarks>
    public void InsertRange(Int32 offset, IEnumerable<Byte> data) {
        if (data is null) {
            throw new ArgumentNullException(nameof(data));
        }
        validateOffset(offset, allowEnd: true);
        Byte[] dataArray = data.ToArray();

        if (dataArray.Length == 0) {
            return;
        }

        _data.InsertRange(offset, dataArray);
        notifyChanged();
    }

    /// <summary>
    /// Removes a range of bytes from the binary data source starting at the specified offset.
    /// </summary>
    /// <param name="offset">
    /// The zero-based index at which the range to be removed begins. Must be within the bounds of the data source.
    /// </param>
    /// <param name="count">
    /// The number of bytes to remove from the data source. Must be non-negative and the range defined by
    /// <paramref name="offset"/> and <paramref name="count"/> must be within the bounds of the data source.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="offset"/> or <paramref name="count"/> is out of range, or if the range defined
    /// by <paramref name="offset"/> and <paramref name="count"/> exceeds the bounds of the data source.
    /// </exception>
    /// <remarks>
    /// This method modifies the data source by removing the specified range of bytes and triggers
    /// notifications for collection and property changes.
    /// </remarks>
    public void RemoveRange(Int32 offset, Int32 count) {
        validateRange(offset, count);

        if (count == 0) {
            return;
        }

        _data.RemoveRange(offset, count);
        notifyChanged();
    }

    /// <summary>
    /// Replaces a range of bytes in the binary source with the specified new data.
    /// </summary>
    /// <param name="offset">
    /// The zero-based position in the binary source at which the replacement begins.
    /// </param>
    /// <param name="bytesToRemove">
    /// The number of bytes to be replaced starting from the specified <paramref name="offset"/>.
    /// </param>
    /// <param name="newData">
    /// A collection of bytes that will replace the specified range in the binary source.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="offset"/> or <paramref name="bytesToRemove"/> is out of the valid range.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="newData"/> is <c>null</c>.
    /// </exception>
    public void ReplaceRange(Int32 offset, Int32 bytesToRemove, IReadOnlyCollection<Byte> newData) {
        validateRange(offset, bytesToRemove);

        _data.RemoveRange(offset, bytesToRemove);
        _data.InsertRange(offset, newData);
        notifyChanged();
    }


    #region Transactional support

    /// <summary>
    /// Temporarily suspends notifications for collection and property changes.
    /// </summary>
    /// <remarks>
    /// This method is used to optimize batch operations by suppressing notifications
    /// until <see cref="EndUpdate"/> is called. During this period, changes to the
    /// binary data source will not trigger any events, such as <see cref="INotifyCollectionChanged.CollectionChanged"/>
    /// or <see cref="INotifyPropertyChanged.PropertyChanged"/>.
    /// </remarks>
    /// <seealso cref="EndUpdate"/>
    public void BeginUpdate() {
        suppressNotifications = true;
    }

    /// <summary>
    /// Resumes notifications for collection and property changes that were previously suspended
    /// by a call to <see cref="BeginUpdate"/>.
    /// </summary>
    /// <remarks>
    /// This method should be called after batch operations are completed to re-enable notifications.
    /// If notifications were suppressed, calling this method will trigger the necessary events,
    /// such as <see cref="INotifyCollectionChanged.CollectionChanged"/> and
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/>, to reflect the changes made during
    /// the update period.
    /// </remarks>
    /// <seealso cref="BeginUpdate"/>
    public void EndUpdate() {
        if (suppressNotifications) {
            suppressNotifications = false;
            notifyChanged();
        }
    }

    #endregion

    #region Explicit IList interface implementation

    /// <inheritdoc cref="IReadOnlyList{T}" />
    public Byte this[Int32 index] {
        get => _data[index];
        set {
            if (_data[index] != value) {
                _data[index] = value;
                notifyChanged();
            }
        }
    }

    /// <inheritdoc cref="IReadOnlyList{T}" />
    public Int32 Count => _data.Count;
    /// <inheritdoc />
    public Boolean IsReadOnly => false;

    /// <inheritdoc />
    public void Add(Byte item) {
        _data.Add(item);
        notifyChanged();
    }

    /// <inheritdoc />
    public void Clear() {
        if (_data.Count > 0) {
            _data.Clear();
            notifyChanged();
        }
    }

    /// <inheritdoc />
    public Boolean Contains(Byte item) {
        return _data.Contains(item);
    }

    /// <inheritdoc />
    public void CopyTo(Byte[] array, Int32 arrayIndex) {
        _data.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public Int32 IndexOf(Byte item) {
        return _data.IndexOf(item);
    }

    /// <inheritdoc />
    public void Insert(Int32 index, Byte item) {
        _data.Insert(index, item);
        notifyChanged();
    }

    /// <inheritdoc />
    public Boolean Remove(Byte item) {
        Boolean removed = _data.Remove(item);
        if (removed) {
            notifyChanged();
        }
        return removed;
    }

    /// <inheritdoc />
    public void RemoveAt(Int32 index) {
        _data.RemoveAt(index);
        notifyChanged();
    }

    /// <inheritdoc />
    public IEnumerator<Byte> GetEnumerator() {
        return _data.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    #endregion

    #region Event triggers

    /// <summary>
    /// Central notification method that fires both collection and property changed events.
    /// </summary>
    void notifyChanged() {
        if (suppressNotifications) {
            return;
        }

        onCollectionChanged(NotifyCollectionChangedAction.Reset);

        // Notify count change and indexer changes
        onPropertyChanged(nameof(Count));
        onPropertyChanged("Item[]");
    }

    void onCollectionChanged(NotifyCollectionChangedAction action) {
        try {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action));
        } catch (NotSupportedException) {
            // Some WPF controls don't support certain change types, fall back to Reset
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    #endregion

    #region Validation (check array bounds)

    void validateOffset(Int32 offset, Boolean allowEnd = false) {
        Int32 maxOffset = allowEnd ? _data.Count : _data.Count - 1;
        if (_data.Count == 0 && offset == 0 && allowEnd) {
            return; // Allow insert at position 0 in empty list
        }
        if (offset < 0 || offset > maxOffset) {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                $"Offset must be between 0 and {maxOffset}. Data size: {_data.Count}."
            );
        }
    }

    void validateRange(Int32 offset, Int32 count) {
        if (count < 0) {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count cannot be negative.");
        }
        if (offset < 0 || offset > _data.Count) {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset must be between 0 and {_data.Count}.");
        }
        if (offset + count > _data.Count) {
            throw new ArgumentException($"Range [{offset}, {offset + count}) exceeds data bounds [0, {_data.Count}).", nameof(count));
        }
    }

    #endregion

    #region Events

    /// <inheritdoc />
    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;


    void onPropertyChanged(String propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}