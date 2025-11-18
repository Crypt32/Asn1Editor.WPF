using SysadminsLV.Asn1Editor.API.ViewModel;

namespace SysadminsLV.Asn1Editor.API.Interfaces;

/// <summary>
/// Represents the content of a tab in the application.
/// Provides functionality to retrieve the main ASN.1 document associated with the tab.
/// </summary>
public interface IAsnDocumentHost {
    /// <summary>
    /// Retrieves the main ASN.1 document associated with the current tab.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="Asn1DocumentVM"/> representing the main ASN.1 document.
    /// </returns>
    Asn1DocumentVM GetPrimaryDocument();
}