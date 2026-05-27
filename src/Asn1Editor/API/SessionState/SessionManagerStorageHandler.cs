using System;
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
class SessionManagerStorageHandler {
    static readonly XmlSerializer _settingsSerializer = new(typeof(SessionDto));
    static readonly String _recoveryFolderPath = Path.Combine(App.AppDataPath, "Recovery");
    static readonly Boolean _isInitialized;

    static SessionManagerStorageHandler() {
        try {
            Directory.CreateDirectory(_recoveryFolderPath);
            _isInitialized = true;
        } catch { }
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
        return Task.Run(() => {
                            String sessionFilePath = Path.Combine(_recoveryFolderPath, $"_session-{session.SessionID}.xml");
                            using var stream = new FileStream(sessionFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                            _settingsSerializer.Serialize(stream, session);
                        });
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
        // Here you would serialize the document to the stream
        // For example: document.Save(stream);
        return recoveryFilePath;
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
        return Task.Run(() => 
                        {
                            try {
                                String recoveryFilePath = Path.Combine(_recoveryFolderPath, $"{recoveryFileID}.bin");
                                if (File.Exists(recoveryFilePath)) {
                                    File.Delete(recoveryFilePath);
                                }
                            } catch (Exception ex) {
                                App.Write(ex);
                            }
                        });
    }
}