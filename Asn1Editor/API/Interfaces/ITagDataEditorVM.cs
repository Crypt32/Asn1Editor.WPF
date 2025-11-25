using SysadminsLV.Asn1Editor.API.Utils;
using SysadminsLV.Asn1Editor.Core.Tree;

namespace SysadminsLV.Asn1Editor.API.Interfaces; 

public interface ITagDataEditorVM {
    AsnNodeValue Node { get; }
    void SetBinding(NodeEditMode editMode);
}