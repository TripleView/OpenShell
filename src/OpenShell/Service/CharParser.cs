using DynamicData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenShell.Service;

public enum SequenceType
{
    Text,
    /// <summary>
    /// Control Sequence Introducer，是在计算机文本处理和终端通信中使用的一种代码序列，用于控制终端或文本显示的格式和行为
    /// </summary>
    Csi,
    Osc,
    SingleEscape,
    /// <summary>
    /// 控制符
    /// </summary>
    ControlCode,
}

public class TitleChangeEventArgs : EventArgs
{
    public string Title
    { get; }

    public TitleChangeEventArgs(string title)
    {
        Title = title;
    }
}

public class ReceivedSequenceEventArgs : EventArgs
{
    public SequenceType Type
    { get; }

    public string Sequence
    { get; }

    public ReceivedSequenceEventArgs(SequenceType type, string sequence)
    {
        Type = type;
        Sequence = sequence;
    }
}

public enum ControlState
{
    Text,
    Esc,
    Csi,
    Osc,
    Esc2,
}
public class CharParser 
{
    private ControlState state = ControlState.Text;
    private List<char> caches = new List<char>();

    public event EventHandler<ReceivedSequenceEventArgs> ReceiveSequence;
    public void Parse(byte[] bytes)
    {
        if (bytes?.Length > 0)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i];
                var ch = (char)b;
                
                switch (state)
                {
                    case ControlState.Text:
                        if (!char.IsControl(ch)||!char.IsAscii(ch))
                        {
                            caches.Add(ch);
                        }
                        else if (ch == '\r')
                        {
                            EndSequence(SequenceType.Text);
                            caches.Add(ch);
                            EndSequence(SequenceType.ControlCode);
                        }
                        else if (b == '\x1b')
                        {
                            EndSequence(SequenceType.Text);
                            state = ControlState.Esc;
                        }
                        else
                        {
                            EndSequence(SequenceType.Text);
                            caches.Add(ch);
                            EndSequence(SequenceType.ControlCode);
                        }
                        break;
                    case ControlState.Esc:
                        if (ch == '[')
                        {
                            state = ControlState.Csi;
                        }
                        else if (ch == ']')
                        {
                            state = ControlState.Osc;
                        }
                        else if (ch >= '0')
                        {
                            caches.Add(ch);
                            EndSequence(SequenceType.SingleEscape);
                            state = ControlState.Text;
                        }
                        else
                        {
                            caches.Add(ch);
                            state = ControlState.Esc2;
                        }
                        break;
                    case ControlState.Esc2:
                        caches.Clear();
                        state = ControlState.Text;
                        break;
                    case ControlState.Csi:
                        caches.Add(ch);
                        if (ch >= 64 && ch <= 126)
                        {
                            EndSequence(SequenceType.Csi);
                            state = ControlState.Text;
                        }
                        break;

                    case ControlState.Osc:
                        //以\a（铃声字符，ASCII码7）结束
                        if (ch == 7)
                        {
                            EndSequence(SequenceType.Osc);
                            state = ControlState.Text;
                        }
                        else
                        {
                            caches.Add(ch);
                        }
                        break;
                }

            }

            if (state == ControlState.Text)
            {
                EndSequence(SequenceType.Text);
            }
        }
    }

    private void EndSequence(SequenceType type)
    {
        if (caches.Any())
        {
            var txt = Encoding.UTF8.GetString(caches.Select(it => (byte)it).ToArray());
            if (ReceiveSequence != null)
                ReceiveSequence(this, new ReceivedSequenceEventArgs(type,txt ));
            caches.Clear();
        }
        
    }

}