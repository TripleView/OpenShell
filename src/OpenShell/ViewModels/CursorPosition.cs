using System.ComponentModel;
using OpenShell.Dto;

namespace OpenShell.ViewModels;

/// <summary>
/// 光标位置信息
/// Cursor position information
/// </summary>
public class CursorPosition : BaseBinding
{
    public CursorPosition(int column, int row)
    {
        Column = column;
        Row = row;
    }


    private int column;
    public int Column
    {
        get => column;
        set
        {
            column = value;
            OnPropertyChanged(nameof(Column));
        }

    }


    private int row;
    public int Row
    {
        get => row;
        set
        {
            row = value;
            OnPropertyChanged(nameof(Row));
        }

    }




    public CursorPosition Clone()
    {
        return new CursorPosition(Column, Row);
    }
}