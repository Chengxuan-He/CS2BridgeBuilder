namespace BridgePrefabGenerator.Settings;

/// <summary>
/// The bridge specific half of every translation.
///
/// The road tables next to this file were taken over from the road exporter, because the status
/// panel and the asset writing are the same feature. Everything this mod adds on top - the deck
/// pickers, the style picker, its own title and report path - lives here instead of being
/// interleaved into six table files, so that adding an option means editing one place per language
/// rather than twelve. Later calls to Option overwrite earlier ones, which is how the reworded
/// entries below take effect.
/// </summary>
internal static partial class UiStringTables
{
    internal static UiStrings WithBridgeText(UiStrings text, string localeId) => localeId switch
    {
        "de-DE" => German(text),
        "es-ES" => Spanish(text),
        "fr-FR" => French(text),
        "it-IT" => Italian(text),
        "ja-JP" => Japanese(text),
        "ko-KR" => Korean(text),
        "pl-PL" => Polish(text),
        "pt-BR" => Portuguese(text),
        "ru-RU" => Russian(text),
        "zh-HANS" => SimplifiedChinese(text),
        "zh-HANT" => TraditionalChinese(text),
        _ => English(text),
    };

    private static UiStrings English(UiStrings text)
    {
        text.Title = "Bridge Prefab Generator";
        text.TabBridge = "Bridge";
        text.GroupDeck = "Upper deck";
        text.GroupStyle = "Style";
        text.GroupLowerDeck = "Lower deck (experimental)";
        text.StateNoStyles = "No bridge style is available. Suspension, extradosed and truss arch bridges come with the Bridges & Ports content.";
        text.StateStyleSource = "Style: {0} (from {1}).";
        text.StateDoubleDeckExperimental = "The lower deck is created with the bridge and cannot be edited or connected on its own.";
        text.OptionDonorBuildStyle = "Keep the style's own";
        text.OptionLowerDeckNone = "None - single deck";
        text.OptionNoDeckChosen = "Nothing chosen";
        text.StateNoUpperDeck = "Pick the road the bridge should carry.";
        text.StateUpperDeck = "Upper deck: {0} ({1} m wide).";
        text.StateStyleFit = "Fitted from {0} ({1} m) to {2} m.";
        text.StateLowerDeck = "Lower deck: {0}, {1} m below, {2}.";
        text.StateDirectionOpposite = "running the opposite way";
        text.StateDirectionSame = "running the same way";
        text.StateExportName = "Will be generated as: {0}";
        text.StateReportHint = "Full report: ModsData\\BridgePrefabGenerator\\last-export-report.txt";
        return text
            .DeckKind("RoadBuilder", "Road Builder")
            .DeckKind("Road", "Road")
            .DeckKind("Train", "Train")
            .DeckKind("Subway", "Subway")
            .DeckKind("Tram", "Tram")
            .Option(nameof(BridgeSetting.StatusText), "What will be generated")
            .Option(nameof(BridgeSetting.BridgeName), "Bridge name",
                "The name the generated bridge is saved under. Regenerated whenever the road, style or second deck changes, so a name left alone always describes what will be built. Type over it to keep your own.")
            .Option(nameof(BridgeSetting.UpperDeckId), "Road to convert",
                "The road the bridge carries. Any registered road can be used, including ones exported earlier.")
            .Option(nameof(BridgeSetting.RescanRoads), "Rescan",
                "Read the available roads, tracks and bridge styles again.")
            .Option(nameof(BridgeSetting.ExportSelected), "Generate bridge",
                "Builds one bridge from the choices above. One at a time: a bridge is a pairing of decks, not something to apply to a list.")
            .Option(nameof(BridgeSetting.ArmRemoval), "Allow removal",
                "Safety catch. Removal deletes asset files and cannot be undone, so the removal button stays disabled until this is on.")
            .Option(nameof(BridgeSetting.RemoveSelected), "Remove generated bridge",
                "Deletes the bridge generated from the road above, and its lower deck if it has one.")
            .Option(nameof(BridgeSetting.BridgeStyleId), "Bridge style",
                "Which bridge the look is taken from. A style with nothing installed to provide it is marked.")
            .Option(nameof(BridgeSetting.BuildStyleOverride), "Build style",
                "Elevated is an ordinary bridge deck on pillars, raised sits on an embankment, quay hugs the shore.")
            .Option(nameof(BridgeSetting.LowerDeckId), "Lower deck",
                "What hangs under the bridge: nothing, another road, or a train, subway or tram track. Choosing the same road as above gives two road decks.")
            .Option(nameof(BridgeSetting.LowerDeckOpposite), "Lower deck runs the opposite way",
                "Inverts the lower deck, so the two decks carry opposite directions.")
            .Option(nameof(BridgeSetting.DeckSpacing), "Deck spacing",
                "Vertical distance between the two decks.")
            .Option(nameof(BridgeSetting.OverwriteExisting), "Overwrite existing",
                "Generate a bridge again even when its asset already exists.")
            .Option(nameof(BridgeSetting.AllowGameplayExport), "Allow generating outside the Editor",
                "Off by default: writing user assets from a city save is riskier than doing it in the Editor.")
            .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "Also remove unused dependencies",
                "After a removal, delete generated net sections and pieces that nothing else references.")
            .Option(nameof(BridgeSetting.EmbedIcons), "Embed thumbnails into the assets",
                "Makes a generated bridge self-contained, so its thumbnail still works when the asset is shared or this mod is disabled.");
    }

    private static UiStrings SimplifiedChinese(UiStrings text)
    {
        text.Title = "桥梁 Prefab 生成器";
        text.TabBridge = "桥梁";
        text.GroupDeck = "上层";
        text.GroupStyle = "样式";
        text.GroupLowerDeck = "下层（实验功能）";
        text.StateNoStyles = "没有可用的桥梁样式。悬索桥、矮塔斜拉桥、桁架拱桥来自 Bridges & Ports 内容。";
        text.StateStyleSource = "样式：{0}（来自 {1}）。";
        text.StateDoubleDeckExperimental = "下层随桥梁一同生成，无法单独编辑或连接。";
        text.OptionDonorBuildStyle = "沿用该样式自带的";
        text.OptionLowerDeckNone = "无 — 单层桥";
        text.OptionNoDeckChosen = "尚未选择";
        text.StateNoUpperDeck = "请选择桥梁承载的道路。";
        text.StateUpperDeck = "上层：{0}（宽 {1} 米）。";
        text.StateStyleFit = "以 {0}（{1} 米）适配到 {2} 米。";
        text.StateLowerDeck = "下层：{0}，位于下方 {1} 米，{2}。";
        text.StateDirectionOpposite = "方向相反";
        text.StateDirectionSame = "方向相同";
        text.StateExportName = "将生成为：{0}";
        text.StateReportHint = "完整报告：ModsData\\BridgePrefabGenerator\\last-export-report.txt";
        return text
            .DeckKind("RoadBuilder", "Road Builder")
            .DeckKind("Road", "道路")
            .DeckKind("Train", "铁路")
            .DeckKind("Subway", "地铁")
            .DeckKind("Tram", "电车")
            .Option(nameof(BridgeSetting.StatusText), "将要生成的内容")
            .Option(nameof(BridgeSetting.BridgeName), "桥梁名称",
                "生成的桥梁保存时使用的名称。每次更改道路、样式或下层甲板时都会重新生成，因此未改动的名称始终描述将要生成的桥梁。可直接输入以使用自定义名称。")
            .Option(nameof(BridgeSetting.UpperDeckId), "要转换的道路",
                "桥梁承载的道路。任何已注册的道路都可以使用，包括此前导出的道路。")
            .Option(nameof(BridgeSetting.RescanRoads), "重新扫描",
                "重新读取可用的道路、轨道和桥梁样式。")
            .Option(nameof(BridgeSetting.ExportSelected), "生成桥梁",
                "按以上选择生成一座桥。一次一座：桥是上下层的配对，无法套用到一个列表上。")
            .Option(nameof(BridgeSetting.ArmRemoval), "允许移除",
                "安全开关。移除会删除资产文件且无法撤销，因此在打开之前移除按钮保持禁用。")
            .Option(nameof(BridgeSetting.RemoveSelected), "移除已生成的桥梁",
                "删除由上方道路生成的桥梁，若有下层则一并删除。")
            .Option(nameof(BridgeSetting.BridgeStyleId), "桥梁样式",
                "外观取自哪一座桥。本机没有内容提供的样式会被标注。")
            .Option(nameof(BridgeSetting.BuildStyleOverride), "建造方式",
                "高架为立柱托起的常规桥面，路堤沿土坡铺设，码头贴岸修建。")
            .Option(nameof(BridgeSetting.LowerDeckId), "下层内容",
                "桥面下方挂什么：不挂、另一条道路，或铁路、地铁、电车轨道。选择与上层相同的道路即为上下两层道路。")
            .Option(nameof(BridgeSetting.LowerDeckOpposite), "下层方向相反",
                "反转下层，使上下两层行车方向相反。")
            .Option(nameof(BridgeSetting.DeckSpacing), "层间距",
                "两层桥面之间的垂直距离。")
            .Option(nameof(BridgeSetting.OverwriteExisting), "覆盖已存在的资产",
                "即使资产已存在也重新生成。")
            .Option(nameof(BridgeSetting.AllowGameplayExport), "允许在编辑器之外生成",
                "默认关闭：在城市存档中写入用户资产比在编辑器中风险更高。")
            .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "同时移除未使用的依赖",
                "移除之后，删除已无任何引用的生成路段与路块。")
            .Option(nameof(BridgeSetting.EmbedIcons), "将缩略图嵌入资产",
                "使生成的桥梁自包含，分享资产或禁用本模组后缩略图仍可用。");
    }

    private static UiStrings TraditionalChinese(UiStrings text)
    {
        text.Title = "橋樑 Prefab 產生器";
        text.TabBridge = "橋樑";
        text.GroupDeck = "上層";
        text.GroupStyle = "樣式";
        text.GroupLowerDeck = "下層（實驗功能）";
        text.StateNoStyles = "沒有可用的橋樑樣式。懸索橋、矮塔斜張橋、桁架拱橋來自 Bridges & Ports 內容。";
        text.StateStyleSource = "樣式：{0}（來自 {1}）。";
        text.StateDoubleDeckExperimental = "下層隨橋樑一同產生，無法單獨編輯或連接。";
        text.OptionDonorBuildStyle = "沿用該樣式自帶的";
        text.OptionLowerDeckNone = "無 — 單層橋";
        text.OptionNoDeckChosen = "尚未選擇";
        text.StateNoUpperDeck = "請選擇橋樑承載的道路。";
        text.StateUpperDeck = "上層：{0}（寬 {1} 公尺）。";
        text.StateStyleFit = "以 {0}（{1} 公尺）配合到 {2} 公尺。";
        text.StateLowerDeck = "下層：{0}，位於下方 {1} 公尺，{2}。";
        text.StateDirectionOpposite = "方向相反";
        text.StateDirectionSame = "方向相同";
        text.StateExportName = "將產生為：{0}";
        text.StateReportHint = "完整報告：ModsData\\BridgePrefabGenerator\\last-export-report.txt";
        return text
            .DeckKind("RoadBuilder", "Road Builder")
            .DeckKind("Road", "道路")
            .DeckKind("Train", "鐵路")
            .DeckKind("Subway", "地鐵")
            .DeckKind("Tram", "電車")
            .Option(nameof(BridgeSetting.StatusText), "將要產生的內容")
            .Option(nameof(BridgeSetting.BridgeName), "橋樑名稱",
                "產生的橋樑儲存時使用的名稱。每次變更道路、樣式或下層甲板時都會重新產生，因此未改動的名稱始終描述將要產生的橋樑。可直接輸入以使用自訂名稱。")
            .Option(nameof(BridgeSetting.UpperDeckId), "要轉換的道路",
                "橋樑承載的道路。任何已註冊的道路都可以使用，包括先前匯出的道路。")
            .Option(nameof(BridgeSetting.RescanRoads), "重新掃描",
                "重新讀取可用的道路、軌道與橋樑樣式。")
            .Option(nameof(BridgeSetting.ExportSelected), "產生橋樑",
                "依以上選擇產生一座橋。一次一座：橋是上下層的配對，無法套用到清單上。")
            .Option(nameof(BridgeSetting.ArmRemoval), "允許移除",
                "安全開關。移除會刪除資產檔案且無法復原，因此在開啟之前移除按鈕保持停用。")
            .Option(nameof(BridgeSetting.RemoveSelected), "移除已產生的橋樑",
                "刪除由上方道路產生的橋樑，若有下層則一併刪除。")
            .Option(nameof(BridgeSetting.BridgeStyleId), "橋樑樣式",
                "外觀取自哪一座橋。本機沒有內容提供的樣式會被標註。")
            .Option(nameof(BridgeSetting.BuildStyleOverride), "建造方式",
                "高架為立柱撐起的常規橋面，路堤沿土坡鋪設，碼頭貼岸修建。")
            .Option(nameof(BridgeSetting.LowerDeckId), "下層內容",
                "橋面下方掛什麼：不掛、另一條道路，或鐵路、地鐵、電車軌道。選擇與上層相同的道路即為上下兩層道路。")
            .Option(nameof(BridgeSetting.LowerDeckOpposite), "下層方向相反",
                "反轉下層，使上下兩層行車方向相反。")
            .Option(nameof(BridgeSetting.DeckSpacing), "層間距",
                "兩層橋面之間的垂直距離。")
            .Option(nameof(BridgeSetting.OverwriteExisting), "覆蓋已存在的資產",
                "即使資產已存在也重新產生。")
            .Option(nameof(BridgeSetting.AllowGameplayExport), "允許在編輯器之外產生",
                "預設關閉：在城市存檔中寫入使用者資產比在編輯器中風險更高。")
            .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "同時移除未使用的相依項",
                "移除之後，刪除已無任何參照的產生路段與路塊。")
            .Option(nameof(BridgeSetting.EmbedIcons), "將縮圖嵌入資產",
                "使產生的橋樑自我包含，分享資產或停用本模組後縮圖仍可用。");
    }

    private static UiStrings Japanese(UiStrings text)
    {
        text.Title = "橋 Prefab ジェネレーター";
        text.TabBridge = "橋";
        text.GroupDeck = "上層";
        text.GroupStyle = "スタイル";
        text.GroupLowerDeck = "下層（実験的）";
        text.StateNoStyles = "利用できる橋のスタイルがありません。吊り橋・エクストラドーズド橋・トラスアーチ橋は Bridges & Ports のコンテンツに含まれます。";
        text.StateStyleSource = "スタイル: {0}（{1} 由来）。";
        text.StateDoubleDeckExperimental = "下層は橋と一緒に生成され、単独では編集も接続もできません。";
        text.OptionDonorBuildStyle = "スタイル本来の設定を使う";
        text.OptionLowerDeckNone = "なし — 一層のみ";
        text.OptionNoDeckChosen = "未選択";
        text.StateNoUpperDeck = "橋が通す道路を選んでください。";
        text.StateUpperDeck = "上層: {0}（幅 {1} m）。";
        text.StateStyleFit = "{0}（{1} m）を {2} m に合わせます。";
        text.StateLowerDeck = "下層: {0}、{1} m 下、{2}。";
        text.StateDirectionOpposite = "逆方向";
        text.StateDirectionSame = "同方向";
        text.StateExportName = "生成名: {0}";
        text.StateReportHint = "詳細レポート: ModsData\\BridgePrefabGenerator\\last-export-report.txt";
        return text
            .DeckKind("RoadBuilder", "Road Builder")
            .DeckKind("Road", "道路")
            .DeckKind("Train", "鉄道")
            .DeckKind("Subway", "地下鉄")
            .DeckKind("Tram", "トラム")
            .Option(nameof(BridgeSetting.StatusText), "生成される内容")
            .Option(nameof(BridgeSetting.BridgeName), "橋の名前",
                "生成される橋の保存名。道路・スタイル・下層デッキを変更するたびに再生成されるため、そのままにしておけば常に生成される橋を表します。上書き入力すれば独自の名前を使えます。")
            .Option(nameof(BridgeSetting.UpperDeckId), "変換する道路",
                "橋が通す道路です。以前に書き出したものを含め、登録済みの道路ならどれでも使えます。")
            .Option(nameof(BridgeSetting.RescanRoads), "再スキャン",
                "利用できる道路・軌道・橋のスタイルを読み直します。")
            .Option(nameof(BridgeSetting.ExportSelected), "橋を生成",
                "上の選択から橋を 1 つ作ります。一度に 1 つ：橋は上下層の組み合わせであり、一覧に適用できるものではありません。")
            .Option(nameof(BridgeSetting.ArmRemoval), "削除を許可",
                "安全装置。削除はアセットファイルを消し、取り消せません。オンにするまで削除ボタンは無効のままです。")
            .Option(nameof(BridgeSetting.RemoveSelected), "生成した橋を削除",
                "上の道路から生成した橋を削除します。下層があれば一緒に削除します。")
            .Option(nameof(BridgeSetting.BridgeStyleId), "橋のスタイル",
                "どの橋から外観を取るか。提供するものが導入されていないスタイルには印が付きます。")
            .Option(nameof(BridgeSetting.BuildStyleOverride), "架設方式",
                "高架は橋脚に載る通常の橋、盛土は土手の上、護岸は岸に沿って作られます。")
            .Option(nameof(BridgeSetting.LowerDeckId), "下層の内容",
                "橋の下に何を吊るすか。なし、別の道路、または鉄道・地下鉄・トラムの軌道。上層と同じ道路を選ぶと二層の道路橋になります。")
            .Option(nameof(BridgeSetting.LowerDeckOpposite), "下層を逆方向にする",
                "下層を反転させ、上下が互いに逆方向になるようにします。")
            .Option(nameof(BridgeSetting.DeckSpacing), "層の間隔",
                "二つの層の垂直方向の間隔です。")
            .Option(nameof(BridgeSetting.OverwriteExisting), "既存のアセットを上書き",
                "アセットが既にあっても作り直します。")
            .Option(nameof(BridgeSetting.AllowGameplayExport), "エディター外での生成を許可",
                "既定はオフ：都市のセーブからユーザーアセットを書き出すのはエディターより危険です。")
            .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "未使用の依存も削除",
                "削除後、どこからも参照されなくなった生成済みのセクションとピースを消します。")
            .Option(nameof(BridgeSetting.EmbedIcons), "サムネイルをアセットに埋め込む",
                "生成した橋を自己完結させ、共有時や本 MOD 無効時でもサムネイルが表示されます。");
    }

    private static UiStrings Korean(UiStrings text)
    {
        text.Title = "교량 Prefab 생성기";
        text.TabBridge = "교량";
        text.GroupDeck = "상층";
        text.GroupStyle = "스타일";
        text.GroupLowerDeck = "하층 (실험적)";
        text.StateNoStyles = "사용할 수 있는 교량 스타일이 없습니다. 현수교, 엑스트라도즈교, 트러스 아치교는 Bridges & Ports 콘텐츠에 들어 있습니다.";
        text.StateStyleSource = "스타일: {0} ({1} 출처).";
        text.StateDoubleDeckExperimental = "하층은 교량과 함께 생성되며 따로 편집하거나 연결할 수 없습니다.";
        text.OptionDonorBuildStyle = "스타일 본래 설정 사용";
        text.OptionLowerDeckNone = "없음 — 단층";
        text.OptionNoDeckChosen = "선택 안 함";
        text.StateNoUpperDeck = "교량이 지나갈 도로를 고르세요.";
        text.StateUpperDeck = "상층: {0} (너비 {1} m).";
        text.StateStyleFit = "{0} ({1} m)을(를) {2} m에 맞춥니다.";
        text.StateLowerDeck = "하층: {0}, {1} m 아래, {2}.";
        text.StateDirectionOpposite = "반대 방향";
        text.StateDirectionSame = "같은 방향";
        text.StateExportName = "생성될 이름: {0}";
        text.StateReportHint = "전체 보고서: ModsData\\BridgePrefabGenerator\\last-export-report.txt";
        return text
            .DeckKind("RoadBuilder", "Road Builder")
            .DeckKind("Road", "도로")
            .DeckKind("Train", "철도")
            .DeckKind("Subway", "지하철")
            .DeckKind("Tram", "트램")
            .Option(nameof(BridgeSetting.StatusText), "생성될 내용")
            .Option(nameof(BridgeSetting.BridgeName), "교량 이름",
                "생성된 교량이 저장되는 이름입니다. 도로, 스타일, 하부 데크를 변경할 때마다 다시 생성되므로 그대로 두면 항상 생성될 교량을 나타냅니다. 직접 입력하면 원하는 이름을 쓸 수 있습니다.")
            .Option(nameof(BridgeSetting.UpperDeckId), "변환할 도로",
                "교량이 지나갈 도로입니다. 이전에 내보낸 것을 포함해 등록된 도로면 무엇이든 쓸 수 있습니다.")
            .Option(nameof(BridgeSetting.RescanRoads), "다시 검색",
                "사용할 수 있는 도로, 궤도, 교량 스타일을 다시 읽습니다.")
            .Option(nameof(BridgeSetting.ExportSelected), "교량 생성",
                "위 선택으로 교량 하나를 만듭니다. 한 번에 하나씩: 교량은 상하층의 짝이며 목록에 적용할 수 있는 것이 아닙니다.")
            .Option(nameof(BridgeSetting.ArmRemoval), "제거 허용",
                "안전장치. 제거는 에셋 파일을 지우며 되돌릴 수 없으므로, 켜기 전까지 제거 버튼은 비활성 상태입니다.")
            .Option(nameof(BridgeSetting.RemoveSelected), "생성된 교량 제거",
                "위 도로에서 생성한 교량을 삭제합니다. 하층이 있으면 함께 삭제합니다.")
            .Option(nameof(BridgeSetting.BridgeStyleId), "교량 스타일",
                "어느 교량에서 외형을 가져올지 정합니다. 제공하는 콘텐츠가 없는 스타일은 표시됩니다.")
            .Option(nameof(BridgeSetting.BuildStyleOverride), "건설 방식",
                "고가는 교각 위의 일반 상판, 성토는 둑 위, 안벽은 물가를 따라 세웁니다.")
            .Option(nameof(BridgeSetting.LowerDeckId), "하층 내용",
                "교량 아래에 무엇을 매달지. 없음, 다른 도로, 또는 철도·지하철·트램 궤도. 상층과 같은 도로를 고르면 복층 도로교가 됩니다.")
            .Option(nameof(BridgeSetting.LowerDeckOpposite), "하층을 반대 방향으로",
                "하층을 뒤집어 위아래가 서로 반대 방향이 되게 합니다.")
            .Option(nameof(BridgeSetting.DeckSpacing), "층 간격",
                "두 상판 사이의 수직 거리입니다.")
            .Option(nameof(BridgeSetting.OverwriteExisting), "기존 에셋 덮어쓰기",
                "에셋이 이미 있어도 다시 생성합니다.")
            .Option(nameof(BridgeSetting.AllowGameplayExport), "에디터 밖에서도 생성 허용",
                "기본은 꺼짐: 도시 저장본에서 사용자 에셋을 쓰는 것은 에디터보다 위험합니다.")
            .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "사용하지 않는 의존 항목도 제거",
                "제거 후, 아무것도 참조하지 않는 생성된 구간과 조각을 삭제합니다.")
            .Option(nameof(BridgeSetting.EmbedIcons), "썸네일을 에셋에 포함",
                "생성한 교량을 자체 완결형으로 만들어, 공유하거나 이 모드를 꺼도 썸네일이 유지됩니다.");
    }

    private static UiStrings German(UiStrings text)
    {
        text.Title = "Brücken-Prefab-Generator";
        text.TabBridge = "Brücke";
        text.GroupDeck = "Obere Ebene";
        text.GroupStyle = "Stil";
        text.GroupLowerDeck = "Untere Ebene (experimentell)";
        text.StateNoStyles = "Kein Brückenstil verfügbar. Hänge-, Extradosed- und Fachwerkbogenbrücken kommen mit den Inhalten von Bridges & Ports.";
        text.StateStyleSource = "Stil: {0} (aus {1}).";
        text.StateDoubleDeckExperimental = "Die untere Ebene entsteht mit der Brücke und lässt sich nicht einzeln bearbeiten oder anschließen.";
        text.OptionDonorBuildStyle = "Vorgabe des Stils behalten";
        text.OptionLowerDeckNone = "Keine – einstöckig";
        text.OptionNoDeckChosen = "Nichts gewählt";
        text.StateNoUpperDeck = "Wähle die Straße, die die Brücke tragen soll.";
        text.StateUpperDeck = "Obere Ebene: {0} ({1} m breit).";
        text.StateStyleFit = "Von {0} ({1} m) auf {2} m angepasst.";
        text.StateLowerDeck = "Untere Ebene: {0}, {1} m darunter, {2}.";
        text.StateDirectionOpposite = "in Gegenrichtung";
        text.StateDirectionSame = "in gleicher Richtung";
        text.StateExportName = "Wird erzeugt als: {0}";
        text.StateReportHint = "Vollständiger Bericht: ModsData\\BridgePrefabGenerator\\last-export-report.txt";
        return text
            .DeckKind("RoadBuilder", "Road Builder")
            .DeckKind("Road", "Straße")
            .DeckKind("Train", "Zug")
            .DeckKind("Subway", "U-Bahn")
            .DeckKind("Tram", "Tram")
            .Option(nameof(BridgeSetting.StatusText), "Was erzeugt wird")
            .Option(nameof(BridgeSetting.BridgeName), "Brückenname",
                "Der Name, unter dem die erzeugte Brücke gespeichert wird. Wird bei jeder Änderung von Straße, Stil oder unterem Deck neu erzeugt. Überschreiben, um einen eigenen Namen zu behalten.")
            .Option(nameof(BridgeSetting.UpperDeckId), "Zu wandelnde Straße",
                "Die Straße, die die Brücke trägt. Jede registrierte Straße ist möglich, auch früher exportierte.")
            .Option(nameof(BridgeSetting.RescanRoads), "Neu einlesen",
                "Liest die verfügbaren Straßen, Gleise und Brückenstile erneut.")
            .Option(nameof(BridgeSetting.ExportSelected), "Brücke erzeugen",
                "Baut eine Brücke aus den Angaben oben. Immer nur eine: eine Brücke ist ein Paar aus Ebenen, nichts, was man auf eine Liste anwendet.")
            .Option(nameof(BridgeSetting.ArmRemoval), "Entfernen erlauben",
                "Sicherung. Das Entfernen löscht Asset-Dateien unwiderruflich, daher bleibt der Knopf bis dahin deaktiviert.")
            .Option(nameof(BridgeSetting.RemoveSelected), "Erzeugte Brücke entfernen",
                "Löscht die aus der obigen Straße erzeugte Brücke, samt unterer Ebene falls vorhanden.")
            .Option(nameof(BridgeSetting.BridgeStyleId), "Brückenstil",
                "Von welcher Brücke das Aussehen übernommen wird. Ein Stil ohne installierte Grundlage wird markiert.")
            .Option(nameof(BridgeSetting.BuildStyleOverride), "Bauweise",
                "Hochgelegt ist ein normales Brückendeck auf Pfeilern, aufgeschüttet liegt auf einem Damm, Kai folgt dem Ufer.")
            .Option(nameof(BridgeSetting.LowerDeckId), "Untere Ebene",
                "Was unter der Brücke hängt: nichts, eine andere Straße, oder ein Zug-, U-Bahn- oder Tramgleis. Dieselbe Straße wie oben ergibt zwei Fahrbahnen.")
            .Option(nameof(BridgeSetting.LowerDeckOpposite), "Untere Ebene in Gegenrichtung",
                "Dreht die untere Ebene um, sodass beide Ebenen entgegengesetzt verlaufen.")
            .Option(nameof(BridgeSetting.DeckSpacing), "Ebenenabstand",
                "Senkrechter Abstand zwischen den beiden Ebenen.")
            .Option(nameof(BridgeSetting.OverwriteExisting), "Vorhandene überschreiben",
                "Erzeugt eine Brücke auch dann neu, wenn ihr Asset schon existiert.")
            .Option(nameof(BridgeSetting.AllowGameplayExport), "Erzeugen außerhalb des Editors erlauben",
                "Standardmäßig aus: Assets aus einem Stadtspielstand zu schreiben ist riskanter als im Editor.")
            .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "Unbenutzte Abhängigkeiten mitlöschen",
                "Löscht nach dem Entfernen erzeugte Abschnitte und Teile, die nichts mehr referenziert.")
            .Option(nameof(BridgeSetting.EmbedIcons), "Vorschaubilder in die Assets einbetten",
                "Macht eine erzeugte Brücke eigenständig, sodass ihr Vorschaubild auch beim Teilen funktioniert.");
    }

    private static UiStrings Spanish(UiStrings text)
    {
        text.Title = "Generador de prefabs de puente";
        text.TabBridge = "Puente";
        text.GroupDeck = "Nivel superior";
        text.GroupStyle = "Estilo";
        text.GroupLowerDeck = "Nivel inferior (experimental)";
        text.StateNoStyles = "No hay ningún estilo de puente disponible. Los puentes colgantes, extradosados y en arco de celosía vienen con el contenido de Bridges & Ports.";
        text.StateStyleSource = "Estilo: {0} (de {1}).";
        text.StateDoubleDeckExperimental = "El nivel inferior se crea con el puente y no se puede editar ni conectar por separado.";
        text.OptionDonorBuildStyle = "Mantener el del estilo";
        text.OptionLowerDeckNone = "Ninguno: un solo tablero";
        text.OptionNoDeckChosen = "Sin elegir";
        text.StateNoUpperDeck = "Elige la carretera que llevará el puente.";
        text.StateUpperDeck = "Nivel superior: {0} ({1} m de ancho).";
        text.StateStyleFit = "Ajustado de {0} ({1} m) a {2} m.";
        text.StateLowerDeck = "Nivel inferior: {0}, {1} m por debajo, {2}.";
        text.StateDirectionOpposite = "en sentido contrario";
        text.StateDirectionSame = "en el mismo sentido";
        text.StateExportName = "Se generará como: {0}";
        text.StateReportHint = "Informe completo: ModsData\\BridgePrefabGenerator\\last-export-report.txt";
        return text
            .DeckKind("RoadBuilder", "Road Builder")
            .DeckKind("Road", "Carretera")
            .DeckKind("Train", "Tren")
            .DeckKind("Subway", "Metro")
            .DeckKind("Tram", "Tranvía")
            .Option(nameof(BridgeSetting.StatusText), "Lo que se generará")
            .Option(nameof(BridgeSetting.BridgeName), "Nombre del puente",
                "El nombre con el que se guarda el puente generado. Se regenera cada vez que cambian la carretera, el estilo o el tablero inferior. Escriba encima para usar el suyo.")
            .Option(nameof(BridgeSetting.UpperDeckId), "Carretera a convertir",
                "La carretera que lleva el puente. Sirve cualquiera registrada, incluidas las exportadas antes.")
            .Option(nameof(BridgeSetting.RescanRoads), "Volver a explorar",
                "Vuelve a leer las carreteras, vías y estilos de puente disponibles.")
            .Option(nameof(BridgeSetting.ExportSelected), "Generar puente",
                "Construye un puente con las opciones de arriba. De uno en uno: un puente es un emparejamiento de tableros, no algo que se aplique a una lista.")
            .Option(nameof(BridgeSetting.ArmRemoval), "Permitir eliminación",
                "Seguro. La eliminación borra archivos de asset y no se puede deshacer, así que el botón sigue desactivado hasta activarlo.")
            .Option(nameof(BridgeSetting.RemoveSelected), "Eliminar el puente generado",
                "Borra el puente generado a partir de la carretera de arriba, y su nivel inferior si lo tiene.")
            .Option(nameof(BridgeSetting.BridgeStyleId), "Estilo de puente",
                "De qué puente se toma el aspecto. Un estilo sin nada instalado que lo proporcione aparece marcado.")
            .Option(nameof(BridgeSetting.BuildStyleOverride), "Modo de construcción",
                "Elevado es un tablero sobre pilares, terraplén se apoya en un talud, muelle sigue la orilla.")
            .Option(nameof(BridgeSetting.LowerDeckId), "Nivel inferior",
                "Qué cuelga bajo el puente: nada, otra carretera, o una vía de tren, metro o tranvía. La misma carretera de arriba da dos tableros viarios.")
            .Option(nameof(BridgeSetting.LowerDeckOpposite), "Nivel inferior en sentido contrario",
                "Invierte el nivel inferior para que ambos niveles lleven sentidos opuestos.")
            .Option(nameof(BridgeSetting.DeckSpacing), "Separación entre niveles",
                "Distancia vertical entre los dos tableros.")
            .Option(nameof(BridgeSetting.OverwriteExisting), "Sobrescribir lo existente",
                "Genera el puente de nuevo aunque su asset ya exista.")
            .Option(nameof(BridgeSetting.AllowGameplayExport), "Permitir generar fuera del Editor",
                "Desactivado por defecto: escribir assets desde una partida es más arriesgado que hacerlo en el Editor.")
            .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "Eliminar también dependencias sin usar",
                "Tras eliminar, borra las secciones y piezas generadas que ya no referencia nada.")
            .Option(nameof(BridgeSetting.EmbedIcons), "Incrustar miniaturas en los assets",
                "Hace que el puente generado sea autónomo, así su miniatura sigue funcionando al compartirlo.");
    }

    private static UiStrings French(UiStrings text)
    {
        text.Title = "Générateur de prefabs de pont";
        text.TabBridge = "Pont";
        text.GroupDeck = "Niveau supérieur";
        text.GroupStyle = "Style";
        text.GroupLowerDeck = "Niveau inférieur (expérimental)";
        text.StateNoStyles = "Aucun style de pont disponible. Les ponts suspendus, extradossés et en arc à treillis viennent du contenu Bridges & Ports.";
        text.StateStyleSource = "Style : {0} (de {1}).";
        text.StateDoubleDeckExperimental = "Le niveau inférieur est créé avec le pont et ne peut être ni modifié ni raccordé séparément.";
        text.OptionDonorBuildStyle = "Garder celui du style";
        text.OptionLowerDeckNone = "Aucun – un seul tablier";
        text.OptionNoDeckChosen = "Rien de choisi";
        text.StateNoUpperDeck = "Choisissez la route que le pont doit porter.";
        text.StateUpperDeck = "Niveau supérieur : {0} ({1} m de large).";
        text.StateStyleFit = "Ajusté de {0} ({1} m) à {2} m.";
        text.StateLowerDeck = "Niveau inférieur : {0}, {1} m plus bas, {2}.";
        text.StateDirectionOpposite = "en sens inverse";
        text.StateDirectionSame = "dans le même sens";
        text.StateExportName = "Sera généré sous : {0}";
        text.StateReportHint = "Rapport complet : ModsData\\BridgePrefabGenerator\\last-export-report.txt";
        return text
            .DeckKind("RoadBuilder", "Road Builder")
            .DeckKind("Road", "Route")
            .DeckKind("Train", "Train")
            .DeckKind("Subway", "Métro")
            .DeckKind("Tram", "Tramway")
            .Option(nameof(BridgeSetting.StatusText), "Ce qui sera généré")
            .Option(nameof(BridgeSetting.BridgeName), "Nom du pont",
                "Le nom sous lequel le pont généré est enregistré. Régénéré à chaque changement de route, de style ou de tablier inférieur. Saisissez le vôtre pour le conserver.")
            .Option(nameof(BridgeSetting.UpperDeckId), "Route à convertir",
                "La route que porte le pont. N'importe quelle route enregistrée convient, y compris celles exportées auparavant.")
            .Option(nameof(BridgeSetting.RescanRoads), "Réanalyser",
                "Relit les routes, voies et styles de pont disponibles.")
            .Option(nameof(BridgeSetting.ExportSelected), "Générer le pont",
                "Construit un pont à partir des choix ci-dessus. Un à la fois : un pont est un appariement de tabliers, pas quelque chose qui s'applique à une liste.")
            .Option(nameof(BridgeSetting.ArmRemoval), "Autoriser la suppression",
                "Sécurité. La suppression efface des fichiers d'asset sans retour possible, le bouton reste donc désactivé jusque-là.")
            .Option(nameof(BridgeSetting.RemoveSelected), "Supprimer le pont généré",
                "Supprime le pont généré à partir de la route ci-dessus, ainsi que son niveau inférieur le cas échéant.")
            .Option(nameof(BridgeSetting.BridgeStyleId), "Style de pont",
                "De quel pont l'aspect est repris. Un style sans rien d'installé pour le fournir est signalé.")
            .Option(nameof(BridgeSetting.BuildStyleOverride), "Mode de construction",
                "Surélevé est un tablier sur piles, remblai repose sur un talus, quai suit la rive.")
            .Option(nameof(BridgeSetting.LowerDeckId), "Niveau inférieur",
                "Ce qui pend sous le pont : rien, une autre route, ou une voie de train, métro ou tram. La même route qu'au-dessus donne deux tabliers routiers.")
            .Option(nameof(BridgeSetting.LowerDeckOpposite), "Niveau inférieur en sens inverse",
                "Inverse le niveau inférieur pour que les deux niveaux aillent en sens opposés.")
            .Option(nameof(BridgeSetting.DeckSpacing), "Écart entre niveaux",
                "Distance verticale entre les deux tabliers.")
            .Option(nameof(BridgeSetting.OverwriteExisting), "Écraser l'existant",
                "Regénère le pont même si son asset existe déjà.")
            .Option(nameof(BridgeSetting.AllowGameplayExport), "Autoriser la génération hors de l'Éditeur",
                "Désactivé par défaut : écrire des assets depuis une partie est plus risqué que dans l'Éditeur.")
            .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "Supprimer aussi les dépendances inutilisées",
                "Après une suppression, efface les sections et pièces générées que plus rien ne référence.")
            .Option(nameof(BridgeSetting.EmbedIcons), "Intégrer les vignettes aux assets",
                "Rend le pont généré autonome, sa vignette fonctionne donc encore une fois partagé.");
    }

    private static UiStrings Italian(UiStrings text)
    {
        text.Title = "Generatore di prefab di ponti";
        text.TabBridge = "Ponte";
        text.GroupDeck = "Livello superiore";
        text.GroupStyle = "Stile";
        text.GroupLowerDeck = "Livello inferiore (sperimentale)";
        text.StateNoStyles = "Nessuno stile di ponte disponibile. I ponti sospesi, extradossati e ad arco reticolare arrivano con i contenuti di Bridges & Ports.";
        text.StateStyleSource = "Stile: {0} (da {1}).";
        text.StateDoubleDeckExperimental = "Il livello inferiore nasce con il ponte e non può essere modificato né collegato da solo.";
        text.OptionDonorBuildStyle = "Mantieni quello dello stile";
        text.OptionLowerDeckNone = "Nessuno – impalcato singolo";
        text.OptionNoDeckChosen = "Niente scelto";
        text.StateNoUpperDeck = "Scegli la strada che il ponte deve portare.";
        text.StateUpperDeck = "Livello superiore: {0} (larghezza {1} m).";
        text.StateStyleFit = "Adattato da {0} ({1} m) a {2} m.";
        text.StateLowerDeck = "Livello inferiore: {0}, {1} m più in basso, {2}.";
        text.StateDirectionOpposite = "in senso opposto";
        text.StateDirectionSame = "nello stesso senso";
        text.StateExportName = "Verrà generato come: {0}";
        text.StateReportHint = "Rapporto completo: ModsData\\BridgePrefabGenerator\\last-export-report.txt";
        return text
            .DeckKind("RoadBuilder", "Road Builder")
            .DeckKind("Road", "Strada")
            .DeckKind("Train", "Treno")
            .DeckKind("Subway", "Metro")
            .DeckKind("Tram", "Tram")
            .Option(nameof(BridgeSetting.StatusText), "Cosa verrà generato")
            .Option(nameof(BridgeSetting.BridgeName), "Nome del ponte",
                "Il nome con cui viene salvato il ponte generato. Rigenerato a ogni modifica di strada, stile o impalcato inferiore. Scrivi sopra per usare il tuo.")
            .Option(nameof(BridgeSetting.UpperDeckId), "Strada da convertire",
                "La strada che il ponte porta. Va bene qualsiasi strada registrata, comprese quelle esportate in precedenza.")
            .Option(nameof(BridgeSetting.RescanRoads), "Rileggi",
                "Rilegge le strade, i binari e gli stili di ponte disponibili.")
            .Option(nameof(BridgeSetting.ExportSelected), "Genera ponte",
                "Costruisce un ponte con le scelte qui sopra. Uno alla volta: un ponte è un abbinamento di impalcati, non qualcosa da applicare a un elenco.")
            .Option(nameof(BridgeSetting.ArmRemoval), "Consenti la rimozione",
                "Sicura. La rimozione cancella file di asset e non si può annullare, quindi il pulsante resta disattivato fino ad allora.")
            .Option(nameof(BridgeSetting.RemoveSelected), "Rimuovi il ponte generato",
                "Elimina il ponte generato dalla strada qui sopra, e il suo livello inferiore se c'è.")
            .Option(nameof(BridgeSetting.BridgeStyleId), "Stile del ponte",
                "Da quale ponte viene preso l'aspetto. Uno stile senza nulla di installato che lo fornisca viene segnalato.")
            .Option(nameof(BridgeSetting.BuildStyleOverride), "Modo di costruzione",
                "Sopraelevato è un impalcato su pile, rilevato poggia su un terrapieno, banchina segue la riva.")
            .Option(nameof(BridgeSetting.LowerDeckId), "Livello inferiore",
                "Cosa pende sotto il ponte: niente, un'altra strada, o un binario di treno, metro o tram. La stessa strada di sopra dà due impalcati stradali.")
            .Option(nameof(BridgeSetting.LowerDeckOpposite), "Livello inferiore in senso opposto",
                "Inverte il livello inferiore, così i due livelli vanno in direzioni opposte.")
            .Option(nameof(BridgeSetting.DeckSpacing), "Distanza fra i livelli",
                "Distanza verticale fra i due impalcati.")
            .Option(nameof(BridgeSetting.OverwriteExisting), "Sovrascrivi l'esistente",
                "Rigenera il ponte anche se il suo asset esiste già.")
            .Option(nameof(BridgeSetting.AllowGameplayExport), "Consenti la generazione fuori dall'Editor",
                "Disattivo per impostazione predefinita: scrivere asset da una partita è più rischioso che nell'Editor.")
            .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "Rimuovi anche le dipendenze inutilizzate",
                "Dopo una rimozione, elimina le sezioni e i pezzi generati che non sono più referenziati.")
            .Option(nameof(BridgeSetting.EmbedIcons), "Incorpora le miniature negli asset",
                "Rende il ponte generato autonomo, così la sua miniatura funziona anche una volta condiviso.");
    }

    private static UiStrings Polish(UiStrings text)
    {
        text.Title = "Generator prefabów mostów";
        text.TabBridge = "Most";
        text.GroupDeck = "Poziom górny";
        text.GroupStyle = "Styl";
        text.GroupLowerDeck = "Poziom dolny (eksperymentalne)";
        text.StateNoStyles = "Brak dostępnych stylów mostów. Mosty wiszące, ekstradosowe i kratownicowe łukowe pochodzą z zawartości Bridges & Ports.";
        text.StateStyleSource = "Styl: {0} (z {1}).";
        text.StateDoubleDeckExperimental = "Dolny poziom powstaje razem z mostem i nie da się go osobno edytować ani podłączyć.";
        text.OptionDonorBuildStyle = "Zostaw ustawienie stylu";
        text.OptionLowerDeckNone = "Brak – jeden pomost";
        text.OptionNoDeckChosen = "Nic nie wybrano";
        text.StateNoUpperDeck = "Wybierz drogę, którą most ma nieść.";
        text.StateUpperDeck = "Poziom górny: {0} (szerokość {1} m).";
        text.StateStyleFit = "Dopasowano z {0} ({1} m) do {2} m.";
        text.StateLowerDeck = "Poziom dolny: {0}, {1} m niżej, {2}.";
        text.StateDirectionOpposite = "w przeciwną stronę";
        text.StateDirectionSame = "w tę samą stronę";
        text.StateExportName = "Zostanie wygenerowany jako: {0}";
        text.StateReportHint = "Pełny raport: ModsData\\BridgePrefabGenerator\\last-export-report.txt";
        return text
            .DeckKind("RoadBuilder", "Road Builder")
            .DeckKind("Road", "Droga")
            .DeckKind("Train", "Kolej")
            .DeckKind("Subway", "Metro")
            .DeckKind("Tram", "Tramwaj")
            .Option(nameof(BridgeSetting.StatusText), "Co zostanie wygenerowane")
            .Option(nameof(BridgeSetting.BridgeName), "Nazwa mostu",
                "Nazwa, pod którą zapisywany jest wygenerowany most. Odtwarzana przy każdej zmianie drogi, stylu lub dolnego pomostu. Wpisz własną, aby ją zachować.")
            .Option(nameof(BridgeSetting.UpperDeckId), "Droga do zamiany",
                "Droga, którą niesie most. Nadaje się każda zarejestrowana, także wcześniej wyeksportowana.")
            .Option(nameof(BridgeSetting.RescanRoads), "Skanuj ponownie",
                "Ponownie czyta dostępne drogi, tory i style mostów.")
            .Option(nameof(BridgeSetting.ExportSelected), "Wygeneruj most",
                "Buduje jeden most z powyższych wyborów. Po jednym: most to para pomostów, a nie coś, co stosuje się do listy.")
            .Option(nameof(BridgeSetting.ArmRemoval), "Zezwól na usuwanie",
                "Zabezpieczenie. Usuwanie kasuje pliki zasobów bezpowrotnie, więc przycisk pozostaje nieaktywny do czasu włączenia.")
            .Option(nameof(BridgeSetting.RemoveSelected), "Usuń wygenerowany most",
                "Kasuje most wygenerowany z powyższej drogi, wraz z dolnym poziomem, jeśli istnieje.")
            .Option(nameof(BridgeSetting.BridgeStyleId), "Styl mostu",
                "Z którego mostu brany jest wygląd. Styl, dla którego nic nie jest zainstalowane, zostaje oznaczony.")
            .Option(nameof(BridgeSetting.BuildStyleOverride), "Sposób budowy",
                "Estakada to zwykły pomost na filarach, nasyp leży na skarpie, nabrzeże biegnie przy brzegu.")
            .Option(nameof(BridgeSetting.LowerDeckId), "Poziom dolny",
                "Co wisi pod mostem: nic, inna droga, albo tor kolejowy, metra lub tramwajowy. Ta sama droga co wyżej daje dwa pomosty drogowe.")
            .Option(nameof(BridgeSetting.LowerDeckOpposite), "Dolny poziom w przeciwną stronę",
                "Odwraca dolny poziom, więc oba poziomy prowadzą w przeciwnych kierunkach.")
            .Option(nameof(BridgeSetting.DeckSpacing), "Odstęp poziomów",
                "Pionowa odległość między pomostami.")
            .Option(nameof(BridgeSetting.OverwriteExisting), "Nadpisz istniejące",
                "Generuje most ponownie, nawet jeśli jego zasób już istnieje.")
            .Option(nameof(BridgeSetting.AllowGameplayExport), "Zezwól na generowanie poza Edytorem",
                "Domyślnie wyłączone: zapisywanie zasobów z zapisu miasta jest bardziej ryzykowne niż w Edytorze.")
            .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "Usuń też nieużywane zależności",
                "Po usunięciu kasuje wygenerowane odcinki i elementy, do których nic już się nie odwołuje.")
            .Option(nameof(BridgeSetting.EmbedIcons), "Osadź miniatury w zasobach",
                "Czyni wygenerowany most samodzielnym, więc miniatura działa też po udostępnieniu.");
    }

    private static UiStrings Portuguese(UiStrings text)
    {
        text.Title = "Gerador de prefabs de ponte";
        text.TabBridge = "Ponte";
        text.GroupDeck = "Nível superior";
        text.GroupStyle = "Estilo";
        text.GroupLowerDeck = "Nível inferior (experimental)";
        text.StateNoStyles = "Nenhum estilo de ponte disponível. Pontes suspensas, extradorso e em arco treliçado vêm com o conteúdo Bridges & Ports.";
        text.StateStyleSource = "Estilo: {0} (de {1}).";
        text.StateDoubleDeckExperimental = "O nível inferior é criado com a ponte e não pode ser editado nem ligado separadamente.";
        text.OptionDonorBuildStyle = "Manter o do estilo";
        text.OptionLowerDeckNone = "Nenhum – tabuleiro único";
        text.OptionNoDeckChosen = "Nada escolhido";
        text.StateNoUpperDeck = "Escolha a via que a ponte deve levar.";
        text.StateUpperDeck = "Nível superior: {0} ({1} m de largura).";
        text.StateStyleFit = "Ajustado de {0} ({1} m) para {2} m.";
        text.StateLowerDeck = "Nível inferior: {0}, {1} m abaixo, {2}.";
        text.StateDirectionOpposite = "no sentido oposto";
        text.StateDirectionSame = "no mesmo sentido";
        text.StateExportName = "Será gerado como: {0}";
        text.StateReportHint = "Relatório completo: ModsData\\BridgePrefabGenerator\\last-export-report.txt";
        return text
            .DeckKind("RoadBuilder", "Road Builder")
            .DeckKind("Road", "Via")
            .DeckKind("Train", "Trem")
            .DeckKind("Subway", "Metrô")
            .DeckKind("Tram", "Bonde")
            .Option(nameof(BridgeSetting.StatusText), "O que será gerado")
            .Option(nameof(BridgeSetting.BridgeName), "Nome da ponte",
                "O nome com que a ponte gerada é guardada. Regenerado sempre que a estrada, o estilo ou o tabuleiro inferior mudam. Escreva por cima para usar o seu.")
            .Option(nameof(BridgeSetting.UpperDeckId), "Via a converter",
                "A via que a ponte leva. Serve qualquer via registrada, inclusive as exportadas antes.")
            .Option(nameof(BridgeSetting.RescanRoads), "Verificar de novo",
                "Relê as vias, linhas e estilos de ponte disponíveis.")
            .Option(nameof(BridgeSetting.ExportSelected), "Gerar ponte",
                "Constrói uma ponte a partir das escolhas acima. Uma de cada vez: uma ponte é um par de tabuleiros, não algo que se aplique a uma lista.")
            .Option(nameof(BridgeSetting.ArmRemoval), "Permitir remoção",
                "Trava de segurança. A remoção apaga arquivos de asset e não pode ser desfeita, então o botão fica desativado até aqui.")
            .Option(nameof(BridgeSetting.RemoveSelected), "Remover a ponte gerada",
                "Apaga a ponte gerada a partir da via acima, e o nível inferior se houver.")
            .Option(nameof(BridgeSetting.BridgeStyleId), "Estilo da ponte",
                "De qual ponte o visual é copiado. Um estilo sem nada instalado que o forneça fica marcado.")
            .Option(nameof(BridgeSetting.BuildStyleOverride), "Modo de construção",
                "Elevada é um tabuleiro sobre pilares, aterro assenta num talude, cais acompanha a margem.")
            .Option(nameof(BridgeSetting.LowerDeckId), "Nível inferior",
                "O que pende sob a ponte: nada, outra via, ou uma linha de trem, metrô ou bonde. A mesma via de cima dá dois tabuleiros rodoviários.")
            .Option(nameof(BridgeSetting.LowerDeckOpposite), "Nível inferior no sentido oposto",
                "Inverte o nível inferior, para que os dois sigam sentidos opostos.")
            .Option(nameof(BridgeSetting.DeckSpacing), "Distância entre níveis",
                "Distância vertical entre os dois tabuleiros.")
            .Option(nameof(BridgeSetting.OverwriteExisting), "Sobrescrever o existente",
                "Gera a ponte de novo mesmo que o asset já exista.")
            .Option(nameof(BridgeSetting.AllowGameplayExport), "Permitir gerar fora do Editor",
                "Desligado por padrão: escrever assets a partir de um save é mais arriscado do que no Editor.")
            .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "Remover também dependências sem uso",
                "Após remover, apaga as seções e peças geradas que nada mais referencia.")
            .Option(nameof(BridgeSetting.EmbedIcons), "Embutir miniaturas nos assets",
                "Torna a ponte gerada autossuficiente, então a miniatura continua funcionando ao compartilhar.");
    }

    private static UiStrings Russian(UiStrings text)
    {
        text.Title = "Генератор префабов мостов";
        text.TabBridge = "Мост";
        text.GroupDeck = "Верхний ярус";
        text.GroupStyle = "Стиль";
        text.GroupLowerDeck = "Нижний ярус (эксперимент)";
        text.StateNoStyles = "Нет доступных стилей мостов. Висячие, экстрадозные и арочные ферменные мосты входят в содержимое Bridges & Ports.";
        text.StateStyleSource = "Стиль: {0} (из {1}).";
        text.StateDoubleDeckExperimental = "Нижний ярус создаётся вместе с мостом и не может редактироваться или подключаться отдельно.";
        text.OptionDonorBuildStyle = "Оставить как в стиле";
        text.OptionLowerDeckNone = "Нет — один ярус";
        text.OptionNoDeckChosen = "Ничего не выбрано";
        text.StateNoUpperDeck = "Выберите дорогу, которую понесёт мост.";
        text.StateUpperDeck = "Верхний ярус: {0} (ширина {1} м).";
        text.StateStyleFit = "Подогнано с {0} ({1} м) до {2} м.";
        text.StateLowerDeck = "Нижний ярус: {0}, на {1} м ниже, {2}.";
        text.StateDirectionOpposite = "в обратную сторону";
        text.StateDirectionSame = "в ту же сторону";
        text.StateExportName = "Будет создан как: {0}";
        text.StateReportHint = "Полный отчёт: ModsData\\BridgePrefabGenerator\\last-export-report.txt";
        return text
            .DeckKind("RoadBuilder", "Road Builder")
            .DeckKind("Road", "Дорога")
            .DeckKind("Train", "Железная дорога")
            .DeckKind("Subway", "Метро")
            .DeckKind("Tram", "Трамвай")
            .Option(nameof(BridgeSetting.StatusText), "Что будет создано")
            .Option(nameof(BridgeSetting.BridgeName), "Название моста",
                "Имя, под которым сохраняется созданный мост. Пересоздаётся при каждом изменении дороги, стиля или нижнего яруса. Введите своё, чтобы сохранить его.")
            .Option(nameof(BridgeSetting.UpperDeckId), "Дорога для преобразования",
                "Дорога, которую несёт мост. Подойдёт любая зарегистрированная, включая экспортированные ранее.")
            .Option(nameof(BridgeSetting.RescanRoads), "Пересканировать",
                "Перечитывает доступные дороги, пути и стили мостов.")
            .Option(nameof(BridgeSetting.ExportSelected), "Создать мост",
                "Строит один мост по выбору выше. По одному: мост — это пара ярусов, а не то, что применяют к списку.")
            .Option(nameof(BridgeSetting.ArmRemoval), "Разрешить удаление",
                "Предохранитель. Удаление стирает файлы ресурсов безвозвратно, поэтому кнопка остаётся отключённой.")
            .Option(nameof(BridgeSetting.RemoveSelected), "Удалить созданный мост",
                "Удаляет мост, созданный из дороги выше, и его нижний ярус, если он есть.")
            .Option(nameof(BridgeSetting.BridgeStyleId), "Стиль моста",
                "С какого моста берётся внешний вид. Стиль, для которого ничего не установлено, помечается.")
            .Option(nameof(BridgeSetting.BuildStyleOverride), "Способ постройки",
                "Эстакада — обычное полотно на опорах, насыпь лежит на валу, набережная идёт вдоль берега.")
            .Option(nameof(BridgeSetting.LowerDeckId), "Нижний ярус",
                "Что висит под мостом: ничего, другая дорога, либо путь поезда, метро или трамвая. Та же дорога, что и сверху, даёт два дорожных полотна.")
            .Option(nameof(BridgeSetting.LowerDeckOpposite), "Нижний ярус в обратную сторону",
                "Разворачивает нижний ярус, чтобы ярусы вели в противоположных направлениях.")
            .Option(nameof(BridgeSetting.DeckSpacing), "Расстояние между ярусами",
                "Расстояние по вертикали между двумя полотнами.")
            .Option(nameof(BridgeSetting.OverwriteExisting), "Перезаписывать существующее",
                "Создаёт мост заново, даже если его ресурс уже есть.")
            .Option(nameof(BridgeSetting.AllowGameplayExport), "Разрешить создание вне редактора",
                "По умолчанию выключено: записывать ресурсы из сохранения города рискованнее, чем в редакторе.")
            .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "Удалять и неиспользуемые зависимости",
                "После удаления стирает созданные секции и части, на которые больше ничего не ссылается.")
            .Option(nameof(BridgeSetting.EmbedIcons), "Встраивать миниатюры в ресурсы",
                "Делает созданный мост самодостаточным, так что миниатюра работает и после передачи другим.");
    }
}
