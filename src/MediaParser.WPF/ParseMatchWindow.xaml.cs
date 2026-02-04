using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Threading;
using MediaParser.WPF.ViewModels;

namespace MediaParser.WPF;

/// <summary>
/// Parse & Match 窗口
/// 
/// 职责：
/// 1. 显示元数据解析面板
/// 2. 显示文件列表和预览
/// 3. 允许用户分配文件到剧集
/// </summary>
public partial class ParseMatchWindow : Window
{
    private MainViewModel _viewModel;
    private DispatcherTimer _progressTimer;
    private bool _isDragging = false;

    public ParseMatchWindow()
    {
        InitializeComponent();
        
        // 设置数据上下文
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        
        // 订阅返回命令
        _viewModel.BackCommand = new RelayCommand(() => 
        {
            this.Close();
        });
        
        // 初始化进度更新定时器
        _progressTimer = new DispatcherTimer();
        _progressTimer.Interval = TimeSpan.FromMilliseconds(100);
        _progressTimer.Tick += OnProgressTimerTick;
        
        // 当选中视频变化时加载视频
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>
    /// 处理 ViewModel 属性变化
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsPlaying))
        {
            if (_viewModel.IsPlaying)
            {
                VideoPlayer?.Play();
                _progressTimer.Start();
            }
            else
            {
                VideoPlayer?.Pause();
                _progressTimer.Stop();
            }
        }
        else if (e.PropertyName == nameof(MainViewModel.SelectedVideo))
        {
            // 选中视频变化时重置播放状态
            _viewModel.IsPlaying = false;
            _viewModel.VideoProgress = 0;
            _viewModel.VideoCurrentTime = 0;
            _viewModel.VideoDuration = 0;
        }
    }

    /// <summary>
    /// 带参数的构造函数（从主窗口传递数据）
    /// </summary>
    public ParseMatchWindow(MainViewModel mainViewModel) : this()
    {
        // 复制视图模型数据
        _viewModel.Videos = mainViewModel.Videos.Where(v => v.IsChecked).ToList();
        _viewModel.Images = mainViewModel.Images.Where(i => i.IsChecked).ToList();
        _viewModel.Show = mainViewModel.Show;
        _viewModel.CurrentFolderPath = mainViewModel.CurrentFolderPath;
        
        // 重置选中状态
        foreach (var video in _viewModel.Videos)
        {
            video.IsSelected = false;
            video.IsChecked = true;
        }
        foreach (var image in _viewModel.Images)
        {
            image.IsSelected = false;
            image.IsChecked = true;
        }
        
        // 设置第一个文件为选中状态（用于预览）
        if (_viewModel.Videos.Count > 0)
        {
            // 确保选中视频
            _viewModel.SelectVideoCommand.Execute(_viewModel.Videos[0]);
        }
        else if (_viewModel.Images.Count > 0)
        {
            _viewModel.SelectImageCommand.Execute(_viewModel.Images[0]);
        }
    }

    /// <summary>
    /// 处理窗口关闭事件
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        // 停止播放
        _viewModel.IsPlaying = false;
        _progressTimer.Stop();
        
        base.OnClosing(e);
    }

    /// <summary>
    /// 处理鼠标拖动窗口
    /// </summary>
    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        this.DragMove();
    }
    
    /// <summary>
    /// 视频点击播放/暂停
    /// </summary>
    private void OnVideoPreviewClick(object sender, MouseButtonEventArgs e)
    {
        TogglePlayPause();
    }
    
    /// <summary>
    /// 切换播放/暂停状态
    /// </summary>
    private void TogglePlayPause()
    {
        if (VideoPlayer?.Source == null)
            return;
            
        if (_viewModel.IsPlaying)
        {
            // 暂停
            VideoPlayer.Pause();
            _viewModel.IsPlaying = false;
            _progressTimer.Stop();
        }
        else
        {
            // 播放
            VideoPlayer.Play();
            _viewModel.IsPlaying = true;
            _progressTimer.Start();
        }
    }
    
    /// <summary>
    /// 视频打开完成
    /// </summary>
    private void OnVideoMediaOpened(object sender, RoutedEventArgs e)
    {
        if (VideoPlayer?.NaturalDuration.HasTimeSpan == true)
        {
            _viewModel.VideoDuration = VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
        }
    }
    
    /// <summary>
    /// 视频播放结束
    /// </summary>
    private void OnVideoMediaEnded(object sender, RoutedEventArgs e)
    {
        _viewModel.IsPlaying = false;
        _viewModel.VideoProgress = 0;
        _progressTimer.Stop();
    }
    
    /// <summary>
    /// 视频加载失败
    /// </summary>
    private void OnVideoMediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        _viewModel.StatusMessage = $"视频加载失败: {e.ErrorException?.Message}";
        _viewModel.IsPlaying = false;
    }
    
    /// <summary>
    /// 进度条拖动开始
    /// </summary>
    private void OnVideoProgressDragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
    {
        _isDragging = true;
        _progressTimer.Stop();
    }
    
    /// <summary>
    /// 进度条拖动完成
    /// </summary>
    private void OnVideoProgressDragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
    {
        _isDragging = false;
        OnVideoSeekRequested(_viewModel.VideoProgress);
        
        if (_viewModel.IsPlaying)
        {
            _progressTimer.Start();
        }
    }
    
    /// <summary>
    /// 点击进度条跳转
    /// </summary>
    private void OnVideoProgressMouseDown(object sender, MouseButtonEventArgs e)
    {
        var slider = sender as System.Windows.Controls.Slider;
        if (slider != null && VideoPlayer.NaturalDuration.HasTimeSpan)
        {
            var position = e.GetPosition(slider);
            var track = slider.Template.FindName("PART_Track", slider) as System.Windows.Controls.Primitives.Track;
            if (track?.Thumb != null)
            {
                var thumbPos = e.GetPosition(track.Thumb);
                if (double.IsNaN(thumbPos.X) == false)
                {
                    var value = track.ValueFromPoint(position);
                    if (double.IsNaN(value) == false)
                    {
                        slider.Value = value;
                        OnVideoSeekRequested(value);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 进度更新定时器
    /// </summary>
    private void OnProgressTimerTick(object? sender, EventArgs e)
    {
        if (_isDragging || VideoPlayer?.Source == null || !_viewModel.IsPlaying)
            return;
            
        try
        {
            // 更新当前时间
            if (VideoPlayer.Position.TotalSeconds > 0)
            {
                _viewModel.VideoCurrentTime = VideoPlayer.Position.TotalSeconds;
            }
            
            // 更新进度条
            if (_viewModel.VideoDuration > 0)
            {
                _viewModel.VideoProgress = (_viewModel.VideoCurrentTime / _viewModel.VideoDuration) * 100;
            }
            
            // 检测播放结束
            if (_viewModel.VideoCurrentTime >= _viewModel.VideoDuration - 0.5)
            {
                _viewModel.IsPlaying = false;
                _viewModel.VideoProgress = 100;
                _progressTimer.Stop();
            }
        }
        catch
        {
            // 忽略更新错误
        }
    }
    
    /// <summary>
    /// 执行视频跳转
    /// </summary>
    private void OnVideoSeekRequested(double progress)
    {
        if (VideoPlayer?.Source == null || !VideoPlayer.NaturalDuration.HasTimeSpan)
            return;
            
        var position = TimeSpan.FromSeconds((progress / 100) * _viewModel.VideoDuration);
        VideoPlayer.Position = position;
        _viewModel.VideoCurrentTime = position.TotalSeconds;
    }
    
    /// <summary>
    /// 图片预览区域鼠标滚轮缩放
    /// </summary>
    private void OnImagePreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (_viewModel.SelectedImage == null)
            return;

        if (e.Delta > 0)
        {
            // 放大
            _viewModel.ImageZoom = Math.Min(_viewModel.ImageZoom * 1.1, 5.0);
        }
        else
        {
            // 缩小
            _viewModel.ImageZoom = Math.Max(_viewModel.ImageZoom / 1.1, 0.1);
        }
    }
}
