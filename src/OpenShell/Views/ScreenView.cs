using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicData;

namespace OpenShell.Views;
/// <summary>
/// 逻辑滚动（实现 ILogicalScrollable）列表共有 100 项，视口可显示 10 项 ⇒ Extent=100，Viewport=10，Offset 在 0..90 之间；PageDown 往往加 10。
/// </summary>
public class ScreenView :Control, ILogicalScrollable
{
    private bool canHorizontallyScroll;
    private bool canVerticallyScroll;
    /// <summary>
    /// 步长。执行一次“细粒度滚动”（如箭头键、滚轮一档、LineUp/LineDown）时，Offset 增减的量（Width 作用于水平，Height 作用于垂直）。
    /// </summary>
    private Size scrollSize;
    /// <summary>
    /// “翻页”步长（PageUp/PageDown 时 Offset 的增量），一般可设为 Viewport。
    /// </summary>
    private Size pageScrollSize;


    /// <summary>
    /// 整个可滚动内容的总尺寸。
    /// </summary>
    private Size extent;
    /// <summary>
    /// 当前可见区域的尺寸（“窗口有多大”）。
    /// </summary>
    private Size viewport;
    /// <summary>
    /// 视口左上角在内容中的位置（“滚到哪里了”）。
    /// </summary>
    private Vector offset;

    private EventHandler? scrollInvalidated;

    private List<Button> btns = new List<Button>();
    private List<Button> showBtns = new List<Button>();
    private int btnCount = 1000000;
    protected override void OnLoaded(RoutedEventArgs e)
    {
        this.Width = this.Bounds.Width;
        this.Height = this.Bounds.Height;
        for (int i = 0; i < btnCount; i++)
        {
            var btn = new Button() { Content = i.ToString() };
            btn.Width = 200;
            btn.Height = 40;
            btn.IsVisible = true;
            btn.ZIndex = 999;
            //((ISetLogicalParent)btn).SetParent(this);
            btns.Add(btn);
            //this.Children.Add(btn);
        }

        this.Offset = new Vector(0, 0);
        //RefreshUi();
        //showBtns = btns.Take(31).ToList();
        //this.VisualChildren.AddRange(showBtns);

        base.OnLoaded(e);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var y = 0;
        foreach (var child in this.VisualChildren)
        {
            ((Control)child).Arrange(new Rect(0, y, 200, 40));
            y += 40;
        }

        return finalSize;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var y = 0;
        foreach (var child in VisualChildren)
        {
            ((Control)child).Measure(new Size(200, 40));
            y += 40;
        }

        return availableSize;
    }

    public ScreenView()
    {
        this.extent = new Size(1, btnCount);
        this.scrollSize = new Size(0, 1);
        this.viewport = new Size(1, 31);
        this.Offset = new Vector(0, 0);
        this.pageScrollSize= new Size(0, 31);
    }

    public bool CanHorizontallyScroll
    {
        get => canHorizontallyScroll;
        set
        {
            this.canHorizontallyScroll = value;
        }
    }
    
    public bool CanVerticallyScroll
    {
        get => canVerticallyScroll;
        set
        {
            this.canVerticallyScroll = value;
        }
    }

    public bool IsLogicalScrollEnabled => true;

    

    public Size ScrollSize => scrollSize;

    public Size PageScrollSize => pageScrollSize;

    public Size Extent => extent;

    public Vector Offset
    {
        get => offset;
        set
        {
            this.offset = value;
            this.VisualChildren.Clear();
            showBtns = btns.Skip((int)value.Y).Take(31).ToList();
            //showBtns = btns.Skip((int)value.Y).Take(1).ToList();
            //showBtns.FirstOrDefault().Background = new SolidColorBrush(Colors.Red);
            //showBtns.FirstOrDefault().Content = "123333333";
            this.VisualChildren.AddRange(showBtns);
            this.LogicalChildren.AddRange(showBtns);
            RefreshUi();
            Debug.WriteLine("当前offset为"+value);
        }
    }

    private void RefreshUi()
    {
        this.InvalidateMeasure();
        this.InvalidateArrange();
        RaiseScrollInvalidated(EventArgs.Empty);
    }

    public Size Viewport => viewport;

    public event EventHandler? ScrollInvalidated
    {
        add=> scrollInvalidated += value;
        remove=> scrollInvalidated -= value;
    }

    /// <summary>
    /// 滚动到指定位置
    /// </summary>
    /// <param name="target"></param>
    /// <param name="targetRect"></param>
    /// <returns></returns>
    public bool BringIntoView(Control target, Rect targetRect)
    {
        return false;
    }

    public Control? GetControlInDirection(NavigationDirection direction, Control? from)
    {
        return null;
    }

    public void RaiseScrollInvalidated(EventArgs e)
    {
        this.scrollInvalidated?.Invoke(this,e);
    }
}