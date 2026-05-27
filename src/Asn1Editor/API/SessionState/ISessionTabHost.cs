using System.Collections.ObjectModel;
using System.ComponentModel;
using SysadminsLV.Asn1Editor.API.ViewModel;

namespace SysadminsLV.Asn1Editor.API.SessionState;

/// <summary>
/// Represents a host interface for managing session tabs within the application.
/// Provides access to the collection of tabs and the currently selected tab.
/// </summary>
public interface ISessionTabHost : INotifyPropertyChanged {
    /// <summary>
    /// Gets a read-only collection of tabs currently managed by the session tab host.
    /// Each tab represents an instance of <see cref="AsnDocumentHostVM"/> containing
    /// document-related data and functionality.
    /// </summary>
    /// <value>
    /// A <see cref="ReadOnlyObservableCollection{T}"/> of <see cref="AsnDocumentHostVM"/> objects
    /// representing the tabs in the session.
    /// </value>
    ReadOnlyObservableCollection<AsnDocumentHostVM> Tabs { get; }
    /// <summary>
    /// Gets or sets the currently selected tab within the session.
    /// </summary>
    /// <value>
    /// An instance of <see cref="AsnDocumentHostVM"/> representing the selected tab,
    /// or <c>null</c> if no tab is currently selected.
    /// </value>
    /// <remarks>
    /// This property allows access to the active tab in the session, enabling operations
    /// that depend on the currently focused document.
    /// </remarks>
    AsnDocumentHostVM? SelectedTab { get; }
}