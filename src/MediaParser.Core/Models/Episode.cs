using System.Collections.Generic;

namespace MediaParser.Core.Models;

/// <summary>
/// 单集（Episode）信息模型
/// </summary>
public class Episode
{
    /// <summary>
    /// 季数
    /// </summary>
    public int Season { get; set; }

    /// <summary>
    /// 集数
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// 集标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 集简介
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 播出年份
    /// </summary>
    public int ReleaseYear { get; set; }

    /// <summary>
    /// 完整播出日期 (如 "2024-09-06")
    /// </summary>
    public string ReleaseDate { get; set; } = string.Empty;

    /// <summary>
    /// 集标签
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 映射的视频文件
    /// </summary>
    public VideoFile? MappedVideo { get; set; }

    /// <summary>
    /// 原始元数据文本
    /// </summary>
    public string RawMetadata { get; set; } = string.Empty;

    /// <summary>
    /// 是否已映射视频
    /// </summary>
    public bool IsVideoMapped => MappedVideo != null;

    /// <summary>
    /// 获取集数标识 (如 "S01E05")
    /// </summary>
    public string GetEpisodeKey() => $"S{Season:D2}E{Number:D2}";

    /// <summary>
    /// 显示标题（如 "S01E05 - 第1话"）
    /// </summary>
    public string DisplayTitle => $"S{Season:D2}E{Number:D2} - {Title}";
}
