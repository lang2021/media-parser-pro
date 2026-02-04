using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MediaParser.WPF.ViewModels;

namespace MediaParser.WPF;

/// <summary>
/// 主窗口
/// 
/// 职责：
/// 1. 加载主视图模型
/// 2. 提供 UI 布局框架
/// 3. 响应用户操作
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel _viewModel;
    private DispatcherTimer _progressTimer;
    private bool _isDraggingSlider;

    public MainWindow()
    {
        InitializeComponent();
        
        // 设置数据上下文
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        
        // 订阅打开解析窗口事件
        _viewModel.OpenParseMatchWindow += OnOpenParseMatchWindow;
        
        // 订阅视频跳转事件
        _viewModel.VideoSeekRequested += OnVideoSeekRequested;
        
        // 初始化进度更新定时器
        _progressTimer = new DispatcherTimer();
        _progressTimer.Interval = TimeSpan.FromMilliseconds(100);
        _progressTimer.Tick += OnProgressTimerTick;
        
        // 当选中视频变化时加载视频
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>
    /// 处理文件 checkbox 状态变化事件
    /// </summary>
    private void OnFileCheckChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.RefreshCheckedCount();
        }
    }

    /// <summary>
    /// 视图模型属性变化处理
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsPlaying))
        {
            if (_viewModel.IsPlaying)
            {
                VideoPlayer.Play();
                _progressTimer.Start();
            }
            else
            {
                VideoPlayer.Pause();
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
    /// 进度定时器 Tick 事件
    /// </summary>
    private void OnProgressTimerTick(object? sender, System.EventArgs e)
    {
        if (_isDraggingSlider || VideoPlayer.NaturalDuration.HasTimeSpan == false)
            return;

        var currentTime = VideoPlayer.Position.TotalSeconds;
        var totalTime = VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;

        if (totalTime > 0)
        {
            _viewModel.VideoCurrentTime = currentTime;
            _viewModel.VideoProgress = (currentTime / totalTime) * 100;
            _viewModel.VideoDuration = totalTime;
        }
    }

    /// <summary>
    /// 视频播放完成事件
    /// </summary>
    private void OnVideoMediaEnded(object sender, System.EventArgs e)
    {
        _viewModel.IsPlaying = false;
        _viewModel.VideoProgress = 100;
        _progressTimer.Stop();
    }

    /// <summary>
    /// 视频加载完成事件
    /// </summary>
    private void OnVideoMediaOpened(object sender, RoutedEventArgs e)
    {
        if (VideoPlayer.NaturalDuration.HasTimeSpan)
        {
            _viewModel.VideoDuration = VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
        }
    }

    /// <summary>
    /// 视频播放失败事件
    /// </summary>
    private void OnVideoMediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        _viewModel.IsPlaying = false;
        MessageBox.Show($"视频播放失败: {e.ErrorException?.Message ?? "未知错误"}", "播放错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>
    /// 视频跳转请求处理
    /// </summary>
    private void OnVideoSeekRequested(double progress)
    {
        if (VideoPlayer.NaturalDuration.HasTimeSpan == false)
            return;

        var totalTime = VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
        var newPosition = TimeSpan.FromSeconds(totalTime * progress / 100);
        VideoPlayer.Position = newPosition;
        _viewModel.VideoCurrentTime = newPosition.TotalSeconds;
    }

    /// <summary>
    /// 视频预览区域点击（切换播放/暂停）
    /// </summary>
    private void OnVideoPreviewClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedVideo != null)
        {
            _viewModel.IsPlaying = !_viewModel.IsPlaying;
        }
    }

    /// <summary>
    /// 进度条拖拽开始
    /// </summary>
    private void OnVideoProgressDragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
    {
        _isDraggingSlider = true;
    }

    /// <summary>
    /// 进度条拖拽完成
    /// </summary>
    private void OnVideoProgressDragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
    {
        _isDraggingSlider = false;
        if (VideoPlayer.NaturalDuration.HasTimeSpan)
        {
            var slider = sender as System.Windows.Controls.Slider;
            if (slider != null)
            {
                var progress = slider.Value;
                OnVideoSeekRequested(progress);
            }
        }
    }

    /// <summary>
    /// 进度条鼠标点击
    /// </summary>
    private void OnVideoProgressMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var slider = sender as System.Windows.Controls.Slider;
        if (slider != null && VideoPlayer.NaturalDuration.HasTimeSpan)
        {
            var position = e.GetPosition(slider);
            var track = slider.Template.FindName("PART_Track", slider) as System.Windows.Controls.Primitives.Track;
            if (track != null)
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

    /// <summary>
    /// 打开解析窗口
    /// </summary>
    private void OnOpenParseMatchWindow()
    {
        var checkedCount = _viewModel.CheckedFilesCount;
        if (checkedCount == 0)
        {
            MessageBox.Show("请先选择要处理的文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 隐藏主窗口
        this.Hide();

        // 创建并显示解析窗口
        var parseWindow = new ParseMatchWindow(_viewModel);
        parseWindow.Owner = this;
        parseWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        parseWindow.Closed += (s, args) =>
        {
            // 解析窗口关闭后显示主窗口
            this.Show();
            this.Activate();
        };
        parseWindow.ShowDialog();
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
