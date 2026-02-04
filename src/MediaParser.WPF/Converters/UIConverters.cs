using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MediaParser.Core.Models;

namespace MediaParser.WPF.Converters;

/// <summary>
/// 空值到可视性转换器（支持反转）
/// </summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public class NullToVisibilityConverter : IValueConverter
{
    public bool IsInvert { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isNull = value == null;
        var result = IsInvert ? isNull : !isNull;
        return result ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值到可视性转换器（支持反转）
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    public bool IsInvert { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            var result = IsInvert ? !boolValue : boolValue;
            return result ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 文件类型到图标转换器
/// </summary>
[ValueConversion(typeof(string), typeof(string))]
public class FileTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string fileType)
        {
            return fileType.ToLowerInvariant() switch
            {
                "video" or "mp4" or "mkv" or "avi" or "mov" or "wmv" => "🎬",
                "image" or "jpg" or "jpeg" or "png" or "gif" or "bmp" or "webp" => "🖼️",
                _ => "📄"
            };
        }
        return "📄";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 文件类型到颜色转换器
/// </summary>
[ValueConversion(typeof(string), typeof(Brush))]
public class FileTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string fileType)
        {
            return fileType.ToLowerInvariant() switch
            {
                "video" or "mp4" or "mkv" or "avi" or "mov" or "wmv" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                "image" or "jpg" or "jpeg" or "png" or "gif" or "bmp" or "webp" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#18181B"))
            };
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#18181B"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 视频文件到时长字符串转换器
/// </summary>
[ValueConversion(typeof(VideoFile), typeof(string))]
public class VideoDurationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is VideoFile video)
        {
            if (video.DurationSeconds > 0)
            {
                var minutes = video.DurationSeconds / 60;
                var seconds = video.DurationSeconds % 60;
                return $"{minutes}:{seconds:D2}";
            }
        }
        return "0:00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 视频文件到分辨率字符串转换器
/// </summary>
[ValueConversion(typeof(VideoFile), typeof(string))]
public class VideoResolutionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is VideoFile video)
        {
            return string.IsNullOrEmpty(video.Resolution) 
                ? "Unknown" 
                : video.Resolution;
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 图片文件到尺寸字符串转换器
/// </summary>
[ValueConversion(typeof(ImageAsset), typeof(string))]
public class ImageDimensionsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ImageAsset image)
        {
            if (image.Width > 0 && image.Height > 0)
            {
                return $"{image.Width} × {image.Height}";
            }
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 文件大小到字符串转换器
/// </summary>
[ValueConversion(typeof(long), typeof(string))]
public class FileSizeConverter : IValueConverter
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
            return $"{len:0.#} {sizes[order]}";
        }
        return "0 B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 对象相等性到布尔值转换器
/// </summary>
[ValueConversion(typeof(object), typeof(bool))]
public class ObjectEqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == parameter;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 秒数到时间字符串转换器（mm:ss 或 hh:mm:ss）
/// </summary>
[ValueConversion(typeof(double), typeof(string))]
public class SecondsToTimeStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double seconds)
        {
            if (seconds < 0) return "00:00";
            
            var timeSpan = TimeSpan.FromSeconds(seconds);
            
            // 如果超过1小时，显示 hh:mm:ss，否则显示 mm:ss
            if (timeSpan.TotalHours >= 1)
            {
                return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            }
            return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
        return "00:00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 图片角色到索引转换器
/// </summary>
[ValueConversion(typeof(ImageRole), typeof(int))]
public class ImageRoleToIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ImageRole role)
        {
            return role switch
            {
                ImageRole.Poster => 0,
                ImageRole.Fanart => 1,
                _ => -1
            };
        }
        return -1;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return index switch
            {
                0 => ImageRole.Poster,
                1 => ImageRole.Fanart,
                _ => ImageRole.Unknown
            };
        }
        return ImageRole.Unknown;
    }
}

/// <summary>
/// EpisodeIndex 到显示字符串转换器（用于调试显示）
/// 显示 EpisodeIndex 和 MappedEpisodeNumber 的值
/// </summary>
[ValueConversion(typeof(int), typeof(string))]
public class EpisodeIndexToDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // 只传入一个参数时，显示当前值
        if (value is int episodeIndex)
        {
            return episodeIndex < 0 ? "未选择" : $"EpisodeIndex: {episodeIndex}";
        }
        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}