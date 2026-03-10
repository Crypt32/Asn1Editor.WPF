using System;
using System.Linq;
using System.Windows.Input;
using SysadminsLV.Asn1Editor.API.Abstractions;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.Utils;
using SysadminsLV.Asn1Editor.Core.Tree;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

/// <summary>
/// View model for the refactored ASN.1 value editor dialog.
/// </summary>
class AsnValueEditorVM : ViewModelBase, IAsnValueEditorVM {
    readonly IUIMessenger _uiMessenger;
    readonly IAsn1DocumentContext _data;

    public AsnValueEditorVM(IHasAsnDocumentTabs appTabs, IUIMessenger uiMessenger) {
        _data = appTabs.SelectedTab!.GetPrimaryDocument().AsnDocContext;
        _uiMessenger = uiMessenger;
        NodeViewOptions = appTabs.NodeViewOptions;
        OkCommand = new RelayCommand(submitValues, canSubmit);
        CloseCommand = new RelayCommand(close);
        TagDetails = String.Empty;
    }

    public ICommand OkCommand { get; }
    public ICommand CloseCommand { get; }
    public NodeViewOptions NodeViewOptions { get; }
    public AsnTreeNode Node => _data.SelectedNode!;

    /// <summary>
    /// Gets the editor context that drives control selection and data binding.
    /// </summary>
    public AsnValueEditorContext? EditorContext {
        get;
        private set {
            field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets a value forcing hex mode for all editor types.
    /// </summary>
    public Boolean ForceHexMode {
        get;
        set {
            if (field != value) {
                field = value;
                OnPropertyChanged();
                RefreshEditorContext();
            }
        }
    }

    /// <summary>
    /// Gets the formatted node metadata displayed at the top.
    /// </summary>
    public String TagDetails {
        get;
        private set {
            field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the dialog result for window closing.
    /// </summary>
    public Boolean? DialogResult {
        get;
        set {
            field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Initializes the editor with the selected node data.
    /// Called by SetBinding method from ITagDataEditorVM interface.
    /// </summary>
    public void SetBinding(NodeEditMode editMode) {
        AsnTreeNode node = _data.SelectedNode!;
        TagDetails = node.Value.GetFormattedMetadata();

        // Extract payload bytes (without tag and length)
        Byte[] tagRawData = _data.RawData
            .Skip(node.Value.Offset)
            .Take(node.Value.TagLength)
            .ToArray();

        EditorContext = new AsnValueEditorContext(node, tagRawData, ForceHexMode);
    }

    /// <summary>
    /// Refreshes the editor context when ForceHexMode changes.
    /// </summary>
    void RefreshEditorContext() {
        if (EditorContext is null) {
            return;
        }

        EditorContext = new AsnValueEditorContext(EditorContext.Node, EditorContext.OriginalEncodedValue, ForceHexMode);
    }

    Boolean canSubmit(Object? obj) {
        return true;
        //return EditorContext is { Result.IsValid: true, HasChanges: true };
    }

    void submitValues(Object obj) {
        // some guard checks to ensure we set up everything correctly. In theory, these should never trigger, but better safe than sorry.
        if (EditorContext?.ValidateCommand is null) {
            _uiMessenger.ShowError("Something is off. Validation command is not provided.");

            return;
        }
        EditorContext.ValidateCommand.Execute(null);
        if (EditorContext.Result is null) {
            _uiMessenger.ShowError("Something is off. Control didn't provide any result.");

            return;
        }

        switch (EditorContext.Result.IsValid) {
            case true when EditorContext.HasChanges:
                // we have some changes, and they look valid, try to update the node
                try {
                    _data.UpdateNode(_data.SelectedNode!, EditorContext.Result.EncodedValue);
                    DialogResult = true;
                } catch (Exception ex) {
                    _uiMessenger.ShowError($"Failed to update node: {ex.Message}", "Encoding Error");
                }

                break;
            case true when !EditorContext.HasChanges:
                // no changes, just close the dialog
                DialogResult = true;
                break;
        }
    }

    void close(Object o) {
        DialogResult = false;
    }
}