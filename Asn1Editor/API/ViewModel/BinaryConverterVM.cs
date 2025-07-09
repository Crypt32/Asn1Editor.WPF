using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using SysadminsLV.Asn1Editor.API.Abstractions;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.Utils;
using SysadminsLV.Asn1Parser;
using SysadminsLV.WPF.OfficeTheme.Toolkit.CLR;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.API.ViewModel;

class BinaryConverterVM : AsyncViewModel {
    readonly IUIMessenger _uiMessenger;
    readonly String master = new('0', 78);
    readonly Func<Byte[], Task>? _action;
    Double width;
    String? text, path;
    EncodingTypeEntry? selectedEncoding;
    Boolean canCheck;

    public BinaryConverterVM(IUIMessenger uiMessenger, NodeViewOptions options, Func<Byte[], Task>? action) {
        _uiMessenger = uiMessenger;
        NodeViewOptions = options;
        _action = action;
        OpenCommand = new AsyncCommand(openFile);
        SaveCommand = new RelayCommand(saveFile, canPrintSave);
        PrintCommand = new RelayCommand(print, canPrintSave);
        ClearCommand = new RelayCommand(clearText);
        ValidateCommand = new AsyncCommand(validateInput, canValidateInput);
        initialize();
        EncodingTypesView = CollectionViewSource.GetDefaultView(EncodingTypes);
        EncodingTypesView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(EncodingTypeEntry.EncodingGroup)));
    }

    public IAsyncCommand OpenCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ClearCommand { get; }
    public IAsyncCommand ValidateCommand { get; }
    public ICommand PrintCommand { get; }

    public NodeViewOptions NodeViewOptions { get; }

    public String? Text {
        get => text;
        set {
            text = value?.Trim();
            OnPropertyChanged();
        }
    }
    public String? Path {
        get => path;
        set {
            path = value;
            OnPropertyChanged();
        }
    }
    public Double TextBoxWidth {
        get => width;
        set {
            width = value;
            OnPropertyChanged();
        }
    }
    public Boolean CanCheck {
        get => canCheck;
        set {
            canCheck = value;
            OnPropertyChanged();
        }
    }
    public EncodingTypeEntry? SelectedEncoding {
        get => selectedEncoding;
        set {
            if (selectedEncoding == value) {
                return;
            }
            selectedEncoding = value;
            if (selectedEncoding is not null) {
                Text = AsnFormatter.BinaryToString(RawData.ToArray(), selectedEncoding.EncodingType);
            }
            OnPropertyChanged();
        }
    }
    public ObservableList<Byte> RawData { get; } = new(true, false);
    // we use this for combobox ItemsSource binding
    public ICollectionView EncodingTypesView { get; }
    // This is observable and public just in case if a list of radiobuttons is necessary,
    // so ItemsControl.ItemsSource can bind to a list of checkboxes
    public ObservableCollection<EncodingTypeEntry> EncodingTypes { get; } = [];

    void initialize() {
        addEncodingToTheList(EncodingType.Base64, "Base64", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemCert, "PEM - Certificate", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemTrustedCert, "PEM - Trusted Certificate", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemNewReq, "PEM - New Request", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemReq, "PEM - Request", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.Base64CrlHeader, "PEM - X.509 CRL", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemEvpPrivateKey, "PEM - EVP Private Key", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemPublicKey, "PEM - Public Key", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemRsaPrivateKey, "PEM - RSA Private Key", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemRsaPublicKey, "PEM - RSA Public Key", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemDsaPrivateKey, "PEM - DSA Private Key", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemDsaPublicKey, "PEM - DSA Public Key", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemDsaParams, "PEM - DSA Parameters", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemECPrivateKey, "PEM - ECDSA Private Key", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemECDsaPublicKey, "PEM - ECDSA Public Key", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemECParams, "PEM - EC Parameters", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemDHParams, "PEM - DH Parameters", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemDHXParams, "PEM - DHX Parameters", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemParams, "PEM - Parameters", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemPkcs7, "PEM - PKCS#7", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemPkcs7Signed, "PEM - PKCS#7 Signed", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemCms, "PEM - CMS", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemPkcs8Encrypted, "PEM - PKCS#8 Encrypted", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemPkcs8Inf, "PEM - PKCS#8 Unencrypted", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.PemSSLSessionParams, "PEM - SSL Session Parameters", EncodingGroup.PEM);
        addEncodingToTheList(EncodingType.Hex, "Hex", EncodingGroup.Hexadecimal);
        addEncodingToTheList(EncodingType.HexAddress, "Hex - with address", EncodingGroup.Hexadecimal);
        addEncodingToTheList(EncodingType.HexAscii, "Hex - with ASCII", EncodingGroup.Hexadecimal);
        addEncodingToTheList(EncodingType.HexAsciiAddress, "Hex - with address and ASCII", EncodingGroup.Hexadecimal);
        addEncodingToTheList(EncodingType.HexRaw, "Hex - raw", EncodingGroup.Hexadecimal);
    }
    void addEncodingToTheList(EncodingType encoding, String displayName, EncodingGroup encodingGroup) {
        EncodingTypes.Add(new EncodingTypeEntry(encoding, displayName, encodingGroup));
    }

    async Task openFile(Object obj, CancellationToken token) {
        if (!getOpenFilePath()) {
            return;
        }
        RawData.Clear();
        IsBusy = true;
        try {
            RawData.AddRange(await FileUtility.FileToBinaryAsync(path));
            formatToSelectedEncoding();
        } catch (Exception e) {
            _uiMessenger.ShowError(e.Message, "Read Error");
            Path = null;
        }
        IsBusy = false;
    }
    void saveFile(Object obj) {
        switch ((String)obj) {
            case "text":
                if (String.IsNullOrEmpty(Path)) {
                    if (!getSaveFilePath()) { return; }
                }
                saveText();
                break;
            case "textas":
                if (!getSaveFilePath()) { return; }
                saveText();
                break;
            case "bin":
                if (String.IsNullOrEmpty(Path)) {
                    if (!getSaveFilePath()) { return; }
                }
                saveBinary();
                break;
            case "binas":
                if (!getSaveFilePath()) { return; }
                saveBinary();
                break;
        }
    }
    async Task validateInput(Object obj, CancellationToken token) {
        if (String.IsNullOrEmpty(Text)) {
            _uiMessenger.ShowWarning("There is nothing to validate.");
            return;
        }
        EncodingType encoding = AsnFormatter.TestInputString(Text);
        RawData.Clear();
        selectedEncoding = EncodingTypes.FirstOrDefault(x => x.EncodingType == encoding);
        OnPropertyChanged(nameof(SelectedEncoding));
        if (SelectedEncoding is null) {
            _uiMessenger.ShowError("Input text cannot be validated.");
            
            return;
        }
        RawData.AddRange(HexUtility.AnyToBinary(Text));
        if (obj?.ToString() == "Decode" && _action is not null) {
            await _action(RawData.ToArray());
        }
    }
    Boolean canValidateInput(Object o) {
        return _action is not null;
    }

    void clearText(Object obj) {
        Text = String.Empty;
        RawData.Clear();
        Path = null;
    }
    void print(Object obj) {
        StaticCommands.Print(Text, NodeViewOptions.FontSize);
    }
    Boolean canPrintSave(Object obj) {
        return CanCheck = RawData.Count > 0;
    }
    Boolean getSaveFilePath() {
        if (!_uiMessenger.TryGetSaveFileName(out String saveFilePath)) {
            return false;
        }
        Path = saveFilePath;

        return true;
    }
    Boolean getOpenFilePath() {
        if (!_uiMessenger.TryGetOpenFileName(out String filePath)) {
            return false;
        }
        Path = filePath;

        return true;
    }
    void saveText() {
        try {
            File.WriteAllText(Path!, Text);
        } catch (Exception e) {
            _uiMessenger.ShowError(e.Message, "Save Error");
        }
    }
    void saveBinary() {
        try {
            File.WriteAllBytes(Path!, RawData.ToArray());
        } catch (Exception e) {
            _uiMessenger.ShowError(e.Message, "Save Error");
        }
    }

    void formatToSelectedEncoding() {
        if (RawData.Count == 0) {
            return;
        }
        
        SelectedEncoding = EncodingTypes[0];
    }
    public void SetBytes(IEnumerable<Byte> rawData) {
        if (rawData is null) { return; }
        RawData.Clear();
        RawData.AddRange(rawData);
        formatToSelectedEncoding();
    }
}