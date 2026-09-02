namespace BridgePrefabGenerator.Settings;

internal static partial class UiStringTables
{
    internal static UiStrings SimplifiedChinese() => new UiStrings
    {
        Title = "道路 Prefab 导出器",
        TabRoads = "道路",
        TabOptions = "选项",
        GroupStatus = "状态",
        GroupSelection = "选择",
        GroupActions = "操作",
        GroupRoads = "道路列表",
        DetailSummary = "宽度 ≈{0} m · 限速 {1}",
        DetailLastExport = "上次导出：{0}",
        GroupExport = "导出",
        GroupMaintenance = "维护",

        StatusNotExported = "未导出",
        StatusExported = "已导出",
        StatusOutdated = "上次导出后配置有变动",
        StatusExportedPendingRestart = "刚导出",
        StatusRemovedPendingRestart = "刚移除，需重启游戏",

        StateNoWorld = "未载入世界。请打开 Editor 以列出 Road Builder 道路。",
        StateGameplayBlocked = "已关闭「允许在 Editor 之外导出」。请打开 Editor，或开启该选项。",
        StateScanning = "正在等待 Road Builder 生成道路……",
        StateNoRoads = "没有找到 Road Builder 道路。请确认本 playset 已启用 Road Builder。",
        StateBrokenRoads = "已跳过 {0} 条道路：Road Builder 未能生成它们（配置缺失）。",
        StateNameConflicts = "已跳过 {0} 条道路：名称冲突。请在 Road Builder 中重命名后再导出。",
        StatePageIndicator = "第 {0}/{1} 页——显示第 {2}–{3} 条，共 {4} 条。",
        StateReady = "共 {0} 条道路：{1} 条已导出，{2} 条未导出，{3} 条有变动。",
        StateSelected = "已勾选 {0} 条。",
        StateRestartHint = "导出的道路已立即注册，无需重启。",
        StateReportHint = "完整报告：ModsData\\RoadPrefabExporter\\last-export-report.txt",
        OperationSummary = "上次操作：导出 {0}，移除 {1}，跳过 {2}，失败 {3}。",
        NothingSelected = "没有可执行的操作：未勾选任何道路。",
    }
        .Option(nameof(BridgeSetting.StatusText), "当前状态",
            "载入了包含 Road Builder 道路的世界后，道路才会列在下面。")
        .Option(nameof(BridgeSetting.RescanRoads), "重新扫描",
            "重新读取道路列表和导出状态。")
        .Option(nameof(BridgeSetting.ExportSelected), "导出勾选的道路",
            "把勾选的每条道路转换成原生 RoadPrefab 资产。使用前请先重启游戏。")
        .Option(nameof(BridgeSetting.ArmRemoval), "允许移除",
            "安全开关。移除会删除资产文件且无法撤销，因此在打开此开关前移除按钮不可用。")
        .Option(nameof(BridgeSetting.RemoveSelected), "移除勾选道路的已导出资产",
            "删除勾选道路对应的已导出资产。城市中已放置的该道路会损坏。")
        .Option(nameof(BridgeSetting.OverwriteExisting), "覆盖已存在的导出资产",
            "即使资产已存在，也重新导出该道路。")
        .Option(nameof(BridgeSetting.AllowGameplayExport), "允许在 Editor 之外导出",
            "默认关闭：在城市存档里写用户资产比在 Editor 中风险更高。")
        .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "同时移除无用依赖",
            "移除之后，删除已不再被任何导出道路引用的路段和路件资产。")
        .Option(nameof(BridgeSetting.EmbedIcons), "把缩略图嵌入资产",
            "让导出的道路自包含：分享给别人、或禁用本模组后缩略图依然显示。每条道路约多占 20-70 KB。关闭时缩略图由本模组的目录提供，只在你自己的机器上有效。");

    internal static UiStrings TraditionalChinese() => new UiStrings
    {
        Title = "道路 Prefab 匯出器",
        TabRoads = "道路",
        TabOptions = "選項",
        GroupStatus = "狀態",
        GroupSelection = "選擇",
        GroupActions = "操作",
        GroupRoads = "道路清單",
        DetailSummary = "寬度 ≈{0} m · 限速 {1}",
        DetailLastExport = "上次匯出：{0}",
        GroupExport = "匯出",
        GroupMaintenance = "維護",

        StatusNotExported = "未匯出",
        StatusExported = "已匯出",
        StatusOutdated = "上次匯出後設定有變動",
        StatusExportedPendingRestart = "剛匯出",
        StatusRemovedPendingRestart = "剛移除，需重新啟動遊戲",

        StateNoWorld = "未載入世界。請開啟 Editor 以列出 Road Builder 道路。",
        StateGameplayBlocked = "已關閉「允許在 Editor 之外匯出」。請開啟 Editor，或啟用該選項。",
        StateScanning = "正在等待 Road Builder 產生道路……",
        StateNoRoads = "找不到 Road Builder 道路。請確認此 playset 已啟用 Road Builder。",
        StateBrokenRoads = "已略過 {0} 條道路：Road Builder 未能產生它們（設定缺失）。",
        StateNameConflicts = "已略過 {0} 條道路：名稱衝突。請在 Road Builder 中重新命名後再匯出。",
        StatePageIndicator = "第 {0}/{1} 頁——顯示第 {2}–{3} 條，共 {4} 條。",
        StateReady = "共 {0} 條道路：{1} 條已匯出，{2} 條未匯出，{3} 條有變動。",
        StateSelected = "已勾選 {0} 條。",
        StateRestartHint = "匯出的道路已立即註冊，無需重新啟動。",
        StateReportHint = "完整報告：ModsData\\RoadPrefabExporter\\last-export-report.txt",
        OperationSummary = "上次操作：匯出 {0}，移除 {1}，略過 {2}，失敗 {3}。",
        NothingSelected = "沒有可執行的操作：未勾選任何道路。",
    }
        .Option(nameof(BridgeSetting.StatusText), "目前狀態",
            "載入包含 Road Builder 道路的世界後，道路才會列在下方。")
        .Option(nameof(BridgeSetting.RescanRoads), "重新掃描",
            "重新讀取道路清單與匯出狀態。")
        .Option(nameof(BridgeSetting.ExportSelected), "匯出勾選的道路",
            "把勾選的每條道路轉換成原生 RoadPrefab 資產。使用前請先重新啟動遊戲。")
        .Option(nameof(BridgeSetting.ArmRemoval), "允許移除",
            "安全開關。移除會刪除資產檔案且無法復原，因此在開啟此開關前移除按鈕無法使用。")
        .Option(nameof(BridgeSetting.RemoveSelected), "移除勾選道路的已匯出資產",
            "刪除勾選道路對應的已匯出資產。城市中已放置的該道路會損壞。")
        .Option(nameof(BridgeSetting.OverwriteExisting), "覆寫已存在的匯出資產",
            "即使資產已存在，也重新匯出該道路。")
        .Option(nameof(BridgeSetting.AllowGameplayExport), "允許在 Editor 之外匯出",
            "預設關閉：在城市存檔中寫入使用者資產比在 Editor 中風險更高。")
        .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "一併移除無用相依項",
            "移除之後，刪除已不再被任何匯出道路引用的路段與路件資產。")
        .Option(nameof(BridgeSetting.EmbedIcons), "將縮圖嵌入資產",
            "讓匯出的道路自我包含：分享給他人、或停用本模組後縮圖依然顯示。每條道路約多佔 20-70 KB。關閉時縮圖由本模組的資料夾提供，只在你自己的電腦上有效。");
}
