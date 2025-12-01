using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using SysadminsLV.Asn1Editor.Controls.Extensions;
using SysadminsLV.WPF.OfficeTheme.Controls;

namespace SysadminsLV.Asn1Editor.Controls;

public class AsnHexViewer : Control {
    const String masterAddr = "12345678";
    const String masterHex = "123456789012345678901234567890123456789012345678";
    const String masterAscii = "1234567890123456";

    static AsnHexViewer() {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(AsnHexViewer),
            new FrameworkPropertyMetadata(typeof(AsnHexViewer)));
        FontSizeProperty.OverrideMetadata(
            typeof(AsnHexViewer),
            new FrameworkPropertyMetadata(OnFontSizeChanged));
    }
    static void OnFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is AsnHexViewer { controlInitialized: true } control) {
            control.calculateWidths();
        }
    }

    RichTextBox[] panes;

    Boolean controlInitialized, scrollLocked;
    readonly TextRange[][] ranges = new TextRange[2][];

    ScrollBar Scroller;
    RichTextBox HexAddrHeaderRtb, HexRawHeaderRtb, HexAsciiHeaderRtb;
    BindableRichTextBox HexAddressPane, HexRawPane, HexAsciiPane;


    #region ShowAddressPane

    public static readonly DependencyProperty ShowAddressPaneProperty = DependencyProperty.Register(
        nameof(ShowAddressPane),
        typeof(Boolean),
        typeof(AsnHexViewer),
        new PropertyMetadata(true));

    public Boolean ShowAddressPane {
        get => (Boolean)GetValue(ShowAddressPaneProperty);
        set => SetValue(ShowAddressPaneProperty, value);
    }

    #endregion

    #region ShowAsciiPane

    public static readonly DependencyProperty ShowAsciiPaneProperty = DependencyProperty.Register(
        nameof(ShowAsciiPane),
        typeof(Boolean),
        typeof(AsnHexViewer),
        new PropertyMetadata(true));

    public Boolean ShowAsciiPane {
        get => (Boolean)GetValue(ShowAsciiPaneProperty);
        set => SetValue(ShowAsciiPaneProperty, value);
    }

    #endregion

    #region DataSource

    public static readonly DependencyProperty DataSourceProperty = DependencyProperty.Register(
        nameof(DataSource),
        typeof(IList<Byte>),
        typeof(AsnHexViewer),
        new FrameworkPropertyMetadata(OnDataSourcePropertyChanged));
    public IList<Byte>? DataSource {
        get => (IList<Byte>)GetValue(DataSourceProperty);
        set => SetValue(DataSourceProperty, value);
    }
    static void OnDataSourcePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e) {
        if (e.OldValue is INotifyCollectionChanged oldValue) {
            oldValue.CollectionChanged -= ((AsnHexViewer)source).OnCollectionChanged;
        }
        if (e.NewValue is INotifyCollectionChanged newValue) {
            newValue.CollectionChanged += ((AsnHexViewer)source).OnCollectionChanged;
        }
        ((AsnHexViewer)source).refreshView();
    }
    void OnCollectionChanged(Object o, NotifyCollectionChangedEventArgs e) {
        refreshView();
    }

    #endregion

    #region SelectedNode

    public static readonly DependencyProperty SelectedNodeProperty = DependencyProperty.Register(
        nameof(SelectedNode),
        typeof(IHexAsnNode),
        typeof(AsnHexViewer),
        new FrameworkPropertyMetadata(onSelectedNodeChanged));
    static void onSelectedNodeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
        var ctrl = (AsnHexViewer)sender;
        if (!ctrl.controlInitialized) {
            return;
        }
        if (e.OldValue is IHexAsnNode oldValue) {
            oldValue.DataChanged -= ctrl.onNodeDataChanged;
        }
        if (e.NewValue is null) {
            ctrl.rebuildPanes(null);
            if (ctrl.IsColoringEnabled) {
                //ctrl.ResetColors();
            }
            return;
        }
        var treeNode = (IHexAsnNode)e.NewValue;
        if (ctrl.IsColoringEnabled) {
            ctrl.rebuildPanes(treeNode);
        }

        treeNode.DataChanged += ctrl.onNodeDataChanged;
    }
    void onNodeDataChanged(Object sender, EventArgs args) {
        // this event handler is potentially triggered from a different thread than UI thread which
        // owns current instance. In this case, we cannot access any dependency property, because
        // different thread owns it. So, check if event fired in UI thread. If so, continue as expected,
        // otherwise invoke this handler in UI thread.
        if (Thread.CurrentThread == Dispatcher.Thread) {
            rebuildPanes(sender as IHexAsnNode);
            //if (sender is IHexAsnNode node) {
            //    reColorHex(node);
            //}
        }
        else {
            Dispatcher.Invoke(new Action<Object, EventArgs>(onNodeDataChanged), sender, args);
        }
    }


    public IHexAsnNode SelectedNode {
        get => (IHexAsnNode)GetValue(SelectedNodeProperty);
        set => SetValue(SelectedNodeProperty, value);
    }

    #endregion

    #region TagOctetBrush

    public static readonly DependencyProperty TagOctetBrushProperty = DependencyProperty.Register(
        nameof(TagOctetBrush),
        typeof(Brush),
        typeof(AsnHexViewer),
        new PropertyMetadata(default));

    public Brush TagOctetBrush {
        get => (Brush)GetValue(TagOctetBrushProperty);
        set => SetValue(TagOctetBrushProperty, value);
    }

    #endregion

    #region TagLengthOctetBrush

    public static readonly DependencyProperty TagLengthOctetBrushProperty = DependencyProperty.Register(
        nameof(TagLengthOctetBrush),
        typeof(Brush),
        typeof(AsnHexViewer),
        new PropertyMetadata(default));

    public Brush TagLengthOctetBrush {
        get => (Brush)GetValue(TagLengthOctetBrushProperty);
        set => SetValue(TagLengthOctetBrushProperty, value);
    }

    #endregion

    #region TagPayloadOctetBrush

    public static readonly DependencyProperty TagPayloadOctetBrushProperty = DependencyProperty.Register(
        nameof(TagPayloadOctetBrush),
        typeof(Brush),
        typeof(AsnHexViewer),
        new PropertyMetadata(default));

    public Brush TagPayloadOctetBrush {
        get => (Brush)GetValue(TagPayloadOctetBrushProperty);
        set => SetValue(TagPayloadOctetBrushProperty, value);
    }

    #endregion

    #region IsColoringEnabled

    public static readonly DependencyProperty IsColoringEnabledProperty = DependencyProperty.Register(
        nameof(IsColoringEnabled),
        typeof(Boolean),
        typeof(AsnHexViewer),
        new PropertyMetadata(onIsColoringEnabledChanged));

    public Boolean IsColoringEnabled {
        get => (Boolean)GetValue(IsColoringEnabledProperty);
        set => SetValue(IsColoringEnabledProperty, value);
    }

    static void onIsColoringEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var ctrl = (AsnHexViewer)d;
        ctrl.rebuildPanes(ctrl.SelectedNode);
    }

    #endregion

    void calculateWidths() {
        HexAddrHeaderRtb.SetWidthToFitString(masterAddr, FontSize);
        HexRawHeaderRtb.SetWidthToFitString(masterHex, FontSize);
        HexAsciiHeaderRtb.SetWidthToFitString(masterAscii, FontSize);
    }
    void buildAddress() {
        var addressParagraph = new Paragraph();
        foreach (Int32 row in Enumerable.Range(0, (Int32)Math.Ceiling((Double)DataSource!.Count / 16))) {
            addressParagraph.Inlines.Add(new Run($"{row * 16:X8}" + Environment.NewLine));
        }
        HexAddressPane.Document.Blocks.Clear();
        HexAddressPane.Document.Blocks.Add(addressParagraph);
    }
    void onRtbScrollChanged(Object sender, ScrollChangedEventArgs e) {
        if (scrollLocked) {
            return;
        }
        scrollLocked = true;
        var scrollViewer = (ScrollViewer)e.OriginalSource;
        Scroller.Maximum = scrollViewer.ScrollableHeight;
        Scroller.ViewportSize = scrollViewer.ViewportHeight;
        Scroller.Value = scrollViewer.VerticalOffset;
        scrollPanes(scrollViewer.VerticalOffset);
        scrollLocked = false;
    }
    void scrollPanes(Double? newValue) {
        Double vOffset = newValue ?? HexRawPane.FontSize * HexRawPane.FontFamily.LineSpacing * (HexRawPane.CurrentLine - 1);
        for (Int32 i = 0; i < panes.Length; i++) {
            if (i > 0) {
                // do not fire re-scroll for the rest of RTBs
                scrollLocked = true;
            }
            panes[i].ScrollToVerticalOffset(vOffset);
        }
        scrollLocked = false;
    }
    void onScrollerScroll(Object sender, ScrollEventArgs e) {
        const Double smallStep = 48;
        const Double bigStep = 256;
        Double finalValue = e.NewValue;

        switch (e.ScrollEventType) {
            case ScrollEventType.LargeDecrement:
                finalValue = e.NewValue - bigStep < Scroller.Minimum
                    ? Scroller.Minimum
                    : e.NewValue - bigStep;
                break;
            case ScrollEventType.LargeIncrement:
                finalValue = e.NewValue + bigStep > Scroller.Maximum
                    ? Scroller.Maximum
                    : e.NewValue + bigStep;
                break;
            case ScrollEventType.SmallDecrement:
                finalValue = e.NewValue - smallStep < Scroller.Minimum
                    ? Scroller.Minimum
                    : e.NewValue - smallStep;
                break;
            case ScrollEventType.SmallIncrement:
                finalValue = e.NewValue + smallStep > Scroller.Maximum
                    ? Scroller.Maximum
                    : e.NewValue + smallStep;
                break;
        }

        Scroller.Value = finalValue;
        scrollPanes(finalValue);
        e.Handled = true;
    }

    void refreshView() {
        if (DataSource is null || !controlInitialized) {
            return;
        }

        buildAddress();
        rebuildPanes(SelectedNode);
    }

    public override void OnApplyTemplate() {
        Scroller = GetTemplateChild("PART_ScrollBar") as ScrollBar;
        if (Scroller is null) {
            throw new ArgumentException("'PART_ScrollBar' part was not found.");
        }
        Scroller.Maximum = 0;
        Scroller.Scroll += onScrollerScroll;

        HexAddrHeaderRtb = GetTemplateChild("PART_AddressHeader") as RichTextBox;
        HexRawHeaderRtb = GetTemplateChild("PART_HexHeader") as RichTextBox;
        HexAsciiHeaderRtb = GetTemplateChild("PART_AsciiHeader") as RichTextBox;

        HexAddressPane = initializeBindableRtb("PART_AddressBody");
        HexRawPane = initializeBindableRtb("PART_HexBody");
        HexAsciiPane = initializeBindableRtb("PART_AsciiBody");

        ranges[0] = new TextRange[3];
        ranges[1] = new TextRange[3];
        panes = [HexAddressPane, HexRawPane, HexAsciiPane];
        controlInitialized = true;
        calculateWidths();
        refreshView();

        base.OnApplyTemplate();
    }

    BindableRichTextBox initializeBindableRtb(String resourceName) {
        var rtb = GetTemplateChild(resourceName) as BindableRichTextBox;
        rtb!.Loaded += (sender, _) => subscribeScrollViewerEvent((TextBoxBase)sender);
        rtb.Document = new FlowDocument();

        return rtb;
    }
    void subscribeScrollViewerEvent(TextBoxBase textBoxBase) {
        if (textBoxBase is null) {
            throw new ArgumentNullException(nameof(textBoxBase));
        }
        if (textBoxBase.Template.FindName("PART_ContentHost", textBoxBase) is not ScrollViewer scroll) {
            throw new ArgumentException("'PART_ContentHost' named element could not be found in current TextBoxBase instance.");
        }
        scroll.ScrollChanged += onRtbScrollChanged;
    }

    #region Hex Colorizer

    void rebuildPanes(IHexAsnNode? node) {
        if (!controlInitialized) {
            return;
        }

        var tokenizer = new AsnHexTokenizer(DataSource, node);
        IReadOnlyCollection<HexSegment> segments = tokenizer.GetColorSegments();
        var hexParagraph = new Paragraph();
        var asciiParagraph = new Paragraph();
        Run anchorRun = null;

        foreach (HexSegment segment in segments) {
            var hexRun = buildRun(segment.HexText, segment.Kind);
            if (segment.Kind == HexDecorationKind.Tag) {
                anchorRun = hexRun;
            }
            hexParagraph.Inlines.Add(hexRun);
            asciiParagraph.Inlines.Add(buildRun(segment.AsciiText, segment.Kind));
        }
        HexRawPane.Document = new FlowDocument(hexParagraph);
        HexAsciiPane.Document = new FlowDocument(asciiParagraph);

        if (anchorRun is not null) {
            HexRawPane.CaretPosition = anchorRun.ContentStart;
        }
        scrollPanes(null);
    }

    Run buildRun(String text, HexDecorationKind kind) {
        var run = new Run(text);
        if (IsColoringEnabled) {
            switch (kind) {
                case HexDecorationKind.Tag:
                    run.FontWeight = FontWeights.Bold;
                    run.Foreground = TagOctetBrush;
                    break;
                case HexDecorationKind.Length:
                    run.FontWeight = FontWeights.Bold;
                    run.Foreground = TagLengthOctetBrush;
                    break;
                case HexDecorationKind.Value:
                    run.FontWeight = FontWeights.Bold;
                    run.Foreground = TagPayloadOctetBrush;
                    break;
            }
        }

        return run;
    }

    #endregion
}
class AsnHexTokenizer {
    const String EOL = "\r\n";

    readonly IList<Byte>? _buffer;
    readonly HexDecorationRange[] _decorations;

    public AsnHexTokenizer(IList<Byte>? buffer, IHexAsnNode? node) {
        _buffer = buffer;
        _decorations = getDecorationRanges(node);
    }

    public IReadOnlyCollection<HexSegment> GetColorSegments() {
        if (_buffer is null) {
            return [];
        }

        var hexSb = new StringBuilder();
        var asciiSb = new StringBuilder();
        var segments = new List<HexSegment>(5);
        var currentKind = HexDecorationKind.None;
        for (Int32 index = 0; index < _buffer!.Count; index++) {
            Int32 col = index % 16;

            var kind = GetKindForByte(index);

            // if decoration kind changed, finish it and store.
            if (kind != currentKind) {
                FlushSegment();
                currentKind = kind;
            }

            // new line, except for the very first line
            if (col == 0 && index > 0) {
                hexSb.Append(EOL);
                asciiSb.Append(EOL);
            }

            // write current octet into string builders
            // hex
            hexSb.Append(_buffer[index].ToString("X2"));
            hexSb.Append(' ');
            // extra space after 8th octet
            if (col == 7) {
                hexSb.Append(' ');
            }

            // ASCII
            Char c = _buffer[index] < 32 || _buffer[index] > 126
                ? '.'
                : (Char)_buffer[index];
            asciiSb.Append(c);
        }
        // flush final segment
        FlushSegment();

        return segments;

        void FlushSegment() {
            if (hexSb.Length == 0) {
                return;
            }

            segments.Add(new HexSegment {
                HexText = hexSb.ToString(),
                AsciiText = asciiSb.ToString(),
                Kind = currentKind
            });
            hexSb.Clear();
            asciiSb.Clear();
        }
    }

    HexDecorationKind GetKindForByte(Int32 byteOffset) {
        foreach (HexDecorationRange d in _decorations) {
            if (d.Length > 0 && byteOffset >= d.Start && byteOffset < d.End) {
                return d.Kind;
            }
        }
        return HexDecorationKind.None;
    }

    HexDecorationRange[] getDecorationRanges(IHexAsnNode? node) {
        if (_buffer is null) {
            return [];
        }
        if (node is null) {
            return [new HexDecorationRange(0, _buffer.Count, HexDecorationKind.None)];
        }

        Int32 lengthStartOffset = node.Offset + 1;
        Int32 lengthLength = node.PayloadStartOffset - node.Offset - 1; // -1 for tag
        Int32 restStartOffset = node.PayloadStartOffset + node.PayloadLength;
        Int32 restLength = _buffer.Count - restStartOffset;

        return [
            // before selected node
            new HexDecorationRange(0, node.Offset, HexDecorationKind.None),
            // tag
            new HexDecorationRange(node.Offset, 1, HexDecorationKind.Tag),
            // length
            new HexDecorationRange(lengthStartOffset, lengthLength, HexDecorationKind.Length),
            // value
            new HexDecorationRange(node.PayloadStartOffset, node.PayloadLength, HexDecorationKind.Value),
            // after selected node
            new HexDecorationRange(restStartOffset, restLength, HexDecorationKind.None)
        ];
    }

    readonly struct HexDecorationRange(Int32 Start, Int32 Length, HexDecorationKind Kind) {
        public readonly Int32 Start = Start;
        public readonly Int32 Length = Length;

        public Int32 End => Start + Length;   // non-inclusive
        public readonly HexDecorationKind Kind = Kind;
    }
}
struct HexSegment {
    public String HexText;
    public String AsciiText;
    public HexDecorationKind Kind;
}
enum HexDecorationKind {
    None,
    Tag,
    Length,
    Value
}
