using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using SysadminsLV.Asn1Editor.API;
using SysadminsLV.Asn1Editor.API.AppStartup;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.Utils;
using SysadminsLV.Asn1Editor.API.Utils.WPF;
using SysadminsLV.Asn1Editor.API.ViewModel;
using SysadminsLV.Asn1Editor.Views;
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
    
    readonly UserSettings _options;

    public App() {
        Dispatcher.UnhandledException += onDispatcherUnhandledException;
        var optionsStorage = new UserSettingsStorage(_appDataPath);
        _options = optionsStorage.Load();
        _options.PropertyChanged += (s, _) => optionsStorage.Save((UserSettings)s);
    }

    public static String AppDataPath => _appDataPath;
    public static IUnityContainer Container { get; private set; }

    static void onDispatcherUnhandledException(Object s, DispatcherUnhandledExceptionEventArgs e) {
        _logger.Write(e.Exception);
        Splasher.CloseSplashScreen();
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
        
        Splasher.SplashScreen = Container.Resolve<SplashWindow>();
        Splasher.MainWindow = Container.Resolve<MainWindow>();
        Splasher.ShowSplashScreen();

        await new StartupPipeline()
            .Add(new InfrastructureStartupTask(Container.Resolve<IOidDbManager>()))
            .Add(new SessionRecoveryStartupTask(Container.Resolve<IUIMessenger>(), Container.Resolve<IMainWindowVM>()))
            .Add(new CliArgumentsStartupTask(Container.Resolve<IMainWindowVM>(), e.Args))
            .RunAsync(Container.Resolve<ISplashScreenVM>());
        Splasher.ShowMainWindow();
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
            .RegisterSingleton<ISplashScreenVM, SplashScreenVM>()
            .RegisterSingleton<IMainWindowVM, MainWindowVM>()
            .RegisterSingleton<AsnDocumentHostManager>()
            .RegisterType<IHasAsnDocumentTabs, AsnDocumentHostManager>()
            .RegisterSingleton<IOidDbManager, OidDbManager>()
            .RegisterType<ITextViewerVM, TextViewerVM>()
            .RegisterType<IAsnValueEditorVM, AsnValueEditorVM>()
            .RegisterType<IOidEditorVM, OidEditorVM>()
            .RegisterType<INewAsnNodeEditorVM, NewAsnNodeEditorVM>()
            .RegisterType<ITreeCommands, TreeViewCommands>()
            .RegisterInstance(_options);
    }
}