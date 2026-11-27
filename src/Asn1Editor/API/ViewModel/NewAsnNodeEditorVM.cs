using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Input;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Parser;
using SysadminsLV.Asn1Parser.Universal;
using SysadminsLV.WPF.Mvvm.Commands;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

public class NewAsnNodeEditorVM : ClosableWindowVM, INewAsnNodeEditorVM {
    Boolean shouldGenerateNode;
    Boolean formTagChecked, decimalTagChecked, hexTagChecked,
        clsConstructedChecked, clsSpecificChecked, clsApplicationChecked, clsPrivateChecked;
    String? decimalTagText, hexTagText, resultTagDecimal, resultTagHex, resultTagName;
    Asn1Type selectedType;

    public NewAsnNodeEditorVM(UserSettings options) {
        UserSettings = options;
        formTagChecked = true;
        OkCommand = new RelayCommand(save, canSave);
        SelectedType = Asn1Type.SEQUENCE;
    }

    public ICommand OkCommand { get; }

    public UserSettings UserSettings { get; }

    public Boolean FormTagChecked {
        get => formTagChecked;
        set {
            if (SetField(ref formTagChecked, value) && formTagChecked) {
                DecimalTagText = String.Empty;
                HexTagText = String.Empty;
                updateResultTagInfo();
            }
        }
    }
    public Boolean DecimalTagChecked {
        get => decimalTagChecked;
        set {
            if (SetField(ref decimalTagChecked, value) && decimalTagChecked) {
                DecimalTagText = "48";
                HexTagText = String.Empty;
            }
        }
    }
    public Boolean HexTagChecked {
        get => hexTagChecked;
        set {
            if (SetField(ref hexTagChecked, value) && hexTagChecked) {
                HexTagText = "30";
                DecimalTagText = String.Empty;
            }
        }
    }

    public Boolean ConstructedChecked {
        get => clsConstructedChecked;
        set {
            if (SetField(ref clsConstructedChecked, value)) {
                updateResultTagInfo();
            }
        }
    }
    public Boolean SpecificChecked {
        get => clsSpecificChecked;
        set {
            if (SetField(ref clsSpecificChecked, value)) {
                updateResultTagInfo();
            }
        }
    }
    public Boolean ApplicationChecked {
        get => clsApplicationChecked;
        set {
            if (SetField(ref clsApplicationChecked, value)) {
                updateResultTagInfo();
            }
        }
    }
    public Boolean PrivateChecked {
        get => clsPrivateChecked;
        set {
            if (SetField(ref clsPrivateChecked, value)) {
                updateResultTagInfo();
            }
        }
    }

    public Asn1Type SelectedType {
        get => selectedType;
        set {
            if (SetField(ref selectedType, value)) {
                ConstructedChecked = SelectedType is Asn1Type.SEQUENCE or Asn1Type.SET;
                // no need to call updateResultTagInfo(), it is already called.
            }
        }
    }
    public String? DecimalTagText {
        get => decimalTagText;
        set {
            if (SetField(ref decimalTagText, value)) {
                updateResultTagInfo();
            }
        }
    }
    public String? HexTagText {
        get => hexTagText;
        set {
            if (SetField(ref hexTagText, value)) {
                updateResultTagInfo();
            }
        }
    }

    public String ResultTagDecimal {
        get => resultTagDecimal;
        set => SetField(ref resultTagDecimal, value);
    }
    public String ResultTagHex {
        get => resultTagHex;
        set => SetField(ref resultTagHex, value);
    }
    public String ResultTagName {
        get => resultTagName;
        set => SetField(ref resultTagName, value);
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
        return tag switch {
            // for BIT_STRING need to include unused bit count
            (Int32)Asn1Type.BIT_STRING => [tag, 1, 0],
            (Int32)Asn1Type.UTCTime => new Asn1UtcTime(DateTime.Now).GetRawData(),
            (Int32)Asn1Type.GeneralizedTime => new Asn1GeneralizedTime(DateTime.Now).GetRawData(),
            _ => [tag, 0]
        };
    }
}
