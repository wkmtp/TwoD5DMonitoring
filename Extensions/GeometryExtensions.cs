using System;
using System.Windows;
using System.Windows.Media;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Extensions
{
    public static class GeometryExtensions
    {
        public static Point SnapToGrid(this Point point, double gridSize)
        {
            return new Point(
                Math.Round(point.X / gridSize) * gridSize,
                Math.Round(point.Y / gridSize) * gridSize
            );
        }

        public static Point RotateAround(this Point point, Point center, double angleDegrees)
        {
            var radians = angleDegrees * Math.PI / 180;
            var cos = Math.Cos(radians);
            var sin = Math.Sin(radians);
            
            var dx = point.X - center.X;
            var dy = point.Y - center.Y;
            
            return new Point(
                center.X + dx * cos - dy * sin,
                center.Y + dx * sin + dy * cos
            );
        }

        public static double DistanceTo(this Point p1, Point p2)
        {
            var dx = p1.X - p2.X;
            var dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static Point GetAnchorPosition(this UIElement element, AnchorPoint anchor)
        {
            var bounds = new Rect(0, 0, element.RenderSize.Width, element.RenderSize.Height);
            
            return anchor.Type switch
            {
                AnchorType.Top => new Point(bounds.Width / 2, 0),
                AnchorType.Bottom => new Point(bounds.Width / 2, bounds.Height),
                AnchorType.Left => new Point(0, bounds.Height / 2),
                AnchorType.Right => new Point(bounds.Width, bounds.Height / 2),
                AnchorType.TopLeft => new Point(0, 0),
                AnchorType.TopRight => new Point(bounds.Width, 0),
                AnchorType.BottomLeft => new Point(0, bounds.Height),
                AnchorType.BottomRight => new Point(bounds.Width, bounds.Height),
                AnchorType.Custom => anchor.Position,
                _ => new Point(bounds.Width / 2, bounds.Height / 2)
            };
        }

        public static Point GetAbsoluteAnchorPosition(this UIElement element, AnchorPoint anchor)
        {
            var relativePos = element.GetAnchorPosition(anchor);
            var transform = element.TransformToAncestor(Application.Current.MainWindow);
            return transform.Transform(relativePos);
        }
    }

    public static class ColorExtensions
    {
        public static Color Lighten(this Color color, double factor)
        {
            factor = Math.Clamp(factor, 0, 1);
            return Color.FromRgb(
                (byte)Math.Min(255, color.R + (255 - color.R) * factor),
                (byte)Math.Min(255, color.G + (255 - color.G) * factor),
                (byte)Math.Min(255, color.B + (255 - color.B) * factor)
            );
        }

        public static Color Darken(this Color color, double factor)
        {
            factor = Math.Clamp(factor, 0, 1);
            return Color.FromRgb(
                (byte)Math.Max(0, color.R * (1 - factor)),
                (byte)Math.Max(0, color.G * (1 - factor)),
                (byte)Math.Max(0, color.B * (1 - factor))
            );
        }

        public static Color WithAlpha(this Color color, byte alpha)
        {
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        public static Color Lerp(this Color from, Color to, double t)
        {
            t = Math.Clamp(t, 0, 1);
            return Color.FromRgb(
                (byte)(from.R + (to.R - from.R) * t),
                (byte)(from.G + (to.G - from.G) * t),
                (byte)(from.B + (to.B - from.B) * t)
            );
        }

        public static SolidColorBrush ToBrush(this Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public static LinearGradientBrush CreateVerticalGradient(params (Color color, double offset)[] stops)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1),
                MappingMode = BrushMappingMode.RelativeToBoundingBox
            };

            foreach (var (color, offset) in stops)
            {
                brush.GradientStops.Add(new GradientStop(color, offset));
            }
            brush.Freeze();
            return brush;
        }

        public static LinearGradientBrush CreateHorizontalGradient(params (Color color, double offset)[] stops)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0.5),
                EndPoint = new Point(1, 0.5),
                MappingMode = BrushMappingMode.RelativeToBoundingBox
            };

            foreach (var (color, offset) in stops)
            {
                brush.GradientStops.Add(new GradientStop(color, offset));
            }
            brush.Freeze();
            return brush;
        }

        public static LinearGradientBrush CreateDiagonalGradient(params (Color color, double offset)[] stops)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                MappingMode = BrushMappingMode.RelativeToBoundingBox
            };

            foreach (var (color, offset) in stops)
            {
                brush.GradientStops.Add(new GradientStop(color, offset));
            }
            brush.Freeze();
            return brush;
        }

        public static RadialGradientBrush CreateRadialGradient(Point center, double radiusX, double radiusY, params (Color color, double offset)[] stops)
        {
            var brush = new RadialGradientBrush
            {
                Center = center,
                RadiusX = radiusX,
                RadiusY = radiusY,
                GradientOrigin = center,
                MappingMode = BrushMappingMode.RelativeToBoundingBox
            };

            foreach (var (color, offset) in stops)
            {
                brush.GradientStops.Add(new GradientStop(color, offset));
            }
            brush.Freeze();
            return brush;
        }

        public static Color GetStateColor(this EquipmentState state)
        {
            return state switch
            {
                EquipmentState.Stopped => Color.FromRgb(128, 128, 128),
                EquipmentState.Running => Color.FromRgb(0, 255, 0),
                EquipmentState.Warning => Color.FromRgb(255, 255, 0),
                EquipmentState.Alarm => Color.FromRgb(255, 0, 0),
                EquipmentState.Maintenance => Color.FromRgb(0, 120, 255),
                _ => Color.FromRgb(128, 128, 128)
            };
        }

        public static SolidColorBrush ToFrozenBrush(this Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }

    public static class GeometryHelper
    {
        public static PathGeometry CreateRoundedRect(Rect rect, double radius)
        {
            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new Point(rect.Left + radius, rect.Top), IsClosed = true };

            figure.Segments.Add(new LineSegment(new Point(rect.Right - radius, rect.Top), true));
            figure.Segments.Add(new ArcSegment(new Point(rect.Right, rect.Top + radius), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true));
            figure.Segments.Add(new LineSegment(new Point(rect.Right, rect.Bottom - radius), true));
            figure.Segments.Add(new ArcSegment(new Point(rect.Right - radius, rect.Bottom), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true));
            figure.Segments.Add(new LineSegment(new Point(rect.Left + radius, rect.Bottom), true));
            figure.Segments.Add(new ArcSegment(new Point(rect.Left, rect.Bottom - radius), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true));
            figure.Segments.Add(new LineSegment(new Point(rect.Left, rect.Top + radius), true));
            figure.Segments.Add(new ArcSegment(new Point(rect.Left + radius, rect.Top), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true));

            geometry.Figures.Add(figure);
            geometry.Freeze();
            return geometry;
        }

        public static PathGeometry CreateCapsule(Rect rect, bool horizontal = true)
        {
            var radius = horizontal ? rect.Height / 2 : rect.Width / 2;
            return CreateRoundedRect(rect, radius);
        }

        public static PathGeometry CreateTrapezoid(double topWidth, double bottomWidth, double height, bool centered = true)
        {
            var geometry = new PathGeometry();
            var figure = new PathFigure { IsClosed = true };

            var leftOffset = centered ? (bottomWidth - topWidth) / 2 : 0;

            figure.StartPoint = new Point(leftOffset, 0);
            figure.Segments.Add(new LineSegment(new Point(leftOffset + topWidth, 0), true));
            figure.Segments.Add(new LineSegment(new Point(leftOffset + bottomWidth, height), true));
            figure.Segments.Add(new LineSegment(new Point(0, height), true));

            geometry.Figures.Add(figure);
            geometry.Freeze();
            return geometry;
        }

        public static PathGeometry CreatePipePath(Point start, Point end, double diameter, bool isElbow = false, Elbow90Pipe.ElbowDirection direction = Elbow90Pipe.ElbowDirection.TopRight)
        {
            var geometry = new PathGeometry();

            if (isElbow)
            {
                var figure = new PathFigure { StartPoint = start };
                Point controlPoint;

                switch (direction)
                {
                    case Elbow90Pipe.ElbowDirection.TopRight:
                        controlPoint = new Point(end.X, start.Y);
                        break;
                    case Elbow90Pipe.ElbowDirection.TopLeft:
                        controlPoint = new Point(start.X, end.Y);
                        break;
                    case Elbow90Pipe.ElbowDirection.BottomRight:
                        controlPoint = new Point(start.X, end.Y);
                        break;
                    case Elbow90Pipe.ElbowDirection.BottomLeft:
                        controlPoint = new Point(end.X, start.Y);
                        break;
                    default:
                        controlPoint = new Point(end.X, start.Y);
                        break;
                }

                figure.Segments.Add(new QuadraticBezierSegment(controlPoint, end, true));
                geometry.Figures.Add(figure);
            }
            else
            {
                var figure = new PathFigure { StartPoint = start };
                figure.Segments.Add(new LineSegment(end, true));
                geometry.Figures.Add(figure);
            }

            geometry.Freeze();
            return geometry;
        }

        public static Point GetElbowControlPoint(Point start, Point end, Elbow90Pipe.ElbowDirection direction)
        {
            return direction switch
            {
                Elbow90Pipe.ElbowDirection.TopRight => new Point(end.X, start.Y),
                Elbow90Pipe.ElbowDirection.TopLeft => new Point(start.X, end.Y),
                Elbow90Pipe.ElbowDirection.BottomRight => new Point(start.X, end.Y),
                Elbow90Pipe.ElbowDirection.BottomLeft => new Point(end.X, start.Y),
                _ => new Point(end.X, start.Y)
            };
        }
    }
}