using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MediaParser.Core.Models;

/// <summary>
/// 视频文件模型
/// </summary>
public class VideoFile : INotifyPropertyChanged
{
    private string _fullPath = string.Empty;
    private string _fileName = string.Empty;
    private long _fileSize;
    private string _extension = string.Empty;
    private int? _mappedEpisodeNumber;
    private long _durationSeconds;
    private string _resolution = string.Empty;
    private string _codec = string.Empty;
    private int _episodeIndex = -1;
    private bool _isSelected;
    private bool _isChecked;

    /// <summary>
    /// 属性变更事件
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 触发属性变更通知
    /// </summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 完整路径
    /// </summary>
    public string FullPath
    {
        get => _fullPath;
        set
        {
            if (_fullPath != value)
            {
                _fullPath = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName
    {
        get => _fileName;
        set
        {
            if (_fileName != value)
            {
                _fileName = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize
    {
        get => _fileSize;
        set
        {
            if (_fileSize != value)
            {
                _fileSize = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 文件扩展名
    /// </summary>
    public string Extension
    {
        get => _extension;
        set
        {
            if (_extension != value)
            {
                _extension = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 映射的集数（可选，手动映射时使用）
    /// </summary>
    public int? MappedEpisodeNumber
    {
        get => _mappedEpisodeNumber;
        set
        {
            if (_mappedEpisodeNumber != value)
            {
                _mappedEpisodeNumber = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMapped));
            }
        }
    }

    /// <summary>
    /// 是否已映射到集数
    /// </summary>
    public bool IsMapped => MappedEpisodeNumber.HasValue;

    /// <summary>
    /// UI 选中的剧集索引 (ComboBox 的 SelectedIndex 值)
    /// -1 表示未选择，0 表示第一个剧集，以此类推
    /// 此属性独立于 MappedEpisodeNumber，用于 UI 绑定
    /// </summary>
    public int EpisodeIndex
    {
        get => _episodeIndex;
        set
        {
            if (_episodeIndex != value)
            {
                _episodeIndex = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 视频时长（秒）
    /// </summary>
    public long DurationSeconds
    {
        get => _durationSeconds;
        set
        {
            if (_durationSeconds != value)
            {
                _durationSeconds = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 视频分辨率
    /// </summary>
    public string Resolution
    {
        get => _resolution;
        set
        {
            if (_resolution != value)
            {
                _resolution = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 视频编解码器
    /// </summary>
    public string Codec
    {
        get => _codec;
        set
        {
            if (_codec != value)
            {
                _codec = value;
                OnPropertyChanged();
            }
        }
    }

    // ==================== UI 状态属性 ====================

    /// <summary>
    /// 是否被选中（用于 UI）
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否被勾选（用于 UI）
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }
    }

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
