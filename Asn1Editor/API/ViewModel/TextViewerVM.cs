using System;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using SysadminsLV.Asn1Editor.API.Abstractions;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.Utils;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

class TextViewerVM : ViewModelBase, ITextViewerVM {
    readonly IUIMessenger _uiMessenger;
    readonly Asn1TreeNode rootNode;
    const Int32 minLength = 60;
    const Int32 defaultLength = 80;
    const Int32 maxLength = 400;
    ITextRenderer renderer;
    Boolean certutilChecked, openSSLChecked;

    String text;
    Int32 currentLength = 80;
    String currentLengthStr = "80";

    public TextViewerVM(IHasAsnDocumentTabs appTabs, NodeViewOptions options, IUIMessenger uiMessenger) {
        rootNode = appTabs.SelectedTab!.GetPrimaryDocument().DataSource.SelectedNode!;
        NodeViewOptions = options;
        _uiMessenger = uiMessenger;
        CurrentLength = defaultLength.ToString(CultureInfo.InvariantCulture);
        SaveCommand = new RelayCommand(saveFile);
        PrintCommand = new RelayCommand(print);
        ApplyCommand = new RelayCommand(applyNewLength);
        CertutilViewChecked = true;
    }

    public ICommand SaveCommand { get; set; }
    public ICommand PrintCommand { get; set; }
    public ICommand ApplyCommand { get; }

    public NodeViewOptions NodeViewOptions { get; }

    public String Text {
        get => text;
        set {
            text = value;
            OnPropertyChanged();
        }
    }
    public String CurrentLength {
        get => currentLengthStr;
        set {
            currentLengthStr = value;
            OnPropertyChanged();
        }
    }
    public Boolean CertutilViewChecked {
        get => certutilChecked;
        set {
            if (certutilChecked == value) {
                return;
            }
            certutilChecked = value;
            if (certutilChecked) {
                renderer = new CertutilRenderer(rootNode);
                refreshView();
            }
            OnPropertyChanged();
        }
    }
    public Boolean OpenSSLViewChecked {
        get => openSSLChecked;
        set {
            if (openSSLChecked == value) {
                return;
            }
            openSSLChecked = value;
            if (openSSLChecked) {
                renderer = new OpenSSLRenderer(rootNode);
                refreshView();
            }
            OnPropertyChanged();
        }
    }

    void refreshView() {
        Text = renderer.RenderText(currentLength);
    }

    void print(Object obj) {
        StaticCommands.Print(Text, NodeViewOptions.FontSize);
    }
    void applyNewLength(Object obj) {
        if (!Int32.TryParse(CurrentLength, NumberStyles.Integer, null, out Int32 value)) {
            CurrentLength = currentLength.ToString(CultureInfo.InvariantCulture);
            return;
        }
        if (value == currentLength) { return; }
        currentLength = value is < minLength or > maxLength
            ? minLength
            : value;
        CurrentLength = currentLength.ToString(CultureInfo.InvariantCulture);
        refreshView();
    }

    void saveFile(Object obj) {
        if (!_uiMessenger.TryGetSaveFileName(out String filePath)) {
            return;
        }
        try {
            File.WriteAllText(filePath, Text);
        } catch (Exception e) {
            _uiMessenger.ShowError(e.Message, "Save Error");
        }
    }
}