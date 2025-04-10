using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using ReactiveUI;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using DynamicData;
using Newtonsoft.Json;
using OpenShell.Dto;
using OpenShell.Service;

namespace OpenShell.ViewModels;

public class ScreenPanelVM : ViewModelBase
{
    public static List<double> All { get; set; } = new List<double>();
    public ObservableCollection<LineDto> Lines { set; get; }

    private ShellStream shellStream;
    private SemaphoreSlim semaphore = new SemaphoreSlim(1);

    /// <summary>
    /// 界面的宽度
    /// </summary>
    public double ClientWidth { get; set; }

    /// <summary>
    /// 界面的高度
    /// </summary>
    public double ClientHeight { get; set; }
    /// <summary>
    /// 界面行数
    /// </summary>
    public int ClientRows { get; set; }
    /// <summary>
    /// 界面列数
    /// </summary>
    public int ClientColumns { get; set; }
    /// <summary>
    /// 用于控制终端自动换行行为的模式，称为自动换行模式（Auto Wrap Mode）。在终端仿真器中，这个模式决定了当光标到达行末时的行为。
    /// 当启用此模式时，如果光标在行的最后一个位置并且有更多字符要输出，光标会自动移动到下一行的开头。这种行为类似于文本编辑器中的自动换行功能。
    /// </summary>
    public bool AutoWrapMode { get; set; }

    /// <summary>
    /// 是否开启应用程序光标键模式,
    ///在正常模式下，光标键发送标准的 ANSI 控制序列。
    ///例如，向上箭头键通常发送 ESC [A。
    ///Application Cursor Keys Mode（应用程序光标键模式）：
    ///在这个模式下，光标键发送不同的控制序列，通常以 ESC O 开头。
    ///例如，向上箭头键在应用程序模式下可能发送 ESC O A
    /// </summary>
    public bool IsApplicationCursorKeysMode { get; set; } = false;

    public Font Font { set; get; }

    /// <summary>
    /// 是否展示光标
    /// </summary>
    public bool IsShowCursor;
    /// <summary>
    /// 是否设置滚动区域
    /// </summary>
    private bool IsSetScrollRegion;
    /// <summary>
    /// 滚动起始行
    /// </summary>
    private int ScrollRegionStart;
    /// <summary>
    /// 滚动结束行
    /// </summary>
    private int ScrollRegionEnd;
    /// <summary>
    /// 备份屏幕光标位置
    /// </summary>
    public CursorPosition BackUpCursorPosition { get; set; }
    
    /// <summary>
    /// 备用屏幕缓冲区
    /// </summary>
    public ObservableCollection<LineDto> AlternateScreenBuffer { set; get; } = new ObservableCollection<LineDto>();
    /// <summary>
    /// 主屏幕缓冲区,由终端窗口的当前尺寸（行数和列数）决定。具体来说，主缓冲区的大小通常等于当前可见的终端窗口的行数和列数
    /// 主缓冲区存储的是当前可见的内容。它负责显示用户当前正在交互的内容
    /// 主缓冲区不同于滚动缓冲区。滚动缓冲区用于存储超出当前可见区域的历史内容，允许用户通过滚动查看之前的输出
    /// </summary>
    public ObservableCollection<LineDto> PrimaryScreenBuffer { set; get; } = new ObservableCollection<LineDto>();
    /// <summary>
    /// 基本颜色表
    /// </summary>
    private List<Color> Colors = new List<Color>
    {
        Color.FromRgb(0, 0, 0), // Dull Black
        Color.FromRgb(205, 0, 0), // Dull Red
        Color.FromRgb(0, 205, 0), // Dull Green
        Color.FromRgb(205, 205, 0), // Dull Yellow
        Color.FromRgb(0, 0, 238), // Dull Blue
        Color.FromRgb(205, 0, 205), // Dull Purple
        Color.FromRgb(0, 205, 205), // Dull Cyan
        Color.FromRgb(192, 192, 192), // Dull White
    };

    public void Test()
    {
        var d = All.Sum();
        Debug.WriteLine("我被点击了");
    }

    private BinaryWriter writer;
  
    /// <summary>
    /// 光标位置信息
    /// Cursor position information
    /// </summary>
    public CursorPosition CursorPosition
    {
        get
        {
            return cursorPosition;
        }
        set
        {
            this.cursorPosition = value;

        }
    }

    private CursorPosition cursorPosition = new CursorPosition(1, 1);

    public event EventHandler<TitleChangeEventArgs> TitleChanged;

    public event EventHandler<EventArgs> ScrollToButtonChanged;

    public ScreenPanelVM()
    {
        //var temps = new List<LineDto>();
        //for (int i = 0; i < 400; i++)
        //{
        //    var line = new LineDto() { };
        //    line.AddVirtualLineRuns(40);
        //    for (int j = 0; j < 40; j++)
        //    {
        //        line.AddLineRuns("a", Font.DefaultFont, j+1);
        //    }

        //    temps.Add(line);
        //}
        Lines = new ObservableCollection<LineDto>();
        //var f = new LineDto() { };
        //f.AddVirtualLineRuns(70);
        //var d = string.Join("", Enumerable.Range(1, 70).Select(x => "a").ToList());
        //f.AddLineRuns(d, Font.CreateDefaultFont(), 1);
        //Lines.Add(f);

        Font = Font.CreateDefaultFont();
        //InitSsh();
    }

    public void ResetClientWidthAndHeight()
    {
        shellStream.SendWindowChangeRequest((uint)this.ClientColumns, (uint)this.ClientRows, (uint)this.ClientWidth, (uint)this.ClientHeight);
    }

    /// <summary>
    /// 重置光标位置
    /// </summary>
    private void ReSetCursor()
    {
        if (Lines.Count >= CursorPosition.Row)
        {
            var lineRuns = Lines[CursorPosition.Row - 1].List;
            if (lineRuns.Count >= CursorPosition.Column)
            {
                if (lastCursorLineRun != null)
                {
                    lastCursorLineRun.IsBlink = false;
                }
                var lineRun = lineRuns[CursorPosition.Column - 1];
                lineRun.IsBlink = true;
                lastCursorLineRun = lineRun;
            }
        }
    }

    /// <summary>
    /// 移除光标位置到行尾的所有字符,如果有新文本则增加
    /// </summary>
    private void RemoveCharactersFromCursorToTheEndOfLine()
    {
        try
        {
            var line = Lines[CursorPosition.Row - 1];
            for (int i = CursorPosition.Column-1; i < line.List.Count; i++)
            {
                var lineRun = line.List[i];
                if (!lineRun.IsVirtual)
                {
                    lineRun.ChangeToVirtual();
                }
               
            }
            //line.List = new ObservableCollection<LineRunDto>(line.List.Take(new Range(0, CursorPosition.Column - 1)).ToList());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
       
    }

    /// <summary>
    /// 移除光标位置到光标右边的N个字符
    /// </summary>
    private void RemoveCharactersFromCursorToRight(int number)
    {
        if (Lines.Count >= CursorPosition.Row)
        {
            var line = Lines[CursorPosition.Row - 1];
            for (int i = CursorPosition.Column -1; i < CursorPosition.Column +number; i++)
            {
                var lineRun = line.List[i];
                if (!lineRun.IsVirtual)
                {
                    lineRun.ChangeToVirtual();
                }
            }
            //line.List = new ObservableCollection<LineRunDto>(line.List.Take(new Range(0, CursorPosition.Column - number)).ToList());
            ReSetCursor();
        }
    }
    /// <summary>
    /// 缓存上一个光标位置，这样光标移动后，方便将上一个光标位置置为不闪烁
    /// </summary>
    private LineRunDto lastCursorLineRun;

    public void Lock()
    {
        //if (isEnter)
        //{
        //    semaphore.Wait();
        //    semaphore.Wait();
        //}


    }

    //public void Release()
    //{
    //    semaphore.Release();
    //    this.isEnter = false;
    //}

    public async Task InitSsh()
    {
        //if (!string.IsNullOrEmpty(settings.KeyFilePath))
        //{
        //    var privateKeyFile = new PrivateKeyFile(settings.KeyFilePath, settings.KeyFilePassphrase);
        //    authentications.Add(new PrivateKeyAuthenticationMethod(settings.Username, privateKeyFile));
        //}
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "setting.json"));
        var loginInfoDto = JsonConvert.DeserializeObject<LoginInfoDto>(json);
        var ip = loginInfoDto.Ip;
        var username = loginInfoDto.UserName;
        var password = loginInfoDto.Password;
        if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(password))
        {
            throw new NotSupportedException("ip/username/pasword can not be empty;ip账号密码不能为空");
        }
        var connectionInfo = new ConnectionInfo(ip,
            username,
            new PasswordAuthenticationMethod(username, password));

        var client = new SshClient(connectionInfo);
        client.KeepAliveInterval = TimeSpan.FromSeconds(20);
        //client.KeepAliveInterval = TimeSpan.FromMinutes(1);
        var cts = new CancellationTokenSource();
        await client.ConnectAsync(cts.Token);

        //"xterm-256color" vt-100
        shellStream = client.CreateShellStream("xterm-256color",(uint) ClientColumns, (uint)ClientRows, (uint)ClientWidth, (uint)ClientHeight, 4096);

        //xterm-256color
        //writer = new BinaryWriter(shellStream, Encoding.UTF8, true);
        shellStream.DataReceived += (async (sender, eventArgs) =>
        {
            //var ff = string.Join(';', eventArgs.Data);
            //var ttt = Encoding.UTF8.GetString(eventArgs.Data);
            //var ts=  ttt.Split("\r\n");
            //Debug.WriteLine("接收到的字节数组为:" + ff);
            var charParser = new CharParser();
            //if (ff.StartsWith("27;91;63;49"))
            //{
            //    Debugger.Break();
            //}
            charParser.ReceiveSequence += CharParser_ReceiveSequence;
            charParser.Parse(eventArgs.Data);
        });

        //shellStream.Write('\033');
        //Task.Run((async () =>
        //{
        //    await Task.Delay(10000);
        //    shellStream.Write(Encoding.UTF8.GetBytes("\x1b[6n"));
        //}));
    }

    private object lockObj = new object();

    private string PrintObj(string s)
    {
        if (s.Length == 1)
        {
            if (s[0] == '\r')
            {
                return "回车";
            }
            if (s[0] == '\n')
            {
                return "换行";
            }
            if (s[0] == '\b')
            {
                return "退格";
            }
            if (s[0] == '\a')
            {
                return "响铃";
            }
            if (s[0] == '\t')
            {
                return "制表符";
            }
          
            return s;
        }
        else
        {
            return s;
        }
    }

    private DateTime? time;
    private List<string> consumeTimes = new List<string>();
    private void CharParser_ReceiveSequence(object? sender, ReceivedSequenceEventArgs e)
    {
        time=DateTime.Now;
        //return;
        //Debug.WriteLine("处理:"+e.Type .ToString().PadRight(20,' ')+PrintObj(e.Sequence));
        //lock (lockObj)
        if (true)
        {
            var handleResult = true;
            if (e.Sequence.Contains("root@"))
            {
                //Debugger.Break();
            }
            switch (e.Type)
            {
                case SequenceType.Text://获取光标所在位置https://segmentfault.com/a/1190000040368567

                    var numberOfAddLines= CursorPosition.Row - this.Lines.Count;
                    if (numberOfAddLines>0)
                    {
                        for (int i = 0; i < numberOfAddLines; i++)
                        {
                            this.LineFeed();
                        }
                    }
                    var editLine = this.Lines[CursorPosition.Row - 1];

                    RemoveCharactersFromCursorToTheEndOfLine();
                    var tempFont = Font.Copy();
                    editLine.AddLineRuns(e.Sequence, tempFont, cursorPosition.Column);

                    MoveCursor(CursorMovementType.Right, e.Sequence.Length);
                    //if (this.Lines.Any())
                    //{
                    //    if (isLineBreaks)
                    //    {
                    //        isLineBreaks = false;
                    //        this.AddLine(e.Sequence);
                    //    }
                    //    else
                    //    {

                    //    }
                    //}
                    //else
                    //{
                    //    this.AddLine(e.Sequence);
                    //}

                    return;
                    //
                    break;
                case SequenceType.Csi:
                    //throw new Exception("123");
                    this.HandleCsi(e.Sequence);
                    break;
                case SequenceType.Osc:
                    handleResult = this.handleOsc(e.Sequence);
                    break;
                case SequenceType.SingleEscape:
                    handleSingleEscape(e.Sequence[0]);
                    break;
                case SequenceType.ControlCode:
                    if (e.Sequence.Length == 0)
                    {
                        return;
                    }

                    var firstCharacter = e.Sequence[0];
                    //换行符,用于将光标移动到下一行的同一列
                    if (firstCharacter == '\n')
                    {
                        if (IsSetScrollRegion)
                        {
                            if (CursorPosition.Row == ScrollRegionEnd)
                            {
                                var newLine = new LineDto() { };
                                newLine.AddVirtualLineRuns(ClientColumns);
                                Dispatcher.UIThread.Invoke((() =>
                                {
                                    this.Lines.RemoveAt(ScrollRegionStart-1);
                                    this.Lines.Insert(ScrollRegionEnd - 1,newLine);
                                }));

                                SetLastItem();
                                if (ScrollToButtonChanged != null)
                                {
                                    ScrollToButtonChanged(this, null);
                                }

                                return;
                            }
                        }
                        MoveCursor(CursorMovementType.Down, 1);
                        if (CursorPosition.Row == this.Lines.Count + 1)
                        {
                            this.LineFeed();
                        }
                    }
                    //表示退格（Backspace）字符,它的作用是将光标向左移动一个位置，通常用于删除光标前的一个字符
                    else if (firstCharacter == '\b')
                    {
                        MoveCursor(CursorMovementType.Left, 1);
                    }
                    else if (firstCharacter == '\t')
                    {
                        //todo 待处理tab，未找到场景
                        Debugger.Break();
                        //tab(false);
                    }
                    //编程中，字符 \a 是一个转义序列，表示警报（Alert）字符，也称为响铃（Bell）字符。
                    //在 ASCII 编码中，它的值是 7。这个字符的作用是触发系统的警报声或其他通知机制
                    else if (firstCharacter == '\a')
                    {
                        // Disabled because annoyance
                        //System.Media.SystemSounds.Beep.Play();
                    }
                    // 字符\r 是一个控制字符，表示“回到行首”。在传统的打字机或终端中，回车符会将光标移动到当前行的开头，而不改变行的位置。
                    //在终端中，\r 常用于覆盖当前行的内容。例如，显示进度条或动态更新一行的内容时，程序可能会使用 \r 来回到行首并重写该行。
                    else if (firstCharacter == '\r')
                    {
                        InitCursor(null, 1);
                    }
                    else
                        handleResult = false;
                    break;
            }
        }

        var dt = DateTime.Now;
        var tt = (dt - time.Value).TotalMilliseconds;
        var f1 = tt.ToString().PadRight(20, ' ') + e.Sequence;
        consumeTimes.Add(f1);
        time = dt;
    }

    /// <summary>
    /// CSI 序列以 ESC [ 开头，后跟一个或多个参数，最后是一个字母终止符
    /// </summary>
    /// <param name="sequence"></param>
    /// <returns></returns>
    private bool HandleCsi(string sequence)
    {
        if (sequence == null || sequence.Length == 0)
        {
            return false;
        }
        bool handled = true;

        var isPrivate = sequence[0] == '?';
        var kind = sequence.Last();
        var codes = new List<int>();
        var realSequence = "";
        if (sequence == ">c")
        {
            //发送设备属性（辅助 DA）。Ps = 0 或省略 -> 请求终端的识别码。响应取决于decTerminalID资源设置。它应该仅适用于 VT220 及以上版本，但 xterm 将其扩展到 VT100。
            //发送设备属性的要点是程序正在向终端询问一些内容。终端发回信息，即响应。这恰好是一个类似于请求的字符串（如果终端实际上​​没有连接到主机，这将很有用）。发送请求的程序必须读取响应，否则您将看到终端上打印出奇怪的字符 -响应的未读部分。
        }
        else
        {
            try
            {
                realSequence = isPrivate ? sequence.Substring(1, sequence.Length - 2) : sequence.Substring(0, sequence.Length - 1);

                codes = realSequence.Split(';')
                    .Where(it => !string.IsNullOrWhiteSpace(it))
                    .Select(it => int.Parse(it)).ToList();
            }
            catch (FormatException ex)
            {
                var a = ex.StackTrace;
                return false;
            }
        }
      

        var oldCursorPosition = CursorPosition.Clone();
        if (isPrivate)
        {
            switch (kind)
            {
                //启用某个模式
                case 'h':
                    foreach (int code in codes)
                        handled &= HandleDecSet(code);
                    break;
                //禁用某个模式
                case 'l':
                    foreach (int code in codes)
                        handled &= HandleDecReset(code);
                    break;

                case 'r':
                    //if (sequence[sequence.Length - 2] == ' ')
                    //{
                    //    int cursorType = getAtOrDefault(codes, 0, 1);
                    //    if (cursorType == 3 || cursorType == 4)
                    //        UnderlineCursor = true;
                    //    else
                    //        UnderlineCursor = false;

                    //    if (cursorType == 0 || cursorType == 1 || cursorType == 3 || cursorType == 5)
                    //        privateModes[XtermDecMode.BlinkCursor] = true;
                    //    else
                    //        privateModes[XtermDecMode.BlinkCursor] = false;
                    //}
                    break;

                default:
                    handled = false;
                    break;
            }
        }
        else
        {
            switch (kind)
            {
                //CSI n L 指令用于在光标当前位置插入 n 行空白行。插入行的操作会将光标所在行及其下方的行向下移动。
                case 'L':
                    //在光标当前位置插入 n 行空白行。
                    //光标所在行及其下方的行会向下移动 n 行。
                    //被移动的行超出滚动区域底部的部分将被丢弃。
                    //插入的行在滚动区域内有效，滚动区域外的行不受影响。
                    var ltype = 1;
                    if (codes.Any())
                    {
                        ltype = codes[0];
                    }

                    if (IsSetScrollRegion)
                    {
                        if (CursorPosition.Row >= ScrollRegionStart && CursorPosition.Row <= ScrollRegionEnd)
                        {
                            Dispatcher.UIThread.Invoke((() =>
                            {
                                for (int i = 0; i < ltype; i++)
                                {
                                    Lines.RemoveAt(ScrollRegionEnd - 1 - i);
                                }
                                for (int i = 0; i < ltype; i++)
                                {
                                    var newLine = new LineDto() { };
                                    newLine.AddVirtualLineRuns(ClientColumns);
                                    this.Lines.Insert(CursorPosition.Row - 1, newLine);
                                }
                            }));

                            
                           

                            SetLastItem();
                            if (ScrollToButtonChanged != null)
                            {
                                ScrollToButtonChanged(this, null);
                            }
                        }
                    }
                   
                    break;
                //用于清除光标所在行的部分或全部内容
                case 'K':
                    var type = 0;
                    if (codes.Any())
                    {
                        type = codes.First();
                    }
                    switch (type)
                    {
                        //清除从光标位置到行尾的所有字符（包括光标位置的字符）。这是默认行为，如果没有指定参数 Ps，则假定为 0。
                        case 0:
                            RemoveCharactersFromCursorToTheEndOfLine();
                            ReSetCursor();
                            break;
                        //清除从光标位置到行首的所有字符（包括光标位置的字符）。
                        case 1:
                            break;
                        //清除整行的所有字符，不论光标位置
                        case 2:
                            break;
                    }
                    break;
                //P 是一个用于删除字符的命令。具体来说，CSI P 用于删除从光标位置后右边的N个字符，并将后续字符左移以填补空白。
                case 'P':
                    var ptype = 1;
                    if (codes.Any())
                    {
                        ptype = codes.First();
                    }
                    //删除光标位置开始右边的ptype个字符
                    RemoveCharactersFromCursorToRight(ptype);
                    break;
                //A 是一个用于控制光标移动的命令。具体来说，CSI A 用于将光标向上移动若干个字符位置
                case 'A':
                    //如果没有指定参数 Ps，则默认移动一个位置
                    var atype = 1;
                    if (codes.Any())
                    {
                        atype = codes.First();
                    }
                    MoveCursor(CursorMovementType.Up, atype);
                    break;
                //B 是一个用于控制光标移动的命令。具体来说，CSI B 用于将光标向下移动若干个字符位置
                case 'B':
                    //如果没有指定参数 Ps，则默认移动一个位置
                    var btype = 1;
                    if (codes.Any())
                    {
                        btype = codes.First();
                    }
                    MoveCursor(CursorMovementType.Down, btype);
                    break;
                //C 是一个用于控制光标移动的命令。具体来说，CSI C 用于将光标向右移动若干个字符位置
                case 'C':
                    //如果没有指定参数 Ps，则默认移动一个位置
                    var ctype = 1;
                    if (codes.Any())
                    {
                        ctype = codes.First();
                    }
                    MoveCursor(CursorMovementType.Right, ctype);
                    break;
                //D 是一个用于控制光标移动的命令。具体来说，CSI D 用于将光标向左移动若干个字符位置
                case 'D':
                    //如果没有指定参数 Ps，则默认移动一个位置
                    var dtype = 1;
                    if (codes.Any())
                    {
                        dtype = codes.First();
                    }
                    MoveCursor(CursorMovementType.Left, dtype);
                    break;
                //终端控制操作
                case 't':
                    if (codes.Any())
                    {
                        var p1 = codes.First();
                        //重置终端的大小和位置到默认值
                        if (p1 == 22)
                        {
                            //todo 1
                        }
                    }
                    break;
                //m是选择图形渲染（SGR，Select Graphic Rendition）命令的终止符,主要是用来渲染字体
                case 'm':
                    HandleSgr(codes);
                    break;
                //H 用于将光标移动到指定的行和列位置,格式为CSI n;m H,其中n代表行号(从1开始),m代表列号(从1开始)。
                case 'H':
                    var tempCodes = realSequence.Split(';');
                    // \x1b[H：将光标移动到屏幕的左上角（第 1 行第 1 列）。
                    if (tempCodes.Length == 0)
                    {
                        tempCodes = new[] { "1", "1" };
                    }
                    else if (tempCodes.Length != 2)
                    {
                        break;
                    }

                    var hRow = string.IsNullOrWhiteSpace(tempCodes[0]) ? 1 : int.Parse(tempCodes[0]);
                    var hColumn = string.IsNullOrWhiteSpace(tempCodes[1]) ? 1 : int.Parse(tempCodes[1]);
                    InitCursor(hRow, hColumn);
                    break;
                //6n 是一个请求光标位置报告的命令。具体来说，CSI 6n 命令用于请求终端返回当前光标的位置
                case 'n':
                    if (codes.FirstOrDefault() == 6)
                    {

                    }
                    break;
                //r 是用于设置滚动区域（scrolling region）
                //CSI t; b r：将滚动区域设置为从第 t 行到第 b 行。
                //t：滚动区域的起始行（从 1 开始）。
                //b：滚动区域的结束行（从 1 开始）。
                //应用场景：设置滚动区域通常用于需要在屏幕的特定部分进行滚动操作的应用程序，如文本编辑器、日志查看器等。
                case 'r':
                    
                    if (codes.Count == 0)
                    {
                        this.IsSetScrollRegion = false;
                    }
                    else if (codes.Count == 2)
                    {
                        this.IsSetScrollRegion = true;
                        this.ScrollRegionStart = codes[0];
                        this.ScrollRegionEnd = codes[1];
                    }
                    break;
                //隐藏光标。
                case 'l':

                    break;
                //这是一个查询终端状态的转义序列，不同的终端可能有不同的响应，用于获取终端的一些特性或配置信息。
                case 'c':
                    //请求终端设备属性
                    if (sequence == ">c")
                    {
                        //todo 1
                    }
                    break;
                //J 是用于清除屏幕的命令。具体来说，CSI n J 命令用于清除屏幕的某些部分，取决于参数 n 的值。
                //CSI 0 J 或 CSI J：清除从光标位置到屏幕末尾的所有内容。
                //CSI 1 J：清除从屏幕开始到光标位置的所有内容。
                //CSI 2 J：清除整个屏幕。
                //CSI 3 J：清除整个屏幕以及滚动缓冲区（这个参数在某些终端中可能不被支持）。
                //光标位置：清除操作不会改变光标的位置，光标会保持在原来的位置。
                case 'J':
                    var jtype = 0;
                    if (codes.Any())
                    {
                        jtype = codes.First();
                    }
                    switch (jtype)
                    {
                        //清除从光标位置到屏幕末尾的所有内容。。这是默认行为，如果没有指定参数 Ps，则假定为 0。
                        case 0:

                            break;
                        //清除从屏幕开始到光标位置的所有内容。
                        case 1:
                            break;
                        //清除整个屏幕。
                        case 2:
                            break;
                        //清除整个屏幕以及滚动缓冲区（这个参数在某些终端中可能不被支持）
                        case 3:
                            break;
                    }
                    break;
               
            }
        }

        return true;
    }

    /// <summary>
    /// DEC Private Mode Set（也称为 DECSET）是一种控制序列，用于设置终端的特定模式。这些模式通常用于控制终端的行为和显示特性。DECSET 使用的控制序列格式为 ESC [ ? Pm h，其中 Pm 是一个或多个用分号分隔的参数
    /// </summary>
    /// <param name="kind"></param>
    /// <returns></returns>
    private bool HandleDecSet(int kind)
    {
        bool handled = true;
        switch ((DecPrivateMode)kind)
        {
            case DecPrivateMode.ApplicationCursorKeys:
                IsApplicationCursorKeysMode = true;
                break;
            //启动自动换行模式
            case DecPrivateMode.AutoWrapMode:
                AutoWrapMode = true;
                break;
            //启用备用屏幕缓冲区 (ESC [ ? 1047 h)：
            //当启用此模式时，终端会切换到备用屏幕缓冲区。备用缓冲区通常用于全屏应用程序（如文本编辑器、文件管理器等），以便在退出应用程序时恢复到原来的屏幕内容。
            case DecPrivateMode.SwitchAlternateDisplayBuffer:
                BackUpLinesOrRestoreLine(true);
                break;
            //备份光标
            case DecPrivateMode.SavingAndRestoringTheCursor:
                BackUpCursorPosition = CursorPosition.Clone();
                break;
            //切换备用屏幕缓冲区以及备份光标
            case DecPrivateMode.SwitchAlternateDisplayBufferAndSavingAndRestoringTheCursor:
                BackUpCursorPosition = CursorPosition.Clone();
                BackUpLinesOrRestoreLine(true);
                break;
            case DecPrivateMode.MouseReportMode2:
                //todo 1
                break;
            case DecPrivateMode.CursorBlink:
                //todo 1
                break;
            case DecPrivateMode.ShowOrHideCursor:
                this.IsShowCursor = true;
                break;
            default:
                handled = false;
                break;
        }

        //setPrivateMode((DecPrivateMode)kind, true);
        return handled;
    }
    /// <summary>
    /// DEC Private Mode Set（也称为 DECSET）是一种控制序列，用于设置终端的特定模式。这些模式通常用于控制终端的行为和显示特性。DECSET 使用的控制序列格式为 ESC [ ? Pm l，其中 Pm 是一个或多个用分号分隔的参数
    /// </summary>
    /// <param name="kind"></param>
    /// <returns></returns>
    private bool HandleDecReset(int kind)
    {
        bool handled = true;
        switch ((DecPrivateMode)kind)
        {
            case DecPrivateMode.ApplicationCursorKeys:
                IsApplicationCursorKeysMode = false;
                break;
            //启动自动换行模式
            case DecPrivateMode.AutoWrapMode:
                AutoWrapMode = false;
                break;
            //停用备用屏幕缓冲区 (ESC [ ? 1047 l)：
            //当停用此模式时，终端会切换到主屏幕缓冲区。备用缓冲区通常用于全屏应用程序（如文本编辑器、文件管理器等），以便在退出应用程序时恢复到原来的屏幕内容。
            case DecPrivateMode.SwitchAlternateDisplayBuffer:

                BackUpLinesOrRestoreLine(false);
                break;
            //还原主光标
            case DecPrivateMode.SavingAndRestoringTheCursor:
                CursorPosition = BackUpCursorPosition.Clone();
                break;
            //切换主屏幕缓冲区以及主光标
            case DecPrivateMode.SwitchAlternateDisplayBufferAndSavingAndRestoringTheCursor:
                CursorPosition = BackUpCursorPosition.Clone();
                BackUpLinesOrRestoreLine(false);
                ReSetCursor();
                break;
            case DecPrivateMode.CursorBlink:
                //todo 1
                break;
            case DecPrivateMode.ShowOrHideCursor:
                //todo 是否显示光标
                this.IsShowCursor = false;
                break;
            default:
                handled = false;
                break;
        }

        //setPrivateMode((DecPrivateMode)kind, true);
        return handled;
    }
    /// <summary>
    /// 备份或者恢复屏幕数据
    /// </summary>
    /// <param name="isBackUp"></param>
    private void BackUpLinesOrRestoreLine(bool isBackUp)
    {
        if (isBackUp)
        {
            this.AlternateScreenBuffer = this.Lines.Clone();
            this.Lines.Clear();
            InitAllLines();
        }
        else
        {
            Dispatcher.UIThread.Invoke((() =>
            {
                this.Lines.Clear();
                this.Lines.AddRange(this.AlternateScreenBuffer.Clone());
            }));
        }
    }

    private void InitAllLines()
    {
        var lines = Enumerable.Range(1, (int)ClientRows).Select(x =>
        {
            var line= new LineDto();
            line.AddVirtualLineRuns((int)ClientColumns);

            return line;
        }).ToList();

        Dispatcher.UIThread.Invoke((() =>
        {
            this.Lines.AddRange(lines);
        }));
       

    }

    /// <summary>
    /// SGR（Select Graphic Rendition）命令是 ANSI 转义序列的一部分，用于控制终端文本的显示属性。通过 SGR 命令，你可以改变文本的颜色、样式（如粗体、下划线）以及背景颜色等。SGR 命令通常用于增强终端输出的可读性和视觉效果。
    /// SGR 命令的基本结构是一个转义序列，通常以 ESC（转义字符，\u001b 或 \033）开头，后跟一个方括号 [，然后是一系列用分号分隔的参数，最后是字母 m。
    /// 例如：\033[31m 或 \u001b[31m。
    /// 假设你想在终端中输出红色的粗体文本：\u001b[1;31mThis is bold red text\u001b[0m
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    private Font HandleSgr(List<int> parameters)
    {
        var isHandle = false;
        //\x1b[m：同样是颜色重置序列，作用和 \x1b[0m 类似，将终端的显示属性重置为默认值。
        if (parameters.Count == 0)
        {
            Font = Font.CreateDefaultFont();
            return Font;
        }

        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            switch (parameter)
            {
                //0用于重置文本样式,使用场景,1.重置文本样式：在终端输出中，0m 用于重置之前设置的任何文本样式。例如，如果之前的文本被设置为粗体或某种颜色，0m 会将其恢复到默认样式。
                //2.结束格式化：通常在彩色输出或格式化文本的结尾使用，以确保后续文本不受之前样式的影响。
                case 0:
                    Font = Font.CreateDefaultFont();
                    break;
                //1粗体
                case 1:
                    Font.Bold = true;
                    break;
                //2 用于设置文本为暗色或降低亮度
                case 2:
                    break;
                //3斜体
                case 3:
                    Font.Italic = true;
                    break;
                //4：下划线。
                case 4:
                    Font.Underline = true;
                    break;
                //5：设置文本为慢速闪烁（slow blink）
                case 5:
                    break;
                //6：设置文本为快速闪烁（rapid blink）。
                case 6:
                    break;
                //7：反显（交换前景色和背景色）。
                case 7:
                    var oldBackground = Font.Background;
                    Font.Background = Font.Foreground;
                    Font.Foreground = oldBackground;
                    break;
                //  8：隐藏文本（使文本不可见）。
                case 8:
                    Font.Hidden = true;
                    break;
                // 9：设置文本为删除线（strikethrough）。
                case 9:
                    Font.StrikeThrough = true;
                    break;
                //10：重置字体样式为默认字体。
                case 10:
                    Font = Font.CreateDefaultFont();
                    break;
                // 23关闭斜体
                case 23:
                    Font.Italic = false;
                    break;
                //24：关闭下划线。
                case 24:
                    Font.Underline = false;
                    break;
                //25：关闭文本的闪烁效果。
                case 25:
                    break;
                //27：关闭反显效果，恢复文本的正常前景色和背景色。
                case 27:
                    var oldForeground = Font.Foreground;
                    Font.Foreground = Font.Background;
                    Font.Background = oldForeground;
                    break;
                //  28：关闭隐藏文本（使文本可见）。
                case 28:
                    Font.Hidden = false;
                    break;
                //29：关闭删除线。
                case 29:
                    Font.StrikeThrough = false;
                    break;
                //38：38 用于设置文本的前景色（文本颜色）为任意的 24 位 RGB 颜色或 256 色调色板中的颜色。这个命令提供了比标准的 8 色或 16 色更丰富的颜色选择。
                //用法,38; 5; n：设置文本的前景色为 256 色调色板中的颜色，其中 n 是颜色的索引（0 - 255）。
                //38; 2; r; g; b：设置文本的前景色为 24 位 RGB 颜色，其中 r、g、b 分别是红、绿、蓝的分量（0 - 255）。
                case 38:
                    if (i == 0)
                    {
                        isHandle = true;
                        var colorType = parameters[1];
                        //256色
                        if (colorType == 5)
                        {
                            var colorCode = parameters[2];
                            Font.Foreground = Get256Color(colorCode);
                        }
                        //24位
                        else if (colorType == 2)
                        {
                            var colorR = parameters[2];
                            var colorG = parameters[3];
                            var colorB = parameters[4];
                            Font.Foreground = Color.FromRgb((byte)colorR, (byte)colorG, (byte)colorB);
                        }
                    }

                    break;
                //39 用于重置文本的前景色（文本颜色）为默认颜色
                case 39:
                    //默认前景色
                    Font.Foreground = Font.DefaultFont.Foreground;
                    break;
                //48 用于设置文本的背景色为任意的 24 位 RGB 颜色或 256 色调色板中的颜色。这个命令提供了比标准的 8 色或 16 色更丰富的颜色选择。
                //用法,48; 5; n：设置文本的背景色为 256 色调色板中的颜色，其中 n 是颜色的索引（0 - 255）。
                //48; 2; r; g; b：设置文本的背景色为 24 位 RGB 颜色，其中 r、g、b 分别是红、绿、蓝的分量（0 - 255）。
                case 48:
                    if (i == 0)
                    {
                        isHandle = true;
                        var colorType = parameters[1];
                        //256色
                        if (colorType == 5)
                        {
                            var colorCode = parameters[2];
                            Font.Background = Get256Color(colorCode);
                        }
                        //24位
                        else if (colorType == 2)
                        {
                            var colorR = parameters[2];
                            var colorG = parameters[3];
                            var colorB = parameters[4];
                            Font.Background = Color.FromRgb((byte)colorR, (byte)colorG, (byte)colorB);
                        }
                    }

                    break;
                //  49 用于重置文本的背景色（文本颜色）为默认颜色
                case 49:
                    //默认背景色
                    Font.Background = Font.DefaultFont.Background;
                    break;
                default:
                    //前景色
                    if (parameter >= 30 && parameter <= 37)
                    {
                        Font.Foreground = this.Colors[parameter - 30];
                    }
                    //前景色
                    if (parameter >= 40 && parameter <= 47)
                    {
                        Font.Background = this.Colors[parameter - 40];
                    }
                    //前景色
                    if (parameter >= 90 && parameter <= 97)
                    {
                        Font.Foreground = this.Colors[parameter - 90];
                    }
                    //前景色
                    if (parameter >= 100 && parameter <= 107)
                    {
                        Font.Background = this.Colors[parameter - 107];
                    }
                    break;
            }

            if (isHandle)
            {
                break;
            }
        }

        return Font;
    }

    private Color Get256Color(int colorCode)
    {
        if (colorCode < 0 || colorCode > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(colorCode), "Color code must be between 0 and 255.");
        }

        if (colorCode < 16)
        {
            // 标准颜色
            return GetStandardColor(colorCode);
        }
        else if (colorCode < 232)
        {
            // 6x6x6 立方体颜色
            int index = colorCode - 16;
            int r = (index / 36) % 6 * 51;
            int g = (index / 6) % 6 * 51;
            int b = index % 6 * 51;
            return Color.FromRgb((byte)r, (byte)g, (byte)b);
        }
        else
        {
            // 灰度颜色
            int gray = (colorCode - 232) * 10 + 8;
            return Color.FromRgb((byte)gray, (byte)gray, (byte)gray);
        }
    }

    private Color GetStandardColor(int colorCode)
    {
        // 定义标准颜色
        Color[] standardColors = new Color[]
        {
            Color.FromRgb(0, 0, 0),       // Black
            Color.FromRgb(128, 0, 0),     // Maroon
            Color.FromRgb(0, 128, 0),     // Green
            Color.FromRgb(128, 128, 0),   // Olive
            Color.FromRgb(0, 0, 128),     // Navy
            Color.FromRgb(128, 0, 128),   // Purple
            Color.FromRgb(0, 128, 128),   // Teal
            Color.FromRgb(192, 192, 192), // Silver
            Color.FromRgb(128, 128, 128), // Gray
            Color.FromRgb(255, 0, 0),     // Red
            Color.FromRgb(0, 255, 0),     // Lime
            Color.FromRgb(255, 255, 0),   // Yellow
            Color.FromRgb(0, 0, 255),     // Blue
            Color.FromRgb(255, 0, 255),   // Fuchsia
            Color.FromRgb(0, 255, 255),   // Aqua
            Color.FromRgb(255, 255, 255)  // White
        };

        return standardColors[colorCode];
    }


    //  	bool handleCsi(string sequence)
    //{
    //	bool handled = true;

    //	bool isPrivate = sequence[0] == '?';
    //	char kind = sequence.Last();
    //	int?[] codes = null;
    //	try
    //	{
    //		string realSequence;
    //		if (isPrivate)
    //			realSequence = sequence.Substring(1, sequence.Length - 2);
    //		else
    //			realSequence = sequence.Substring(0, sequence.Length - 1);
    //		codes = (from str in realSequence.Split(';') select str.Length > 0 ? (int?) int.Parse(str) : null).ToArray();
    //	}
    //	catch (FormatException ex)
    //	{
    //		var a = ex.StackTrace;
    //	}

    //	Point oldCursorPos = CursorPos;
    //	if (isPrivate)
    //	{
    //		switch (kind)
    //		{
    //			case 'h':
    //				foreach (int code in codes)
    //					handled &= handleDecSet(code);
    //				break;

    //			case 'l':
    //				foreach (int code in codes)
    //					handled &= handleDecReset(code);
    //				break;

    //			case 'r':
    //				if (sequence[sequence.Length - 2] == ' ')
    //				{
    //					int cursorType = getAtOrDefault(codes, 0, 1);
    //					if (cursorType == 3 || cursorType == 4)
    //						UnderlineCursor = true;
    //					else
    //						UnderlineCursor = false;

    //					if (cursorType == 0 || cursorType == 1 || cursorType == 3 || cursorType == 5)
    //						privateModes[XtermDecMode.BlinkCursor] = true;
    //					else
    //						privateModes[XtermDecMode.BlinkCursor] = false;
    //				}
    //				break;

    //			default:
    //				handled = false;
    //				break;
    //		}
    //	}
    //	else
    //	{
    //		switch (kind)
    //		{
    //			case '@':
    //				// ICH: Insert x = 1 blank characters at cursor, moving the cursor accordingly.
    //				InsertCharacters(new string(' ', getAtOrDefault(codes, 0, 1)), CurrentFont);
    //				break;

    //			case 'A':
    //                      //光标上移 n（默认1） 行 <<若至屏幕顶端则无效>>
    //                  // CUU: Move cursor up x = 1 rows, clamped to screen size.
    //                  //CursorPos = new Point(CursorPos.Col, CursorPos.Row - getAtOrDefault(codes, 0, 1));
    //				break;

    //			case 'B':
    //                      //光标下移 n （默认1）行 <<若至屏幕底端则无效>>
    //                  // CUD: Move cursor down x = 1 rows, clamped to screen size.
    //                  //CursorPos = new Point(CursorPos.Col, CursorPos.Row + getAtOrDefault(codes, 0, 1));
    //                  break;

    //			case 'C':
    //			case 'a':
    //                      //光标前移 n （默认1）列 <<若至屏幕右端则无效>>
    //                  // CUF: Move cursor right x = 1 columns, clamped to screen size.
    //                  //CursorPos = new Point(CursorPos.Col + getAtOrDefault(codes, 0, 1), CursorPos.Row);
    //                  break;

    //			case 'D':
    //                  //光标后退 n （默认1）列 <<若至屏幕左端则无效>>
    //                  // CUB: Move cursor left x = 1 columns, clamped to screen size.
    //                  //CursorPos = new Point(CursorPos.Col - getAtOrDefault(codes, 0, 1), CursorPos.Row);
    //                  break;

    //			case 'E':
    //                  //光标下移 n （默认1）行 <<非标准>>
    //                  // CNL: Move cursor to the first column of the xth = 1 row below the cursor.
    //                  //CursorPos = new Point(0, CursorPos.Row + getAtOrDefault(codes, 0, 1));
    //                  break;

    //			case 'F':
    //                  //光标上移 n （默认1）行 <<非标准>>
    //                  // CPL: Move cursor to the first column of the xth = 1 row above the cursor.
    //                  //CursorPos = new Point(0, CursorPos.Row - getAtOrDefault(codes, 0, 1));
    //                  break;

    //			case 'G':
    //			case '`':
    //                  //光标移动至当前行n（默认1）列 <<非标准>>
    //                  // CHA: Move cursor to the xth = 1 column of the current row.
    //                  //CursorPos = new Point(getAtOrDefault(codes, 0, 1) - 1, CursorPos.Row);
    //                  break;

    //			case 'H':
    //			case 'f':
    //				// CUP: Move cursor to the xth = 1 row and yth = 1 column of the screen.
    //				{
    //					//int row = getAtOrDefault(codes, 0, 1);
    //					//int col = getAtOrDefault(codes, 1, 1);
    //					//CursorPos = new Point(col - 1, row - 1);
    //				}
    //				break;

    //			case 'I':
    //				// CHT: Move the cursor forward x = 1 tabstops.  Tabstops seem to be every 8 characters.
    //				int times = getAtOrDefault(codes, 0, 1);
    //				for (int i = 0; i < times; ++i)
    //					tab(false);
    //				break;

    //			case 'J':
    //				// ED: Erase certain lines depending on x = 0 WITHOUT changing the cursor 
    //				//     position:
    //				//     x = 0: Erase everything under and to the right of the cursor on the 
    //				//            current line, then everything on the lines underneath.
    //				//     x = 1: Erase everything to the left (not under?) of the cursor on 
    //				//            the current line, then everything on the lines above.
    //				//     x = 2: Erase everything.
    //				switch (getAtOrDefault(codes, 0, 0))
    //				{
    //					case 0:
    //						EraseCharacters(Size.Col - oldCursorPos.Col, false);
    //						for (int i = oldCursorPos.Row + 1; i < Size.Row; ++i)
    //						{
    //							CursorPos = new Point(0, i);
    //							EraseCharacters(Size.Col, false);
    //						}
    //						break;

    //					case 1:
    //						for (int i = 0; i < oldCursorPos.Row; ++i)
    //						{
    //							CursorPos = new Point(0, i);
    //							EraseCharacters(Size.Col, false);
    //						}
    //						EraseCharacters(oldCursorPos.Col, false);
    //						break;

    //					case 2:
    //						for (int i = 0; i < Size.Row; ++i)
    //						{
    //							CursorPos = new Point(0, i);
    //							EraseCharacters(Size.Col, false);
    //						}
    //						break;
    //				}
    //				CursorPos = oldCursorPos;
    //				break;

    //			// UNIMPLEMENTED: 
    //			// DECSED: Do the same as ED, except respect the character
    //			//         protection attribute set with DECSCA?

    //			case 'K':
    //				// EL: Erase a portion of the line the cursor is on depending on x = 0
    //				//     WITHOUT changing the cursor position:
    //				//     x = 0: Erase everything under and to the right of the cursor
    //				//     x = 1: Erase everything under and to the left of the cursor
    //				//     x = 2: Erase the entire line
    //				switch (getAtOrDefault(codes, 0, 0))
    //				{
    //					case 0:
    //						EraseCharacters(Size.Col - CursorPos.Col, false);
    //						break;

    //					case 1:
    //						CursorPos = new Point(0, CursorPos.Row);
    //						EraseCharacters(oldCursorPos.Col + 1, false);
    //						break;

    //					case 2:
    //						CursorPos = new Point(0, CursorPos.Row);
    //						EraseCharacters(Size.Col + 1, false);
    //						break;
    //				}
    //				CursorPos = oldCursorPos;
    //				break;

    //			// UNIMPLEMENTED: 
    //			// DECSEL: Do the same as EL, except respect the character
    //			//         protection attribute set with DECSCA?

    //			case 'L':
    //				// IL: Insert x = 1 lines at the cursor, scrolling using the scroll region
    //				//     if necessary WITHOUT moving the cursor.  The insertion happens just
    //				//     before the current line (so the current line is pushed down).
    //				{
    //					int rows = getAtOrDefault(codes, 0, 1);
    //					// Copy rows to their new location.  Start at the bottom of the 
    //					// scrollable region so we don't delete rows before we copy them.
    //					for (int i = scrollRegionBottom - 1; i >= CursorPos.Row; --i)
    //					{
    //						lines[i].DeleteCharacters(0, lines[i].Length);
    //						// Only copy rows if the "original" row is being affected by the
    //						// insertion operation.
    //						if (i - rows >= Math.Max(CursorPos.Row, scrollRegionTop))
    //						{
    //							foreach (var run in lines[i - rows].Runs)
    //							{
    //								lines[i].SetCharacters(lines[i].Length, run.Text, run.Font);
    //							}
    //						}
    //					}
    //				}
    //				break;

    //			case 'M':
    //				// DL: Delete x = 1 lines at the cursor, scrolling lines up from the bottom
    //				//     of the scroll region WITHOUT moving the cursor.  The current line is
    //				//     included in the delete.
    //				{
    //					int rows = getAtOrDefault(codes, 0, 1);
    //					for (int i = CursorPos.Row; i < scrollRegionBottom; ++i)
    //					{
    //						lines[i].DeleteCharacters(0, lines[i].Length);
    //						if (i + rows < scrollRegionBottom)
    //						{
    //							foreach (var run in lines[i + rows].Runs)
    //							{
    //								lines[i].SetCharacters(lines[i].Length, run.Text, run.Font);
    //							}
    //						}
    //					}
    //				}
    //				break;

    //			case 'P':
    //				// DCH: Delete x = 1 characters starting at the cursor.
    //				DeleteCharacters(getAtOrDefault(codes, 0, 1));
    //				break;

    //			case 'S':
    //				// SU: Scroll up x = 1 lines, respecting the scroll regions.  All lines
    //				//     that are more than x lines from the top are moved up x lines.  Rows
    //				//     near the bottom are replaced with empty lines.
    //				scroll(false, getAtOrDefault(codes, 0, 1));
    //				break;

    //			case 'T':
    //				// SD: Scroll down x = 1 lines.  Similar to SU, except in the opposite 
    //				//     direction.  Rows near the top are replaced with empty lines.
    //				scroll(true, getAtOrDefault(codes, 0, 1));
    //				break;

    //			case 'X':
    //				// ECH: Erase x = 1 characters, starting at and including the cursor.  Not
    //				//      sure if this should wrap lines.  Cursor is moved accordingly.
    //				EraseCharacters(getAtOrDefault(codes, 0, 1));
    //				break;

    //			case 'Z':
    //				// CBT: Move the cursor back x = 1 tabstops.  Same as CHT, except reversed.
    //				tab(true);
    //				break;

    //			// UNIMPLEMENTED:
    //			// REP: Repeat the character in the cell to the left of the cursor x = 1 
    //			//      times to the right, moving the cursor accordingly.  Not sure if 
    //			//      these are inserts or overwrites.

    //			case 'd':
    //				// VPA: Move cursor to the xth = 1 row without changing the column.
    //				{
    //					int row = getAtOrDefault(codes, 0, 1);
    //					CursorPos = new Point(CursorPos.Col, row - 1);
    //				}
    //				break;

    //			case 'e':
    //				// VPR: Move the cursor down x = 1 rows without changing the column.
    //				{
    //					int rows = getAtOrDefault(codes, 0, 1);
    //					CursorPos = new Point(CursorPos.Col, CursorPos.Row + rows);
    //				}
    //				break;

    //			case 'm':
    //				// SGR: Set attribute of next characters to be written.
    //				handled = handleCsiSgr(codes);
    //				break;

    //			case 'r':
    //				// DECSTBM: Set scroll region to start at row x = 1 and end at row
    //				//          y = height.  Top is inclusive, bottom is exclusive.
    //				//          Also, set cursor to the top-left corner?
    //				scrollRegionTop = getAtOrDefault(codes, 0, 1) - 1;
    //				scrollRegionBottom = getAtOrDefault(codes, 1, Size.Row);
    //				CursorPos = new Point(0, 0);
    //				break;

    //			case 's':
    //                  //保存光标位置
    //                  // Save cursor position
    //                  savedCursorPos.Col = CursorPos.Col;
    //				savedCursorPos.Row = CursorPos.Row;
    //				break;

    //			case 'u':
    //                  //取出保存的光标位置来使用
    //                  // Restore cursor position
    //                  CursorPos = new Point(savedCursorPos.Col, savedCursorPos.Row);
    //				break;

    //			default:
    //				handled = false;
    //				break;
    //		}
    //	}


    //	return handled;
    //}

    bool handleSingleEscape(char kind)
    {
        bool handled = true;
        string sequence = new string(kind, 1);
        //if (kind == '=')
        //	applicationKeypad = true;
        //else if (kind == '>')
        //	applicationKeypad = false;
        if (kind == '7')
        {

        }
        //savedCursorPos = CursorPos;
        else if (kind == '8')
        {

        }
        //CursorPos = savedCursorPos;
        else if (kind == 'M')
        {

        }
        //启用应用程序键模式。在该模式下，功能键（如 F1 - F12）发送的转义序列会被调整，以便应用程序能够识别。
        //用途：使应用程序能够更好地处理功能键输入。
        else if (kind == '=')
        {
            //todo shibie
        }
        else
        {
            handled = false;
            Debug.WriteLine("处理失败："+sequence);
        }
        
        return handled;
    }

    bool handleOsc(string sequence)
    {
        bool handled = true;
        int separatorIndex = sequence.IndexOf(';');
        if (separatorIndex == -1)
            separatorIndex = sequence.Length;
        int kind;
        if (!int.TryParse(sequence.Substring(0, separatorIndex), out kind))
        {
            System.Diagnostics.Debug.WriteLine($"Invalid OSC kind.  Sequence: {sequence}");
            return false;
        }
        switch (kind)
        {
            //客户端标题变更
            case 0:
                if (TitleChanged != null && separatorIndex != sequence.Length)
                    TitleChanged(this, new TitleChangeEventArgs(sequence.Substring(separatorIndex + 1)));
                break;
            //11 是用于查询或设置终端背景颜色的命令
            //OSC 11 的用法
            // 设置背景颜色：通过发送 OSC 11; color BEL，可以设置终端的背景颜色，其中 color 是颜色的表示，可以是 RGB 值或其他颜色格式。
            //查询背景颜色：通过发送 OSC 11; ? BEL，可以请求终端返回当前的背景颜色。
            //详细说明
            //    OSC：Operating System Command，通常由 ESC ]（即 \x1B]）引入。
            //11：指定操作类型为背景颜色设置或查询。
            //color：颜色值，可以是 rgb:RRRR / GGGG / BBBB 格式的 RGB 值。
            //BEL：终止符，通常是 ASCII 的 BEL 字符（\x07）或 ESC \（即 \x1B\\）。
            case 11:

                break;
            default:
                handled = false;
                break;
        }
       
        return handled;
    }
    private void InitData()
    {
        //Lines = new ObservableCollection<LineDto>()
        //{
        //    new LineDto()
        //    {
        //        Text = "Last login: Wed May 29 15:42:10 2024 from nit-i94509-o.atlbattery.com",
        //        LineFragmentDtos = new List<LineFragmentDto>()
        //        {

        //            new LineFragmentDto()
        //            {
        //                Text = "Last login: Wed May 29 15:42:10 2024 from nit-i94509-o.atlbattery.com"
        //            }
        //        }
        //    },
        //    new LineDto()
        //    {
        //        Text = "[root@cnndpgitapp01 ~]#",
        //        LineFragmentDtos = new List<LineFragmentDto>()
        //        {
        //            new LineFragmentDto()
        //            {
        //                Text = "[root@cnndpgitapp01 ~]#"
        //            }
        //        }
        //    }
        //};
        SetLastItem();
    }

    private void SetLastItem()
    {
        if (Lines.Any())
        {
            Lines.Last().IsLast = true;
        }
    }

    public void Send(char c)
    {
        shellStream.WriteByte((byte)c);
        shellStream.Flush();
    }
    public void SendBytes(List<byte> bytes)
    {
        if (shellStream == null)
        {
            return;
        }
        shellStream.Write(bytes.ToArray());
        shellStream.Flush();
    }
    public bool SendKey(Key key, KeyModifiers keyModifiers)
    {
        if (shellStream == null)
        {
            return false;
        }
        byte[] bytesToWrite = null;
        var encoding = Encoding.UTF8;

        if (keyModifiers.HasFlag(KeyModifiers.Control))
        {
            if ((key >= Key.A) && (key <= Key.Z))
            {
                bytesToWrite = new[] { (byte)(key - Key.A + 1) };
            }
            else
            {
                switch (key)
                {
                    case Key.OemOpenBrackets:
                        bytesToWrite = new Byte[] { 27 };
                        break;
                    case Key.Oem5:
                        bytesToWrite = new Byte[] { 28 };
                        break;
                    case Key.OemCloseBrackets:
                        bytesToWrite = new Byte[] { 29 };
                        break;
                }

                if (keyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    switch (key)
                    {
                        case Key.D6:
                            bytesToWrite = new Byte[] { 30 };
                            break;
                        case Key.OemMinus:
                            bytesToWrite = new Byte[] { 31 };
                            break;
                    }
                }
            }
        }
        else
        {
            string output = null;
            switch (key)
            {
                case Key.Escape:
                    output = "\x1b"; break;
                case Key.Tab:
                    output = "\t"; break;
                case Key.Home:
                    output = "\x1b[1~"; break;
                case Key.Insert:
                    output = "\x1b[2~"; break;
                case Key.Delete:
                    output = "\x1b[3~"; break;
                case Key.End:
                    output = "\x1b[4~"; break;
                case Key.PageUp:
                    output = "\x1b[5~"; break;
                case Key.PageDown:
                    output = "\x1b[6~"; break;
                case Key.F1:
                    output = "\x1bOP"; break;
                case Key.F2:
                    output = "\x1bOQ"; break;
                case Key.F3:
                    output = "\x1bOR"; break;
                case Key.F4:
                    output = "\x1bOS"; break;
                case Key.F5:
                    output = "\x1b[15~"; break;
                case Key.F6:
                    output = "\x1b[17~"; break;
                case Key.F7:
                    output = "\x1b[18~"; break;
                case Key.F8:
                    output = "\x1b[19~"; break;
                case Key.F9:
                    output = "\x1b[20~"; break;
                case Key.F10:
                    output = "\x1b[21~"; break;
                case Key.F11:
                    output = "\x1b[23~"; break;
                case Key.F12:
                    output = "\x1b[24~"; break;
            }

            if (output != null)
            {
                bytesToWrite = encoding.GetBytes(output);
            }
        }

        if (bytesToWrite == null)
        {
            string output = null;
            switch (key)
            {
                case Key.Left:
                    output = IsApplicationCursorKeysMode? "\x1bOD" : "\x1b[D"; 
                    break;
                case Key.Right:
                    output = IsApplicationCursorKeysMode ? "\x1bOC" : "\x1b[C";
                    break;
                case Key.Up:
                    output = IsApplicationCursorKeysMode ? "\x1bOA" : "\x1b[A";
                    break;
                case Key.Down:
                    output = IsApplicationCursorKeysMode ? "\x1bOB" : "\x1b[B";
                    break;
                case Key.Back:
                    output = "\b";
                    break;
                case Key.Enter:
                    output = "\r";
                    break;
            }

            if (output != null)
            {
                bytesToWrite = encoding.GetBytes(output);
            }
        }

        if (bytesToWrite != null)
        {
            SendBytes(bytesToWrite.ToList());
            return true;
        }
        return false;
    }

    //public void UpdateLastLine(string txt)
    //{
    //    var last = Lines.FirstOrDefault(it => it.IsLast);
    //    if (last != null)
    //    {
    //        last.Text += txt;
    //    }
    //}
    public void AddLine(string txt)
    {
        var newLine = new LineDto() { };
        var lineRunCount = (int)ClientColumns > txt.Length ? (int)ClientColumns : txt.Length;
        newLine.AddVirtualLineRuns(lineRunCount);
        newLine.AddLineRuns(txt, Font.Copy(), cursorPosition.Column);
        Dispatcher.UIThread.Invoke((() =>
        {
            this.Lines.Add(newLine);
        }));
       
        InitCursor(this.Lines.Count, txt.Length + 1);
        SetLastItem();
        if (ScrollToButtonChanged != null)
        {
            ScrollToButtonChanged(this, null);
        }
    }

    /// <summary>
    /// 换行
    /// </summary>
    public void LineFeed()
    {
        var newLine = new LineDto() { };
        newLine.AddVirtualLineRuns(ClientColumns);
        Dispatcher.UIThread.Invoke((() =>
        {
            this.Lines.Add(newLine);
        }));

        SetLastItem();
        if (ScrollToButtonChanged != null)
        {
            ScrollToButtonChanged(this, null);
        }
    }


    /// <summary>
    /// 滚动条滚动到底部
    /// </summary>
    public void ScrollToBottom()
    {

    }

    /// <summary>
    /// 移动光标
    /// </summary>
    /// <param name="type"></param>
    /// <param name="step"></param>
    public void MoveCursor(CursorMovementType type, int step)
    {
        if (type == CursorMovementType.Right)
        {
            this.CursorPosition.Column += step;
        }
        else if (type == CursorMovementType.Left)
        {
            this.CursorPosition.Column -= step;
        }
        else if (type == CursorMovementType.Up)
        {
            this.CursorPosition.Row -= step;
        }
        else if (type == CursorMovementType.Down)
        {
            this.CursorPosition.Row += step;
        }
        ReSetCursor();
    }

    /// <summary>
    /// 初始化光标
    /// </summary>
    /// <param name="row"></param>
    /// <param name="column"></param>
    public void InitCursor(int? row, int? column)
    {
        var hasChange = false;
        if (row.HasValue)
        {
            hasChange = true;
            this.CursorPosition.Row = row.Value;
        }
        if (column.HasValue)
        {
            hasChange = true;
            this.CursorPosition.Column = column.Value;
        }

        if (hasChange)
        {
            ReSetCursor();
        }
    }
}