namespace MediaParser.Core.Models;

/// <summary>
/// 视频文件模型
/// </summary>
public class VideoFile
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
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 文件扩展名
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// 映射的集数（可选，手动映射时使用）
    /// </summary>
    public int? MappedEpisodeNumber { get; set; }

    /// <summary>
    /// 是否已映射到集数
    /// </summary>
    public bool IsMapped => MappedEpisodeNumber.HasValue;

    /// <summary>
    /// 视频时长（秒）
    /// </summary>
    public long DurationSeconds { get; set; }

    /// <summary>
    /// 视频分辨率
    /// </summary>
    public string Resolution { get; set; } = string.Empty;

    /// <summary>
    /// 视频编解码器
    /// </summary>
    public string Codec { get; set; } = string.Empty;

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
    /// 获取不含扩展名的文件名
    /// </summary>
    public string GetBaseName() => System.IO.Path.GetFileNameWithoutExtension(FileName);

    /// <summary>
    /// 尝试从文件名推断集数
    /// </summary>
    public bool TryInferEpisodeNumber(out int episodeNumber)
    {
        episodeNumber = 0;
        
        // 常见模式: S01E05, 05, [05], -05, 第05话
        var patterns = new[]
        {
            @"S(\d+)E(\d+)",    // S01E05
            @"\[(\d+)\]",        // [05]
            @"[-_](\d{2,3})",    // -05, _12
            @"第(\d+)话",         // 第05话
            @"第(\d+)集",         // 第05集
            @"\((\d+)\)",        // (05)
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(FileName, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                // 如果是 S01E05 格式，取第二个捕获组
                if (pattern.Contains("S(\\d+)E(\\d+)") && match.Groups.Count > 2)
                {
                    if (int.TryParse(match.Groups[2].Value, out episodeNumber))
                        return true;
                }
                else if (int.TryParse(match.Groups[1].Value, out episodeNumber))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
