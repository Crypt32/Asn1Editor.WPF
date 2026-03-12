namespace SysadminsLV.Asn1Editor.API.ModelObjects;

/// <summary>
/// Defines OID search scopes.
/// </summary>
public enum OidSearchScope {
    /// <summary>
    /// Includes all OID mappings from lookup tables, built-in and user-defined.
    /// </summary>
    All         = 0,
    /// <summary>
    /// Includes only built-in OID mappings.
    /// </summary>
    BuiltIn     = 1,
    /// <summary>
    /// Includes only user-defined OID mappings.
    /// </summary>
    UserDefined = 2
}