#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using SysadminsLV.Asn1Editor.API.Abstractions;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.Utils;
using SysadminsLV.Asn1Editor.API.Utils.ASN;
using SysadminsLV.Asn1Parser;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

class TreeViewCommands : ViewModelBase, ITreeCommands {
    static readonly List<Byte> _excludedTags = Asn1Reader.GetRestrictedTags();
    readonly IWindowFactory _windowFactory;
    readonly IUIMessenger _uiMessenger;
    readonly IHasAsnDocumentTabs _tabs;

    Boolean hasNodeClipboardData;

    public TreeViewCommands(IWindowFactory windowFactory, IHasAsnDocumentTabs appTabs) {
        _windowFactory = windowFactory;
        _uiMessenger = windowFactory.GetUIMessenger();
        _tabs = appTabs;
        SaveNodeCommand = new RelayCommand(saveBinaryNode, ensureNodeSelected);
        ShowNodeTextViewer = new RelayCommand(showNodeTextViewer, ensureNodeSelected);
        ShowNodeInConverter = new RelayCommand(showNodeInConverter, ensureNodeSelected);
        ShowNodeHashCommand = new RelayCommand(showNodeHashes, ensureNodeSelected);
        EditNodeCommand = new RelayCommand(editNodeContent, ensureNodeSelected);
        RegisterOidCommand = new RelayCommand(registerOid, ensureNodeSelected);
        AddNewNodeCommand = new RelayCommand(addNewNode, canAddNewNode);
        DeleteNodeCommand = new RelayCommand(removeNode, ensureNodeSelected);
        CutNodeCommand = new RelayCommand(cutNode, canCutNode);
        CopyNodeCommand = new RelayCommand(copyNode, ensureNodeSelected);
        PasteBeforeCommand = new AsyncCommand(pasteBefore, canPasteBeforeAfter);
        PasteAfterCommand = new AsyncCommand(pasteAfter, canPasteBeforeAfter);
        PasteLastCommand = new AsyncCommand(pasteLast, canPasteLast);
    }

    public ICommand ShowNodeTextViewer { get; }
    public ICommand ShowNodeInConverter { get; }
    public ICommand ShowNodeHashCommand { get; }
    public ICommand EditNodeCommand { get; }
    public ICommand RegisterOidCommand { get; }
    public ICommand SaveNodeCommand { get; }
    public ICommand AddNewNodeCommand { get; }
    public ICommand DeleteNodeCommand { get; }
    public ICommand CutNodeCommand { get; }
    public ICommand CopyNodeCommand { get; }
    public IAsyncCommand PasteBeforeCommand { get; }
    public IAsyncCommand PasteAfterCommand { get; }
    public IAsyncCommand PasteLastCommand { get; }

    public Boolean HasNodeClipboardData {
        get => hasNodeClipboardData;
        set {
            hasNodeClipboardData = value;
            OnPropertyChanged();
        }
    }

    void saveBinaryNode(Object o) {
        if (!_uiMessenger.TryGetSaveFileName(out String filePath)) {
            return;
        }
        isTabSelected(out IDataSource data); // granted to be non-null
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
        isTabSelected(out IDataSource data); // granted to be non-null
        _windowFactory.ShowNodeHashesDialog(data);
    }
    void showNodeInConverter(Object o) {
        isTabSelected(out IDataSource data); // granted to be non-null
        if (data?.SelectedNode is not null) {
            IEnumerable<Byte> nodeData = data.RawData.Skip(data.SelectedNode.Offset).Take(data.SelectedNode.TagLength);

            _windowFactory.ShowConverterWindow(nodeData, null);
        }
    }
    void editNodeContent(Object o) {
        isTabSelected(out IDataSource data); // granted to be non-null
        if (data?.SelectedNode is not null) {
            _windowFactory.ShowNodeContentEditor((NodeEditMode)o);
        }
    }
    void registerOid(Object obj) {
        isTabSelected(out IDataSource data); // granted to be non-null
        if (data?.SelectedNode is not null) {
            Asn1TreeNode node = data.SelectedNode;
            String oidValue = AsnDecoder.GetEditValue(new Asn1Reader(data.RawData.Skip(node.Offset).Take(node.TagLength).ToArray())).TextValue;
            String friendlyName = OidResolver.Resolve(oidValue); // TODO: replace with ResolveFriendlyName
            _windowFactory.ShowOidEditor(new OidDto(oidValue, friendlyName, false));
        }
    }
    void addNewNode(Object o) {
        isTabSelected(out IDataSource data); // granted to be non-null
        Byte[]? nodeRawData = _windowFactory.ShowNewAsnNodeEditor(data);
        //Asn1Lite nodeValue = _windowFactory.ShowNodeContentEditor(NodeEditMode.NewNode);
        if (nodeRawData is null) {
            return;
        }

        Asn1TreeNode node = data.AddNode(nodeRawData);
        if (node.Value is { IsContainer: false, Tag: not ((Byte)Asn1Type.NULL or (Byte)Asn1Type.SEQUENCE or (Byte)Asn1Type.SET) }) {
            EditNodeCommand.Execute(NodeEditMode.Text);
        }
    }
    void removeNode(Object o) {
        isTabSelected(out IDataSource data); // granted to be non-null
        Boolean response = _uiMessenger.YesNo("Do you want to delete the node?\nThis action cannot be undone.", "Delete");
        if (response) {
            data!.RemoveSelectedNode();
        }
    }
    void cutNode(Object o) {
        isTabSelected(out IDataSource data); // granted to be non-null
        copyNodePrivate(data);
        data.RemoveSelectedNode();
    }
    void copyNode(Object o) {
        isTabSelected(out IDataSource data); // granted to be non-null
        copyNodePrivate(data);
    }
    void copyNodePrivate(IDataSource data) {
        ClipboardManager.SetClipboardData(
        data.RawData
                .Skip(data.SelectedNode!.Value.Offset)
                .Take(data.SelectedNode.Value.TagLength)
        );
        HasNodeClipboardData = true;
    }
    Task pasteBefore(Object o, CancellationToken token) {
        isTabSelected(out IDataSource data); // granted to be non-null
        return data!.InsertNode(ClipboardManager.GetClipboardBytes().ToArray(), NodeAddOption.Before);
    }
    Task pasteAfter(Object o, CancellationToken token) {
        isTabSelected(out IDataSource data); // granted to be non-null
        return data!.InsertNode(ClipboardManager.GetClipboardBytes().ToArray(), NodeAddOption.After);
    }
    Task pasteLast(Object o, CancellationToken token) {
        isTabSelected(out IDataSource data); // granted to be non-null
        return data!.InsertNode(ClipboardManager.GetClipboardBytes().ToArray(), NodeAddOption.Last);
    }

    Boolean ensureNodeSelected(Object o) {
        return isTabSelected(out IDataSource data) && data!.SelectedNode is not null;
    }
    Boolean canAddNewNode(Object o) {
        return isTabSelected(out IDataSource data)
               && (data!.Tree.Count == 0 || (data.SelectedNode is not null && !_excludedTags.Contains(data.SelectedNode.Value.Tag)));
    }
    Boolean canCutNode(Object? o) {
        return isTabSelected(out IDataSource data)
               && data!.SelectedNode is { Parent: not null };
    }
    Boolean canPasteBeforeAfter(Object o) {
        return isTabSelected(out IDataSource _) && HasNodeClipboardData && canCutNode(null);
    }
    Boolean canPasteLast(Object o) {
        Boolean preCondition = isTabSelected(out IDataSource data) && HasNodeClipboardData;
        if (!preCondition) {
            return false;
        }

        if (data!.SelectedNode is null) {
            return false;
        }

        return !_excludedTags.Contains(data.SelectedNode.Value.Tag) &&
               String.IsNullOrEmpty(data.SelectedNode.Value.ExplicitValue);
    }
    Boolean isTabSelected(out IDataSource? dataSource) {
        dataSource = null;
        if (_tabs.SelectedTab is not null) {
            Asn1DocumentVM document = _tabs.SelectedTab.GetPrimaryDocument();
            if (document.IsEnabled) {
                dataSource = _tabs.SelectedTab.GetPrimaryDocument().DataSource;

                return true;
            }
        }

        return false;
    }
}