using System;
using System.Collections.Generic;

namespace SysadminsLV.Asn1Editor.Core.Extensions; 

static class StringExtensions {
    public static IEnumerable<String> SplitByLength(this String str, Int32 maxLength) {
        for (Int32 index = 0; index < str.Length; index += maxLength) {
            yield return str.Substring(index, Math.Min(maxLength, str.Length - index));
        }
    }
}