# 深深形象来源与项目谱系

这份记录把“可直接核实的事实”“媒体报道”和“尚未证实的推断”分开，避免把网络流传说法写成确定版权链。

## 简要结论

“深深”不是 DeepSeek 官方公布的角色，而是蓝发鲸鱼娘社区形象的衍生版本。本仓库当前精灵表不是从上游 WebM 直接重新封装：制作提示词参考了上游角色身份特征，再使用 OpenAI 图像工具生成/修改新帧，并经过人工选择、对齐、去背和打包。

## 证据分级

| 可信度 | 可以确认的内容 | 依据 |
| --- | --- | --- |
| 高 | 上善无形公开发布蓝发鲸鱼娘形象，并标注 CC BY-NC-SA 4.0 | [创作者公开动态](https://www.bilibili.com/opus/1231977657712771073) |
| 高 | `MerZlin/dsh-pet-indesktop` 源于 `PC2005-cloud/dsh-pet` | [MerZlin 仓库说明](https://github.com/MerZlin/dsh-pet-indesktop) |
| 高 | `ianlike-ui/dsh-pet-standalone` 的素材、动画链和交互设计来自 PC2005 与 MerZlin 项目 | [ianlike-ui 仓库说明](https://github.com/ianlike-ui/dsh-pet-standalone) |
| 高 | 本项目生成提示词以 `dsh-pet-standalone` 的蓝发鲸鱼娘为身份参考 | 仓库内 `source/prompts/base-pet.md` |
| 中 | 角色早期名为“溟月”；ZipZipPipe 后续用 GPT Image 2 加入 DeepSeek 与女仆元素 | [字母 AI / 36Kr 二手报道](https://eu.36kr.com/zh/p/3947452108789632)，尚未在本仓库内找到完整的早期一手发布链 |
| 未证实 | 某一个具体上游动画文件是否严格经过“ZipZipPipe → PC2005 → MerZlin → Ian”逐文件传递 | 现有公开说明不足以完成文件级证明 |

## 两条应分开理解的谱系

社区角色演化（部分环节来自媒体报道）：

`上善无形的鲸鱼娘 → 社区 DeepSeek/女仆化二创 → 更多桌宠衍生`

可直接确认的项目参考关系：

`PC2005-cloud/dsh-pet → MerZlin/dsh-pet-indesktop → ianlike-ui/dsh-pet-standalone → shenshen-codex-pet`

两条线有视觉与社区背景上的关联，但不能在缺少一手证据时合并成一条精确的文件版权转移链。

## 本项目作出的修改

- 将角色名称设为“深深”，ASCII ID 为 `shenshen`；
- 生成并筛选待机、移动、招手、跳跃、失败、等待、工作、审阅与 16 个观察方向；
- 人工校正方向、脚底基线、透明边缘和帧占用；
- 打包为 8×11、1536×2288 的完整 atlas；
- 无损转换为 ChatGPT/Codex 桌面与 CLI 包；
- 从前 9 行无损导出 1536×1872 的 ChatGPT Web 上传版。

维护者确认已取得原作者的非商业许可；授权证据不随公开仓库上传，以避免泄露私人通信。许可范围见 [`ASSET_LICENSE.md`](ASSET_LICENSE.md)，发布注意事项见 [`LEGAL_NOTICE.md`](LEGAL_NOTICE.md)。
