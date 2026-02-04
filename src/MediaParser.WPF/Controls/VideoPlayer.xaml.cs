using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MediaParser.WPF.Controls;

/// <summary>
/// 视频播放器控件
/// 使用 WPF MediaElement 实现视频预览功能
/// </summary>
public class VideoPlayer : Control
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(string),
        typeof(VideoPlayer),
        new PropertyMetadata(null, OnSourceChanged));

    public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(
        nameof(IsPlaying),
        typeof(bool),
        typeof(VideoPlayer),
        new PropertyMetadata(false));

    public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register(
        nameof(Volume),
        typeof(double),
        typeof(VideoPlayer),
        new PropertyMetadata(1.0));

    public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(
        nameof(Stretch),
        typeof(Stretch),
        typeof(VideoPlayer),
        new PropertyMetadata(Stretch.Uniform));

    public string? Source
    {
        get => (string?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public double Volume
    {
        get => (double)GetValue(VolumeProperty);
        set => SetValue(VolumeProperty, Math.Clamp(value, 0.0, 1.0));
    }

    public Stretch Stretch
    {
        get => (Stretch)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    static VideoPlayer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(VideoPlayer), new FrameworkPropertyMetadata(typeof(VideoPlayer)));
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // 源更改时的处理
    }
}
