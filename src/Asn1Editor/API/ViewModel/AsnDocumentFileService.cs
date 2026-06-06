using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.Utils;
using SysadminsLV.Asn1Parser;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

/// <summary>
/// Handles all file I/O operations for ASN.1 document tabs: opening, saving, reloading and drag-dropping files.
/// </summary>
class AsnDocumentFileService {
    readonly IUIMessenger _uiMessenger;
    readonly AsnDocumentHostManager _tabManager;

    public AsnDocumentFileService(IUIMessenger uiMessenger, AsnDocumentHostManager tabManager) {
        _uiMessenger = uiMessenger;
        _tabManager = tabManager;
    }

    #region Open

    /// <summary>
    /// Prompts the user to choose a file and opens it in a tab.
    /// </summary>
    public Task OpenFileAsync() {
        _uiMessenger.TryGetOpenFileName(out String filePath);
        if (String.IsNullOrWhiteSpace(filePath)) {
            return Task.CompletedTask;
        }

        return CreateTabFromFileAsync(filePath);
    }

    /// <summary>
    /// Opens a file from a known path in a tab. Used by drag-drop and external callers.
    /// </summary>
    public Task OpenExistingAsync(String filePath) {
        return CreateTabFromFileAsync(filePath);
    }

    /// <summary>
    /// Handles a drag-drop operation: validates the dropped path and opens the file.
    /// </summary>
    public Task DropFileAsync(String? filePath) {
        if (File.Exists(filePath)) {
            return CreateTabFromFileAsync(filePath);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Decodes raw bytes (already validated as DER) and loads them into an available tab.
    /// Exposed as a delegate to the converter window.
    /// </summary>
    public Task OpenRawAsync(Byte[] rawBytes) {
        var asn = new Asn1Reader(rawBytes);
        try {
            asn.BuildOffsetMap();
            AsnDocumentHostVM tab = _tabManager.GetAvailableTab(out _);
            return tab.GetPrimaryDocument().Decode(rawBytes, false);
        } catch (Exception ex) {
            _uiMessenger.ShowError(ex.Message, "Read Error");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Decodes a Base64-encoded string and loads the result into an available tab.
    /// </summary>
    public async Task OpenRawAsync(String base64String) {
        try {
            await OpenRawAsync(Convert.FromBase64String(base64String));
        } catch (Exception ex) {
            _uiMessenger.ShowError(ex.Message, "Read Error");
        }
    }

    #endregion

    /// <summary>
    /// Reloads the primary document of the currently selected tab from disk.
    /// Prompts the user for confirmation if the document has unsaved changes.
    /// </summary>
    public async Task ReloadActiveDocumentAsync() {
        AsnDocumentHostVM? selectedTab = _tabManager.SelectedTab;
        if (selectedTab is null) {
            return;
        }
        Asn1DocumentVM doc = selectedTab.GetPrimaryDocument();
        if (String.IsNullOrEmpty(doc.Path)) {
            return;
        }
        if (doc.IsModified) {
            Boolean confirmResult = _uiMessenger.YesNo(
                "Reloading will discard all unsaved changes. Do you want to continue?",
                "Confirm Reload");
            if (!confirmResult) {
                return;
            }
        }

        try {
            doc.Reset();
            IEnumerable<Byte> bytes = await FileUtility.FileToBinaryAsync(doc.Path);
            await doc.Decode(bytes, true);
        } catch (Exception ex) {
            _uiMessenger.ShowError(ex.Message, "Reload Error");
        }
    }

    #region Save/Write

    // 'obj' parameter semantics:
    //   null  – save current tab using its existing/default path
    //   "1"   – save current tab with a user-chosen path (Save As)
    //   "2"   – save all tabs (reserved for future use)
    /// <summary>
    /// Entry point for all save commands.
    /// </summary>
    public void SaveFile(String? obj) {
        AsnDocumentHostVM? selectedTab = _tabManager.SelectedTab;
        if (obj is null) {
            WriteFile(selectedTab);
        } else {
            switch (obj) {
                case "1": {
                    if (GetSaveFilePath(out String filePath)) {
                        WriteFile(selectedTab, filePath);
                    }
                    break;
                }
                case "2":
                    // save all tabs — reserved
                    break;
            }
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> when the save / print commands should be enabled.
    /// </summary>
    public Boolean CanSave(Object obj) {
        return _tabManager.SelectedTab?.GetPrimaryDocument().AsnDocContext.RawData.Count > 0;
    }

    /// <summary>
    /// Writes the primary document of <paramref name="tab"/> to disk.
    /// If <paramref name="filePath"/> is omitted the document's existing path is used;
    /// if that is also absent the user is prompted for a save location.
    /// </summary>
    /// <returns><see langword="true"/> on success; <see langword="false"/> if the operation was cancelled or failed.</returns>
    public Boolean WriteFile(AsnDocumentHostVM? tab, String? filePath = null) {
        if (tab is null) {
            return false;
        }
        Asn1DocumentVM doc = tab.GetPrimaryDocument();
        filePath ??= doc.Path;
        if (String.IsNullOrEmpty(filePath) && !GetSaveFilePath(out filePath)) {
            return false;
        }
        try {
            File.WriteAllBytes(filePath, doc.AsnDocContext.RawData.ToArray());
            doc.Path = filePath;
            doc.IsModified = false;
            return true;
        } catch (Exception e) {
            _uiMessenger.ShowError(e.Message, "Save Error");
        }
        return false;
    }

    public Boolean GetSaveFilePath(out String saveFilePath) {
        return _uiMessenger.TryGetSaveFileName(out saveFilePath);
    }

    #endregion

    /// <summary>
    /// Reads a file from disk and decodes it into an available tab.
    /// If a new tab was created but decoding fails the temporary tab is closed.
    /// </summary>
    public async Task CreateTabFromFileAsync(String file) {
        AsnDocumentHostVM tab = _tabManager.GetAvailableTab(out Boolean isNew);
        Asn1DocumentVM doc = tab.GetPrimaryDocument();
        doc.Path = file;
        try {
            IEnumerable<Byte> bytes = await FileUtility.FileToBinaryAsync(file);
            await doc.Decode(bytes, true);
        } catch (Exception ex) {
            _uiMessenger.ShowError(ex.Message, "Read Error");
            if (isNew) {
                _tabManager.RemoveTab(tab);
            }
        }
    }
}