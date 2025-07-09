#nullable enable
using System;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.ViewModel;

namespace SysadminsLV.Asn1Editor.API.Abstractions;

public interface IHasAsnDocumentTabs {
    NodeViewOptions NodeViewOptions { get; }
    Asn1DocumentVM? SelectedTab { get; }

    Task RefreshTabs(Func<Asn1TreeNode, Boolean>? filter = null);
}