# 深深桌宠（Shenshen Pet）

一个非官方、非商用的双模式桌宠项目：既可以作为 Windows 独立桌宠运行，也可以安装为 ChatGPT/Codex 桌面端的自定义宠物。

> 本项目与 OpenAI 没有隶属、合作或背书关系。角色素材已获得原作者的非商业使用许可；商业使用需要另行取得书面授权。

![全部状态](previews/all-states.gif)

![观察方向](previews/look-loop.gif)

## 两种模式

| 模式 | 运行宿主 | 用途 |
| --- | --- | --- |
| Windows 独立桌宠 | `ShenshenPet.exe` | 双击运行、拖动、点击互动、自动散步、鼠标方向追踪、托盘菜单 |
| Codex 自定义桌宠 | ChatGPT/Codex 桌面端 | 自动响应 Running、Needs input、Ready、Blocked 等任务状态 |

两种模式共用 `pet/pet.manifest.json` 和同一套 8×11 动画语义，避免状态定义分叉。OpenAI Docs 中的 [Pets 文档](https://learn.chatgpt.com/docs/pets)介绍了桌面宠物的选择、唤醒和任务状态行为。

## 直接使用

### Windows 独立版

从 GitHub Releases 下载 `ShenshenPet-Windows-x64.zip`，解压后双击 `ShenshenPet.exe`。

请先完整解压 ZIP，不要直接在压缩包预览窗口中运行。独立版已包含 .NET 运行时，不需要另外安装 SDK；程序只允许启动一个实例。

- 单击：跳跃
- 拖动：移动桌宠
- 右键：招手、预览状态、缩放、置顶、开机启动、安装到 Codex、退出
- 双击托盘图标：重新显示桌宠

### Codex 专用版

从 Releases 下载 `Shenshen-Codex-Pet.zip`，解压后运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\install-codex-pet.ps1
```

也可以从源码目录安装：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install-codex-pet.ps1
```

安装完成后，在 ChatGPT/Codex 桌面端打开 **设置 > Pets**，选择 **Refresh** 和“深深”，再输入 `/pet` 唤醒。安装位置默认是：

```text
%USERPROFILE%\.codex\pets\shenshen\
├─ pet.json
└─ spritesheet.webp
```

如果设置了 `CODEX_HOME`，安装器会使用该目录。

### 常见问题

- 关闭透明窗口会把桌宠隐藏到系统托盘；需要彻底结束时，请在右键菜单或托盘菜单中选择“退出”。
- 如果 Windows 显示 SmartScreen 提示，这是因为当前发布包尚未使用商业代码签名证书，不代表检测到了恶意代码；可先核对 Release 中的文件来源。
- 如果桌宠提示资源缺失，请重新完整解压发布 ZIP。无法恢复的运行错误会记录到 `%LOCALAPPDATA%\ShenshenPet\crash.log`，便于提交 Issue 时定位。

## 从源码构建

要求：

- Windows 10 或更高版本
- .NET 10 SDK
- Python 3.11+ 与 `requirements.txt` 中的 Pillow（仅用于素材验证/重建 Codex WebP）

```powershell
python -m pip install -r requirements.txt
python scripts\build_layout_guides.py
python scripts\build_codex_package.py
python scripts\verify_sprite.py
python scripts\verify_metadata.py
dotnet build ShenshenPet.sln --configuration Release
dotnet run --project tests\ShenshenPet.Core.Tests --configuration Release
```

生成两个发布 ZIP：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-release.ps1
```

输出位于 `dist/`。

## 动画合同

- 精灵表：`1536 × 2288`、RGBA、透明背景
- 网格：`8 列 × 11 行`
- 单格：`192 × 208`
- Codex 版本：`spriteVersionNumber: 2`
- 标准行：`idle`、`running-right`、`running-left`、`waving`、`jumping`、`failed`、`waiting`、`running`、`review`
- 最后两行：16 个顺时针观察方向

`active-work` 只作为旧名称兼容别名；唯一规范名称是 `running`。完整帧数、帧时长和方向映射位于 [`pet/pet.manifest.json`](pet/pet.manifest.json)。

## 目录

| 路径 | 内容 |
| --- | --- |
| `src/ShenshenPet.Core/` | 共享 manifest、动画状态机、Codex 安装逻辑 |
| `src/ShenshenPet.Windows/` | WPF 透明桌宠窗口与交互 |
| `pet/codex/` | 可直接安装的 Codex v2 宠物包 |
| `assets/spritesheet-v2.png` | 独立版使用的最终 PNG atlas |
| `source/` | 原始生成素材与提示词记录，不由构建覆盖 |
| `previews/` | 状态和方向预览 |
| `scripts/` | 构建、安装和严格素材验证 |
| `tests/` | 无第三方测试框架的核心回归测试 |
| `qa/`、`metadata/` | 素材制作期的归档质量记录和元数据 |

## 发布与授权

- 新增应用代码采用 MIT 许可，具体范围见 [`LICENSE`](LICENSE)。
- 角色、美术、精灵表及其衍生素材仅允许非商业使用，见 [`ASSET_LICENSE.md`](ASSET_LICENSE.md)。
- AI 生成与人工整理过程见 [`AI_PROVENANCE.md`](AI_PROVENANCE.md)。
- 上游来源和授权说明见 [`LEGAL_NOTICE.md`](LEGAL_NOTICE.md)。

“AI 生成”是来源披露，不会替代原作者许可，也不表示 OpenAI 拥有或背书本项目。

## English summary

Shenshen Pet is an unofficial, non-commercial dual-mode desktop companion. It ships as a click-to-run Windows WPF app and as a local Codex v2 custom-pet package. Code is MIT-licensed; character artwork and derived visual assets are restricted to non-commercial use.
