using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Threading;
using DynamicData;
using Newtonsoft.Json;
using ReactiveUI;
using OpenShell.Dto;

namespace OpenShell.ViewModels;


public class LineDto : BaseBinding
{
    //private string text;

    public string TextFF => string.Join("", List.Select(x => x.Text));


    private int columns;


    /// <summary>
    /// 添加显示的文本
    /// </summary>
    public void AddLineRuns(string text, Font font, int startColumn)
    {
        if (startColumn == 0)
        {
            return;
        }

        var diff = text.Length + startColumn - 1 - list.Count;
        //列表长度不够，则进行扩容
        if (diff > 0)
        {
            AddVirtualLineRuns(diff);
        }

        for (var i = 0; i < text.Length; i++)
        {
            var item = List[i + startColumn - 1];
            item.Text = text[i].ToString();
            item.Font = font;
            item.IsVirtual = false;
        }

        OnPropertyChanged(nameof(List));
    }

    /// <summary>
    /// 添加虚拟文本列表
    /// </summary>

    public void AddVirtualLineRuns(int columns)
    {
        this.columns = columns;
        var tempList = new List<LineRunDto>();
        for (var i = 0; i < columns; i++)
        {
            var item = new LineRunDto()
            {
                Index = i,
                Text = "",
                Font = Font.CreateDefaultFont(),
                IsVirtual = true
            };
            tempList.Add(item);
        }

        Dispatcher.UIThread.Invoke(() =>
        {
            List.AddRange(tempList);
        });

        OnPropertyChanged(nameof(List));
    }


    public ObservableCollection<LineRunDto> List
    {
        get
        {
            return list;
        }
        set
        {
            list = value;
            OnPropertyChanged(nameof(List));
        }

    }

    private ObservableCollection<LineRunDto> list = new ObservableCollection<LineRunDto>();


    public int Index { get; set; }

    public bool IsLast { get; set; }
}


