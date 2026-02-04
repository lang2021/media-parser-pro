using System.ComponentModel;
using System.Runtime.CompilerServices;
using MediaParser.Core.Models;

namespace MediaParser.Core.Modules.MediaValidator;

/// <summary>
/// 视频验证结果
/// </summary>
public class ValidationResult
{
    public bool Success { get; set; }
    public List<VideoFile> Videos { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// 视频验证器选项
/// </summary>
public class MediaValidatorOptions
{
    /// <summary>
    /// 支持的视频扩展名
    /// </summary>
    public string[] SupportedExtensions { get; set; } = { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v" };

    /// <summary>
    /// 是否递归扫描子目录
    /// </summary>
    public bool Recursive { get; set; } = false;

    /// <summary>
    /// 是否自动推断集数
    /// </summary>
    public bool AutoInferEpisode { get; set; } = true;

    /// <summary>
    /// 文件大小下限（字节）
    /// </summary>
    public long MinFileSize { get; set; } = 1024 * 1024; // 1MB
}

/// <summary>
/// 视频验证器
/// 
/// 职责：
/// 1. 扫描目录中的视频文件
/// 2. 预览视频信息
/// 3. 建立 Episode 与 VideoFile 的映射关系
/// </summary>
public class MediaValidator : INotifyPropertyChanged
{
    private readonly MediaValidatorOptions _options;
    private List<VideoFile> _videos = new();
    private string? _lastScannedDirectory;
    private bool _isScanning;

    /// <summary>
    /// 已扫描的视频文件列表
    /// </summary>
    public List<VideoFile> Videos
    {
        get => _videos;
        private set
        {
            if (_videos != value)
            {
                _videos = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 最后扫描的目录
    /// </summary>
    public string? LastScannedDirectory
    {
        get => _lastScannedDirectory;
        private set
        {
            if (_lastScannedDirectory != value)
            {
                _lastScannedDirectory = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否正在扫描
    /// </summary>
    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (_isScanning != value)
            {
                _isScanning = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 扫描进度（0-100）
    /// </summary>
    public int ScanProgress { get; private set; }

    /// <summary>
    /// 扫描进度变化事件
    /// </summary>
    public event Action<int, string>? ScanProgressChanged;

    /// <summary>
    /// 扫描完成事件
    /// </summary>
    public event Action<ValidationResult>? ScanCompleted;

    /// <summary>
    /// 属性变化事件
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    public MediaValidator(MediaValidatorOptions? options = null)
    {
        _options = options ?? new MediaValidatorOptions();
    }

    /// <summary>
    /// 扫描目录中的视频文件
    /// </summary>
    public ValidationResult ScanDirectory(string directoryPath)
    {
        var result = new ValidationResult { Success = false };

        if (!Directory.Exists(directoryPath))
        {
            result.Errors.Add($"目录不存在: {directoryPath}");
            return result;
        }

        IsScanning = true;
        ScanProgress = 0;
        LastScannedDirectory = directoryPath;

        try
        {
            var searchOption = _options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var allFiles = Directory.GetFiles(directoryPath, "*.*", searchOption)
                .Where(f => IsVideoFile(f))
                .ToList();

            var videos = new List<VideoFile>();
            var totalFiles = allFiles.Count;
            var processedFiles = 0;

            foreach (var filePath in allFiles)
            {
                try
                {
                    var video = CreateVideoFile(filePath);
                    if (video != null)
                    {
                        // 自动推断集数
                        if (_options.AutoInferEpisode)
                        {
                            if (video.TryInferEpisodeNumber(out var episodeNumber))
                            {
                                video.MappedEpisodeNumber = episodeNumber;
                            }
                        }
                        videos.Add(video);
                    }

                    processedFiles++;
                    ScanProgress = (int)((double)processedFiles / totalFiles * 100);
                    ScanProgressChanged?.Invoke(ScanProgress, Path.GetFileName(filePath));
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"无法处理文件 {filePath}: {ex.Message}");
                }
            }

            Videos = videos;
            result.Success = true;
            result.Videos = videos;

            if (videos.Count == 0)
            {
                result.Warnings.Add("未找到任何视频文件");
            }

            ScanCompleted?.Invoke(result);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"扫描目录时出错: {ex.Message}");
        }
        finally
        {
            IsScanning = false;
        }

        return result;
    }

    /// <summary>
    /// 扫描单个文件
    /// </summary>
    public VideoFile? ScanFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        if (!IsVideoFile(filePath))
        {
            return null;
        }

        var video = CreateVideoFile(filePath);
        return video;
    }

    /// <summary>
    /// 检查文件是否为视频文件
    /// </summary>
    public bool IsVideoFile(string filePath)
    {
        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
        return _options.SupportedExtensions.Contains(extension);
    }

    /// <summary>
    /// 创建 VideoFile 对象
    /// </summary>
    private VideoFile? CreateVideoFile(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);

            // 过滤太小的文件
            if (fileInfo.Length < _options.MinFileSize)
            {
                return null;
            }

            var video = new VideoFile
            {
                FullPath = filePath,
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                Extension = fileInfo.Extension.ToLowerInvariant()
            };

            return video;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 将视频映射到集数
    /// </summary>
    public bool MapVideoToEpisode(VideoFile video, int episodeNumber)
    {
        if (!Videos.Contains(video))
        {
            return false;
        }

        video.MappedEpisodeNumber = episodeNumber;
        OnPropertyChanged(nameof(Videos));
        return true;
    }

    /// <summary>
    /// 取消视频的集数映射
    /// </summary>
    public bool UnmapVideo(VideoFile video)
    {
        if (!Videos.Contains(video))
        {
            return false;
        }

        video.MappedEpisodeNumber = null;
        OnPropertyChanged(nameof(Videos));
        return true;
    }

    /// <summary>
    /// 自动匹配视频到集数（基于文件名推断）
    /// </summary>
    public int AutoMapVideos()
    {
        var mappedCount = 0;

        foreach (var video in Videos)
        {
            if (video.IsMapped)
            {
                continue;
            }

            if (video.TryInferEpisodeNumber(out var episodeNumber))
            {
                video.MappedEpisodeNumber = episodeNumber;
                mappedCount++;
            }
        }

        if (mappedCount > 0)
        {
            OnPropertyChanged(nameof(Videos));
        }

        return mappedCount;
    }

    /// <summary>
    /// 清除所有映射
    /// </summary>
    public void ClearMappings()
    {
        foreach (var video in Videos)
        {
            video.MappedEpisodeNumber = null;
        }
        OnPropertyChanged(nameof(Videos));
    }

    /// <summary>
    /// 获取未映射的视频列表
    /// </summary>
    public List<VideoFile> GetUnmappedVideos()
    {
        return Videos.Where(v => !v.IsMapped).ToList();
    }

    /// <summary>
    /// 获取已映射到指定集数的视频
    /// </summary>
    public VideoFile? GetVideoForEpisode(int episodeNumber)
    {
        return Videos.FirstOrDefault(v => v.MappedEpisodeNumber == episodeNumber);
    }

    /// <summary>
    /// 获取映射统计信息
    /// </summary>
    public (int total, int mapped, int unmapped) GetMappingStats()
    {
        var total = Videos.Count;
        var mapped = Videos.Count(v => v.IsMapped);
        var unmapped = total - mapped;
        return (total, mapped, unmapped);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
