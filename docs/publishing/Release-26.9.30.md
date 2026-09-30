# Bridge Builder 26.9.30

Changes since the 26.9.29 source release (`cff7f37109f9eb3d3ca7fb6c6d0e82e78fd937b6`).
The double-deck railway seam fix received user in-game acceptance on 2026-09-30.
This record accompanies GitHub branch synchronization; it does not indicate a Paradox Mods upload.

## English

1. Fixed unwanted crossovers at matching, two-edge lower railway seams on double-deck bridges, including the resulting gaps in trusses and railings. Genuine junctions and mismatched track layouts are excluded; network ownership and deck separation are unchanged.
2. Added optional Node Controller support for this repair. It follows Node Controller's replacement geometry pipeline without disabling the mod or overwriting its node settings; the native game pipeline is also supported.
3. Hardened automatic cleanup: wait for catalogue/publication work, revalidate current damage on separate frames, and do not treat an old error message or a failed inspection as permission to delete a bridge. Manual deletion and generation are coordinated with cleanup.
4. Corrected network-deletion timing to run before native topology/render maintenance. Preserve surviving shared junctions and their children, respect secondary lane/object lifetimes and current ownership, and wait for placed references to retire before finalizing asset cleanup.
5. Added an early registration guard for null sections and auxiliary-network references, including overhead/underground sections. Invalid Bridge Builder networks are quarantined before native initialization, preventing the covered NetInitializeSystem null-reference path; recovered networks can be registered again.
6. Added on-disk checks for mismatched bridge UUID identities, duplicate prefab names/CIDs and missing geometry references, including collision losers hidden by native registration. Retired prefab files and sidecars are hash-checked and moved to recoverable inactive storage; live prefab indices and rendering resources are retained.
7. Added four-sample preview supersampling, combined with the existing 2x render resolution. Linear colour and transparency are accumulated together to improve thin cables and silhouettes without dark edge fringes.
8. Reworked preview lighting: directional key and fill lights, a front/top key direction, no cast shadows, fixed exposure and an isolated neutral environment independent of map day/night lighting. City lighting and authored bridge lights are not modified.
9. Removed the preview's mixed Point/ProjectorBox light setup associated with a potential HDRP light-index ordering hazard. Added preview lifecycle/readback diagnostics; this is not a claim that every player-side HDRP IndexOutOfRangeException has been resolved.
10. Isolated prototype mesh reads into private buffers, including LODs, so bridge generation no longer releases shared prototype streaming resources. Preserve surface-slot order and validate total submesh/surface alignment before exporting.
11. Added read-only prototype material audits and bounded railway seam diagnostics covering backend execution, topology, lane connections and repair hits; failures retain native geometry instead of partially applying a repair.
12. Bundled the pinned self-contained Harmony runtime and license in build/install/staging, added cleanup and network-validation regression tooling, and synchronized assembly, UI and draft publishing metadata to 26.9.30.

Back up important saves. Recreate bridges affected by old generated geometry when needed. Damage detection covers the documented structural/identity/reference faults, not every possible malformed mesh. The player's previously reported HDRP exception still requires a fresh matching log to establish its exact cause.

## 简体中文

1. 修复双层桥梁下层铁路在轨道布局一致、仅连接前后两段的内部接缝处产生多余道岔，以及由此造成的桁架、栏杆缺口。排除真实岔口和轨道布局不一致的连接，不修改网络归属及层间距。
2. 为上述修复增加可选的 Node Controller 适配，接入其替代几何计算流程；不关闭该模组，不覆盖其节点设置，同时支持游戏原版流程。
3. 加强异常桥梁自动清理：等待目录和资产发布流程完成，在不同帧复核当前损坏状态；不再仅凭旧报错或检查失败删除桥梁，并协调手动删除、生成与自动清理。
4. 修正网络删除时机，在原生拓扑及渲染维护之前执行；保护仍有连接的共享节点及其子对象，尊重次级车道、次级对象生命周期与当前归属，等待已铺设引用完成清理后再处理资产。
5. 增加注册前空引用检查，覆盖道路断面、上方/地下断面及附属网络。损坏的 Bridge Builder 网络在原生初始化前隔离，防止已识别的 NetInitializeSystem 空引用路径；恢复正常的网络可以重新注册。
6. 增加磁盘检查，识别桥梁 UUID 身份不匹配、prefab 名称/CID 冲突及几何引用缺失，包括因标识冲突而未能注册的资产。失效 prefab 文件及附属文件经哈希校验后移至可恢复的非活动目录，保留运行中的 prefab 索引和渲染资源。
7. 预览新增四次采样抗锯齿，与原有双倍渲染分辨率结合；同时累积线性色彩与透明度，改善细缆绳和轮廓锯齿，避免透明边缘黑边。
8. 调整预览光照：主光和补光统一使用平行光，主光照向正面及顶部，关闭投射阴影；固定曝光并隔离地图昼夜环境光，不改变城市光照或桥梁原有灯具。
9. 移除预览原有的 Point/ProjectorBox 混合光源组合，规避已识别的 HDRP 光源索引排序风险；增加预览生命周期和读回诊断，不将其描述为所有玩家 HDRP 越界异常的通用修复。
10. 原型网格及各级 LOD 改用独立缓冲区读取，避免生成桥梁时释放原型共享的流式渲染资源；保留材质槽顺序，导出前校验子网格与材质槽的对应关系。
11. 增加只读原型材质检查及限量铁路接缝日志，记录实际计算流程、拓扑、轨道连接和修复命中；修复失败时恢复原生几何，避免只修改部分结果。
12. 构建、安装及打包流程附带固定版本的独立 Harmony 运行库和许可证，补充清理与网络校验回归工具，统一程序集、UI 和发布配置草案版本号为 26.9.30。

请备份重要存档，必要时重新创建受旧版生成几何影响的桥梁。损坏检查覆盖已说明的结构、身份及引用问题，并非所有网格损坏。此前玩家报告的 HDRP 异常仍需新的对应日志才能确定其准确原因。

## 繁體中文

1. 修復雙層橋梁下層鐵路在軌道配置一致、僅連接前後兩段的內部接縫處產生多餘道岔，以及由此造成的桁架、欄杆缺口。排除真正岔口及軌道配置不一致的連接，不修改網路歸屬與層間距。
2. 為上述修復增加可選的 Node Controller 相容處理，接入其替代幾何計算流程；不關閉該模組，不覆寫其節點設定，同時支援遊戲原版流程。
3. 強化異常橋梁自動清理：等待目錄及資產發布流程完成，在不同影格複核目前損壞狀態；不再僅憑舊錯誤或檢查失敗刪除橋梁，並協調手動刪除、生成與自動清理。
4. 修正網路刪除時機，在原生拓撲及渲染維護之前執行；保護仍有連接的共用節點及其子物件，尊重次級車道、次級物件生命週期與目前歸屬，等待已鋪設引用完成清理後再處理資產。
5. 增加註冊前空引用檢查，涵蓋道路斷面、上方／地下斷面及附屬網路。損壞的 Bridge Builder 網路在原生初始化前隔離，防止已識別的 NetInitializeSystem 空引用路徑；恢復正常的網路可以重新註冊。
6. 增加磁碟檢查，識別橋梁 UUID 身分不符、prefab 名稱／CID 衝突及幾何引用遺失，包括因識別碼衝突而未能註冊的資產。失效 prefab 檔案及附屬檔案經雜湊校驗後移至可復原的非活動目錄，保留執行中的 prefab 索引與渲染資源。
7. 預覽新增四次取樣抗鋸齒，結合原有雙倍渲染解析度；同時累積線性色彩與透明度，改善細纜繩及輪廓鋸齒，避免透明邊緣黑邊。
8. 調整預覽光照：主光及補光統一使用平行光，主光照向正面及頂部，關閉投射陰影；固定曝光並隔離地圖晝夜環境光，不改變城市光照或橋梁原有燈具。
9. 移除預覽原有的 Point／ProjectorBox 混合光源組合，避開已識別的 HDRP 光源索引排序風險；增加預覽生命週期及讀回診斷，不將其描述為所有玩家 HDRP 越界異常的通用修復。
10. 原型網格及各級 LOD 改用獨立緩衝區讀取，避免生成橋梁時釋放原型共用的串流渲染資源；保留材質槽順序，匯出前校驗子網格與材質槽的對應關係。
11. 增加唯讀原型材質檢查及限量鐵路接縫日誌，記錄實際計算流程、拓撲、軌道連接及修復命中；修復失敗時還原原生幾何，避免只修改部分結果。
12. 建置、安裝及打包流程附帶固定版本的獨立 Harmony 執行階段與授權文件，補充清理及網路校驗回歸工具，統一組件、UI 及發布設定草稿版本號為 26.9.30。

請備份重要存檔，必要時重新建立受舊版生成幾何影響的橋梁。損壞檢查涵蓋已說明的結構、身分及引用問題，並非所有網格損壞。先前玩家回報的 HDRP 異常仍需新的對應日誌才能確定其準確原因。
