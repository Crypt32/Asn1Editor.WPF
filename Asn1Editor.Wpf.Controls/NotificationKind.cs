namespace SysadminsLV.Asn1Editor.Controls;

/// <summary>
/// Specifies the type of notification to be displayed.
/// </summary>
/// 
public enum NotificationKind {
    /// <summary>
    /// Represents an informational notification.
    /// </summary>
    Info,
    /// <summary>
    /// Represents a success notification.
    /// </summary>
    Success,
    /// <summary>
    /// Represents a warning notification.
    /// </summary>
    Warning,
    /// <summary>
    /// Represents a danger or error notification.
    /// </summary>
    Danger
}