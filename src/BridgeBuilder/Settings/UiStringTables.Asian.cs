namespace BridgeBuilder.Settings;

internal static partial class UiStringTables
{
    internal static UiStrings Japanese() => new UiStrings
    {
        Title = "道路プレハブ エクスポーター",
        TabRoads = "道路",
        TabOptions = "設定",
        GroupStatus = "状態",
        GroupSelection = "選択",
        GroupActions = "操作",
        GroupRoads = "道路一覧",
        DetailSummary = "幅 ~{0} m・制限速度 {1}",
        DetailLastExport = "前回のエクスポート: {0}",
        GroupExport = "エクスポート",
        GroupMaintenance = "メンテナンス",

        StatusNotExported = "未エクスポート",
        StatusExported = "エクスポート済み",
        StatusOutdated = "前回のエクスポート以降に変更あり",
        StatusExportedPendingRestart = "今エクスポートしました",
        StatusRemovedPendingRestart = "今削除しました - 再起動が必要",

        StateNoWorld = "ワールドが読み込まれていません。エディターを開くと Road Builder の道路が一覧表示されます。",
        StateGameplayBlocked = "エディター外でのエクスポートは無効です。エディターを開くか、「エディター外でのエクスポートを許可」を有効にしてください。",
        StateScanning = "Road Builder が道路を生成し終えるのを待っています...",
        StateNoRoads = "Road Builder の道路が見つかりません。このプレイセットで Road Builder が有効か確認してください。",
        StateBrokenRoads = "{0} 件の道路をスキップしました: Road Builder が生成できませんでした (構成が見つかりません)。",
        StateNameConflicts = "{0} 件の道路をスキップしました: 名前が衝突しています。Road Builder で改名してください。",
        StatePageIndicator = "{1} ページ中 {0} ページ目 - {4} 件中 {2}-{3} 件を表示。",
        StateReady = "道路 {0} 件: エクスポート済み {1}、未エクスポート {2}、変更あり {3}。",
        StateSelected = "{0} 件を選択中。",
        StateRestartHint = "エクスポートした道路はすぐに登録されます。再起動は不要です。",
        StateReportHint = "詳細レポート: ModsData\\RoadPrefabExporter\\last-export-report.txt",
        OperationSummary = "前回の実行: エクスポート {0}、削除 {1}、スキップ {2}、失敗 {3}。",
        NothingSelected = "実行する対象がありません: 道路が 1 つも選択されていません。",
    }
        .Option(nameof(BridgeSetting.StatusText), "現在の状態",
            "Road Builder の道路を含むワールドを読み込むと、下に道路が一覧表示されます。")
        .Option(nameof(BridgeSetting.RescanRoads), "再スキャン",
            "道路一覧とエクスポート状態を読み込み直します。")
        .Option(nameof(BridgeSetting.ExportSelected), "選択した道路をエクスポート",
            "選択した各道路をネイティブの RoadPrefab アセットに変換します。使用前にゲームを再起動してください。")
        .Option(nameof(BridgeSetting.ArmRemoval), "削除を許可",
            "安全装置です。削除はアセットファイルを消去し取り消せないため、これを有効にするまで削除ボタンは使えません。")
        .Option(nameof(BridgeSetting.RemoveSelected), "選択した道路のエクスポートを削除",
            "エクスポート済みアセットを削除します。都市にすでに設置済みの道路は壊れます。")
        .Option(nameof(BridgeSetting.OverwriteExisting), "既存のエクスポートを上書き",
            "アセットがすでに存在していても道路を再エクスポートします。")
        .Option(nameof(BridgeSetting.AllowGameplayExport), "エディター外でのエクスポートを許可",
            "既定では無効: 都市のセーブからユーザーアセットを書き込むのはエディターより危険です。")
        .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "未使用の依存アセットも削除",
            "削除後、残ったどのエクスポート道路からも参照されない区間・パーツのアセットを消します。")
        .Option(nameof(BridgeSetting.EmbedIcons), "サムネイルをアセットに埋め込む",
            "エクスポートした道路を自己完結させ、共有先や本 MOD を無効にした状態でもサムネイルが表示されます。道路 1 件あたり約 20〜70 KB 増えます。無効の場合、サムネイルは本 MOD のフォルダーから配信され、自分の PC でのみ表示されます。");

    internal static UiStrings Korean() => new UiStrings
    {
        Title = "도로 프리팹 익스포터",
        TabRoads = "도로",
        TabOptions = "설정",
        GroupStatus = "상태",
        GroupSelection = "선택",
        GroupActions = "작업",
        GroupRoads = "도로 목록",
        DetailSummary = "폭 ~{0} m · 제한속도 {1}",
        DetailLastExport = "마지막 내보내기: {0}",
        GroupExport = "내보내기",
        GroupMaintenance = "관리",

        StatusNotExported = "내보내지 않음",
        StatusExported = "내보냄",
        StatusOutdated = "마지막 내보내기 이후 변경됨",
        StatusExportedPendingRestart = "방금 내보냄",
        StatusRemovedPendingRestart = "방금 제거함 - 재시작 필요",

        StateNoWorld = "불러온 월드가 없습니다. 에디터를 열면 Road Builder 도로가 표시됩니다.",
        StateGameplayBlocked = "에디터 밖에서의 내보내기가 꺼져 있습니다. 에디터를 열거나 \"에디터 밖에서 내보내기 허용\"을 켜세요.",
        StateScanning = "Road Builder가 도로 생성을 마칠 때까지 기다리는 중...",
        StateNoRoads = "Road Builder 도로를 찾지 못했습니다. 이 플레이세트에서 Road Builder가 켜져 있는지 확인하세요.",
        StateBrokenRoads = "{0}개 도로를 건너뛰었습니다: Road Builder가 생성하지 못했습니다(구성 없음).",
        StateNameConflicts = "{0}개 도로를 건너뛰었습니다: 이름이 충돌합니다. Road Builder에서 이름을 바꾸세요.",
        StatePageIndicator = "{1}페이지 중 {0}페이지 - 전체 {4}개 중 {2}-{3}개 표시.",
        StateReady = "도로 {0}개: 내보냄 {1}, 안 함 {2}, 변경됨 {3}.",
        StateSelected = "{0}개 선택됨.",
        StateRestartHint = "내보낸 도로는 즉시 등록됩니다. 재시작이 필요 없습니다.",
        StateReportHint = "전체 보고서: ModsData\\RoadPrefabExporter\\last-export-report.txt",
        OperationSummary = "마지막 실행: 내보냄 {0}, 제거 {1}, 건너뜀 {2}, 실패 {3}.",
        NothingSelected = "할 작업이 없습니다: 선택된 도로가 없습니다.",
    }
        .Option(nameof(BridgeSetting.StatusText), "현재 상태",
            "Road Builder 도로가 있는 월드를 불러오면 아래에 도로가 나열됩니다.")
        .Option(nameof(BridgeSetting.RescanRoads), "다시 검색",
            "도로 목록과 내보내기 상태를 다시 읽습니다.")
        .Option(nameof(BridgeSetting.ExportSelected), "선택한 도로 내보내기",
            "선택한 각 도로를 기본 RoadPrefab 에셋으로 변환합니다. 사용 전에 게임을 재시작하세요.")
        .Option(nameof(BridgeSetting.ArmRemoval), "제거 허용",
            "안전장치입니다. 제거는 에셋 파일을 삭제하며 되돌릴 수 없으므로, 이 항목을 켜기 전까지 제거 버튼은 비활성 상태입니다.")
        .Option(nameof(BridgeSetting.RemoveSelected), "선택한 도로의 내보내기 제거",
            "내보낸 에셋을 삭제합니다. 도시에 이미 배치된 해당 도로는 손상됩니다.")
        .Option(nameof(BridgeSetting.OverwriteExisting), "기존 내보내기 덮어쓰기",
            "에셋이 이미 있어도 도로를 다시 내보냅니다.")
        .Option(nameof(BridgeSetting.AllowGameplayExport), "에디터 밖에서 내보내기 허용",
            "기본값은 꺼짐: 도시 세이브에서 사용자 에셋을 기록하는 것은 에디터보다 위험합니다.")
        .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "사용하지 않는 종속 항목도 제거",
            "제거 후, 남아 있는 어떤 내보낸 도로도 참조하지 않는 구간·조각 에셋을 삭제합니다.")
        .Option(nameof(BridgeSetting.EmbedIcons), "썸네일을 에셋에 포함",
            "내보낸 도로를 자체 완결형으로 만들어 공유하거나 이 모드를 꺼도 썸네일이 표시됩니다. 도로당 약 20~70 KB가 늘어납니다. 끄면 썸네일은 이 모드 폴더에서 제공되며 본인 PC에서만 표시됩니다.");
}
