using System;
using System.Threading.Tasks;

namespace SysadminsLV.Asn1Editor.API.AppStartup;

/// <summary>
/// Represents a startup task that can be executed during the application initialization process.
/// </summary>
interface IStartupTask {
    /// <summary>
    /// Gets the display name of the startup task.
    /// </summary>
    /// <value>
    /// A <see cref="String"/> representing the name of the task to be displayed.
    /// </value>
    String DisplayName { get; }
    /// <summary>
    /// Executes the startup task asynchronously.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation.
    /// </returns>
    Task ExecuteAsync();
}