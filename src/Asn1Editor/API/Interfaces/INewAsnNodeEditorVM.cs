using System;

namespace SysadminsLV.Asn1Editor.API.Interfaces;

public interface INewAsnNodeEditorVM {
    /// <summary>
    /// Gets new ASN node raw data.
    /// </summary>
    /// <returns>Node encoded raw data.</returns>
    Byte[]? GetAsnNode();
}