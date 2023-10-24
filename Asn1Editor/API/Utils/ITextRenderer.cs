﻿using System;

namespace SysadminsLV.Asn1Editor.API.Utils;

/// <summary>
/// Represents decoded ASN.1 binary dump.
/// </summary>
public interface ITextRenderer {
    /// <summary>
    /// Generates decoded ASN.1 binary dump.
    /// </summary>
    /// <param name="textWidth">Max line width in characters. The line must be wrapped if it exceeds this limit.</param>
    /// <returns></returns>
    String RenderText(Int32 textWidth);
}