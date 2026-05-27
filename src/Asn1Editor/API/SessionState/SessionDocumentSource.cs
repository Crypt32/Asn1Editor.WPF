using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using SysadminsLV.Asn1Editor.API.ModelObjects;

namespace SysadminsLV.Asn1Editor.API.SessionState;

/// <summary>
/// Represents a source for managing session documents within the application.
/// This class handles the interaction between session tabs and their associated state,
/// including monitoring changes in the collection of tabs and their properties.
/// </summary>
/// <remarks>
/// The <see cref="SessionDocumentSource"/> class is responsible for saving session states
/// asynchronously when changes occur in the session tabs or their properties.
/// It also utilizes a timer to periodically save the session state.
/// </remarks>
class SessionDocumentSource : IDisposable {
    readonly ISessionTabHost _sessionTabHost;
    readonly INotifyCollectionChanged _tabsChangeSource;
    readonly DispatcherTimer _timer = new();

    public SessionDocumentSource(ISessionTabHost sessionTabHost, NodeViewOptions nodeViewOptions) {
        _sessionTabHost = sessionTabHost;
        _tabsChangeSource = sessionTabHost.Tabs;
        _sessionTabHost.PropertyChanged += SessionTabHost_OnPropertyChanged;
        _tabsChangeSource.CollectionChanged += TabsChangeSource_OnCollectionChanged;
        _timer.Tick += Timer_OnTick;
        _timer.Interval = TimeSpan.FromSeconds(30);
        //_timer.Start();
    }
    

    async Task saveSessionAsync() {
        _timer.Stop();
        try {
            await SessionBackupManager.Instance.SaveSessionAsync(_sessionTabHost);
        } catch (Exception ex) {
            App.Write(ex);
        } finally {
            _timer.Start();
        }
    }

    async void Timer_OnTick(Object sender, EventArgs e) {
        await saveSessionAsync();
    }
    async void TabsChangeSource_OnCollectionChanged(Object sender, NotifyCollectionChangedEventArgs args) {
        await saveSessionAsync();
    }
    async void SessionTabHost_OnPropertyChanged(Object sender, PropertyChangedEventArgs args) {
        await saveSessionAsync();
    }

    public void Shutdown() {
        SessionBackupManager.Instance.Shutdown();
    }

    /// <inheritdoc />
    public void Dispose() {
        _sessionTabHost.PropertyChanged -= SessionTabHost_OnPropertyChanged;
        _tabsChangeSource.CollectionChanged -= TabsChangeSource_OnCollectionChanged;
        _timer.Stop();
        _timer.Tick -= Timer_OnTick;
    }
}