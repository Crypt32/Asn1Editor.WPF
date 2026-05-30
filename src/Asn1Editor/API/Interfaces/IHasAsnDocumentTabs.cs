using System;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.ViewModel;
using SysadminsLV.Asn1Editor.Core.Tree;

namespace SysadminsLV.Asn1Editor.API.Interfaces;

public interface IHasAsnDocumentTabs {
    /// <summary>
    /// Gets the currently selected tab in the ASN.1 document editor.
    /// </summary>
    /// <value>
    /// An instance of <see cref="AsnDocumentHostVM"/> representing the selected tab, or <c>null</c> if no tab is selected.
    /// </value>
    AsnDocumentHostVM? SelectedTab { get; }

    /// <summary>
    /// Refreshes the tabs in the ASN.1 document interface, applying an optional filter to determine
    /// which nodes should be updated.
    /// </summary>
    /// <param name="filter">
    /// A function that defines the filtering logic for nodes. If provided, only nodes that satisfy
    /// the filter condition will be refreshed. If <see langword="null"/>, all nodes will be refreshed.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation of refreshing the tabs.
    /// </returns>
    Task RefreshTabs(Func<AsnTreeNode, Boolean>? filter = null);
}