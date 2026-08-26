# Changelog

本项目采用语义化版本号。视觉素材始终受非商业许可约束。

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

[0.3.0]: https://github.com/schrieeeffer/shenshen-codex-pet/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/schrieeeffer/shenshen-codex-pet/releases/tag/v0.2.0
