using System;

namespace SysadminsLV.Asn1Editor.API.Interfaces;

/// <summary>
/// Represents the ViewModel for the splash screen in the ASN.1 Editor application.
/// This interface defines properties to manage the current action being displayed
/// and the progress of the startup process.
/// </summary>
public interface ISplashScreenVM {
    /// <summary>
    /// Gets or sets the current action being performed during the application startup process.
    /// </summary>
    /// <value>
    /// A <see cref="String"/> representing the description of the current action.
    /// This value is displayed on the splash screen to inform the user about the ongoing process.
    /// </value>
    /// <remarks>
    /// This property is typically updated by the startup pipeline to reflect the progress of initialization tasks.
    /// </remarks>
    String? CurrentAction { get; set; }
    /// <summary>
    /// Gets or sets the progress of the startup process as a percentage.
    /// </summary>
    /// <value>
    /// A <see cref="Double"/> value representing the progress percentage, where 0 indicates no progress
    /// and 100 indicates completion.
    /// </value>
    /// <remarks>
    /// This property is typically updated during the execution of startup tasks to reflect the
    /// current progress of the application initialization process.
    /// </remarks>
    Double Progress { get; set; }
}
