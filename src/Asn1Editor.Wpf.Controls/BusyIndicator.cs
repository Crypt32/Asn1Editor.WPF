#nullable enable
using System;
using System.Windows;
using System.Windows.Controls;

namespace SysadminsLV.Asn1Editor.Controls;

public class BusyIndicator : Control {
    static BusyIndicator() {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(BusyIndicator),
            new FrameworkPropertyMetadata(typeof(BusyIndicator)));
    }

    #region HeaderText

    public static readonly DependencyProperty HeaderTextProperty = DependencyProperty.Register(
        nameof(HeaderText),
        typeof(String),
        typeof(BusyIndicator),
        new PropertyMetadata(default));

    public String HeaderText {
        get => (String)GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    #endregion

    #region Text

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(String),
        typeof(BusyIndicator),
        new PropertyMetadata(default));

    public String Text {
        get => (String)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    #endregion

    #region IsIndeterminate

    public static readonly DependencyProperty IsIndeterminateProperty = DependencyProperty.Register(
        nameof(IsIndeterminate),
        typeof(Boolean),
        typeof(BusyIndicator),
        new PropertyMetadata(true));

    public Boolean IsIndeterminate {
        get => (Boolean)GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    #endregion

    #region ProgressValue

    public static readonly DependencyProperty ProgressValueProperty = DependencyProperty.Register(
        nameof(ProgressValue),
        typeof(Double),
        typeof(BusyIndicator),
        new PropertyMetadata(default));

    public Double ProgressValue {
        get => (Double)GetValue(ProgressValueProperty);
        set => SetValue(ProgressValueProperty, value);
    }

    #endregion

    #region IsShown

    public static readonly DependencyProperty IsShownProperty = DependencyProperty.Register(
        nameof(IsShown),
        typeof(Boolean),
        typeof(BusyIndicator),
        new PropertyMetadata(false));
    /// <summary>
    /// indicates whether the control should show or not.
    /// </summary>
    public Boolean IsShown {
        get => (Boolean)GetValue(IsShownProperty);
        set => SetValue(IsShownProperty, value);
    }

    #endregion
}
