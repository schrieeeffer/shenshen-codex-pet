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
| Windows 独立桌面（推荐） | [`ShenshenPet-Windows-x64.zip`](https://github.com/schrieeeffer/shenshen-codex-pet/releases/latest/download/ShenshenPet-Windows-x64.zip) | 自包含绿色版，完整解压即可用 |
| Windows 独立桌面（小体积） | [`ShenshenPet-Windows-x64-runtime-shared.zip`](https://github.com/schrieeeffer/shenshen-codex-pet/releases/latest/download/ShenshenPet-Windows-x64-runtime-shared.zip) | 复用系统 .NET 10 Desktop Runtime，减少下载和磁盘体积；常驻内存与自包含版接近 |
| ChatGPT/Codex 桌面端与 Codex CLI | [`Shenshen-Codex-Pet.zip`](https://github.com/schrieeeffer/shenshen-codex-pet/releases/latest/download/Shenshen-Codex-Pet.zip) | 根据 Running、Needs input、Ready、Blocked 等任务状态切换动画 |
| ChatGPT Web | [`Shenshen-ChatGPT-Web-Pet.webp`](https://github.com/schrieeeffer/shenshen-codex-pet/releases/latest/download/Shenshen-ChatGPT-Web-Pet.webp) | 上传到 ChatGPT 网页版，在支持 Pets 的 Work 对话中显示 |
| Windows 角色包 | [`Shenshen-Default-Pet-Pack.zip`](https://github.com/schrieeeffer/shenshen-codex-pet/releases/latest/download/Shenshen-Default-Pet-Pack.zip) | 测试“导入 Pet Pack”，或作为自制角色包模板 |

下载后可用 [`SHA256SUMS.txt`](https://github.com/schrieeeffer/shenshen-codex-pet/releases/latest/download/SHA256SUMS.txt)核对文件完整性。完整变更见 [`CHANGELOG.md`](CHANGELOG.md)。

## 使用方法

### 1. Windows 独立桌宠

1. 下载 `ShenshenPet-Windows-x64.zip` 并**完整解压**；如果已经安装 [`.NET 10 Desktop Runtime x64`](https://dotnet.microsoft.com/download/dotnet/10.0)，也可使用更小的 `ShenshenPet-Windows-x64-runtime-shared.zip`；
2. 双击 `ShenshenPet.exe`；
3. 用左键点击她跳跃，按住左键拖动，右键打开菜单。

两个版本都不需要 .NET SDK。关闭透明窗口只会隐藏到系统托盘；要完全结束，请在右键菜单或托盘菜单中选择“退出”。`v0.5.0` 默认开启节能模式，并将隐藏/暂停时的渲染计时器完全停止。

右键菜单还可以每天领取白饭、喂食提升羁绊，以及导入符合 [`PET_PACK_SPEC.md`](PET_PACK_SPEC.md) 的非商用角色包。成长进度离线保存在应用设置中；同一角色包 `id` 再次导入会安全更新，可随时恢复内置深深。

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

### 5. 让 Windows 深深响应 Codex 任务（可选）

在 Windows 深深的右键菜单中选择“安装 Codex 状态桥接”，然后在 Codex CLI 输入 `/hooks`，检查并信任新增 Hook。之后独立桌宠会响应：

- 提交新任务：`running`；
- Codex 等待授权：`waiting`；
- 本轮完成：`review`；
- 会话结束：`idle`。

该功能基于 [OpenAI Codex Hooks](https://learn.chatgpt.com/docs/hooks)，默认异步运行，只同步预定义动画状态。安装前会备份 Hook 配置，卸载只删除带深深标记的处理器。

## 安全与资源占用

| 项目 | 默认行为 |
| --- | --- |
| 管理员权限 | 不需要；应用清单使用 `asInvoker` |
| 网络与遥测 | Windows 独立版没有网络请求、账号登录、API Key 或遥测 |
| 开机启动 | 默认关闭；只有用户主动勾选后才写入当前用户的 `HKCU\...\Run`，取消勾选会删除该值 |
| 注册表 | 除上述可选启动项外不写注册表 |
| 本地文件 | 设置/成长写入 `%LOCALAPPDATA%\ShenshenPet\settings.json`；角色包写入 `packs\`；可选桥接写入 `codex-state.json`；不可恢复错误写入 `crash.log` |
| 后台常驻 | 关闭窗口时会留在托盘；选择“退出”后进程结束 |
| Codex 安装器 | 只复制 `pet.json` 与 `spritesheet.webp` 到 `%CODEX_HOME%\pets\shenshen` |
| Codex 状态桥接 | 默认未安装；用户点击后先备份配置，只同步预定义动画状态 |

`v0.5.0` 把旧的 16 ms（约 60 FPS）轮询改为默认 100 ms（10 FPS），节能模式下自动散步也更少；窗口隐藏或动画暂停时计时器停止，并启用 Windows EcoQoS/较低进程优先级，在系统繁忙时主动让出 CPU。发布包不再启动时解码整张 atlas，而是按需加载当前动作帧，并只缓存最近 12 张。托盘改为原生 Win32 实现，不再加载 WinForms/System.Drawing。WPF、系统版本、缩放和已缓存动画帧仍会影响实际数值。

实际资源占用会随 Windows 版本、显示缩放和当前动画变化。隐藏到托盘或暂停动画后会停止渲染计时器；完全不使用时请选择“退出”，进程结束后不再占用内存和 CPU。

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

`dist/` 会生成两个 Windows ZIP、桌面/CLI Pet ZIP、Web 上传 WebP、默认 Pet Pack 和 `SHA256SUMS.txt`。发布包会先运行两个 Windows 版本及轻量 Hook 助手的自检，CI 也会重建并逐像素验证两个 WebP。

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
| `src/ShenshenPet.Bridge/` | 轻量 Codex Hook 状态助手 |
| `pet/codex/` | ChatGPT/Codex 桌面与 CLI 的完整 v2 Pet |
| `pet/web/` | ChatGPT Web 的 8×9 上传图 |
| `assets/spritesheet-v2.png` | 独立版使用的最终 PNG atlas |
| `source/` | 原始生成素材与提示词记录，不由构建覆盖 |
| `previews/` | 状态和方向预览 |
| `scripts/` | 构建、安装、打包和严格素材验证 |
| `tests/` | 无第三方测试框架的核心回归测试 |
| `qa/`、`metadata/` | 素材制作期的归档质量记录和元数据 |
| `PET_PACK_SPEC.md` | Windows 角色包格式与安全限制 |

## 来源与许可

- 角色来源、证据强度与项目谱系：[`ORIGINS.md`](ORIGINS.md)；
- 发布授权边界：[`LEGAL_NOTICE.md`](LEGAL_NOTICE.md)；
- 视觉素材：[`ASSET_LICENSE.md`](ASSET_LICENSE.md)，CC BY-NC-SA 4.0 与上游条款；
- AI 与人工制作过程：[`AI_PROVENANCE.md`](AI_PROVENANCE.md)；
- 新增应用代码：[`LICENSE`](LICENSE)，MIT。

“AI 生成”是来源披露，不会替代原作者许可，也不表示 OpenAI 拥有或背书本项目。

## Roadmap

- `v0.5` 已完成：Codex Hooks 状态桥接、Pet Pack 导入、白饭/羁绊成长和低功耗运行；
- 后续：可复现签名构建、更多原创角色包、多显示器/无障碍优化，以及社区角色包索引。

欢迎通过 Issue 提交复现问题、动作建议和原创非商业 Pet Pack。贡献前请阅读 [`CONTRIBUTING.md`](CONTRIBUTING.md)。

## English summary

Shenshen Pet is an unofficial, non-commercial companion for Windows, ChatGPT/Codex desktop, Codex CLI, and ChatGPT Web. The Windows build is portable and offline. Code is MIT-licensed; visual assets are CC BY-NC-SA 4.0 where licensable, subject to upstream rights and the maintainer's non-commercial permission.
