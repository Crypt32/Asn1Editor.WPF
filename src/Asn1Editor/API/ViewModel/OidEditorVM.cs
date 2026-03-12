using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using SysadminsLV.Asn1Editor.API.Abstractions;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Parser;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.API.ViewModel;
class OidEditorVM : ViewModelBase, IOidEditorVM {
    readonly Regex _regex = new(@"^[0-2]\.([0-9]|[12][0-9]|3[0-9])(\.\d+)*$", RegexOptions.Compiled);
    readonly ObservableCollection<OidDto> _oidList = [];
    readonly IHasAsnDocumentTabs _tabs;
    readonly IOidDbManager _oidMgr;

    String oidValue, friendlyName, searchText;
    Boolean? dialogResult;
    OidDto? selectedItem;
    OidSearchScope searchScope;

    public OidEditorVM(IHasAsnDocumentTabs tabs, IOidDbManager oidMgr) {
        _tabs = tabs;
        _oidMgr = oidMgr;
        ReloadCommand = new RelayCommand(reload);
        SaveCommand = new AsyncCommand(save, canSave);
        ResetCommand = new RelayCommand(reset);
        CloseCommand = new RelayCommand(_ => DialogResult = true);
        RemoveOidCommand = new AsyncCommand(removeOid, canRemoveOid);

        OidView = CollectionViewSource.GetDefaultView(_oidList);
        OidView.SortDescriptions.Add(new SortDescription(nameof(OidDto.Value), ListSortDirection.Ascending));
        OidView.Filter = filterOidList;

        SearchScope = OidSearchScope.UserDefined;
        NodeViewOptions = tabs.NodeViewOptions;
    }
    Boolean filterOidList(Object obj) {
        if (obj is not OidDto entry) {
            return false;
        }

        switch (SearchScope) {
            case OidSearchScope.BuiltIn:
                if (entry.UserDefined) {
                    return false;
                }
                break;
            case OidSearchScope.UserDefined:
                if (!entry.UserDefined) {
                    return false;
                }
                break;
        }
        if (String.IsNullOrEmpty(SearchText)) {
            return true;
        }
        return entry.Value.StartsWith(SearchText)
               || entry.FriendlyName.IndexOf(SearchText, StringComparison.CurrentCultureIgnoreCase) >= 0;
    }

    public ICommand ReloadCommand { get; }
    public IAsyncCommand SaveCommand { get; }
    public IAsyncCommand RemoveOidCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand CloseCommand { get; }
    public NodeViewOptions NodeViewOptions { get; }

    public ICollectionView OidView { get; }

    public Boolean? DialogResult {
        get => dialogResult;
        set {
            dialogResult = value;
            OnPropertyChanged();
        }
    }
    public String OidValue {
        get => oidValue;
        set {
            oidValue = value;
            OnPropertyChanged();
        }
    }
    public String FriendlyName {
        get => friendlyName;
        set {
            friendlyName = value;
            OnPropertyChanged();
        }
    }
    public String SearchText {
        get => searchText;
        set {
            searchText = value;
            OnPropertyChanged();
            OidView.Refresh();
        }
    }
    public OidDto? SelectedItem {
        get => selectedItem;
        set {
            selectedItem = value;
            OnPropertyChanged();
        }
    }
    public OidSearchScope SearchScope {
        get => searchScope;
        set {
            if (searchScope != value) {
                searchScope = value;
                OnPropertyChanged();
                OidView.Refresh();
            }
        }
    }

    void reload(Object o) {
        _oidList.Clear();
        foreach (OidDto oid in OidResolver.GetOidLookup()) {
            _oidList.Add(oid);
        }
    }
    async Task save(Object o, CancellationToken token = default) {
        OidDto oidEntry = OidResolver.GetOidEntry(OidValue);
        if (oidEntry is null) {
            _oidList.Add(new OidDto(OidValue, FriendlyName, true));
        } else {
            oidEntry.SetFriendlyName(friendlyName);
        }
        OidResolver.Add(OidValue, FriendlyName, true);
        if (await _oidMgr.SaveUserLookup()) {
            reset(null);
        }
        OidView.Refresh();
        await _tabs.RefreshTabs(x => x.Value.Tag == (Byte)Asn1Type.OBJECT_IDENTIFIER);
    }
    Boolean canSave(Object o) {
        return !String.IsNullOrWhiteSpace(OidValue) && !String.IsNullOrWhiteSpace(FriendlyName) && _regex.IsMatch(OidValue);
    }
    void reset(Object? o) {
        OidValue = null;
        FriendlyName = null;
    }
    async Task removeOid(Object o, CancellationToken token = default) {
        OidDto backupOid = SelectedItem!;
        OidValue = SelectedItem!.Value;
        FriendlyName = SelectedItem.FriendlyName;
        _oidList.Remove(SelectedItem);
        OidResolver.Remove(OidValue);
        // fall-back
        if (!await _oidMgr.SaveUserLookup()) {
            OidValue = String.Empty;
            FriendlyName = String.Empty;
            OidResolver.Add(backupOid.Value, backupOid.FriendlyName, backupOid.UserDefined);
        } else {
            await _tabs.RefreshTabs(x => x.Value.Tag == (Byte)Asn1Type.OBJECT_IDENTIFIER);
        }
    }
    Boolean canRemoveOid(Object o) {
        return SelectedItem is not null;
    }
}