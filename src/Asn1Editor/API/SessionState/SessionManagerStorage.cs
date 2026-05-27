using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SysadminsLV.Asn1Editor.API.ViewModel;

namespace SysadminsLV.Asn1Editor.API.SessionState;

/// <summary>
/// Provides functionality for managing session storage, including writing and deleting
/// session and recovery files. This class handles the persistence of session data
/// and recovery files to ensure application state can be restored after unexpected
/// interruptions or crashes.
/// </summary>
class SessionManagerStorage {
    static readonly XmlSerializer _sessionSerializer = new(typeof(SessionDto));
    static readonly String _recoveryFolderPath = Path.Combine(App.AppDataPath, "Recovery");
    static readonly Boolean _isInitialized;

    static SessionManagerStorage() {
        try {
            Directory.CreateDirectory(_recoveryFolderPath);
            _isInitialized = true;
        } catch { }
    }

    /// <summary>
    /// Reads all recovery session files from the recovery folder and deserializes them into a list of <see cref="SessionDto"/> objects.
    /// </summary>
    /// <returns>
    /// A list of <see cref="SessionDto"/> objects representing the recovered sessions. If no valid recovery files are found,
    /// an empty list is returned.
    /// </returns>
    /// <remarks>
    /// This method iterates through all files in the recovery folder that match the naming pattern "_session-*.xml".
    /// Each file is deserialized into a <see cref="SessionDto"/> object. If a file cannot be read or deserialized,
    /// the exception is logged, and the method continues processing the remaining files.
    /// </remarks>
    /// <exception cref="IOException">
    /// Thrown if an I/O error occurs while accessing the recovery folder or its files.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown if the application does not have the required permissions to access the recovery folder or its files.
    /// </exception>
    public IList<SessionDto> ReadRecoverySessions() {
        List<SessionDto> sessions = [];
        try {
            foreach (String file in Directory.GetFiles(_recoveryFolderPath, "_session-*.xml")) {
                try {
                    using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var session = (SessionDto)_sessionSerializer.Deserialize(stream);
                    sessions.Add(session);
                } catch (Exception ex) {
                    // if for whatever reason session file cannot be read, log the error move on
                    App.Write(ex);
                }
            }
        } catch (Exception ex) {
            App.Write(ex);
        }
        return sessions;
    }
    public Task<IList<SessionDto>> ReadRecoverySessionsAsync() {
        return Task.Run(ReadRecoverySessions);
    }

    public Byte[]? ReadRecoveryFile(String recoveryFileID) {
        try {
            String recoveryFilePath = Path.Combine(_recoveryFolderPath, $"{recoveryFileID}.bin");
            if (!File.Exists(recoveryFilePath)) {
                return null;
            }

            return File.ReadAllBytes(recoveryFilePath);
        } catch (Exception ex) {
            App.Write(ex);
        }

        return null;
    }
    public Task<Byte[]?> ReadRecoveryFileAsync(String? recoveryFileID) {
        return Task.Run(() => ReadRecoveryFile(recoveryFileID));
    }

    /// <summary>
    /// Writes the specified session data to a file in the recovery folder.
    /// </summary>
    /// <param name="session">
    /// An instance of <see cref="SessionDto"/> containing the session data to be serialized and saved.
    /// </param>
    /// <remarks>
    /// This method serializes the provided <see cref="SessionDto"/> instance into an XML file
    /// and stores it in the recovery folder. If an error occurs during the process, the exception
    /// is logged using the application's logging mechanism.
    /// </remarks>
    public void WriteSession(SessionDto session) {
        try {
            String sessionFilePath = Path.Combine(_recoveryFolderPath, $"_session-{session.SessionID}.xml");
            using var stream = new FileStream(sessionFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            _sessionSerializer.Serialize(stream, session);
        } catch (Exception ex) {
            App.Write(ex);
        }
    }
    /// <summary>
    /// Writes the current session data to persistent storage.
    /// </summary>
    /// <param name="session">
    /// An instance of <see cref="SessionDto"/> containing the session data to be saved.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the session storage handler is not initialized.
    /// </exception>
    public Task WriteSessionAsync(SessionDto session) {
        if (!_isInitialized) {
            throw new InvalidOperationException("Session storage handler is not initialized.");
        }
        return Task.Run(() => WriteSession(session));
    }
    /// <summary>
    /// Writes a recovery file for the specified ASN.1 document asynchronously.
    /// This method ensures that the document's state is saved to a recovery file,
    /// which can be used to restore the document in case of an unexpected interruption
    /// or crash.
    /// </summary>
    /// <param name="document">
    /// The ASN.1 document for which the recovery file is to be created. The document
    /// must contain valid data and a unique identifier.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the full path to the created recovery file.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the session storage handler is not initialized before calling this method.
    /// </exception>
    public async Task<String> WriteRecoveryFileAsync(Asn1DocumentVM document) {
        if (!_isInitialized) {
            throw new InvalidOperationException("Session storage handler is not initialized.");
        }
        String recoveryFilePath = Path.Combine(_recoveryFolderPath, $"{document.ID}.bin");
        using var stream = new FileStream(recoveryFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.WriteAsync(document.AsnDocContext.RawData.ToArray(), 0, document.AsnDocContext.RawData.Count);

        return recoveryFilePath;
    }
    /// <summary>
    /// Deletes a recovery file associated with the specified recovery file identifier.
    /// </summary>
    /// <param name="recoveryFileID">
    /// A <see cref="String"/> representing the unique identifier of the recovery file to be deleted.
    /// </param>
    /// <remarks>
    /// This method attempts to delete the recovery file from the designated recovery folder.
    /// If the file does not exist, no action is taken. Any exceptions encountered during the
    /// deletion process are logged using the application's logging mechanism.
    /// </remarks>
    public void DeleteRecoveryFile(String recoveryFileID) {
        try {
            String recoveryFilePath = Path.Combine(_recoveryFolderPath, $"{recoveryFileID}.bin");
            if (File.Exists(recoveryFilePath)) {
                File.Delete(recoveryFilePath);
            }
        } catch (Exception ex) {
            App.Write(ex);
        }
    }
    public void DeleteSessionFile(String fileName) {
        try {
            String sessionFilePath = Path.Combine(_recoveryFolderPath, fileName);
            if (File.Exists(sessionFilePath)) {
                File.Delete(sessionFilePath);
            }
        } catch (Exception ex) {
            App.Write(ex);
        }
    }
    /// <summary>
    /// Deletes the recovery file associated with the specified recovery file identifier.
    /// </summary>
    /// <param name="recoveryFileID">
    /// The unique identifier of the recovery file to be deleted.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation.
    /// </returns>
    /// <remarks>
    /// This method attempts to locate and delete the recovery file corresponding to the provided
    /// identifier. If the file does not exist, the method completes without throwing an exception.
    /// Any errors encountered during the deletion process are logged.
    /// </remarks>
    public Task DeleteRecoveryFileAsync(String recoveryFileID) {
        return Task.Run(() => DeleteRecoveryFile(recoveryFileID));
    }

    public void DeleteRecoverySession(String sessionID) {
        try {
            String sessionFilePath = Path.Combine(_recoveryFolderPath, $"_session-{sessionID}.xml");
            if (File.Exists(sessionFilePath)) {
                File.Delete(sessionFilePath);
            }
        } catch (Exception ex) { }
    }
    public Task DeleteRecoverySessionAsync(String sessionID) {
        return Task.Run(() => DeleteRecoverySession(sessionID));
    }
}