using System;
using System.ComponentModel;
using System.Windows.Input;
using SysadminsLV.Asn1Editor.API.Interfaces;
using SysadminsLV.Asn1Editor.API.ModelObjects;
using SysadminsLV.Asn1Editor.API.Utils.WPF;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.API.ViewModel;


public class AsnDocumentHostVM : ViewModelBase, IAsnDocumentHost {
    Asn1DocumentVM left;

    public AsnDocumentHostVM(NodeViewOptions nodeViewOptions, ITreeCommands treeCommands) {
        NodeViewOptions = nodeViewOptions;
        TreeCommands = treeCommands;
        StartCompareModeCommand = new RelayCommand(start);
        ExitCompareModeCommand = new RelayCommand(exit, _ => IsCompareMode);
        left = new Asn1DocumentVM(nodeViewOptions, treeCommands);
        left.PropertyChanged += onMainContentPropertyChanged;
    }

    public ICommand StartCompareModeCommand { get; }
    public ICommand ExitCompareModeCommand { get; }
    public NodeViewOptions NodeViewOptions { get; }
    public ITreeCommands TreeCommands { get; }
    // Unique identifier for scroll synchronization between compare tabs
    public String ScrollGroupId { get; } = Guid.NewGuid().ToString("N");

    public Boolean IsScrollbarSynchronized {
        get;
        set {
            field = value;
            OnPropertyChanged();
        }
    } = true;


    public String Header =>
        IsCompareMode
            ? "Comparing: " + Left.Header + " <> " + (Right?.Header ?? "")
            : Left.Header;
    public Asn1DocumentVM Left {
        get => left;
        set {
            left.PropertyChanged -= onMainContentPropertyChanged;
            left = value ?? new Asn1DocumentVM(NodeViewOptions, TreeCommands);
            left.PropertyChanged += onMainContentPropertyChanged;
            OnPropertyChanged();
        }
    }
    public Asn1DocumentVM? Right
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }
    public Boolean IsCompareMode
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    void refreshHeader() {
        OnPropertyChanged(nameof(Header));
    }
    void start(Object? o) {
        if (o is not TabCompareParam param || param.Left is null || ReferenceEquals(param.Left, param.Right)) {
            return;
        }

        Right = param.Right!.GetPrimaryDocument();
        Right.IsEnabled = false;

        IsCompareMode = true;
        refreshHeader();
    }
    void exit(Object o) {
        IsCompareMode = false;
        Right!.IsEnabled = true;
        Right = null;
        refreshHeader();
    }

    void onMainContentPropertyChanged(Object sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(Asn1DocumentVM.Header)) {
            refreshHeader();
        }
    }

    public Asn1DocumentVM GetPrimaryDocument() {
        return Left;
    }
}
