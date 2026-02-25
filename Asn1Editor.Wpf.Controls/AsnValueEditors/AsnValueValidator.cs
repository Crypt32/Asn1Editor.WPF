using System;
using SysadminsLV.Asn1Parser;
using SysadminsLV.Asn1Parser.Universal;

namespace SysadminsLV.Asn1Editor.Controls;

class AsnValueValidator(Byte tag) {

    public AsnValueValidationResult Validate(String inputValue) {
        try {
            Byte[] payload = AsnFormatter.StringToBinary(inputValue, EncodingType.Hex);
            return AsnValueValidationResult.Ok(encode(Asn1Utils.Encode(payload, tag)));
        } catch (Exception ex) {
            return AsnValueValidationResult.Fail(ex.Message);
        }
    }

    Byte[] encode(Byte[] inputValue) {
        switch ((Asn1Type)tag) {
            case Asn1Type.BOOLEAN:
                if (inputValue.Length != 1) {
                    throw new ArgumentException("BOOLEAN must be a single octet.");
                }
                return new Asn1Boolean(inputValue).GetRawData();
            case Asn1Type.INTEGER:
                return new Asn1Integer(inputValue).GetRawData();
            case Asn1Type.OCTET_STRING:
                return new Asn1OctetString(inputValue, false).GetRawData();
            case Asn1Type.NULL:
                return new Asn1Null().GetRawData();
            case Asn1Type.OBJECT_IDENTIFIER:
                return new Asn1ObjectIdentifier(inputValue).GetRawData();
            case Asn1Type.RELATIVE_OID:
                return new Asn1RelativeOid(inputValue).GetRawData();
            case Asn1Type.ENUMERATED:
                return new Asn1Enumerated(inputValue).GetRawData();
            case Asn1Type.UTF8String:
                return new Asn1UTF8String(inputValue).GetRawData();
            case Asn1Type.NumericString:
                return new Asn1NumericString(inputValue).GetRawData();
            case Asn1Type.TeletexString:
                return new Asn1TeletexString(inputValue).GetRawData();
            case Asn1Type.VideotexString:
                return Asn1Utils.Encode(inputValue, tag);
            case Asn1Type.PrintableString:
                return new Asn1PrintableString(inputValue).GetRawData();
            case Asn1Type.IA5String:
                return new Asn1IA5String(inputValue).GetRawData();
            case Asn1Type.UTCTime:
                return new Asn1UtcTime(inputValue).GetRawData();
            case Asn1Type.GeneralizedTime:
                return new Asn1GeneralizedTime(inputValue).GetRawData();
            case Asn1Type.VisibleString:
                return new Asn1VisibleString(inputValue).GetRawData();
            case Asn1Type.UniversalString:
                return new Asn1UniversalString(inputValue).GetRawData();
            case Asn1Type.BMPString:
                return new Asn1BMPString(inputValue).GetRawData();
            default:
                return inputValue;
        }
    }
}