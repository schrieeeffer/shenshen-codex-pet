# 深深 Codex 桌宠安装包

这是本项目的 Codex v2 自定义宠物发布包，仅允许非商业使用。

## 一键安装

在本目录打开 PowerShell：

```powershell
powershell -ExecutionPolicy Bypass -File .\install-codex-pet.ps1
```

安装器会复制到 `%CODEX_HOME%\pets\shenshen`；如果未设置 `CODEX_HOME`，则使用 `%USERPROFILE%\.codex\pets\shenshen`。

安装完成后，在 ChatGPT/Codex 桌面端打开 **设置 > Pets**，选择 **Refresh** 和“深深”，再输入 `/pet` 唤醒。

## 手动安装

将 `shenshen` 整个文件夹复制到：

```text
%USERPROFILE%\.codex\pets\shenshen
```

目录内必须保留 `pet.json` 和 `spritesheet.webp`。

本项目非 OpenAI 官方产品。素材许可见随包提供的 `ASSET_LICENSE.md`。
