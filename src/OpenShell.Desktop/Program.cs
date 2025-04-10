using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia;
using Avalonia.ReactiveUI;

namespace OpenShell.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
       
        //var f = new SemaphoreSlim(1);
        //f.Wait();
        //f.Release();

        //f.Wait();
        //f.Wait();
        //var c = 123;
        //f.Wait();
        //f.Release();
        var a = (char)13;
       
        var fff = new List<string>();
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(fff.ToArray());
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseFont()
            .UseReactiveUI();
}
