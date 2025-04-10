using System;
using System.Diagnostics;
using Avalonia.Media;

namespace OpenShell.Dto;

public class Font : IEquatable<Font>
{

    /// <summary>
    /// 前景色
    /// </summary>
    public Color Foreground;
    /// <summary>
    /// 背景色
    /// </summary>
    public Color Background;
    /// <summary>
    /// 粗体
    /// </summary>
    public bool Bold;
    public bool Faint;
    /// <summary>
    /// 下划线
    /// </summary>
    public bool Underline;
    /// <summary>
    /// 斜体
    /// </summary>
    public bool Italic;
    /// <summary>
    /// 中划线
    /// </summary>
    public bool StrikeThrough;
    public bool Hidden;
    public bool Inverse;

    public override bool Equals(object obj)
    {
        if (obj is Font objFont)
        {
            return this == objFont;
        }
        else
            return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public bool Equals(Font other)
    {
        return this == other;
    }

    public static bool operator ==(Font _1, Font _2)
    {
        // 检查是否为同一对象或都为null
        if (ReferenceEquals(_1, _2))
        {
            return true;
        }

        // 检查是否有一个为null
        if (ReferenceEquals(_1, null) || ReferenceEquals(_2, null))
        {
            return false;
        }
        return
            _1.Foreground == _2.Foreground &&
            _1.Background == _2.Background &&
            _1.Bold == _2.Bold &&
            _1.Underline == _2.Underline &&
            _1.Hidden == _2.Hidden &&
            _1.Inverse == _2.Inverse;
    }

    public static bool operator !=(Font _1, Font _2)
    {
        return !(_1 == _2);
    }

    public static Font DefaultFont { set; get; } = new Font()
    {
        Background = Colors.Black,
        Foreground = Colors.White
    };

    public static Font CreateDefaultFont()
    {
        return new Font()
        {
            Background = Colors.Black,
            Foreground = Colors.White
        };
    }

    public Font Copy()
    {
        return new Font()
        {
            Background = Background,
            Bold = Bold,
            Faint = Faint,
            Foreground = Foreground,
            Hidden = Hidden,
            Inverse = Inverse,
            Italic = Italic,
            StrikeThrough = StrikeThrough,
            Underline = Underline
        };
    }
}