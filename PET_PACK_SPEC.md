# Shenshen Pet Pack v1

Pet Pack 是 Windows 独立版可导入的非商用角色包。文件扩展名可以是 `.zip` 或 `.shenshenpet`，两者内容相同。

## 最小结构

```text
my-pet.zip
├─ pet.manifest.json
└─ assets/
   └─ spritesheet-v2.png
```

`pet.manifest.json` 必须位于 ZIP 根目录。精灵表路径由 manifest 的 `atlas.path` 指定；建议使用 `assets/spritesheet-v2.png`。可以附带 `README.md`、许可和来源说明。

## 动画合同

- PNG、RGBA、透明背景；
- 整表 `1536×2288`，8 列×11 行；
- 单格 `192×208`；
- 前 9 行依次为 `idle`、`running-right`、`running-left`、`waving`、`jumping`、`failed`、`waiting`、`running`、`review`；
- 最后两行为 16 个观察方向；
- 行、帧数、时长、循环与 SHA-256 必须由 manifest 完整声明。

可以复制本仓库的 [`pet/pet.manifest.json`](pet/pet.manifest.json)作为模板，但必须修改 `id`、`displayName`、描述和精灵表 SHA-256。`id` 只能包含 1–32 个小写字母、数字和连字符。

## 导入安全限制

Windows 端在解压前后都会验证：

- 最多 32 个文件、解压后总计不超过 30 MiB；
- 禁止绝对路径、`..` 目录穿越和符号链接；
- atlas 必须是 manifest 声明尺寸的 PNG；
- atlas SHA-256 必须与 manifest 完全一致；
- 写入 `%LOCALAPPDATA%\ShenshenPet\packs\<id>` 前先在同目录暂存并验证。

同一 `id` 再次导入会更新该角色包。内置“深深”始终保留，可在右键菜单中恢复。

## 许可要求

分发 Pet Pack 时，请同时提供素材来源、AI 制作说明（如适用）和真实许可条件。不要把 MIT 代码许可自动套用于美术。默认深深包受 `ASSET_LICENSE.md` 所列 CC BY-NC-SA 4.0 与上游非商业条款约束。
