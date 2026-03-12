using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SysadminsLV.Asn1Editor.Controls.Behaviors;

public static class ScrollSyncBehavior {
    // group registry, contains all groups and their viewers
    // one group per tab
    static readonly ScrollGroupRegistry _registry = new();

    public static readonly DependencyProperty GroupProperty =
        DependencyProperty.RegisterAttached(
            "Group",
            typeof(String),
            typeof(ScrollSyncBehavior),
            new PropertyMetadata(null, OnGroupChanged));

    public static void SetGroup(DependencyObject element, String value) {
        element.SetValue(GroupProperty, value);
    }

    public static String GetGroup(DependencyObject element) {
        return (String)element.GetValue(GroupProperty);
    }

    static void OnGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not FrameworkElement fe) {
            return;
        }

        String? oldName = e.OldValue as String;
        String? newName = e.NewValue as String;

        // detach from old group if group changed
        if (!String.IsNullOrEmpty(oldName) &&
            (String.IsNullOrEmpty(newName) ||
             !String.Equals(oldName, newName, StringComparison.Ordinal))) {

            DetachFromGroup(oldName, fe);
        }

        // new value, may not be a string yet. Waiting for BindingExpression and so on. Do nothing.
        if (String.IsNullOrEmpty(newName)) {
            return;
        }

        if (fe.IsLoaded) {
            AttachWhenReady(fe, newName);
        } else {
            fe.Loaded -= OnElementLoaded;
            fe.Loaded += OnElementLoaded;
        }
    }

    static void OnElementLoaded(Object sender, RoutedEventArgs e) {
        FrameworkElement fe = (FrameworkElement)sender;
        fe.Loaded -= OnElementLoaded;

        String groupName = GetGroup(fe);
        if (String.IsNullOrEmpty(groupName)) {
            return;
        }

        AttachWhenReady(fe, groupName);
    }

    static void AttachWhenReady(FrameworkElement fe, String groupName) {
        // if Right panel is not visible yet, wait for it
        if (!fe.IsVisible) {
            fe.IsVisibleChanged -= OnElementIsVisibleChanged;
            fe.IsVisibleChanged += OnElementIsVisibleChanged;
            return;
        }

        // if it is visible, try to find and register ScrollViewer with a group
        ScrollViewer? sv = FindScrollViewer(fe);
        if (sv == null) {
            // Подстрахуемся: ещё один заход после layout
            fe.Dispatcher.BeginInvoke(new Action(() => {
                String currentGroup = GetGroup(fe);
                if (String.IsNullOrEmpty(currentGroup)) {
                    return;
                }

                ScrollViewer? retrySv = FindScrollViewer(fe);
                if (retrySv != null) {
                    AttachToGroup(currentGroup, retrySv);
                }
            }), DispatcherPriority.Loaded);
            return;
        }

        AttachToGroup(groupName, sv);
    }

    static void OnElementIsVisibleChanged(Object sender, DependencyPropertyChangedEventArgs e) {
        if (e.NewValue is not true) {
            return;
        }

        FrameworkElement fe = (FrameworkElement)sender;
        fe.IsVisibleChanged -= OnElementIsVisibleChanged;

        String groupName = GetGroup(fe);
        if (String.IsNullOrEmpty(groupName)) {
            return;
        }

        AttachWhenReady(fe, groupName);
    }

    static void AttachToGroup(String groupName, ScrollViewer sv) {
        _registry.AttachViewer(groupName, sv);

        sv.ScrollChanged -= OnScrollChanged;
        sv.ScrollChanged += OnScrollChanged;

        sv.Unloaded -= OnViewerUnloaded;
        sv.Unloaded += OnViewerUnloaded;
    }

    static void DetachFromGroup(String groupName, FrameworkElement fe) {
        ScrollViewer? sv = FindScrollViewer(fe);
        if (sv is null) {
            return;
        }

        _registry.DetachViewer(groupName, sv);

        sv.ScrollChanged -= OnScrollChanged;
        sv.Unloaded -= OnViewerUnloaded;
    }

    static void OnViewerUnloaded(Object sender, RoutedEventArgs e) {
        ScrollViewer sv = (ScrollViewer)sender;

        _registry.RemoveViewer(sv);

        sv.ScrollChanged -= OnScrollChanged;
        sv.Unloaded -= OnViewerUnloaded;
    }

    // here we do the actual scroll synchronization. We do percentage-based sync
    // to accommodate different content sizes
    static void OnScrollChanged(Object sender, ScrollChangedEventArgs e) {
        // react to changes to both vertical and horizontal scrolls
        if (e is { VerticalChange: 0, HorizontalChange: 0 }) {
            return;
        }

        ScrollViewer source = (ScrollViewer)sender;

        if (!_registry.TryGetGroupByViewer(source, out ScrollGroup group)) {
            return;
        }

        if (group.IsUpdating) {
            return;
        }

        // calculate fractions for vertical and horizontal offsets
        Double vDenom = source.ExtentHeight - source.ViewportHeight;
        Double hDenom = source.ExtentWidth - source.ViewportWidth;

        Double vFrac = 0;
        Double hFrac = 0;
        Boolean hasV = vDenom > 0;
        Boolean hasH = hDenom > 0;

        if (hasV) {
            vFrac = source.VerticalOffset / vDenom;
        }

        if (hasH) {
            hFrac = source.HorizontalOffset / hDenom;
        }

        try {
            group.IsUpdating = true;

            foreach (ScrollViewer other in group.Viewers) {
                if (ReferenceEquals(other, source)) {
                    continue;
                }

                // vertical sync
                if (hasV) {
                    Double oVDenom = other.ExtentHeight - other.ViewportHeight;
                    if (oVDenom > 0) {
                        Double targetV = vFrac * oVDenom;
                        other.ScrollToVerticalOffset(targetV);
                    }
                    else {
                        other.ScrollToVerticalOffset(0);
                    }
                }

                // horizontal sync
                if (hasH) {
                    Double oHDenom = other.ExtentWidth - other.ViewportWidth;
                    if (oHDenom > 0) {
                        Double targetH = hFrac * oHDenom;
                        other.ScrollToHorizontalOffset(targetH);
                    }
                    else {
                        other.ScrollToHorizontalOffset(0);
                    }
                }
            }
        }
        finally {
            group.IsUpdating = false;
        }
    }

    static ScrollViewer? FindScrollViewer(DependencyObject root) {
        if (root is ScrollViewer sv) {
            return sv;
        }

        Int32 count = VisualTreeHelper.GetChildrenCount(root);
        for (Int32 i = 0; i < count; i++) {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            ScrollViewer? result = FindScrollViewer(child);
            if (result != null) {
                return result;
            }
        }

        return null;
    }

    // helper class to represent a scroll group (tab) and its scroll viewers
    sealed class ScrollGroup {
        public List<ScrollViewer> Viewers { get; } = [];
        public Boolean IsUpdating { get; set; }
    }
    // scroll group registry, manages all groups and their viewers
    // maintains both, forward and reverse mappings for faster lookups
    // in addition, handles group creation and deletion to keep them in sync
    sealed class ScrollGroupRegistry {
        readonly Dictionary<String, ScrollGroup> _groups = new(StringComparer.Ordinal);
        readonly Dictionary<ScrollViewer, ScrollGroup> _viewerToGroup = new();

        ScrollGroup GetOrCreateGroup(String groupName) {
            if (!_groups.TryGetValue(groupName, out ScrollGroup? group)) {
                group = new ScrollGroup();
                _groups[groupName] = group;
            }
            return group;
        }

        public Boolean TryGetGroupByName(String groupName, out ScrollGroup group) {
            return _groups.TryGetValue(groupName, out group!);
        }

        public Boolean TryGetGroupByViewer(ScrollViewer viewer, out ScrollGroup group) {
            return _viewerToGroup.TryGetValue(viewer, out group!);
        }

        public void AttachViewer(String groupName, ScrollViewer sv) {
            ScrollGroup group = GetOrCreateGroup(groupName);

            if (group.Viewers.Contains(sv)) {
                // уже в группе
                return;
            }

            group.Viewers.Add(sv);
            _viewerToGroup[sv] = group;
        }

        public void DetachViewer(String groupName, ScrollViewer sv) {
            if (!_groups.TryGetValue(groupName, out ScrollGroup? group)) {
                return;
            }

            if (!group.Viewers.Remove(sv)) {
                return;
            }

            _viewerToGroup.Remove(sv);

            if (group.Viewers.Count == 0) {
                _groups.Remove(groupName);
            }
        }

        public void RemoveViewer(ScrollViewer sv) {
            if (!_viewerToGroup.TryGetValue(sv, out ScrollGroup? group)) {
                return;
            }

            _viewerToGroup.Remove(sv);
            group.Viewers.Remove(sv);

            if (group.Viewers.Count == 0) {
                // find group key and remove entire group
                String? keyToRemove = null;
                foreach (KeyValuePair<String, ScrollGroup> kv in _groups) {
                    if (ReferenceEquals(kv.Value, group)) {
                        keyToRemove = kv.Key;
                        break;
                    }
                }

                if (keyToRemove != null) {
                    _groups.Remove(keyToRemove);
                }
            }
        }
    }
}
