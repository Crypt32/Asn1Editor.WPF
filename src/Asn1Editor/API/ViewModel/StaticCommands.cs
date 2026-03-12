using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SysadminsLV.Asn1Editor.API.ViewModel; 

static class StaticCommands {
    public static void Print(String text, Int32 fontSize) {
        var printDialog = new PrintDialog();
        if (!printDialog.ShowDialog().GetValueOrDefault()) { return; }
        var flowDocument = new FlowDocument {
            PageHeight = printDialog.PrintableAreaHeight,
            PageWidth = printDialog.PrintableAreaWidth,
            PagePadding = new Thickness(25),
            ColumnGap = 0,
        };
        flowDocument.ColumnWidth = flowDocument.PageWidth -
                                   flowDocument.ColumnGap -
                                   flowDocument.PagePadding.Left -
                                   flowDocument.PagePadding.Right;
        var paragraph = new Paragraph {
            Margin = new Thickness(0),
            FontFamily = new FontFamily("Consolas"),
            FontSize = fontSize
        };
        paragraph.Inlines.Add(new Run(text));
        flowDocument.Blocks.Add(paragraph);
        DocumentPaginator paginator = ((IDocumentPaginatorSource)flowDocument).DocumentPaginator;
        printDialog.PrintDocument(paginator, String.Empty);
    }
}