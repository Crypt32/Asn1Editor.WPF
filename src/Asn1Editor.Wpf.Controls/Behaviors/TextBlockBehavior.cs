using System;
using System.Windows;
using System.Windows.Controls;

namespace SysadminsLV.Asn1Editor.Controls.Behaviors;

public static class TextBlockBehavior {
    #region TrimLength

    public static readonly DependencyProperty TrimLengthProperty = DependencyProperty.RegisterAttached(
        "TrimLength",
        typeof(Int32),
        typeof(TextBlockBehavior), new PropertyMetadata(OnTrimLengthChanged));
    static void OnTrimLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is TextBlock textBlock) {
            setTextBlockText(textBlock);
        }
    }

    public static Int32 GetTrimLength(DependencyObject d) {
        return (Int32)d.GetValue(TrimLengthProperty);
    }
    public static void SetTrimLength(DependencyObject d, Int32 value) {
        d.SetValue(TrimLengthProperty, value);
    }

    #endregion

    #region SourceText

    public static readonly DependencyProperty SourceTextProperty = DependencyProperty.RegisterAttached(
        "SourceText",
        typeof(String),
        typeof(TextBlockBehavior), new PropertyMetadata(OnSourceTextChanged));
    static void OnSourceTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is TextBlock textBlock) {
            setTextBlockText(textBlock);
        }
    }

    public static String GetSourceText(DependencyObject d) {
        return (String)d.GetValue(SourceTextProperty);
    }
    public static void SetSourceText(DependencyObject d, String value) {
        d.SetValue(SourceTextProperty, value);
    }

    #endregion

    static void setTextBlockText(TextBlock textBlock) {
        Int32 trimLength = GetTrimLength(textBlock);
        String sourceText = GetSourceText(textBlock) ?? String.Empty;
        if (trimLength == 0 || sourceText.Length <= trimLength) {
            textBlock.Text = GetSourceText(textBlock);
        } else {
            textBlock.Text = sourceText.Substring(0, trimLength) + "...";
        }
    }
}