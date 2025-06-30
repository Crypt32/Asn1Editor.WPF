#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.Abstractions;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.Utils;

namespace SysadminsLV.Asn1Editor.API.Interfaces;

interface IWindowFactory {
    IUIMessenger GetUIMessenger();
    void ShowLicenseDialog();
    void ShowAboutDialog();
    Asn1Lite ShowNodeContentEditor(NodeEditMode editMode);
    void ShowNodeTextViewer();
    void ShowConverterWindow(IEnumerable<Byte> data, Func<Byte[], Task>? action);
    void ShowOidEditor(OidDto? oidValue = null);
    Byte[]? ShowNewAsnNodeEditor(IDataSource dataSource);
    void ShowNodeHashesDialog(IDataSource dataSource);
}