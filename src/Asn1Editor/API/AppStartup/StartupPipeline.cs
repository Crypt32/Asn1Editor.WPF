using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SysadminsLV.Asn1Editor.API.AppStartup;

/// <summary>
/// Represents a pipeline for managing and executing startup tasks during the application initialization process.
/// </summary>
/// <remarks>
/// The <see cref="StartupPipeline"/> class allows for the registration and sequential execution of tasks
/// that implement the <see cref="IStartupTask"/> interface. Tasks can be added to the pipeline using the
/// <see cref="Add"/> method, and the pipeline can be executed asynchronously using the <see cref="RunAsync"/> method.
/// </remarks>
class StartupPipeline {
    readonly List<IStartupTask> _tasks = [];

    /// <summary>
    /// Adds a startup task to the pipeline for execution during the application initialization process.
    /// </summary>
    /// <param name="task">
    /// The startup task to be added. This task must implement the <see cref="IStartupTask"/> interface.
    /// </param>
    /// <returns>
    /// The current instance of <see cref="StartupPipeline"/>, allowing for method chaining.
    /// </returns>
    public StartupPipeline Add(IStartupTask task) {
        _tasks.Add(task);

        return this;
    }
    /// <summary>
    /// Executes all registered startup tasks asynchronously.
    /// </summary>
    /// <param name="onProgress">
    /// An optional callback that is invoked with the display name of each task as it starts execution.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    public async Task RunAsync(Action<String>? onProgress = null) {
        foreach (IStartupTask task in _tasks) {
            onProgress?.Invoke(task.DisplayName);
            await task.ExecuteAsync();
        }
    }
}