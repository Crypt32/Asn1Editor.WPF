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
public interface IBinarySource : IReadOnlyList<Byte>, INotifyCollectionChanged, INotifyPropertyChanged;