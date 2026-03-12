using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace SysadminsLV.Asn1Editor.Controls;

/// <summary>
/// Represents a panel that manages and displays a collection of <see cref="NotificationItem"/> controls.
/// </summary>
/// <remarks>
/// The <see cref="NotificationPanel"/> is a WPF <see cref="ItemsControl"/> designed to host multiple 
/// <see cref="NotificationItem"/> instances. It provides functionality to automatically handle the 
/// <see cref="NotificationItem.DismissRequested"/> event and optionally remove items from the panel when they are closed.
/// </remarks>
public class NotificationPanel : ItemsControl {
    static NotificationPanel() {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NotificationPanel),
            new FrameworkPropertyMetadata(typeof(NotificationPanel)));

        // subscribing to CloseRequested for all child NotificationItems
        EventManager.RegisterClassHandler(
            typeof(NotificationPanel),
            NotificationItem.DismissRequestedEvent,
            new RoutedEventHandler(OnItemCloseRequested));
    }

    #region RemoveItemOnClose

    public static readonly DependencyProperty RemoveItemOnCloseProperty =
        DependencyProperty.Register(
            nameof(RemoveItemOnClose),
            typeof(Boolean),
            typeof(NotificationPanel),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets a value indicating whether a <see cref="NotificationItem"/> should be automatically 
    /// removed from the <see cref="NotificationPanel"/> when it is closed.
    /// </summary>
    /// <value>
    /// <c>true</c> if the <see cref="NotificationItem"/> should be removed upon closure; otherwise, <c>false</c>.
    /// The default value is <c>false</c>.
    /// </value>
    /// <remarks>
    /// When this property is set to <c>true</c>, the <see cref="NotificationPanel"/> will handle the 
    /// <see cref="NotificationItem.DismissRequested"/> event and remove the corresponding item from the panel.
    /// </remarks>
    public Boolean RemoveItemOnClose {
        get => (Boolean)GetValue(RemoveItemOnCloseProperty);
        set => SetValue(RemoveItemOnCloseProperty, value);
    }

    #endregion

    #region Containers

    protected override Boolean IsItemItsOwnContainerOverride(Object item) {
        return item is NotificationItem;
    }

    protected override DependencyObject GetContainerForItemOverride() {
        return new NotificationItem();
    }

    #endregion

    static void OnItemCloseRequested(Object sender, RoutedEventArgs e) {
        var panel = (NotificationPanel)sender;

        if (!panel.RemoveItemOnClose) {
            return;
        }

        if (e.OriginalSource is not NotificationItem item) {
            return;
        }

        // If exists in ItemsSource, and it is IList – remove data item
        if (panel.ItemsSource is IList list) {
            Object? dataItem = panel.ItemContainerGenerator.ItemFromContainer(item);
            if (!ReferenceEquals(dataItem, DependencyProperty.UnsetValue) &&
                list.Contains(dataItem)) {
                list.Remove(dataItem);
            }
            return;
        }

        // otherwise work with Items (manual NotificationItem in XAML)
        if (panel.Items.Contains(item)) {
            panel.Items.Remove(item);
        }
    }
}