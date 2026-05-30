using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.Interfaces;

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
    /// Executes all registered startup tasks asynchronously, optionally updating the splash screen
    /// with the current progress and action being performed.
    /// </summary>
    /// <param name="splashVM">
    /// An optional instance of <see cref="ISplashScreenVM"/> used to display the current action
    /// and progress of the startup tasks. If <c>null</c>, no updates will be displayed.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation of executing the startup tasks.
    /// </returns>
    /// <remarks>
    /// This method iterates through all tasks added to the pipeline, executing them sequentially.
    /// If a splash screen ViewModel is provided, it updates the current action and progress
    /// during the execution of each task.
    /// </remarks>
    public async Task RunAsync(ISplashScreenVM? splashVM = null) {
        Int32 total = _tasks.Count;
        for (Int32 i = 0; i < total; i++) {
            IStartupTask task = _tasks[i];
            if (splashVM is not null) {
                splashVM.CurrentAction = task.DisplayName;
                splashVM.Progress = (Double)i / total * 100;
            }
            await task.ExecuteAsync();
        }
        if (splashVM is not null) {
            splashVM.CurrentAction = null;
            splashVM.Progress = 100;
        }
    }
}