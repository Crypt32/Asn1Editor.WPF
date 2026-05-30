using System;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.ViewModel;
using SysadminsLV.Asn1Editor.Core.Tree;

namespace SysadminsLV.Asn1Editor.API.Interfaces;

public interface IHasAsnDocumentTabs {
    AsnDocumentHostVM? SelectedTab { get; }

    Task RefreshTabs(Func<AsnTreeNode, Boolean>? filter = null);
}