using System;
using System.ComponentModel;
using System.Windows.Input;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.API.ViewModel;


class AsnDocumentHostVM : ViewModelBase, IAsnDocumentHost {
    readonly MainWindowVM _owner;
    Asn1DocumentVM left;
    Asn1DocumentVM? right;
    Boolean isCompareMode;
    String header;

    public AsnDocumentHostVM(MainWindowVM owner) {
        _owner = owner;
        StartCommand = new RelayCommand(start);
        ExitCommand = new RelayCommand(exit, _ => IsCompareMode);
        left = new Asn1DocumentVM(_owner.NodeViewOptions, _owner.TreeCommands);
        left.PropertyChanged += onMainContentPropertyChanged;
    }

    public ICommand StartCommand { get; }
    public ICommand ExitCommand { get; }

    public String Header =>
        isCompareMode
            ? "Comparing: " + (left.Header ?? "") + " <> " + (right?.Header ?? "")
            : Left.Header;
    public Asn1DocumentVM Left {
        get => left;
        set {
            left.PropertyChanged -= onMainContentPropertyChanged;
            left = value ?? new Asn1DocumentVM(_owner.NodeViewOptions, _owner.TreeCommands);
            left.PropertyChanged += onMainContentPropertyChanged;
            OnPropertyChanged();
        }
    }
    public Asn1DocumentVM? Right {
        get => right;
        set {
            right = value;
            OnPropertyChanged();
        }
    }
    public Boolean IsCompareMode {
        get => isCompareMode;
        set {
            isCompareMode = value;
            OnPropertyChanged();
        }
    }

    void start(object? o) {
        if (o is not TabCompareParam param) return;
        if (ReferenceEquals(param.Left, param.Right)) return;

        //Left = param.Left;
        Right = param.Right;

        // слева активным делаем левый документ
        _owner.SelectedTab = param.Left;

        IsCompareMode = true;
    }
    void exit(Object o) {
        IsCompareMode = false;
        //Left = null;
        Right = null;
    }

    void onMainContentPropertyChanged(Object sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(Asn1DocumentVM.Header)) {
            OnPropertyChanged(nameof(Header));
        }
    }

    public Asn1DocumentVM GetPrimaryDocument() {
        return Left;
    }
}
