using System.Text.Json;
using MediaParser.Core.Models;
using MediaParser.Core.Modules.ArchiveProcessor;
using MediaParser.Core.Modules.ImageManager;
using MediaParser.Core.Modules.MediaValidator;
using MediaParser.Core.Modules.MetadataParser;
using MediaParser.Core.Workflow;

namespace MediaParser.CLI;

/// <summary>
/// CLI 程序入口
/// 
/// 功能：
/// 1. 支持命令行参数解析
/// 2. 调用 Core 模块处理
/// 3. 输出 JSON 格式结果
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // 解析命令行参数
        var metadata = GetArgument(args, "--metadata", "-m") ?? "";
        var directory = GetArgument(args, "--directory", "-d") ?? "";
        var output = GetArgument(args, "--output", "-o") ?? "";
        var autoMap = HasArgument(args, "--auto-map", "-a");
        var verbose = HasArgument(args, "--verbose", "-v");
        var help = HasArgument(args, "--help", "-h", "-?");

        if (help)
        {
            PrintHelp();
            return 0;
        }

        await ExecuteAsync(metadata, directory, output, autoMap, verbose);
        return 0;
    }

    private static string? GetArgument(string[] args, string longName, string shortName)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == longName || args[i] == shortName)
            {
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                {
                    return args[i + 1];
                }
            }
            else if (args[i].StartsWith(longName + "=") || args[i].StartsWith(shortName + "="))
            {
                var parts = args[i].Split('=', 2);
                return parts.Length > 1 ? parts[1] : null;
            }
        }
        return null;
    }

    private static bool HasArgument(string[] args, string longName, string shortName, string altShort = "")
    {
        return args.Any(a => a == longName || a == shortName || a == altShort);
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"
MediaParser Pro - 媒体文件归档工具

用法: MediaParser.CLI [选项]

选项:
  -m, --metadata <文本或文件>  元数据文本或文件路径
  -d, --directory <目录>       包含媒体文件的目录路径
  -o, --output <目录>          输出目录路径
  -a, --auto-map               自动映射视频文件到集数
  -v, --verbose                显示详细输出
  -h, --help                   显示此帮助信息

示例:
  MediaParser.CLI --metadata ""标题|原名|2024|制作商|导演|演员|标签|1-12"" --directory ""C:\Videos"" --output ""D:\Output""
  MediaParser.CLI -m metadata.txt -d /path/to/videos -o /path/to/output -a
");
    }

    private static async Task ExecuteAsync(string metadata, string directory, string output, bool autoMap, bool verbose)
    {
        var result = new CliResult { Success = false };

        try
        {
            // 1. 解析元数据
            if (!string.IsNullOrEmpty(metadata))
            {
                var metadataText = ReadMetadata(metadata);
                var parser = new MetadataParser();
                var parseResult = parser.Parse(metadataText);

                if (!parseResult.Success)
                {
                    result.Errors.AddRange(parseResult.Errors);
                    OutputJson(result);
                    return;
                }

                result.Show = parseResult.Show;
                result.Episodes = parseResult.Episodes;
            }

            // 2. 扫描视频文件
            if (!string.IsNullOrEmpty(directory))
            {
                var validator = new MediaValidator(new MediaValidatorOptions { AutoInferEpisode = autoMap });
                var videoResult = validator.ScanDirectory(directory);

                if (!videoResult.Success)
                {
                    result.Errors.AddRange(videoResult.Errors);
                }

                result.Videos = videoResult.Videos;

                // 3. 扫描图片文件
                var imageManager = new ImageManager();
                var imageResult = imageManager.ScanDirectory(directory);
                result.Images = imageResult.Images;
            }

            // 4. 初始化工作流控制器
            var controller = new WorkflowController();

            if (result.Show != null)
            {
                controller.Show = result.Show;
            }

            if (result.Videos != null)
            {
                controller.Videos = result.Videos;
            }

            if (result.Images != null)
            {
                controller.Images = result.Images;
            }

            // 5. 尝试转换到 Ready 状态
            var canArchive = controller.TryTransitionToReady();
            result.ValidationSummary = controller.ValidationSummary;
            result.CanArchive = canArchive;
            result.State = controller.CurrentState.ToString();

            // 6. 执行归档（如果需要）
            if (canArchive && !string.IsNullOrEmpty(output))
            {
                var archiveOptions = new ArchiveProcessorOptions { OutputDirectory = output };
                var archiveProcessor = new ArchiveProcessor(archiveOptions);
                var archiveResult = archiveProcessor.Archive(result.Show!, result.Videos!, result.Images!);

                result.ArchiveResult = new ArchiveResult
                {
                    Success = archiveResult.Success,
                    OutputDirectory = archiveResult.OutputDirectory,
                    CreatedFilesCount = archiveResult.CreatedFiles.Count,
                    TotalBytesWritten = archiveResult.TotalBytesWritten
                };

                result.Success = archiveResult.Success;

                if (!archiveResult.Success)
                {
                    result.Errors.AddRange(archiveResult.Errors);
                }
                else
                {
                    result.Message = $"归档成功！输出目录: {archiveResult.OutputDirectory}";
                }
            }
            else if (canArchive)
            {
                result.Message = "数据校验通过，可以使用 --output 参数执行归档";
            }
            else
            {
                result.Errors.Add(controller.LastErrorMessage);
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"执行时出错: {ex.Message}");

            if (verbose)
            {
                result.Errors.Add(ex.StackTrace ?? "");
            }
        }

        OutputJson(result);
    }

    private static string ReadMetadata(string input)
    {
        if (File.Exists(input))
        {
            return File.ReadAllText(input);
        }

        return input;
    }

    private static void OutputJson(CliResult result)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(result, options);
        Console.WriteLine(json);
    }
}

/// <summary>
/// CLI 输出结果
/// </summary>
public class CliResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? State { get; set; }
    public bool CanArchive { get; set; }
    public Show? Show { get; set; }
    public List<Episode> Episodes { get; set; } = new();
    public List<VideoFile> Videos { get; set; } = new();
    public List<ImageAsset> Images { get; set; } = new();
    public ValidationSummary? ValidationSummary { get; set; }
    public ArchiveResult? ArchiveResult { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// 归档结果（CLI 版本）
/// </summary>
public class ArchiveResult
{
    public bool Success { get; set; }
    public string? OutputDirectory { get; set; }
    public int CreatedFilesCount { get; set; }
    public long TotalBytesWritten { get; set; }
}
