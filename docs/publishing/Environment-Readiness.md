# Paradox Mods 发布环境状态

更新：2026-09-23。

## 已配置工具链

- Unity 2022.3.62f2，许可证及 UnityModsProject 初始化已在 2026-09-20 验证。
- Burst 1.8.23、Collections 2.5.7、Entities 1.3.10。
- 私有 .NET Runtime 6.0.36 用于运行官方 ModPostProcessor/ModPublisher。
- Burst 官方 tarball：`C:/Users/admin/Downloads/BridgeBuilder-Burst-1.8.23/com.unity.burst-1.8.23.tgz`，不要删除。
- 工具路径取自本机 `CSII_*` 用户环境变量，不在仓库存放账号凭据。

## 本次候选

版本 **26.9.23**。用户已确认桥梁验收通过；本次只新增列表加载状态和转圈动画。
候选目录：`C:/Users/admin/Downloads/BridgeBuilder-release-26.9.23-20260923`。

每次打包都重新运行官方后处理，不复用 0.3.6 的旧 DLL。具体退出码及文件哈希保存在候选的
`PostProcess.log`、`ProcessedContent.json` 和 `Readiness.json`。只有后处理成功才使用
`publish-content` 作为待上传目录。新动画的游戏内 UI 验收仍待完成。

尚未授权向 Paradox Mods 上传、创建条目或公开发布。信息录入见 `Information-Entry.md`。
