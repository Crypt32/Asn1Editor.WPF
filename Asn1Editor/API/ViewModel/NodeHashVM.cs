using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.API.ViewModel;
// The idea is plain and simple: instead of creating separate bindings to string properties,
// it makes sense to create a single collection of strings that represent all hashes and then
// bind to the collection by fixed indexes.

class NodeHashVM : ClosableWindowVM {
    readonly IDataSource _dataSource;
    readonly StringBuilder _sb = new(128);
    readonly String[] _algorithms = ["MD5", "SHA1", "SHA256", "SHA384", "SHA512"];

    Boolean isHexChecked, isBase64Checked;

    public NodeHashVM(IDataSource dataSource) {
        for (Int32 i = 0; i < 10; i++) {
            Hashes.Add(String.Empty);
        }
        _dataSource = dataSource;
        NodeViewOptions = dataSource.NodeViewOptions;
        CopyValueCommand = new RelayCommand(copyValue);
        IsHexChecked = true;
        calculateAllHashes();
    }

    public ICommand CopyValueCommand { get; }

    public ObservableCollection<String> Hashes { get; } = [];
    public NodeViewOptions NodeViewOptions { get; }

    public Boolean IsHexChecked {
        get => isHexChecked;
        set {
            isHexChecked = value;
            OnPropertyChanged();
            calculateAllHashes();
        }
    }
    public Boolean IsBase64Checked {
        get => isBase64Checked;
        set {
            isBase64Checked = value;
            OnPropertyChanged();
            calculateAllHashes();
        }
    }

    void calculateAllHashes() {
        Byte[] data = _dataSource.RawData.Skip(_dataSource.SelectedNode!.Offset).Take(_dataSource.SelectedNode.TagLength).ToArray();
        calculateHashes(data, 0);
        data = _dataSource.RawData.Skip(_dataSource.SelectedNode.PayloadStartOffset).Take(_dataSource.SelectedNode.PayloadLength).ToArray();
        calculateHashes(data, _algorithms.Length);

    }
    void calculateHashes(Byte[] data, Int32 shift) {
        for (Int32 index = 0; index < _algorithms.Length; index++) {
            String hashAlg = _algorithms[index];
            _sb.Clear();
            using var hasher = HashAlgorithm.Create(hashAlg);
            if (IsHexChecked) {
                foreach (Byte b in hasher!.ComputeHash(data)) {
                    _sb.Append(b.ToString("x2"));
                }

                Hashes[index + shift] = _sb.ToString();
            } else {
                Hashes[index + shift] = Convert.ToBase64String(hasher!.ComputeHash(data));
            }
        }
    }

    void copyValue(Object o) {
        if (!Int32.TryParse(o?.ToString(), out Int32 index) || index >= Hashes.Count) {
            return;
        }
        Clipboard.SetText(Hashes[index]);
    }
}
