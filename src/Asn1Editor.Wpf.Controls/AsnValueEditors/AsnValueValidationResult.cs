using System;

namespace SysadminsLV.Asn1Editor.Controls;

/// <summary>
/// Represents the result of ASN.1 value validation, providing information about the validity of the tag,
/// the encoded value if valid, or an error message if invalid.
/// </summary>
public class AsnValueValidationResult {
    AsnValueValidationResult() { }

    /// <summary>
    /// Gets a value indicating whether the ASN.1 value validation was successful.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the validation was successful; otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// When the value is <see langword="true"/>, the <see cref="EncodedValue"/> property contains the valid encoded value,
    /// and the <see cref="ErrorMessage"/> property is <see langword="null"/>. 
    /// When the value is <see langword="false"/>, the <see cref="ErrorMessage"/> property contains the error details,
    /// and the <see cref="EncodedValue"/> property is <see langword="null"/>.
    /// </remarks>
    public Boolean IsValid {
        get;
        private init {
            if (field != value) {
                if (value) {
                    ErrorMessage = null;
                } else {
                    EncodedValue = null;
                }
                field = value;
            }
        }
    }

    /// <summary>
    /// Gets the encoded value of the ASN.1 tag if the validation was successful. Value includes the full TLV (Tag-Length-Value) encoding of the ASN.1 tag.
    /// </summary>
    /// <value>
    /// A byte array containing the encoded value of the ASN.1 tag if <see cref="IsValid"/> is <see langword="true"/>; 
    /// otherwise, <see langword="null"/>.
    /// </value>
    /// <remarks>
    /// This property is populated only when the validation is successful. If the validation fails, this property is 
    /// set to <see langword="null"/> and the <see cref="ErrorMessage"/> property contains the error details.
    /// </remarks>
    public Byte[]? EncodedValue { get; private init; }
    /// <summary>
    /// Gets the error message describing the reason for validation failure, if applicable.
    /// </summary>
    /// <value>
    /// A string containing the error message if <see cref="IsValid"/> is <see langword="false"/>; 
    /// otherwise, <see langword="null"/>.
    /// </value>
    /// <remarks>
    /// This property is populated only when the validation fails. If the validation is successful, 
    /// this property is set to <see langword="null"/> and the <see cref="EncodedValue"/> property contains 
    /// the valid encoded value.
    /// </remarks>
    public String? ErrorMessage { get; private init; }

    /// <summary>
    /// Creates a new instance of <see cref="AsnValueValidationResult"/> representing a successful validation result.
    /// </summary>
    /// <param name="encodedValue">The encoded value that has been successfully validated.</param>
    /// <returns>
    /// An instance of <see cref="AsnValueValidationResult"/> with <see cref="IsValid"/> set to <c>true</c> and the provided <paramref name="encodedValue"/>.
    /// </returns>
    public static AsnValueValidationResult Ok(Byte[] encodedValue) {
        return new AsnValueValidationResult {
            IsValid = true,
            EncodedValue = encodedValue
        };
    }
    /// <summary>
    /// Creates a new instance of <see cref="AsnValueValidationResult"/> representing a failed validation result.
    /// </summary>
    /// <param name="errorMessage">The error message describing the reason for the validation failure.</param>
    /// <returns>
    /// An instance of <see cref="AsnValueValidationResult"/> with <see cref="IsValid"/> set to <c>false</c> and the provided <paramref name="errorMessage"/>.
    /// </returns>
    public static AsnValueValidationResult Fail(String errorMessage) {
        return new AsnValueValidationResult {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }
}
