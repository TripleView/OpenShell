namespace OpenShell.Dto;

public enum DecPrivateMode
{
    /// <summary>
    /// 启用 (h)：将光标键(箭头键)设置为应用程序模式。禁用 (l)：将光标键(箭头键)设置为正常模式。
    /// Application Cursor Keys Mode 是终端仿真器中的一种模式，影响光标键（箭头键）发送的控制序列。这个模式主要用于支持终端应用程序（如文本编辑器和全屏应用程序）在不同的输入场景下接收不同的光标键输入。
    /// 在终端仿真器中，光标键可以发送两种不同的控制序列：
    /// Normal Mode（正常模式）：
    ///在正常模式下，光标键发送标准的 ANSI 控制序列。
    ///例如，向上箭头键通常发送 ESC [A。
    ///Application Cursor Keys Mode（应用程序光标键模式）：
    ///在这个模式下，光标键发送不同的控制序列，通常以 ESC O 开头。
    ///例如，向上箭头键在应用程序模式下可能发送 ESC O A
    /// </summary>
    ApplicationCursorKeys = 1,

    /// <summary>
    ///启用 (h)：将终端设置为 132 列模式。 禁用 (l)：将终端设置为 80 列模式
    /// </summary>
    Column132Mode = 3,

    /// <summary>
    /// 启用 (h)：启用反显模式（背景和前景颜色互换）。禁用 (l)：禁用反显模式。
    /// </summary>
    ScreenMode = 5,

    /// <summary>
    /// 启用 (h)：光标位置相对于滚动区域。禁用 (l)：光标位置相对于整个屏幕。
    /// </summary>
    CursorOriginMode = 6,

    /// <summary>
    /// 启用 (h)：启用自动换行模式。禁用 (l)：禁用自动换行模式。
    /// </summary>
    AutoWrapMode = 7,

    /// <summary>
    /// 启用 (h)：启用键盘自动重复。禁用 (l)：禁用键盘自动重复。
    /// </summary>
    AutoRepeatKeys = 8,
    /// <summary>
    /// 启用 (h)：启用 X10 鼠标报告模式。禁用 (l)：禁用 X10 鼠标报告模式。
    /// </summary>
    MouseX10CompatibilityMode =9,
    /// <summary>
    /// 启用/禁用光标闪烁
    /// </summary>
    CursorBlink = 12,
    /// <summary>
    /// 启用 (h)：显示光标。禁用 (l)：隐藏光标。
    /// </summary>
    ShowOrHideCursor =25,
    /// <summary>
    /// 启用 (h)：切换到备用屏幕缓冲区。禁用 (l)：切换回主屏幕缓冲区。
    /// </summary>
    UseAlternateScreenBuffer = 47,
    /// <summary>
    /// 启用鼠标报告模式
    /// </summary>
    MouseReportMode = 1000,

    /// <summary>
    /// 鼠标高亮跟踪模式
    /// </summary>
    MouseHighlightTracking = 1001,

    /// <summary>
    /// 鼠标按钮事件模式
    /// </summary>
    MouseButtonEventMode = 1002,

    /// <summary>
    /// 鼠标移动事件模式
    /// </summary>
    MouseMotionEventMode = 1003,

    /// <summary>
    /// 发送焦点事件
    /// </summary>
    SendFocusEvents = 1004,

    /// <summary>
    /// 启用 UTF-8 鼠标模式
    /// </summary>
    Utf8MouseMode = 1005,

    /// <summary>
    /// 启用 SGR 鼠标模式
    /// </summary>
    SgrMouseMode = 1006,

    /// <summary>
    /// 启用鼠标像素位置模式
    /// </summary>
    MousePixelPositionMode = 1016,

    /// <summary>
    /// 启用 Bracketed Paste Mode
    /// </summary>
    BracketedPasteMode = 2004,

    /// <summary>
    /// 启用 Meta 键处理（旧版）
    /// </summary>
    MetaKeyHandling = 1034,

    /// <summary>
    /// 切换备用显示缓冲区
    /// Switch alternate display buffer
    /// </summary>
    SwitchAlternateDisplayBuffer = 1047,
    /// <summary>
    /// 1048 是一个用于控制终端光标位置保存和恢复的模式。在 xterm 和一些其他终端仿真器中，?1048 用于保存当前光标位置并在需要时恢复。
    /// 当启用此模式时，终端会保存当前光标的位置。这个功能通常用于在切换到备用屏幕缓冲区之前保存光标位置，以便在返回主屏幕缓冲区时能够恢复到原来的光标位置。
    /// 禁用此模式会恢复之前保存的光标位置。这通常在从备用屏幕缓冲区返回到主屏幕缓冲区时使用，以确保光标位置的一致性。
    /// </summary>
    SavingAndRestoringTheCursor = 1048,
    /// <summary>
    /// 切换备用显示缓冲区以及保存和恢复光标
    /// </summary>
    SwitchAlternateDisplayBufferAndSavingAndRestoringTheCursor = 1049,

    /// <summary>
    /// 启用终端报告窗口标题
    /// </summary>
    ReportWindowTitle = 1050,

    /// <summary>
    /// 启用终端报告窗口图标
    /// </summary>
    ReportWindowIcon = 1051,

    /// <summary>
    /// 启用终端报告窗口图标和标题
    /// </summary>
    ReportWindowIconAndTitle = 1052,

    /// <summary>
    /// 启用终端报告窗口大小和标题
    /// </summary>
    ReportWindowSizeAndTitle = 1053,
    /// <summary>
    /// 启用鼠标报告模式。当启用该模式后，终端会在鼠标移动、点击等操作时发送相应的转义序列，让应用程序能够捕获鼠标事件。
    /// </summary>
    MouseReportMode2 = 2004,
}
