# 生成参考文件

`layout-guides/` 中的图片是由 `scripts/build_layout_guides.py` 根据 `metadata/pet-request.json` 机械生成的网格、安全边距和基线参考，不属于角色美术，也不应被复制到最终精灵图中。

规范角色身份参考位于 `source/canonical-base.png`。历史元数据已经改用仓库相对路径；无法随仓库还原的单次生成中间文件统一标为 `archive://generation-run/...`。
