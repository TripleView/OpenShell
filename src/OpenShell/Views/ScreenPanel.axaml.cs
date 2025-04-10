using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System.Diagnostics;
using System;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using OpenShell.ViewModels;

namespace OpenShell.Views
{
    public partial class ScreenPanel : StackPanel
    {
        private bool isDragging = false;
        private bool isStartDragging = false;
        private Point startPoint;
        private Point endPoint;
        private DrawingContext drawingContext;
        private DateTime? previewRenderDateTime;
        public ScreenPanel()
        {
            InitializeComponent();
            this.Background = Brushes.Black;
        }

        protected sealed override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            //var d = this.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
            //d.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            //d.AllowAutoHide = false;
            //base.OnAttachedToVisualTree(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (isDragging)
            {

            }
            this.isDragging = false;
            this.isStartDragging = false;
            base.OnPointerReleased(e);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            var clickInfo = e.GetCurrentPoint(this);
            if (e.Pointer.Type == PointerType.Mouse)
            {
                Debug.WriteLine("点击启动");
                if (clickInfo.Properties.IsLeftButtonPressed)
                {
                    this.isDragging = false;
                    this.isStartDragging = true;
                    this.startPoint = e.GetPosition(this);
                    previewRenderDateTime = DateTime.Now;
                    ScreenPanelVM.All.Clear();
                    var childrens = this.GetVisualDescendants().OfType<Line>().ToList();
                    foreach (var children in childrens)
                    {
                        if (children is Line line)
                        {
                            var f1 = line.GetVisualDescendants().OfType<LineRun>().ToList();


                            foreach (var lineRun in f1)
                            {
                                var lineRunDto = (lineRun.DataContext as LineRunDto);

                                if (lineRunDto.IsSelect == true)
                                {
                                    lineRunDto.IsSelect = false;
                                  
                                }
                            }
                        }
                        
                    }
                }
            }


            //base.OnPointerPressed(e);
        }


        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (isStartDragging)
            {
                //Debug.WriteLine($"isStartDragging:{isStartDragging},previewRenderDateTime:{previewRenderDateTime},diff:{(DateTime.Now - previewRenderDateTime.Value).Milliseconds}");
            }

            if (isStartDragging && (previewRenderDateTime == null || (previewRenderDateTime.HasValue && (DateTime.Now - previewRenderDateTime.Value).TotalMilliseconds > 50)))
            {
                var c = DataContext;
                previewRenderDateTime = DateTime.Now;
                this.isDragging = true;
                this.endPoint = e.GetPosition(this);
                var allrect = new Rect(startPoint, endPoint);
               
                var childrens = this.GetVisualDescendants().OfType<Line>().ToList();
                
                foreach (var children in childrens)
                {
                    if (children is Line line)
                    {
                        line.StartPoint = this.TranslatePoint(startPoint, line) ?? new Point(0, 0);
                        line.EndPoint = this.TranslatePoint(endPoint, line) ?? new Point(0, 0);
                        var f1 = line.GetVisualDescendants().OfType<LineRun>().ToList();
                        
                        foreach (var lineRun in f1)
                        {
                            var lineRunDto = (lineRun.DataContext as LineRunDto);
                            var lineTr = lineRun.TransformToVisual(this).Value;
                            var linex = lineTr.Transform(new Point(0, 0));
                            //var liney = lineTr.Transform(new Point(0+lineRun.Width, 0+lineRun.Height));

                            var lineRect = new Rect(linex.X, linex.Y, lineRun.Width, lineRun.Height);
                            var isIn = this.IsInRect(lineRect, allrect);
                            if (isIn)
                            {
                                if (lineRunDto.IsSelect == false)
                                {
                                    lineRunDto.IsSelect = true;
                                }
                            }
                            else
                            {
                                if (lineRunDto.IsSelect)
                                {
                                    lineRunDto.IsSelect = false;
                                    
                                }
                            }
                        }
                        line.IsDragging = isDragging;
                    }

                }
                var end = DateTime.Now;
                var fffffff = (previewRenderDateTime.Value - end).TotalMilliseconds;
                //Debug.WriteLine("全部一次耗时" + fffffff);
            }
            //base.OnPointerMoved(e);
        }

        /// <summary>
        /// Determine if one range is within another range
        /// 判断一个范围是否在另一个范围内
        /// </summary>
        /// <param name="lineRect"></param>
        /// <param name="allRect"></param>
        /// <returns></returns>
        private bool IsInRect(Rect lineRect, Rect allRect)
        {

            //选框是正方向，方向是左向右
            var xIsLeftToRight = (allRect.X <= lineRect.X && lineRect.X + lineRect.Width <= allRect.X + allRect.Width)
                                 || (lineRect.X + lineRect.Width >= allRect.X && lineRect.X <= allRect.X)
                                   || (lineRect.X + lineRect.Width >= allRect.X + allRect.Width && lineRect.X <= allRect.X + allRect.Width);
            //选框是反方向。方向是右向左
            var xIsRightToLeft = allRect.X >= lineRect.X + lineRect.Width && allRect.X + allRect.Width <= lineRect.X
                                 || (lineRect.X + lineRect.Width >= allRect.X && lineRect.X <= allRect.X)
                                 || (lineRect.X <= allRect.X + allRect.Width && lineRect.X + lineRect.Width >= allRect.X + allRect.Width);
            //起点y在某一行
            var startYInLine = allRect.Y >= lineRect.Y && lineRect.Y + lineRect.Height >= allRect.Y;
            //终点y在某一行
            var endYInLine = allRect.Y + allRect.Height >= lineRect.Y &&
                             allRect.Y + allRect.Height <= lineRect.Y + lineRect.Height;
            //终点Y在起点Y上面
            var endYIsAboveStartY = allRect.Y + allRect.Height <= allRect.Y;
            //y轴选中的时候，处于中间的部分必然选中
            var yCoverLineFromTopToBottom = allRect.Y <= lineRect.Y && allRect.Y + allRect.Height >= lineRect.Y + lineRect.Height;
            var yCoverLineFromBottomToTop = allRect.Y >= lineRect.Y + lineRect.Height && allRect.Y + allRect.Height <= lineRect.Y;

            if (yCoverLineFromTopToBottom || yCoverLineFromBottomToTop)
            {
                return true;
            }

            //判断起始Y和终点Y是否在同一行
            if (startYInLine && endYInLine)
            {
                return xIsLeftToRight || xIsRightToLeft;
            }
            else
            {
                //如果终点y在起点y的上面，那么起点y的x轴左边部分被选中，终点y轴的右边被选中
                if (endYIsAboveStartY)
                {
                    return startYInLine && (lineRect.X + lineRect.Width <= allRect.X || (lineRect.X <= allRect.X && lineRect.X + lineRect.Width >= allRect.X))
                           || (endYInLine && (allRect.X + allRect.Width <= lineRect.X || (lineRect.X <= allRect.X + allRect.Width && lineRect.X + lineRect.Width >= allRect.X + allRect.Width)));
                }
                else
                {
                    //如果终点y在起点y的下面，那么起点y的x轴右边部分被选中，终点y轴的左边被选中
                    return (lineRect.X >= allRect.X || (lineRect.X <= allRect.X && lineRect.X + lineRect.Width >= allRect.X))
                        && startYInLine || (endYInLine && ((allRect.X + allRect.Width >= lineRect.X + lineRect.Width) || (lineRect.X <= allRect.X + allRect.Width && lineRect.X + lineRect.Width >= allRect.X + allRect.Width)));
                }
            }
        }

    }
}
