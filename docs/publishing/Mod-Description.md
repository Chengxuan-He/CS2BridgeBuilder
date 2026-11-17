# Bridge Builder

## English

Bridge Builder combines available roads or tracks with supported bridge styles in Cities: Skylines II. Choose a network and bridge style, inspect the live 3D preview, then save your bridge or place it with the game's network construction tool.

### Features

*   Added GO 224m Viaduct Bridge, Nanpu Bridge and Lupu Bridge styles from their corresponding source mods.

*   Fixed suspension, cable-stayed, truss-arch, tied-arch and covered wooden styles, depending on installed content and supported prototypes.
*   Custom movable bridges: drawbridges, lift bridges and pedestrian movable styles from Bridges &amp; Ports.
*   Independent upper and lower roads or tracks on supported double-deck styles, including opposite deck directions.
*   Search and filter networks and bridge styles. Use Create to save a bridge, or Create and build to enter construction mode when it is unlocked.
*   Preview, rename, export, build and delete saved bridges from the management panel.
*   Continuous asynchronous preview generation and viewport-sized rendering with antialiasing. Actual waiting time depends on the selected content and hardware.
*   Native-material-based shaders with current-skeleton binding for supported movable surfaces and decals, with owner-local persisted dependencies.
*   A multilingual interface and configurable shortcut (Ctrl+B by default). Bridges normally inherit their source unlock requirements; the settings can bypass development-point restrictions.

### Compatibility

Every source registered for a style must be installed, enabled and natively available. Missing required content hides only the affected styles.

Bridges &amp; Ports DLC is not required to use Bridge Builder. Only bridge styles that use its content require it.

These integrations are optional; select only content you use.

**Network creation**

*   [Road Builder [BETA]](https://mods.paradoxplaza.com/mods/87190/Windows)
*   [Street Studio Public V2](https://mods.paradoxplaza.com/mods/165677/Windows)

**Bridge content**

*   [Bridge Expansion Pack](https://mods.paradoxplaza.com/mods/92245/Windows)
*   [Bridge Expansion Pack: B&P](https://mods.paradoxplaza.com/mods/124160/Windows)
*   [GO 224m Viaduct Bridge](https://mods.paradoxplaza.com/mods/166012/Windows)
*   [上海南浦大桥](https://mods.paradoxplaza.com/mods/162995/Windows)
*   [卢浦大桥](https://mods.paradoxplaza.com/mods/151335/Windows)

Art Deco Viaduct and double-deck Golden Gate from Bridge Expansion Pack also require San Francisco Set DLC. Bridge Expansion Pack: B&P remains optional compatible content, not a registered prerequisite for the current styles.

**Bridge generation**

*   [Bridge Engineer](https://mods.paradoxplaza.com/mods/164700/Windows)

**Node editing**

*   [Node Controller](https://mods.paradoxplaza.com/mods/138523/Windows)

### Notes

Follow Street Studio’s own guidance: disable Road Builder when using its independent fork.

Install the game/DLC and optional mod content required by the roads or supported bridge styles you select. The double-deck Golden Gate style comes from Bridge Expansion Pack, not the base single-deck Golden Gate prototype.

Keep Bridge Builder installed and enabled. Mod-owned precompiled material programs require its startup registration; operation after disabling or uninstalling the mod is not supported. Required game/DLC resources remain prerequisites. Saved bridges have their own dependency snapshots; updating the mod does not regenerate their geometry.

There is no reliable fully automatic importer for arbitrary custom bridge assets. Supporting a new style requires bridge-specific measurements, implementation and visual checks. A coding agent does not guarantee a correct result in one attempt. See [Adding a bridge style with a coding agent](https://github.com/Chengxuan-He/CS2BridgeBuilder#adding-a-bridge-style-with-a-coding-agent). Compatibility with every third-party asset or network/style combination is not promised.

*   Back up your city before removing a bridge. **Deleting a generated bridge also removes its placed instances.**
*   If a self-check reports changes and requests a restart, restart the game before continuing.
*   Recreate older bridges affected by a generation fix when needed; preserve unaffected bridges and back up your save first.
*   Report the game/mod versions, bridge style, selected networks, screenshots and relevant logs when something goes wrong. Rendering and motion can require further verification for particular combinations.

Source: [GitHub](https://github.com/Chengxuan-He/CS2BridgeBuilder). Original project code is MIT-licensed; game and third-party assets retain their respective licenses.

## 简体中文

Bridge Builder 可在《城市：天际线 II》中将可用道路或轨道与已支持的桥型组合。选择路网与桥型，查看实时 3D 预览，然后保存桥梁，或使用游戏原生路网工具建造。

### 功能

*   新增 GO 224m Viaduct Bridge、南浦大桥及卢浦大桥桥型，使用对应来源模组。

*   提供已支持的悬索桥、斜拉桥、桁架拱桥、系杆拱桥及木廊桥；可用桥型取决于已安装内容和支持的原型。
*   自定义活动桥梁：支持 Bridges &amp; Ports 的开启桥、升降桥和行人活动桥型。
*   支持的双层桥可以分别选择上下层道路或轨道，并设置相反方向。
*   搜索和筛选路网与桥型；“创建”保存桥梁，“创建并建造”在桥梁已解锁时进入建造模式。
*   在管理面板预览、重命名、导出、建造和删除已保存桥梁。
*   连续后台生成与异步调度预览，按可见预览区域渲染并保留抗锯齿；实际耗时取决于内容与硬件。
*   对受支持的活动桥表面和贴花使用原生材质着色器加当前骨骼绑定，并保存每座桥独立的依赖资源。
*   多语言界面和可配置快捷键（默认 Ctrl+B）。通常继承原型解锁条件；设置中可取消发展点数限制。

### 兼容性

桥型登记的每个来源均须已安装、启用且原生可用。缺失所需内容只会隐藏相关桥型。

使用 Bridge Builder 不需要 Bridges &amp; Ports DLC；只有使用该 DLC 内容的桥梁样式才需要安装它。

以下为可选兼容模组，按所需内容选择。

**路网创建**

*   [Road Builder [BETA]](https://mods.paradoxplaza.com/mods/87190/Windows)
*   [Street Studio Public V2](https://mods.paradoxplaza.com/mods/165677/Windows)

**桥梁内容**

*   [Bridge Expansion Pack](https://mods.paradoxplaza.com/mods/92245/Windows)
*   [Bridge Expansion Pack: B&P](https://mods.paradoxplaza.com/mods/124160/Windows)
*   [GO 224m Viaduct Bridge](https://mods.paradoxplaza.com/mods/166012/Windows)
*   [上海南浦大桥](https://mods.paradoxplaza.com/mods/162995/Windows)
*   [卢浦大桥](https://mods.paradoxplaza.com/mods/151335/Windows)

Bridge Expansion Pack 的 Art Deco 高架桥和双层金门大桥还需要 San Francisco Set DLC。Bridge Expansion Pack: B&P 仍为可选兼容内容，不是当前桥型登记的前置要求。

**桥梁生成**

*   [Bridge Engineer](https://mods.paradoxplaza.com/mods/164700/Windows)

**节点编辑**

*   [Node Controller](https://mods.paradoxplaza.com/mods/138523/Windows)

### 注意事项

请遵循 Street Studio 的官方说明：使用其独立分支时停用 Road Builder。

使用特定道路或受支持的桥型时，需要对应的游戏、DLC 或可选模组内容。双层金门桥来自 Bridge Expansion Pack，不是原版单层金门桥的双层模式。

请保持 Bridge Builder 安装并启用。模组预编译材质程序需要它在启动时注册，不支持停用或卸载后的正常使用。仍须保留必要的游戏和 DLC 资源。已保存桥梁拥有独立依赖副本，但更新模组不会重新生成其几何结构。

目前没有可靠的任意自定义桥梁全自动导入功能。新增桥型需要针对该桥进行测量、实现和视觉检查；即使使用编程 Agent，也不能保证一次完成。请参阅 [Adding a bridge style with a coding agent](https://github.com/Chengxuan-He/CS2BridgeBuilder#adding-a-bridge-style-with-a-coding-agent)。不承诺兼容所有第三方资产或路网与桥型组合。

*   删除前请备份城市。**删除生成的桥梁也会移除地图中已放置的实例。**
*   如果自检报告更改并提示重启，请重启游戏后再继续。
*   必要时重新创建受旧版生成问题影响的桥梁；保留未受影响的桥梁，并先备份存档。
*   报告问题时请提供游戏及模组版本、桥型、所选路网、截图和相关日志。特定组合的渲染及运动效果可能仍需进一步验证。

项目原创代码采用 MIT 许可证；游戏及第三方资产仍适用各自许可证。

## 繁體中文

Bridge Builder 可在《Cities: Skylines II》中將可用道路或軌道與已支援的橋型組合。選擇路網與橋型，查看即時 3D 預覽，然後儲存橋梁，或使用遊戲原生路網工具建造。

### 功能

*   新增 GO 224m Viaduct Bridge、南浦大橋及盧浦大橋橋型，使用對應來源模組。

*   提供已支援的懸索橋、斜張橋、桁架拱橋、繫桿拱橋及木廊橋；可用橋型取決於已安裝內容和支援的原型。
*   自訂活動橋梁：支援 Bridges &amp; Ports 的開啟橋、升降橋和行人活動橋型。
*   支援的雙層橋可以分別選擇上下層道路或軌道，並設定相反方向。
*   搜尋和篩選路網與橋型；「建立」儲存橋梁，「建立並建造」在橋梁已解鎖時進入建造模式。
*   在管理面板預覽、重新命名、匯出、建造和刪除已儲存橋梁。
*   連續背景生成與非同步排程預覽，依可見預覽區域渲染並保留抗鋸齒；實際耗時取決於內容與硬體。
*   對受支援的活動橋表面和貼花使用原生材質著色器加目前骨骼綁定，並儲存每座橋獨立的相依資源。
*   多語言介面和可設定快捷鍵（預設 Ctrl+B）。通常繼承原型解鎖條件；設定中可取消發展點數限制。

### 相容性

橋型登記的每個來源均須已安裝、啟用且原生可用。缺少所需內容只會隱藏相關橋型。

使用 Bridge Builder 不需要 Bridges &amp; Ports DLC；只有使用該 DLC 內容的橋梁樣式才需要安裝它。

以下為選用相容模組，依所需內容選擇。

**路網建立**

*   [Road Builder [BETA]](https://mods.paradoxplaza.com/mods/87190/Windows)
*   [Street Studio Public V2](https://mods.paradoxplaza.com/mods/165677/Windows)

**橋梁內容**

*   [Bridge Expansion Pack](https://mods.paradoxplaza.com/mods/92245/Windows)
*   [Bridge Expansion Pack: B&P](https://mods.paradoxplaza.com/mods/124160/Windows)
*   [GO 224m Viaduct Bridge](https://mods.paradoxplaza.com/mods/166012/Windows)
*   [上海南浦大桥](https://mods.paradoxplaza.com/mods/162995/Windows)
*   [卢浦大桥](https://mods.paradoxplaza.com/mods/151335/Windows)

Bridge Expansion Pack 的 Art Deco 高架橋和雙層金門大橋還需要 San Francisco Set DLC。Bridge Expansion Pack: B&P 仍為選用相容內容，不是目前橋型登記的前置要求。

**橋梁生成**

*   [Bridge Engineer](https://mods.paradoxplaza.com/mods/164700/Windows)

**節點編輯**

*   [Node Controller](https://mods.paradoxplaza.com/mods/138523/Windows)

### 注意事項

請遵循 Street Studio 的官方說明：使用其獨立分支時停用 Road Builder。

使用特定道路或受支援的橋型時，需要對應的遊戲、DLC 或選用模組內容。雙層金門橋來自 Bridge Expansion Pack，不是原版單層金門橋的雙層模式。

請保持 Bridge Builder 安裝並啟用。模組預編譯材質程式需要它在啟動時註冊，不支援停用或解除安裝後的正常使用。仍須保留必要的遊戲和 DLC 資源。已儲存橋梁擁有獨立相依副本，但更新模組不會重新生成其幾何結構。

目前沒有可靠的任意自訂橋梁全自動匯入功能。新增橋型需要針對該橋進行測量、實作和視覺檢查；即使使用程式設計 Agent，也不能保證一次完成。請參閱 [Adding a bridge style with a coding agent](https://github.com/Chengxuan-He/CS2BridgeBuilder#adding-a-bridge-style-with-a-coding-agent)。不承諾相容所有第三方資產或路網與橋型組合。

*   刪除前請備份城市。**刪除生成的橋梁也會移除地圖中已放置的實例。**
*   如果自我檢查報告變更並提示重新啟動，請重新啟動遊戲後再繼續。
*   必要時重新建立受舊版生成問題影響的橋梁；保留未受影響的橋梁，並先備份存檔。
*   回報問題時請提供遊戲及模組版本、橋型、所選路網、截圖和相關記錄。特定組合的渲染及運動效果可能仍需進一步驗證。

專案原創程式碼採用 MIT 授權；遊戲及第三方資產仍適用各自授權。