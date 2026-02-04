namespace MediaParser.Core.Workflow;

/// <summary>
/// 工作流状态枚举
/// </summary>
public enum WorkflowState
{
    /// <summary>
    /// 准备中 - 初始状态或数据校验不通过
    /// </summary>
    Draft,

    /// <summary>
    /// 可归档 - 所有校验通过
    /// </summary>
    Ready,

    /// <summary>
    /// 已归档 - 归档操作完成
    /// </summary>
    Archived
}

/// <summary>
/// 工作流校验结果
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public ValidationCategory Category { get; set; }
    public static ValidationResult Success(ValidationCategory category) => new()
    {
        IsValid = true,
        Category = category,
        Message = "校验通过"
    };

    public static ValidationResult Fail(ValidationCategory category, string message) => new()
    {
        IsValid = false,
        Category = category,
        Message = message
    };
}

/// <summary>
/// 校验分类
/// </summary>
public enum ValidationCategory
{
    /// <summary>
    /// 元数据有效性
    /// </summary>
    Metadata,

    /// <summary>
    /// 视频映射完整性
    /// </summary>
    VideoMapping,

    /// <summary>
    /// 图片角色标记
    /// </summary>
    ImageAssets
}

/// <summary>
/// 媒体类型枚举
/// </summary>
public enum MediaType
{
    /// <summary>
    /// 剧集（TV Series）
    /// </summary>
    Series,

    /// <summary>
    /// 电影（Movie）
    /// </summary>
    Movie
}
