using System;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.ViewModel;

namespace SysadminsLV.Asn1Editor.API.Interfaces;

public interface IMainWindowVM {
    Asn1DocumentVM? SelectedTab { get; }

    /// <summary>
    /// Raises UI prompt if user wants to save contents of unsaved file.
    /// </summary>
    /// <param name="tab">Document to save.</param>
    /// <returns>
    ///     <para><strong>True</strong> if user opted to save and save action succeeded or user opted to not save file.</para>
    ///     <para><strong>False</strong> if user opted to save file and save action failed or user opted to cancel operation.</para>
    /// </returns>
    Boolean RequestFileSave(Asn1DocumentVM tab);
    /// <summary>
    /// Requests all tab closing. Internally, this method calls <see cref="RequestFileSave"/> method to prompt to
    /// save unsaved data.
    /// </summary>
    /// <returns>
    ///     <para><strong>True</strong> if user opted to save unsaved files and save action succeeded or user opted to not save file.</para>
    ///     <para><strong>False</strong> if user opted to save file and save action failed or user opted to cancel operation.</para>
    /// </returns>
    Boolean CloseAllTabs();
    Task OpenExistingAsync(String filePath);
    Task OpenRawAsync(String base64String);
}