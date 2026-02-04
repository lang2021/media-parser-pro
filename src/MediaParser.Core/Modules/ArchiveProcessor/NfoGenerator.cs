using System.Text;
using MediaParser.Core.Models;

namespace MediaParser.Core.Modules.ArchiveProcessor;

/// <summary>
/// NFO 文件生成器
/// 生成符合 Kodi/Emby/Jellyfin 规范的简洁 NFO 文件
/// </summary>
public class NfoGenerator
{
    /// <summary>
    /// 生成 tvshow.nfo 内容（简洁版）
    /// 参考格式：title, originaltitle, year, studio, director, actor, tag, plot
    /// </summary>
    public string GenerateTvshowNfo(Show show)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<tvshow>");

        // 标题
        if (!string.IsNullOrEmpty(show.Title))
        {
            sb.AppendLine($"  <title>{EscapeXml(show.Title)}</title>");
        }

        // 原标题
        if (!string.IsNullOrEmpty(show.OriginalTitle))
        {
            sb.AppendLine($"  <originaltitle>{EscapeXml(show.OriginalTitle)}</originaltitle>");
        }

        // 年份
        if (show.Year > 0)
        {
            sb.AppendLine($"  <year>{show.Year}</year>");
        }

        // 制作商
        if (!string.IsNullOrEmpty(show.Studio))
        {
            sb.AppendLine($"  <studio>{EscapeXml(show.Studio)}</studio>");
        }

        // 导演
        if (!string.IsNullOrEmpty(show.Director))
        {
            sb.AppendLine($"  <director>{EscapeXml(show.Director)}</director>");
        }

        // 演员
        if (show.Actors.Count > 0)
        {
            foreach (var actor in show.Actors)
            {
                sb.AppendLine($"  <actor>");
                sb.AppendLine($"    <name>{EscapeXml(actor)}</name>");
                sb.AppendLine($"  </actor>");
            }
        }

        // 标签
        if (show.Tags.Count > 0)
        {
            foreach (var tag in show.Tags)
            {
                sb.AppendLine($"  <tag>{EscapeXml(tag)}</tag>");
            }
        }

        // 简介
        if (!string.IsNullOrEmpty(show.Summary))
        {
            sb.AppendLine($"  <plot>{EscapeXml(show.Summary)}</plot>");
        }

        sb.AppendLine("</tvshow>");

        return sb.ToString();
    }

    /// <summary>
    /// 生成 episode.nfo 内容（简洁版）
    /// 参考格式：title, season, episode, showtitle, originaltitle, aired, plot, director, actor, tag
    /// </summary>
    public string GenerateEpisodeNfo(Show show, Episode episode)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<episodedetails>");

        // 标题
        if (!string.IsNullOrEmpty(episode.Title))
        {
            sb.AppendLine($"  <title>{EscapeXml(episode.Title)}</title>");
        }
        else
        {
            sb.AppendLine($"  <title>第{episode.Number}话</title>");
        }

        // 季和集数
        sb.AppendLine($"  <season>{episode.Season}</season>");
        sb.AppendLine($"  <episode>{episode.Number}</episode>");

        // 剧集标题
        if (!string.IsNullOrEmpty(show.Title))
        {
            sb.AppendLine($"  <showtitle>{EscapeXml(show.Title)}</showtitle>");
        }

        // 原标题
        if (!string.IsNullOrEmpty(show.OriginalTitle))
        {
            sb.AppendLine($"  <originaltitle>{EscapeXml(show.OriginalTitle)}</originaltitle>");
        }

        // 播出日期（使用完整日期）
        if (!string.IsNullOrEmpty(episode.ReleaseDate))
        {
            sb.AppendLine($"  <aired>{episode.ReleaseDate}</aired>");
        }
        else if (episode.ReleaseYear > 0)
        {
            sb.AppendLine($"  <aired>{episode.ReleaseYear}-01-01</aired>");
        }

        // 集简介（过滤掉"首播: "前缀）
        if (!string.IsNullOrEmpty(episode.Summary))
        {
            var cleanSummary = episode.Summary.Replace("首播: ", "").Trim();
            if (!string.IsNullOrEmpty(cleanSummary))
            {
                sb.AppendLine($"  <plot>{EscapeXml(cleanSummary)}</plot>");
            }
        }

        // 导演
        if (!string.IsNullOrEmpty(show.Director))
        {
            sb.AppendLine($"  <director>{EscapeXml(show.Director)}</director>");
        }

        // 演员
        if (show.Actors.Count > 0)
        {
            foreach (var actor in show.Actors)
            {
                sb.AppendLine($"  <actor>");
                sb.AppendLine($"    <name>{EscapeXml(actor)}</name>");
                sb.AppendLine($"  </actor>");
            }
        }

        // 标签
        var tagsToUse = episode.Tags.Count > 0 ? episode.Tags : show.Tags;
        if (tagsToUse.Count > 0)
        {
            foreach (var tag in tagsToUse)
            {
                sb.AppendLine($"  <tag>{EscapeXml(tag)}</tag>");
            }
        }

        sb.AppendLine("</episodedetails>");

        return sb.ToString();
    }

    /// <summary>
    /// 生成简化的 JSON 元数据（用于调试）
    /// </summary>
    public string GenerateJsonMetadata(Show show, List<Episode> episodes)
    {
        var metadata = new
        {
            title = show.Title,
            originalTitle = show.OriginalTitle,
            year = show.Year,
            studio = show.Studio,
            director = show.Director,
            actors = show.Actors,
            tags = show.Tags,
            summary = show.Summary,
            rating = show.Rating,
            season = show.Season,
            episodeCount = episodes.Count,
            episodes = episodes.Select(ep => new
            {
                season = ep.Season,
                number = ep.Number,
                title = ep.Title,
                summary = ep.Summary,
                releaseYear = ep.ReleaseYear
            }).ToList()
        };

        return System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    /// <summary>
    /// XML 字符转义
    /// </summary>
    private string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
