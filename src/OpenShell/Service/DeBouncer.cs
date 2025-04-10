using System;
using System.Threading;

namespace OpenShell.Service;

/// <summary>
/// 去抖动器
/// </summary>
public class DeBouncer
{
    private Timer timer;
    private Action action;

    public DeBouncer()
    {
        
    }

    public void DeBounce(Action action, int millisecond)
    {
        this.action = action;
        timer?.Change(Timeout.Infinite, Timeout.Infinite);
        timer = new Timer(ExecuteTask, null, millisecond, Timeout.Infinite);
    }

    private void ExecuteTask(object? state)
    {
        this.action.Invoke();
    }
}