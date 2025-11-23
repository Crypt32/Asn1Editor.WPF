using System;

namespace SysadminsLV.Asn1Editor.Controls;

public class AsnNonEditableValueEditor : AsnValueEditor {
    protected override void OnBinaryValueChanged(Byte[] oldValue, Byte[] newValue) {
        // No editing allowed, do nothing
    }
}
