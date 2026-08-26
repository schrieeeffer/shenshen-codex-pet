# Changelog

本项目采用语义化版本号。视觉素材始终受非商业许可约束。

## [0.5.0] - 2026-08-26

### Added

- 新增默认开启的节能模式：10 FPS、降低自动散步频率、隐藏或暂停时完全停止渲染计时器；
- 发布包把完整 atlas 切成按需加载帧，并将常驻帧缓存限制为 12 张，避免为未显示动作保留解码内存；
- 节能模式启用 Windows EcoQoS/较低进程优先级，系统繁忙时主动让出 CPU；
- 新增 `ShenshenPet-Windows-x64-runtime-shared.zip`，复用系统 .NET 10 Desktop Runtime，减少下载与磁盘体积；
- 新增本地“白饭/羁绊”成长玩法：每日领取、喂食与等级进度均只写入本机设置；
- 新增 Pet Pack v1 安全导入、内置角色恢复和默认示例包；
- 新增可选 Codex 生命周期 Hook 状态桥接，响应任务开始、等待授权、完成和会话结束；
- 新增 Codex Hook 配置的保留式合并、安装前备份和精准卸载测试。

### Changed

- 系统托盘从 Windows Forms 改为轻量 Win32 实现，独立版不再加载 WinForms/System.Drawing；
- Codex/Web WebP 与运行时 PNG 统一由锁定的 Pillow 12.3.0 生成并验证；
- Codex Hook 使用 8 KiB 的异步状态助手，只保存动画状态，不保存提示词、工具参数或聊天记录；
- Release 同时生成免运行库版、共享运行库小体积版、Codex Pet、Web Pet、默认 Pet Pack 与统一校验和；
- 直接从 v0.3.0 演进到 v0.5.0，原计划中的 v0.4 状态桥接已包含在本版本。

## [0.3.0] - 2026-08-26

### Added

- 新增 `1536×1872` 的 ChatGPT Web 自定义 Pet 上传文件；
- Codex CLI 使用说明与兼容终端说明；
- 自动生成 `SHA256SUMS.txt`，便于核对 Release 文件完整性；
- `ORIGINS.md`：区分角色来源、媒体报道、项目参考链与未证实环节；
- README 安全/隐私卡、卸载步骤和 SmartScreen 排查说明。

### Changed

- 桌面/CLI 与 Web WebP 统一由完整 PNG atlas 无损生成并逐像素验证；
- Windows 自包含 EXE 关闭内部压缩，在保持 ZIP 下载体积优势的同时显著降低运行内存；
- Codex 安装脚本改为 Windows PowerShell 5 安全的 ASCII 输出，并加入发布包级安装冒烟测试；
- 视觉素材许可明确为 CC BY-NC-SA 4.0（在可许可范围内）并保留上游权利与维护者取得的非商业许可；
- GitHub Release 现在同时发布 Windows ZIP、桌面/CLI ZIP、WebP 和校验和。

## [0.2.0] - 2026-08-25

- 首个双模式版本；
- 新增可双击运行的 Windows WPF 绿色版；
- 新增 ChatGPT/Codex 桌面自定义 Pet 包、安装脚本、动画 manifest、CI 与 Release 流水线；
- 加入严格 atlas、元数据、核心状态机与发布包自检。

[0.5.0]: https://github.com/schrieeeffer/shenshen-codex-pet/compare/v0.2.0...v0.5.0
[0.3.0]: https://github.com/schrieeeffer/shenshen-codex-pet/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/schrieeeffer/shenshen-codex-pet/releases/tag/v0.2.0
