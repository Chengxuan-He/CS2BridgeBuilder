# Bridge Builder — 发布信息录入

更新：2026-09-23。发布候选版本为 **26.9.23**，程序集版本为 **26.9.23.0**。

用户已确认所有桥梁验收通过；本次新增列表加载动画，仍待游戏内 UI 检查。

## 待发布文件

候选目录：`C:/Users/admin/Downloads/BridgeBuilder-release-26.9.23-20260923`。

后续上传应使用 `publish-content`，不是 `content`、`postprocessed` 或 `bin/Release`。
官方后处理结果以包内 `PostProcess.log` 和 `ProcessedContent.json` 为准。
源码提交与内容哈希见 `Readiness.json`。本步骤只准备文件，不上传 Paradox Mods。

## 待录入或确认

- 名称：Bridge Builder；版本：26.9.23。
- 游戏兼容版本：请确认；草稿保留 `CONFIRM_BEFORE_PUBLISH`。
- 可见性：草稿为 Private，公开发布前确认。
- ModId：首次发布留空，成功后保留。
- 说明：`Mod-Description.md`，包含简体中文、繁體中文、English。
- 封面：`Thumbnail.png`，800×800 Rajdhani 实底版本。
- 截图：使用最终游戏版本截图，参照 `Image-Brief.md`。
- Road Builder 是可选兼容项，不列为强制依赖。
- 源码：https://github.com/Chengxuan-He/CS2BridgeBuilder 。
- 账号由用户在官方工具登录；不要把密码或令牌放入仓库。

删除生成桥梁会移除对应已建实例。缺失桥梁修复不自动覆盖原磁盘存档；提醒玩家备份并另存。
