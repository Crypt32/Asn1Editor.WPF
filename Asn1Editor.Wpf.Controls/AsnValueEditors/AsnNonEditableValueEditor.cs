using System;
using System.Collections.Generic;

namespace SysadminsLV.Asn1Editor.Controls;

public class AsnNonEditableValueEditor : AsnValueEditor {
    protected override void OnBinaryValueChanged(IList<Byte> oldValue, IList<Byte> newValue) {
        // No editing allowed, do nothing
    }
}
