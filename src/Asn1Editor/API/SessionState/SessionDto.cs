using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SysadminsLV.Asn1Editor.API.SessionState;

/// <summary>
/// Represents the state of a session in the application.
/// This class serves as a data transfer object (DTO) for persisting and restoring
/// session-related information, such as session metadata, process details, and the
/// state of open tabs.
/// </summary>
[XmlRoot("session")]
public sealed class SessionDto {
    /// <summary>
    /// Gets or sets the version of the session data.
    /// This property is used to track the schema or format version of the session,
    /// ensuring compatibility during serialization and deserialization processes.
    /// </summary>
    [XmlAttribute("version")]
    public Int32 Version { get; set; }
    /// <summary>
    /// Gets or sets the unique identifier for the session.
    /// This property is used to distinguish between different sessions
    /// and is typically represented as a globally unique identifier (GUID).
    /// </summary>
    [XmlAttribute("sessionId")]
    public String SessionID { get; set; } = String.Empty;
    /// <summary>
    /// Gets or sets the unique identifier of the process associated with the session.
    /// This property is used to track the process ID of the application instance
    /// that created or is managing the session.
    /// </summary>
    [XmlAttribute("processID")]
    public Int32 ProcessID { get; set; }
    /// <summary>
    /// Gets or sets the UTC timestamp indicating when the session was created.
    /// This property is used to track the creation time of the session for 
    /// auditing and metadata purposes.
    /// </summary>
    [XmlAttribute("createdUtc")]
    public DateTime CreatedUtc { get; set; }
    /// <summary>
    /// Gets or sets the UTC timestamp of the last update to the session.
    /// This property is used to track when the session was last modified.
    /// </summary>
    [XmlAttribute("updatedUtc")]
    public DateTime UpdatedUtc { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the currently selected tab in the session.
    /// This property is used to persist and restore the state of the selected tab
    /// when saving or loading a session.
    /// </summary>
    [XmlAttribute("selectedDocumentId")]
    public String? SelectedTabID { get; set; }

    /// <summary>
    /// Gets or sets the collection of open tabs in the session.
    /// Each tab is represented by a <see cref="SessionTabDto"/> object, which contains
    /// metadata about the tab, such as its ID, order, title, and associated file paths.
    /// </summary>
    /// <remarks>
    /// This property is used to persist and restore the state of open documents in the session.
    /// The collection is serialized as an XML array with each tab represented as an XML element.
    /// </remarks>
    [XmlArray("openDocuments")]
    [XmlArrayItem("document")]
    public List<SessionTabDto> OpenTabs { get; set; } = [];
}

/// <summary>
/// Represents a data transfer object (DTO) for a session tab in the application.
/// This class is used to store and manage the state of individual tabs within a session,
/// including metadata such as document ID, order, title, and file paths.
/// </summary>
public sealed class SessionTabDto {
    /// <summary>
    /// Gets or sets the unique identifier of the tab, which is used to track the tab
    /// across sessions and associate it with its recovery file if needed.
    /// </summary>
    [XmlAttribute("id")]
    public String ID { get; set; } = String.Empty;
    /// <summary>
    /// Gets or set the tab order index, which is used to restore the original tab arrangement when the session is loaded.
    /// </summary>
    [XmlAttribute("order")]
    public Int32 Order { get; set; }
    /// <summary>
    /// Gets or sets the title of the session tab.
    /// </summary>
    /// <remarks>
    /// This property represents the display title of the session tab, which is used
    /// to identify and distinguish the tab in the user interface.
    /// </remarks>
    [XmlAttribute("title")]
    public String Title { get; set; } = String.Empty;
    /// <summary>
    /// Gets or sets the file system path to the source file associated with the session tab.
    /// </summary>
    /// <value>
    /// A <see cref="String"/> representing the file path to the source file. 
    /// This value can be <see langword="null"/> if no source file is associated.
    /// </value>
    [XmlAttribute("sourcePath")]
    public String? SourcePath { get; set; }
    /// <summary>
    /// Gets or sets the path to the recovery file associated with the session tab.
    /// </summary>
    /// <remarks>
    /// The recovery file is used to store unsaved changes for the session tab, allowing recovery in case of unexpected interruptions.
    /// </remarks>
    [XmlAttribute("recoveryFile")]
    public String? RecoveryFile { get; set; }
    /// <summary>
    /// Gets or sets the compare document identifier when current tab is in compare mode.
    /// </summary>
    [XmlAttribute("compareId")]
    public String? CompareID { get; set; }
    [XmlIgnore]
    public Boolean IsDirty => !String.IsNullOrWhiteSpace(RecoveryFile);
}

public sealed record SessionRecoveryDto(String? SelectedTabID, List<SessionTabRecoveryDto> Tabs);
public sealed record SessionTabRecoveryDto(String ID, String Name, String? SourcePath, String? CompareID, Byte[]? RecoveryData);