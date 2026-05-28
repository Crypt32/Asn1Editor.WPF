using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.Interfaces;

namespace SysadminsLV.Asn1Editor.API.AppStartup;

/// <summary>
/// Represents a startup task responsible for processing command-line arguments during the application's initialization phase.
/// </summary>
/// <remarks>
/// This class implements the <see cref="IStartupTask"/> interface and is designed to handle
/// the parsing and execution of specific command-line arguments, such as opening files or raw data.
/// </remarks>
class CliArgumentsStartupTask(IMainWindowVM mainWindowVM, IReadOnlyList<String> args) : IStartupTask {
    /// <inheritdoc />
    public String DisplayName => "Processing command-line arguments...";

    /// <inheritdoc />
    public async Task ExecuteAsync() {
        for (Int32 i = 0; i < args.Count;) {
            switch (args[i].ToLower()) {
                case "-path":
                    i++;
                    if (args.Count <= i) {
                        throw new ArgumentException(args[i]);
                    }
                    await mainWindowVM.OpenExistingAsync(args[i]);
                    return;
                case "-raw":
                    i++;
                    if (args.Count <= i) {
                        throw new ArgumentException(args[i]);
                    }
                    await mainWindowVM.OpenRawAsync(args[i]);
                    return;
                default:
                    await mainWindowVM.OpenExistingAsync(args[i]);
                    return;
            }
        }
    }
}