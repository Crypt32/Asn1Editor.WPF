using System;
using System.Security.Cryptography;

namespace SysadminsLV.Asn1Editor.Core;

/// <summary>
/// Defines a contract for resolving object identifiers (OIDs) to their corresponding friendly names
/// or other representations. This interface is intended to provide a mechanism for OID resolution
/// within the application.
/// </summary>
public interface IOidResolver {
    String? ResolveOid(String oidValue);
}

/// <summary>
/// Provides a stub implementation of the IOidResolver interface for resolving object identifier (OID) values.
/// </summary>
/// <remarks>This class is intended for testing or placeholder scenarios where a minimal OID resolution is
/// required. It does not perform validation or lookup beyond basic OID value extraction.</remarks>
class OidResolverStub : IOidResolver {
    public String? ResolveOid(String oidValue) {
        return new Oid(oidValue).Value;
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
    static IOidResolver resolver = new OidResolverStub();
    
    public static IOidResolver Resolver
    {
        get => resolver;
        set => resolver = value ?? new OidResolverStub();
    }
}