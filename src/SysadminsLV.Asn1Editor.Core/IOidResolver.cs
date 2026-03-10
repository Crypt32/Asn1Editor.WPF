using System;
using System.Security.Cryptography;

namespace SysadminsLV.Asn1Editor.Core;

/// <summary>
/// Defines a contract for resolving object identifiers (OIDs) to their corresponding friendly names
/// or other representations. This interface is intended to provide a mechanism for OID resolution
/// within the application.
/// </summary>
public interface IOidResolver {
    /// <summary>
    /// Resolves the friendly name associated with a given object identifier (OID) value.
    /// </summary>
    /// <param name="oidValue">
    /// The OID value to resolve. This should be a valid object identifier string.
    /// </param>
    /// <returns>
    /// A string representing the friendly name associated with the specified OID value, 
    /// or <see langword="null"/> if no friendly name is found.
    /// </returns>
    String? ResolveFriendlyName(String oidValue);
    /// <summary>
    /// Resolves the object identifier (OID) associated with a given friendly name.
    /// </summary>
    /// <param name="friendlyName">
    /// The friendly name to resolve. This should be a valid and recognizable friendly name
    /// corresponding to an OID.
    /// </param>
    /// <returns>
    /// A string representing the OID associated with the specified friendly name, 
    /// or <see langword="null"/> if no OID is found.
    /// </returns>
    String? ResolveOid(String friendlyName);
}

/// <summary>
/// Provides a stub implementation of the IOidResolver interface for resolving object identifier (OID) values.
/// </summary>
/// <remarks>This class is intended for testing or placeholder scenarios where a minimal OID resolution is
/// required. It does not perform validation or lookup beyond basic OID value extraction.</remarks>
class OidResolverStub : IOidResolver {
    public String? ResolveFriendlyName(String oidValue) {
        return new Oid(oidValue).Value;
    }
    public String? ResolveOid(String friendlyName) {
        return new Oid(friendlyName).FriendlyName;
    }
}

/// <summary>
/// Provides services for resolving object identifiers (OIDs) within the application.
/// </summary>
/// <remarks>
/// This static class acts as a central point for OID resolution by exposing a configurable
/// <see cref="IOidResolver"/> instance. By default, it uses a stub implementation, but it can
/// be replaced with a custom resolver to support advanced OID resolution scenarios.
/// </remarks>
public static class OidServices {
    public static IOidResolver Resolver { get; set; } = new OidResolverStub();
}