namespace MediaParser.Core.Models;

/// <summary>
/// 图片资源模型
/// </summary>
public class ImageAsset
{
    /// <summary>
    /// 完整路径
    /// </summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 图片角色类型
    /// </summary>
    public ImageRole Role { get; set; } = ImageRole.Unknown;

    /// <summary>
    /// 图片宽度
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 图片高度
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 图片格式
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// 是否已标记角色
    /// </summary>
    public bool IsRoleAssigned => Role != ImageRole.Unknown;

    // ==================== UI 状态属性 ====================

    /// <summary>
    /// 是否被选中（用于 UI）
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// 是否被勾选（用于 UI）
    /// </summary>
    public bool IsChecked { get; set; }

    /// <summary>
    /// 获取图片尺寸字符串
    /// </summary>
    public string GetDimensions() => $"{Width}x{Height}";
}

/// <summary>
/// 图片角色类型
/// </summary>
public enum ImageRole
{
    /// <summary>
    /// 未指定
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 海报（封面）
    /// </summary>
    Poster,

    /// <summary>
    /// 背景图
    /// </summary>
    Fanart,

    /// <summary>
    /// 缩略图
    /// </summary>
    Thumb,

    /// <summary>
    /// 剧照
    /// </summary>
    Still,

    /// <summary>
    /// 横幅
    /// </summary>
    Banner,

    /// <summary>
    /// 角色图
    /// </summary>
    Character,

    /// <summary>
    /// Logo
    /// </summary>
    Logo
}
