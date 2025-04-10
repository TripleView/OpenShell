using Avalonia;
using Avalonia.Controls;
using System;
namespace OpenShell.Views;

/// <summary>
/// 支持自动换行的面板
/// </summary>
public class AutomaticLineWrapPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        double lineWidth = 0;
        double lineHeight = 0;
        double totalHeight = 0;
        double maxWidth = 0;

        foreach (var child in Children)
        {
            child.Measure(availableSize);
            if (lineWidth + child.DesiredSize.Width > availableSize.Width)
            {
                totalHeight += lineHeight;
                maxWidth = Math.Max(maxWidth, lineWidth);
                lineWidth = 0;
                lineHeight = 0;
            }

            lineWidth += child.DesiredSize.Width;
            lineHeight = Math.Max(lineHeight, child.DesiredSize.Height);
        }

        totalHeight += lineHeight;
        maxWidth = Math.Max(maxWidth, lineWidth);

        return new Size(maxWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double lineWidth = 0;
        double lineHeight = 0;
        double y = 0;

        foreach (var child in Children)
        {
            if (lineWidth + child.DesiredSize.Width > finalSize.Width)
            {
                y += lineHeight;
                lineWidth = 0;
                lineHeight = 0;
            }

            child.Arrange(new Rect(lineWidth, y, child.DesiredSize.Width, child.DesiredSize.Height));
            lineWidth += child.DesiredSize.Width;
            lineHeight = Math.Max(lineHeight, child.DesiredSize.Height);
        }

        return finalSize;
    }
}
