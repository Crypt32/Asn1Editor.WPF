using System;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.ViewModel;
using SysadminsLV.Asn1Editor.Core.Tree;

namespace SysadminsLV.Asn1Editor.API.Abstractions;

public interface IHasAsnDocumentTabs {
    NodeViewOptions NodeViewOptions { get; }
    AsnDocumentHostVM? SelectedTab { get; }

    Task RefreshTabs(Func<AsnTreeNode, Boolean>? filter = null);
}