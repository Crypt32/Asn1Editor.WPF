using System;
using System.Text;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.SessionState;

namespace SysadminsLV.Asn1Editor.API.AppStartup;

/// <summary>
/// Represents a startup task responsible for recovering the previous application session
/// during the initialization phase.
/// </summary>
/// <remarks>
/// This class implements the <see cref="IStartupTask"/> interface and is designed to check
/// for any unsaved session data from the last application run. If such data exists, it provides
/// the user with an option to restore the session.
/// </remarks>
class SessionRecoveryStartupTask(IUIMessenger messenger, IMainWindowVM mainWindowVM) : IStartupTask {
    /// <inheritdoc />
    public String DisplayName => "Checking for session recovery...";

    /// <inheritdoc />
    public async Task ExecuteAsync() {
        var recoveryManager = new SessionRecoveryManager();
        SessionRecoveryDto recoveryData = await recoveryManager.GetSessionRecoveryAsync();

        if (recoveryData.Tabs.Count == 0) {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("The application was not closed correctly last time.");
        sb.AppendLine("The following documents can be restored:");
        sb.AppendLine();
        foreach (var tab in recoveryData.Tabs) {
            sb.AppendLine($"- {tab.Name}");
        }
        sb.AppendLine();
        sb.AppendLine("Do you want to restore these documents?");

        if (messenger.YesNo(sb.ToString(), "Restore session")) {
            await mainWindowVM.RestoreSessionAsync(recoveryData);
        }
    }
}