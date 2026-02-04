---
name: MediaParser Pro - WPF 桌面应用开发计划
overview: 基于 PRD 文档，使用 WPF + .NET 10 开发 MediaParser Pro 桌面应用。架构分为 Core（核心类库）、WPF（UI 层）、CLI（命令行入口）三层，状态机是核心决策者。
todos:
  - id: create_solution
    content: 创建解决方案结构（Core / WPF / CLI 项目）
    status: completed
  - id: data_models
    content: 实现数据模型（Show / Episode / VideoFile / ImageAsset）
    status: completed
  - id: workflow_controller
    content: 实现 WorkflowController 状态机（含元数据/视频/图片校验逻辑）
    status: completed
  - id: metadata_parser
    content: 实现 MetadataParser 模块
    status: completed
  - id: media_validator
    content: 实现 MediaValidator 模块
    status: completed
  - id: image_manager
    content: 实现 ImageManager 模块
    status: completed
  - id: archive_processor
    content: 实现 ArchiveProcessor 模块
    status: completed
  - id: cli_interface
    content: 实现 CLI 接口
    status: completed
  - id: wpf_ui
    content: 实现 WPF UI（ViewModels + Views）
    status: completed
  - id: video_preview
    content: 集成 LibVLCSharp 视频预览
    status: completed
  - id: integration_test
    content: 端到端测试与错误处理
    status: completed
---

# MediaParser Pro - WPF 桌面应用开发计划

## 技术栈确认

- **.NET 版本**：.NET 10（您当前环境）
- **UI 框架**：WPF（MVVM 模式）
- **视频播放**：LibVLCSharp
- **项目结构**：Core / WPF / CLI 三层分离

## 解决方案结构

```
MediaParserPro/
├── src/
│   ├── MediaParser.Core/          # 核心类库（无 UI 依赖）
│   │   ├── Models/                # 数据模型
│   │   ├── Modules/
│   │   │   ├── MetadataParser/
│   │   │   ├── MediaValidator/
│   │   │   ├── ImageManager/
│   │   │   └── ArchiveProcessor/
│   │   └── Workflow/
│   │       └── WorkflowController.cs
│   │
│   ├── MediaParser.WPF/           # WPF UI 项目
│   │   ├── Views/
│   │   ├── ViewModels/
│   │   ├── Converters/
│   │   └── App.xaml
│   │
│   └── MediaParser.CLI/           # CLI 入口项目
│       └── Program.cs
│
└── MediaParserPro.sln
```

## 核心约束（强制遵守）

### WorkflowController 校验逻辑

**Phase 1 的 WorkflowController 必须包含完整的校验逻辑**，而非仅状态枚举。校验规则：

| 校验项 | 校验内容 | 不通过时的处理 |

|--------|----------|----------------|

| **元数据有效性** | Title 非空、Season > 0、Episodes 列表非空 | 状态 = Draft |

| **视频映射完整性** | 每一集都映射到一个视频文件 | 状态 = Draft |

| **图片角色标记** | 至少存在一张 poster 或 fanart | 状态 = Draft（警告但允许） |

状态切换规则：

- 任一模块数据变化 → 状态 = Draft
- 全部校验通过 → 状态 = Ready
- 归档成功 → 状态 = Archived

### UI 可用性绑定规则

**Phase 4 实现 UI 时必须遵守**：

- UI 按钮的 `IsEnabled` 属性必须绑定到 `WorkflowController.CurrentState`
- ViewModel 不得自行实现业务判断逻辑
- 示例：`StartArchiveButton.IsEnabled = {Binding WorkflowState, Converter={StaticResource ReadyStateToBoolConverter}}`

## 开发阶段

### Phase 1：核心模型与状态机（1-2周）

**目标**：建立数据模型与状态机，状态机包含完整校验逻辑

数据模型：

- [ ] `MediaParser.Core/Models/Show.cs`
- [ ] `MediaParser.Core/Models/Episode.cs`
- [ ] `MediaParser.Core/Models/VideoFile.cs`
- [ ] `MediaParser.Core/Models/ImageAsset.cs`

状态机（核心）：

- [ ] `MediaParser.Core/Workflow/WorkflowState.cs` - 状态枚举
- [ ] `MediaParser.Core/Workflow/WorkflowController.cs` - 状态机 + 校验逻辑
  - `ValidateMetadata()` - 元数据有效性校验
  - `ValidateVideoMapping()` - 视频映射完整性校验
  - `ValidateImageAssets()` - 图片角色标记校验
  - `TryTransitionToReady()` - 尝试进入 Ready 状态
  - `TryArchive()` - 执行归档

### Phase 2：核心模块实现（3-5周）

- [ ] `MetadataParser.cs` + `RegexTemplates.cs`
- [ ] `MediaValidator.cs`
- [ ] `ImageManager.cs`
- [ ] `ArchiveProcessor.cs` + `NfoGenerator.cs`

### Phase 3：CLI 接口（6周）

- [ ] `MediaParser.CLI/Program.cs`

### Phase 4：WPF UI 与 MVVM 绑定（7-9周）

- [ ] ViewModels（MainViewModel, MetadataViewModel 等）
- [ ] Views（资源选择页、解析匹配页）
- [ ] **UI 可用性绑定**：按钮 `IsEnabled` 绑定 `WorkflowState`
- [ ] VideoPlayer.xaml（LibVLCSharp 集成）

### Phase 5：集成与测试（10周）

- [ ] 端到端流程测试
- [ ] 状态机边界测试
- [ ] IO 错误处理测试

## UI 参考

- `desktop-app-ui/` 目录保留，作为 UI 交互原型参考（Next.js）

## 禁止事项

- 禁止添加网络刮削、云同步、账号系统等 PRD 未定义功能
- 禁止在 UI 或 ViewModel 中实现业务判断逻辑
- 禁止绕过 WorkflowController 直接决策流程