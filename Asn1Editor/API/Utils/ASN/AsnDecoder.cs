using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Parser;
using SysadminsLV.Asn1Parser.Universal;

namespace SysadminsLV.Asn1Editor.API.Utils.ASN;

static class AsnDecoder {
    public static AsnViewValue GetEditValue(Asn1Reader asn) {
        var retValue = new AsnViewValue {
            Options = AsnViewValueOptions.SupportsPrintableText
        };
        switch ((Asn1Type)asn.Tag) {
            case Asn1Type.INTEGER:
                retValue.TextValue = new Asn1Integer(asn.GetTagRawData()).Value.ToString();
                break;
            case Asn1Type.BIT_STRING:
                var bitString = new Asn1BitString(asn);
                retValue.TextValue = HexUtility.GetHexEditString(bitString.Value);
                retValue.UnusedBits = bitString.UnusedBits;
                retValue.Options = AsnViewValueOptions.None;
                break;
            case Asn1Type.OBJECT_IDENTIFIER:
                Oid oid = new Asn1ObjectIdentifier(asn).Value;
                retValue.TextValue = oid.Value;
                break;
            case Asn1Type.RELATIVE_OID:
                retValue.TextValue = DecodeRelativeOid(asn);
                break;
            case Asn1Type.BOOLEAN:
            case Asn1Type.UTCTime:
            case Asn1Type.GeneralizedTime:
            case Asn1Type.UTF8String:
            case Asn1Type.NumericString:
            case Asn1Type.PrintableString:
            case Asn1Type.TeletexString:
            case Asn1Type.VideotexString:
            case Asn1Type.IA5String:
            case Asn1Type.VisibleString:
            case Asn1Type.UniversalString:
            case Asn1Type.BMPString:
                retValue.TextValue = GetViewValue(asn);
                break;
            default:
                if (tryGetUtfString(asn.GetPayload(), out String value)) {
                    retValue.TextValue = value;

                    return retValue;
                }

                if ((asn.Tag & (Byte)Asn1Type.TAG_MASK) == 6) {
                    retValue.TextValue = Encoding.UTF8.GetString(asn.GetPayload());
                } else {
                    retValue.TextValue = HexUtility.GetHexEditString(asn.GetPayload());
                    retValue.Options = AsnViewValueOptions.None;
                }
                break;
        }

        return retValue;
    }
    public static String GetViewValue(Asn1Reader asn) {
        if (asn.PayloadLength == 0 && asn.Tag != (Byte)Asn1Type.NULL) { return "NULL"; }
        switch ((Asn1Type)asn.Tag) {
            case Asn1Type.BOOLEAN:
                return new Asn1Boolean(asn).Value.ToString();
            case Asn1Type.INTEGER:
                return DecodeInteger(asn);
            case Asn1Type.BIT_STRING:
                return DecodeBitString(asn);
            case Asn1Type.OCTET_STRING:
                return DecodeOctetString(asn);
            case Asn1Type.NULL:
                return null;
            case Asn1Type.OBJECT_IDENTIFIER:
                return DecodeObjectIdentifier(asn);
            case Asn1Type.RELATIVE_OID:
                return DecodeRelativeOid(asn);
            case Asn1Type.UTF8String:
            case Asn1Type.VisibleString:
                return Encoding.UTF8.GetString(asn.GetPayload());
            // we do not care on encoding enforcement when viewing data
            case Asn1Type.NumericString:
            case Asn1Type.PrintableString:
            case Asn1Type.TeletexString:
            case Asn1Type.VideotexString:
            case Asn1Type.IA5String:
                return Encoding.ASCII.GetString(asn.GetPayload());
            case Asn1Type.UTCTime:
            case Asn1Type.GeneralizedTime:
                return DecodeDateTime(asn);
            case Asn1Type.BMPString:
                return new Asn1BMPString(asn).Value;
            case Asn1Type.UniversalString:
                return new Asn1UniversalString(asn).Value;
            default:
                if (tryGetUtfString(asn.GetPayload(), out String value)) {
                    return value;
                }
                return (asn.Tag & (Byte)Asn1Type.TAG_MASK) == 6
                    ? DecodeUTF8String(asn)
                    : DecodeOctetString(asn);
        }
    }

    static Boolean tryGetUtfString(Byte[] payload, out String? value) {
        value = null;
        if (payload.Any(x => x is < 20 or > 127)) {
            return false;
        }

        value = Encoding.UTF7.GetString(payload);

        return true;
    }

    #region Data type encoders
    static Byte[] EncodeUTCTime(DateTime time) {
        Char[] chars = (time.ToUniversalTime().ToString("yyMMddHHmmss") + "Z").ToCharArray();
        Byte[] rawData = new Byte[chars.Length];
        Int32 index = 0;
        foreach (Char element in chars) {
            rawData[index] = Convert.ToByte(element);
            index++;
        }
        return Asn1Utils.Encode(rawData, (Byte)Asn1Type.UTCTime);
    }
    static Byte[] EncodeGeneralizedTime(DateTime time) {
        Char[] chars = (time.ToUniversalTime().ToString("yyyyMMddHHmmss") + "Z").ToCharArray();
        Byte[] rawData = new Byte[chars.Length];
        Int32 index = 0;
        foreach (Char element in chars) {
            rawData[index] = Convert.ToByte(element);
            index++;
        }
        return Asn1Utils.Encode(rawData, (Byte)Asn1Type.GeneralizedTime);
    }

    public static Byte[] EncodeHex(Byte tag, String hexString, Byte unusedBits) {
        Byte[] hexBytes = AsnFormatter.StringToBinary(hexString, EncodingType.Hex);
        return Asn1Utils.Encode(hexBytes, tag);
    }
    public static Byte[] EncodeGeneric(Byte tag, String value, Byte unusedBits) {
        switch ((Asn1Type)tag) {
            case Asn1Type.BOOLEAN:
                return new Asn1Boolean(Boolean.Parse(value)).GetRawData();
            case Asn1Type.INTEGER:
                return new Asn1Integer(BigInteger.Parse(value)).GetRawData();
            case Asn1Type.BIT_STRING:
                return new Asn1BitString(HexUtility.HexToBinary(value), unusedBits).GetRawData();
            case Asn1Type.OCTET_STRING:
                return new Asn1OctetString(HexUtility.HexToBinary(value), false).GetRawData();
            case Asn1Type.NULL:
                return new Asn1Null().GetRawData();
            case Asn1Type.OBJECT_IDENTIFIER:
                return new Asn1ObjectIdentifier(value).GetRawData();
            case Asn1Type.RELATIVE_OID:
                return new Asn1RelativeOid(value).GetRawData();
            case Asn1Type.ENUMERATED:
                return new Asn1Enumerated(UInt64.Parse(value)).GetRawData();
            case Asn1Type.UTF8String:
                return new Asn1UTF8String(value).GetRawData();
            case Asn1Type.NumericString:
                return new Asn1NumericString(value).GetRawData();
            case Asn1Type.TeletexString:
                return new Asn1TeletexString(value).GetRawData();
            case Asn1Type.VideotexString:
                return Asn1Utils.Encode(Encoding.ASCII.GetBytes(value), tag);
            case Asn1Type.PrintableString:
                return new Asn1PrintableString(value).GetRawData();
            case Asn1Type.IA5String:
                return new Asn1IA5String(value).GetRawData();
            case Asn1Type.UTCTime:
                return EncodeUTCTime(DateTime.ParseExact(value, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture));
            case Asn1Type.GeneralizedTime:
                return EncodeGeneralizedTime(DateTime.ParseExact(value, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture));
            //case (Byte)ASN1Tags.GraphicString
            case Asn1Type.VisibleString:
                return new Asn1VisibleString(value).GetRawData();
            case Asn1Type.GeneralString:
                return Asn1Utils.Encode(Encoding.UTF8.GetBytes(value), tag);
            case Asn1Type.UniversalString:
                return new Asn1UniversalString(value).GetRawData();
            case Asn1Type.BMPString:
                return new Asn1BMPString(value).GetRawData();
            default:
                return Asn1Utils.Encode(Encoding.UTF8.GetBytes(value), tag);
        }
    }
    #endregion
    #region Data type robust decoders
    static String DecodeInteger(Asn1Reader asn) {
        return new BigInteger(asn.GetPayload().Reverse().ToArray()).ToString();
    }
    static String DecodeBitString(Asn1Reader asn) {
        if (asn.PayloadLength == 1) {
            return $"Unused bits: {asn[asn.PayloadStartOffset]} : NULL";
        }
        return String.Format(
            "Unused bits: {0} : {1}",
            asn[asn.PayloadStartOffset],
            AsnFormatter.BinaryToString(
                asn.GetRawData(),
                EncodingType.HexRaw,
                EncodingFormat.NOCRLF,
                asn.PayloadStartOffset + 1,
                asn.PayloadLength - 1)
        );
    }
    static String DecodeOctetString(Asn1Reader asn) {
        return AsnFormatter.BinaryToString(
            asn.GetRawData(),
            EncodingType.HexRaw,
            EncodingFormat.NOCRLF, asn.PayloadStartOffset, asn.PayloadLength);
    }
    static String DecodeObjectIdentifier(Asn1Reader asn) {
        Oid oid = new Asn1ObjectIdentifier(asn).Value;
        String friendlyName = OidResolver.ResolveOid(oid.Value);
        if (friendlyName is null) {
            return oid.Value;
        }

        return $"{friendlyName} ({oid.Value})";
    }
    static String DecodeRelativeOid(Asn1Reader asn) {
        return ((Asn1RelativeOid)asn.GetTagObject()).Value;
    }
    static String DecodeUTF8String(Asn1Reader asn) {
        return Encoding.UTF8.GetString(asn.GetRawData(), asn.PayloadStartOffset, asn.PayloadLength);
    }
    static String DecodeDateTime(Asn1Reader asn) {
        DateTime dt = Asn1DateTime.DecodeAnyDateTime(asn).Value;
        return dt.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
    }
    #endregion
}