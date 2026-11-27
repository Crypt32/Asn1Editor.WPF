using System;
using SysadminsLV.Asn1Parser;
using SysadminsLV.WPF.Mvvm;

namespace SysadminsLV.Asn1Editor.API.ModelObjects;
class EncodingTypeEntry : ViewModelBase {
    public EncodingTypeEntry(EncodingType encodingType, String displayName, EncodingGroup encodingGroup) {
        EncodingType = encodingType;
        DisplayName = displayName;
        EncodingGroup = encodingGroup;
    }

    public EncodingType EncodingType { get; set; }
    public String DisplayName { get; set; }
    public EncodingGroup EncodingGroup { get; set; }
    public Boolean IsChecked
    {
        get;
        set => SetField(ref field, value);
    }
}
enum EncodingGroup {
    PEM,
    Hexadecimal
}