# 安全策略

## 支持范围

仅维护最新发布版本和 `main` 分支。Windows 独立版不需要管理员权限、API Key 或网络访问，也不包含遥测。

默认不启用开机启动。只有用户主动勾选后，程序才会写入当前用户的 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\ShenshenPet`；取消勾选会删除该值。应用设置与崩溃日志只写入 `%LOCALAPPDATA%\ShenshenPet`。

发布包没有商业代码签名证书，因此 SmartScreen 可能显示信誉提示。请从官方 Release 下载并使用同一 Release 的 `SHA256SUMS.txt` 核对哈希；如果杀毒软件报告具体恶意软件或哈希不一致，不要绕过警告。

## 报告问题

请优先使用 GitHub 仓库的 **Security > Report a vulnerability** 私密报告功能。不要在公开 Issue 中粘贴凭据、个人路径、日志中的私人信息或授权证明原件。

建议报告中包含受影响版本、复现步骤、预期影响和最小化日志。普通崩溃、动画错位和功能建议可以使用公开 Issue。
