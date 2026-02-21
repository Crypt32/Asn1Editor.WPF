using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;

namespace SysadminsLV.Asn1Editor.Controls;

public abstract class AsnValueEditor : Control {
    protected AsnValueEditor() {
        Loaded += onControlLoaded;
    }

    void onControlLoaded(Object sender, RoutedEventArgs e) {
        // Expose the validation command to the ViewModel
        SetCurrentValue(ValidateCommandProperty, new RelayCommand(onValidateCommandExecuted));
    }

    /// <summary>
    /// Performs validation of the current ASN.1 value and returns the validation result.
    /// </summary>
    /// <remarks>
    /// This method is invoked by the <see cref="ValidateCommand"/> and is responsible for validating
    /// the ASN.1 value represented by the editor. The result of the validation is encapsulated in an
    /// instance of <see cref="AsnValueValidationResult"/>, which provides details about the validity,
    /// the encoded value (if valid), or an error message (if invalid).
    /// <para>Inheritors SHALL NOT call this method manually, because it will render no result.</para>
    /// </remarks>
    /// <returns>
    /// An <see cref="AsnValueValidationResult"/> instance containing the outcome of the validation.
    /// </returns>
    protected abstract AsnValueValidationResult PerformValidation();
    /// <summary>
    /// Sets the validation result for the current control by performing an out-of-band validation.
    /// </summary>
    /// <remarks>
    /// This method invokes the <see cref="PerformValidation"/> method to obtain the validation result
    /// and updates the <see cref="Result"/> dependency property with the obtained value.
    /// <para>
    /// Inheritors MAY call this method to trigger validation and update the result without waiting for trigger from user code.
    /// It is useful in scenarios where the control needs to perform validation immediately after certain events
    /// (e.g., after loading or when input value changes) without requiring explicit user action to trigger the validation command.
    /// </para>
    /// </remarks>
    protected void SetOutOfBandValidationResult() {
        SetCurrentValue(ResultProperty, PerformValidation());
    }

    #region InputValue

    // can't be Byte[]:
    // https://stackoverflow.com/questions/926486/wpf-compilation-error-tags-of-type-propertyarraystart-are-not-supported-in
    public static readonly DependencyProperty InputValueProperty = DependencyProperty.Register(
        nameof(InputValue),
        typeof(IList<Byte>),
        typeof(AsnValueEditor),
        new FrameworkPropertyMetadata(null, onInputValueChanged));
    // must be full TLV value
    public IList<Byte> InputValue {
        get => (IList<Byte>)GetValue(InputValueProperty);
        set => SetValue(InputValueProperty, value);
    }

    static void onInputValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is AsnValueEditor editor) {
            editor.OnInputValueChanged((Byte[])e.OldValue, (Byte[])e.NewValue);
        }
    }

    protected abstract void OnInputValueChanged(Byte[]? oldValue, Byte[]? newValue);

    #endregion

    #region Result

    public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
        nameof(Result),
        typeof(AsnValueValidationResult),
        typeof(AsnValueEditor),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public AsnValueValidationResult Result {
        get => (AsnValueValidationResult)GetValue(ResultProperty);
        set => SetValue(ResultProperty, value);
    }

    #endregion

    #region ValidateCommand

    // expose command to user code to trigger validation from ViewModel. Control itself will set this
    // command to execute PerformValidation and update Result property.
    public static readonly DependencyProperty ValidateCommandProperty = DependencyProperty.Register(
        nameof(ValidateCommand),
        typeof(ICommand),
        typeof(AsnValueEditor),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public ICommand ValidateCommand {
        get => (ICommand)GetValue(ValidateCommandProperty);
        set => SetValue(ValidateCommandProperty, value);
    }

    void onValidateCommandExecuted(Object? o) {
        SetOutOfBandValidationResult();
    }

    #endregion
}