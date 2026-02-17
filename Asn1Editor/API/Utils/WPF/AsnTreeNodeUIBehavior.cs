using System;
using System.Windows;
using System.Windows.Controls;

namespace SysadminsLV.Asn1Editor.API.Utils.WPF;

/// <summary>
/// Provides attached properties and behaviors for managing the UI behavior of tree nodes in WPF applications.
/// </summary>
public static class AsnTreeNodeUIBehavior {
    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.RegisterAttached(
            "IsExpanded",
            typeof(Boolean),
            typeof(AsnTreeNodeUIBehavior),
            new FrameworkPropertyMetadata(
                true, // Default: expanded
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsExpandedChanged
            )
        );

    public static Boolean GetIsExpanded(DependencyObject obj) => (Boolean)obj.GetValue(IsExpandedProperty);
    public static void SetIsExpanded(DependencyObject obj, Boolean value) => obj.SetValue(IsExpandedProperty, value);

    // Sync attached property with TreeViewItem.IsExpanded
    static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is TreeViewItem item && e.NewValue is Boolean isExpanded) {
            // Prevent infinite loop
            if (item.IsExpanded != isExpanded) {
                item.IsExpanded = isExpanded;
            }
        }
    }
}