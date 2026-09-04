using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Converters
{
    public class EquipmentStateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.EquipmentState state)
            {
                return state switch
                {
                    Models.EquipmentState.Running => Brushes.LimeGreen,
                    Models.EquipmentState.Stopped => Brushes.Gray,
                    Models.EquipmentState.Warning => Brushes.Yellow,
                    Models.EquipmentState.Alarm => Brushes.Red,
                    Models.EquipmentState.Maintenance => Brushes.Orange,
                    _ => Brushes.Gray
                };
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class DiameterToShadowBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                var brush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    MappingMode = BrushMappingMode.RelativeToBoundingBox
                };
                brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.0));
                brush.GradientStops.Add(new GradientStop(Color.FromArgb(80, 0, 0, 0), 0.5));
                brush.GradientStops.Add(new GradientStop(Color.FromArgb(160, 0, 0, 0), 1.0));
                brush.Freeze();
                return brush;
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class DiameterToShadowThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return d * 1.5;
            return 30;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class DiameterToHighlightThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return Math.Max(1, d * 0.15);
            return 3;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class FlowDirectionToDashOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.FlowDirection dir)
                return dir == Models.FlowDirection.Forward ? 0 : 20;
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class DiameterToCanvasLeftConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return -d / 2;
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class DiameterToCanvasTopConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return -d / 2;
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class DiameterToHighlightOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return -d * 0.35;
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class PipeTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.PipeType type)
                return type != Models.PipeType.None ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class PipeTypeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.PipeType type)
            {
                return type switch
                {
                    Models.PipeType.CarbonConveying => CreateCarbonBrush(),
                    Models.PipeType.OilSupply => CreateOilBrush(),
                    Models.PipeType.CoolingWater => CreateWaterBrush(),
                    Models.PipeType.CompressedAir => CreateAirBrush(),
                    Models.PipeType.DustCollection => CreateDustBrush(),
                    _ => Brushes.Gray
                };
            }
            return Brushes.Gray;
        }

        private LinearGradientBrush CreateCarbonBrush()
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                MappingMode = BrushMappingMode.RelativeToBoundingBox
            };
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x10, 0x10, 0x10), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x1A, 0x1A, 0x1A), 0.3));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x15, 0x15, 0x15), 0.7));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x0A, 0x0A, 0x0A), 1.0));
            brush.Freeze();
            return brush;
        }

        private LinearGradientBrush CreateOilBrush()
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                MappingMode = BrushMappingMode.RelativeToBoundingBox
            };
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xD7, 0x00), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xDA, 0xA5, 0x20), 0.4));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xB8, 0x86, 0x0B), 0.8));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x8B, 0x69, 0x14), 1.0));
            brush.Freeze();
            return brush;
        }

        private LinearGradientBrush CreateWaterBrush()
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                MappingMode = BrushMappingMode.RelativeToBoundingBox
            };
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x00, 0x66, 0xCC), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x00, 0x44, 0x99), 0.5));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x00, 0x22, 0x66), 1.0));
            brush.Freeze();
            return brush;
        }

        private LinearGradientBrush CreateAirBrush()
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                MappingMode = BrushMappingMode.RelativeToBoundingBox
            };
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x88, 0xAA, 0xCC), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x66, 0x88, 0xAA), 0.5));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x44, 0x66, 0x88), 1.0));
            brush.Freeze();
            return brush;
        }

        private LinearGradientBrush CreateDustBrush()
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                MappingMode = BrushMappingMode.RelativeToBoundingBox
            };
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x55, 0x55, 0x55), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x33, 0x33, 0x33), 0.5));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x11, 0x11, 0x11), 1.0));
            brush.Freeze();
            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class PipeTypeToParticleColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.PipeType type)
            {
                return type switch
                {
                    Models.PipeType.CarbonConveying => Brushes.White,
                    Models.PipeType.OilSupply => Brushes.Gold,
                    Models.PipeType.CoolingWater => Brushes.LightBlue,
                    Models.PipeType.CompressedAir => Brushes.White,
                    Models.PipeType.DustCollection => Brushes.LightGray,
                    _ => Brushes.White
                };
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class PipeTypeToArrowColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.PipeType type)
            {
                return type switch
                {
                    Models.PipeType.CarbonConveying => Brushes.Orange,
                    Models.PipeType.OilSupply => Brushes.Gold,
                    Models.PipeType.CoolingWater => Brushes.Cyan,
                    Models.PipeType.CompressedAir => Brushes.White,
                    Models.PipeType.DustCollection => Brushes.Gray,
                    _ => Brushes.White
                };
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToConnectedBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool connected)
                return connected ? Brushes.LimeGreen : Brushes.DodgerBlue;
            return Brushes.DodgerBlue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class DiameterToShadowWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double w)
                return w * 0.8;
            return 80;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class PointToCanvasLeftConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Point p)
                return p.X - 7;
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class PointToCanvasTopConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Point p)
                return p.Y - 7;
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "";
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToYesNoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "是" : "否";
            return "否";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToCurrentTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "当前" : "历史";
            return "历史";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToLoadingTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "加载中..." : "就绪";
            return "就绪";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EquipmentState state)
            {
                return state switch
                {
                    EquipmentState.Running => Brushes.LimeGreen,
                    EquipmentState.Stopped => Brushes.Gray,
                    EquipmentState.Warning => Brushes.Gold,
                    EquipmentState.Alarm => Brushes.Red,
                    EquipmentState.Maintenance => Brushes.Blue,
                    _ => Brushes.Gray
                };
            }

            if (value is RecipeStatus rStatus)
            {
                return rStatus switch
                {
                    RecipeStatus.Draft => Brushes.Gray,
                    RecipeStatus.PendingReview => Brushes.Orange,
                    RecipeStatus.Approved => Brushes.DodgerBlue,
                    RecipeStatus.Released => Brushes.LimeGreen,
                    RecipeStatus.Archived => Brushes.Purple,
                    RecipeStatus.Obsolete => Brushes.DarkRed,
                    _ => Brushes.Gray
                };
            }

            if (value is BatchStatus bStatus)
            {
                return bStatus switch
                {
                    BatchStatus.Created => Brushes.Gray,
                    BatchStatus.Weighing => Brushes.Orange,
                    BatchStatus.Weighed => Brushes.DodgerBlue,
                    BatchStatus.Mixing => Brushes.DeepSkyBlue,
                    BatchStatus.Mixed => Brushes.LimeGreen,
                    BatchStatus.Dumped => Brushes.Cyan,
                    BatchStatus.Completed => Brushes.LimeGreen,
                    BatchStatus.Aborted => Brushes.Red,
                    BatchStatus.OnHold => Brushes.Yellow,
                    _ => Brushes.Gray
                };
            }

            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class MaterialTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MaterialType mType)
            {
                return mType switch
                {
                    MaterialType.RawMaterial => Brushes.LimeGreen,
                    MaterialType.SemiFinished => Brushes.DodgerBlue,
                    MaterialType.FinishedProduct => Brushes.Gold,
                    MaterialType.Additive => Brushes.Orange,
                    MaterialType.Packaging => Brushes.Purple,
                    MaterialType.Other => Brushes.Gray,
                    _ => Brushes.White
                };
            }

            if (value is RecipeType rType)
            {
                return rType switch
                {
                    RecipeType.Weighing => Brushes.LimeGreen,
                    RecipeType.Mixing => Brushes.DodgerBlue,
                    RecipeType.Extrusion => Brushes.Orange,
                    RecipeType.Calendering => Brushes.Purple,
                    RecipeType.Other => Brushes.Gray,
                    _ => Brushes.White
                };
            }

            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StepTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is StepType sType)
            {
                return sType switch
                {
                    StepType.AddMaterial => Brushes.LimeGreen,
                    StepType.Mix => Brushes.DodgerBlue,
                    StepType.Heat => Brushes.Orange,
                    StepType.Cool => Brushes.Cyan,
                    StepType.Dump => Brushes.Gold,
                    StepType.Wait => Brushes.Yellow,
                    StepType.Custom => Brushes.Purple,
                    _ => Brushes.White
                };
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class DownloadStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DownloadStatus dStatus)
            {
                return dStatus switch
                {
                    DownloadStatus.Pending => Brushes.Gray,
                    DownloadStatus.InProgress => Brushes.Orange,
                    DownloadStatus.Success => Brushes.LimeGreen,
                    DownloadStatus.Failed => Brushes.Red,
                    DownloadStatus.Partial => Brushes.Yellow,
                    DownloadStatus.Verified => Brushes.DodgerBlue,
                    _ => Brushes.Gray
                };
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BatchStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BatchStatus bStatus)
            {
                return bStatus switch
                {
                    BatchStatus.Created => Brushes.Gray,
                    BatchStatus.Weighing => Brushes.Orange,
                    BatchStatus.Weighed => Brushes.DodgerBlue,
                    BatchStatus.Mixing => Brushes.DeepSkyBlue,
                    BatchStatus.Mixed => Brushes.LimeGreen,
                    BatchStatus.Dumped => Brushes.Cyan,
                    BatchStatus.Completed => Brushes.LimeGreen,
                    BatchStatus.Aborted => Brushes.Red,
                    BatchStatus.OnHold => Brushes.Yellow,
                    _ => Brushes.Gray
                };
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ReportTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ReportType type)
            {
                return type switch
                {
                    ReportType.Shift => Brushes.Orange,
                    ReportType.Daily => Brushes.DodgerBlue,
                    ReportType.Monthly => Brushes.Purple,
                    ReportType.Yearly => Brushes.Gold,
                    ReportType.Custom => Brushes.Gray,
                    _ => Brushes.White
                };
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToGeneratingTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "生成中..." : "就绪";
            return "就绪";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToAutoRefreshTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "自动刷新: 开启" : "自动刷新: 关闭";
            return "自动刷新: 关闭";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class RefreshIntervalTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int seconds)
                return $"({seconds}秒)";
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class TotalPointsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class CountToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (value is long lCount)
                return lCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToAccentBgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? new SolidColorBrush(Color.FromRgb(0x19, 0x76, 0xD2)) : new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A));
            return new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class CountToCategoryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
                return $"{count} 类";
            if (value is long lCount)
                return $"{lCount} 类";
            return "0 类";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}