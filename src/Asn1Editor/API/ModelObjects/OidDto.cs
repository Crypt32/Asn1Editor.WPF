using System;
using System.Security.Cryptography;

namespace SysadminsLV.Asn1Editor.API.ModelObjects;

/// <summary>
/// Represents Object Identifier wrapper.
/// </summary>
public class OidDto {
    public OidDto(String value, String friendlyName, Boolean userDefined) {
        Value = value;
        FriendlyName = friendlyName;
        UserDefined = userDefined;
    }

    /// <inheritdoc cref="Oid.FriendlyName"/>
    public String FriendlyName { get; private set; }
    /// <inheritdoc cref="Oid.Value"/>
    public String Value { get; }
    /// <summary>
    /// Specifies if the current OID mapping is built-in or user-defined.
    /// </summary>
    public Boolean UserDefined { get; private set; }

    public void SetFriendlyName(String friendlyName) {
        FriendlyName = friendlyName;
        UserDefined = true;
    }
}
