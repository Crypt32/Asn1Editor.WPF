using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using SysadminsLV.WPF.OfficeTheme.Toolkit;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.Views.Windows;

/// <summary>
/// Interaction logic for LicenseWindow.xaml
/// </summary>
public partial class LicenseWindow {
    public LicenseWindow() {
        CloseCommand = new RelayCommand(o => Close());
        InitializeComponent();
        loadEula();
        DataContext = this;
    }
    public ICommand CloseCommand { get; }

    void loadEula() {
        const String resourceName = "SysadminsLV.Asn1Editor.EULA.rtf";
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream is not null) {
            var textRange = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
            textRange.Load(resourceStream, DataFormats.Rtf);
        } else {
            MsgBox.Show("Error", "License file not found.");
        }
    }
}