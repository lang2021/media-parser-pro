using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MediaParser.Core.Models;

/// <summary>
/// 图片资源模型
/// </summary>
public class ImageAsset : INotifyPropertyChanged
{
    private string _fullPath = string.Empty;
    private string _fileName = string.Empty;
    private ImageRole _role = ImageRole.Unknown;
    private int _width;
    private int _height;
    private long _fileSize;
    private string _format = string.Empty;
    private int _roleIndex = -1;
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
    /// 图片角色类型
    /// </summary>
    public ImageRole Role
    {
        get => _role;
        set
        {
            if (_role != value)
            {
                _role = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsRoleAssigned));
            }
        }
    }

    /// <summary>
    /// UI 选中的角色索引 (ComboBox 的 SelectedIndex 值)
    /// -1 表示未选择，0 表示第一个角色（Poster），1 表示第二个角色（Fanart）
    /// 此属性独立于 Role，用于 UI 绑定
    /// </summary>
    public int RoleIndex
    {
        get => _roleIndex;
        set
        {
            if (_roleIndex != value)
            {
                _roleIndex = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 图片宽度
    /// </summary>
    public int Width
    {
        get => _width;
        set
        {
            if (_width != value)
            {
                _width = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 图片高度
    /// </summary>
    public int Height
    {
        get => _height;
        set
        {
            if (_height != value)
            {
                _height = value;
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
    /// 图片格式
    /// </summary>
    public string Format
    {
        get => _format;
        set
        {
            if (_format != value)
            {
                _format = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否已标记角色
    /// </summary>
    public bool IsRoleAssigned => Role != ImageRole.Unknown;

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
