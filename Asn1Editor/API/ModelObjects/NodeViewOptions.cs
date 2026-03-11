using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Xml.Serialization;
using SysadminsLV.Asn1Editor.API.ViewModel;
using SysadminsLV.Asn1Editor.Core.Tree;
using SysadminsLV.Asn1Parser;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.API.ModelObjects;

[XmlRoot("appSettings")]
public class NodeViewOptions : ViewModelBase, INodeViewOptions {

    public NodeViewOptions() {
        TagView.PropertyChanged += OnTagViewPropertyChanged;
        HexViewer.PropertyChanged += OnHexViewerPropertyChanged;
    }


    public ICommand ToggleToolbar => new RelayCommand(_ => UseRibbonToolbar = !UseRibbonToolbar);

    [XmlElement("showTagNum")]
    public Boolean ShowTagNumber {
        get;
        set {
            if (value == field) {
                return;
            }

            field = value;
            OnPropertyChanged();
            triggerRequireTreeRefresh();
        }
    }

    [XmlElement("showTagOffset")]
    public Boolean ShowNodeOffset {
        get;
        set {
            if (value == field) {
                return;
            }

            field = value;
            OnPropertyChanged();
            triggerRequireTreeRefresh();
        }
    } = true;
    [XmlElement("showNodeLength")]
    public Boolean ShowNodeLength {
        get;
        set {
            if (value == field) {
                return;
            }

            field = value;
            OnPropertyChanged();
            triggerRequireTreeRefresh();
        }
    } = true;
    [XmlElement("showHexHeader")]
    public Boolean ShowInHex {
        get;
        set {
            if (value == field) {
                return;
            }

            field = value;
            OnPropertyChanged();
            triggerRequireTreeRefresh();
        }
    }
    [XmlElement("showNodeContent")]
    public Boolean ShowContent {
        get;
        set {
            if (value == field) {
                return;
            }

            field = value;
            OnPropertyChanged();
            triggerRequireTreeRefresh();
        }
    } = true;
    [XmlElement("showNodePath")]
    public Boolean ShowNodePath {
        get;
        set {
            if (value == field) {
                return;
            }

            field = value;
            OnPropertyChanged();
            triggerRequireTreeRefresh();
        }
    }

    [XmlElement(ElementName = "useRibbonToolbar")]
    public Boolean UseRibbonToolbar {
        get;
        set {
            if (value == field) {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    } = true;
    [XmlElement(ElementName = "ribbonMinimized")]
    public Boolean RibbonMinimized {
        get;
        set {
            if (value == field) {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    }
    [XmlElement("fontSize")]
    public Int32 FontSize {
        get;
        set {
            if (value == field) {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    } = 12;
    [XmlElement("maxTreeTextLength")]
    public Int32 MaxTreeTextLength {
        get;
        set {
            if (value == field) {
                return;
            }

            field = value;
            OnPropertyChanged();
            triggerRequireTreeRefresh();
        }
    } = 150;

    [XmlElement("hexViewer")]
    public HexViewerViewOptions HexViewer {
        get;
        set {
            if (field is not null) {
                field.PropertyChanged -= OnHexViewerPropertyChanged;
            }
            field = value;
            if (field is not null) {
                field.PropertyChanged += OnHexViewerPropertyChanged;
            }
            OnPropertyChanged();
        }
    } = new();

    [XmlElement("tagView")]
    public AsnTagViewOptions TagView {
        get;
        set {
            if (field is not null) {
                field.PropertyChanged -= OnTagViewPropertyChanged;
            }
            field = value;
            if (field is not null) {
                field.PropertyChanged += OnTagViewPropertyChanged;
            }
            OnPropertyChanged();
        }
    } = new();

    void OnTagViewPropertyChanged(Object sender, PropertyChangedEventArgs args) {
        OnPropertyChanged(nameof(TagView));
        switch (args.PropertyName) {
            case nameof(AsnTagViewOptions.Integer):
                triggerRequireTreeRefresh(x => x.Value.Tag == (Byte)Asn1Type.INTEGER);
                break;
            case nameof(AsnTagViewOptions.DateTime):
                triggerRequireTreeRefresh(x => x.Value.Tag is (Byte)Asn1Type.GeneralizedTime or (Byte)Asn1Type.UTCTime);
                break;
        }
    }
    void OnHexViewerPropertyChanged(Object sender, PropertyChangedEventArgs args) {
        OnPropertyChanged(nameof(HexViewer));
    }

    public IAsnIntegerViewOptions GetIntegerViewOptions() {
        return TagView.Integer;
    }
    public IAsnDateTimeViewOptions GetDateTimeViewOptions() {
        return TagView.DateTime;
    }

    void triggerRequireTreeRefresh(Func<AsnTreeNode, Boolean>? filter = null) {
        RequireTreeRefresh?.Invoke(this, new RequireTreeRefreshEventArgs(filter));
    }

    public event EventHandler<RequireTreeRefreshEventArgs>? RequireTreeRefresh;
}

/// <summary>
/// Represents the configuration options for the Hex Viewer in the application.
/// This class provides properties to control the visibility and behavior of various
/// panels and features within the Hex Viewer, such as the address panel, ASCII panel,
/// and coloring options.
/// </summary>
public class HexViewerViewOptions : ViewModelBase {
    /// <summary>
    /// Gets or sets a value indicating whether the Hex Viewer is visible in the application.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the Hex Viewer is visible; otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// This property controls the visibility of the Hex Viewer component in the user interface.
    /// It is bound to various UI elements, such as toggle buttons and visibility converters,
    /// to dynamically show or hide the Hex Viewer based on user interaction or application state.
    /// </remarks>
    [XmlElement("showHexViewer")]
    public Boolean ShowHexViewer {
        get;
        set {
            if (value == field) {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    } = true;
    /// <summary>
    /// Gets or sets a value indicating whether the address panel is visible in the Hex Viewer.
    /// The address panel displays the memory addresses corresponding to the data being viewed.
    /// </summary>
    /// <value>
    /// <c>true</c> if the address panel is visible; otherwise, <c>false</c>.
    /// The default value is <c>true</c>.
    /// </value>
    [XmlElement(ElementName = "showAddressPanel")]
    public Boolean ShowAddrPanel {
        get;
        set {
            if (value == field) {
                return;
            }
            field = value;
            OnPropertyChanged();
        }
    } = true;
    /// <summary>
    /// Gets or sets a value indicating whether the ASCII panel is visible in the Hex Viewer.
    /// The ASCII panel displays the textual representation of the hexadecimal data.
    /// </summary>
    [XmlElement(ElementName = "showAsciiPanel")]
    public Boolean ShowAsciiPanel {
        get;
        set {
            if (value == field) {
                return;
            }
            field = value;
            OnPropertyChanged();
        }
    } = true;
    /// <summary>
    /// Gets or sets a value indicating whether hex coloring for selected tree node is enabled in the Hex Viewer.
    /// When enabled, the Hex Viewer applies color coding to enhance the readability
    /// of the displayed data.
    /// </summary>
    [XmlElement(ElementName = "coloringEnabled")]
    public Boolean ColoringEnabled {
        get;
        set {
            if (value == field) {
                return;
            }
            field = value;
            OnPropertyChanged();
        }
    } = true;
}

/// <summary>
/// Represents the configuration options for displaying and formatting ASN.1 tags in the application.
/// This class provides properties to control the appearance and behavior of tag-related views,
/// such as integer and date/time formatting options.
/// </summary>
public class AsnTagViewOptions : ViewModelBase {
    public AsnTagViewOptions() {
        Integer.PropertyChanged += Integer_OnPropertyChanged;
        DateTime.PropertyChanged += DateTime_OnPropertyChanged;
    }

    void Integer_OnPropertyChanged(Object sender, PropertyChangedEventArgs e) {
        OnPropertyChanged(nameof(Integer));
    }
    void DateTime_OnPropertyChanged(Object sender, PropertyChangedEventArgs e) {
        OnPropertyChanged(nameof(DateTime));
    }

    /// <summary>
    /// Gets or sets the configuration options for displaying and formatting ASN.1 integer values.
    /// This property allows customization of how integer values are represented in the application,
    /// such as whether they are displayed as integers or in another format.
    /// </summary>
    [XmlElement("integer")]
    public AsnIntegerViewOptions Integer {
        get;
        set {
            if (field is not null) {
                field.PropertyChanged -= Integer_OnPropertyChanged;
            }
            field = value;
            if (field is not null) {
                field.PropertyChanged += Integer_OnPropertyChanged;
            }
            OnPropertyChanged();
        }
    } = new();
    /// <summary>
    /// Gets or sets the configuration options for displaying and formatting ASN.1 date and time values
    /// in the application. This property allows customization of date and time representations,
    /// such as enabling or disabling specific formats like ISO 8601.
    /// </summary>
    [XmlElement("datetime")]
    public AsnDateTimeViewOptions DateTime {
        get;
        set {
            if (field is not null) {
                field.PropertyChanged -= DateTime_OnPropertyChanged;
            }
            field = value;
            if (field is not null) {
                field.PropertyChanged += DateTime_OnPropertyChanged;
            }
            OnPropertyChanged();
        }

    } = new();
}

/// <summary>
/// Represents the configuration options for displaying and formatting ASN.1 date and time values
/// in the application. This class provides properties to control the format of date and time
/// representations, such as enabling or disabling the use of ISO 8601 format.
/// </summary>
public class AsnDateTimeViewOptions : ViewModelBase, IAsnDateTimeViewOptions {
    /// <summary>
    /// Gets or sets a value indicating whether date and time values should be displayed
    /// in ISO 8601 format in the ASN.1 tree view.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if date and time values should be displayed in ISO 8601 format;
    /// otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// ISO 8601 is an international standard for representing date and time values.
    /// Enabling this option ensures consistent formatting of date and time values
    /// across the application.
    /// </remarks>
    [XmlElement("useISO8601Format")]
    public Boolean UseISO8601Format {
        get;
        set {
            if (value == field) {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    }
}
/// <summary>
/// Represents the configuration options for displaying and formatting ASN.1 integer values in the application.
/// </summary>
public class AsnIntegerViewOptions : ViewModelBase, IAsnIntegerViewOptions {
    /// <summary>
    /// Gets or sets a value indicating whether integers should be displayed as their numeric values
    /// instead of their hexadecimal representation in the ASN.1 tree view.
    /// </summary>
    /// <value>
    /// <c>true</c> if integers are displayed as numeric values; otherwise, <c>false</c>.
    /// Default is <c>false</c>, meaning integers are displayed in hexadecimal format.
    /// </value>
    [XmlElement("integerAsInteger")]
    public Boolean IntegerAsInteger {
        get;
        set {
            if (value == field) {
                return;
            }
            field = value;
            OnPropertyChanged();
        }
    }
}

/// <summary>
/// Provides data for the <see cref="NodeViewOptions.RequireTreeRefresh"/> event, which is triggered
/// when a tree refresh is required. This class encapsulates a filter function that determines
/// which nodes in the tree should be affected by the refresh operation.
/// </summary>
/// <remarks>
/// The <see cref="RequireTreeRefreshEventArgs"/> class is used to pass additional information
/// about the refresh operation, such as a filter function that can be applied to the nodes
/// in the tree. This allows for selective updates based on specific criteria.
/// </remarks>
public class RequireTreeRefreshEventArgs(Func<AsnTreeNode, Boolean>? filter = null) : EventArgs {
    /// <summary>
    /// Gets the filter function that determines which <see cref="AsnTreeNode"/> objects
    /// should be affected by the tree refresh operation.
    /// </summary>
    /// <value>
    /// A function that takes an <see cref="AsnTreeNode"/> as input and returns a <see cref="Boolean"/>.
    /// If the function returns <c>true</c>, the node is included in the refresh operation; otherwise, it is excluded.
    /// </value>
    /// <remarks>
    /// This property allows for selective updates to the tree by applying a filter function.
    /// If the value is <c>null</c>, no filtering is applied, and all nodes are refreshed.
    /// </remarks>
    public Func<AsnTreeNode, Boolean>? Filter { get; } = filter;
}