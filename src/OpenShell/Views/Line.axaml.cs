using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Collections.Generic;
using System.Globalization;
using System;
using Avalonia.Threading;
using OpenShell.Views;
using System.Collections.ObjectModel;
using OpenShell.Dto;

namespace OpenShell;

public partial class Line : UserControl
{
    public Line()
    {
        InitializeComponent();

        Cursor = new Cursor(StandardCursorType.Ibeam);

    }

    public bool IsDragging = false;
    public Point StartPoint;
    public Point EndPoint;

}