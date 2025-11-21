using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.Controls;

/// <summary>
/// Represents a notification item control that can display an icon, a message, and supports close functionality.
/// </summary>
/// <remarks>
/// The <see cref="NotificationItem"/> is a customizable WPF control that provides a way to display notifications
/// with different visual styles based on the <see cref="NotificationKind"/>. It supports an optional close button
/// and raises a <see cref="DismissRequested"/> event when the notification is closed.
/// </remarks>
public class NotificationItem : ContentControl {
    static NotificationItem() {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NotificationItem),
            new FrameworkPropertyMetadata(typeof(NotificationItem)));
    }

    public NotificationItem() {
        DismissCommand = new RelayCommand(_ => Dismiss(), _ => IsDismissable);
    }

    #region Icon

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(ImageSource),
            typeof(NotificationItem),
            new PropertyMetadata(null));
    
    public ImageSource? Icon {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    #endregion

    #region Kind

    public static readonly DependencyProperty KindProperty =
        DependencyProperty.Register(
            nameof(Kind),
            typeof(NotificationKind),
            typeof(NotificationItem),
            new PropertyMetadata(NotificationKind.Info));

    public NotificationKind Kind {
        get => (NotificationKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    #endregion

    #region IsDismissable / IsDismissed

    public static readonly DependencyProperty IsDismissableProperty =
        DependencyProperty.Register(
            nameof(IsDismissable),
            typeof(Boolean),
            typeof(NotificationItem),
            new PropertyMetadata(true));

    public Boolean IsDismissable {
        get => (Boolean)GetValue(IsDismissableProperty);
        set => SetValue(IsDismissableProperty, value);
    }

    public static readonly DependencyProperty IsDismissedProperty =
        DependencyProperty.Register(
            nameof(IsDismissed),
            typeof(Boolean),
            typeof(NotificationItem),
            new PropertyMetadata(false));

    public Boolean IsDismissed {
        get => (Boolean)GetValue(IsDismissedProperty);
        set => SetValue(IsDismissedProperty, value);
    }

    #endregion

    #region CloseRequested event

    public static readonly RoutedEvent DismissRequestedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(DismissRequested),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(NotificationItem));

    public event RoutedEventHandler DismissRequested {
        add => AddHandler(DismissRequestedEvent, value);
        remove => RemoveHandler(DismissRequestedEvent, value);
    }

    protected virtual void OnDismissRequested() {
        var args = new RoutedEventArgs(DismissRequestedEvent, this);
        RaiseEvent(args);
    }

    public void Dismiss() {
        if (!IsDismissable) {
            return;
        }

        IsDismissed = true;
        OnDismissRequested();
    }

    #endregion

    #region DismissCommand

    public ICommand DismissCommand { get; }
    
    #endregion
}