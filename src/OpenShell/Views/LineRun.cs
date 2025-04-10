using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Threading;
using OpenShell.Dto;
using OpenShell.Service;
using OpenShell.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Font = OpenShell.Dto.Font;

namespace OpenShell.Views;

/// <summary>
/// 视图中一行中的一部分
/// </summary>
public class LineRun : Control, ICustomHitTest
{
    public bool CheckBackGroundColorIsBaseColor => this.Font.Background.Equals(Font.DefaultFont.Background);

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<LineRun, string>(nameof(Text));



    /// <summary>
    /// IsVirtual StyledProperty definition
    /// </summary>
    public static readonly StyledProperty<bool> IsVirtualProperty =
        AvaloniaProperty.Register<LineRun, bool>(nameof(IsVirtual), false);

    /// <summary>
    /// Gets or sets the IsVirtual property. This StyledProperty 
    /// indicates ....
    /// </summary>
    public bool IsVirtual
    {
        get => this.GetValue(IsVirtualProperty);
        set => SetValue(IsVirtualProperty, value);
    }


    public static readonly StyledProperty<bool> IsBlinkProperty = AvaloniaProperty.Register<LineRun, bool>(nameof(IsBlink), defaultValue: false);

    public bool IsBlink
    {
        get
        {
            return GetValue(IsBlinkProperty);
        }
        set
        {
            SetValue(IsBlinkProperty, value);
        }
    }

    public static readonly StyledProperty<bool> IsSelectProperty = AvaloniaProperty.Register<LineRun, bool>(nameof(IsSelect), defaultValue: false);

    public bool IsSelect
    {
        get
        {
            return GetValue(IsSelectProperty);
        }
        set
        {
            SetValue(IsSelectProperty, value);
        }
    }


    public string Text
    {
        set
        {
            SetValue(TextProperty, value);
        }
        get
        {
            return GetValue(TextProperty);

        }
    }


    public static readonly StyledProperty<Font> FontProperty =
        AvaloniaProperty.Register<LineRun, Font>(nameof(Font), Font.CreateDefaultFont());


    public int Index
    {
        get
        {
            return GetValue(IndexProperty);
        }
        set
        {
            SetValue(IndexProperty, value);
        }
    }

    public static readonly StyledProperty<int> IndexProperty =
        AvaloniaProperty.Register<LineRun, int>(nameof(Index), 0);

    public Rect Rect { get; set; }
    public Point Point { get; set; }
    /// <summary>
    /// 字体
    /// </summary>
    public Font Font
    {
        get
        {
            return GetValue(FontProperty);
        }
        set
        {
            SetValue(FontProperty, value);
        }
    }

    public DrawingContext DrawingContext { get; set; }

    public LineRun()
    {
        var c = this.DataContext;
        this.Cursor = new Cursor(StandardCursorType.Ibeam);
        var typeface = new Typeface(FontFamily.Default, Font.Italic ? FontStyle.Italic : FontStyle.Normal,
            Font.Bold ? FontWeight.Bold : FontWeight.Normal, FontStretch.Normal);
        var emptyFormattedText = new FormattedText("a", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            typeface, fontSize, new SolidColorBrush(Colors.Red));

        this.Width = emptyFormattedText.Width;
        this.Height = emptyFormattedText.Height;

        this.PropertyChanged += LineRun_PropertyChanged; ;
    }

    private DateTime? LastD;
    private Guid Id = Guid.NewGuid();
    private Task tt;
    private int renderingCount = 0;
    private int propertyChangeRenderingCount = 0;
    private List<string> propertyChangeName = new List<string>();
    private void LineRun_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        var propertyName = e.Property.Name;
        //Debug.WriteLine("属性变化了"+propertyName);
        if (propertyName == nameof(IsBlink))
        {
            if (this.IsBlink)
            {
                Dispatcher.UIThread.Post((async () =>
                {

                    try
                    {
                        //LastD = DateTime.Now;
                        //Debug.WriteLine(Id + LastD.Value.ToString("yyyy-MM-dd mm-hh-ss fff") + "===" +
                        //                this.IsBlink);
                        while (true)
                        {

                            if (this.IsBlink == false)
                            {
                                break;
                            }

                            //propertyChangeRenderingCount++;
                            this.InvalidateVisual();

                            await Task.Delay(500);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                }));
            }
            else
            {
                this.InvalidateVisual();
            }
        }
        else
        {
            // "Index",
            var ownerProperties = new List<string>() { "IsVirtual", "IsBlink", "Text", "Font", "IsSelect" };

            if (ownerProperties.Contains(propertyName))
            {
                if (debouncer == null)
                {
                    debouncer = new DeBouncer();
                }
                debouncer.DeBounce((() =>
                {
                    Dispatcher.UIThread.Invoke((() =>
                    {
                        propertyChangeRenderingCount++;
                        var f = propertyName.PadRight(20, ' ');
                        if (e.OldValue != null)
                        {
                            f += e.OldValue.ToString().PadRight(10, ' ');
                        }
                        else
                        {
                            f += " ".PadRight(10, ' ');
                        }

                        f += e.NewValue != null ? e.NewValue.ToString() : "空";
                        this.propertyChangeName.Add(f);
                        this.InvalidateVisual();
                    }));

                }), 20);
            }
        }
    }

    private DeBouncer debouncer;

    private bool currentBlinkValue = false;

    private FormattedText emptyFormattedText;

    private int fontSize = 20;

    public void InternalRender()
    {
        //Debug.WriteLine(DateTime.Now+"---"+this.Id);
        var typeface = new Typeface(FontFamily.Default, Font.Italic ? FontStyle.Italic : FontStyle.Normal,
            Font.Bold ? FontWeight.Bold : FontWeight.Normal, FontStretch.Normal);
        //var formattedText = new FormattedText(Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
        //    typeface, 20, new SolidColorBrush(Font.Foreground));

        var formattedText = new FormattedText(Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            typeface, fontSize, new SolidColorBrush(Font.Foreground));
        //Debug.WriteLine("颜色为"+Font.Foreground);
        var fontWidth = formattedText.Width;
        var fontHeight = formattedText.Height;
        if (string.IsNullOrWhiteSpace(Text) || IsVirtual)
        {
            if (emptyFormattedText == null)
            {
                emptyFormattedText = new FormattedText("a", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    typeface, fontSize, new SolidColorBrush(Colors.Red));
            }

            fontWidth = emptyFormattedText.Width;
            fontHeight = emptyFormattedText.Height;
        }

        this.Point = new Point(0, 0);
        //this.Rect = new Rect(0, 0, 0 + fontWidth + 2, 0 + fontHeight);
        this.Rect = new Rect(0, 0, 0 + fontWidth + 2, 0 + fontHeight);
        //Debug.WriteLine(Text+"颜色为"+Font.Background);
        if (Font.Background.Equals(Colors.Blue))
        {
            var c = 123;
        }
        var decorations = new TextDecorationCollection();
        if (Font.Underline)
        {
            decorations.AddRange(TextDecorations.Underline);
        }
        if (Font.StrikeThrough)
        {
            decorations.AddRange(TextDecorations.Strikethrough);
        }

        if (decorations.Count > 0)
        {
            formattedText.SetTextDecorations(decorations);
        }

        var sd = new SolidColorBrush(Font.Background);

        if (this.DataContext is LineRunDto lineRunDto && lineRunDto.IsSelect)
        {
            //Debug.WriteLine("IsBlue"+ Font.Background.Equals(Colors.Blue));
            sd = new SolidColorBrush(Colors.Blue);
        }

        DrawingContext.FillRectangle(sd, Rect);
        if (!this.IsVirtual)
        {
            DrawingContext.DrawText(formattedText, Point);
        }


        if (this.IsBlink)
        {
            if (currentBlinkValue)
            {
                //var i = -2;
                //DrawingContext.DrawLine(new Pen(new SolidColorBrush(Colors.Black)), new Point(0, i + fontHeight), new Point(fontWidth, i + fontHeight));
                for (int i = -4; i < -1; i++)
                {
                    DrawingContext.DrawLine(new Pen(new SolidColorBrush(Font.Foreground)), new Point(0, i + fontHeight), new Point(fontWidth, i + fontHeight));
                }

            }
            currentBlinkValue = !currentBlinkValue;
        }

    }
    public sealed override void Render(DrawingContext context)
    {
        if (!IsVirtual)
        {
            this.renderingCount++;
        }
       
        if (this.renderingCount > 2 && IsBlink==false)
        {
            //// 创建一个新的StackTrace对象
            //StackTrace stackTrace = new StackTrace();

            //// 打印堆栈帧信息
            //Console.WriteLine("Call Stack:");
            //foreach (StackFrame frame in stackTrace.GetFrames())
            //{
            //    // 获取方法信息
            //    var method = frame.GetMethod();
            //    Debug.WriteLine($"Method: {method.Name}, Declaring Type: {method.DeclaringType}");
            //}
            var c = Text;
            //Debug.WriteLine($"我被渲染了{renderingCount}次,其中属性变化{this.propertyChangeRenderingCount}次，变化的属性值为{propertyChangeName.StringJoin()}");
        }
        this.DrawingContext = context;
        var t1 = DateTime.Now;
        this.InternalRender();
        var t12 = (DateTime.Now - t1).TotalMilliseconds;
        ScreenPanelVM.All.Add(t12);
        //base.Render(context);
    }


    private Timer timer;
    public bool HitTest(Point point)
    {
        return true;
        throw new NotImplementedException();
    }
}