# Codex 技术交接：深深双模式桌宠

## 当前成果

本目录包含完整角色动画资产、Windows WPF 独立桌宠和 Codex v2 安装包。最终源 atlas 是：

`assets/spritesheet-v2.png`

Windows 入口位于 `src/ShenshenPet.Windows/`，共享动画状态机位于 `src/ShenshenPet.Core/`，桌面/CLI Codex 包位于 `pet/codex/`，ChatGPT Web 上传版位于 `pet/web/`。所有运行时语义以 `pet/pet.manifest.json` 为准。

## 固定几何合同

- 整表：1536×2288 RGBA PNG
- 网格：8 列×11 行
- 单格：192×208
- 未使用格必须保持透明

| 行号（从 0 开始） | 状态 | 有效帧 | 含义 |
| ---: | --- | ---: | --- |
| 0 | `idle` | 6 | 待机呼吸、眨眼 |
| 1 | `running-right` | 8 | 向屏幕右侧移动 |
| 2 | `running-left` | 8 | 向屏幕左侧移动 |
| 3 | `waving` | 4 | 招手/打招呼 |
| 4 | `jumping` | 5 | 起跳、腾空、落地 |
| 5 | `failed` | 8 | 失败或任务报错 |
| 6 | `waiting` | 6 | 等待用户输入/授权 |
| 7 | `running` | 6 | Codex 正在处理任务（`active-work` 仅为旧别名） |
| 8 | `review` | 6 | 结果已生成，等待检查 |
| 9 | `look-a` | 8 | 000° 到 157.5°，每步 22.5° |
| 10 | `look-b` | 8 | 180° 到 337.5°，每步 22.5° |

方向采用观看者坐标：90° 指向屏幕右边，270° 指向屏幕左边。

ChatGPT Web 只接受 1536×1872 的 8×9 标准状态表，因此 `pet/web/spritesheet.webp` 必须是完整 atlas 前 9 行的逐像素无损导出，不包含观察方向两行。完整 8×11 WebP 同时供 ChatGPT/Codex 桌面端和兼容的 Codex CLI 使用。

## 建议的事件映射

| DesktopPet/Codex 事件 | 动画状态 |
| --- | --- |
| 无任务 | `idle` |
| 正在生成、执行命令或修改文件 | `running` |
| 等待确认、审批或用户补充 | `waiting` |
| 生成结束、等待用户检查 | `review` |
| 执行失败 | `failed` |
| 成功提示或首次启动 | `waving` |
| 拖动或水平移动 | `running-left` / `running-right` |
| 用户点击角色 | `jumping` 或 `waving` |

## 构建与验证

1. `python scripts/build_codex_package.py`
2. `python scripts/verify_sprite.py`
3. `dotnet build ShenshenPet.sln --configuration Release`
4. `dotnet run --project tests/ShenshenPet.Core.Tests --configuration Release --no-build`
5. `powershell -ExecutionPolicy Bypass -File scripts/build-release.ps1`

## 验收标准

- 左右移动方向不能反转。
- 跳跃必须真正离开待机基线并落回原位。
- `running` 是工作/思考动作，不是脚步奔跑。
- 透明边缘无粉色残留。
- 角色缩放时使用 nearest/bilinear 的选择必须经过实际小尺寸预览比较。
- 修改后的最终 atlas 仍须通过 `scripts/verify_sprite.py`。
