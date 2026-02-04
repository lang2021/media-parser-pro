using System.Text.RegularExpressions;
using MediaParser.Core.Models;

namespace MediaParser.Core.Modules.MetadataParser;

/// <summary>
/// 元数据解析器配置
/// </summary>
public class MetadataParserOptions
{
    /// <summary>
    /// 是否自动识别季数（从标题或文件名中提取）
    /// </summary>
    public bool AutoDetectSeason { get; set; } = true;

    /// <summary>
    /// 默认季数（当无法识别时使用）
    /// </summary>
    public int DefaultSeason { get; set; } = 1;

    /// <summary>
    /// 是否自动推断年份
    /// </summary>
    public bool AutoDetectYear { get; set; } = true;
}

/// <summary>
/// 元数据解析结果
/// </summary>
public class ParseResult
{
    public bool Success { get; set; }
    public Show? Show { get; set; }
    public List<Episode> Episodes { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// 元数据解析器
/// 
/// 职责：
/// 1. 使用正则表达式模板解析文本
/// 2. 使用 NFO XML 解析器解析 NFO 格式
/// 3. 输出结构化 Show + Episodes 对象
/// 4. 支持提取：标题、原名、年份、制作商、导演、演员、标签
/// </summary>
public class MetadataParser
{
    private readonly MetadataParserOptions _options;
    private readonly List<RegexTemplate> _templates;
    private readonly NfoParser _nfoParser;

    public MetadataParser(MetadataParserOptions? options = null)
    {
        _options = options ?? new MetadataParserOptions();
        _templates = RegexTemplateRegistry.GetTemplates();
        _nfoParser = new NfoParser();
    }

    /// <summary>
    /// 解析元数据文本
    /// </summary>
    /// <param name="text">原始文本（支持多行粘贴）</param>
    /// <returns>解析结果</returns>
    public ParseResult Parse(string text)
    {
        var result = new ParseResult { Success = false };

        if (string.IsNullOrWhiteSpace(text))
        {
            result.Errors.Add("输入文本为空");
            return result;
        }

        // 首先尝试 NFO XML 解析（优先级最高）
        if (IsLikelyNfoFormat(text))
        {
            var nfoResult = _nfoParser.Parse(text);
            if (nfoResult.Success)
            {
                nfoResult.Warnings.Add("使用 NFO XML 解析器");
                return nfoResult;
            }
        }

        // 尝试使用每个正则模板进行解析
        foreach (var template in _templates)
        {
            var templateResult = TryParseWithTemplate(text, template);
            if (templateResult.Success)
            {
                templateResult.Warnings.Add($"使用模板: {template.Name}");
                return templateResult;
            }
        }

        // 所有模板都失败，尝试通用解析
        result = TryGenericParse(text);

        return result;
    }

    /// <summary>
    /// 检测是否为 NFO 格式
    /// 优先检查是否包含 NFO XML 标签
    /// </summary>
    private bool IsLikelyNfoFormat(string text)
    {
        // 方法1：检查是否包含 NFO XML 根标签（最可靠）
        var hasTvshowRoot = text.Contains("<tvshow", StringComparison.OrdinalIgnoreCase);
        var hasEpisodeRoot = text.Contains("<episodedetails", StringComparison.OrdinalIgnoreCase);
        
        if (hasTvshowRoot || hasEpisodeRoot)
        {
            return true;
        }
        
        // 方法2：检查是否包含常见 NFO 标签（降级方案）
        var nfoTags = new[] { "<title>", "</title>", "<plot>", "</plot>", 
                             "<actor>", "</actor>", "<genre>", "</genre>" };
        
        var matchCount = nfoTags.Count(tag => text.Contains(tag, StringComparison.OrdinalIgnoreCase));
        
        // 至少需要 4 个标签匹配
        return matchCount >= 4;
    }

    /// <summary>
    /// 使用指定模板尝试解析
    /// </summary>
    private ParseResult TryParseWithTemplate(string text, RegexTemplate template)
    {
        var result = new ParseResult { Success = false };

        try
        {
            var match = template.Pattern.Match(text);
            if (!match.Success)
            {
                return result;
            }

            var show = new Show();
            var episodes = new List<Episode>();

            // 解析剧集信息
            foreach (var field in template.Fields)
            {
                var group = match.Groups[field.Name];
                if (group?.Success == true)
                {
                    var value = group.Value.Trim();

                    switch (field.Target)
                    {
                        case FieldTarget.ShowTitle:
                            show.Title = value;
                            break;
                        case FieldTarget.OriginalTitle:
                            show.OriginalTitle = value;
                            break;
                        case FieldTarget.Year:
                            if (int.TryParse(value, out var year))
                                show.Year = year;
                            break;
                        case FieldTarget.Studio:
                            show.Studio = value;
                            break;
                        case FieldTarget.Director:
                            show.Director = value;
                            break;
                        case FieldTarget.Actors:
                            show.Actors = value.Split(new[] { ',', '、', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(a => a.Trim())
                                .Where(a => !string.IsNullOrEmpty(a))
                                .ToList();
                            break;
                        case FieldTarget.Tags:
                            show.Tags = value.Split(new[] { ',', '、', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim())
                                .Where(t => !string.IsNullOrEmpty(t))
                                .ToList();
                            break;
                        case FieldTarget.Summary:
                            show.Summary = value;
                            break;
                        case FieldTarget.EpisodeList:
                            episodes = ParseEpisodes(value, show.Season, show.Year);
                            break;
                        case FieldTarget.Rating:
                            if (double.TryParse(value, out var rating))
                                show.Rating = Math.Clamp(rating, 0, 10);
                            break;
                    }
                }
            }

            // 自动检测季数
            if (_options.AutoDetectSeason && show.Season <= 0)
            {
                var autoSeason = DetectSeasonFromText(text);
                if (autoSeason > 0)
                {
                    show.Season = autoSeason;
                }
                else
                {
                    show.Season = _options.DefaultSeason;
                }
            }

            // 自动检测年份
            if (_options.AutoDetectYear && show.Year <= 0)
            {
                var autoYear = DetectYearFromText(text);
                if (autoYear > 0)
                {
                    show.Year = autoYear;
                }
            }

            // 为没有标题的集数添加默认标题
            foreach (var ep in episodes)
            {
                if (string.IsNullOrEmpty(ep.Title))
                {
                    ep.Title = $"第{ep.Number}话";
                }
            }

            result.Success = true;
            result.Show = show;
            result.Episodes = episodes;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"模板解析异常: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 通用解析（当模板匹配失败时使用）
    /// </summary>
    private ParseResult TryGenericParse(string text)
    {
        var result = new ParseResult { Success = false };
        var show = new Show();
        var episodes = new List<Episode>();

        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // 跳过明显的非元数据行
            if (IsNonMetadataLine(trimmedLine))
                continue;

            // 尝试识别各种字段
            if (TryExtractField(trimmedLine, out var fieldType, out var value))
            {
                ApplyField(show, fieldType, value);
            }
            else
            {
                // 如果无法识别，可能是集数信息或简介
                if (IsLikelyEpisodeLine(trimmedLine))
                {
                    var epInfo = TryParseEpisodeLine(trimmedLine);
                    if (epInfo != null)
                    {
                        epInfo.Season = show.Season > 0 ? show.Season : _options.DefaultSeason;
                        epInfo.ReleaseYear = show.Year > 0 ? show.Year : DateTime.Now.Year;
                        episodes.Add(epInfo);
                    }
                }
                else if (string.IsNullOrEmpty(show.Summary) && trimmedLine.Length > 20)
                {
                    // 假设最长的行是简介
                    show.Summary = trimmedLine;
                }
            }
        }

        // 如果仍然没有集数，创建一个默认集数
        if (episodes.Count == 0)
        {
            episodes.Add(new Episode
            {
                Season = show.Season > 0 ? show.Season : _options.DefaultSeason,
                Number = 1,
                Title = "第1话",
                ReleaseYear = show.Year > 0 ? show.Year : DateTime.Now.Year
            });
            result.Warnings.Add("未能解析出具体集数，已创建默认集数");
        }

        // 确保季数设置正确
        foreach (var ep in episodes)
        {
            if (ep.Season <= 0)
                ep.Season = show.Season > 0 ? show.Season : _options.DefaultSeason;
        }

        result.Success = true;
        result.Show = show;
        result.Episodes = episodes;

        return result;
    }

    /// <summary>
    /// 解析集数列表
    /// </summary>
    private List<Episode> ParseEpisodes(string episodeText, int season, int year)
    {
        var episodes = new List<Episode>();

        // 常见格式：01-12, 01,02,03, [01][02][03]
        var patterns = new[]
        {
            @"(\d{1,3})\s*[-~至]\s*(\d{1,3})",  // 01-12
            @"\[(\d{1,3})\]",                    // [01]
            @"(\d{1,3})(?:,|、|；)",             // 01,
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(episodeText, pattern);
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    if (pattern.Contains("-") && match.Groups.Count > 2)
                    {
                        // 范围格式 01-12
                        if (int.TryParse(match.Groups[1].Value, out var start) &&
                            int.TryParse(match.Groups[2].Value, out var end))
                        {
                            for (int i = start; i <= end; i++)
                            {
                                episodes.Add(new Episode
                                {
                                    Season = season,
                                    Number = i,
                                    Title = $"第{i}话",
                                    ReleaseYear = year
                                });
                            }
                        }
                    }
                    else
                    {
                        // 单集格式
                        if (int.TryParse(match.Groups[1].Value, out var epNum))
                        {
                            episodes.Add(new Episode
                            {
                                Season = season,
                                Number = epNum,
                                Title = $"第{epNum}话",
                                ReleaseYear = year
                            });
                        }
                    }
                }
                break;
            }
        }

        return episodes;
    }

    /// <summary>
    /// 从文本中检测季数
    /// </summary>
    private int DetectSeasonFromText(string text)
    {
        // 常见模式: 第1季, Season 1, S1, 第01季
        var patterns = new[]
        {
            @"第(\d+)季",
            @"Season\s*(\d+)",
            @"S(\d+)",
            @"\((\d+)季\)",
            @"第\s*(\d+)\s*季度"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var season))
            {
                return season;
            }
        }

        return 0;
    }

    /// <summary>
    /// 从文本中检测年份
    /// </summary>
    private int DetectYearFromText(string text)
    {
        var match = Regex.Match(text, @"(19|20)\d{2}");
        if (match.Success && int.TryParse(match.Value, out var year))
        {
            return year;
        }
        return 0;
    }

    /// <summary>
    /// 判断是否为非元数据行
    /// </summary>
    private bool IsNonMetadataLine(string line)
    {
        // 跳过纯数字行、URL 行等
        if (Regex.IsMatch(line, @"^https?://", RegexOptions.IgnoreCase))
            return true;
        if (Regex.IsMatch(line, @"^\d{4}-\d{2}-\d{2}"))  // 日期行
            return true;
        if (line.Length < 3 && !char.IsLetterOrDigit(line[0]))
            return true;
        return false;
    }

    /// <summary>
    /// 尝试提取字段
    /// </summary>
    private bool TryExtractField(string line, out FieldTarget fieldType, out string value)
    {
        fieldType = FieldTarget.Unknown;
        value = string.Empty;

        var patterns = new Dictionary<FieldTarget, Regex>
        {
            { FieldTarget.ShowTitle, new Regex(@"^(?:标题|Name)[:：]\s*(.+)", RegexOptions.IgnoreCase) },
            { FieldTarget.OriginalTitle, new Regex(@"^(?:原名|Original)[:：]\s*(.+)", RegexOptions.IgnoreCase) },
            { FieldTarget.Year, new Regex(@"^(?:年份|Year|发行年份)[:：]?\s*(\d{4})", RegexOptions.IgnoreCase) },
            { FieldTarget.Studio, new Regex(@"^(?:制作商|厂商|制作公司|Studio)[:：]\s*(.+)", RegexOptions.IgnoreCase) },
            { FieldTarget.Director, new Regex(@"^(?:导演|Director)[:：]\s*(.+)", RegexOptions.IgnoreCase) },
            { FieldTarget.Actors, new Regex(@"^(?:演员|Cast|主演|声优)[:：]\s*(.+)", RegexOptions.IgnoreCase) },
            { FieldTarget.Tags, new Regex(@"^(?:标签|Tags|类型|Genre)[:：]\s*(.+)", RegexOptions.IgnoreCase) },
            { FieldTarget.Rating, new Regex(@"^(?:评分|Rating)[:：]?\s*(\d+\.?\d*)", RegexOptions.IgnoreCase) },
        };

        foreach (var kvp in patterns)
        {
            var match = kvp.Value.Match(line);
            if (match.Success)
            {
                fieldType = kvp.Key;
                value = match.Groups[1].Value.Trim();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 应用字段值
    /// </summary>
    private void ApplyField(Show show, FieldTarget fieldType, string value)
    {
        switch (fieldType)
        {
            case FieldTarget.ShowTitle:
                if (string.IsNullOrEmpty(show.Title))
                    show.Title = value;
                break;
            case FieldTarget.OriginalTitle:
                if (string.IsNullOrEmpty(show.OriginalTitle))
                    show.OriginalTitle = value;
                break;
            case FieldTarget.Year:
                if (show.Year <= 0 && int.TryParse(value, out var year))
                    show.Year = year;
                break;
            case FieldTarget.Studio:
                if (string.IsNullOrEmpty(show.Studio))
                    show.Studio = value;
                break;
            case FieldTarget.Director:
                if (string.IsNullOrEmpty(show.Director))
                    show.Director = value;
                break;
            case FieldTarget.Actors:
                if (show.Actors.Count == 0)
                    show.Actors = value.Split(new[] { ',', '、', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(a => a.Trim())
                        .Where(a => !string.IsNullOrEmpty(a))
                        .ToList();
                break;
            case FieldTarget.Tags:
                if (show.Tags.Count == 0)
                    show.Tags = value.Split(new[] { ',', '、', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .ToList();
                break;
        }
    }

    /// <summary>
    /// 判断是否为可能的集数行
    /// </summary>
    private bool IsLikelyEpisodeLine(string line)
    {
        // 第1话, 01话, 1话, [01], 第01集
        var patterns = new[]
        {
            @"^第\d+话",
            @"^\[\d+\]",
            @"^\d+话",
            @"^第\d+集",
            @"^Ep?\s*\d+",
            @"^\d{2}$"
        };

        return patterns.Any(p => Regex.IsMatch(line, p));
    }

    /// <summary>
    /// 尝试解析集数行
    /// </summary>
    private Episode? TryParseEpisodeLine(string line)
    {
        var patterns = new Dictionary<string, Func<Match, Episode?>>
        {
            { @"第(\d+)话", m => new Episode { Number = int.Parse(m.Groups[1].Value), Title = m.Groups[0].Value } },
            { @"\[(\d+)\]", m => new Episode { Number = int.Parse(m.Groups[1].Value), Title = m.Groups[0].Value } },
            { @"第(\d+)集", m => new Episode { Number = int.Parse(m.Groups[1].Value), Title = m.Groups[0].Value } },
            { @"Ep?\s*(\d+)", m => new Episode { Number = int.Parse(m.Groups[1].Value), Title = m.Groups[0].Value } },
        };

        foreach (var kvp in patterns)
        {
            var match = Regex.Match(line, kvp.Key);
            if (match.Success)
            {
                return kvp.Value(match);
            }
        }

        return null;
    }
}

/// <summary>
/// 字段目标类型
/// </summary>
public enum FieldTarget
{
    Unknown,
    ShowTitle,
    OriginalTitle,
    Year,
    Studio,
    Director,
    Actors,
    Tags,
    Summary,
    EpisodeList,
    Rating
}
