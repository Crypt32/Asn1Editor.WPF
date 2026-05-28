using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using SysadminsLV.Asn1Editor.API;
using SysadminsLV.Asn1Editor.API.Abstractions;
using SysadminsLV.Asn1Editor.API.AppStartup;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.Utils;
using SysadminsLV.Asn1Editor.API.Utils.WPF;
using SysadminsLV.Asn1Editor.API.ViewModel;
using SysadminsLV.Asn1Editor.Views.Windows;
using Unity;
using Path = System.IO.Path;

namespace SysadminsLV.Asn1Editor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App {
    static readonly String _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Sysadmins LV\Asn1Editor");
    static readonly Logger _logger = new(_appDataPath);
    
    readonly NodeViewOptions _options;

    public App() {
        Dispatcher.UnhandledException += onDispatcherUnhandledException;
        var optionsStorage = new NodeViewOptionsStorage(_appDataPath);
        _options = optionsStorage.Load();
        _options.PropertyChanged += (s, _) => optionsStorage.Save((NodeViewOptions)s);
    }

    public static String AppDataPath => _appDataPath;
    public static IUnityContainer Container { get; private set; }

    static void onDispatcherUnhandledException(Object s, DispatcherUnhandledExceptionEventArgs e) {
        _logger.Write(e.Exception);
    }

    public static void Write(Exception e) {
        _logger.Write(e);
    }
    public static void Write(String s) {
        _logger.Write(s);
    }
    async protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);
        logStartupHeader();
        configureUnity();
        var mainWindow = Container.Resolve<MainWindow>();
        await new StartupPipeline()
            .Add(new InfrastructureStartupTask(Container.Resolve<IOidDbManager>()))
            .Add(new SessionRecoveryStartupTask(Container.Resolve<IUIMessenger>(), Container.Resolve<IMainWindowVM>()))
            .Add(new CliArgumentsStartupTask(Container.Resolve<IMainWindowVM>(), e.Args))
            .RunAsync();
        mainWindow.Show();
    }
    static void logStartupHeader() {
        _logger.Write("******************************** Started ********************************");
        _logger.Write($"Process: {Process.GetCurrentProcess().ProcessName}");
        _logger.Write($"PID    : {Process.GetCurrentProcess().Id}");
        _logger.Write($"Version: {Assembly.GetExecutingAssembly().GetName().Version}");
        _logger.Write("*************************************************************************");
    }

    protected override void OnExit(ExitEventArgs e) {
        _logger.Dispose();
        base.OnExit(e);
    }
    void configureUnity() {
        Container = new UnityContainer();
        Container.RegisterType<MainWindow>()
            .RegisterType<IWindowFactory, WindowFactory>()
            .RegisterType<IAppCommands, AppCommands>()
            .RegisterType<IAsnValueEditorWindow, AsnValueEditorWindow>()
            .RegisterType<IUIMessenger, UIMessenger>()
            // view models
            .RegisterSingleton<MainWindowVM>()
            .RegisterSingleton<IMainWindowVM, MainWindowVM>()
            .RegisterType<IHasAsnDocumentTabs, MainWindowVM>()
            .RegisterType<ITextViewerVM, TextViewerVM>()
            .RegisterType<IAsnValueEditorVM, AsnValueEditorVM>()
            .RegisterType<IOidEditorVM, OidEditorVM>()
            .RegisterType<INewAsnNodeEditorVM, NewAsnNodeEditorVM>()
            .RegisterInstance(_options);
        var oidMgr = new OidDbManager(Container.Resolve<IUIMessenger>()) {
            OidLookupLocations = [Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, _appDataPath]
        };
        Container.RegisterInstance<IOidDbManager>(oidMgr);
    }
}