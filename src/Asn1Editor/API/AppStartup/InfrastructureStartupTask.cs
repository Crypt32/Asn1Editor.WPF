using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.Core;

namespace SysadminsLV.Asn1Editor.API.AppStartup;

/// <summary>
/// Represents a startup task responsible for initializing the application's infrastructure components.
/// </summary>
/// <remarks>
/// This class implements the <see cref="IStartupTask"/> interface and is specifically designed
/// to handle the initialization of the OID database and related services during the application's startup process.
/// </remarks>
class InfrastructureStartupTask(IOidDbManager oidDbManager) : IStartupTask {
    /// <inheritdoc />
    public String DisplayName => "Loading OID database...";

    void execute() {
        oidDbManager.OidLookupLocations = [Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, App.AppDataPath];
        oidDbManager.ReloadLookup();
    }

    /// <inheritdoc />
    public async Task ExecuteAsync() {
        await Task.Run(execute);
        OidServices.Resolver = new OidResolverWrapper();
    }
}