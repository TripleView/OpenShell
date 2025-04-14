using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Interactivity;
using DynamicData;

namespace OpenShell.Views;

public class ScreenView : StackPanel, ILogicalScrollable
{
    private bool canHorizontallyScroll;
    private bool canVerticallyScroll;
    private Size scrollSize;
    private Size pageScrollSize;



    private Size extent;

    private Size viewport;

    private Vector offset;

    private EventHandler? scrollInvalidated;

    private List<Button> btns = new List<Button>();

    protected override void OnLoaded(RoutedEventArgs e)
    {
        this.Width = this.Bounds.Width;
        this.Height = this.Bounds.Height;
        for (int i = 0; i < 1000; i++)
        {
            var btn = new Button() { Content = i.ToString() };
            //((ISetLogicalParent)btn).SetParent(this);
            btns.Add(btn);
            //this.Children.Add(btn);
        }
        this.Children.AddRange(btns.Take(31));
        //this.InvalidateArrange();
        base.OnLoaded(e);
    }

    public ScreenView()
    {
        this.extent = new Size(1, 1000);
        this.scrollSize = new Size(1, 1);
        this.viewport = new Size(1, 31);
        
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
            this.Children.Clear();
            this.Children.AddRange(btns.Skip((int)value.Y).Take(31));
            Debug.WriteLine("当前offset为"+value);
        }
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