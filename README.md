# 深深桌宠（Shenshen Pet）

[![Release](https://img.shields.io/github/v/release/schrieeeffer/shenshen-codex-pet)](https://github.com/schrieeeffer/shenshen-codex-pet/releases/latest)
[![CI](https://github.com/schrieeeffer/shenshen-codex-pet/actions/workflows/ci.yml/badge.svg)](https://github.com/schrieeeffer/shenshen-codex-pet/actions/workflows/ci.yml)
[![Windows](https://img.shields.io/badge/Windows-10%2B-0078D4)](https://github.com/schrieeeffer/shenshen-codex-pet/releases/latest)
[![Visuals: CC BY-NC-SA 4.0](https://img.shields.io/badge/visuals-CC%20BY--NC--SA%204.0-lightgrey)](ASSET_LICENSE.md)

一个非官方、非商用的蓝发鲸鱼娘桌宠：既能双击运行在 Windows 桌面，也能作为 ChatGPT/Codex 的自定义 Pet 使用。

> 本项目与 DeepSeek、OpenAI、ChatGPT 均无隶属、合作或背书关系。维护者已取得原作者的非商业许可；代码和视觉素材使用不同许可。

![全部状态](previews/all-states.gif)

![观察方向](previews/look-loop.gif)

## 下载哪个版本

| 使用位置 | 下载文件 | 能做什么 |
| --- | --- | --- |
| Windows 独立桌面 | [`ShenshenPet-Windows-x64.zip`](https://github.com/schrieeeffer/shenshen-codex-pet/releases/latest/download/ShenshenPet-Windows-x64.zip) | 双击运行、拖动、点击、自动散步、跟随鼠标方向、托盘菜单 |
| ChatGPT/Codex 桌面端与 Codex CLI | [`Shenshen-Codex-Pet.zip`](https://github.com/schrieeeffer/shenshen-codex-pet/releases/latest/download/Shenshen-Codex-Pet.zip) | 根据 Running、Needs input、Ready、Blocked 等任务状态切换动画 |
| ChatGPT Web | [`Shenshen-ChatGPT-Web-Pet.webp`](https://github.com/schrieeeffer/shenshen-codex-pet/releases/latest/download/Shenshen-ChatGPT-Web-Pet.webp) | 上传到 ChatGPT 网页版，在支持 Pets 的 Work 对话中显示 |

下载后可用 [`SHA256SUMS.txt`](https://github.com/schrieeeffer/shenshen-codex-pet/releases/latest/download/SHA256SUMS.txt)核对文件完整性。完整变更见 [`CHANGELOG.md`](CHANGELOG.md)。

## 使用方法

### 1. Windows 独立桌宠

1. 下载 `ShenshenPet-Windows-x64.zip` 并**完整解压**；
2. 双击 `ShenshenPet.exe`；
3. 用左键点击她跳跃，按住左键拖动，右键打开菜单。

独立版已包含 .NET 运行时，不需要安装 SDK。关闭透明窗口只会隐藏到系统托盘；要完全结束，请在右键菜单或托盘菜单中选择“退出”。

### 2. ChatGPT/Codex 桌面端

下载并解压 `Shenshen-Codex-Pet.zip`，在该目录运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\install-codex-pet.ps1
```

安装完成后，在 ChatGPT/Codex 桌面端打开 **设置 > Pets**，选择 **Refresh** 和“深深”，再输入 `/pet` 唤醒。默认安装位置是：

```text
%USERPROFILE%\.codex\pets\shenshen\
├─ pet.json
└─ spritesheet.webp
```

如果设置了 `CODEX_HOME`，安装器会使用该目录。桌面端的本地自定义 Pet 不会自动同步到 ChatGPT Web，需要在网页端单独上传 Web 版。

### 3. Codex CLI

桌面端和 CLI 共用上面的本地安装目录。安装后在交互式 Codex CLI 中输入：

```text
/pets
/pets shenshen
/pets off
```

CLI Pet 需要 iTerm2 3.6+，或支持 Kitty graphics / Sixel 的终端；在 tmux 和 Zellij 中不可用。Codex IDE 扩展目前没有 Pet 选择器或悬浮宠物。

### 4. ChatGPT Web

1. 下载 `Shenshen-ChatGPT-Web-Pet.webp`；
2. 打开 **Settings > Personalization > Pet > Select pet**；
3. 选择 **Upload pet**，上传该 WebP。

Web Pets 是否可用取决于账号和工作区。Web 版只显示在支持的 ChatGPT Work 对话内，不提供桌面悬浮层、活动托盘或 `/pet` 命令。该文件是透明、无损的 `1536×1872` WebP，低于官方 20 MiB 上限。

官方行为和兼容条件以 [OpenAI Pets 文档](https://learn.chatgpt.com/docs/pets)为准。

## 安全与隐私卡

| 项目 | 默认行为 |
| --- | --- |
| 管理员权限 | 不需要；应用清单使用 `asInvoker` |
| 网络与遥测 | Windows 独立版没有网络请求、账号登录、API Key 或遥测 |
| 开机启动 | 默认关闭；只有用户主动勾选后才写入当前用户的 `HKCU\...\Run`，取消勾选会删除该值 |
| 注册表 | 除上述可选启动项外不写注册表 |
| 本地文件 | 设置写入 `%LOCALAPPDATA%\ShenshenPet\settings.json`；发生不可恢复错误时写入同目录 `crash.log` |
| 后台常驻 | 关闭窗口时会留在托盘；选择“退出”后进程结束 |
| Codex 安装器 | 只复制 `pet.json` 与 `spritesheet.webp` 到 `%CODEX_HOME%\pets\shenshen` |

`v0.3.0` 关闭了 .NET 单文件包的内部压缩，外层 ZIP 仍负责下载压缩。这样解压后的 EXE 会更大，但减少了运行时解压程序集的额外内存；在本次 Windows 30 秒冒烟测试中，私有内存约从 216 MiB 降至 143 MiB，工作集约从 347 MiB 降至 223 MiB。WPF、自包含 .NET 运行时、系统版本、缩放和已缓存动画帧都会影响实际数值。

卸载绿色版前先取消“开机启动”并退出程序，然后删除解压目录；需要清理设置时再删除 `%LOCALAPPDATA%\ShenshenPet`。卸载 Codex Pet 只需删除 `%USERPROFILE%\.codex\pets\shenshen`（或对应 `CODEX_HOME` 路径）。

### 为什么 Windows 会显示 SmartScreen

当前 EXE 没有商业代码签名证书，且新发布文件尚未积累 SmartScreen 信誉，所以从互联网下载后可能出现“Windows 已保护你的电脑”。这是信誉/签名提示，并不等于 Defender 已检测到病毒。

建议先确认文件来自本仓库 Release，再核对 SHA-256：

```powershell
Get-FileHash .\ShenshenPet-Windows-x64.zip -Algorithm SHA256
```

将结果与同一 Release 的 `SHA256SUMS.txt` 比较。确认无误后，可在 SmartScreen 中选择“更多信息 > 仍要运行”。如果安全软件明确报告具体恶意软件名称或哈希不一致，请不要运行并提交 Issue。

## 从源码构建

要求：

- Windows 10 或更高版本；
- .NET 10 SDK；
- Python 3.11+ 与 `requirements.txt` 中固定版本的 Pillow。

```powershell
python -m pip install -r requirements.txt
python scripts\build_layout_guides.py
python scripts\build_codex_package.py
python scripts\verify_sprite.py
python scripts\verify_metadata.py
dotnet build ShenshenPet.sln --configuration Release
dotnet run --project tests\ShenshenPet.Core.Tests --configuration Release
```

构建完整 Release：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-release.ps1
```

`dist/` 会生成 Windows ZIP、桌面/CLI Pet ZIP、Web 上传 WebP 和 `SHA256SUMS.txt`。发布包会先运行 EXE 的 `--self-test`，CI 也会重建并逐像素验证两个 WebP。

## 动画合同

- 完整精灵表：`1536×2288`、RGBA、8 列×11 行、单格 `192×208`；
- 标准状态行：`idle`、`running-right`、`running-left`、`waving`、`jumping`、`failed`、`waiting`、`running`、`review`；
- 最后两行：16 个顺时针观察方向；
- ChatGPT Web：完整精灵表的前 9 行，`1536×1872`；
- `active-work` 只作为旧名称兼容别名，唯一规范名称是 `running`。

帧数、时长、循环方式、别名与方向映射全部以 [`pet/pet.manifest.json`](pet/pet.manifest.json) 为准。

## 项目结构

| 路径 | 内容 |
| --- | --- |
| `src/ShenshenPet.Core/` | 共享 manifest、动画状态机、Codex 安装逻辑 |
| `src/ShenshenPet.Windows/` | WPF 透明桌宠窗口与交互 |
| `pet/codex/` | ChatGPT/Codex 桌面与 CLI 的完整 v2 Pet |
| `pet/web/` | ChatGPT Web 的 8×9 上传图 |
| `assets/spritesheet-v2.png` | 独立版使用的最终 PNG atlas |
| `source/` | 原始生成素材与提示词记录，不由构建覆盖 |
| `previews/` | 状态和方向预览 |
| `scripts/` | 构建、安装、打包和严格素材验证 |
| `tests/` | 无第三方测试框架的核心回归测试 |
| `qa/`、`metadata/` | 素材制作期的归档质量记录和元数据 |

## 来源与许可

- 角色来源、证据强度与项目谱系：[`ORIGINS.md`](ORIGINS.md)；
- 发布授权边界：[`LEGAL_NOTICE.md`](LEGAL_NOTICE.md)；
- 视觉素材：[`ASSET_LICENSE.md`](ASSET_LICENSE.md)，CC BY-NC-SA 4.0 与上游条款；
- AI 与人工制作过程：[`AI_PROVENANCE.md`](AI_PROVENANCE.md)；
- 新增应用代码：[`LICENSE`](LICENSE)，MIT。

“AI 生成”是来源披露，不会替代原作者许可，也不表示 OpenAI 拥有或背书本项目。

## Roadmap

- `v0.4`：可选 Codex Hooks/App Server 状态桥接，让独立 Windows 桌宠响应真实任务状态；
- `v0.5`：可导入 Pet Pack，以及“吃白饭”成长/收集玩法；
- 后续：可复现构建与代码签名方案、更多原创角色包、无障碍与多显示器体验优化。

欢迎通过 Issue 提交复现问题、动作建议和原创非商业 Pet Pack。贡献前请阅读 [`CONTRIBUTING.md`](CONTRIBUTING.md)。

## English summary

Shenshen Pet is an unofficial, non-commercial companion for Windows, ChatGPT/Codex desktop, Codex CLI, and ChatGPT Web. The Windows build is portable and offline. Code is MIT-licensed; visual assets are CC BY-NC-SA 4.0 where licensable, subject to upstream rights and the maintainer's non-commercial permission.
