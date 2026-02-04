using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using MediaParser.Core.Models;

namespace MediaParser.Core.Modules.ImageManager;

/// <summary>
/// 图片管理结果
/// </summary>
public class ImageManagerResult
{
    public bool Success { get; set; }
    public List<ImageAsset> Images { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// 图片管理器选项
/// </summary>
public class ImageManagerOptions
{
    /// <summary>
    /// 支持的图片扩展名
    /// </summary>
    public string[] SupportedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tbn" };

    /// <summary>
    /// 是否递归扫描子目录
    /// </summary>
    public bool Recursive { get; set; } = false;

    /// <summary>
    /// 文件大小下限（字节）
    /// </summary>
    public long MinFileSize { get; set; } = 1024; // 1KB

    /// <summary>
    /// 最大图片尺寸（用于缩略图生成）
    /// </summary>
    public int MaxDimension { get; set; } = 4096;
}

/// <summary>
/// 图片管理器
/// 
/// 职责：
/// 1. 扫描目录中的图片文件
/// 2. 显示缩略图
/// 3. 标记 poster/fanart 角色
/// 4. 保留原始扩展名
/// </summary>
public class ImageManager : INotifyPropertyChanged
{
    private readonly ImageManagerOptions _options;
    private List<ImageAsset> _images = new();
    private string? _lastScannedDirectory;
    private bool _isScanning;

    /// <summary>
    /// 已扫描的图片列表
    /// </summary>
    public List<ImageAsset> Images
    {
        get => _images;
        private set
        {
            if (_images != value)
            {
                _images = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 最后扫描的目录
    /// </summary>
    public string? LastScannedDirectory
    {
        get => _lastScannedDirectory;
        private set
        {
            if (_lastScannedDirectory != value)
            {
                _lastScannedDirectory = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否正在扫描
    /// </summary>
    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (_isScanning != value)
            {
                _isScanning = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 扫描进度（0-100）
    /// </summary>
    public int ScanProgress { get; private set; }

    /// <summary>
    /// 扫描进度变化事件
    /// </summary>
    public event Action<int, string>? ScanProgressChanged;

    /// <summary>
    /// 扫描完成事件
    /// </summary>
    public event Action<ImageManagerResult>? ScanCompleted;

    /// <summary>
    /// 图片角色变化事件
    /// </summary>
    public event Action<ImageAsset, ImageRole>? ImageRoleChanged;

    /// <summary>
    /// 属性变化事件
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    public ImageManager(ImageManagerOptions? options = null)
    {
        _options = options ?? new ImageManagerOptions();
    }

    /// <summary>
    /// 扫描目录中的图片文件
    /// </summary>
    public ImageManagerResult ScanDirectory(string directoryPath)
    {
        var result = new ImageManagerResult { Success = false };

        if (!Directory.Exists(directoryPath))
        {
            result.Errors.Add($"目录不存在: {directoryPath}");
            return result;
        }

        IsScanning = true;
        ScanProgress = 0;
        LastScannedDirectory = directoryPath;

        try
        {
            var searchOption = _options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var allFiles = Directory.GetFiles(directoryPath, "*.*", searchOption)
                .Where(f => IsImageFile(f))
                .ToList();

            var images = new List<ImageAsset>();
            var totalFiles = allFiles.Count;
            var processedFiles = 0;

            foreach (var filePath in allFiles)
            {
                try
                {
                    var image = CreateImageAsset(filePath);
                    if (image != null)
                    {
                        // 自动识别海报（通常是竖向的）
                        AutoDetectPoster(image);
                        images.Add(image);
                    }

                    processedFiles++;
                    ScanProgress = (int)((double)processedFiles / totalFiles * 100);
                    ScanProgressChanged?.Invoke(ScanProgress, Path.GetFileName(filePath));
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"无法处理文件 {filePath}: {ex.Message}");
                }
            }

            Images = images;
            result.Success = true;
            result.Images = images;

            if (images.Count == 0)
            {
                result.Warnings.Add("未找到任何图片文件");
            }

            ScanCompleted?.Invoke(result);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"扫描目录时出错: {ex.Message}");
        }
        finally
        {
            IsScanning = false;
        }

        return result;
    }

    /// <summary>
    /// 扫描单个图片文件
    /// </summary>
    public ImageAsset? ScanFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        if (!IsImageFile(filePath))
        {
            return null;
        }

        var image = CreateImageAsset(filePath);
        if (image != null)
        {
            AutoDetectPoster(image);
        }
        return image;
    }

    /// <summary>
    /// 检查文件是否为图片文件
    /// </summary>
    public bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
        return _options.SupportedExtensions.Contains(extension);
    }

    /// <summary>
    /// 创建 ImageAsset 对象
    /// </summary>
    private ImageAsset? CreateImageAsset(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);

            // 过滤太小的文件
            if (fileInfo.Length < _options.MinFileSize)
            {
                return null;
            }

            var image = new ImageAsset
            {
                FullPath = filePath,
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                Format = fileInfo.Extension.TrimStart('.').ToUpperInvariant()
            };

            // 获取图片尺寸
            try
            {
                using var img = Image.FromFile(filePath);
                image.Width = img.Width;
                image.Height = img.Height;
            }
            catch
            {
                // 如果无法读取尺寸，使用默认值
                image.Width = 0;
                image.Height = 0;
            }

            return image;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 自动检测海报（基于宽高比）
    /// </summary>
    private void AutoDetectPoster(ImageAsset image)
    {
        // 如果宽高比小于 0.7，认为是海报（竖向）
        if (image.Width > 0 && image.Height > 0)
        {
            var ratio = (double)image.Width / image.Height;
            if (ratio < 0.7)
            {
                image.Role = ImageRole.Poster;
            }
        }
    }

    /// <summary>
    /// 设置图片角色
    /// </summary>
    public bool SetImageRole(ImageAsset image, ImageRole role)
    {
        if (!Images.Contains(image))
        {
            return false;
        }

        var oldRole = image.Role;
        image.Role = role;
        OnPropertyChanged(nameof(Images));
        ImageRoleChanged?.Invoke(image, role);

        return true;
    }

    /// <summary>
    /// 批量设置角色
    /// </summary>
    public int SetRoleForAll(ImageRole fromRole, ImageRole toRole)
    {
        var count = 0;
        foreach (var image in Images)
        {
            if (image.Role == fromRole)
            {
                image.Role = toRole;
                count++;
                ImageRoleChanged?.Invoke(image, toRole);
            }
        }
        if (count > 0)
        {
            OnPropertyChanged(nameof(Images));
        }
        return count;
    }

    /// <summary>
    /// 清除所有图片的角色标记
    /// </summary>
    public void ClearAllRoles()
    {
        foreach (var image in Images)
        {
            image.Role = ImageRole.Unknown;
        }
        OnPropertyChanged(nameof(Images));
    }

    /// <summary>
    /// 获取指定角色的所有图片
    /// </summary>
    public List<ImageAsset> GetImagesByRole(ImageRole role)
    {
        return Images.Where(img => img.Role == role).ToList();
    }

    /// <summary>
    /// 获取海报图片
    /// </summary>
    public ImageAsset? GetPoster()
    {
        return Images.FirstOrDefault(img => img.Role == ImageRole.Poster);
    }

    /// <summary>
    /// 获取背景图
    /// </summary>
    public ImageAsset? GetFanart()
    {
        return Images.FirstOrDefault(img => img.Role == ImageRole.Fanart);
    }

    /// <summary>
    /// 获取未标记角色的图片
    /// </summary>
    public List<ImageAsset> GetUnassignedImages()
    {
        return Images.Where(img => !img.IsRoleAssigned).ToList();
    }

    /// <summary>
    /// 获取图片统计信息
    /// </summary>
    public Dictionary<ImageRole, int> GetImageStats()
    {
        return Images
            .GroupBy(img => img.Role)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// 移除图片（从列表中删除）
    /// </summary>
    public bool RemoveImage(ImageAsset image)
    {
        if (!Images.Contains(image))
        {
            return false;
        }

        Images.Remove(image);
        OnPropertyChanged(nameof(Images));
        return true;
    }

    /// <summary>
    /// 添加图片到列表
    /// </summary>
    public bool AddImage(ImageAsset image)
    {
        if (Images.Contains(image))
        {
            return false;
        }

        Images.Add(image);
        OnPropertyChanged(nameof(Images));
        return true;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
