# 人行木廊桥桥面上方中心立柱审计与修复

## 最新状态：用户验收通过

2026-09-19 用户确认人行木廊桥返修验收合格，并要求将其与已验收的 TrussArch01
合并到 pre-release、切换分支并安装。下面的“待验收”表述保留为部署时的历史记录。

用户确认 TrussArch01 合格，但拒绝首版木廊桥的 32.164623 m 宽中心立柱。
用户随后将“1.6 m”明确更正为“修复前的立柱顶端宽度”。本次仅修改桥面上方中心立柱，
并按这一明确要求取代首版随路宽增加整根柱宽的处理；不修改桥下桥墩或 TrussArch01。

用原型的 272 个立柱顶点身份对齐真实修复前/首版生成网格，读取 136 个柱顶顶点：

| 样本 | 顶端 y (m) | 顶端 minX (m) | 顶端 maxX (m) | 顶端全宽 (m) | 整柱全宽 (m) |
| --- | ---: | ---: | ---: | ---: | ---: |
| 实际原型 | 4.4311523 | -0.08231163 | 0.08231163 | 0.16462326 | 0.16462326 |
| 修复前生成物 | 4.4311523 | -0.80877763 | 0.80877763 | 1.6175553 | 24.509445 |
| 用户拒绝的首版生成物 | 4.4311523 | -16.082312 | 16.082312 | 32.164623 | 32.164623 |

旧样本来自下文清理前备份中的 `PedestrianBridgeCoveredWood01 Section 32 Piece.Geometry`，
SHA256 为 `4D3C0C763AC82EA99C48B0D6EA842F2487A82376CAA1891DAD7318728AC3A0D9`。
这不是把截图的约 1.6 m 当成精确值，也不是把原型薄柱宽误写成 1.6 m。

元编程命令现接收原型归档和修复前高清 Geometry，核对索引拓扑、17 根柱的顶端边界、
原型 y/z，输出目标左右边界和原有顶点映射。运行时只应用不可变矩形面坐标，
不读取旧样本、不从运行时高度分片取宽、不再将道路增宽加到柱宽上。
目标完整宽度固定为上述实测 1.6175553 m，柱身上下相同；高度、纵向厚度和位置不变。
高清 272 顶点、LOD1 189 顶点统一处理；原型 LOD2 的空立柱映射保留。

两项代码仍位于用户指定的 dev。TrussArch01Geometry.cs SHA256 保持
`650E2B3211DE4888785519C3CF714B9D2D2B9FDBD348642897F1C1A3E5646B3F`。
新版编译和安装完成，五个安装文件均通过哈希核对，DLL SHA256 为
`F0830B589129D022BF5FF850AA1CDF7E52E2E63A3A5961A5E2C4728EF4F12D9D`。
元编程使用实际原型和生成物，未运行视觉单元测试，也未操作游戏生成样本。

按仓库要求停止游戏后，本轮备份位于
`C:/Users/admin/Documents/Codex/2026-09-01/qi/outputs/coveredwood-top-width-20260919`。
已清理 23 个自产 ImportedData 目录、24 个几何相关文件及 1 个状态文件，
再次清理结果为零，保留 9 个道路导出依赖；剩余 Prefab 中没有被删除几何的 CID 引用。
模组缓存、存档及 pre-release 均未修改。木廊桥近景/远景视觉结果仍需重新生成后验收。

以下是首版实现的历史审计，首版变宽规则已被上述用户要求取代。

## 基线和范围

依用户最新明确指示，两项修复均在 `dev` 完成；以 `8386e64` 为基线。
这次指示覆盖此前单桥分支工作流。未将修复合入 `pre-release`，待游戏验证合格再合并。
修改前已读取根目录、运行时代码、元编程目录的 AGENTS.md 以及完整项目合同。

本报告对象是桥面上方、木廊内部的中心立柱，**不是桥下桥墩**。
与 TrussArch01 桥墩宽度输入修复一起编译，互不借用原型参数。

## 实际原型与实际旧生成物

原型：`PedestrianBridgeCoveredWood01` 的 `PedestrianBridgeCoveredWood01 Section`，
其唯一 net piece 是 `PedestrianBridgeCoveredWood01Cover Mesh`，声明长 64 m、宽 9.5 m。
原型原始压缩顶点从已安装游戏的以下归档只读解码，而非从图片猜测：

`D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content/BridgesAndPorts/Blob.cok`

| LOD | 归档 Geometry | 总顶点数 | 中心立柱顶点数 |
| --- | --- | ---: | ---: |
| 高清 | PedestrianBridgeCoveredWood01Cover_321693eb11cd6dbcd19faf61b9cac862.Geometry | 10564 | 272 |
| LOD1 | PedestrianBridgeCoveredWood01Cover_LOD1_9a6210a3c2b36fe34abf99311c03c593.Geometry | 6563 | 189 |
| LOD2 | PedestrianBridgeCoveredWood01Cover_LOD2_d4af66f1043b4287b5d5484901b2057b.Geometry | 444 | 0 |

高清原型具有 17 根独立矩形中心立柱，每根左右边界为
`[-0.08231163, 0.08231163]` m，上下边界为 `[0.15230179, 4.4311523]` m。
中心立柱沿桥长分布；每根完整构件跨过 x=0。LOD1 的对应顶点继承高清坐标分类。
原型 LOD2 自身没有中心立柱，仅余屋面中线的 x=0 顶点，不能擅自添加新的 LOD 构件。

实际旧生成物是用户生成的 `一块板六车道_CoveredWood`，道路宽 40 m，
原导出报告记录结构增宽 32 m。实际文件为
`PedestrianBridgeCoveredWood01 Section 32 Piece.Geometry` 及两个 LOD。
清理前已读取并保存在
`C:/Users/admin/Documents/Codex/2026-09-01/qi/outputs/trussarch01-before-fix-20260919/BridgeBuilder`。

逐顶点对应发现，高清及 LOD1 的同一根矩形柱，底部 x 约为 ±12.25 m，
顶部约为 ±0.81 m。原型上下相同的 x 坐标被通用高度分片 profile 赋予不同伸缩比例，
所以生成物成为楔形；不是桥下立柱摆放或灯光问题。

## 已实施

1. 新增 `GeometryMetaprogram --covered-wood-columns <Blob.cok>`，从上述实际原型识别 17 根柱，
   审核跨中心线关系及恒定矩形边界，输出精确顶点范围与原型跨度。
2. LOD1 顶点身份由高清原型坐标继承；LOD2 明确记录空范围，保持原型简化结果。
3. `CoveredWoodColumnData` 保存生成的不可变参数；运行时不使用高度阈值、拓扑推断或最近点搜索。
4. `CoveredWoodGeometry` 对记录的完整立柱使用自身恒定跨度伸缩，所有高度使用同一规则，
   消除楔形。按合同跨 x=0 构件的规则，32 m 增宽后立柱完整宽度为原柱宽度加 32 m，
   而非强制保持细柱宽度，也不是把桥下桥墩改成路宽。
5. 保留其他构件、y/z 坐标、索引、材质和原始顶点通道。缺失/失配的映射返回失败，
   在当前几何写出前拒绝；未添加运行时显式 throw。

## 验证边界

元编程已对真实原型生成上述全部映射；两项修改一同通过 `tools/Build.ps1` 编译。
没有运行视觉单元测试。代码路径确认 piece 和 LOD 均收到相同变宽参数。
这只证明来源、顶点身份及可编译性，**尚不证明游戏近/远景已修复**。

已停止并确认 Cities2.exe 不在运行。清理完成 73 个本模组 ImportedData 目录、196 个几何文件、
2 个状态文件，保留 9 个 RBExportDep 道路依赖；未触碰模组缓存和存档。
另有 65 个旧 pre-release 的造价克隆目录不在 dev 清理脚本覆盖范围；早先删除命令在工具
创建进程前被拒绝，并非已确认的 PowerShell ExecutionPolicy 限制。历史清单见
`Pending-Generated-Pricing-Cleanup-20260919.txt`，这些目录已备份。

用户手动删除后，已逐项确认全部 65 个目录不存在；ImportedData 的 Prefab 文件中也不再
包含本次空引用错误对应的六个缺失 CID。再次执行仓库清理程序没有发现剩余自产资产，
保留 9 个道路导出依赖。确认游戏关闭后，已用 `tools/Install.ps1 -SkipBuild` 安装 dev。
旧安装文件备份到上述备份目录的 `InstalledModBeforeDev` 子目录。
DLL、PDB、MJS、CSS、SVG 的安装文件与编译产物/源资源 SHA256 全部一致。
安装 DLL SHA256 为 `CB5FA8374C04CAD1A1BB79AF6258A0226D3818614450A4406918827B79CA45E1`。

部署已完成，视觉验证仍未完成。由用户重新生成两种桥并检查近景和远景；
验证合格前不合并 pre-release。此次部署未修改模组缓存或存档。
