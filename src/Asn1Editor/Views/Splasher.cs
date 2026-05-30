using SysadminsLV.Asn1Editor.Views.Windows;

namespace SysadminsLV.Asn1Editor.Views;

/// <summary>
/// Provides static methods and properties to manage the application's splash screen and main window.
/// </summary>
/// <remarks>
/// This class is responsible for displaying and closing the splash screen and main window during the application's lifecycle.
/// It acts as a centralized utility for managing these windows.
/// </remarks>
static class Splasher {
    /// <summary>
    /// Gets or sets the instance of the splash screen window used during the application's startup process.
    /// </summary>
    /// <value>
    /// An instance of <see cref="SplashWindow"/> representing the splash screen.
    /// </value>
    /// <remarks>
    /// This property is used to manage the splash screen's lifecycle, such as displaying or closing it.
    /// </remarks>
    public static SplashWindow? SplashScreen { get; set; }
    /// <summary>
    /// Gets or sets the main application window.
    /// </summary>
    /// <remarks>
    /// This property provides access to the main window of the application, which serves as the primary user interface.
    /// It is typically initialized during the application's startup process and is used to display the main content.
    /// </remarks>
    public static MainWindow? MainWindow { get; set; }

    /// <summary>
    /// Displays the application's splash screen.
    /// </summary>
    /// <remarks>
    /// This method initializes and shows the splash screen window, allowing users to view a loading interface
    /// while the application performs startup tasks.
    /// </remarks>
    public static void ShowSplashScreen() {
        SplashScreen?.Show();
    }
    /// <summary>
    /// Closes the application's splash screen.
    /// </summary>
    /// <remarks>
    /// This method hides and disposes of the splash screen window if it is currently displayed.
    /// It is typically called after the application has completed its initialization tasks.
    /// </remarks>
    public static void CloseSplashScreen() {
        SplashScreen?.Close();
    }
    /// <summary>
    /// Displays the main application window and ensures the splash screen is closed.
    /// </summary>
    /// <remarks>
    /// This method is responsible for transitioning from the splash screen to the main application window.
    /// It first closes the splash screen, if it is displayed, and then opens the main window.
    /// </remarks>
    public static void ShowMainWindow() {
        CloseSplashScreen();
        MainWindow?.Show();
    }
    /// <summary>
    /// Closes the main application window.
    /// </summary>
    /// <remarks>
    /// This method hides and disposes of the main application window if it is currently displayed.
    /// It is typically called when the application is shutting down or transitioning to another state.
    /// </remarks>
    public static void CloseMainWindow() {
        MainWindow?.Close();
    }
}
