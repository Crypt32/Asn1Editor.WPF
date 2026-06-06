using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.Utils;
using SysadminsLV.Asn1Editor.Controls;
using SysadminsLV.Asn1Editor.Core.ASN;
using SysadminsLV.Asn1Editor.Core.Tree;
using SysadminsLV.Asn1Parser;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

class TreeViewCommands : ViewModelBase, ITreeCommands {
    static readonly List<Byte> _excludedTags = Asn1Reader.GetRestrictedTags();
    readonly IWindowFactory _windowFactory;
    readonly IUIMessenger _uiMessenger;
    readonly IHasAsnDocumentTabs _tabs;

    public TreeViewCommands(IWindowFactory windowFactory, IHasAsnDocumentTabs appTabs, IUIMessenger uiMessenger) {
        _windowFactory = windowFactory;
        _uiMessenger = uiMessenger;
        _tabs = appTabs;
        initializeTreeCommands();
    }

    public CommandBindingCollection Bindings { get; } = [];

    public Boolean HasNodeClipboardData {
        get;
        set {
            field = value;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    void initializeTreeCommands() {
        Bindings.Add(new CommandBinding(
            AsnTreeViewCommands.ShowNodeTextViewer,
            (_, e) => showNodeTextViewer(e.Parameter),
            (_, e) => e.CanExecute = ensureNodeSelected(e.Parameter)));
        Bindings.Add(new CommandBinding(
            AsnTreeViewCommands.ShowNodeHashCommand,
            (_, e) => showNodeHashes(e.Parameter),
            (_, e) => e.CanExecute = ensureNodeSelected(e.Parameter)));
        Bindings.Add(new CommandBinding(
            AsnTreeViewCommands.ShowNodeInConverter,
            (_, e) => showNodeInConverter(e.Parameter),
            (_, e) => e.CanExecute = ensureNodeSelected(e.Parameter)));
        Bindings.Add(new CommandBinding(
            AsnTreeViewCommands.SaveNodeCommand,
            (_, e) => saveBinaryNode(e.Parameter),
            (_, e) => e.CanExecute = ensureNodeSelected(e.Parameter)));
        Bindings.Add(new CommandBinding(
            AsnTreeViewCommands.EditNodeCommand,
            (_, e) => editNodeContent(e.Parameter),
            (_, e) => e.CanExecute = ensureNodeSelected(e.Parameter)));
        Bindings.Add(new CommandBinding(
            AsnTreeViewCommands.RegisterOidCommand,
            (_, e) => registerOid(e.Parameter),
            (_, e) => e.CanExecute = ensureNodeSelected(e.Parameter)));
        Bindings.Add(new CommandBinding(
            AsnTreeViewCommands.AddNewNodeCommand,
            async (_, e) => await addNewNode(e.Parameter, CancellationToken.None),
            (_, e) => e.CanExecute = canAddNewNode(e.Parameter)));
        Bindings.Add(new CommandBinding(
            AsnTreeViewCommands.DeleteNodeCommand,
            (_, e) => removeNode(e.Parameter),
            (_, e) => e.CanExecute = ensureNodeSelected(e.Parameter)));
        Bindings.Add(new CommandBinding(
            AsnTreeViewCommands.CutNodeCommand,
            (_, e) => cutNode(e.Parameter),
            (_, e) => e.CanExecute = canCutNode(e.Parameter)));
        Bindings.Add(new CommandBinding(
            AsnTreeViewCommands.CopyNodeCommand,
            (_, e) => copyNode(e.Parameter),
            (_, e) => e.CanExecute = ensureNodeSelected(e.Parameter)));

        Bindings.Add(new CommandBinding(
            AsnTreeViewCommands.PasteBeforeCommand,
            async (_, e) => await pasteBefore(e.Parameter, CancellationToken.None),
            (_, e) => e.CanExecute = canPasteBeforeAfter(e.Parameter)));
        Bindings.Add(new CommandBinding(
            AsnTreeViewCommands.PasteAfterCommand,
            async (_, e) => await pasteAfter(e.Parameter, CancellationToken.None),
            (_, e) => e.CanExecute = canPasteBeforeAfter(e.Parameter)));
        Bindings.Add(new CommandBinding(
            AsnTreeViewCommands.PasteLastCommand,
            async (_, e) => await pasteLast(e.Parameter, CancellationToken.None),
            (_, e) => e.CanExecute = canPasteLast(e.Parameter)));
    }

    void saveBinaryNode(Object o) {
        if (!_uiMessenger.TryGetSaveFileName(out String filePath)) {
            return;
        }
        isTabSelected(out IAsn1DocumentContext data); // granted to be non-null
        try {
            File.WriteAllBytes(filePath, data!.RawData.Skip(data.SelectedNode!.Value.Offset).Take(data.SelectedNode.Value.TagLength).ToArray());
        } catch (Exception e) {
            _uiMessenger.ShowError(e.Message, "Save Error");
        }
    }
    void showNodeTextViewer(Object o) {
        _windowFactory.ShowNodeTextViewer();
    }
    void showNodeHashes(Object o) {
        isTabSelected(out IAsn1DocumentContext data); // granted to be non-null
        _windowFactory.ShowNodeHashesDialog(data);
    }
    void showNodeInConverter(Object o) {
        isTabSelected(out IAsn1DocumentContext data); // granted to be non-null
        if (data?.SelectedNode is not null) {
            IEnumerable<Byte> nodeData = data.RawData.Skip(data.SelectedNode.Value.Offset).Take(data.SelectedNode.Value.TagLength);

            _windowFactory.ShowConverterWindow(nodeData, null);
        }
    }
    void editNodeContent(Object o) {
        isTabSelected(out IAsn1DocumentContext data); // granted to be non-null
        if (data?.SelectedNode is not null) {
            _windowFactory.ShowNodeContentEditor(o is NodeEditMode mode ? mode : NodeEditMode.Text);
        }
    }
    void registerOid(Object obj) {
        isTabSelected(out IAsn1DocumentContext data); // granted to be non-null
        if (data?.SelectedNode is not null) {
            AsnTreeNode node = data.SelectedNode;
            String oidValue = AsnDecoder.GetEditValue(new Asn1Reader(data.RawData.Skip(node.Value.Offset).Take(node.Value.TagLength).ToArray())).TextValue;
            String friendlyName = OidResolver.Resolve(oidValue); // TODO: replace with ResolveFriendlyName
            _windowFactory.ShowOidEditor(new OidDto(oidValue, friendlyName, false));
        }
    }
    async Task addNewNode(Object o, CancellationToken ct) {
        isTabSelected(out IAsn1DocumentContext data); // granted to be non-null
        Byte[]? nodeRawData = _windowFactory.ShowNewAsnNodeEditor(data);
        if (nodeRawData is null) {
            return;
        }

        AsnTreeNode node = await data.AddNode(nodeRawData, data.SelectedNode);
        data.SelectedNode = node;
        if (node.Value is { IsContainer: false, Tag: not ((Byte)Asn1Type.NULL or (Byte)Asn1Type.SEQUENCE or (Byte)Asn1Type.SET) }) {
            editNodeContent(NodeEditMode.Text);
        }
    }
    void removeNode(Object o) {
        isTabSelected(out IAsn1DocumentContext data); // granted to be non-null
        Boolean response = _uiMessenger.YesNo("Do you want to delete the node?\nThis action cannot be undone.", "Delete");
        if (response) {
            data!.RemoveNode(data.SelectedNode);
        }
    }
    void cutNode(Object o) {
        isTabSelected(out IAsn1DocumentContext data); // granted to be non-null
        copyNodePrivate(data);
        data.RemoveNode(data.SelectedNode);
    }
    void copyNode(Object o) {
        isTabSelected(out IAsn1DocumentContext data); // granted to be non-null
        copyNodePrivate(data);
    }
    void copyNodePrivate(IAsn1DocumentContext data) {
        ClipboardManager.SetClipboardData(
        data.RawData
                .Skip(data.SelectedNode!.Value.Offset)
                .Take(data.SelectedNode.Value.TagLength)
        );
        HasNodeClipboardData = true;
    }
    Task pasteBefore(Object o, CancellationToken token) {
        isTabSelected(out IAsn1DocumentContext data); // granted to be non-null
        return data!.InsertNode(data.SelectedNode, NodeAddOption.Before, ClipboardManager.GetClipboardBytes().ToArray());
    }
    Task pasteAfter(Object o, CancellationToken token) {
        isTabSelected(out IAsn1DocumentContext data); // granted to be non-null
        return data!.InsertNode(data.SelectedNode, NodeAddOption.After, ClipboardManager.GetClipboardBytes().ToArray());
    }
    Task pasteLast(Object o, CancellationToken token) {
        isTabSelected(out IAsn1DocumentContext data); // granted to be non-null
        return data!.InsertNode(data.SelectedNode, NodeAddOption.Last, ClipboardManager.GetClipboardBytes().ToArray());
    }

    Boolean ensureNodeSelected(Object o) {
        return isTabSelected(out IAsn1DocumentContext data) && data!.SelectedNode is not null;
    }
    Boolean canAddNewNode(Object o) {
        return isTabSelected(out IAsn1DocumentContext data)
               && (data!.Tree.Count == 0 || (data.SelectedNode is not null && !_excludedTags.Contains(data.SelectedNode.Value.Tag)));
    }
    Boolean canCutNode(Object? o) {
        return isTabSelected(out IAsn1DocumentContext data)
               && data!.SelectedNode is { Parent: not null };
    }
    Boolean canPasteBeforeAfter(Object o) {
        return isTabSelected(out IAsn1DocumentContext _) && HasNodeClipboardData && canCutNode(null);
    }
    Boolean canPasteLast(Object o) {
        Boolean preCondition = isTabSelected(out IAsn1DocumentContext data) && HasNodeClipboardData;
        if (!preCondition) {
            return false;
        }

        if (data!.SelectedNode is null) {
            return false;
        }

        return !_excludedTags.Contains(data.SelectedNode.Value.Tag) &&
               String.IsNullOrEmpty(data.SelectedNode.Value.ExplicitValue);
    }
    Boolean isTabSelected(out IAsn1DocumentContext? dataSource) {
        dataSource = null;
        if (_tabs.SelectedTab is not null) {
            Asn1DocumentVM document = _tabs.SelectedTab.GetPrimaryDocument();
            if (document.IsEnabled) {
                dataSource = _tabs.SelectedTab.GetPrimaryDocument().AsnDocContext;

                return true;
            }
        }

        return false;
    }
}