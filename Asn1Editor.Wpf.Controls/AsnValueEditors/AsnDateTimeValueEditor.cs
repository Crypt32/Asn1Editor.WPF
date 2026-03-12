using System;
using System.Windows;
using System.Windows.Controls;
using SysadminsLV.Asn1Parser.Universal;

namespace SysadminsLV.Asn1Editor.Controls;

public abstract class AsnDateTimeValueEditor : AsnValueEditor {
    Boolean updatingFromValue;

    #region Value

    static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(DateTimeOffset),
        typeof(AsnDateTimeValueEditor),
        new PropertyMetadata(DateTimeOffset.Now, OnValueChanged));
    static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs args) {
        if (d is AsnDateTimeValueEditor editor && args.NewValue is DateTimeOffset dateTime) {
            if (editor.updatingFromValue) {
                return;
            }
            try {
                // block loops
                editor.updatingFromValue = true;
                DateTimeOffset local = TimeZoneInfo.ConvertTime(dateTime, editor.TimeZone);

                // update the date/time components and the UTC value based on the new Value.
                // use checks to avoid unnecessary updates that could trigger more events.
                if (editor.Date != local.Date) {
                    editor.Date = local.Date;
                }
                if (editor.Hour != local.Hour) {
                    editor.Hour = local.Hour;
                }
                if (editor.Minute != local.Minute) {
                    editor.Minute = local.Minute;
                }
                if (editor.Second != local.Second) {
                    editor.Second = local.Second;
                }

                editor.SetValue(UtcValuePropertyKey, dateTime.UtcDateTime);
            } finally {
                // resume
                editor.updatingFromValue = false;
            }
        }
    }

    public DateTimeOffset Value {
        get => (DateTimeOffset)GetValue(ValueProperty);
        private set => SetValue(ValueProperty, value);
    }

    #endregion

    #region UTC Value

    static readonly DependencyPropertyKey UtcValuePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(UtcValue),
        typeof(DateTime),
        typeof(AsnDateTimeValueEditor),
        new PropertyMetadata(default(DateTime)));

    static readonly DependencyProperty UtcValueProperty = UtcValuePropertyKey.DependencyProperty;

    public DateTime UtcValue => (DateTime)GetValue(UtcValueProperty);

    #endregion

    #region Date

    public static readonly DependencyProperty DateProperty = DependencyProperty.Register(
        nameof(Date),
        typeof(DateTime),
        typeof(AsnDateTimeValueEditor),
        new FrameworkPropertyMetadata(default(DateTime), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDateTimeControlsChanged));

    public DateTime Date {
        get => (DateTime)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    #endregion

    #region Hour

    static readonly DependencyProperty HourProperty = DependencyProperty.Register(
        nameof(Hour),
        typeof(Int32),
        typeof(AsnDateTimeValueEditor),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDateTimeControlsChanged));

    public Int32 Hour {
        get => (Int32)GetValue(HourProperty);
        set => SetValue(HourProperty, value);
    }

    #endregion

    #region Minute

    static readonly DependencyProperty MinuteProperty = DependencyProperty.Register(
        nameof(Minute),
        typeof(Int32),
        typeof(AsnDateTimeValueEditor),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDateTimeControlsChanged));

    public Int32 Minute {
        get => (Int32)GetValue(MinuteProperty);
        set => SetValue(MinuteProperty, value);
    }

    #endregion

    #region Second

    static readonly DependencyProperty SecondProperty = DependencyProperty.Register(
        nameof(Second),
        typeof(Int32),
        typeof(AsnDateTimeValueEditor),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDateTimeControlsChanged));

    public Int32 Second {
        get => (Int32)GetValue(SecondProperty);
        set => SetValue(SecondProperty, value);
    }

    #endregion

    #region TimeZone

    static readonly DependencyProperty TimeZoneProperty = DependencyProperty.Register(
        nameof(TimeZone),
        typeof(TimeZoneInfo),
        typeof(AsnDateTimeValueEditor),
        new FrameworkPropertyMetadata(TimeZoneInfo.Local, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDateTimeControlsChanged));
    static void OnDateTimeControlsChanged(DependencyObject d, DependencyPropertyChangedEventArgs args) {
        if (d is AsnDateTimeValueEditor editor) {
            editor.updateDateTime();
        }
    }

    public TimeZoneInfo TimeZone {
        get => (TimeZoneInfo)GetValue(TimeZoneProperty);
        set => SetValue(TimeZoneProperty, value);
    }

    #endregion

    protected override void OnInputValueChanged(Byte[]? oldValue, Byte[]? newValue) {
        if (newValue is not null) {
            Value = newValue.Length == 2
                ? DateTime.Now
                : GetDateTimeFromEncodedValue(newValue);
        }
    }
    protected abstract DateTime GetDateTimeFromEncodedValue(Byte[] encodedValue);

    // This method is called when any of the date/time components or the time zone changes, and it updates the Value property accordingly.
    void updateDateTime() {
        if (updatingFromValue) {
            return;
        }

        var dateTime = new DateTime(
            Date.Year,
            Date.Month,
            Date.Day,
            Hour,
            Minute,
            Second);

        TimeSpan offset = TimeZone.GetUtcOffset(dateTime);
        SetCurrentValue(ValueProperty, new DateTimeOffset(dateTime, offset));
    }

    static AsnDateTimeValueEditor() {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(AsnDateTimeValueEditor),
            new FrameworkPropertyMetadata(typeof(AsnDateTimeValueEditor)));
    }
}

public class AsnUtcTimeValueEditor : AsnDateTimeValueEditor {
    static readonly DateTime UtcMinValue = new DateTime(1950, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    static readonly DateTime UtcMaxValue = new DateTime(2049, 12, 31, 23, 59, 59, DateTimeKind.Utc);

    protected override AsnValueValidationResult PerformValidation() {
        if (UtcValue < UtcMinValue) {
            return AsnValueValidationResult.Fail("ASN.1 UTCTime cannot be less than January 1, 1950.");
        }
        if (UtcValue > UtcMaxValue) {
            return AsnValueValidationResult.Fail("ASN.1 UTCTime cannot be greater than December 31, 2049.");
        }

        Byte[] rawData = new Asn1UtcTime(Value.UtcDateTime.ToLocalTime()).GetRawData();
        return AsnValueValidationResult.Ok(rawData);
    }
    protected override DateTime GetDateTimeFromEncodedValue(Byte[] encodedValue) {
        return new Asn1UtcTime(encodedValue).Value;
    }

    public override void OnApplyTemplate() {
        base.OnApplyTemplate();

        if (GetTemplateChild("PART_DatePicker") is DatePicker datePicker) {
            datePicker.DisplayDateStart = UtcMinValue;
            datePicker.DisplayDateEnd = UtcMaxValue;
        }
    }


    static AsnUtcTimeValueEditor() {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(AsnUtcTimeValueEditor),
            new FrameworkPropertyMetadata(typeof(AsnUtcTimeValueEditor)));
    }
}

public class AsnGeneralizedTimeValueEditor : AsnDateTimeValueEditor {
    protected override AsnValueValidationResult PerformValidation() {
        Byte[] rawData = new Asn1GeneralizedTime(Value.UtcDateTime.ToLocalTime()).GetRawData();
        return AsnValueValidationResult.Ok(rawData);
    }
    protected override DateTime GetDateTimeFromEncodedValue(Byte[] encodedValue) {
        return new Asn1GeneralizedTime(encodedValue).Value;
    }

    static AsnGeneralizedTimeValueEditor() {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(AsnGeneralizedTimeValueEditor),
            new FrameworkPropertyMetadata(typeof(AsnGeneralizedTimeValueEditor)));
    }
}