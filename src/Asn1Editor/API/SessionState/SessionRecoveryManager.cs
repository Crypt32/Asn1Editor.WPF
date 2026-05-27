using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SysadminsLV.Asn1Editor.API.SessionState;

/// <summary>
/// Provides functionality to recover application sessions.
/// This class is responsible for identifying and restoring session data from previous application runs,
/// ensuring continuity and minimizing data loss in case of unexpected shutdowns or crashes.
/// </summary>
class SessionRecoveryManager {
    static readonly SessionManagerStorage _storageHandler = new();

    /// <summary>
    /// Asynchronously retrieves session recovery data from the storage.
    /// </summary>
    /// <remarks>
    /// This method reads previously saved session data, processes it to identify valid recovery sessions,
    /// and constructs a recovery object containing the state of open tabs and associated metadata.
    /// It ensures that recovery files and session data are cleaned up after being processed to prevent
    /// duplicate recovery attempts in the future.
    /// </remarks>
    /// <returns>
    /// A <see cref="SessionRecoveryDto"/> object containing the recovered session data, including
    /// information about open tabs and their recovery states.
    /// </returns>
    /// <exception cref="Exception">
    /// Exceptions are caught and suppressed during the recovery process to ensure that the method
    /// completes without interruption, even if some sessions or tabs cannot be recovered.
    /// </exception>
    public async Task<SessionRecoveryDto> GetSessionRecoveryAsync() {
        IList<SessionDto> sessionList = await _storageHandler.ReadRecoverySessionsAsync();
        List<SessionTabRecoveryDto> sessionsTabs = [];
        String? selectedTab = null;
        foreach (SessionDto sessionDto in sessionList.OrderBy(x => x.UpdatedUtc)) {
            DateTime? actualStartTimeUtc = getProcessStartTimeUtc(sessionDto.ProcessID);
            if (actualStartTimeUtc is not null) {
                TimeSpan delta = actualStartTimeUtc.Value - sessionDto.CreatedUtc;
                // allow some leeway in process start time to account for clock skew and potential delays in session creation
                // if condition succeeds, it is highly likely that the session belongs to the parallel process,
                // so do nothing, it is not supposed for recovery.
                if (Math.Abs(delta.TotalSeconds) < 2) {
                    continue;
                }
            }

            selectedTab = sessionDto.SelectedTabID;
            foreach (SessionTabDto sessionTab in sessionDto.OpenTabs) {
                Byte[]? recoveryData = null;
                if (!String.IsNullOrEmpty(sessionTab.RecoveryFile)) {
                    recoveryData = await _storageHandler.ReadRecoveryFileAsync(sessionTab.ID);
                    // once recovery data is read, delete the recovery file to prevent it from being offered for recovery again
                    await _storageHandler.DeleteRecoveryFileAsync(sessionTab.ID);
                }
                sessionsTabs.Add(new SessionTabRecoveryDto(sessionTab.ID, sessionTab.Title, sessionTab.SourcePath, sessionTab.CompareID, recoveryData));
            }
            // once recovery data is read, delete the session file to prevent it from being offered for recovery again
            await _storageHandler.DeleteRecoverySessionAsync(sessionDto.SessionID);
        }

        return new SessionRecoveryDto(selectedTab, sessionsTabs);
    }

    static DateTime? getProcessStartTimeUtc(Int32 processId) {
        try {
            Process process = Process.GetProcessById(processId);

            return process.StartTime.ToUniversalTime();
        } catch {
            return null;
        }
    }
}