using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaParser.Core.Workflow;

namespace MediaParser.WPF.Converters;

/// <summary>
/// 工作流状态到布尔值的转换器
/// 用于 UI 按钮的 IsEnabled 绑定
/// 
/// 强制约束：UI 按钮的 IsEnabled 必须绑定到 WorkflowController.CurrentState
/// </summary>
[ValueConversion(typeof(WorkflowState), typeof(bool))]
public class WorkflowStateToBoolConverter : IValueConverter
{
    /// <summary>
    /// 目标状态（当 WorkflowState 等于此值时返回 true）
    /// </summary>
    public WorkflowState TargetState { get; set; } = WorkflowState.Ready;

    /// <summary>
    /// 反转结果
    /// </summary>
    public bool Invert { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is WorkflowState state)
        {
            var result = state == TargetState;
            return Invert ? !result : result;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 多状态转换器（支持多个目标状态）
/// </summary>
[ValueConversion(typeof(WorkflowState), typeof(bool))]
public class WorkflowStateMultiConverter : IMultiValueConverter
{
    public WorkflowState[] TargetStates { get; set; } = { WorkflowState.Ready };

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length > 0 && values[0] is WorkflowState state)
        {
            return TargetStates.Contains(state);
        }
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 状态到可视性转换器
/// </summary>
[ValueConversion(typeof(WorkflowState), typeof(Visibility))]
public class WorkflowStateToVisibilityConverter : IValueConverter
{
    public WorkflowState VisibleState { get; set; } = WorkflowState.Ready;
    public bool Invert { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is WorkflowState state)
        {
            var isVisible = state == VisibleState;
            return Invert 
                ? (isVisible ? Visibility.Collapsed : Visibility.Visible)
                : (isVisible ? Visibility.Visible : Visibility.Collapsed);
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 状态到字符串转换器
/// </summary>
[ValueConversion(typeof(WorkflowState), typeof(string))]
public class WorkflowStateToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is WorkflowState state)
        {
            return state switch
            {
                WorkflowState.Draft => "准备中",
                WorkflowState.Ready => "就绪",
                WorkflowState.Archived => "已完成",
                _ => "未知"
            };
        }
        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 状态到颜色转换器
/// </summary>
[ValueConversion(typeof(WorkflowState), typeof(Brush))]
public class WorkflowStateToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is WorkflowState state)
        {
            return state switch
            {
                WorkflowState.Draft => Brushes.Orange,
                WorkflowState.Ready => Brushes.Green,
                WorkflowState.Archived => Brushes.Blue,
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值到颜色转换器（绿/红）
/// </summary>
[ValueConversion(typeof(bool), typeof(Brush))]
public class BoolToGreenRedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Brushes.Green : Brushes.Red;
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值反转转换器
/// </summary>
[ValueConversion(typeof(bool), typeof(bool))]
public class BoolInvertConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}

/// <summary>
/// 列表到字符串转换器
/// </summary>
[ValueConversion(typeof(System.Collections.IList), typeof(string))]
public class ListToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Collections.IList list)
        {
            return string.Join(", ", list.Cast<object>().Select(o => o?.ToString() ?? ""));
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 字节数到人类可读格式转换器
/// </summary>
[ValueConversion(typeof(long), typeof(string))]
public class BytesToHumanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
        return "0 B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 文件路径到 BitmapImage 转换器
/// </summary>
[ValueConversion(typeof(string), typeof(BitmapSource))]
public class PathToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string path && File.Exists(path))
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
