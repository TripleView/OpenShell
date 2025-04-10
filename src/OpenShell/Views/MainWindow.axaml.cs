using System;
using System.Diagnostics;
using System.Globalization;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicData;
using Newtonsoft.Json;
using ReactiveUI;
using OpenShell.Dto;
using OpenShell.ViewModels;

namespace OpenShell.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.AttachDevTools();
        this.AddHandler(InputElement.TextInputEvent, TextInputHandler, RoutingStrategies.Bubble);
        this.Activated += MainWindow_Activated;
        this.Loaded +=async (s,e)=>await MainWindow_Loaded(s,e);
        //由于窗口尺寸变化会导致频繁触发sizechange事件，所以这里采取一个防抖的技术
        //var sizeChangeObservable = Observable.FromEventPattern<SizeChangedEventArgs>(
        //    handler => this.SizeChanged += handler,
        //    handler => this.SizeChanged -= handler);
        //sizeChangeObservable.Throttle(TimeSpan.FromMilliseconds(500))
        //    .Subscribe(e =>
        //    {
        //        MainWindow_SizeChanged(e.Sender, e.EventArgs);
        //    });

       
    }

    private bool isLoad = false;

    private void MainWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        Dispatcher.UIThread.Post((() =>
        {
            Debug.WriteLine("大小变化了");
            if (isLoad)
            {
                RecalculateClientHeightAndWidth();
                screenPanelVm.ResetClientWidthAndHeight();
            }
        }));


    }

    private async Task MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        RecalculateClientHeightAndWidth();

        await screenPanelVm.InitSsh();
        screenPanelVm.ScrollToButtonChanged += ScreenPanelVm_ScrollToButtonChanged;
        isLoad = true;
    }

    private void MainWindow_Activated(object? sender, System.EventArgs e)
    {
        //RecalculateClientHeightAndWidth();

        //screenPanelVm.InitSsh();
        //screenPanelVm.ScrollToButtonChanged += ScreenPanelVm_ScrollToButtonChanged;
    }

    private void RecalculateClientHeightAndWidth()
    {
        screenPanelVm.ClientHeight = this.Bounds.Height;

        screenPanelVm.ClientWidth =this.Bounds.Width;
        var virtualLineRunSize = GetVirtualLineRunSize();
        var rows =(int) Math.Floor(this.Bounds.Height / virtualLineRunSize.Height);
        var columns =(int) Math.Floor(this.Bounds.Width / virtualLineRunSize.Width);
        screenPanelVm.ClientRows = rows;
        screenPanelVm.ClientColumns = columns;
    }

    private Size GetVirtualLineRunSize()
    {
        var t = new LineRun();
        t.DataContext = new LineRunDto()
        {
            Font = Font.CreateDefaultFont(),
            Text = "a",
            IsVirtual = false
        };
        // 假设可用的最大大小
        Size availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

        // 3. 测量控件
        t.Measure(availableSize);

        // 4. 排列控件
        t.Arrange(new Rect(t.DesiredSize));

        // 5. 获取实际大小
        Size actualSize = t.DesiredSize;
        return actualSize;
    }

    private void ScreenPanelVm_ScrollToButtonChanged(object? sender, System.EventArgs e)
    {
        Dispatcher.UIThread.Post((() =>
        {
            var scrollViewer = this.FindControl<ScrollViewer>("ScrollViewer");

            if (scrollViewer != null)
            {
                scrollViewer.ScrollToEnd();
                //c.Offset = new Vector(c.Offset.X, c.Extent.Height);
            }
        }));
        
    }

    private ScreenPanelVM screenPanelVm
    {
        get => DataContext as ScreenPanelVM;
    }

    private void ScrollToBottom()
    {

    }

    private void TextInputHandler(object? sender, TextInputEventArgs e)
    {
        //if (string.IsNullOrWhiteSpace(e.Text))
        //{
        //    return;
        //}
        var txt = e.Text;
       
        foreach (var ch in txt)
        {
            if (!char.IsControl(ch) || ch == 27 || ch == 8 || ch == 13)
            {
                screenPanelVm.Send(ch);
            }
            else
            {
                Debugger.Break();
            }
        }

        e.Handled = true;
        // Handler code
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        //Debug.WriteLine(e.Key);
        var result = screenPanelVm.SendKey(e.Key, e.KeyModifiers);
        e.Handled = result;
    }

}
