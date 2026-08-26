# 贡献指南

感谢改进深深桌宠。提交变更前请注意：

1. 角色和视觉素材只允许非商业使用；贡献素材即表示你有权按 `ASSET_LICENSE.md` 的范围提供。
2. 不要直接覆盖 `source/` 或 `assets/spritesheet-v2.png`。重建产物先放入 `build/` 并通过校验。
3. 动画状态以 `pet/pet.manifest.json` 为唯一来源；规范工作状态名是 `running`。
4. 保持左右移动方向和 16 个观察方向的语义，不能静默镜像具有不对称特征的角色。
5. 新功能应兼顾透明窗口、多个显示器、高 DPI 和系统“减少动画”设置。
6. Pet Pack 输入必须保持路径、解压体积、PNG 几何和哈希验证；Codex Hook 不得保存提示词、工具输入或聊天正文。

提交前运行：

```powershell
python scripts\verify_sprite.py
python scripts\verify_metadata.py
dotnet build ShenshenPet.sln --configuration Release
dotnet run --project tests\ShenshenPet.Core.Tests --configuration Release
powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1
```

如果改动最终 atlas，还必须先运行 `python scripts\build_codex_package.py`，并确认 `pet/codex/spritesheet.webp` 与 `pet/web/spritesheet.webp` 的逐像素验证均通过。新增或改编视觉素材需要保留来源、修改说明和 CC BY-NC-SA 4.0 的相同方式共享条件。
