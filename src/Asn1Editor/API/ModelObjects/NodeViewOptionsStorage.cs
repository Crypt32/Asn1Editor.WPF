using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SysadminsLV.Asn1Editor.API.ModelObjects;

/// <summary>
/// Provides functionality to manage the storage and retrieval of node view options.
/// </summary>
/// <remarks>
/// This class is responsible for serializing and deserializing <see cref="NodeViewOptions"/> 
/// to and from a configuration file. It ensures that user preferences for node view settings 
/// are persisted across application sessions.
/// </remarks>
class NodeViewOptionsStorage(String appDataPath) {
    static readonly XmlSerializer _serializer = new(typeof(NodeViewOptions));

    readonly String _configFilePath = Path.Combine(appDataPath, "user.config");

    /// <summary>
    /// Loads the node view options from the configuration file.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="NodeViewOptions"/> containing the deserialized settings 
    /// from the configuration file. If the file does not exist or an error occurs during 
    /// deserialization, a new instance of <see cref="NodeViewOptions"/> with default values is returned.
    /// </returns>
    /// <remarks>
    /// This method ensures that user preferences for node view settings are restored 
    /// from the configuration file. If the file is missing or corrupted, it gracefully 
    /// falls back to default settings.
    /// </remarks>
    public NodeViewOptions Load() {
        if (!File.Exists(_configFilePath)) {
            return new NodeViewOptions();
        }
        try {
            using var sr = new StreamReader(_configFilePath);
            return (NodeViewOptions)_serializer.Deserialize(sr);
        } catch {
            return new NodeViewOptions();
        }
    }

    /// <summary>
    /// Saves the specified node view options to the configuration file.
    /// </summary>
    /// <param name="options">
    /// An instance of <see cref="NodeViewOptions"/> containing the settings to be serialized and saved.
    /// </param>
    /// <remarks>
    /// This method serializes the provided <see cref="NodeViewOptions"/> instance and writes it to the 
    /// configuration file. It ensures that user preferences for node view settings are persisted 
    /// for future application sessions.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if an error occurs during the serialization process.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an error occurs while writing to the configuration file.
    /// </exception>
    public void Save(NodeViewOptions options) {
        using var sw = new StreamWriter(_configFilePath, false);
        using var xw = XmlWriter.Create(sw);
        _serializer.Serialize(xw, options);
    }
}