using System;

namespace SysadminsLV.Asn1Editor.API.Abstractions;

/// <summary>
/// Represents an abstraction for UI message boxes. For UI-less applications, this interface can be
/// implemented in a non-blocking way.
/// </summary>
public interface IUIMessenger {
    /// <summary>
    /// Shows informational message.
    /// </summary>
    /// <param name="message">Informational message body.</param>
    /// <param name="header">Optional header title. Default value is 'Information'.</param>
    void ShowInformation(String message, String header = "Information");
    /// <summary>
    /// Shows warning message.
    /// </summary>
    /// <param name="message">Warning message body.</param>
    /// <param name="header">Optional header title. Default value is 'Warning'.</param>
    void ShowWarning(String message, String header = "Warning");
    /// <summary>
    /// Shows error message.
    /// </summary>
    /// <param name="message">Error message body.</param>
    /// <param name="header">Optional header title. Default value is 'Error'.</param>
    void ShowError(String message, String header = "Error");
    /// <summary>
    /// Shows question prompt which requires the client to respond with Yes/No action.
    /// </summary>
    /// <param name="question">Prompt message body.</param>
    /// <param name="header">Optional header title. Default value is 'Question'.</param>
    /// <returns><c>true</c> if client responded with <strong>Yes</strong>, otherwise <c>false</c>.</returns>
    Boolean YesNo(String question, String header = "Question");
    /// <summary>
    /// Shows question prompt which requires the client to respond with Yes/No/Cancel action.
    /// </summary>
    /// <param name="question">Prompt message body.</param>
    /// <param name="header">Optional header title. Default value is 'Question'.</param>
    /// <returns>
    /// <list type="bullet">
    ///     <item><c>true</c> if client responded with <strong>Yes</strong></item>
    ///     <item><c>false</c> if client responded with <strong>No</strong></item>
    ///     <item><c>null</c> if client responded with <strong>Cancel</strong></item>
    /// </list>
    /// </returns>
    Boolean? YesNoCancel(String question, String header = "Question");
    /// <summary>
    /// Shows "Save" dialog to select a path to a file.
    /// </summary>
    /// <param name="filePath">Full path to a file if method returns <c>true</c>.</param>
    /// <returns>
    ///     <list type="bullet">
    ///         <item><c>true</c> - file location is selected. In this case <strong>filePath</strong> parameter is populated.</item>
    ///         <item><c>false</c> - file location selection aborted or declined.</item>
    ///     </list>
    /// </returns>
    Boolean TryGetSaveFileName(out String filePath);
    /// <summary>
    /// Shows "Open" dialog to select a path to a file.
    /// </summary>
    /// <param name="filePath">Full path to a file if method returns <c>true</c>.</param>
    /// <returns>
    ///     <list type="bullet">
    ///         <item><c>true</c> - file location is selected. In this case <strong>filePath</strong> parameter is populated.</item>
    ///         <item><c>false</c> - file location selection aborted or declined.</item>
    ///     </list>
    /// </returns>
    Boolean TryGetOpenFileName(out String filePath);
}