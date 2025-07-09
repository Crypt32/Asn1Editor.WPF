#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.Abstractions;
using SysadminsLV.Asn1Editor.API.ModelObjects;

namespace SysadminsLV.Asn1Editor.API;

interface IOidDbManager {
    void ReloadLookup();
    Task<Boolean> SaveUserLookup();
}
class OidDbManager(IUIMessenger uiMessenger) : IOidDbManager {

    const String oidFileName = "OID.txt";
    public String[] OidLookupLocations { get; set; } = [];

    public void ReloadLookup() {
        OidResolver.Reset();
        readOids();
    }
    public async Task<Boolean> SaveUserLookup() {
        try {
            String path = Path.Combine(OidLookupLocations[1], oidFileName);
            using var stream = new StreamWriter(path);
            foreach (OidDto oid in OidResolver.GetOidLookup().Where(x => x.UserDefined)) {
                await stream.WriteLineAsync(oid.Value + "," + oid.FriendlyName);
            }
        } catch (Exception ex) {
            App.Write(ex);
            uiMessenger.ShowError($"User OID lookup save failed:\n{ex.Message}");

            return false;
        }

        return true;
    }
    void readOids() {
        for (Int32 i = 0; i < OidLookupLocations.Length; i++) {
            Boolean userKey = Convert.ToBoolean(i);
            String oidFolderLocation = OidLookupLocations[i];
            String filePath = Path.Combine(oidFolderLocation, oidFileName);
            readOidMapFromFile(filePath, userKey);
        }
    }
    static void readOidMapFromFile(String filePath, Boolean userKey) {
        if (!File.Exists(filePath)) {
            return;
        }
        String[] strings = File.ReadAllLines(filePath);
        foreach (String[] tokens in strings.Select(str => str.Split([','], StringSplitOptions.RemoveEmptyEntries))) {
            if (tokens.Length != 2) {
                continue;
            }
            try {
                OidResolver.Add(tokens[0], tokens[1].Trim(), userKey);
            } catch { }
        }
    }
}
/// <summary>
/// Represents object identifier (OID) resolver helper class.
/// </summary>
public static class OidResolver {
    static readonly Dictionary<String, OidDto> _oidLookup = new();
    static readonly Dictionary<String, OidDto> _nameLookup = new();

    /// <summary>
    /// Attempts to resolve OID value to friendly name.
    /// </summary>
    /// <param name="oidValue">OID value to resolve.</param>
    /// <returns>Resolved friendly name or null if no mapping defined.</returns>
    public static String? ResolveOid(String? oidValue) {
        oidValue ??= String.Empty;
        if (_oidLookup.TryGetValue(oidValue, out OidDto resolvedOid)) {
            return resolvedOid.FriendlyName;
        }

        return new Oid(oidValue).FriendlyName;
    }
    /// <summary>
    /// Resolves OID friendly name to OID value.
    /// </summary>
    /// <param name="friendlyName">OID friendly name.</param>
    /// <returns>Resolved OID value or null if no mapping defined.</returns>
    public static String? ResolveFriendlyName(String? friendlyName) {
        friendlyName ??= String.Empty;
        if (_nameLookup.TryGetValue(friendlyName, out OidDto resolvedOid)) {
            return resolvedOid.Value;
        }

        var oid = new Oid(friendlyName);
        if (friendlyName.Equals(oid.Value)) {
            return null;
        }

        return oid.Value;
    }
    /// <summary>
    /// Resolves OID value to friendly name or friendly name to OID value.
    /// </summary>
    /// <param name="oidValueOrFriendlyName">OID value or friendly name.</param>
    /// <returns>Resolved value or null if no mapping defined for passed OID string.</returns>
    public static String? Resolve(String oidValueOrFriendlyName) {
        String resolvedOid = ResolveOid(oidValueOrFriendlyName);
        if (resolvedOid is null) {
            return ResolveFriendlyName(oidValueOrFriendlyName);
        }

        return resolvedOid;
    }
    /// <summary>
    /// Adds new OID mapping to internal lookup dictionary.
    /// OID mapping will be overwritten if specified OID value was already added to dictionary.
    /// </summary>
    /// <param name="oidValue">OID value in a digit-dot form.</param>
    /// <param name="friendlyName">A friendly name for OID value.</param>
    /// <param name="userKey">Specifies if OID mapping is from user dictionary.</param>
    /// <exception cref="ArgumentNullException">
    ///     Either, <strong>oidValue</strong> or <strong>friendlyName</strong> parameters are null, empty or whitespace sequences.
    /// </exception>
    public static void Add(String oidValue, String friendlyName, Boolean userKey) {
        if (String.IsNullOrWhiteSpace(oidValue)) {
            throw new ArgumentNullException(nameof(oidValue));
        }
        if (String.IsNullOrWhiteSpace(friendlyName)) {
            throw new ArgumentNullException(nameof(friendlyName));
        }
        var oid = new OidDto(oidValue, friendlyName, userKey);
        _oidLookup[oidValue] = oid;
        _nameLookup[friendlyName] = oid;
    }
    public static void Remove(String oidValue) {
        if (_oidLookup.TryGetValue(oidValue, out OidDto oid)) {
            _oidLookup.Remove(oidValue);
            _nameLookup.Remove(oid.FriendlyName);
        }
    }
    public static IEnumerable<OidDto> GetOidLookup() {
        return _oidLookup.Values.ToList();
    }
    public static OidDto? GetOidEntry(String oidValue) {
        _oidLookup.TryGetValue(oidValue, out OidDto? value);

        return value;
    }
    public static void Reset() {
        _nameLookup.Clear();
        _oidLookup.Clear();
    }
}
