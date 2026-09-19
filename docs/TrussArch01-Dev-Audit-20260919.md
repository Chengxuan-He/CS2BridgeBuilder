# 蓝色上承式桁架拱桥：dev 审计记录

## 用户验收结果

2026-09-19 用户明确反馈：`TrussArch01 验收合格`，本项按用户游戏验收通过记录。
之后木廊桥中心立柱返修未改动本桥实现。`TrussArch01Geometry.cs` SHA256 保持
`650E2B3211DE4888785519C3CF714B9D2D2B9FDBD348642897F1C1A3E5646B3F`。
此次结果不代替下文尚未完成的原型路宽口径归一审计。
随后用户确认木廊桥返修验收合格，要求两项修复一起合并到 pre-release 并安装。

## 分支与状态

- 当前项目已保存到 `pre-release` 提交 `b0fbec59052cb0b24c410bd0efa84d9bdc4936de`。
- 初始审计在 `dev` 提交 `8386e64` 上进行，未合并或挑拣 pre-release 的修改。
- pre-release 保存了桥墩宽度计划传递修改，编译通过，但没有新模型或游戏内视觉验证。
- 初始 dev 审计仅新增本文。用户随后要求立即修复；修复在 `bridge/truss-arch-01`
  从 dev 快进到的 `8386e64` 基线上进行，状态见文末。

## 使用的审计资料

1. `agent-contract/BRIDGE_WIDTH_INVARIANT_AUDIT.md` 中的 `TrussArch01` 行。
2. `agent-contract/bridge-width-invariant-measurements.tsv` 对应实测行。
3. 本机 `ModsData/BridgeBuilder/asset-anatomy.txt`，内部生成时间
   `2026-09-14 16:10:01Z`。
4. 本机 `ModsData/BridgeBuilder/tower-measurements.txt`。

历史报告记录的是桥梁整体外轮廓：原型左右边界 `[-9.203892, 9.215492]`，
生成桥梁边界 `[-23.21549, 23.21549]`，全宽增量 `-0.01159668`
（`0xBC3E0000`）。它不是“桥墩主体与拱等宽”的分部件测量；不能将此值
再次作为桥墩补偿，也不能用历史合格或微小偏差结论解释本次明显宽度错误。

## dev 代码审计

- `BridgeTowers.cs` 将 `TrussArchBridge01NetPillar` 标记为 `support: true`，
  原型路宽记录为 20 m；支撑不会被当作桥塔候选选中。
- `BridgeStyle.Selection.ExtraFor` 在没有桥塔候选时仍读取 `RoadOf(style.Id)`，
  拱按目标宽度减原型路宽计算增量。
- `BridgeComposer.FitTower` 的主支撑回退却把目标 `deckWidth` 传作源路宽。
- `TowerFactory.ExtraFor` 因而计算 `deckWidth - authored` 为零，再把这个结果
  传给 `PierExtraForSection`。桥墩没有得到拱的目标减原型路宽项。
- `MeasureStructureExtra` 已接收拱的宽度计划，但 dev 仅为绿色 `TrussArch03`
  保存它，蓝色未使用该计划。

结论：存在可定位的宽度计划传递缺陷，而不是仅需再次调整历史全宽审计常量。
修复应使蓝色桥墩与拱使用同一宽度计划，结合已测定的主体/拱原型宽差使主体等宽。
独立底座不能与主体混为同一个测量对象；最高细节与各级 LOD 必须一起检查。
这些是代码证据，不是本次生成模型的实测结果。

## 首次检查记录（已被下方新样本测量补充）

在 dev 执行现有审计工具（输出指定到仓库外，未覆盖历史 TSV）：

```powershell
.\tools\AuditBridgeWidths.ps1 -Styles TrussArch01 -OutputPath <outputs>/trussarch01-dev-width-audit-20260919.tsv
```

工具退出码为 1，原始失败原因：

```text
ERROR: Required real prefab block is absent: 两块板六车道_TrussArch01
```

首次检查的转储仅含此桥的原型，没有对应生成样本；当时运行时桥梁登记表为空，
最近导出报告是金门大桥删除记录。因此不能给出本次桥墩/拱全宽数值、误差位模式
或 LOD 一致性结论，也不能将本次实测审计标为通过或跳过。

## 新样本实测：2026-09-19 18:51:45

用户已实际生成 `一块板六车道_TrussArch01` 并提供近景截图，红框标出了窄于拱的桥墩。
样本导出时间为 `2026-09-19T10:51:45.9330813Z`，所选路面实测宽度 40 m。
不再以缺失历史命名样本为阻塞条件。

本机游戏数据根目录为 `C:/Users/admin/AppData/LocalLow/Colossal Order/Cities Skylines II`。
从 `ImportedData/一块板六车道_TrussArch01/一块板六车道_TrussArch01.Prefab` 出发，
沿 `.cid` 和 prefab 引用递归解析，确认下面九个 Geometry 文件均属于该桥的实际依赖图，
包括经过 `Pricing_*` 克隆的 section 引用，而不是按文件名猜测当前样本。
Geometry 文件位于游戏数据根目录的 `BridgeBuilder` 目录。

通过现有 GeometryMetaprogram 的二进制解码器只读加载实际顶点，按 `maxX - minX`
计算完整横向宽度，结果转为 binary32。下表十进制使用可回读原始 float 的格式，
不把导出日志的三位小数用于精确比较。

| 部件 / LOD | 顶点数 | minX (m) | maxX (m) | 完整宽度 (m) | 宽度位模式 |
| --- | ---: | ---: | ---: | ---: | --- |
| 拱 / 高清 | 150909 | -23.209644 | 23.209644 | 46.41929 | 0x4239AD5A |
| 拱 / LOD1 | 104056 | -23.209644 | 23.209644 | 46.41929 | 0x4239AD5A |
| 拱 / LOD2 | 7842 | -23.113575 | 23.113575 | 46.22715 | 0x4238E89A |
| 桥墩主体 / 高清 | 4332 | -13.208363 | 13.210926 | 26.419289 | 0x41D35AB4 |
| 桥墩主体 / LOD1 | 3065 | -13.208363 | 13.210926 | 26.419289 | 0x41D35AB4 |
| 桥墩主体 / LOD2 | 491 | -13.173695 | 13.194691 | 26.368385 | 0x41D2F274 |
| 独立 Mesh 1 / 高清 | 294 | -14.713551 | 14.725269 | 29.43882 | 0x41EB82B4 |
| 独立 Mesh 1 / LOD1 | 208 | -14.713551 | 14.725269 | 29.43882 | 0x41EB82B4 |
| 独立 Mesh 1 / LOD2 | 109 | -14.607349 | 14.686207 | 29.293556 | 0x41EA5934 |

文件对应：拱为 `TrussArchBridge01 Section 31 (2) Piece[ LOD1/LOD2].Geometry`；
桥墩主体为 `TrussArch01-40-一块板六车道_TrussArch01 Mesh[ LOD1/LOD2].Geometry`；
独立部件为同前缀 `Mesh 1[ LOD1/LOD2].Geometry`。
桥墩 object CID 为 `b59c654c1101e60c678088483c61b3cc`，主体与 Mesh 1 的本地位置为零，
旋转为单位四元数。这里的 Mesh 1 仅用于区分独立网格，不因名字就判定它满足契约的 base 定义。

高清 Geometry SHA256：

- 拱：`654C61ED5DA802CF84A1FB1801C5283301BCB39D3C271003921785551B9648FD`
- 桥墩主体：`7BBB48ED03DAEE3016F0CF25F62EFF5A1F54CE9A51B8A998C2AC6D44797A583C`
- 独立 Mesh 1：`011326AD07B840E9B699C0BC44D75DBF5A1FACBFD9991EA8495637CE93E452B4`

### 结论：桥墩与拱等宽检查失败

高清及 LOD1 的拱减桥墩主体宽度为 `20f`（`0x41A00000`），LOD2 为
`19.858765f`（`0x419EDEC0`）。这不是整体外轮廓历史审计中的 0.01159668 m 偏差，
也不是截图透视造成的错觉。不能用整体放大桥墩的方法处理。

本次导出报告记录拱增量约 31.019 m，桥墩主体增量仅约 11.220 m；
与上文源路宽错误回退的代码路径相符：目标路宽 40 m 被当作桥墩的源路宽，
丢掉了拱实际使用的 `40 - 20` 项。报告中的桥墩“生成拱宽 26.419 m”是该错误
计划的推导值，不是实际拱网格测量值。18:58:14 的后续木廊桥导出已覆盖
`last-export-report.txt`；上述蓝桥报告信息来自本轮覆盖前读取，样本 Geometry 仍保留。

### 构件级规则核查（按用户补充及 AGENTS.md）

- 桥墩不是单一构件。完全位于中心线一侧的立柱及其附属侧件必须刚性平移，
  保持柱截面、厚度和高度；不能整体横向缩放桥墩。
- 达到或跨过 `x = 0` 的横梁按各自原型跨度拉伸，连接端与平移后的立柱保持连接。
  不跨中心线的构件不得仅因名称含“梁”而拉伸。
- `TrussArch01Geometry.TryApply` 已有两条路径：未标记顶点调用 `Spread`，
  拉伸映射顶点应用离线生成的系数。当前映射为高清 224/4332、LOD1 224/3065、
  LOD2 40/491 个顶点走系数路径，其余走平移路径；这些计数不能代替逐构件验证。
- `PortalCoefficients.FromTopology` 目前用 ±0.001 m 判断跨中心线，
  `TowerWidening.Spread` 也使用中心容差，而契约要求以是否实际达到/跨越零为准。
  应在原型构件审查中单独核验该历史容差是否误分类；不能据此臆断本次 20 m 缺口由容差造成。
- LOD2 使用原型高清顶点分类映射，不能因低模焊接成一个连通体就把立柱改成拉伸件。
  本轮还未完成原型逐构件/各 LOD 的形状差分，不宣称分类全部通过。

上述为修复前审计结论：部件等宽检查明确失败。

## 原型实测补充（从游戏安装包读取，不使用生成结果反推）

桥梁原型为 `TrussArchBridge01`，结构 section 为 `TrussArchBridge01 Section`，
拱网格为 `TrussArchBridge01Net Mesh`，支撑 object 为 `TrussArchBridge01NetPillar`。
支撑 object 的两个独立网格是 `TrussArchBridge01NetPillar Mesh` 和
`TrussArchBridge01NetPillarBase Mesh`。原型及其 LOD 的引用来自
`asset-anatomy.txt` 的 359422–360260 行附近，不以硬编码源常量冒充原型测量。

直接只读打开游戏原始归档：
`D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content/Game/Blob_BridgesAndPorts.cok`，
从归档条目解码 Geometry 顶点，与生成样本使用相同解码器、相同 `maxX - minX` 全宽口径。
没有运行游戏或构造合成测试顶点。

| 原型部件 / LOD | 顶点数 | minX (m) | maxX (m) | 完整宽度 (m) | 宽度位模式 |
| --- | ---: | ---: | ---: | ---: | --- |
| 拱 / 高清 | 150909 | -7.699951 | 7.699951 | 15.399902 | 0x41766600 |
| 拱 / LOD1 | 104056 | -7.699951 | 7.699951 | 15.399902 | 0x41766600 |
| 拱 / LOD2 | 7842 | -7.603882 | 7.603882 | 15.207764 | 0x41735300 |
| 桥墩主体 / 高清 | 4332 | -7.5981445 | 7.600708 | 15.198853 | 0x41732E80 |
| 桥墩主体 / LOD1 | 3065 | -7.5981445 | 7.600708 | 15.198853 | 0x41732E80 |
| 桥墩主体 / LOD2 | 491 | -7.5634766 | 7.5844727 | 15.147949 | 0x41725E00 |
| 独立 PillarBase / 高清 | 294 | -9.203857 | 9.215576 | 18.419434 | 0x41935B00 |
| 独立 PillarBase / LOD1 | 208 | -9.203857 | 9.215576 | 18.419434 | 0x41935B00 |
| 独立 PillarBase / LOD2 | 109 | -9.097656 | 9.176514 | 18.27417 | 0x41923180 |

归档内的准确文件名（均为 `.Geometry`）：

- `TrussArchBridge01Net_8be3082637e01129ebf92cf68ab9402a`
- `TrussArchBridge01Net_LOD1_17e04f83068be63de50c2dde4aa09e2b`
- `TrussArchBridge01Net_LOD2_6c5ba0cb01d872b0963937f6e987bd57`
- `TrussArchBridge01NetPillar_a42c63261dc78b13d723cdff971612af`
- `TrussArchBridge01NetPillar_LOD1_7bc63680693ea6dda871ce1160cd7ba3`
- `TrussArchBridge01NetPillar_LOD2_641895873cba3df2a9970d6c6288efb9`
- `TrussArchBridge01NetPillarBase_89390445e2e541e60a5b7e398de767a8`
- `TrussArchBridge01NetPillarBase_LOD1_0b1fbd7dee15f15d70b0d4500d94eb1d`
- `TrussArchBridge01NetPillarBase_LOD2_2276ab09722eb24a18ae184b32d029b8`

原型的 LOD2 本来就不是与高清完全相同的外边界，不能把原型自带的简化差异全部归因于
此次变形。原型 prefab 的 `m_Bounds` 与压缩 Geometry 顶点边界也有细微差异，不能混用。

对原型高清桥墩和旧生成桥墩执行实际顶点差分：4332 个顶点，32 个焊接分组；
26 个组呈刚性平移，6 个跨中心线组呈非统一位移（横梁端点随立柱平移，内部拉伸）；
最大 y、z 位移均为 0，最大 x 位移约 5.610218 m。分组仅用于诊断，
运行时仍使用原有明确顶点映射，不以连通体整体缩放代替逻辑构件分类。

原型路宽的不同口径（历史审计 12 m、记录的结构源路宽 20 m、变体报告 22 m）
尚未完成同边界归一，因此这里不伪造完整桥梁宽差不变量的通过结论，也不实施自动审计补偿。
本次修复对象是已确认的宽度计划传递缺陷，不是向运行时追加 20 m 补偿常量。

## 修复实施与验证状态

用户要求立即修复后，在 `bridge/truss-arch-01` 完成以下修改：

1. 为蓝色桁架拱桥保留 composer 已算出的结构宽度计划，每座新桥开始时清空，避免跨桥复用。
2. 桥墩主体读取该计划及既有原型主体/拱关系，不再使用会发生 `target - target` 的回退计算。
3. 独立 PillarBase 使用同一原始计划，去掉反向推算计划的未使用函数。
4. 缺失计划时在生成桥墩几何前返回失败；未增加显式 `throw`。
5. 没有更改立柱/横梁分类数据或全局缩放；既有高清、LOD1、LOD2 派生路径接收相同计划。

`tools/Build.ps1` 编译通过，`git diff --check` 无空白错误。没有执行视觉单元测试。
已调用 `taskkill /F /IM Cities2.exe`，随后确认进程退出。

清理前已备份 138 个 ImportedData 目标目录，以及本模组 Geometry、导出状态和登记表至：
`C:/Users/admin/Documents/Codex/2026-09-01/qi/outputs/trussarch01-before-fix-20260919`。
随后用户明确要求在 dev 修复、部署开发版并待验证合格后再合并 pre-release，
因此工作区已切回 dev，保留本修复，并加入 CoveredWood 中心立柱修复。
两项修复合并编译后的 DLL SHA256：
`CB5FA8374C04CAD1A1BB79AF6258A0226D3818614450A4406918827B79CA45E1`。

仓库清理脚本已成功删除 73 个本模组 ImportedData 目录、196 个几何文件和 2 个状态文件，
并保留 9 个 RBExportDep 目录。另有 65 个旧 pre-release 造价依赖未被 dev 脚本覆盖；
独立删除被工具执行策略拦截。用户再次要求删除全部桥梁相关文件后重试，仍在进程创建前被拒绝。
没有换用其他执行通道绕过限制。准确位置见 `Pending-Generated-Pricing-Cleanup-20260919.txt`。

上述是部署前的阻塞记录；其中工具拒绝发生在进程创建前，不能据此归因于 PowerShell
ExecutionPolicy。用户随后手动删除目录，已逐项核验 65 个目标全部不存在。
ImportedData 的 Prefab 文件中也不再引用此次报错的六个缺失 CID；仓库清理程序再次运行
未发现剩余自产资产，保留 9 个道路导出依赖。

确认 Cities2.exe 不在运行后，旧安装文件已备份到上述备份目录的
`InstalledModBeforeDev` 子目录。执行 `tools/Install.ps1 -SkipBuild` 完成 dev 安装，
DLL、PDB、MJS、CSS、SVG 均已逐项核对 SHA256，与本次产物/源资源一致；DLL 哈希见上。
没有改动模组缓存、存档或 pre-release。

部署已完成，但未在游戏中重新生成验证。仍需用户重新生成同一道路的 TrussArch01 和
CoveredWood，并核验近景及远景。验证合格前不合并 pre-release。
