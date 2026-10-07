# Bridge Builder

## English

### Overview

Bridge Builder lets you combine available roads or tracks with bridge styles in Cities: Skylines II. Choose a deck and a bridge style, inspect the combination in an isometric 3D preview, then place the generated bridge using the game's network construction tool. Supported double-deck styles let you choose the upper and lower networks independently and reverse the direction of one deck relative to the other.

### Features

- Choose from available suspension, cable-stayed, truss-arch, tied-arch and covered wooden bridge styles. The selection depends on installed content and supported prototypes.
- Search roads and tracks, filter them by network type, and search bridge styles.
- Use **Create** to save a bridge, or **Create and build** to close the mod panel and enter construction mode when the bridge is unlocked.
- Search, filter, preview, rename, build and delete previously created bridges.
- Generated bridges belong to the Bridge Builder asset pack. The interface supports multiple languages.

### Getting started

Load a city and open Bridge Builder from its toolbar icon. Select a road or track and a bridge style. For a double-deck bridge, choose the second network and the deck-direction option. Review the preview and bridge name, then select Create or Create and build.

- **If the bridge self-check is triggered, please restart the game.**
- Uninstalling Bridge Builder does not remove bridges you have already created. These saved bridges remain available for placement and use. To remove them, use Bridge Builder’s bridge management tools.

### Compatibility

- Use Road Builder to create custom roads for your bridges. Once a bridge has been generated, deleting its source road in Road Builder does not prevent you from placing that saved bridge again. This does not mean required DLC, content mods or shared assets can be uninstalled.

- Compatible with [Road Builder](https://mods.paradoxplaza.com/mods/87190/Windows) and [Bridge Expansion Pack](https://mods.paradoxplaza.com/mods/92245/Windows). **Both mods are optional, not required dependencies of Bridge Builder.** Install them only to use their roads or supported bridge styles.
- Some bridge styles require their corresponding DLC or content mod. The double-deck Golden Gate style comes from **Bridge Expansion Pack**; it is not a double-deck mode of the base single-deck Golden Gate prototype.
- Compatible with [Node Controller](https://mods.paradoxplaza.com/mods/138523/Windows). **Node Controller is optional, not a required dependency.** The double-deck lower-railway seam fix also supports its geometry pipeline without overriding its node settings.
### Important notes

- **After updating to a version that fixes a bridge-generation problem, delete and recreate affected bridges created with older versions.** Updating the mod does not regenerate existing bridge assets. Back up your save first; deleting a bridge also removes its placed instances.
- Settings provide a configurable shortcut (Ctrl+B by default) and an option to remove development-point restrictions. Bridges normally inherit their prototypes' unlock requirements; enabling the option bypasses development unlock restrictions when building.
- Bascule bridges, pedestrian bascule bridges and lift bridges are not currently supported. Not every network/style combination is supported.
- **Deleting a generated bridge also removes its placed instances from the map. Back up your save first.** This is not simply removing a catalogue entry.
- When reporting a problem, include game and mod versions, the bridge style, selected networks, screenshots and relevant mod logs.

Source: [GitHub](https://github.com/Chengxuan-He/CS2BridgeBuilder). Original project code is MIT-licensed; game and third-party assets retain their respective licenses.

## 简体中文

### 简介

Bridge Builder（桥梁建造者）让你在《Cities: Skylines II》中，将可用的道路或轨道与桥梁样式组合，创建适合自己城市的桥梁。选择桥面、挑选桥型，在等轴测 3D 预览中查看组合效果，再将生成的桥梁交给游戏道路建造工具进行铺设。对于支持双层结构的桥型，你可以分别选择上下层路网，并设置上下层方向是否相反。

### 主要功能

- 支持可用的悬索桥、斜拉桥、桁架拱桥、系杆拱桥和木廊桥等桥型；具体选择取决于已安装内容及当前支持的原型。
- 道路与轨道列表支持搜索和类型筛选，桥型列表支持搜索。
- 提供“创建”和“创建并建造”两个入口；桥梁已解锁时，后者会关闭模组界面并进入建造模式。
- 管理已创建桥梁：搜索、筛选、预览、修改显示名称、再次建造或删除。
- 生成桥梁归入 Bridge Builder 数据包，界面支持多语言。

### 使用方法

进入城市后，点击 Bridge Builder 图标，选择道路或轨道及桥型；使用双层桥时再选择另一层路网和方向选项。确认预览与桥梁名称后，点击“创建”保存桥梁，或点击“创建并建造”开始铺设。

- **如果触发了桥梁自检，请重启游戏。**
- 卸载桥梁建造者（Bridge Builder）不会自动删除已创建的桥梁。已保存的桥梁仍可正常铺设和使用。如需移除，请通过桥梁建造者的桥梁管理功能操作。

### 兼容性

- 如需使用自定义道路创建桥梁，请使用 Road Builder。桥梁生成并保存后，即使在 Road Builder 中删除其原始道路，仍可继续铺设已保存的桥梁。这不代表可以卸载桥梁所需的 DLC、内容模组或共享资产。

- 兼容 [Road Builder](https://mods.paradoxplaza.com/mods/87190/Windows) 与 [Bridge Expansion Pack](https://mods.paradoxplaza.com/mods/92245/Windows)。**两者均为可选模组，不是 Bridge Builder 的必需前置。** 仅在使用它们提供的道路或受支持桥型时才需要安装。
- 部分桥型需要对应 DLC 或内容模组。双层金门大桥来自 **Bridge Expansion Pack**，不是基础版单层金门大桥的双层模式。
- 兼容 [Node Controller](https://mods.paradoxplaza.com/mods/138523/Windows)。**Node Controller 为可选模组，不是必需前置。** 双层桥梁下层铁路接缝修复同时适配其几何计算流程，不覆盖其节点设置。
### 注意事项

- **若更新修复了旧版本生成桥梁时的问题，请删除受影响的旧桥梁并重新创建。** 更新模组不会自动重新生成已有桥梁资产。操作前请备份存档；删除桥梁也会移除地图中已铺设的对应实例。
- 设置页提供快捷键（默认 Ctrl+B）和“移除发展点数限制”选项。默认继承桥梁原型的解锁条件；启用该选项后不再因发展解锁条件阻止建造。
- 当前不支持生成开合桥、人行开合桥和升降桥。并非所有路网与桥型组合都受支持。
- **删除已创建桥梁会同时移除地图中使用该桥梁的实例。操作前请备份存档。** 不要把删除当作仅移除列表条目。
- 反馈问题时，请附上游戏与模组版本、桥型、所选路网、截图和相关模组日志。

源码：[GitHub](https://github.com/Chengxuan-He/CS2BridgeBuilder)。项目原创代码采用 MIT 协议；游戏及第三方资产保留各自授权。

## 繁體中文

### 簡介

Bridge Builder（橋梁建造者）讓你在《Cities: Skylines II》中，將可用的道路或軌道與橋梁樣式組合，建立適合自己城市的橋梁。選擇橋面、挑選橋型，在等軸測 3D 預覽中查看組合效果，再使用遊戲的道路建造工具鋪設生成的橋梁。對於支援雙層結構的橋型，你可以分別選擇上下層路網，並設定上下層方向是否相反。

### 主要功能

- 支援可用的懸索橋、斜張橋、桁架拱橋、繫桿拱橋與木廊橋等橋型；實際選項取決於已安裝內容及目前支援的原型。
- 道路與軌道清單支援搜尋及類型篩選，橋型清單支援搜尋。
- 提供「建立」與「建立並建造」兩個入口；橋梁已解鎖時，後者會關閉模組介面並進入建造模式。
- 管理已建立橋梁：搜尋、篩選、預覽、修改顯示名稱、再次建造或刪除。
- 生成橋梁歸入 Bridge Builder 資料包，介面支援多語言。

### 使用方法

進入城市後，點擊 Bridge Builder 圖示，選擇道路或軌道及橋型；使用雙層橋時，再選擇另一層路網和方向選項。確認預覽與橋梁名稱後，點擊建立按鈕儲存橋梁，或使用建立並建造功能開始鋪設。

- **如果觸發了橋梁自檢，請重新啟動遊戲。**
- 解除安裝橋梁建造者（Bridge Builder）不會自動刪除已建立的橋梁。已儲存的橋梁仍可正常鋪設及使用。如需移除，請透過橋梁建造者的橋梁管理功能操作。

### 相容性

- 如需使用自訂道路建立橋梁，請使用 Road Builder。橋梁生成並儲存後，即使在 Road Builder 中刪除其原始道路，仍可繼續鋪設已儲存的橋梁。這不代表可以解除安裝橋梁所需的 DLC、內容模組或共用資產。

- 相容 [Road Builder](https://mods.paradoxplaza.com/mods/87190/Windows) 與 [Bridge Expansion Pack](https://mods.paradoxplaza.com/mods/92245/Windows)。**兩者皆為可選模組，並非 Bridge Builder 的必要前置。** 僅在使用它們提供的道路或受支援橋型時才需要安裝。
- 部分橋型需要對應 DLC 或內容模組。雙層金門大橋來自 **Bridge Expansion Pack**，不是基礎版單層金門大橋的雙層模式。
- 相容 [Node Controller](https://mods.paradoxplaza.com/mods/138523/Windows)。**Node Controller 為可選模組，並非必要前置。** 雙層橋梁下層鐵路接縫修復同時支援其幾何計算流程，不覆寫其節點設定。
### 注意事項

- **若更新修復了舊版本生成橋梁時的問題，請刪除受影響的舊橋梁並重新建立。** 更新模組不會自動重新生成現有橋梁資產。操作前請備份存檔；刪除橋梁也會移除地圖中已鋪設的對應實例。
- 設定頁提供快捷鍵（預設 Ctrl+B）及「移除發展點數限制」選項。預設繼承橋梁原型的解鎖條件；啟用此選項後不再因發展解鎖條件阻止建造。
- 目前不支援生成開合橋、人行開合橋與升降橋。並非所有路網與橋型組合都受支援。
- **刪除已建立橋梁會同時移除地圖中使用該橋梁的實例。操作前請備份存檔。** 這不只是刪除清單項目。
- 回報問題時，請附上遊戲與模組版本、橋型、所選路網、截圖及相關模組日誌。

原始碼：[GitHub](https://github.com/Chengxuan-He/CS2BridgeBuilder)。專案原創程式碼採用 MIT 授權；遊戲及第三方資產保留各自授權。
