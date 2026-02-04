using System.ComponentModel;
using System.Runtime.CompilerServices;
using MediaParser.Core.Models;

namespace MediaParser.Core.Workflow;

/// <summary>
/// 工作流控制器 - 核心状态机决策者
/// 
/// 职责：
/// 1. 维护工作流状态（Draft/Ready/Archived）
/// 2. 提供数据变化通知
/// 3. 执行校验逻辑（唯一决策者）
/// 4. 管理状态转换
/// </summary>
public class WorkflowController : INotifyPropertyChanged
{
    private WorkflowState _currentState = WorkflowState.Draft;
    private Show? _show;
    private List<VideoFile> _videos = new();
    private List<ImageAsset> _images = new();
    private string _lastErrorMessage = string.Empty;

    /// <summary>
    /// 当前工作流状态
    /// </summary>
    public WorkflowState CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState != value)
            {
                _currentState = value;
                OnPropertyChanged();
                OnStateChanged(value);
            }
        }
    }

    /// <summary>
    /// 剧集元数据
    /// </summary>
    public Show? Show
    {
        get => _show;
        set
        {
            if (_show != value)
            {
                _show = value;
                OnPropertyChanged();
                OnMetadataChanged();
            }
        }
    }

    /// <summary>
    /// 视频文件列表
    /// </summary>
    public List<VideoFile> Videos
    {
        get => _videos;
        set
        {
            if (_videos != value)
            {
                _videos = value;
                OnPropertyChanged();
                OnVideosChanged();
            }
        }
    }

    /// <summary>
    /// 图片资源列表
    /// </summary>
    public List<ImageAsset> Images
    {
        get => _images;
        set
        {
            if (_images != value)
            {
                _images = value;
                OnPropertyChanged();
                OnImagesChanged();
            }
        }
    }

    /// <summary>
    /// 当前选中的剧集
    /// </summary>
    public Episode? SelectedEpisode { get; set; }

    /// <summary>
    /// 最后的错误消息
    /// </summary>
    public string LastErrorMessage
    {
        get => _lastErrorMessage;
        private set
        {
            if (_lastErrorMessage != value)
            {
                _lastErrorMessage = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否可以执行归档操作
    /// </summary>
    public bool CanArchive => CurrentState == WorkflowState.Ready;

    /// <summary>
    /// 是否已归档
    /// </summary>
    public bool IsArchived => CurrentState == WorkflowState.Archived;

    /// <summary>
    /// 校验结果摘要
    /// </summary>
    public ValidationSummary? ValidationSummary { get; private set; }

    /// <summary>
    /// 元数据变化事件
    /// </summary>
    public event Action? MetadataChanged;

    /// <summary>
    /// 视频列表变化事件
    /// </summary>
    public event Action? VideosChanged;

    /// <summary>
    /// 图片列表变化事件
    /// </summary>
    public event Action? ImagesChanged;

    /// <summary>
    /// 状态变化事件
    /// </summary>
    public event Action<WorkflowState>? StateChanged;

    /// <summary>
    /// 属性变化事件
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    #region 状态转换方法

    /// <summary>
    /// 元数据变化时调用 - 状态回退到 Draft
    /// </summary>
    public void OnMetadataChanged()
    {
        TransitionTo(WorkflowState.Draft);
        MetadataChanged?.Invoke();
    }

    /// <summary>
    /// 视频列表变化时调用 - 状态回退到 Draft
    /// </summary>
    public void OnVideosChanged()
    {
        TransitionTo(WorkflowState.Draft);
        VideosChanged?.Invoke();
    }

    /// <summary>
    /// 图片列表变化时调用 - 状态回退到 Draft
    /// </summary>
    public void OnImagesChanged()
    {
        TransitionTo(WorkflowState.Draft);
        ImagesChanged?.Invoke();
    }

    /// <summary>
    /// 尝试转换到 Ready 状态 - 执行完整校验
    /// </summary>
    /// <returns>校验是否通过</returns>
    public bool TryTransitionToReady()
    {
        var summary = RunFullValidation();
        ValidationSummary = summary;

        if (summary.IsFullyValid)
        {
            CurrentState = WorkflowState.Ready;
            LastErrorMessage = string.Empty;
            return true;
        }
        else
        {
            CurrentState = WorkflowState.Draft;
            LastErrorMessage = summary.GetFailureSummary();
        return false;
        }
    }

    /// <summary>
    /// 执行归档操作
    /// </summary>
    /// <returns>归档是否成功</returns>
    public bool TryArchive()
    {
        if (CurrentState != WorkflowState.Ready)
            {
            LastErrorMessage = "当前状态不允许归档操作";
            return false;
        }

        // 再次验证确保数据完整性
        var summary = RunFullValidation();
        if (!summary.IsFullyValid)
        {
            LastErrorMessage = "归档前校验失败: " + summary.GetFailureSummary();
            return false;
        }

        // 归档成功
            CurrentState = WorkflowState.Archived;
        LastErrorMessage = string.Empty;
        return true;
    }

    /// <summary>
    /// 重置到初始状态
    /// </summary>
    public void Reset()
    {
        _show = null;
        _videos = new();
        _images = new();
        ValidationSummary = null;
        LastErrorMessage = string.Empty;
        TransitionTo(WorkflowState.Draft);
    }

    #endregion

    #region 校验逻辑（核心决策）

    /// <summary>
    /// 运行完整校验（三个维度）
    /// </summary>
    public ValidationSummary RunFullValidation()
    {
        var summary = new ValidationSummary();

        // 1. 元数据有效性校验
        summary.MetadataResult = ValidateMetadata();

        // 2. 视频映射完整性校验
        summary.VideoMappingResult = ValidateVideoMapping();

        // 3. 图片角色标记校验
        summary.ImageAssetsResult = ValidateImageAssets();

        return summary;
    }

    /// <summary>
    /// 元数据有效性校验
    /// 校验项：Title 非空、Season > 0、Episodes 列表非空
    /// </summary>
    public ValidationResult ValidateMetadata()
    {
        if (Show == null)
        {
            return ValidationResult.Fail(ValidationCategory.Metadata, "元数据为空");
        }

        if (string.IsNullOrWhiteSpace(Show.Title))
        {
            return ValidationResult.Fail(ValidationCategory.Metadata, "标题不能为空");
        }

        if (Show.Season <= 0)
        {
            return ValidationResult.Fail(ValidationCategory.Metadata, "季数必须大于 0");
        }

        if (Show.Episodes == null || Show.Episodes.Count == 0)
        {
            return ValidationResult.Fail(ValidationCategory.Metadata, "剧集列表不能为空");
    }

        // 验证每集的有效性
        foreach (var episode in Show.Episodes)
    {
            if (episode.Number <= 0)
        {
                return ValidationResult.Fail(ValidationCategory.Metadata, $"集数 {episode.Number} 无效，必须大于 0");
            }
        }

        return ValidationResult.Success(ValidationCategory.Metadata);
    }

    /// <summary>
    /// 视频映射完整性校验
    /// 校验项：已选择的每个视频都必须映射到一个剧集
    /// 注意：不要求每个剧集都有视频
    /// </summary>
    public ValidationResult ValidateVideoMapping()
    {
        if (Videos == null || Videos.Count == 0)
        {
            return ValidationResult.Success(ValidationCategory.VideoMapping);
        }

        var unmappedVideos = new List<string>();

        foreach (var video in Videos)
        {
            // 检查视频是否已映射到某个剧集
            bool isMapped = Show?.Episodes.Any(e => e.MappedVideo?.FullPath == video.FullPath) ?? false;
            if (!isMapped)
            {
                unmappedVideos.Add(Path.GetFileName(video.FullPath));
            }
        }

        if (unmappedVideos.Count > 0)
        {
            var count = unmappedVideos.Count;
            var preview = count <= 3 
                ? string.Join(", ", unmappedVideos) 
                : string.Join(", ", unmappedVideos.Take(3)) + $"... (共{count}个)";
            return ValidationResult.Fail(ValidationCategory.VideoMapping, $"以下视频未分配剧集: {preview}");
        }

        return ValidationResult.Success(ValidationCategory.VideoMapping);
    }

    /// <summary>
    /// 图片角色标记校验
    /// 校验项：已选择的每张图片都必须分配一个角色（Poster 或 Fanart）
    /// </summary>
    public ValidationResult ValidateImageAssets()
    {
        if (Images == null || Images.Count == 0)
        {
            return ValidationResult.Success(ValidationCategory.ImageAssets);
        }

        var unassignedImages = new List<string>();

        foreach (var image in Images)
        {
            if (image.Role == ImageRole.Unknown)
            {
                unassignedImages.Add(image.FileName);
            }
        }

        if (unassignedImages.Count > 0)
        {
            var count = unassignedImages.Count;
            var preview = count <= 3 
                ? string.Join(", ", unassignedImages) 
                : string.Join(", ", unassignedImages.Take(3)) + $"... (共{count}张)";
            return ValidationResult.Fail(ValidationCategory.ImageAssets, $"以下图片未分配角色: {preview}");
        }

        return ValidationResult.Success(ValidationCategory.ImageAssets);
    }

    #endregion

    #region 私有方法

    private void TransitionTo(WorkflowState newState)
    {
        if (CurrentState == WorkflowState.Archived)
        {
            // 已归档后只能重置，不能转换到其他状态
            return;
        }

        CurrentState = newState;
    }

    private void OnStateChanged(WorkflowState newState)
        {
        StateChanged?.Invoke(newState);
        }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    #endregion
    }

    /// <summary>
/// 校验结果摘要
    /// </summary>
public class ValidationSummary
{
    public ValidationResult MetadataResult { get; set; } = new() { IsValid = true };
    public ValidationResult VideoMappingResult { get; set; } = new() { IsValid = true };
    public ValidationResult ImageAssetsResult { get; set; } = new() { IsValid = true };

    /// <summary>
    /// 是否全部校验通过
    /// </summary>
    public bool IsFullyValid => MetadataResult.IsValid && VideoMappingResult.IsValid;

    /// <summary>
    /// 是否有关键项未通过（影响状态转换）
    /// </summary>
    public bool HasCriticalFailures => !MetadataResult.IsValid || !VideoMappingResult.IsValid;

    /// <summary>
    /// 获取失败摘要
    /// </summary>
    public string GetFailureSummary()
    {
        var failures = new List<string>();

        if (!MetadataResult.IsValid)
            failures.Add($"元数据: {MetadataResult.Message}");
        
        if (!VideoMappingResult.IsValid)
            failures.Add($"视频映射: {VideoMappingResult.Message}");

        if (!ImageAssetsResult.IsValid)
            failures.Add($"图片: {ImageAssetsResult.Message}");

        return string.Join("; ", failures);
    }

    /// <summary>
    /// 获取详细状态描述
    /// </summary>
    public string GetDetailedStatus()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"元数据: {(MetadataResult.IsValid ? "✓" : "✗")} {MetadataResult.Message}");
        sb.AppendLine($"视频映射: {(VideoMappingResult.IsValid ? "✓" : "✗")} {VideoMappingResult.Message}");
        sb.AppendLine($"图片: {(ImageAssetsResult.IsValid ? "✓" : "⚠")} {ImageAssetsResult.Message}");
        return sb.ToString().TrimEnd();
    }
}
