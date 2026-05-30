using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.SessionState;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

/// <summary>
/// Manages the collection of ASN.1 document host view models within the application.
/// Provides functionality to add, remove, and manage tabs, as well as access the currently selected tab.
/// </summary>
/// <remarks>
/// This class serves as the primary manager for handling multiple ASN.1 document tabs, enabling
/// operations such as creating new tabs, reusing existing tabs, and clearing all tabs.
/// It implements the <see cref="ISessionTabHost"/> interface to provide session tab management capabilities.
/// </remarks>
class AsnDocumentHostManager : ViewModelBase, ISessionTabHost {
    readonly ObservableCollection<AsnDocumentHostVM> _tabs = [];
    readonly UserSettings _userSettings;
    readonly ITreeCommands _treeCommands;

    public AsnDocumentHostManager(UserSettings userSettings, ITreeCommands treeCommands) {
        _userSettings = userSettings;
        _treeCommands = treeCommands;
        Tabs = new ReadOnlyObservableCollection<AsnDocumentHostVM>(_tabs);
    }

    /// <summary>
    /// Gets the collection of ASN.1 document host view models currently managed by this instance.
    /// </summary>
    /// <value>
    /// A read-only collection of <see cref="AsnDocumentHostVM"/> objects representing the tabs
    /// available in the application.
    /// </value>
    /// <remarks>
    /// This property provides access to the tabs managed by the <see cref="AsnDocumentHostManager"/>.
    /// The collection is read-only to ensure that modifications to the tab list are performed
    /// exclusively through the methods provided by the manager, such as <see cref="AddTab"/> and
    /// <see cref="RemoveTab"/>.
    /// </remarks>
    public ReadOnlyObservableCollection<AsnDocumentHostVM> Tabs { get; }

    /// <summary>
    /// Gets or sets the currently selected tab in the collection of ASN.1 document host view models.
    /// </summary>
    /// <value>
    /// An instance of <see cref="AsnDocumentHostVM"/> representing the currently selected tab,
    /// or <c>null</c> if no tab is selected.
    /// </value>
    /// <remarks>
    /// Changing the selected tab raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
    /// to notify the UI or other components of the update.
    /// </remarks>
    public AsnDocumentHostVM? SelectedTab {
        get;
        set {
            if (!Equals(field, value)) {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Creates and adds a new tab to the collection of ASN.1 document host view models.
    /// </summary>
    /// <remarks>
    /// This method initializes a new instance of the <see cref="AsnDocumentHostVM"/> class using the
    /// current user settings and tree commands, adds it to the tab collection, and sets it as the selected tab.
    /// </remarks>
    /// <returns>
    /// The newly created <see cref="AsnDocumentHostVM"/> instance representing the new tab.
    /// </returns>
    public AsnDocumentHostVM AddNewTab() {
        var tab = new AsnDocumentHostVM(_userSettings, _treeCommands);
        AddTab(tab);
        return tab;
    }

    /// <summary>
    /// Adds a new ASN.1 document host view model to the collection of managed tabs.
    /// </summary>
    /// <param name="tab">
    /// The <see cref="AsnDocumentHostVM"/> instance to be added as a new tab.
    /// </param>
    /// <remarks>
    /// This method adds the specified tab to the internal collection of tabs and sets it as the currently selected tab.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="tab"/> parameter is <c>null</c>.
    /// </exception>
    public void AddTab(AsnDocumentHostVM tab) {
        _tabs.Add(tab);
        SelectedTab = tab;
    }

    /// <summary>
    /// Retrieves an available tab for use, either by reusing an existing tab or creating a new one.
    /// </summary>
    /// <param name="isNew">
    /// When this method returns, contains a value indicating whether a new tab was created. 
    /// <c>true</c> if a new tab was created; otherwise, <c>false</c>.
    /// </param>
    /// <returns>
    /// An instance of <see cref="AsnDocumentHostVM"/> representing the available tab.
    /// </returns>
    /// <remarks>
    /// This method first checks if the currently selected tab can be reused. If it can, the method returns
    /// the selected tab. Otherwise, a new tab is created and returned.
    /// </remarks>
    public AsnDocumentHostVM GetAvailableTab(out Boolean isNew) {
        isNew = false;
        Boolean useExistingTab = SelectedTab is not null && SelectedTab.GetPrimaryDocument().CanReuse;
        if (useExistingTab && Tabs.Any()) {
            return SelectedTab!;
        }

        isNew = true;
        return AddNewTab();
    }

    /// <summary>
    /// Removes the specified ASN.1 document host view model from the collection of managed tabs.
    /// </summary>
    /// <param name="tab">
    /// The <see cref="AsnDocumentHostVM"/> instance representing the tab to be removed.
    /// </param>
    /// <remarks>
    /// This method enables the removal of a specific tab from the collection managed by the
    /// <see cref="AsnDocumentHostManager"/>. If the tab has a secondary document (represented by
    /// the <see cref="AsnDocumentHostVM.Right"/> property), its <see cref="Asn1DocumentVM.IsEnabled"/>
    /// property is set to <c>true</c> before the tab is removed.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="tab"/> parameter is <c>null</c>.
    /// </exception>
    public void RemoveTab(AsnDocumentHostVM tab) {
        tab.Right?.IsEnabled = true;
        _tabs.Remove(tab);
    }

    /// <summary>
    /// Clears all tabs from the collection of ASN.1 document host view models.
    /// </summary>
    /// <remarks>
    /// This method removes all tabs from the internal collection, effectively resetting the state of the manager.
    /// It does not perform any additional cleanup or disposal of the tab contents.
    /// </remarks>
    public void Clear() {
        _tabs.Clear();
    }
}