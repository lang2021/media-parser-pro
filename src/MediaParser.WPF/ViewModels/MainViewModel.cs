using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using MediaParser.Core.Models;
using MediaParser.Core.Modules.ArchiveProcessor;
using MediaParser.Core.Modules.ImageManager;
using MediaParser.Core.Modules.MediaValidator;
using MediaParser.Core.Modules.MetadataParser;
using MediaParser.Core.Workflow;
using Ookii.Dialogs.Wpf;

namespace MediaParser.WPF.ViewModels;

/// <summary>
/// 主视图模型
/// 
/// 职责：
/// 1. 管理所有子模块（MetadataParser, MediaValidator, ImageManager）
/// 2. 协调 WorkflowController
/// 3. 暴露给 UI 绑定的属性和命令
/// 4. UI 可用性绑定到 WorkflowController 状态（强制约束）
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    // Core 模块实例
    private readonly MetadataParser _metadataParser;
    private readonly MediaValidator _mediaValidator;
    private readonly ImageManager _imageManager;
    private readonly ArchiveProcessor _archiveProcessor;
    
    // 工作流控制器（核心决策者）
    private readonly WorkflowController _workflowController;

    // UI 状态
    private bool _isProcessing;
    private string _statusMessage = "就绪";
    private string? _errorMessage;
    private VideoFile? _selectedVideo;
    private ImageAsset? _selectedImage;
    private MediaType _selectedMediaType = MediaType.Series;

    /// <summary>
    /// 打开解析窗口事件
    /// </summary>
    public event Action? OpenParseMatchWindow;

    public MainViewModel()
    {
        _metadataParser = new MetadataParser();
        _mediaValidator = new MediaValidator();
        _imageManager = new ImageManager();
        _archiveProcessor = new ArchiveProcessor();
        _workflowController = new WorkflowController();

        // 订阅工作流状态变化事件
        _workflowController.StateChanged += OnWorkflowStateChanged;
        _workflowController.MetadataChanged += OnMetadataChanged;
        _workflowController.VideosChanged += OnVideosChanged;
        _workflowController.ImagesChanged += OnImagesChanged;

        // 初始化返回命令
        BackCommand = new RelayCommand(() =>
        {
            StatusMessage = "返回主页";
        });
    }

    #region 公开属性

    /// <summary>
    /// 工作流控制器（只读，供 UI 绑定使用）
    /// </summary>
    public WorkflowController WorkflowController => _workflowController;

    /// <summary>
    /// 当前工作流状态（快捷访问）
    /// </summary>
    public WorkflowState CurrentState => _workflowController.CurrentState;

    /// <summary>
    /// 是否可以归档（绑定到按钮 IsEnabled）
    /// </summary>
    public bool CanArchive => _workflowController.CanArchive;

    /// <summary>
    /// 是否已归档
    /// </summary>
    public bool IsArchived => _workflowController.IsArchived;

    /// <summary>
    /// 剧集元数据
    /// </summary>
    public Show? Show
    {
        get => _workflowController.Show;
        set
        {
            if (_workflowController.Show != value)
            {
                _workflowController.Show = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasMetadata));
                OnPropertyChanged(nameof(Episodes)); // 通知 Episodes 也改变了
            }
        }
    }

    /// <summary>
    /// 视频文件列表
    /// </summary>
    public List<VideoFile> Videos
    {
        get => _workflowController.Videos;
        set => _workflowController.Videos = value;
    }

    /// <summary>
    /// 视频文件列表（用于 XAML 绑定兼容）
    /// </summary>
    public List<VideoFile> VideoFiles
    {
        get => Videos;
        set => Videos = value;
    }

    /// <summary>
    /// 图片资源列表
    /// </summary>
    public List<ImageAsset> Images
    {
        get => _workflowController.Images;
        set => _workflowController.Images = value;
    }

    /// <summary>
    /// 图片资源列表（用于 XAML 绑定兼容）
    /// </summary>
    public List<ImageAsset> ImageFiles
    {
        get => Images;
        set => Images = value;
    }

    /// <summary>
    /// 是否存在元数据
    /// </summary>
    public bool HasMetadata => Show != null;

    /// <summary>
    /// 是否正在处理
    /// </summary>
    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            if (_isProcessing != value)
            {
                _isProcessing = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 状态消息
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 选中的媒体类型（剧集/电影）
    /// </summary>
    public MediaType SelectedMediaType
    {
        get => _selectedMediaType;
        set
        {
            if (_selectedMediaType != value)
            {
                _selectedMediaType = value;
                OnPropertyChanged();
            }
        }
    }

/// <summary>
/// 当前选中的视频
/// </summary>
public VideoFile? SelectedVideo
{
    get => _selectedVideo;
    set
    {
        if (_selectedVideo != value)
        {
            // 清除之前选中视频的 IsSelected 状态
            if (_selectedVideo != null)
            {
                _selectedVideo.IsSelected = false;
            }
            
            _selectedVideo = value;
            
            // 设置新选中视频的 IsSelected 状态
            if (_selectedVideo != null)
            {
                _selectedVideo.IsSelected = true;
                // 同时清除图片选中状态
                if (_selectedImage != null)
                {
                    _selectedImage.IsSelected = false;
                    _selectedImage = null;
                    OnPropertyChanged(nameof(SelectedImage));
                    OnPropertyChanged(nameof(SelectedImagePath));
                }
                
                // 如果当前已选中剧集，自动映射视频到该剧集
                if (CurrentEpisodeIndex >= 0)
                {
                    MapVideoToEpisode(_selectedVideo, CurrentEpisodeIndex + 1);
                }
            }
            
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedVideoPath));
            OnPropertyChanged(nameof(SelectedFile));
        }
    }
}

    /// <summary>
    /// 当前选中的视频路径（用于播放器绑定）
    /// </summary>
    public string? SelectedVideoPath => SelectedVideo?.FullPath;

/// <summary>
/// 当前选中的图片
/// </summary>
public ImageAsset? SelectedImage
{
    get => _selectedImage;
    set
    {
        if (_selectedImage != value)
        {
            // 清除之前选中图片的 IsSelected 状态
            if (_selectedImage != null)
            {
                _selectedImage.IsSelected = false;
            }
            
            _selectedImage = value;
            
            // 设置新选中图片的 IsSelected 状态
            if (_selectedImage != null)
            {
                _selectedImage.IsSelected = true;
                // 同时清除视频选中状态
                if (_selectedVideo != null)
                {
                    _selectedVideo.IsSelected = false;
                    _selectedVideo = null;
                    OnPropertyChanged(nameof(SelectedVideo));
                    OnPropertyChanged(nameof(SelectedVideoPath));
                }
            }
            
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedImagePath));
            OnPropertyChanged(nameof(SelectedImageRole));
            OnPropertyChanged(nameof(SelectedFile));
        }
    }
}

    /// <summary>
    /// 当前选中的图片路径（用于图片预览绑定）
    /// </summary>
    public string? SelectedImagePath => SelectedImage?.FullPath;

    /// <summary>
    /// 当前选中的图片角色（用于 ComboBox 绑定）
    /// </summary>
    public ImageRole SelectedImageRole
    {
        get => SelectedImage?.Role ?? ImageRole.Unknown;
        set
        {
            if (SelectedImage != null && SelectedImage.Role != value)
            {
                SetImageRole(SelectedImage, value);
            }
        }
    }

    #region 视频播放控制属性

    /// <summary>
    /// 是否正在播放
    /// </summary>
    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (_isPlaying != value)
            {
                _isPlaying = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlayPauseButtonText));
            }
        }
    }
    private bool _isPlaying;

    /// <summary>
    /// 播放/暂停按钮文字
    /// </summary>
    public string PlayPauseButtonText => IsPlaying ? "⏸" : "▶";

    /// <summary>
    /// 视频播放进度（0-100）
    /// </summary>
    public double VideoProgress
    {
        get => _videoProgress;
        set
        {
            if (_videoProgress != value)
            {
                _videoProgress = value;
                OnPropertyChanged();
            }
        }
    }
    private double _videoProgress;

    /// <summary>
    /// 当前播放时间（秒）
    /// </summary>
    public double VideoCurrentTime
    {
        get => _videoCurrentTime;
        set
        {
            if (_videoCurrentTime != value)
            {
                _videoCurrentTime = value;
                OnPropertyChanged();
            }
        }
    }
    private double _videoCurrentTime;

    /// <summary>
    /// 视频总时长（秒）
    /// </summary>
    public double VideoDuration
    {
        get => _videoDuration;
        set
        {
            if (_videoDuration != value)
            {
                _videoDuration = value;
                OnPropertyChanged();
            }
        }
    }
    private double _videoDuration;

    #endregion

    #region 图片预览缩放控制

    /// <summary>
    /// 图片缩放比例（1.0 = 100%）
    /// </summary>
    public double ImageZoom
    {
        get => _imageZoom;
        set
        {
            if (_imageZoom != value)
            {
                _imageZoom = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ZoomPercentage));
            }
        }
    }
    private double _imageZoom = 1.0;

    /// <summary>
    /// 缩放百分比（用于 UI 显示）
    /// </summary>
    public double ZoomPercentage => ImageZoom * 100;

    /// <summary>
    /// 图片尺寸字符串（用于 UI 显示）
    /// </summary>
    public string SelectedImageDimensions
    {
        get
        {
            if (SelectedImage != null && SelectedImage.Width > 0 && SelectedImage.Height > 0)
            {
                return $"{SelectedImage.Width} × {SelectedImage.Height}";
            }
            return "Unknown";
        }
    }

    #endregion

    /// <summary>
    /// 当前选中的剧集
    /// </summary>
    public Episode? SelectedEpisode
    {
        get => _workflowController.SelectedEpisode;
        set
        {
            if (_workflowController.SelectedEpisode != value)
            {
                _workflowController.SelectedEpisode = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 校验结果摘要
    /// </summary>
    public ValidationSummary? ValidationSummary => _workflowController.ValidationSummary;

/// <summary>
/// 最后的错误消息
/// </summary>
public string LastErrorMessage => _workflowController.LastErrorMessage;

#endregion

#region UI 状态属性（用于新界面）

/// <summary>
/// 当前选择的文件夹路径
/// </summary>
public string? CurrentFolderPath
{
    get => _currentFolderPath;
    set
    {
        if (_currentFolderPath != value)
        {
            _currentFolderPath = value;
            OnPropertyChanged();
        }
    }
}
private string? _currentFolderPath;

/// <summary>
/// 当前选中的文件（视频或图片）
/// </summary>
public object? SelectedFile
{
    get
    {
        if (_selectedVideo != null) return _selectedVideo;
        if (_selectedImage != null) return _selectedImage;
        return null;
    }
}

/// <summary>
/// 已选中文件的数量
/// </summary>
public int CheckedFilesCount
{
    get
    {
        var checkedVideos = Videos.Count(v => v.IsChecked);
        var checkedImages = Images.Count(i => i.IsChecked);
        return checkedVideos + checkedImages;
    }
}

#endregion

    #region 命令

    /// <summary>
    /// 解析元数据命令
    /// </summary>
    public RelayCommand ParseMetadataCommand => new RelayCommand(async () =>
    {
        await ParseMetadataFromTextAsync();
    });

    /// <summary>
    /// 清空元数据命令
    /// </summary>
    public RelayCommand ClearMetadataCommand => new RelayCommand(() =>
    {
        MetadataText = "";
        Show = new Show();
        Episodes.Clear();
        ErrorMessage = "";
    });

    /// <summary>
    /// 选择目录命令
    /// </summary>
    public RelayCommand SelectDirectoryCommand => new RelayCommand(async () =>
    {
        await SelectDirectoryAsync();
    });

    /// <summary>
    /// 解析并生成命令（导航到 Parse & Match 页面）
    /// </summary>
    public RelayCommand ParseGenerateCommand => new RelayCommand(() =>
    {
        var checkedFiles = GetCheckedFiles();
        if (checkedFiles.Count == 0)
        {
            StatusMessage = "请先选择要处理的文件";
            return;
        }
        
        StatusMessage = $"准备处理 {checkedFiles.Count} 个文件...";
        
        // 触发打开解析窗口事件
        OpenParseMatchWindow?.Invoke();
    });

    /// <summary>
    /// 映射视频到集数命令
    /// </summary>
    public RelayCommand<MapEpisodeCommandPayload> MapVideoToEpisodeCommand => 
        new RelayCommand<MapEpisodeCommandPayload>(payload =>
        {
            if (payload != null)
            {
                MapVideoToEpisode(payload.Video, payload.EpisodeNumber);
            }
        });

    /// <summary>
    /// 设置图片角色命令
    /// </summary>
    public RelayCommand<SetImageRoleCommandPayload> SetImageRoleCommand =>
        new RelayCommand<SetImageRoleCommandPayload>(payload =>
        {
            if (payload != null)
            {
                SetImageRole(payload.Image, payload.Role);
            }
        });

    /// <summary>
    /// 尝试归档命令（检查状态并执行）
    /// </summary>
    public RelayCommand ArchiveCommand => new RelayCommand(async () =>
    {
        await ArchiveAsync();
    });

    /// <summary>
    /// 重置命令
    /// </summary>
    public RelayCommand ResetCommand => new RelayCommand(() =>
    {
        Reset();
    });

    /// <summary>
    /// 加载文件命令
    /// </summary>
    public RelayCommand LoadFilesCommand => new RelayCommand(async () =>
    {
        await LoadFilesAsync();
    });

    /// <summary>
    /// 添加视频命令
    /// </summary>
    public RelayCommand AddVideoCommand => new RelayCommand(async () =>
    {
        await AddVideoAsync();
    });

    /// <summary>
    /// 添加图片命令
    /// </summary>
    public RelayCommand AddImageCommand => new RelayCommand(async () =>
    {
        await AddImageAsync();
    });

    /// <summary>
    /// 刷新校验状态命令
    /// </summary>
    public RelayCommand RefreshValidationCommand => new RelayCommand(() =>
    {
        RefreshValidation();
    });

/// <summary>
/// 选择视频命令
/// </summary>
public RelayCommand<VideoFile> SelectVideoCommand => new RelayCommand<VideoFile>(video =>
{
    SelectedVideo = video;
});

/// <summary>
/// 选择图片命令
/// </summary>
public RelayCommand<ImageAsset> SelectImageCommand => new RelayCommand<ImageAsset>(image =>
{
    SelectedImage = image;
});

/// <summary>
    /// 返回命令
    /// </summary>
    public RelayCommand BackCommand { get; set; }

    /// <summary>
    /// 开始处理命令
    /// </summary>
    public RelayCommand StartProcessingCommand => new RelayCommand(async () =>
    {
        await ArchiveAsync();
    });

/// <summary>
/// 人脸映射命令
/// </summary>
public RelayCommand FaceMappingCommand => new RelayCommand(async () =>
{
    await FaceMappingAsync();
});

/// <summary>
/// 播放/暂停视频命令
/// </summary>
public RelayCommand PlayPauseCommand => new RelayCommand(() =>
{
    IsPlaying = !IsPlaying;
    if (IsPlaying)
    {
        StatusMessage = $"正在播放: {SelectedVideo?.FileName}";
    }
    else
    {
        StatusMessage = "已暂停";
    }
});

/// <summary>
/// 停止视频命令
/// </summary>
public RelayCommand StopVideoCommand => new RelayCommand(() =>
{
    IsPlaying = false;
    VideoProgress = 0;
    VideoCurrentTime = 0;
    StatusMessage = "播放已停止";
});

/// <summary>
/// 跳转到指定进度命令
/// </summary>
public RelayCommand<double> SeekVideoCommand => new RelayCommand<double>(progress =>
{
    VideoProgress = progress;
    // 这里通过事件通知 MediaElement 跳转
    VideoSeekRequested?.Invoke(progress);
});

/// <summary>
/// 视频跳转请求事件
/// </summary>
public event Action<double>? VideoSeekRequested;

#endregion

#region 图片缩放命令

/// <summary>
/// 放大图片命令
/// </summary>
public RelayCommand ZoomInCommand => new RelayCommand(() =>
{
    if (SelectedImage != null)
    {
        ImageZoom = Math.Min(ImageZoom * 1.25, 5.0); // 最大 500%
        StatusMessage = $"缩放: {ZoomPercentage:F0}%";
    }
});

/// <summary>
/// 缩小图片命令
/// </summary>
public RelayCommand ZoomOutCommand => new RelayCommand(() =>
{
    if (SelectedImage != null)
    {
        ImageZoom = Math.Max(ImageZoom / 1.25, 0.1); // 最小 10%
        StatusMessage = $"缩放: {ZoomPercentage:F0}%";
    }
});

/// <summary>
/// 重置图片缩放命令
/// </summary>
public RelayCommand ZoomResetCommand => new RelayCommand(() =>
{
    if (SelectedImage != null)
    {
        ImageZoom = 1.0;
        StatusMessage = "缩放已重置";
    }
});

/// <summary>
/// 文件选中状态切换命令
/// </summary>
public RelayCommand<object> ToggleFileCheckCommand => new RelayCommand<object>(file =>
{
    if (file is VideoFile video)
    {
        video.IsChecked = !video.IsChecked;
        OnPropertyChanged(nameof(CheckedFilesCount));
    }
    else if (file is ImageAsset image)
    {
        image.IsChecked = !image.IsChecked;
        OnPropertyChanged(nameof(CheckedFilesCount));
    }
});

#endregion

#region 解析界面专用属性

/// <summary>
/// 元数据文本（粘贴解析用）
/// </summary>
public string MetadataText { get; set; } = "";

/// <summary>
/// 最后解析的警告信息
/// </summary>
public string? LastParseWarnings { get; set; }

/// <summary>
/// 当前选中的剧集索引
/// </summary>
public int CurrentEpisodeIndex
{
    get => _currentEpisodeIndex;
    set
    {
        if (_currentEpisodeIndex != value)
        {
            _currentEpisodeIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentEpisode));
            
            // 当选中剧集改变时，如果当前选中了视频，自动映射
            if (value >= 0 && SelectedVideo != null)
            {
                MapVideoToEpisode(SelectedVideo, value + 1); // 集号从1开始
            }
        }
    }
}
private int _currentEpisodeIndex;

/// <summary>
/// 当前选中的剧集
/// </summary>
public Episode? CurrentEpisode
{
    get
    {
        if (Show?.Episodes != null && Show.Episodes.Count > CurrentEpisodeIndex && CurrentEpisodeIndex >= 0)
        {
            return Show.Episodes[CurrentEpisodeIndex];
        }
        return null;
    }
}

/// <summary>
/// 所有文件（视频 + 图片）
/// </summary>
public List<object> AllFiles
{
    get
    {
        var allFiles = new List<object>();
        allFiles.AddRange(Videos);
        allFiles.AddRange(Images);
        return allFiles;
    }
}

/// <summary>
/// 剧集列表（用于绑定）
/// </summary>
public List<Episode> Episodes => Show?.Episodes ?? new List<Episode>();

#endregion

    #region 方法

    /// <summary>
    /// 加载文件
    /// </summary>
    public async Task LoadFilesAsync()
    {
        if (IsProcessing) return;

        IsProcessing = true;
        StatusMessage = "正在加载文件...";
        ErrorMessage = null;

        try
        {
            await Task.Run(() =>
            {
                // 扫描视频
                var videoResult = _mediaValidator.ScanDirectory(Directory.GetCurrentDirectory());
                if (!videoResult.Success)
                {
                    ErrorMessage = string.Join("; ", videoResult.Errors);
                }
                Videos = videoResult.Videos;

                // 扫描图片
                var imageResult = _imageManager.ScanDirectory(Directory.GetCurrentDirectory());
                Images = imageResult.Images;

                // 自动匹配
                var mappedCount = _mediaValidator.AutoMapVideos();
                Videos = _mediaValidator.Videos;

                StatusMessage = $"加载完成！视频: {Videos.Count}, 图片: {Images.Count}";
            });
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// 添加视频文件
    /// </summary>
    public async Task AddVideoAsync()
    {
        // 这里可以添加文件选择对话框逻辑
        StatusMessage = "添加视频功能";
    }

    /// <summary>
    /// 添加图片文件
    /// </summary>
    public async Task AddImageAsync()
    {
        // 这里可以添加文件选择对话框逻辑
        StatusMessage = "添加图片功能";
    }

    /// <summary>
    /// 解析元数据
    /// </summary>
    public async Task ParseMetadataAsync(string? text = null)
    {
        if (IsProcessing) return;

        IsProcessing = true;
        StatusMessage = "正在解析元数据...";
        ErrorMessage = null;

        try
        {
            await Task.Run(() =>
            {
                // 如果没有提供文本，使用 Show.RawMetadata
                var metadataText = text ?? Show?.RawMetadata ?? "";
                
                if (string.IsNullOrWhiteSpace(metadataText))
                {
                    ErrorMessage = "请输入元数据文本";
                    return;
                }

                var result = _metadataParser.Parse(metadataText);

                if (!result.Success)
                {
                    ErrorMessage = string.Join("; ", result.Errors);
                    return;
                }

                // 更新 Show 和 Episodes
                if (result.Show != null)
                {
                    Show = result.Show;
                    Show.RawMetadata = metadataText;
                }
                else
                {
                    // 创建新的 Show
                    Show = new Show { RawMetadata = metadataText };
                }

                // 将解析的集数添加到 Show
                if (Show != null)
                {
                    foreach (var episode in result.Episodes)
                    {
                        if (!Show.Episodes.Any(e => e.Season == episode.Season && e.Number == episode.Number))
                        {
                            Show.Episodes.Add(episode);
                        }
                    }
                }

                StatusMessage = $"解析成功！共 {result.Episodes.Count} 集";
            });
        }
        finally
        {
            IsProcessing = false;
        }
    }

/// <summary>
/// 选择并扫描目录
/// </summary>
public async Task SelectDirectoryAsync()
{
    if (IsProcessing) return;

    var dialog = new VistaFolderBrowserDialog
    {
        Description = "选择媒体文件目录"
    };

    if (dialog.ShowDialog() != true)
    {
        StatusMessage = "已取消选择";
        return;
    }

    CurrentFolderPath = dialog.SelectedPath;
    IsProcessing = true;
    StatusMessage = "正在扫描目录...";
    ErrorMessage = null;

    try
    {
        await Task.Run(() =>
        {
            // 扫描视频
            var videoResult = _mediaValidator.ScanDirectory(CurrentFolderPath);
            if (!videoResult.Success)
            {
                ErrorMessage = string.Join("; ", videoResult.Errors);
            }
            
            // 重置 UI 状态
            foreach (var video in videoResult.Videos)
            {
                video.IsSelected = false;
                video.IsChecked = false;
            }
            Videos = videoResult.Videos;

            // 扫描图片
            var imageResult = _imageManager.ScanDirectory(CurrentFolderPath);
            
            // 重置 UI 状态
            foreach (var image in imageResult.Images)
            {
                image.IsSelected = false;
                image.IsChecked = false;
            }
            Images = imageResult.Images;

            // 自动匹配
            var mappedCount = _mediaValidator.AutoMapVideos();
            Videos = _mediaValidator.Videos;

            StatusMessage = $"扫描完成！视频: {Videos.Count}, 图片: {Images.Count}, 自动映射: {mappedCount}";
            OnPropertyChanged(nameof(CheckedFilesCount));
        });
    }
    finally
    {
        IsProcessing = false;
    }
}

/// <summary>
/// 获取已勾选的文件列表
/// </summary>
public List<object> GetCheckedFiles()
{
    var checkedFiles = new List<object>();
    
    foreach (var video in Videos)
    {
        if (video.IsChecked)
        {
            checkedFiles.Add(video);
        }
    }
    
    foreach (var image in Images)
    {
        if (image.IsChecked)
        {
            checkedFiles.Add(image);
        }
    }
    
    return checkedFiles;
}

/// <summary>
/// 刷新选中文件计数
/// </summary>
public void RefreshCheckedCount()
{
    OnPropertyChanged(nameof(CheckedFilesCount));
}

/// <summary>
/// 开始处理（归档）
/// </summary>
public async Task StartProcessingAsync()
{
    if (IsProcessing) return;
    
    IsProcessing = true;
    StatusMessage = "正在开始处理...";
    ErrorMessage = null;

    try
    {
        await Task.Run(() =>
        {
            // 执行归档
            if (Show != null)
            {
                var result = _archiveProcessor.Archive(Show, Videos, Images);
                
                if (!result.Success)
                {
                    ErrorMessage = string.Join("; ", result.Errors);
                    StatusMessage = "处理失败";
                }
                else
                {
                    StatusMessage = $"处理成功！输出目录: {result.OutputDirectory}";
                }
            }
            else
            {
                ErrorMessage = "没有可用的元数据";
                StatusMessage = "处理失败";
            }
        });
    }
    finally
    {
        IsProcessing = false;
    }
}

/// <summary>
/// 人脸映射
/// </summary>
public async Task FaceMappingAsync()
{
    if (IsProcessing) return;
    
    IsProcessing = true;
    StatusMessage = "正在进行人脸映射...";
    ErrorMessage = null;

    try
    {
        await Task.Run(() =>
        {
            // TODO: 实现人脸映射逻辑
            StatusMessage = "人脸映射完成（模拟）";
        });
    }
    finally
    {
        IsProcessing = false;
    }
}

/// <summary>
/// 解析元数据（从文本）
/// </summary>
public async Task ParseMetadataFromTextAsync()
{
    if (IsProcessing) return;
    
    IsProcessing = true;
    StatusMessage = "正在解析元数据...";
    ErrorMessage = null;
    LastParseWarnings = null;

    try
    {
        // 在后台线程执行解析
        var result = await Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(MetadataText))
            {
                return (Success: false, Error: "请输入元数据文本", Warnings: new List<string>(), Episodes: new List<Episode>(), Show: (Show?)null, StatusMessage: "解析失败");
            }

            var parseResult = _metadataParser.Parse(MetadataText);

            if (!parseResult.Success)
            {
                return (Success: false, Error: string.Join("; ", parseResult.Errors), Warnings: parseResult.Warnings, Episodes: new List<Episode>(), Show: (Show?)null, StatusMessage: "解析失败");
            }

            // 更新 Show 和 Episodes
            Show? show;
            if (parseResult.Show != null)
            {
                show = parseResult.Show;
                show.RawMetadata = MetadataText;
            }
            else
            {
                show = new Show { RawMetadata = MetadataText };
            }

            // 将解析的集数添加到 Show
            foreach (var episode in parseResult.Episodes)
            {
                if (!show.Episodes.Any(e => e.Season == episode.Season && e.Number == episode.Number))
                {
                    show.Episodes.Add(episode);
                }
            }

            var episodeCount = parseResult.Episodes.Count;
            string statusMessage;
            List<string> warnings = new List<string>();
            
            if (parseResult.Warnings.Any())
            {
                statusMessage = $"✓ 解析成功！{episodeCount} 集";
                warnings = parseResult.Warnings;
            }
            else
            {
                statusMessage = $"✓ 解析成功！共 {episodeCount} 集";
            }

            if (episodeCount == 0)
            {
                statusMessage += " (未识别到集数信息)";
            }

            return (Success: true, Error: (string?)null, Warnings: warnings, Episodes: show.Episodes.ToList(), Show: show, StatusMessage: statusMessage);
        });

        // 在 UI 线程更新属性
        if (result.Success)
        {
            Show = result.Show!;
            StatusMessage = result.StatusMessage;
            LastParseWarnings = result.Warnings.Any() ? string.Join("\n", result.Warnings) : null;
        }
        else
        {
            ErrorMessage = result.Error;
            StatusMessage = "解析失败";
        }

        // 触发属性变更通知以更新 UI
        OnPropertyChanged(nameof(Episodes));
        OnPropertyChanged(nameof(CurrentEpisode));
    }
    finally
    {
        IsProcessing = false;
    }
}


    /// <summary>
    /// 映射视频到集数
    /// </summary>
    public void MapVideoToEpisode(VideoFile video, int episodeNumber)
    {
        if (video == null) return;

        video.MappedEpisodeNumber = episodeNumber;
        
        // 首先清除该视频之前映射到的所有剧集
        foreach (var ep in Show?.Episodes ?? Enumerable.Empty<Episode>())
        {
            if (ep.MappedVideo?.FullPath == video.FullPath)
            {
                ep.MappedVideo = null;
            }
        }
        
        // 找到对应的集并设置映射
        var episode = Show?.Episodes.FirstOrDefault(e => e.Number == episodeNumber);
        if (episode != null)
        {
            episode.MappedVideo = video;
        }

        OnPropertyChanged(nameof(Videos));
        RefreshValidation();
    }

    /// <summary>
    /// 设置图片角色
    /// </summary>
    public void SetImageRole(ImageAsset image, ImageRole role)
    {
        if (image == null) return;

        // 直接修改图片角色
        image.Role = role;
        
        // 创建一个新的列表引用来触发 UI 刷新
        Images = new List<ImageAsset>(Images);
        
        // 刷新验证状态
        RefreshValidation();
    }

    /// <summary>
    /// 执行归档
    /// </summary>
    public async Task ArchiveAsync()
    {
        if (IsProcessing) return;

        // 确认是否执行归档
        var confirmResult = Application.Current.Dispatcher.Invoke(() =>
        {
            return MessageBox.Show(
                "确定要开始归档操作吗？\n\n归档将处理当前加载的媒体文件。",
                "确认归档",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
        });

        if (confirmResult != MessageBoxResult.Yes)
        {
            StatusMessage = "已取消归档";
            return;
        }

        // 确保在 UI 线程上执行
        if (Application.Current.Dispatcher == null || 
            Application.Current.Dispatcher.Thread != Thread.CurrentThread)
        {
            // 如果不在 UI 线程，使用 Send 在 UI 线程上执行
            string? selectedPath = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new VistaFolderBrowserDialog
                {
                    Description = "选择归档输出目录"
                };

                if (dialog.ShowDialog() == true)
                {
                    selectedPath = dialog.SelectedPath;
                }
            });

            if (string.IsNullOrEmpty(selectedPath))
            {
                StatusMessage = "已取消归档";
                return;
            }

            // 在后台线程执行归档
            StatusMessage = "正在归档...";
            ErrorMessage = null;
            await Task.Run(() => ArchiveToDirectory(selectedPath!));
            return;
        }

        // UI 线程上执行
        var vistaDialog = new VistaFolderBrowserDialog
        {
            Description = "选择归档输出目录"
        };

        if (vistaDialog.ShowDialog() != true)
        {
            StatusMessage = "已取消归档";
            return;
        }

        string outputDirectory = vistaDialog.SelectedPath;
        if (string.IsNullOrEmpty(outputDirectory))
        {
            StatusMessage = "无效的输出目录";
            return;
        }

        // 在后台线程执行归档
        StatusMessage = "正在归档...";
        ErrorMessage = null;
        await Task.Run(() => ArchiveToDirectory(outputDirectory));
    }

    /// <summary>
    /// 获取无法归档的原因
    /// </summary>
    private string GetArchiveDisabledReason()
    {
        // 首先检查工作流状态
        if (CurrentState != WorkflowState.Ready)
        {
            if (CurrentState == WorkflowState.Archived)
                return "媒体已归档，不能再次归档";
            if (CurrentState == WorkflowState.Draft)
                return LastErrorMessage ?? "工作流处于草稿状态，请检查错误信息";
            return $"工作流状态为 {CurrentState}，不允许归档";
        }
        
        if (Show == null)
            return "未解析剧集元数据";
        
        if (Show.Episodes == null || Show.Episodes.Count == 0)
            return "未找到剧集信息";
        
        // 检查是否有视频未映射
        var unmappedVideos = Videos.Where(v => 
            Show.Episodes.All(e => e.MappedVideo?.FullPath != v.FullPath)).ToList();
        if (unmappedVideos.Count > 0)
            return $"有 {unmappedVideos.Count} 个视频未映射到剧集";
        
        // 检查是否有图片未分配角色
        var unassignedImages = Images.Where(i => i.Role == ImageRole.Unknown).ToList();
        if (unassignedImages.Count > 0)
            return $"有 {unassignedImages.Count} 张图片未分配角色";
        
        return "所有检查已通过，但工作流状态不允许归档";
    }

    /// <summary>
    /// 执行归档到指定目录
    /// </summary>
    private void ArchiveToDirectory(string outputDirectory)
    {
        // 注意：IsProcessing 已经在 ArchiveAsync 中设置为 true，这里不需要重复设置

        try
        {
            var result = _archiveProcessor.Archive(Show!, Videos, Images, outputDirectory);

            // 在 UI 线程显示通知
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (!result.Success)
                    {
                        ErrorMessage = string.Join("; ", result.Errors);
                        StatusMessage = "归档失败";
                        MessageBox.Show(
                            $"归档过程中出现错误:\n{ErrorMessage}", 
                            "归档失败", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
                    }
                    else
                    {
                        StatusMessage = $"归档成功！输出目录: {result.OutputDirectory}";
                        MessageBox.Show(
                            $"媒体文件已成功归档到:\n{result.OutputDirectory}", 
                            "归档完成", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);
                        // 归档成功后，工作流状态应该已经是 Archived
                        OnPropertyChanged(nameof(CanArchive));
                        OnPropertyChanged(nameof(IsArchived));
                    }
                }
                finally
                {
                    // 确保 IsProcessing 在 UI 线程上设置为 false
                    IsProcessing = false;
                }
            });
        }
        catch (Exception ex)
        {
            // 捕获异常并在 UI 线程上显示
            Application.Current.Dispatcher.Invoke(() =>
            {
                ErrorMessage = $"归档过程中出错: {ex.Message}";
                StatusMessage = "归档失败";
                IsProcessing = false;
                MessageBox.Show(
                    $"归档过程中出现错误:\n{ex.Message}", 
                    "归档失败", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            });
        }
    }

    /// <summary>
    /// 重置工作流
    /// </summary>
    public void Reset()
    {
        _workflowController.Reset();
        Show = null;
        Videos = new List<VideoFile>();
        Images = new List<ImageAsset>();
        ErrorMessage = null;
        StatusMessage = "已重置";
        
        OnPropertyChanged(nameof(HasMetadata));
        OnPropertyChanged(nameof(CanArchive));
        OnPropertyChanged(nameof(IsArchived));
    }

    /// <summary>
    /// 刷新校验状态
    /// </summary>
    public void RefreshValidation()
    {
        _workflowController.TryTransitionToReady();
        
        OnPropertyChanged(nameof(CurrentState));
        OnPropertyChanged(nameof(CanArchive));
        OnPropertyChanged(nameof(IsArchived));
        OnPropertyChanged(nameof(ValidationSummary));
        OnPropertyChanged(nameof(LastErrorMessage));
    }

    #endregion

    #region 事件处理

    private void OnWorkflowStateChanged(WorkflowState newState)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            OnPropertyChanged(nameof(CurrentState));
            OnPropertyChanged(nameof(CanArchive));
            OnPropertyChanged(nameof(IsArchived));
            StatusMessage = newState switch
            {
                WorkflowState.Draft => "准备中 - 请完善数据",
                WorkflowState.Ready => "就绪 - 可以归档",
                WorkflowState.Archived => "已归档完成",
                _ => "未知状态"
            };
        });
    }

    private void OnMetadataChanged()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            OnPropertyChanged(nameof(Show));
            OnPropertyChanged(nameof(HasMetadata));
        });
    }

    private void OnVideosChanged()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            OnPropertyChanged(nameof(Videos));
        });
    }

    private void OnImagesChanged()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            OnPropertyChanged(nameof(Images));
        });
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}

/// <summary>
/// 映射集数命令参数
/// </summary>
public class MapEpisodeCommandPayload
{
    public VideoFile Video { get; set; } = null!;
    public int EpisodeNumber { get; set; }
}

/// <summary>
/// 设置图片角色命令参数
/// </summary>
public class SetImageRoleCommandPayload
{
    public ImageAsset Image { get; set; } = null!;
    public ImageRole Role { get; set; }
}

/// <summary>
/// 剧集显示帮助类（用于 UI 绑定）
/// </summary>
public class EpisodeDisplayItem
{
    public Episode Episode { get; set; } = null!;
    public string EpisodeDisplay => $"Episode {Episode.Number}";
    public string DisplayTitle => string.IsNullOrEmpty(Episode.Title) 
        ? $"Episode {Episode.Number}" 
        : $"Episode {Episode.Number} - {Episode.Title}";
}

/// <summary>
/// 文件包装器（用于 UI 状态管理）
/// </summary>
public class FileWrapper<T>
{
    public T File { get; set; } = default!;
    public bool IsSelected { get; set; }
    public bool IsChecked { get; set; }
}
