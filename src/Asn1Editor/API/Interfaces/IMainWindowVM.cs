using System;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.SessionState;
using SysadminsLV.Asn1Editor.API.ViewModel;

namespace SysadminsLV.Asn1Editor.API.Interfaces;

public interface IMainWindowVM {
    AsnDocumentHostVM? SelectedTab { get; }
    ITreeCommands TreeCommands { get; }

    /// <summary>
    /// Requests all tab closing. Internally, this method calls a prompt to save unsaved data if necessary.
    /// </summary>
    /// <returns>
    ///     <para><strong>True</strong> if user opted to save unsaved files and save action succeeded or user opted to not save file.</para>
    ///     <para><strong>False</strong> if user opted to save file and save action failed or user opted to cancel operation.</para>
    /// </returns>
    Boolean CloseAllTabs();
    Task OpenExistingAsync(String filePath);
    Task OpenRawAsync(String base64String);
    /// <summary>
    /// Instructs main window view model to perform necessary actions to prepare for application shutdown,
    /// such as prompting user to save unsaved data and releasing resources.
    /// </summary>
    void Shutdown();
    Task RestoreSessionAsync(SessionRecoveryDto recoveryData);
}