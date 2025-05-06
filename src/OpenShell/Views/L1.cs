using Avalonia.Controls;
using Avalonia.Media;
using SkiaSharp;
using System.Globalization;
using Avalonia;
using static System.Net.Mime.MediaTypeNames;

namespace Layer.Views;

public class L1 : Control
{
    public override void Render(DrawingContext context)
    {
        //Brushes.DarkSalmon
        context.DrawRectangle(Brushes.DarkSalmon, new Pen(),this.Bounds);
        base.Render(context);
    }
}

public class L2 : Control
{
    public override void Render(DrawingContext context)
    {
        var ft = CreateFormattedText("我们");
        var origin = new Point();

        // TODO: Format diff.
        // ft.SetForegroundBrush(Brushes.Red, 8, 2);
        // ft.SetForegroundBrush(Brushes.Red, 11, 2);

        context.DrawText(ft, origin);
        base.Render(context);
    }

    public FormattedText CreateFormattedText(string text)
    {
        return new FormattedText(text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default, 
            50,
            Brushes.White);
    }
}

public class L3 : Control
{
    public override void Render(DrawingContext context)
    {
        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure { StartPoint = new Point(0, 100) };

        // 创建一个直线段
        var lineSegment = new LineSegment { Point = new Point(100, 100) };
        pathFigure.Segments.Add(lineSegment);

        var lineSegment2 = new LineSegment { Point = new Point(100, 0) };
        pathFigure.Segments.Add(lineSegment2);

        var lineSegment3= new LineSegment { Point = new Point(0, 100) };
        //pathFigure.Segments.Add(lineSegment3);
        // 创建一个弧线段
        //var arcSegment = new ArcSegment
        //{
        //    Point = new Point(100, 100),
        //    Size = new Size(50, 50),
        //    RotationAngle = 0,
        //    IsLargeArc = false,
        //    SweepDirection = SweepDirection.Clockwise
        //};
        //pathFigure.Segments.Add(arcSegment);

        pathGeometry.Figures.Add(pathFigure);
        context.DrawGeometry(Brushes.CadetBlue,new Pen(),pathGeometry);
        //base.Render(context);
    }
}