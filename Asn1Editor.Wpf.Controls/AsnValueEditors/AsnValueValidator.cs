using System;
using SysadminsLV.Asn1Editor.Core.ASN;
using SysadminsLV.Asn1Parser;

namespace SysadminsLV.Asn1Editor.Controls;

abstract class AsnValueValidator(Byte tag) {
    public Byte Tag { get; } = tag;

    public abstract AsnValueValidationResult Validate(Byte[] inputValue);
    public AsnValueValidationResult Validate(String inputValue) {
        try {
            return AsnValueValidationResult.Ok(AsnDecoder.EncodeGeneric(Tag, inputValue, 0));
        } catch (Exception ex) {
            return AsnValueValidationResult.Fail(ex.Message);
        }
    }

    public static AsnValueValidator Create(Byte tag) {
        if (tag == (Byte)Asn1Type.BOOLEAN) {
            return new AsnBooleanTagValidator(tag);
        }

        return new AsnVariantValidator(tag);
    }
}
sealed class AsnVariantValidator(Byte tag) : AsnValueValidator(tag) {
    public override AsnValueValidationResult Validate(Byte[] inputValue) {
        return AsnValueValidationResult.Ok(inputValue);
    }
}
sealed class AsnBooleanTagValidator(Byte tag) : AsnValueValidator(tag) {
    public override AsnValueValidationResult Validate(Byte[] inputValue) {
        if (inputValue is not { Length: 3 }) {
            return AsnValueValidationResult.Fail("BOOLEAN must be a single octet.");
        }

        return AsnValueValidationResult.Ok(inputValue);
    }
}
