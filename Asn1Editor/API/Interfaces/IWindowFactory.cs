#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.Abstractions;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.Utils;
using SysadminsLV.Asn1Editor.Core.Tree;

namespace SysadminsLV.Asn1Editor.API.Interfaces;

interface IWindowFactory {
    IUIMessenger GetUIMessenger();
    void ShowLicenseDialog();
    void ShowAboutDialog();
    AsnNodeValue ShowNodeContentEditor(NodeEditMode editMode);
    void ShowNodeTextViewer();
    void ShowConverterWindow(IEnumerable<Byte> data, Func<Byte[], Task>? action);
    void ShowOidEditor(OidDto? oidValue = null);
    Byte[]? ShowNewAsnNodeEditor(IAsn1DocumentContext asnDocContext);
    void ShowNodeHashesDialog(IAsn1DocumentContext asnDocContext);
}