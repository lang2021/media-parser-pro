using System.Text.RegularExpressions;
using System.Xml.Linq;
using MediaParser.Core.Models;

namespace MediaParser.Core.Modules.MetadataParser;

/// <summary>
/// NFO XML 解析器
/// 支持解析 Kodi/MediaCenter 标准的 NFO 文件格式
/// </summary>
public class NfoParser
{
    /// <summary>
    /// 解析 NFO 文本
    /// </summary>
    /// <param name="nfoText">NFO 文件内容</param>
    /// <returns>解析结果</returns>
    public ParseResult Parse(string nfoText)
    {
        var result = new ParseResult { Success = false };

        if (string.IsNullOrWhiteSpace(nfoText))
        {
            result.Errors.Add("NFO 文本为空");
            return result;
        }

        try
        {
            // 首先提取纯 NFO XML 内容（处理文本中混有描述性文字的情况）
            var cleanText = ExtractNfoContent(nfoText);
            
            if (string.IsNullOrWhiteSpace(cleanText))
            {
                result.Errors.Add("无法提取 NFO 内容");
                return result;
            }
            
            // 尝试解析单个 NFO 块（tvshow 或 episodedetails）
            var doc = XDocument.Parse(cleanText);
            
            // 检测 NFO 类型
            if (doc.Root?.Name.LocalName == "tvshow")
            {
                result = ParseTvShowNfo(doc);
                
                // tvshow 之后可能还有 episodedetails，尝试解析
                var episodes = TryParseEpisodesFromText(nfoText);
                if (episodes.Any())
                {
                    result.Episodes = episodes;
                }
            }
            else if (doc.Root?.Name.LocalName == "episodedetails")
            {
                result = ParseEpisodeNfo(doc);
                
                // 尝试从文本中提取更多信息
                var showInfo = TryParseShowInfoFromText(nfoText);
                if (showInfo != null)
                {
                    if (string.IsNullOrEmpty(result.Show?.Title) && !string.IsNullOrEmpty(showInfo.Title))
                        result.Show!.Title = showInfo.Title;
                    if (string.IsNullOrEmpty(result.Show?.OriginalTitle) && !string.IsNullOrEmpty(showInfo.OriginalTitle))
                        result.Show!.OriginalTitle = showInfo.OriginalTitle;
                }
            }
            else
            {
                // 尝试混合模式（包含多个 NFO 内容）
                result = TryParseMixedNfo(cleanText);
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"XML 解析失败: {ex.Message}");
        }

        return result;
    }
    
    /// <summary>
    /// 从文本中提取剧集信息
    /// </summary>
    private List<Episode> TryParseEpisodesFromText(string text)
    {
        var episodes = new List<Episode>();
        
        // 查找所有 <episodedetails> 块
        var pattern = @"<episodedetails[^>]*>(.*?)</episodedetails>";
        var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        foreach (Match match in matches)
        {
            try
            {
                var episodeDoc = XDocument.Parse(match.Value);
                var epResult = ParseEpisodeNfo(episodeDoc);
                if (epResult.Success && epResult.Episodes.Any())
                {
                    episodes.Add(epResult.Episodes.First());
                }
            }
            catch
            {
                // 忽略解析失败的块
            }
        }
        
        return episodes;
    }
    
    /// <summary>
    /// 从文本中提取剧集信息（当主解析失败时的备用方法）
    /// </summary>
    private Show? TryParseShowInfoFromText(string text)
    {
        var show = new Show();
        var updated = false;
        
        // 尝试提取标题
        var titleMatch = Regex.Match(text, @"<title[^>]*>([^<]+)</title>", RegexOptions.IgnoreCase);
        if (titleMatch.Success)
        {
            show.Title = titleMatch.Groups[1].Value.Trim();
            updated = true;
        }
        
        return updated ? show : null;
    }

    /// <summary>
    /// 从混合文本中提取 NFO XML 内容
    /// 处理如 "tvshow.nfo：<tvshow>...</tvshow>" 的情况
    /// </summary>
    private string ExtractNfoContent(string text)
    {
        // 查找第一个 <tvshow> 块
        var tvshowStartMatch = Regex.Match(text, @"<tvshow[^>]*>", RegexOptions.IgnoreCase);
        if (tvshowStartMatch.Success)
        {
            var endMatch = Regex.Match(text.Substring(tvshowStartMatch.Index), @"</tvshow>", RegexOptions.IgnoreCase);
            if (endMatch.Success)
            {
                // endMatch.Index 是从 tvshowStartMatch.Index 开始的相对位置
                // 需要加上 tvshowStartMatch.Length 来获取结束位置的绝对索引
                // 然后加上 endMatch.Length 来包含结束标签
                var endIndex = tvshowStartMatch.Index + endMatch.Index + endMatch.Length;
                var content = text.Substring(tvshowStartMatch.Index, endIndex - tvshowStartMatch.Index);
                return content;
            }
        }
        
        // 查找第一个 <episodedetails> 块
        var episodeStartMatch = Regex.Match(text, @"<episodedetails[^>]*>", RegexOptions.IgnoreCase);
        if (episodeStartMatch.Success)
        {
            var endMatch = Regex.Match(text.Substring(episodeStartMatch.Index), @"</episodedetails>", RegexOptions.IgnoreCase);
            if (endMatch.Success)
            {
                var endIndex = episodeStartMatch.Index + endMatch.Index + endMatch.Length;
                var content = text.Substring(episodeStartMatch.Index, endIndex - episodeStartMatch.Index);
                return content;
            }
        }
        
        // 没有找到标准 NFO 标签，返回原文本
        return text;
    }

    /// <summary>
    /// 移除 XML 声明
    /// </summary>
    private string RemoveXmlDeclaration(string text)
    {
        // 移除 <?xml ... ?> 声明
        var result = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"<\?xml[^?]*\?>",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return result.Trim();
    }

    /// <summary>
    /// 解析 tvshow NFO
    /// </summary>
    private ParseResult ParseTvShowNfo(XDocument doc)
    {
        var result = new ParseResult { Success = true };
        var root = doc.Root;

        var show = new Show();

        // 解析标题
        show.Title = GetElementValue(root, "title") ?? "";
        show.OriginalTitle = GetElementValue(root, "originaltitle") ?? "";
        show.SortTitle = GetElementValue(root, "sorttitle") ?? "";

        // 解析首播日期（完整日期和年份）
        var premiered = GetElementValue(root, "premiered");
        if (!string.IsNullOrEmpty(premiered))
        {
            show.Premiered = premiered;
            if (DateTime.TryParse(premiered, out var date))
            {
                show.Year = date.Year;
            }
        }

        // 解析简介（Plot 和 Summary 都填充）
        var plot = GetElementValue(root, "plot") ?? "";
        show.Plot = plot;
        show.Summary = plot;

        // 解析制作商
        show.Studio = GetElementValue(root, "studio") ?? "";

        // 解析导演
        show.Director = GetElementValue(root, "director") ?? "";

        // 解析标签（genre 和 tag 可能有多个）
        var tagsList = new List<string>();
        
        if (root != null)
        {
            // 解析 genre 元素
            var genres = root.Elements("genre");
            foreach (var genre in genres)
            {
                var genreText = genre.Value?.Trim();
                if (!string.IsNullOrEmpty(genreText) && genreText != "成人动画") // 跳过通用分类
                {
                    tagsList.Add(genreText);
                }
            }
        
            // 解析 tag 元素
            var tags = root.Elements("tag");
            foreach (var tag in tags)
            {
                var tagText = tag.Value?.Trim();
                if (!string.IsNullOrEmpty(tagText) && !tagsList.Contains(tagText))
                {
                    tagsList.Add(tagText);
                }
            }
        }
        
        show.Tags = tagsList;

        // 解析演员（actor 元素）
        var actors = new List<string>();
        var actorElements = root.Elements("actor");
        foreach (var actor in actorElements)
        {
            var name = GetElementValue(actor, "name");
            if (!string.IsNullOrEmpty(name))
            {
                var role = GetElementValue(actor, "role");
                var voice = GetElementValue(actor, "voice");
                
                if (!string.IsNullOrEmpty(voice))
                {
                    actors.Add($"{name} (CV: {voice})");
                }
                else if (!string.IsNullOrEmpty(role))
                {
                    actors.Add($"{name} ({role})");
                }
                else
                {
                    actors.Add(name);
                }
            }
        }
        show.Actors = actors;

        result.Show = show;
        return result;
    }

    /// <summary>
    /// 解析 episode NFO
    /// </summary>
    private ParseResult ParseEpisodeNfo(XDocument doc)
    {
        var result = new ParseResult { Success = true };
        var root = doc.Root;

        var episode = new Episode();

        // 解析集标题
        episode.Title = GetElementValue(root, "title") ?? "";

        // 解析季号和集号
        var season = GetElementValue(root, "season");
        var epNum = GetElementValue(root, "episode");

        if (!string.IsNullOrEmpty(season) && int.TryParse(season, out var s))
        {
            episode.Season = s;
        }
        if (!string.IsNullOrEmpty(epNum) && int.TryParse(epNum, out var e))
        {
            episode.Number = e;
        }

        // 解析播出日期
        var aired = GetElementValue(root, "aired");
        if (!string.IsNullOrEmpty(aired))
        {
            episode.ReleaseDate = aired;
            episode.ReleaseYear = aired.Length >= 4 ? int.Parse(aired.Substring(0, 4)) : DateTime.Now.Year;
            // 注意：不再将"首播: 日期"添加到Summary，只存储ReleaseDate
        }

        // 解析简介（过滤掉"首播: "前缀）
        var plot = GetElementValue(root, "plot");
        if (!string.IsNullOrEmpty(plot))
        {
            // 过滤掉"首播: "前缀
            var cleanPlot = plot.Replace("首播: ", "").Trim();
            episode.Summary = string.IsNullOrEmpty(episode.Summary) 
                ? cleanPlot 
                : $"{episode.Summary}\n{cleanPlot}";
        }

        // 解析 Episode 级别的标签（genre 和 tag 可能有多个）
        if (root != null)
        {
            var epTagsList = new List<string>();
            
            // 解析 genre 元素
            var genres = root.Elements("genre");
            foreach (var genre in genres)
            {
                var genreText = genre.Value?.Trim();
                if (!string.IsNullOrEmpty(genreText) && genreText != "成人动画")
                {
                    epTagsList.Add(genreText);
                }
            }
            
            // 解析 tag 元素
            var tags = root.Elements("tag");
            foreach (var tag in tags)
            {
                var tagText = tag.Value?.Trim();
                if (!string.IsNullOrEmpty(tagText) && !epTagsList.Contains(tagText))
                {
                    epTagsList.Add(tagText);
                }
            }
            
            episode.Tags = epTagsList;
        }

        result.Episodes.Add(episode);

        // 创建一个空的 Show
        result.Show = new Show();

        return result;
    }

    /// <summary>
    /// 尝试解析混合 NFO 文本（包含多个 NFO，可能混有其他文本）
    /// </summary>
    private ParseResult TryParseMixedNfo(string text)
    {
        var result = new ParseResult { Success = false };
        var show = new Show();
        var episodes = new List<Episode>();

        // 方法：找到所有 <tvshow> 和 <episodedetails> 的完整块
        // 使用更可靠的方法：按索引位置分割
        var tvshowStarts = new List<int>();
        var episodeStarts = new List<int>();
        
        // 找到所有开始标签的位置
        var tvshowStartPattern = new Regex(@"<tvshow[^>]*>", RegexOptions.IgnoreCase);
        var episodeStartPattern = new Regex(@"<episodedetails[^>]*>", RegexOptions.IgnoreCase);
        
        foreach (Match match in tvshowStartPattern.Matches(text))
        {
            tvshowStarts.Add(match.Index);
        }
        
        foreach (Match match in episodeStartPattern.Matches(text))
        {
            episodeStarts.Add(match.Index);
        }
        
        // 合并并排序所有开始位置
        var allStarts = tvshowStarts.Concat(episodeStarts)
            .OrderBy(i => i)
            .Distinct()
            .ToList();
        
        // 找到所有结束标签的位置
        var tvshowEndPattern = new Regex(@"</tvshow>");
        var episodeEndPattern = new Regex(@"</episodedetails>");
        
        var tvshowEnds = new List<(int Start, int End)>();
        var episodeEnds = new List<(int Start, int End)>();
        
        foreach (Match startMatch in tvshowStartPattern.Matches(text))
        {
            var endMatch = tvshowEndPattern.Match(text, startMatch.Index + startMatch.Length);
            if (endMatch.Success)
            {
                tvshowEnds.Add((startMatch.Index, endMatch.Index + endMatch.Length));
            }
        }
        
        foreach (Match startMatch in episodeStartPattern.Matches(text))
        {
            var endMatch = episodeEndPattern.Match(text, startMatch.Index + startMatch.Length);
            if (endMatch.Success)
            {
                episodeEnds.Add((startMatch.Index, endMatch.Index + endMatch.Length));
            }
        }
        
        // 合并所有块
        var allBlocks = new List<(int Start, int End, string Type)>();
        foreach (var block in tvshowEnds)
        {
            allBlocks.Add((block.Start, block.End, "tvshow"));
        }
        foreach (var block in episodeEnds)
        {
            allBlocks.Add((block.Start, block.End, "episodedetails"));
        }
        
        // 按开始位置排序
        allBlocks = allBlocks.OrderBy(b => b.Start).ToList();
        
        foreach (var block in allBlocks)
        {
            var nfoContent = text.Substring(block.Start, block.End - block.Start);
            
            try
            {
                var doc = XDocument.Parse(nfoContent);
                ParseResult? parseResult = null;
                
                if (doc.Root?.Name.LocalName == "tvshow")
                {
                    parseResult = ParseTvShowNfo(doc);
                }
                else if (doc.Root?.Name.LocalName == "episodedetails")
                {
                    parseResult = ParseEpisodeNfo(doc);
                }
                
                if (parseResult != null && parseResult.Success)
                {
                    if (parseResult.Show != null)
                    {
                        MergeShow(show, parseResult.Show);
                    }
                    if (parseResult.Episodes.Any())
                    {
                        episodes.AddRange(parseResult.Episodes);
                    }
                    result.Success = true;
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"解析 NFO 块失败: {ex.Message}");
            }
        }

        // 如果没有成功解析任何块，尝试从文件名中提取集数信息
        if (!result.Success && episodes.Count == 0)
        {
            var episodePattern = @"S(\d+)E(\d+)|[- ](\d{2,3})(?:\.|\s|$|\)|])";
            var epMatches = Regex.Matches(text, episodePattern, RegexOptions.IgnoreCase);
            
            var extractedEpisodes = new HashSet<int>();
            foreach (Match epMatch in epMatches)
            {
                int season = 1;
                int epNum = 0;
                
                if (epMatch.Groups[1].Success && epMatch.Groups[2].Success)
                {
                    int.TryParse(epMatch.Groups[1].Value, out season);
                    int.TryParse(epMatch.Groups[2].Value, out epNum);
                }
                else if (epMatch.Groups[3].Success)
                {
                    int.TryParse(epMatch.Groups[3].Value, out epNum);
                    season = 1;
                }
                
                if (epNum > 0 && !extractedEpisodes.Contains(epNum))
                {
                    extractedEpisodes.Add(epNum);
                    episodes.Add(new Episode
                    {
                        Season = season,
                        Number = epNum,
                        Title = $"第{epNum}话",
                        ReleaseYear = 2024
                    });
                }
            }
            
            if (episodes.Count > 0)
            {
                result.Success = true;
                result.Warnings.Add("从文件名中提取了集数信息");
            }
        }

        result.Show = show;
        result.Episodes = episodes;

        return result;
    }

    /// <summary>
    /// 合并 Show 信息
    /// </summary>
    private void MergeShow(Show target, Show source)
    {
        if (string.IsNullOrEmpty(target.Title) && !string.IsNullOrEmpty(source.Title))
            target.Title = source.Title;
        if (string.IsNullOrEmpty(target.OriginalTitle) && !string.IsNullOrEmpty(source.OriginalTitle))
            target.OriginalTitle = source.OriginalTitle;
        if (string.IsNullOrEmpty(target.SortTitle) && !string.IsNullOrEmpty(source.SortTitle))
            target.SortTitle = source.SortTitle;
        if (string.IsNullOrEmpty(target.Premiered) && !string.IsNullOrEmpty(source.Premiered))
            target.Premiered = source.Premiered;
        if (string.IsNullOrEmpty(target.Plot) && !string.IsNullOrEmpty(source.Plot))
            target.Plot = source.Plot;
        if (target.Year == 0 && source.Year > 0)
            target.Year = source.Year;
        if (string.IsNullOrEmpty(target.Studio) && !string.IsNullOrEmpty(source.Studio))
            target.Studio = source.Studio;
        if (string.IsNullOrEmpty(target.Director) && !string.IsNullOrEmpty(source.Director))
            target.Director = source.Director;
        
        // 合并演员列表
        if (source.Actors.Any())
        {
            if (!target.Actors.Any())
            {
                target.Actors = source.Actors;
            }
            else
            {
                foreach (var actor in source.Actors)
                {
                    if (!target.Actors.Contains(actor))
                    {
                        target.Actors.Add(actor);
                    }
                }
            }
        }
        
        // 合并标签列表
        if (source.Tags.Any())
        {
            if (!target.Tags.Any())
            {
                target.Tags = source.Tags;
            }
            else
            {
                foreach (var tag in source.Tags)
                {
                    if (!target.Tags.Contains(tag))
                    {
                        target.Tags.Add(tag);
                    }
                }
            }
        }
        
        if (string.IsNullOrEmpty(target.Summary) && !string.IsNullOrEmpty(source.Summary))
            target.Summary = source.Summary;
    }

    /// <summary>
    /// 获取子元素值
    /// </summary>
    private string? GetElementValue(XElement? parent, string elementName)
    {
        if (parent == null) return null;
        
        var element = parent.Element(elementName);
        return element?.Value?.Trim();
    }
}
