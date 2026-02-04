using System.Text.RegularExpressions;

namespace MediaParser.Core.Modules.MetadataParser;

/// <summary>
/// 正则表达式模板
/// </summary>
public class RegexTemplate
{
    /// <summary>
    /// 模板名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 正则表达式模式
    /// </summary>
    public Regex Pattern { get; set; } = null!;

    /// <summary>
    /// 模板字段定义
    /// </summary>
    public List<RegexField> Fields { get; set; } = new();

    /// <summary>
    /// 模板优先级（数字越小优先级越高）
    /// </summary>
    public int Priority { get; set; }
}

/// <summary>
/// 正则字段定义
/// </summary>
public class RegexField
{
    /// <summary>
    /// 字段名称（对应正则捕获组名称）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 字段说明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 目标属性
    /// </summary>
    public FieldTarget Target { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; }
}

/// <summary>
/// 正则模板注册表
/// </summary>
public static class RegexTemplateRegistry
{
    private static readonly List<RegexTemplate> _templates = new();

    static RegexTemplateRegistry()
    {
        InitializeTemplates();
    }

    /// <summary>
    /// 获取所有模板（按优先级排序）
    /// </summary>
    public static List<RegexTemplate> GetTemplates()
    {
        return _templates.OrderBy(t => t.Priority).ToList();
    }

    /// <summary>
    /// 初始化模板集合
    /// </summary>
    private static void InitializeTemplates()
    {
        // 模板1：标准格式 - 标题|原名|年份|制作商|导演|演员|标签|集数
        _templates.Add(new RegexTemplate
        {
            Name = "标准格式（竖线分隔）",
            Priority = 1,
            Pattern = new Regex(
                @"^(?<Title>[^|]+)\s*\|\s*(?<OriginalTitle>[^|]*)\s*\|\s*(?<Year>\d{4})\s*\|\s*(?<Studio>[^|]*)\s*\|\s*(?<Director>[^|]*)\s*\|\s*(?<Actors>[^|]*)\s*\|\s*(?<Tags>[^|]*)\s*\|\s*(?<Episodes>[^|]+)$",
                RegexOptions.IgnoreCase),
            Fields = new List<RegexField>
            {
                new() { Name = "Title", Target = FieldTarget.ShowTitle, IsRequired = true },
                new() { Name = "OriginalTitle", Target = FieldTarget.OriginalTitle },
                new() { Name = "Year", Target = FieldTarget.Year },
                new() { Name = "Studio", Target = FieldTarget.Studio },
                new() { Name = "Director", Target = FieldTarget.Director },
                new() { Name = "Actors", Target = FieldTarget.Actors },
                new() { Name = "Tags", Target = FieldTarget.Tags },
                new() { Name = "Episodes", Target = FieldTarget.EpisodeList }
            }
        });

        // 模板2：AT-X 格式
        _templates.Add(new RegexTemplate
        {
            Name = "AT-X格式",
            Priority = 2,
            Pattern = new Regex(
                @"^(?<Title>[^\(]+?)\s*(?:\((?<OriginalTitle>[^)]+)\))?\s*(?:\((?<Year>\d{4})\))?\s*(?:\[(?<Studio>[^\]]+)\])?(?:\s*(?<Director>导演[:：]?[^，,]+))?(?:\s*(?<Actors>演员[:：]?[^，,]+))?(?:\s*【(?<Tags>[^【】]+)】)?\s*(?:-\s*(?<Episodes>\d+(?:-\d+)?))?",
                RegexOptions.IgnoreCase),
            Fields = new List<RegexField>
            {
                new() { Name = "Title", Target = FieldTarget.ShowTitle, IsRequired = true },
                new() { Name = "OriginalTitle", Target = FieldTarget.OriginalTitle },
                new() { Name = "Year", Target = FieldTarget.Year },
                new() { Name = "Studio", Target = FieldTarget.Studio },
                new() { Name = "Director", Target = FieldTarget.Director },
                new() { Name = "Actors", Target = FieldTarget.Actors },
                new() { Name = "Tags", Target = FieldTarget.Tags },
                new() { Name = "Episodes", Target = FieldTarget.EpisodeList }
            }
        });

        // 模板3：带集数标题格式
        _templates.Add(new RegexTemplate
        {
            Name = "带集数标题格式",
            Priority = 3,
            Pattern = new Regex(
                @"^\[?(?<Title>[^\[\]第话\d]+?)(?:第(?:\d+季\s*)?(?:\d+)[话集])?\]?\s*(?:\((?<Year>\d{4})\))?(?:\s*-\s*(?<Studio>[^-]+))?(?:\s*导演[:：]\s*(?<Director>[^,\n]+))?(?:\s*演员[:：]\s*(?<Actors>[^\n]+))?",
                RegexOptions.IgnoreCase),
            Fields = new List<RegexField>
            {
                new() { Name = "Title", Target = FieldTarget.ShowTitle, IsRequired = true },
                new() { Name = "Year", Target = FieldTarget.Year },
                new() { Name = "Studio", Target = FieldTarget.Studio },
                new() { Name = "Director", Target = FieldTarget.Director },
                new() { Name = "Actors", Target = FieldTarget.Actors }
            }
        });

        // 模板4：简单格式（标题 + 年份）
        _templates.Add(new RegexTemplate
        {
            Name = "简单格式",
            Priority = 4,
            Pattern = new Regex(
                @"^(?<Title>[^\d\(【【】】\[\]]+?)(?:\s*\(?(?<Year>\d{4})\)?)?(?:\s*【(?<Tags>[^【】]+)】)?(?:\s*-\s*(?<Studio>[^-]+))?",
                RegexOptions.IgnoreCase),
            Fields = new List<RegexField>
            {
                new() { Name = "Title", Target = FieldTarget.ShowTitle, IsRequired = true },
                new() { Name = "Year", Target = FieldTarget.Year },
                new() { Name = "Studio", Target = FieldTarget.Studio },
                new() { Name = "Tags", Target = FieldTarget.Tags }
            }
        });

        // 模板5：日文格式
        _templates.Add(new RegexTemplate
        {
            Name = "日文格式",
            Priority = 5,
            Pattern = new Regex(
                @"^(?<Title>[^\(【【】】\[\]]+?)(?:\s*\((?<OriginalTitle>[^)]+)\))?(?:\s*\[(?<Studio>[^\]]+)\])?(?:\s*(?<Year>\d{4}))?(?:\s*-\s*(?<Episodes>\d+(?:-\d+)?))?",
                RegexOptions.IgnoreCase),
            Fields = new List<RegexField>
            {
                new() { Name = "Title", Target = FieldTarget.ShowTitle, IsRequired = true },
                new() { Name = "OriginalTitle", Target = FieldTarget.OriginalTitle },
                new() { Name = "Studio", Target = FieldTarget.Studio },
                new() { Name = "Year", Target = FieldTarget.Year },
                new() { Name = "Episodes", Target = FieldTarget.EpisodeList }
            }
        });

        // 模板6：豆瓣/ bangumi 格式（带评分）
        _templates.Add(new RegexTemplate
        {
            Name = "豆瓣/bangumi格式",
            Priority = 6,
            Pattern = new Regex(
                @"^(?<Title>[^\(【【】】\[\]]+?)\s*(?:\((?<OriginalTitle>[^)]+)\))?\s*(?:\(?(?<Year>\d{4})\)?)?\s*(?:评分[:：]?\s*(?<Rating>\d+\.?\d*))?(?:\s*制作[:：]?\s*(?<Studio>[^,\n]+))?",
                RegexOptions.IgnoreCase),
            Fields = new List<RegexField>
            {
                new() { Name = "Title", Target = FieldTarget.ShowTitle, IsRequired = true },
                new() { Name = "OriginalTitle", Target = FieldTarget.OriginalTitle },
                new() { Name = "Year", Target = FieldTarget.Year },
                new() { Name = "Rating", Target = FieldTarget.Rating },
                new() { Name = "Studio", Target = FieldTarget.Studio }
            }
        });

        // 模板7：多行格式（带标签）
        _templates.Add(new RegexTemplate
        {
            Name = "多行格式",
            Priority = 7,
            Pattern = new Regex(
                @"标题[:：]\s*(?<Title>[^\n]+?)(?:\n原名[:：]\s*(?<OriginalTitle>[^\n]+))?(?:\n年份[:：]?\s*(?<Year>\d{4}))?(?:\n制作商[:：]\s*(?<Studio>[^\n]+))?(?:\n导演[:：]\s*(?<Director>[^\n]+))?(?:\n演员[:：]\s*(?<Actors>[^\n]+))?(?:\n标签[:：]\s*(?<Tags>[^\n]+))?",
                RegexOptions.IgnoreCase),
            Fields = new List<RegexField>
            {
                new() { Name = "Title", Target = FieldTarget.ShowTitle, IsRequired = true },
                new() { Name = "OriginalTitle", Target = FieldTarget.OriginalTitle },
                new() { Name = "Year", Target = FieldTarget.Year },
                new() { Name = "Studio", Target = FieldTarget.Studio },
                new() { Name = "Director", Target = FieldTarget.Director },
                new() { Name = "Actors", Target = FieldTarget.Actors },
                new() { Name = "Tags", Target = FieldTarget.Tags }
            }
        });
    }
}
