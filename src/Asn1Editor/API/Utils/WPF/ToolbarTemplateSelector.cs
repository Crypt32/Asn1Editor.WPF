using System;
using System.Windows;
using System.Windows.Controls;

namespace SysadminsLV.Asn1Editor.API.Utils.WPF;

public class ToolbarTemplateSelector : DataTemplateSelector {
    public DataTemplate RibbonTemplate { get; set; }
    public DataTemplate ClassicTemplate { get; set; }

    public override DataTemplate SelectTemplate(Object? item, DependencyObject container) {
        if (item is null or true) {
            return RibbonTemplate;
        }
        return ClassicTemplate;
    }
}