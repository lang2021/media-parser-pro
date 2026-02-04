# MediaParser Pro

MediaParser Pro 是一款用于 **动漫 / 剧集元数据解析与归档** 的工具。
它可以解析 NFO 文件，匹配本地视频与图片资源，并自动生成符合 **Kodi / Emby / Jellyfin** 规范的媒体库目录结构。

> ⚠️ **项目状态：开发中（Preview）**
> 当前已实现 NFO 解析与基础归档流程，功能仍在持续完善，可能存在不稳定情况。

---

## Features | 功能特性

### 元数据解析（Metadata Parsing）

* 支持解析 Kodi / MediaCenter 标准 NFO 文件
* 自动提取剧集信息：标题、原标题、年份、剧情、演员、标签等
* 支持 `<tvshow>` 与 `<episodedetails>` 混合格式
* 可处理包含描述性文本的非标准 NFO 内容

### 视频与图片管理（Video & Image Management）

* 支持将视频文件手动映射到剧集条目
* 支持为图片分配角色（Poster、Fanart、Thumb、Banner 等）
* 支持批量处理多个视频与图片文件

### 归档与生成（Archiving）

* 自动生成符合 Kodi / Emby / Jellyfin 规范的目录结构
* 自动生成 NFO 文件：

  * `tvshow.nfo`：剧集元数据
  * `S01E01.nfo`：单集元数据（零填充格式）
* 视频文件自动重命名为：`S01E01.mp4`
* 图片文件自动规范命名：`poster.jpg`、`fanart.jpg` 等
* 支持仅归档已分配的视频与图片资源

---

## Directory Structure | 输出目录结构

生成的媒体库结构示例：

```
Show Title (Year)/
├── tvshow.nfo
├── poster.jpg
├── fanart.jpg
└── Season 01/
    ├── S01E01.nfo
    ├── S01E01.mp4
    ├── S01E02.nfo
    └── S01E02.mp4
```

---

## NFO Format | NFO 示例

### tvshow.nfo

```xml
<?xml version="1.0" encoding="utf-8"?>
<tvshow>
  <title>剧集标题</title>
  <originaltitle>Original Title</originaltitle>
  <year>2024</year>
  <studio>制作商</studio>
  <director>导演</director>
  <actor>
    <name>演员名 (CV/角色)</name>
  </actor>
  <tag>标签1</tag>
  <tag>标签2</tag>
  <plot>剧情简介</plot>
</tvshow>
```

### episode.nfo

```xml
<?xml version="1.0" encoding="utf-8"?>
<episodedetails>
  <title>第01话</title>
  <season>1</season>
  <episode>1</episode>
  <showtitle>剧集标题</showtitle>
  <originaltitle>Original Title</originaltitle>
  <aired>2024-09-06</aired>
  <plot>单集剧情</plot>
  <director>导演</director>
  <actor>
    <name>演员名</name>
  </actor>
  <tag>标签</tag>
</episodedetails>
```

---

## Project Structure | 项目结构

```
MediaParserPro/
├── src/
│   ├── MediaParser.Core/
│   │   ├── Models/
│   │   ├── Modules/
│   │   │   ├── MetadataParser/
│   │   │   └── ArchiveProcessor/
│   │   └── Workflow/
│   │
│   ├── MediaParser.WPF/
│   │   ├── ViewModels/
│   │   ├── ParseMatchWindow.xaml
│   │   └── Converters/
│   │
│   └── MediaParser.CLI/
│       └── Program.cs
│
├── MediaParserPro.sln
└── README.md
```

---

## Tech Stack | 技术栈

* **.NET 10.0 (Windows)**
* **C# 12**
* **WPF + MVVM**
* **XDocument**（XML 解析）
* **Ookii.Dialogs.Wpf**（文件夹选择）

---

## Getting Started | 快速开始

### 环境要求

* .NET 10 SDK 或更高版本
* Windows 10 / 11

### 编译与运行

```bash
dotnet build
dotnet run --project src/MediaParser.WPF
```

### 使用流程

1. 加载 NFO 文件
2. 查看并确认解析结果
3. 分配视频文件到对应剧集
4. 为图片分配 Poster、Fanart 等角色
5. 选择输出目录并执行归档

---

## API Reference | API 示例

### NfoParser

```csharp
var parser = new NfoParser();
var result = parser.Parse(nfoText);
```

### ArchiveProcessor

```csharp
var processor = new ArchiveProcessor();
var result = processor.Archive(show, videos, images, outputDirectory);
```

## License

MIT License

---

## Contributing

欢迎提交 Issue 与 Pull Request。

---

## Contact

如有问题或建议，请通过 GitHub Issues 反馈。
