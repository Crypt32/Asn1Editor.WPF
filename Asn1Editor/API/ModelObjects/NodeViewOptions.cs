using System;
using System.Windows.Input;
using System.Xml.Serialization;
using SysadminsLV.Asn1Editor.API.ViewModel;
using SysadminsLV.Asn1Parser;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.API.ModelObjects; 

[XmlRoot("appSettings")]
public class NodeViewOptions : ViewModelBase {
    Boolean showTagNumber,
        showNodeOffset = true,
        showNodeLength = true,
        showInHex,
        showContent = true,
        showNodePath,
        intAsInt,
        showHexViewer,
        showHexAddrPanel = true,
        showHexAsciiPanel = true,
        hexViewerColoringEnabled = true,
        useRibbonToolbar = true,
        ribbonMinimized;
    Int32 fontSize = 12;
    Int32 maxStringLength = 150;
    
    public ICommand ToggleToolbar => new RelayCommand(_ => UseRibbonToolbar = !UseRibbonToolbar);

    [XmlElement("showTagNum")]
    public Boolean ShowTagNumber {
        get => showTagNumber;
        set {
            if (value == showTagNumber) {
                return;
            }
            showTagNumber = value;
            OnPropertyChanged();
            triggerRequireTreeRefresh();
        }
    }
    [XmlElement("showTagOffset")]
    public Boolean ShowNodeOffset {
        get => showNodeOffset;
        set {
            if (value == showNodeOffset) {
                return;
            }
            showNodeOffset = value;
            OnPropertyChanged();
            triggerRequireTreeRefresh();
        }
    }
    [XmlElement("showNodeLength")]
    public Boolean ShowNodeLength {
        get => showNodeLength;
        set {
            if (value == showNodeLength) {
                return;
            }
            showNodeLength = value;
            OnPropertyChanged();
            triggerRequireTreeRefresh();
        }
    }
    [XmlElement("showHexHeader")]
    public Boolean ShowInHex {
        get => showInHex;
        set {
            if (value == showInHex) {
                return;
            }
            showInHex = value;
            OnPropertyChanged();
            triggerRequireTreeRefresh();
        }
    }
    [XmlElement("showNodeContent")]
    public Boolean ShowContent {
        get => showContent;
        set {
            if (value == showContent) {
                return;
            }
            showContent = value;
            OnPropertyChanged();
            triggerRequireTreeRefresh();
        }
    }
    [XmlElement("showNodePath")]
    public Boolean ShowNodePath {
        get => showNodePath;
        set {
            if (value == showNodePath) {
                return;
            }
            showNodePath = value;
            OnPropertyChanged();
            triggerRequireTreeRefresh();
        }
    }
    [XmlElement("forceInteger")]
    public Boolean IntegerAsInteger {
        get => intAsInt;
        set {
            if (value == intAsInt) {
                return;
            }
            intAsInt = value;
            OnPropertyChanged();
            triggerRequireTreeRefresh(x => x.Value.Tag == (Byte)Asn1Type.INTEGER);
        }
    }
    [XmlElement("showHexViewer")]
    public Boolean ShowHexViewer {
        get => showHexViewer;
        set {
            if (value == showHexViewer) {
                return;
            }
            showHexViewer = value;
            OnPropertyChanged();
        }
    }
    [XmlElement("showAddrPanel")]
    public Boolean ShowHexAddrPanel {
        get => showHexAddrPanel;
        set {
            if (value == showHexAddrPanel) {
                return;
            }
            showHexAddrPanel = value;
            OnPropertyChanged();
        }
    }
    [XmlElement(ElementName = "showAsciiPanel")]
    public Boolean ShowHexAsciiPanel {
        get => showHexAsciiPanel;
        set {
            if (value == showHexAsciiPanel) {
                return;
            }
            showHexAsciiPanel = value;
            OnPropertyChanged();
        }
    }
    [XmlElement(ElementName = "hexViewerColoringEnabled")]
    public Boolean HexViewerColoringEnabled {
        get => hexViewerColoringEnabled;
        set {
            if (value == hexViewerColoringEnabled) {
                return;
            }
            hexViewerColoringEnabled = value;
            OnPropertyChanged();
        }
    }
    [XmlElement(ElementName = "useRibbonToolbar")]
    public Boolean UseRibbonToolbar {
        get => useRibbonToolbar;
        set {
            if (value == useRibbonToolbar) {
                return;
            }
            useRibbonToolbar = value;
            OnPropertyChanged();
        }
    }
    [XmlElement(ElementName = "ribbonMinimized")]
    public Boolean RibbonMinimized {
        get => ribbonMinimized;
        set {
            if (value == ribbonMinimized) {
                return;
            }
            ribbonMinimized = value;
            OnPropertyChanged();
        }
    }
    [XmlElement("fontSize")]
    public Int32 FontSize {
        get => fontSize;
        set {
            if (value == fontSize) {
                return;
            }
            fontSize = value;
            OnPropertyChanged();
        }
    }
    [XmlElement("maxTreeTextLength")]
    public Int32 MaxTreeTextLength {
        get => maxStringLength;
        set {
            if (value == maxStringLength) {
                return;
            }
            maxStringLength = value;
            OnPropertyChanged();
            triggerRequireTreeRefresh();
        }
    }

    void triggerRequireTreeRefresh(Func<Asn1TreeNode, Boolean>? filter = null) {
        RequireTreeRefresh?.Invoke(this, new RequireTreeRefreshEventArgs(filter));
    }

    public event EventHandler<RequireTreeRefreshEventArgs> RequireTreeRefresh;
}
public class RequireTreeRefreshEventArgs(Func<Asn1TreeNode, Boolean>? filter = null) : EventArgs {
    public Func<Asn1TreeNode, Boolean>? Filter { get; } = filter;
}