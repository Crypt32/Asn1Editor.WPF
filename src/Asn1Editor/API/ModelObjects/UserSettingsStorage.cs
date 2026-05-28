using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SysadminsLV.Asn1Editor.API.ModelObjects;

/// <summary>
/// Represents a storage mechanism for user settings in the application.
/// </summary>
/// <remarks>
/// This class provides functionality to load and save user-specific settings 
/// to a configuration file located in the application's data directory. 
/// It ensures that user preferences are persisted across application sessions.
/// </remarks>
class UserSettingsStorage(String appDataPath) {
    static readonly XmlSerializer _serializer = new(typeof(UserSettings));

    readonly String _configFilePath = Path.Combine(appDataPath, "user.config");

    /// <summary>
    /// Loads the user settings from the configuration file.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="UserSettings"/> containing the user preferences.
    /// If the configuration file does not exist or an error occurs during deserialization,
    /// a new instance of <see cref="UserSettings"/> with default values is returned.
    /// </returns>
    /// <remarks>
    /// This method attempts to deserialize the user settings from the configuration file
    /// located in the application's data directory. If the file is missing or corrupted,
    /// it ensures that a default set of user settings is provided.
    /// </remarks>
    public UserSettings Load() {
        if (!File.Exists(_configFilePath)) {
            return new UserSettings();
        }
        try {
            using var sr = new StreamReader(_configFilePath);
            return (UserSettings)_serializer.Deserialize(sr);
        } catch {
            return new UserSettings();
        }
    }

    /// <summary>
    /// Saves the specified user settings to the configuration file.
    /// </summary>
    /// <param name="options">
    /// An instance of <see cref="UserSettings"/> containing the user preferences to be saved.
    /// </param>
    /// <remarks>
    /// This method serializes the provided <see cref="UserSettings"/> object and writes it to 
    /// the configuration file located in the application's data directory. If the file already 
    /// exists, it will be overwritten.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the serialization process encounters an error.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if there is an issue writing to the configuration file.
    /// </exception>
    public void Save(UserSettings options) {
        using var sw = new StreamWriter(_configFilePath, false);
        using var xw = XmlWriter.Create(sw);
        _serializer.Serialize(xw, options);
    }
}