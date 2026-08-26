# 深深 ChatGPT/Codex Pet 安装包

这是“深深”的完整 8×11 自定义 Pet 包，可供 ChatGPT/Codex 桌面端和兼容的 Codex CLI 使用。仅允许非商业使用。

## 一键安装

在本目录打开 PowerShell：

```powershell
powershell -ExecutionPolicy Bypass -File .\install-codex-pet.ps1
```

安装器只会把 `pet.json` 与 `spritesheet.webp` 复制到 `%CODEX_HOME%\pets\shenshen`；如果未设置 `CODEX_HOME`，则使用 `%USERPROFILE%\.codex\pets\shenshen`。它不需要管理员权限，不写注册表，也不访问网络。

## ChatGPT/Codex 桌面端

安装后打开 **设置 > Pets**，选择 **Refresh** 和“深深”，再输入 `/pet` 唤醒。

## Codex CLI

在交互式 CLI 中输入 `/pets` 打开选择器，或输入 `/pets shenshen` 直接选择；使用 `/pets off` 关闭。终端必须支持 iTerm2 3.6+、Kitty graphics 或 Sixel，且不能位于 tmux/Zellij 内。

## 手动安装

将 `shenshen` 整个文件夹复制到：

```text
%USERPROFILE%\.codex\pets\shenshen
```

目录内必须保留 `pet.json` 和 `spritesheet.webp`。ChatGPT Web 不能直接使用这个 8×11 文件，请从 GitHub Release 下载单独的 `Shenshen-ChatGPT-Web-Pet.webp`。

本项目非 DeepSeek 或 OpenAI 官方产品。视觉素材许可、来源和 AI 制作说明见随包提供的 `ASSET_LICENSE.md`、`LEGAL_NOTICE.md` 与 `AI_PROVENANCE.md`。
