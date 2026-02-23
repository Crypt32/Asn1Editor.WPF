using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using SysadminsLV.Asn1Editor.Core;
using SysadminsLV.Asn1Editor.Core.ASN;
using SysadminsLV.Asn1Parser;
using SysadminsLV.Asn1Parser.Universal;

namespace SysadminsLV.Asn1Editor.Controls;

public class AsnOidEditor : AsnValueEditor {
    static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(String),
        typeof(AsnOidEditor),
        new PropertyMetadata(null, onValueChanged));
    static void onValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is AsnOidEditor editor) {
            editor.OidInfo.Resolve(editor.Value);
        }
    }

    public String Value {
        get => (String)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    static readonly DependencyProperty OidInfoProperty = DependencyProperty.Register(
        nameof(OidInfo),
        typeof(AsnOidInfo),
        typeof(AsnOidEditor),
        new PropertyMetadata(new AsnOidInfo()));

    public AsnOidInfo OidInfo {
        get => (AsnOidInfo)GetValue(OidInfoProperty);
        private set => SetValue(OidInfoProperty, value);
    }

    public override void OnApplyTemplate() {
        if (Template.FindName("PART_Hyperlink", this) is Hyperlink link) {
            link.RequestNavigate += onLinkClicked;
        }
    }
    static void onLinkClicked(Object sender, RequestNavigateEventArgs e) {
        var sInfo = new ProcessStartInfo(e.Uri.AbsoluteUri) {
            UseShellExecute = true,
        };
        Process.Start(sInfo);
    }

    protected override AsnValueValidationResult PerformValidation() {
        if (String.IsNullOrEmpty(OidInfo.Value)) {
            return AsnValueValidationResult.Fail("Specified OID format is not valid.");
        }

        try {
            return AsnValueValidationResult.Ok(AsnDecoder.EncodeGeneric((Byte)Asn1Type.OBJECT_IDENTIFIER, OidInfo.Value, 0));
        } catch (Exception ex) {
            return AsnValueValidationResult.Fail(ex.Message);
        }
    }
    protected override void OnInputValueChanged(Byte[]? oldValue, Byte[]? newValue) {
        if (newValue is not null) {
            var oid = new Asn1ObjectIdentifier(newValue);
            Value = oid.Value.Value;
        }
    }

    static AsnOidEditor() {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(AsnOidEditor),
            new FrameworkPropertyMetadata(typeof(AsnOidEditor)));
    }
}

public class AsnOidInfo : INotifyPropertyChanged {
    readonly Regex _oidRegex = new(@"^(?:(?:0|1)\.(?:[0-9]|[1-3][0-9])|2\.(?:0|[1-9][0-9]*))(?:\.(?:0|[1-9][0-9]*))*$", RegexOptions.Compiled);

    public String? FriendlyName {
        get;
        set => SetField(ref field, value);
    }
    public String Value {
        get;
        set => SetField(ref field, value);
    } = String.Empty;
    public String? ExternalUri {
        get;
        set => SetField(ref field, value);
    }

    internal void Resolve(String oidString) {
        if (_oidRegex.IsMatch(oidString)) {
            FriendlyName = OidServices.Resolver.ResolveOid(oidString);
            Value = oidString;
            ExternalUri = "https://oidref.com/" + Value;
        } else {
            FriendlyName = oidString;
            Value = OidServices.Resolver.ResolveFriendlyName(oidString) ?? String.Empty;
            ExternalUri = null;
        }
    }


    #region INotifyPropertyChanged

    void SetField<T>(ref T field, T value, [CallerMemberName] String? propertyName = null) {
        if (EqualityComparer<T>.Default.Equals(field, value)) {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion
}