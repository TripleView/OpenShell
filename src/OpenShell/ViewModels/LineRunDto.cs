using System.ComponentModel;
using ReactiveUI;
using OpenShell.Dto;

namespace OpenShell.ViewModels;

public class LineRunDto : BaseBinding
{
    /// <summary>
    /// 因为多属性变化可能会导致多次刷新，所以属性变化通知改为手动进行
    /// </summary>
    public void NotifyPropertyChanged()
    {
        OnPropertyChanged(nameof(Text));
    }
    public int Index { get; set; }

    //public string Text { get; set; }

    public string Text
    {
        get => text;
        set
        {
            if (text != value)
            {
                text = value;
                OnPropertyChanged(nameof(Text));
            }

        }
    }

    private string text;

    private bool isSelect;
    /// <summary>
    /// indice whether this is select
    /// </summary>
    public bool IsSelect
    {
        get => isSelect;
        set
        {
            if (isSelect != value)
            {
                isSelect = value;
                OnPropertyChanged(nameof(IsSelect));
            }
        }
    }



    public bool IsBlink
    {
        get => isBlink;
        set
        {
            if (isBlink != value)
            {
                isBlink = value;
                OnPropertyChanged(nameof(IsBlink));
            }
        }
    }

    private bool isBlink;


    private bool isVirtual;
    public bool IsVirtual
    {
        get => isVirtual;
        set
        {
            if (isVirtual != value)
            {
                isVirtual = value;
                OnPropertyChanged(nameof(IsVirtual));
            }
        }
    }


    private Font font;
    public Font Font
    {
        get => font;
        set
        {
            if (font != value)
            {
                font = value;
                OnPropertyChanged(nameof(Font));
            }
        }
    }
    /// <summary>
    /// 使当前lineRun无效
    /// </summary>
    public void ChangeToVirtual()
    {
        StopNotify();
        Font = Font.CreateDefaultFont();
        IsVirtual = true;
        Text = "";
        StartNotify();
        NotifyPropertyChanged();
    }
}