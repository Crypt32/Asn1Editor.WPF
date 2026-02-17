using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Input;
using SysadminsLV.Asn1Editor.API.Abstractions;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Parser;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.API.ViewModel;
public class NewAsnNodeEditorVM : ClosableWindowVM, INewAsnNodeEditorVM {
    Boolean shouldGenerateNode;
    Boolean formTagChecked, decimalTagChecked, hexTagChecked,
        clsConstructedChecked, clsSpecificChecked, clsApplicationChecked, clsPrivateChecked;
    String? decimalTagText, hexTagText, resultTagDecimal, resultTagHex, resultTagName;
    Asn1Type selectedType;

    public NewAsnNodeEditorVM(NodeViewOptions options) {
        NodeViewOptions = options;
        formTagChecked = true;
        OkCommand = new RelayCommand(save, canSave);
        SelectedType = Asn1Type.SEQUENCE;
    }

    public ICommand OkCommand { get; }

    public NodeViewOptions NodeViewOptions { get; }

    public Boolean FormTagChecked {
        get => formTagChecked;
        set {
            formTagChecked = value;
            OnPropertyChanged();
            if (formTagChecked) {
                DecimalTagText = null;
                HexTagText = null;
                updateResultTagInfo();
            }
        }
    }
    public Boolean DecimalTagChecked {
        get => decimalTagChecked;
        set {
            decimalTagChecked = value;
            OnPropertyChanged();
            if (decimalTagChecked) {
                DecimalTagText = "48";
                HexTagText = null;
            }
        }
    }
    public Boolean HexTagChecked {
        get => hexTagChecked;
        set {
            hexTagChecked = value;
            OnPropertyChanged();
            if (hexTagChecked) {
                HexTagText = "30";
                DecimalTagText = null;
            }
        }
    }

    public Boolean ConstructedChecked {
        get => clsConstructedChecked;
        set {
            clsConstructedChecked = value;
            OnPropertyChanged();
            updateResultTagInfo();
        }
    }
    public Boolean SpecificChecked {
        get => clsSpecificChecked;
        set {
            clsSpecificChecked = value;
            OnPropertyChanged();
            updateResultTagInfo();
        }
    }
    public Boolean ApplicationChecked {
        get => clsApplicationChecked;
        set {
            clsApplicationChecked = value;
            OnPropertyChanged();
            updateResultTagInfo();
        }
    }
    public Boolean PrivateChecked {
        get => clsPrivateChecked;
        set {
            clsPrivateChecked = value;
            OnPropertyChanged();
            updateResultTagInfo();
        }
    }

    public Asn1Type SelectedType {
        get => selectedType;
        set {
            selectedType = value;
            OnPropertyChanged();
            ConstructedChecked = SelectedType is Asn1Type.SEQUENCE or Asn1Type.SET;
            // no need to call updateResultTagInfo(), it is already called.
        }
    }
    public String? DecimalTagText {
        get => decimalTagText;
        set {
            decimalTagText = value;
            OnPropertyChanged();
            updateResultTagInfo();
        }
    }
    public String? HexTagText {
        get => hexTagText;
        set {
            hexTagText = value;
            OnPropertyChanged();
            updateResultTagInfo();
        }
    }

    public String ResultTagDecimal {
        get => resultTagDecimal;
        set {
            resultTagDecimal = value;
            OnPropertyChanged();
        }
    }
    public String ResultTagHex {
        get => resultTagHex;
        set {
            resultTagHex = value;
            OnPropertyChanged();
        }
    }
    public String ResultTagName {
        get => resultTagName;
        set {
            resultTagName = value;
            OnPropertyChanged();
        }
    }

    Byte getResultingTag() {
        Byte retValue;
        if (FormTagChecked) {
            retValue = (Byte)SelectedType;
            if (ConstructedChecked) {
                retValue |= (Byte)Asn1Class.CONSTRUCTED;
            }
            if (SpecificChecked) {
                retValue |= (Byte)Asn1Class.CONTEXT_SPECIFIC;
            }
            if (ApplicationChecked) {
                retValue |= (Byte)Asn1Class.APPLICATION;
            }
            if (PrivateChecked) {
                retValue |= (Byte)Asn1Class.PRIVATE;
            }
        } else if (DecimalTagChecked) {
            retValue = Byte.Parse(DecimalTagText!);
        } else {
            retValue = Byte.Parse(HexTagText!, NumberStyles.HexNumber);
        }

        return retValue;
    }
    void updateResultTagInfo() {
        Byte tag = getResultingTag();
        ResultTagDecimal = tag.ToString();
        ResultTagHex = "0x" + tag.ToString("X2");
        ResultTagName = Asn1Reader.GetTagName(tag);
    }
    void save(Object o) {
        shouldGenerateNode = true;
        CloseCommand.Execute(null);
    }
    Boolean canSave(Object o) {
        if (FormTagChecked) {
            if (SelectedType == Asn1Type.RESERVED) {
                return ConstructedChecked || SpecificChecked || ApplicationChecked || PrivateChecked;
            }

            return true;
        }
        if (DecimalTagChecked && Byte.TryParse(DecimalTagText, out _)) {
            return true;
        }
        if (HexTagChecked) {
            String value = HexTagText ?? String.Empty;
            return Regex.IsMatch(value, "^[0-9A-F]{1,2}$", RegexOptions.IgnoreCase);
        }

        return false;
    }
    public Byte[]? GetAsnNode() {
        if (!shouldGenerateNode) {
            return null;
        }
        Byte tag = getResultingTag();
        // for BIT_STRING need to include unused bit count
        if (tag == (Int32)Asn1Type.BIT_STRING) {
            return [tag, 1, 0];
        }
        return [getResultingTag(), 0];
    }
}
