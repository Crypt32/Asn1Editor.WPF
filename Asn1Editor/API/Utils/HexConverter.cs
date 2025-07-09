using System;
using System.Collections.Generic;
using SysadminsLV.Asn1Parser;

namespace SysadminsLV.Asn1Editor.API.Utils;

static class HexUtility {
    public static String GetHexEditString(Byte[] rawData) {
        return AsnFormatter.BinaryToString(rawData, EncodingType.Hex);
    }
    public static String BinaryToHex(Byte[] rawData) {
        return AsnFormatter.BinaryToString(rawData, EncodingType.Hex, EncodingFormat.NOCR);
    }
    public static Byte[] HexToBinary(String hexString) {
        return AsnFormatter.StringToBinary(hexString, EncodingType.Hex);
    }
    public static IEnumerable<Byte>? AnyToBinary(String anyString) {
        try {
            return AsnFormatter.StringToBinary(anyString, EncodingType.HexAny);
        } catch {
            try {
                return AsnFormatter.StringToBinary(anyString, EncodingType.StringAny);
            } catch {
                try {
                    return AsnFormatter.StringToBinary(anyString, EncodingType.Binary);
                } catch {
                    return null;
                }
            }
        }
    }
}