using System.Text;
using System.Xml;
using MediaParser.Core.Models;

namespace MediaParser.Core.Modules.ArchiveProcessor;

/// <summary>
/// 归档结果
/// </summary>
public class ArchiveResult
{
    public bool Success { get; set; }
    public string? OutputDirectory { get; set; }
    public List<string> CreatedFiles { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public long TotalBytesWritten { get; set; }
}

/// <summary>
/// 归档处理器选项
/// </summary>
public class ArchiveProcessorOptions
{
    /// <summary>
    /// 输出根目录
    /// </summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 是否使用季文件夹
    /// </summary>
    public bool UseSeasonFolder { get; set; } = true;

    /// <summary>
    /// 是否保留原始文件名
    /// </summary>
    public bool PreserveOriginalFilename { get; set; } = true;

    /// <summary>
    /// NFO 文件格式（kodi/emby/jellyfin）
    /// </summary>
    public string NfoFormat { get; set; } = "kodi";

    /// <summary>
    /// 是否生成缩略图
    /// </summary>
    public bool GenerateThumbnails { get; set; } = false;

    /// <summary>
    /// 视频文件命名模板
    /// </summary>
    public string VideoFileNamingTemplate { get; set; } = "{Title} - S{Season:00}E{Episode:00}{Extension}";

    /// <summary>
    /// 是否使用 UTF-8 BOM
    /// </summary>
    public bool UseUtf8Bom { get; set; } = false;
}

/// <summary>
/// 归档处理器
/// 
/// 职责：
/// 1. 生成目录结构
/// 2. 生成 NFO 文件（tvshow.nfo, episode.nfo）
/// 3. 复制视频文件和图片文件
/// 4. 符合 Kodi/Emby 规范
/// </summary>
public class ArchiveProcessor
{
    private readonly ArchiveProcessorOptions _options;
    private readonly NfoGenerator _nfoGenerator;

    public ArchiveProcessor(ArchiveProcessorOptions? options = null)
    {
        _options = options ?? new ArchiveProcessorOptions();
        _nfoGenerator = new NfoGenerator();
    }

    /// <summary>
    /// 执行归档操作
    /// </summary>
    public ArchiveResult Archive(Show show, List<VideoFile> videos, List<ImageAsset> images, string? customOutputDirectory = null)
    {
        var result = new ArchiveResult { Success = false };

        if (show == null)
        {
            result.Errors.Add("剧集元数据为空");
            return result;
        }

        if (string.IsNullOrEmpty(show.Title))
        {
            result.Errors.Add("剧集标题为空");
            return result;
        }

        try
        {
            // 确定输出目录（优先使用自定义目录）
            var outputDir = DetermineOutputDirectory(show, customOutputDirectory);
            result.OutputDirectory = outputDir;

            // 创建目录结构
            CreateDirectoryStructure(outputDir, show);

            // 生成并写入 tvshow.nfo
            var tvshowNfo = _nfoGenerator.GenerateTvshowNfo(show);
            var tvshowPath = Path.Combine(outputDir, "tvshow.nfo");
            WriteNfoFile(tvshowPath, tvshowNfo);
            result.CreatedFiles.Add(tvshowPath);

            // 获取季文件夹路径
            var seasonDir = GetSeasonDirectory(outputDir, show);

            // ========== 步骤 1: 生成所有剧集的 NFO ==========
            foreach (var episode in show.Episodes)
            {
                // 生成 episode.nfo（命名为 S01E01.nfo 格式）
                var episodeKey = $"S{show.Season:D2}E{episode.Number:D2}";
                var episodeNfo = _nfoGenerator.GenerateEpisodeNfo(show, episode);
                var episodeNfoPath = Path.Combine(seasonDir, $"{episodeKey}.nfo");
                WriteNfoFile(episodeNfoPath, episodeNfo);
                result.CreatedFiles.Add(episodeNfoPath);
            }

            // ========== 步骤 2: 只处理已映射的视频（每个视频只处理一次）==========
            var processedVideos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var video in videos)
            {
                // 跳过未映射到剧集的视频
                if (video.MappedEpisodeNumber <= 0)
                {
                    result.Warnings.Add($"视频未映射到剧集，已跳过: {video.FileName}");
                    continue;
                }

                // 检查是否已处理过（避免重复）
                if (processedVideos.Contains(video.FullPath))
                {
                    result.Warnings.Add($"视频已处理过，已跳过: {video.FileName}");
                    continue;
                }

                // 找到对应的剧集
                var episode = show.Episodes.FirstOrDefault(e => 
                    e.Season == show.Season && e.Number == video.MappedEpisodeNumber);
                
                if (episode == null)
                {
                    result.Warnings.Add($"未找到剧集 S{show.Season:D2}E{video.MappedEpisodeNumber:D2}，已跳过: {video.FileName}");
                    continue;
                }

                // 检查源文件是否存在
                if (!File.Exists(video.FullPath))
                {
                    result.Errors.Add($"视频文件不存在: {video.FullPath}");
                    continue;
                }

                try
                {
                    // 生成目标文件名
                    var episodeKey = $"S{show.Season:D2}E{episode.Number:D2}";
                    var extension = video.Extension.TrimStart('.');
                    var destFileName = $"{episodeKey}.{extension}";
                    var destPath = Path.Combine(seasonDir, destFileName);

                    // 复制文件
                    File.Copy(video.FullPath, destPath, overwrite: true);
                    processedVideos.Add(video.FullPath);
                    result.CreatedFiles.Add(destPath);
                    result.TotalBytesWritten += new FileInfo(destPath).Length;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"复制视频文件失败 {video.FullPath}: {ex.Message}");
                }
            }

            // ========== 步骤 3: 复制图片文件（只处理已分配角色的图片）==========
            foreach (var image in images)
            {
                // 跳过未分配角色的图片
                if (image.Role == ImageRole.Unknown)
                {
                    result.Warnings.Add($"图片未分配角色，已跳过: {image.FileName}");
                    continue;
                }

                if (!File.Exists(image.FullPath))
                {
                    result.Warnings.Add($"图片文件不存在: {image.FullPath}");
                    continue;
                }

                var destPath = GetImageDestinationPath(outputDir, image);
                try
                {
                    var directory = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.Copy(image.FullPath, destPath, overwrite: true);
                    result.CreatedFiles.Add(destPath);
                    result.TotalBytesWritten += new FileInfo(destPath).Length;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"复制图片文件失败 {image.FullPath}: {ex.Message}");
                }
            }

            result.Success = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"归档过程发生错误: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 确定输出目录
    /// </summary>
    private string DetermineOutputDirectory(Show show, string? customOutputDirectory = null)
    {
        // 如果有自定义输出目录，直接使用
        if (!string.IsNullOrEmpty(customOutputDirectory))
        {
            var sanitizedTitle = SanitizeFolderName(show.Title);
            if (show.Year > 0)
            {
                sanitizedTitle = $"{sanitizedTitle} ({show.Year})";
            }
            return Path.Combine(customOutputDirectory, sanitizedTitle);
        }

        var baseDir = string.IsNullOrEmpty(_options.OutputDirectory)
            ? Path.GetDirectoryName(show.Episodes.FirstOrDefault()?.MappedVideo?.FullPath ?? "")
            : _options.OutputDirectory;

        if (string.IsNullOrEmpty(baseDir))
        {
            baseDir = Environment.CurrentDirectory;
        }

        // 生成符合规范的目录名
        var folderName = SanitizeFolderName(show.Title);
        if (show.Year > 0)
        {
            folderName = $"{folderName} ({show.Year})";
        }

        return Path.Combine(baseDir, folderName);
    }

    /// <summary>
    /// 创建目录结构
    /// </summary>
    private void CreateDirectoryStructure(string rootDir, Show show)
    {
        // 创建根目录
        if (!Directory.Exists(rootDir))
        {
            Directory.CreateDirectory(rootDir);
        }

        // 创建季文件夹（如果需要）
        if (_options.UseSeasonFolder && show.Season > 0)
        {
            var seasonDir = Path.Combine(rootDir, $"Season {show.Season:D2}");
            if (!Directory.Exists(seasonDir))
            {
                Directory.CreateDirectory(seasonDir);
            }
        }
    }

    /// <summary>
    /// 获取季文件夹路径
    /// </summary>
    private string GetSeasonDirectory(string rootDir, Show show)
    {
        if (_options.UseSeasonFolder)
        {
            return Path.Combine(rootDir, $"Season {show.Season:D2}");
        }
        return rootDir;
    }

    /// <summary>
    /// 获取图片目标路径
    /// 规则：所有图片直接放在根目录（和 tvshow.nfo 同级）
    /// poster → poster.jpg, fanart → fanart.jpg
    /// </summary>
    private string GetImageDestinationPath(string rootDir, ImageAsset image)
    {
        // 根据角色确定目标文件名，直接放在根目录
        return image.Role switch
        {
            ImageRole.Poster => Path.Combine(rootDir, "poster.jpg"),
            ImageRole.Fanart => Path.Combine(rootDir, "fanart.jpg"),
            ImageRole.Thumb => Path.Combine(rootDir, image.FileName),
            ImageRole.Still => Path.Combine(rootDir, image.FileName),
            ImageRole.Banner => Path.Combine(rootDir, image.FileName),
            ImageRole.Character => Path.Combine(rootDir, image.FileName),
            ImageRole.Logo => Path.Combine(rootDir, image.FileName),
            _ => Path.Combine(rootDir, image.FileName)
        };
    }

    /// <summary>
    /// 写入 NFO 文件（UTF-8 无 BOM）
    /// </summary>
    private void WriteNfoFile(string filePath, string content)
    {
        var encoding = _options.UseUtf8Bom ? Encoding.UTF8 : new UTF8Encoding(false);
        File.WriteAllText(filePath, content, encoding);
    }

    /// <summary>
    /// 清理文件夹名称（移除非法字符）
    /// </summary>
    private string SanitizeFolderName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Unknown";

        var invalidChars = Path.GetInvalidFileNameChars()
            .Where(c => c != '.' && c != ' ' && c != '-' && c != '_')
            .ToArray();

        var sanitized = invalidChars.Aggregate(name, (current, c) => current.Replace(c, '_'));

        // 限制长度
        if (sanitized.Length > 100)
        {
            sanitized = sanitized[..100];
        }

        return sanitized.Trim();
    }
}
