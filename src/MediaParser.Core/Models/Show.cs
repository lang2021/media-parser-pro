using System.Collections.Generic;

namespace MediaParser.Core.Models;

/// <summary>
/// 剧集（Show）元数据模型
/// </summary>
public class Show
{
    /// <summary>
    /// 标题（中文名）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 原名（原始语言名称）
    /// </summary>
    public string OriginalTitle { get; set; } = string.Empty;

    /// <summary>
    /// 排序标题
    /// </summary>
    public string SortTitle { get; set; } = string.Empty;

    /// <summary>
    /// 发行日期
    /// </summary>
    public string Premiered { get; set; } = string.Empty;

    /// <summary>
    /// 简介/剧情
    /// </summary>
    public string Plot { get; set; } = string.Empty;

    /// <summary>
    /// 年份
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// 制作商/工作室
    /// </summary>
    public string Studio { get; set; } = string.Empty;

    /// <summary>
    /// 导演
    /// </summary>
    public string Director { get; set; } = string.Empty;

    /// <summary>
    /// 演员列表
    /// </summary>
    public List<string> Actors { get; set; } = new();

    /// <summary>
    /// 标签列表
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 剧集列表
    /// </summary>
    public List<Episode> Episodes { get; set; } = new();

    /// <summary>
    /// 简介
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 评分 (0-10)
    /// </summary>
    public double Rating { get; set; }

    /// <summary>
    /// 剧集类型 (TV/OVA/ONA/Movie)
    /// </summary>
    public string ShowType { get; set; } = "TV";

    /// <summary>
    /// 状态 (Airing/Completed/Hiatus/Cancelled)
    /// </summary>
    public string Status { get; set; } = "Completed";

    /// <summary>
    /// 季数
    /// </summary>
    public int Season { get; set; } = 1;

    /// <summary>
    /// 原始元数据文本（用于重新解析）
    /// </summary>
    public string RawMetadata { get; set; } = string.Empty;
}
