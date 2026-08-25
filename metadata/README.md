# Metadata archive

此目录保存角色生成需求和制作任务记录，用于审计来源，不参与桌宠运行。运行时唯一动画合同是 `pet/pet.manifest.json`。

- 可复现输入使用仓库相对路径。
- 没有随仓库保存的单次生成中间文件使用 `archive://generation-run/...` URI，明确表示它们是归档引用而非本地文件。
- 不应把归档任务 JSON 当作构建清单或运行时资源索引。
