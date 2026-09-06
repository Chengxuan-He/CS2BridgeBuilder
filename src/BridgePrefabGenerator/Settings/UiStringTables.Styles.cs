namespace BridgePrefabGenerator.Settings;

/// <summary>
/// The names of the fixed bridge styles, in every shipped language.
///
/// Kept apart from the rest of the translations because these are the one set of strings that must
/// line up with a table of ids in another file - <see cref="Bridges.BridgeStyleDefinitions"/> - and
/// having them in one place per language makes a missing translation obvious rather than something
/// to hunt for across six table files.
/// </summary>
internal static partial class UiStringTables
{
    internal static UiStrings WithStyleNames(UiStrings text, string localeId) => localeId switch
    {
        "de-DE" => GermanStyles(text),
        "es-ES" => SpanishStyles(text),
        "fr-FR" => FrenchStyles(text),
        "it-IT" => ItalianStyles(text),
        "ja-JP" => JapaneseStyles(text),
        "ko-KR" => KoreanStyles(text),
        "pl-PL" => PolishStyles(text),
        "pt-BR" => PortugueseStyles(text),
        "ru-RU" => RussianStyles(text),
        "zh-HANS" => SimplifiedChineseStyles(text),
        "zh-HANT" => TraditionalChineseStyles(text),
        _ => EnglishStyles(text),
    };

    private static UiStrings EnglishStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0} (not available)";
        text.StateStyleNotInstalled = "Nothing installed provides the style \"{0}\". Pick another one below.";
        return text
            .Style("Suspension", "Suspension Bridge")
            .Style("SuspensionGolden", "Golden Suspension Bridge")
            .Style("CableStayed", "Cable-Stayed Bridge (H pylon)")
            .Style("Extradosed01", "Extradosed Bridge (V pylon, double deck)")
            .Style("Extradosed02", "Extradosed Bridge (A pylon, double deck)")
            .Style("Extradosed03", "Cable-Stayed Bridge (V pylon)")
            .Style("ExtradosedLarge", "Cable-Stayed Bridge (single-column pylon)")
            .Style("TrussArch", "Truss Arch Bridge (arch below)")
            .Style("TrussArch01", "Truss Arch Bridge (arch above · blue)")
            .Style("TrussArch02", "Truss Arch Bridge (arch above · white)")
            .Style("TrussArch03", "Truss Arch Bridge (arch above · green)")
            .Style("TiedArch", "Tied Arch Bridge")
            .Style("Grand", "Grand Bridge")
            .Style("Draw", "Bascule Bridge")
            .Style("Lift", "Lift Bridge")
            .Style("PedestrianDraw", "Pedestrian Bascule Bridge")
            .Style("CoveredWood", "Covered Wooden Bridge");
    }

    private static UiStrings SimplifiedChineseStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0}（不可用）";
        text.StateStyleNotInstalled = "本机没有提供样式“{0}”的内容。请在下方换一个。";
        return text
            .Style("Suspension", "悬索桥")
            .Style("SuspensionGolden", "悬索桥（黄色）")
            .Style("CableStayed", "斜拉桥（H型桥塔）")
            .Style("Extradosed01", "双层斜拉桥（V型桥塔）")
            .Style("Extradosed02", "双层斜拉桥（A型桥塔）")
            .Style("Extradosed03", "斜拉桥（V型桥塔）")
            .Style("ExtradosedLarge", "斜拉桥（单柱桥塔）")
            .Style("TrussArch", "桁架拱桥（下承式）")
            .Style("TrussArch01", "桁架拱桥（上承式·蓝色）")
            .Style("TrussArch02", "桁架拱桥（上承式·白色）")
            .Style("TrussArch03", "桁架拱桥（上承式·绿色）")
            .Style("TiedArch", "系杆拱桥")
            .Style("Grand", "宏伟大桥")
            .Style("Draw", "开合桥")
            .Style("Lift", "升降桥")
            .Style("PedestrianDraw", "人行开合桥")
            .Style("CoveredWood", "木廊桥");
    }

    private static UiStrings TraditionalChineseStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0}（不可用）";
        text.StateStyleNotInstalled = "本機沒有提供樣式「{0}」的內容。請在下方換一個。";
        return text
            .Style("Suspension", "懸索橋")
            .Style("SuspensionGolden", "懸索橋（黃色）")
            .Style("CableStayed", "斜拉橋（H型橋塔）")
            .Style("Extradosed01", "雙層斜拉橋（V型橋塔）")
            .Style("Extradosed02", "雙層斜拉橋（A型橋塔）")
            .Style("Extradosed03", "斜拉橋（V型橋塔）")
            .Style("ExtradosedLarge", "斜拉橋（單柱橋塔）")
            .Style("TrussArch", "桁架拱橋（下承式）")
            .Style("TrussArch01", "桁架拱橋（上承式·藍色）")
            .Style("TrussArch02", "桁架拱橋（上承式·白色）")
            .Style("TrussArch03", "桁架拱橋（上承式·綠色）")
            .Style("TiedArch", "繫桿拱橋")
            .Style("Grand", "宏偉大橋")
            .Style("Draw", "開合橋")
            .Style("Lift", "升降橋")
            .Style("PedestrianDraw", "人行開合橋")
            .Style("CoveredWood", "木廊橋");
    }

    private static UiStrings JapaneseStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0}（利用不可）";
        text.StateStyleNotInstalled = "スタイル「{0}」を提供するものが導入されていません。下から別のものを選んでください。";
        return text
            .Style("Suspension", "吊り橋")
            .Style("SuspensionGolden", "吊り橋（金色）")
            .Style("CableStayed", "斜張橋（H型主塔）")
            .Style("Extradosed01", "二層斜張橋（V型主塔）")
            .Style("Extradosed02", "二層斜張橋（A型主塔）")
            .Style("Extradosed03", "斜張橋（V型主塔）")
            .Style("ExtradosedLarge", "斜張橋（単柱主塔）")
            .Style("TrussArch", "トラスアーチ橋（下路式）")
            .Style("TrussArch01", "トラスアーチ橋（上路式・青）")
            .Style("TrussArch02", "トラスアーチ橋（上路式・白）")
            .Style("TrussArch03", "トラスアーチ橋（上路式・緑）")
            .Style("TiedArch", "ローゼ橋")
            .Style("Grand", "グランドブリッジ")
            .Style("Draw", "跳ね橋")
            .Style("Lift", "昇開橋")
            .Style("PedestrianDraw", "歩行者用跳ね橋")
            .Style("CoveredWood", "屋根付き木橋");
    }

    private static UiStrings KoreanStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0} (사용 불가)";
        text.StateStyleNotInstalled = "스타일 \"{0}\"을(를) 제공하는 콘텐츠가 없습니다. 아래에서 다른 것을 고르세요.";
        return text
            .Style("Suspension", "현수교")
            .Style("SuspensionGolden", "현수교 (금색)")
            .Style("CableStayed", "사장교 (H형 주탑)")
            .Style("Extradosed01", "복층 사장교 (V형 주탑)")
            .Style("Extradosed02", "복층 사장교 (A형 주탑)")
            .Style("Extradosed03", "사장교 (V형 주탑)")
            .Style("ExtradosedLarge", "사장교 (단주 주탑)")
            .Style("TrussArch", "트러스 아치교 (하로교)")
            .Style("TrussArch01", "트러스 아치교 (상로식·파란색)")
            .Style("TrussArch02", "트러스 아치교 (상로식·흰색)")
            .Style("TrussArch03", "트러스 아치교 (상로식·초록색)")
            .Style("TiedArch", "타이드 아치교")
            .Style("Grand", "그랜드 브리지")
            .Style("Draw", "도개교")
            .Style("Lift", "승개교")
            .Style("PedestrianDraw", "보행자 도개교")
            .Style("CoveredWood", "지붕 있는 목교");
    }

    private static UiStrings GermanStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0} (nicht verfügbar)";
        text.StateStyleNotInstalled = "Für den Stil \"{0}\" ist nichts installiert. Wähle unten einen anderen.";
        return text
            .Style("Suspension", "Hängebrücke")
            .Style("SuspensionGolden", "Hängebrücke (golden)")
            .Style("CableStayed", "Schrägseilbrücke (H-Pylon)")
            .Style("Extradosed01", "Extradosed-Brücke (V-Pylon, zweistöckig)")
            .Style("Extradosed02", "Extradosed-Brücke (A-Pylon, zweistöckig)")
            .Style("Extradosed03", "Schrägseilbrücke (V-Pylon)")
            .Style("ExtradosedLarge", "Schrägseilbrücke (Einzelpylon)")
            .Style("TrussArch", "Fachwerkbogenbrücke (Bogen unten)")
            .Style("TrussArch01", "Fachwerkbogenbrücke (Bogen oben · blau)")
            .Style("TrussArch02", "Fachwerkbogenbrücke (Bogen oben · weiß)")
            .Style("TrussArch03", "Fachwerkbogenbrücke (Bogen oben · grün)")
            .Style("TiedArch", "Stabbogenbrücke")
            .Style("Grand", "Große Brücke")
            .Style("Draw", "Klappbrücke")
            .Style("Lift", "Hubbrücke")
            .Style("PedestrianDraw", "Fußgänger-Klappbrücke")
            .Style("CoveredWood", "Gedeckte Holzbrücke");
    }

    private static UiStrings SpanishStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0} (no disponible)";
        text.StateStyleNotInstalled = "No hay nada instalado que proporcione el estilo \"{0}\". Elige otro abajo.";
        return text
            .Style("Suspension", "Puente colgante")
            .Style("SuspensionGolden", "Puente colgante (dorado)")
            .Style("CableStayed", "Puente atirantado (pilono en H)")
            .Style("Extradosed01", "Puente extradosado (pilono en V, dos niveles)")
            .Style("Extradosed02", "Puente extradosado (pilono en A, dos niveles)")
            .Style("Extradosed03", "Puente atirantado (pilono en V)")
            .Style("ExtradosedLarge", "Puente atirantado (pilono único)")
            .Style("TrussArch", "Puente en arco de celosía (arco inferior)")
            .Style("TrussArch01", "Puente en arco de celosía (arco superior · azul)")
            .Style("TrussArch02", "Puente en arco de celosía (arco superior · blanco)")
            .Style("TrussArch03", "Puente en arco de celosía (arco superior · verde)")
            .Style("TiedArch", "Puente en arco atirantado")
            .Style("Grand", "Gran puente")
            .Style("Draw", "Puente basculante")
            .Style("Lift", "Puente elevadizo")
            .Style("PedestrianDraw", "Puente basculante peatonal")
            .Style("CoveredWood", "Puente de madera cubierto");
    }

    private static UiStrings FrenchStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0} (non disponible)";
        text.StateStyleNotInstalled = "Rien d'installé ne fournit le style « {0} ». Choisissez-en un autre ci-dessous.";
        return text
            .Style("Suspension", "Pont suspendu")
            .Style("SuspensionGolden", "Pont suspendu (doré)")
            .Style("CableStayed", "Pont haubané (pylône en H)")
            .Style("Extradosed01", "Pont extradossé (pylône en V, deux niveaux)")
            .Style("Extradosed02", "Pont extradossé (pylône en A, deux niveaux)")
            .Style("Extradosed03", "Pont haubané (pylône en V)")
            .Style("ExtradosedLarge", "Pont haubané (pylône unique)")
            .Style("TrussArch", "Pont en arc à treillis (arc inférieur)")
            .Style("TrussArch01", "Pont en arc à treillis (arc supérieur · bleu)")
            .Style("TrussArch02", "Pont en arc à treillis (arc supérieur · blanc)")
            .Style("TrussArch03", "Pont en arc à treillis (arc supérieur · vert)")
            .Style("TiedArch", "Pont bow-string")
            .Style("Grand", "Grand pont")
            .Style("Draw", "Pont basculant")
            .Style("Lift", "Pont levant")
            .Style("PedestrianDraw", "Passerelle basculante")
            .Style("CoveredWood", "Pont de bois couvert");
    }

    private static UiStrings ItalianStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0} (non disponibile)";
        text.StateStyleNotInstalled = "Nulla di installato fornisce lo stile \"{0}\". Scegline un altro qui sotto.";
        return text
            .Style("Suspension", "Ponte sospeso")
            .Style("SuspensionGolden", "Ponte sospeso (dorato)")
            .Style("CableStayed", "Ponte strallato (pilone a H)")
            .Style("Extradosed01", "Ponte extradossato (pilone a V, due impalcati)")
            .Style("Extradosed02", "Ponte extradossato (pilone a A, due impalcati)")
            .Style("Extradosed03", "Ponte strallato (pilone a V)")
            .Style("ExtradosedLarge", "Ponte strallato (pilone singolo)")
            .Style("TrussArch", "Ponte ad arco reticolare (arco inferiore)")
            .Style("TrussArch01", "Ponte ad arco reticolare (arco superiore · blu)")
            .Style("TrussArch02", "Ponte ad arco reticolare (arco superiore · bianco)")
            .Style("TrussArch03", "Ponte ad arco reticolare (arco superiore · verde)")
            .Style("TiedArch", "Ponte ad arco a via inferiore")
            .Style("Grand", "Gran ponte")
            .Style("Draw", "Ponte basculante")
            .Style("Lift", "Ponte sollevabile")
            .Style("PedestrianDraw", "Passerella basculante")
            .Style("CoveredWood", "Ponte di legno coperto");
    }

    private static UiStrings PolishStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0} (niedostępny)";
        text.StateStyleNotInstalled = "Nic zainstalowanego nie dostarcza stylu \"{0}\". Wybierz inny poniżej.";
        return text
            .Style("Suspension", "Most wiszący")
            .Style("SuspensionGolden", "Most wiszący (złoty)")
            .Style("CableStayed", "Most podwieszany (pylon H)")
            .Style("Extradosed01", "Most extradosed (pylon V, dwupoziomowy)")
            .Style("Extradosed02", "Most extradosed (pylon A, dwupoziomowy)")
            .Style("Extradosed03", "Most podwieszany (pylon V)")
            .Style("ExtradosedLarge", "Most podwieszany (pylon pojedynczy)")
            .Style("TrussArch", "Kratownicowy most łukowy (łuk dolny)")
            .Style("TrussArch01", "Kratownicowy most łukowy (łuk górny · niebieski)")
            .Style("TrussArch02", "Kratownicowy most łukowy (łuk górny · biały)")
            .Style("TrussArch03", "Kratownicowy most łukowy (łuk górny · zielony)")
            .Style("TiedArch", "Most łukowy ze ściągiem")
            .Style("Grand", "Wielki most")
            .Style("Draw", "Most zwodzony")
            .Style("Lift", "Most podnoszony")
            .Style("PedestrianDraw", "Kładka zwodzona")
            .Style("CoveredWood", "Kryty most drewniany");
    }

    private static UiStrings PortugueseStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0} (indisponível)";
        text.StateStyleNotInstalled = "Nada instalado fornece o estilo \"{0}\". Escolha outro abaixo.";
        return text
            .Style("Suspension", "Ponte suspensa")
            .Style("SuspensionGolden", "Ponte suspensa (dourada)")
            .Style("CableStayed", "Ponte atirantada (pilone em H)")
            .Style("Extradosed01", "Ponte extradorsal (pilone em V, dois tabuleiros)")
            .Style("Extradosed02", "Ponte extradorsal (pilone em A, dois tabuleiros)")
            .Style("Extradosed03", "Ponte atirantada (pilone em V)")
            .Style("ExtradosedLarge", "Ponte atirantada (pilone único)")
            .Style("TrussArch", "Ponte em arco treliçado (arco inferior)")
            .Style("TrussArch01", "Ponte em arco treliçado (arco superior · azul)")
            .Style("TrussArch02", "Ponte em arco treliçado (arco superior · branca)")
            .Style("TrussArch03", "Ponte em arco treliçado (arco superior · verde)")
            .Style("TiedArch", "Ponte em arco atirantado")
            .Style("Grand", "Grande ponte")
            .Style("Draw", "Ponte basculante")
            .Style("Lift", "Ponte levadiça")
            .Style("PedestrianDraw", "Passarela basculante")
            .Style("CoveredWood", "Ponte de madeira coberta");
    }

    private static UiStrings RussianStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0} (недоступно)";
        text.StateStyleNotInstalled = "Ничто из установленного не предоставляет стиль «{0}». Выберите другой ниже.";
        return text
            .Style("Suspension", "Висячий мост")
            .Style("SuspensionGolden", "Висячий мост (золотой)")
            .Style("CableStayed", "Вантовый мост (H-пилон)")
            .Style("Extradosed01", "Экстрадозный мост (V-пилон, двухъярусный)")
            .Style("Extradosed02", "Экстрадозный мост (A-пилон, двухъярусный)")
            .Style("Extradosed03", "Вантовый мост (V-пилон)")
            .Style("ExtradosedLarge", "Вантовый мост (одностоечный пилон)")
            .Style("TrussArch", "Арочный ферменный мост (арка снизу)")
            .Style("TrussArch01", "Арочный ферменный мост (арка сверху · синий)")
            .Style("TrussArch02", "Арочный ферменный мост (арка сверху · белый)")
            .Style("TrussArch03", "Арочный ферменный мост (арка сверху · зелёный)")
            .Style("TiedArch", "Арочный мост с затяжкой")
            .Style("Grand", "Большой мост")
            .Style("Draw", "Разводной мост")
            .Style("Lift", "Подъёмный мост")
            .Style("PedestrianDraw", "Пешеходный разводной мост")
            .Style("CoveredWood", "Крытый деревянный мост");
    }
}
