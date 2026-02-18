using System;
using System.Linq;
using System.Windows.Input;
using SysadminsLV.Asn1Editor.API.Abstractions;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.Utils;
using SysadminsLV.Asn1Editor.Core.ASN;
using SysadminsLV.Asn1Editor.Core.Tree;
using SysadminsLV.Asn1Parser;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

class TagDataEditorVM : ViewModelBase, ITagDataEditorVM {
    readonly IUIMessenger _uiMessenger;
    readonly IAsn1DocumentContext _data;
    NodeEditMode mode;
    Boolean? dialogResult;
    String tagDetails;
    AsnViewValue tagValue, oldValue;
    Boolean rbText = true, rbHex, isReadonly;


    public TagDataEditorVM(IHasAsnDocumentTabs appTabs, IUIMessenger uiMessenger) {
        _data = appTabs.SelectedTab!.GetPrimaryDocument().AsnDocContext;
        _uiMessenger = uiMessenger;
        NodeViewOptions = appTabs.NodeViewOptions;
        OkCommand = new RelayCommand(submitValues);
        CloseCommand = new RelayCommand(close);
        initialize();
    }

    public ICommand OkCommand { get; }
    public ICommand CloseCommand { get; }
    public AsnNodeValue Node { get; private set; }
    public NodeViewOptions NodeViewOptions { get; }

    /// <summary>
    /// Gets or sets ASN node formatted text information.
    /// </summary>
    public String TagDetails {
        get => tagDetails;
        set {
            tagDetails = value;
            OnPropertyChanged();
        }
    }
    public AsnViewValue TagValue {
        get => tagValue;
        set {
            tagValue = value;
            OnPropertyChanged();
        }
    }
    public Boolean UnusedBitsVisible => Node.Tag == (Byte)Asn1Type.BIT_STRING;
    public Boolean IsReadOnly {
        get => isReadonly;
        set {
            isReadonly = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEditable));
        }
    }
    public Boolean IsEditable => !IsReadOnly;
    public Boolean RbText {
        get => rbText;
        set {
            if (rbText == value) {
                return;
            }
            rbText = value;
            if (rbText) {
                editText();
            }
            OnPropertyChanged();
        }
    }
    public Boolean RbHex {
        get => rbHex;
        set {
            if (rbHex == value) {
                return;
            }
            rbHex = value;
            if (rbHex) {
                editHex();
            }
            OnPropertyChanged();
        }
    }
    public Boolean IsRbTextEnabled => (TagValue.Options & AsnViewValueOptions.SupportsPrintableText) != 0;
    public Boolean? DialogResult {
        get => dialogResult;
        set {
            dialogResult = value;
            OnPropertyChanged();
        }
    }

    void initialize() {
        tagValue = new AsnViewValue();
    }
    void submitValues(Object obj) {
        if (Equals(TagValue, oldValue)) {
            DialogResult = true;
            return;
        }
        saveEditChanges();
    }
    void close(Object o) {
        Node = null;
        DialogResult = true;
    }
    void saveEditChanges() {
        Byte[]? binValue = validateValue();
        if (binValue is null) {
            return;
        }

        _data.UpdateNode(_data.SelectedNode!, binValue);
        DialogResult = true;
    }
    Byte[]? validateValue() {
        Byte[] binValue = null;
        try {
            // if we are in hex mode, or we are in text mode, but text is hex, go to hex
            if (rbHex || (!rbHex && (TagValue.Options & AsnViewValueOptions.SupportsPrintableText) == 0)) {
                String textValue = Node.Tag == (Int32)Asn1Type.BIT_STRING
                    ? $"{TagValue.UnusedBits:X2} {TagValue.TextValue}"
                    : TagValue.TextValue;

                binValue = AsnDecoder.EncodeHex(Node.Tag, textValue, TagValue.UnusedBits);
            } else {
                binValue = AsnDecoder.EncodeGeneric(Node.Tag, TagValue.TextValue, TagValue.UnusedBits);
            }
        } catch (Exception e) {
            _uiMessenger.ShowError(e.Message);
        }
        return binValue;
    }
    void setRbHexSilent() {
        rbHex = true;
        OnPropertyChanged(nameof(RbHex));
    }
    void editText() {
        if (Node is null) {
            return;
        }

        TagValue = AsnDecoder.GetEditValue(new Asn1Reader(_data.RawData.Skip(Node.Offset).Take(Node.TagLength).ToArray()));
        if ((TagValue.Options & AsnViewValueOptions.SupportsPrintableText) == 0) {
            setRbHexSilent();
        }
        OnPropertyChanged(nameof(IsRbTextEnabled));
    }
    void editHex() {
        if (Node is null) { return; }
        TagValue.TextValue = HexUtility.BinaryToHex(_data.RawData
            .Skip(Node.PayloadStartOffset)
            .Take(Node.PayloadLength)
            .ToArray());
    }
    void copyValues() {
        Node = _data.SelectedNode!.Value;
        TagValue.UnusedBits = Node.UnusedBits;
        IsReadOnly = Node.IsContainer || Node.Tag == (Byte)Asn1Type.NULL;
        TagDetails = Node.GetFormattedMetadata();
        OnPropertyChanged(nameof(UnusedBitsVisible));
    }

    public void SetBinding(NodeEditMode editMode) {
        mode = editMode;
        copyValues();
        if (IsReadOnly || Node.Tag == (Byte)Asn1Type.NULL) {
            TagValue.TextValue = "Containers and NULL (0x05) tags are not editable";
        } else {
            editText();
        }
        oldValue = TagValue.Clone();
    }
}