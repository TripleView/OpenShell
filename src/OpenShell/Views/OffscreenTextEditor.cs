using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.Utilities;
using System;

namespace Demo.Controls
{
    public class OffscreenTextEditor : Control
    {
        private string _text = "Hello Avalonia!\n这是一个简易    文本编辑示例。\n试试鼠标拖动选择，或键盘输入。\n滚轮滚动，大文本也能移动视口。\nIME 预编辑显示需连接输入法回调到 SetImePreedit/CommitImeText。";
        private int _selStart = 0; // 插入点/选择起点
        private int _selEnd = 0;   // 选择终点
        private bool _captured;
        private bool _offscreenDirty = true;

        // 离屏
        private RenderTargetBitmap? _offscreen;

        // 布局与样式
        private readonly Typeface _typeface = new Typeface("Cascadia Mono, Consolas, monospace");
        private readonly double _fontSize = 16;
        private readonly IBrush _foreground = Brushes.White;
        private readonly IBrush _background = Brushes.Black;
        private readonly IBrush _selectionBrush = Brushes.DodgerBlue;

        // 光标
        private bool _caretVisible = true;
        private readonly TimeSpan _caretBlinkInterval = TimeSpan.FromMilliseconds(500);
        private DispatcherTimer? _caretBlinkTimer;

        // 滚动（虚拟视口）
        private double _verticalOffset = 0.0; // 视口顶部在内容中的位置（像素）
        private const double WheelScrollPixels = 40.0;

        // IME 预编辑（组合输入）展示
        private string? _imePreeditText;     // 当前预编辑文本（未提交）
        private int _imePreeditCaret = 0;    // 预编辑文本内的光标位置（相对预编辑字符串）
        private readonly IBrush _imePreeditBrush = Brushes.White; // 预编辑文本颜色
        private readonly Pen _imePreeditUnderlinePen = new Pen(Brushes.White, 1);

        public OffscreenTextEditor()
        {
            Focusable = true;
            ClipToBounds = true;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            StartCaretBlink();
            InvalidateOffscreen();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            StopCaretBlink();
            _offscreen?.Dispose();
            _offscreen = null;
        }

        protected override void OnGotFocus(Avalonia.Input.FocusChangedEventArgs e)
        {
            base.OnGotFocus(e);
            StartCaretBlink();
            ResetCaretBlink();
        }

        protected  void OnLostFocus(FocusChangedEventArgs e)
        {
            base.OnLostFocus(e);
            StopCaretBlink();
            _caretVisible = false;
            InvalidateVisual();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == BoundsProperty)
            {
                InvalidateOffscreen();
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value ?? string.Empty;
                    ClampSelection();
                    InvalidateOffscreen();
                    InvalidateVisual();
                }
            }
        }

        public int SelectionStart
        {
            get => _selStart;
            set
            {
                if (_selStart != value)
                {
                    _selStart = Math.Clamp(value, 0, _text.Length);
                    InvalidateOffscreen();
                    InvalidateVisual();
                    ScrollCaretIntoView();
                }
            }
        }

        public int SelectionEnd
        {
            get => _selEnd;
            set
            {
                if (_selEnd != value)
                {
                    _selEnd = Math.Clamp(value, 0, _text.Length);
                    InvalidateOffscreen();
                    InvalidateVisual();
                    ScrollCaretIntoView();
                }
            }
        }

        private void InvalidateOffscreen()
        {
            _offscreenDirty = true;
            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            EnsureOffscreen();
            if (_offscreen is not null)
            {
                context.DrawImage(_offscreen, new Rect(Bounds.Size));
            }
        }

        private void EnsureOffscreen()
        {
            if (!_offscreenDirty || Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            _offscreenDirty = false;

            var scale = Avalonia.Controls.TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
            var px = new PixelSize(
                Math.Max(1, (int)Math.Ceiling(Bounds.Width * scale)),
                Math.Max(1, (int)Math.Ceiling(Bounds.Height * scale)));

            _offscreen?.Dispose();
            _offscreen = new RenderTargetBitmap(px);

            using var ctx = _offscreen.CreateDrawingContext(true);

            // 背景
            ctx.DrawRectangle(_background, null, new Rect(Bounds.Size));

            // 文本布局（内容宽度限制为视口宽度）
            var layout = CreateLayout(Bounds.Width);

            // 将绘制上下文裁剪到视口，并向上平移 verticalOffset（虚拟滚动）
            ctx.PushClip(new Rect(Bounds.Size));
            ctx.PushTransform(Matrix.CreateTranslation(0, -_verticalOffset));

            // 选区绘制
            var (start, length) = GetNormalizedSelection();
            if (length > 0)
            {
                var rects = layout.HitTestTextRange(start, length);
                foreach (var r in rects)
                {
                    ctx.DrawRectangle(_selectionBrush, null, r);
                }
            }

            // 文本绘制
            layout.Draw(ctx, new Point(0, 0));

            // IME 预编辑文本（在插入点位置绘制，带下划线）
            if (!string.IsNullOrEmpty(_imePreeditText))
            {
                var caretRect = GetCaretRect(layout, _selStart);
                var preeditLayout = new TextLayout(
                    text: _imePreeditText,
                    typeface: _typeface,
                    fontSize: _fontSize,
                    foreground: _imePreeditBrush,
                    textAlignment: TextAlignment.Left,
                    TextWrapping.NoWrap,
                    TextTrimming.None,
                    textDecorations: null,
                    maxWidth: Bounds.Width
                );

                var drawPoint = new Point(caretRect.X, caretRect.Y);
                preeditLayout.Draw(ctx, drawPoint);

                // 下划线（预编辑）
                var underlineY = drawPoint.Y + preeditLayout.Height - 1;
                var underlineEndX = drawPoint.X + preeditLayout.Width;
                ctx.DrawLine(_imePreeditUnderlinePen, new Point(drawPoint.X, underlineY), new Point(underlineEndX, underlineY));

                // 可选：预编辑内光标
                if (_imePreeditCaret > 0 && _imePreeditCaret <= _imePreeditText.Length)
                {
                    var preCaretRect = preeditLayout.HitTestTextPosition(_imePreeditCaret);
                    DrawCaret(ctx, new Rect(preCaretRect.X + drawPoint.X, preCaretRect.Y + drawPoint.Y, 1, preCaretRect.Height));
                }
            }

            // 插入光标绘制（无选区且控件有焦点、IME未占用时）
            if (length == 0 && IsFocused && _imePreeditText is null && _caretVisible)
            {
                var caretRect = GetCaretRect(layout, _selStart);
                DrawCaret(ctx, caretRect);
            }

            //ctx.PopTransform();
            //ctx.PopClip();

            // 可选：绘制一个简单竖向滚动条（内容高度超过视口时）
            DrawSimpleScrollbar(ctx, layout);
        }

        private TextLayout CreateLayout(double maxWidth)
        {
            var overrides = new[]
            {
                new ValueSpan<TextRunProperties>(
                    start: 0, length: 1,
                    value: new GenericTextRunProperties(
                        new Typeface("Segoe UI"), 16,foregroundBrush:Brushes.Red,  backgroundBrush: null, textDecorations: TextDecorations.Underline)),
                new ValueSpan<TextRunProperties>(
                    start: 1, length: 1,
                    value: new GenericTextRunProperties(
                        new Typeface("Consolas"), 16,foregroundBrush:Brushes.Blue)),
                new ValueSpan<TextRunProperties>(
                    start: 2, length: 1,
                    value: new GenericTextRunProperties(
                        new Typeface("Times New Roman"), 16))
            };
            return new TextLayout(
                text: _text ?? string.Empty,
                typeface: _typeface,
                fontSize: _fontSize,
                foreground: _foreground,
                textAlignment: TextAlignment.Left,
                TextWrapping.Wrap,
                 TextTrimming.None,
                textDecorations: null,
                maxWidth: maxWidth,
                textStyleOverrides: overrides
            );
        }

        private (int start, int length) GetNormalizedSelection()
        {
            var s = Math.Clamp(_selStart, 0, _text.Length);
            var e = Math.Clamp(_selEnd, 0, _text.Length);
            if (s <= e) return (s, e - s);
            return (e, s - e);
        }

        private void ClampSelection()
        {
            _selStart = Math.Clamp(_selStart, 0, _text.Length);
            _selEnd = Math.Clamp(_selEnd, 0, _text.Length);
        }

        private Rect GetCaretRect(TextLayout layout, int textIndex)
        {
            // TextLayout.HitTestTextPosition 返回该字符位置的矩形（可能在某些小版本需替换为其他命名）
            var r = layout.HitTestTextPosition(Math.Clamp(textIndex, 0, _text.Length));
            // 宽度使用1像素线更符合编辑器视觉
            return new Rect(r.X, r.Y, 1, r.Height);
        }

        private void DrawCaret(DrawingContext ctx, Rect r)
        {
            // 竖线型插入光标
            ctx.DrawRectangle(Brushes.White, null, r);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            Focus();

            var p = e.GetPosition(this);
            var idx = HitTestToTextIndex(p);
            _selStart = _selEnd = idx;

            _captured = true;
            e.Pointer.Capture(this);

            ResetCaretBlink();
            ScrollCaretIntoView();

            InvalidateOffscreen();
            InvalidateVisual();
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (_captured)
            {
                var p = e.GetPosition(this);
                _selEnd = HitTestToTextIndex(p);
                InvalidateOffscreen();
                InvalidateVisual();
                e.Handled = true;
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (_captured)
            {
                _captured = false;
                e.Pointer.Capture(null);
                e.Handled = true;
            }
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            var deltaY = e.Delta.Y; // 向上为正，向下为负（如有相反，可取 -e.Delta.Y）
            ScrollBy(-deltaY * WheelScrollPixels);
            e.Handled = true;
        }

        private int HitTestToTextIndex(Point p)
        {
            var layout = CreateLayout(Bounds.Width);
            // 把指针位置转换到内容坐标（加上 verticalOffset）
            var hit = layout.HitTestPoint(new Point(p.X, p.Y + _verticalOffset));
            var idx = hit.TextPosition;
            return Math.Clamp(idx, 0, _text.Length);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            // 若存在预编辑文本，通常输入法会先清除预编辑再提交最终文本。
            // 这里先清掉预编辑展示。
            if (!string.IsNullOrEmpty(_imePreeditText))
            {
                ClearImePreedit();
            }

            if (string.IsNullOrEmpty(e.Text))
                return;

            DeleteSelectionIfAny();
            InsertTextAtCaret(e.Text);

            ResetCaretBlink();
            ScrollCaretIntoView();

            InvalidateOffscreen();
            InvalidateVisual();
            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            bool changed = false;

            switch (e.Key)
            {
                case Key.Back:
                    if (!DeleteSelectionIfAny())
                    {
                        if (_selStart > 0)
                        {
                            _text = _text.Remove(_selStart - 1, 1);
                            _selStart--;
                            _selEnd = _selStart;
                            changed = true;
                        }
                    }
                    e.Handled = true;
                    break;

                case Key.Delete:
                    if (!DeleteSelectionIfAny())
                    {
                        if (_selStart < _text.Length)
                        {
                            _text = _text.Remove(_selStart, 1);
                            _selEnd = _selStart;
                            changed = true;
                        }
                    }
                    e.Handled = true;
                    break;

                case Key.Left:
                    if (_selStart > 0)
                    {
                        _selStart--;
                        if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                            _selEnd = _selStart;
                        changed = true;
                    }
                    e.Handled = true;
                    break;

                case Key.Right:
                    if (_selStart < _text.Length)
                    {
                        _selStart++;
                        if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                            _selEnd = _selStart;
                        changed = true;
                    }
                    e.Handled = true;
                    break;

                case Key.Home:
                    _selStart = 0;
                    if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                        _selEnd = _selStart;
                    changed = true;
                    e.Handled = true;
                    break;

                case Key.End:
                    _selStart = _text.Length;
                    if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                        _selEnd = _selStart;
                    changed = true;
                    e.Handled = true;
                    break;

                // 滚动相关快捷键
                case Key.PageUp:
                    ScrollBy(-Bounds.Height * 0.9);
                    e.Handled = true;
                    break;

                case Key.PageDown:
                    ScrollBy(Bounds.Height * 0.9);
                    e.Handled = true;
                    break;

                case Key.Space:
                    // 示例：当有预编辑时，避免闪烁（根据需要处理）
                    break;
            }

            if (changed)
            {
                ResetCaretBlink();
                ScrollCaretIntoView();
                InvalidateOffscreen();
                InvalidateVisual();
            }
        }

        private bool DeleteSelectionIfAny()
        {
            var (start, length) = GetNormalizedSelection();
            if (length > 0)
            {
                _text = _text.Remove(start, length);
                _selStart = _selEnd = start;
                return true;
            }
            return false;
        }

        private void InsertTextAtCaret(string s)
        {
            var (start, length) = GetNormalizedSelection();
            if (length > 0)
            {
                _text = _text.Remove(start, length);
                _selStart = _selEnd = start;
            }

            _text = _text.Insert(_selStart, s);
            _selStart += s.Length;
            _selEnd = _selStart;
        }

        private void StartCaretBlink()
        {
            _caretBlinkTimer ??= new DispatcherTimer { Interval = _caretBlinkInterval };
            _caretBlinkTimer.Tick += CaretBlinkTimer_Tick;
            _caretBlinkTimer.Start();
            _caretVisible = true;
        }

        private void StopCaretBlink()
        {
            if (_caretBlinkTimer is not null)
            {
                _caretBlinkTimer.Tick -= CaretBlinkTimer_Tick;
                _caretBlinkTimer.Stop();
            }
        }

        private void CaretBlinkTimer_Tick(object? sender, EventArgs e)
        {
            if (IsFocused && _imePreeditText is null)
            {
                _caretVisible = !_caretVisible;
                InvalidateVisual();
            }
        }

        private void ResetCaretBlink()
        {
            _caretVisible = true;
            // 让下一次 Tick 再开始闪烁
        }

        private void ScrollBy(double dy)
        {
            var layout = CreateLayout(Bounds.Width);
            var contentHeight = layout.Height;

            if (contentHeight <= Bounds.Height)
            {
                _verticalOffset = 0;
            }
            else
            {
                _verticalOffset = Math.Clamp(_verticalOffset + dy, 0, contentHeight - Bounds.Height);
            }

            InvalidateOffscreen();
            InvalidateVisual();
        }

        private void ScrollCaretIntoView()
        {
            var layout = CreateLayout(Bounds.Width);
            var caretRect = GetCaretRect(layout, _selStart);

            // 把 caret 的 y 可见化（考虑 verticalOffset）
            var visibleTop = _verticalOffset;
            var visibleBottom = _verticalOffset + Bounds.Height;

            if (caretRect.Top < visibleTop)
                _verticalOffset = caretRect.Top;
            else if (caretRect.Bottom > visibleBottom)
                _verticalOffset = caretRect.Bottom - Bounds.Height;

            // Clamp
            var contentHeight = layout.Height;
            if (contentHeight <= Bounds.Height)
                _verticalOffset = 0;
            else
                _verticalOffset = Math.Clamp(_verticalOffset, 0, contentHeight - Bounds.Height);
        }

        private void DrawSimpleScrollbar(DrawingContext ctx, TextLayout layout)
        {
            var contentHeight = layout.Height;
            if (contentHeight <= Bounds.Height)
                return;

            var trackWidth = 6;
            var padding = 2;
            var trackRect = new Rect(Bounds.Width - trackWidth - padding, padding, trackWidth, Bounds.Height - padding * 2);

            // 滑块高度与位置
            var viewportRatio = Bounds.Height / contentHeight;
            var thumbHeight = Math.Max(20, trackRect.Height * viewportRatio);
            var scrollRatio = _verticalOffset / (contentHeight - Bounds.Height);
            var thumbY = trackRect.Y + (trackRect.Height - thumbHeight) * scrollRatio;

            var trackBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
            var thumbBrush = new SolidColorBrush(Color.FromArgb(160, 30, 144, 255)); // DodgerBlue 类似

            ctx.DrawRectangle(trackBrush, null, trackRect);
            ctx.DrawRectangle(thumbBrush, null, new Rect(trackRect.X, thumbY, trackRect.Width, thumbHeight));
        }

        // =============== IME 组合输入（预编辑）支持（最小逻辑） ===============

        // 在你的输入法/平台回调中调用此方法设置预编辑文本与其内部光标位置。
        public void SetImePreedit(string? preeditText, int caretInPreedit = 0)
        {
            _imePreeditText = preeditText;
            _imePreeditCaret = Math.Clamp(caretInPreedit, 0, preeditText?.Length ?? 0);
            ResetCaretBlink();
            InvalidateOffscreen();
            InvalidateVisual();
        }

        // 清除预编辑显示（比如输入法结束预编辑或开始提交）
        public void ClearImePreedit()
        {
            _imePreeditText = null;
            _imePreeditCaret = 0;
            ResetCaretBlink();
            InvalidateOffscreen();
            InvalidateVisual();
        }

        // 在输入法最终提交文本时调用（通常也会触发 OnTextInput，二者择一即可）
        public void CommitImeText(string text)
        {
            ClearImePreedit();

            DeleteSelectionIfAny();
            InsertTextAtCaret(text);

            ResetCaretBlink();
            ScrollCaretIntoView();

            InvalidateOffscreen();
            InvalidateVisual();
        }

        // ================================================================
    }
}