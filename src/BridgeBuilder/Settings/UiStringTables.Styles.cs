namespace BridgeBuilder.Settings;

/// <summary>
/// The names of the bridge styles, in every shipped language.
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
            .Style("Suspension", "Blue Suspension Bridge")
            .Style("SuspensionDouble", "Blue Double-Deck Suspension Bridge")
            .Style("Suspension01", "Gray Suspension Bridge")
            .Style("Suspension02", "Gray Double-Deck Suspension Bridge")
            .Style("SuspensionGolden", "Golden Suspension Bridge")
            .Style("GoldenGate", "Golden Gate Suspension Bridge")
            .Style("GoldenGateDouble", "Golden Gate Double-Deck Suspension Bridge")
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
            .Style("Grand", "Extra-Large Suspension Bridge")
            .Style("Draw", "Bascule Bridge")
            .Style("Draw01", "Curved-Soffit Girder Bascule Bridge")
            .Style("Draw02", "Solid-Web Girder Bascule Bridge")
            .Style("Draw03", "Deck-Truss Bascule Bridge")
            .Style("Lift", "Lift Bridge")
            .Style("Lift01", "Portal-Frame Lift Bridge")
            .Style("Lift02", "Curved-Chord Truss Lift Bridge")
            .Style("Lift03", "Double-Deck Lift Bridge")
            .Style("Lift04", "Straight-Tower Truss Lift Bridge")
            .Style("Lift05", "Curved-Brace Tower Lift Bridge")
            .Style("PedestrianDraw", "Pedestrian Bascule Bridge")
            .Style("PedestrianDraw01", "Fan-Cable Pedestrian Bascule Bridge")
            .Style("PedestrianDraw02", "Overhead-Beam Pedestrian Bascule Bridge")
            .Style("CoveredWood", "Covered Wooden Bridge")
            .Style("WoodenCovered", "Wooden Covered Bridge");
    }

    private static UiStrings SimplifiedChineseStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0}（不可用）";
        text.StateStyleNotInstalled = "本机没有提供样式“{0}”的内容。请在下方换一个。";
        return text
            .Style("Suspension", "悬索桥（蓝色）")
            .Style("SuspensionDouble", "双层悬索桥（蓝色）")
            .Style("Suspension01", "悬索桥（灰色）")
            .Style("Suspension02", "双层悬索桥（灰色）")
            .Style("SuspensionGolden", "悬索桥（金色）")
            .Style("GoldenGate", "悬索桥（金门大桥）")
            .Style("GoldenGateDouble", "双层悬索桥（金门大桥）")
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
            .Style("Grand", "悬索桥（特大桥）")
            .Style("Draw", "开合桥")
            .Style("Draw01", "弧底梁式开合桥")
            .Style("Draw02", "实腹梁式开合桥")
            .Style("Draw03", "桥下桁架开合桥")
            .Style("Lift", "升降桥")
            .Style("Lift01", "门架式升降桥")
            .Style("Lift02", "弧顶桁架升降桥")
            .Style("Lift03", "双层升降桥")
            .Style("Lift04", "直塔桁架升降桥")
            .Style("Lift05", "弯撑塔式升降桥")
            .Style("PedestrianDraw", "人行开合桥")
            .Style("PedestrianDraw01", "扇索人行开合桥")
            .Style("PedestrianDraw02", "挑梁式人行开合桥")
            .Style("CoveredWood", "人行木廊桥")
            .Style("WoodenCovered", "木质廊桥");
    }

    private static UiStrings TraditionalChineseStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0}（不可用）";
        text.StateStyleNotInstalled = "本機沒有提供樣式「{0}」的內容。請在下方換一個。";
        return text
            .Style("Suspension", "懸索橋（藍色）")
            .Style("SuspensionDouble", "雙層懸索橋（藍色）")
            .Style("Suspension01", "懸索橋（灰色）")
            .Style("Suspension02", "雙層懸索橋（灰色）")
            .Style("SuspensionGolden", "懸索橋（金色）")
            .Style("GoldenGate", "懸索橋（金門大橋）")
            .Style("GoldenGateDouble", "雙層懸索橋（金門大橋）")
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
            .Style("Grand", "懸索橋（特大橋）")
            .Style("Draw", "開合橋")
            .Style("Draw01", "弧底梁式開合橋")
            .Style("Draw02", "實腹梁式開合橋")
            .Style("Draw03", "橋下桁架開合橋")
            .Style("Lift", "升降橋")
            .Style("Lift01", "門架式升降橋")
            .Style("Lift02", "弧頂桁架升降橋")
            .Style("Lift03", "雙層升降橋")
            .Style("Lift04", "直塔桁架升降橋")
            .Style("Lift05", "彎撐塔式升降橋")
            .Style("PedestrianDraw", "人行開合橋")
            .Style("PedestrianDraw01", "扇索人行開合橋")
            .Style("PedestrianDraw02", "挑梁式人行開合橋")
            .Style("CoveredWood", "人行木廊橋")
            .Style("WoodenCovered", "木造廊橋");
    }

    private static UiStrings JapaneseStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0}（利用不可）";
        text.StateStyleNotInstalled = "スタイル「{0}」を提供するものが導入されていません。下から別のものを選んでください。";
        return text
            .Style("Suspension", "吊り橋（青）")
            .Style("SuspensionDouble", "二層吊り橋（青）")
            .Style("Suspension01", "吊り橋（灰色）")
            .Style("Suspension02", "二層吊り橋（灰色）")
            .Style("SuspensionGolden", "吊り橋（金色）")
            .Style("GoldenGate", "吊り橋（ゴールデンゲート）")
            .Style("GoldenGateDouble", "二層吊り橋（ゴールデンゲート）")
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
            .Style("Grand", "吊り橋（特大）")
            .Style("Draw", "跳ね橋")
            .Style("Draw01", "桁下が弧を描く跳ね橋")
            .Style("Draw02", "板桁式跳ね橋")
            .Style("Draw03", "上路トラス式跳ね橋")
            .Style("Lift", "昇開橋")
            .Style("Lift01", "門型昇開橋")
            .Style("Lift02", "曲弦トラス昇開橋")
            .Style("Lift03", "二層式昇開橋")
            .Style("Lift04", "直塔トラス昇開橋")
            .Style("Lift05", "曲線控え柱の昇開橋")
            .Style("PedestrianDraw", "歩行者用跳ね橋")
            .Style("PedestrianDraw01", "扇状ケーブルの歩行者用跳ね橋")
            .Style("PedestrianDraw02", "上部張出し梁の歩行者用跳ね橋")
            .Style("CoveredWood", "歩行者用屋根付き木橋")
            .Style("WoodenCovered", "屋根付き木橋");
    }

    private static UiStrings KoreanStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0} (사용 불가)";
        text.StateStyleNotInstalled = "스타일 \"{0}\"을(를) 제공하는 콘텐츠가 없습니다. 아래에서 다른 것을 고르세요.";
        return text
            .Style("Suspension", "현수교 (파란색)")
            .Style("SuspensionDouble", "복층 현수교 (파란색)")
            .Style("Suspension01", "현수교 (회색)")
            .Style("Suspension02", "복층 현수교 (회색)")
            .Style("SuspensionGolden", "현수교 (금색)")
            .Style("GoldenGate", "현수교 (골든게이트)")
            .Style("GoldenGateDouble", "복층 현수교 (골든게이트)")
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
            .Style("Grand", "현수교 (초대형)")
            .Style("Draw", "도개교")
            .Style("Draw01", "아랫면이 곡선인 거더 도개교")
            .Style("Draw02", "판형 거더 도개교")
            .Style("Draw03", "상로 트러스 도개교")
            .Style("Lift", "승개교")
            .Style("Lift01", "문형 승개교")
            .Style("Lift02", "곡현 트러스 승개교")
            .Style("Lift03", "복층 승개교")
            .Style("Lift04", "직립 탑 트러스 승개교")
            .Style("Lift05", "곡선 버팀탑 승개교")
            .Style("PedestrianDraw", "보행자 도개교")
            .Style("PedestrianDraw01", "부채꼴 케이블 보행자 도개교")
            .Style("PedestrianDraw02", "상부 돌출보 보행자 도개교")
            .Style("CoveredWood", "보행자용 지붕 목교")
            .Style("WoodenCovered", "지붕 있는 목교");
    }

    private static UiStrings GermanStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0} (nicht verfügbar)";
        text.StateStyleNotInstalled = "Für den Stil \"{0}\" ist nichts installiert. Wähle unten einen anderen.";
        return text
            .Style("Suspension", "Hängebrücke (blau)")
            .Style("SuspensionDouble", "Doppelstock-Hängebrücke (blau)")
            .Style("Suspension01", "Hängebrücke (grau)")
            .Style("Suspension02", "Doppelstock-Hängebrücke (grau)")
            .Style("SuspensionGolden", "Hängebrücke (golden)")
            .Style("GoldenGate", "Golden-Gate-Hängebrücke")
            .Style("GoldenGateDouble", "Doppelstock-Golden-Gate-Hängebrücke")
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
            .Style("Grand", "Hängebrücke (extra groß)")
            .Style("Draw", "Klappbrücke")
            .Style("Draw01", "Klappbrücke mit geschwungenen Trägerunterseiten")
            .Style("Draw02", "Vollwandträger-Klappbrücke")
            .Style("Draw03", "Klappbrücke mit untenliegendem Fachwerk")
            .Style("Lift", "Hubbrücke")
            .Style("Lift01", "Portalhubbrücke")
            .Style("Lift02", "Fachwerkhubbrücke mit gebogenem Obergurt")
            .Style("Lift03", "Doppelstock-Hubbrücke")
            .Style("Lift04", "Fachwerkhubbrücke mit geraden Türmen")
            .Style("Lift05", "Hubbrücke mit geschwungenen Turmstreben")
            .Style("PedestrianDraw", "Fußgänger-Klappbrücke")
            .Style("PedestrianDraw01", "Fußgänger-Klappbrücke mit Fächerseilen")
            .Style("PedestrianDraw02", "Fußgänger-Klappbrücke mit oberen Auslegern")
            .Style("CoveredWood", "Gedeckte Fußgänger-Holzbrücke")
            .Style("WoodenCovered", "Gedeckte Holzbrücke");
    }

    private static UiStrings SpanishStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0} (no disponible)";
        text.StateStyleNotInstalled = "No hay nada instalado que proporcione el estilo \"{0}\". Elige otro abajo.";
        return text
            .Style("Suspension", "Puente colgante (azul)")
            .Style("SuspensionDouble", "Puente colgante de dos niveles (azul)")
            .Style("Suspension01", "Puente colgante (gris)")
            .Style("Suspension02", "Puente colgante de dos niveles (gris)")
            .Style("SuspensionGolden", "Puente colgante (dorado)")
            .Style("GoldenGate", "Puente colgante Golden Gate")
            .Style("GoldenGateDouble", "Puente colgante Golden Gate de dos niveles")
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
            .Style("Grand", "Puente colgante extragrande")
            .Style("Draw", "Puente basculante")
            .Style("Draw01", "Puente basculante de vigas de intradós curvo")
            .Style("Draw02", "Puente basculante de vigas de alma llena")
            .Style("Draw03", "Puente basculante con celosía bajo el tablero")
            .Style("Lift", "Puente elevadizo")
            .Style("Lift01", "Puente elevadizo de pórtico")
            .Style("Lift02", "Puente elevadizo de celosía con cordón superior curvo")
            .Style("Lift03", "Puente elevadizo de doble tablero")
            .Style("Lift04", "Puente elevadizo de celosía con torres rectas")
            .Style("Lift05", "Puente elevadizo con contrafuertes curvos")
            .Style("PedestrianDraw", "Puente basculante peatonal")
            .Style("PedestrianDraw01", "Pasarela basculante con cables en abanico")
            .Style("PedestrianDraw02", "Pasarela basculante con vigas superiores en voladizo")
            .Style("CoveredWood", "Pasarela peatonal de madera cubierta")
            .Style("WoodenCovered", "Puente de madera cubierto");
    }

    private static UiStrings FrenchStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0} (non disponible)";
        text.StateStyleNotInstalled = "Rien d'installé ne fournit le style « {0} ». Choisissez-en un autre ci-dessous.";
        return text
            .Style("Suspension", "Pont suspendu (bleu)")
            .Style("SuspensionDouble", "Pont suspendu à deux niveaux (bleu)")
            .Style("Suspension01", "Pont suspendu (gris)")
            .Style("Suspension02", "Pont suspendu à deux niveaux (gris)")
            .Style("SuspensionGolden", "Pont suspendu (doré)")
            .Style("GoldenGate", "Pont suspendu Golden Gate")
            .Style("GoldenGateDouble", "Pont suspendu Golden Gate à deux niveaux")
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
            .Style("Grand", "Pont suspendu extra-large")
            .Style("Draw", "Pont basculant")
            .Style("Draw01", "Pont basculant à poutres à intrados courbe")
            .Style("Draw02", "Pont basculant à poutres à âme pleine")
            .Style("Draw03", "Pont basculant à treillis sous tablier")
            .Style("Lift", "Pont levant")
            .Style("Lift01", "Pont levant à portique")
            .Style("Lift02", "Pont levant à treillis à membrure supérieure courbe")
            .Style("Lift03", "Pont levant à double tablier")
            .Style("Lift04", "Pont levant en treillis à tours droites")
            .Style("Lift05", "Pont levant à tours à contreforts courbes")
            .Style("PedestrianDraw", "Passerelle basculante")
            .Style("PedestrianDraw01", "Passerelle basculante à câbles en éventail")
            .Style("PedestrianDraw02", "Passerelle basculante à poutres supérieures en porte-à-faux")
            .Style("CoveredWood", "Passerelle piétonne couverte en bois")
            .Style("WoodenCovered", "Pont de bois couvert");
    }

    private static UiStrings ItalianStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0} (non disponibile)";
        text.StateStyleNotInstalled = "Nulla di installato fornisce lo stile \"{0}\". Scegline un altro qui sotto.";
        return text
            .Style("Suspension", "Ponte sospeso (blu)")
            .Style("SuspensionDouble", "Ponte sospeso a due livelli (blu)")
            .Style("Suspension01", "Ponte sospeso (grigio)")
            .Style("Suspension02", "Ponte sospeso a due livelli (grigio)")
            .Style("SuspensionGolden", "Ponte sospeso (dorato)")
            .Style("GoldenGate", "Ponte sospeso Golden Gate")
            .Style("GoldenGateDouble", "Ponte sospeso Golden Gate a due livelli")
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
            .Style("Grand", "Ponte sospeso extra-large")
            .Style("Draw", "Ponte basculante")
            .Style("Draw01", "Ponte basculante a travi con intradosso curvo")
            .Style("Draw02", "Ponte basculante a travi a parete piena")
            .Style("Draw03", "Ponte basculante a traliccio sotto l’impalcato")
            .Style("Lift", "Ponte sollevabile")
            .Style("Lift01", "Ponte sollevabile a portale")
            .Style("Lift02", "Ponte sollevabile a traliccio con corrente superiore curvo")
            .Style("Lift03", "Ponte sollevabile a doppio impalcato")
            .Style("Lift04", "Ponte sollevabile a traliccio con torri dritte")
            .Style("Lift05", "Ponte sollevabile con contrafforti curvi")
            .Style("PedestrianDraw", "Passerella basculante")
            .Style("PedestrianDraw01", "Passerella basculante con cavi a ventaglio")
            .Style("PedestrianDraw02", "Passerella basculante con travi superiori a sbalzo")
            .Style("CoveredWood", "Passerella pedonale coperta in legno")
            .Style("WoodenCovered", "Ponte di legno coperto");
    }

    private static UiStrings PolishStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0} (niedostępny)";
        text.StateStyleNotInstalled = "Nic zainstalowanego nie dostarcza stylu \"{0}\". Wybierz inny poniżej.";
        return text
            .Style("Suspension", "Most wiszący (niebieski)")
            .Style("SuspensionDouble", "Dwupoziomowy most wiszący (niebieski)")
            .Style("Suspension01", "Most wiszący (szary)")
            .Style("Suspension02", "Dwupoziomowy most wiszący (szary)")
            .Style("SuspensionGolden", "Most wiszący (złoty)")
            .Style("GoldenGate", "Most wiszący Golden Gate")
            .Style("GoldenGateDouble", "Dwupoziomowy most wiszący Golden Gate")
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
            .Style("Grand", "Bardzo duży most wiszący")
            .Style("Draw", "Most zwodzony")
            .Style("Draw01", "Most zwodzony z dźwigarami o łukowym spodzie")
            .Style("Draw02", "Most zwodzony z dźwigarami pełnościennymi")
            .Style("Draw03", "Most zwodzony z kratownicą pod pomostem")
            .Style("Lift", "Most podnoszony")
            .Style("Lift01", "Bramowy most podnoszony")
            .Style("Lift02", "Most podnoszony z kratownicą o łukowym pasie górnym")
            .Style("Lift03", "Dwupoziomowy most podnoszony")
            .Style("Lift04", "Kratownicowy most podnoszony z prostymi wieżami")
            .Style("Lift05", "Most podnoszony z łukowymi zastrzałami wież")
            .Style("PedestrianDraw", "Kładka zwodzona")
            .Style("PedestrianDraw01", "Kładka zwodzona z wachlarzem lin")
            .Style("PedestrianDraw02", "Kładka zwodzona z górnymi wspornikami")
            .Style("CoveredWood", "Kryta drewniana kładka")
            .Style("WoodenCovered", "Kryty most drewniany");
    }

    private static UiStrings PortugueseStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0} (indisponível)";
        text.StateStyleNotInstalled = "Nada instalado fornece o estilo \"{0}\". Escolha outro abaixo.";
        return text
            .Style("Suspension", "Ponte suspensa (azul)")
            .Style("SuspensionDouble", "Ponte suspensa de dois níveis (azul)")
            .Style("Suspension01", "Ponte suspensa (cinza)")
            .Style("Suspension02", "Ponte suspensa de dois níveis (cinza)")
            .Style("SuspensionGolden", "Ponte suspensa (dourada)")
            .Style("GoldenGate", "Ponte suspensa Golden Gate")
            .Style("GoldenGateDouble", "Ponte suspensa Golden Gate de dois níveis")
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
            .Style("Grand", "Ponte suspensa extragrande")
            .Style("Draw", "Ponte basculante")
            .Style("Draw01", "Ponte basculante com vigas de face inferior curva")
            .Style("Draw02", "Ponte basculante com vigas de alma cheia")
            .Style("Draw03", "Ponte basculante com treliça sob o tabuleiro")
            .Style("Lift", "Ponte levadiça")
            .Style("Lift01", "Ponte levadiça de pórtico")
            .Style("Lift02", "Ponte levadiça de treliça com banzo superior curvo")
            .Style("Lift03", "Ponte levadiça de tabuleiro duplo")
            .Style("Lift04", "Ponte levadiça de treliça com torres retas")
            .Style("Lift05", "Ponte levadiça com contrafortes curvos")
            .Style("PedestrianDraw", "Passarela basculante")
            .Style("PedestrianDraw01", "Passarela basculante com cabos em leque")
            .Style("PedestrianDraw02", "Passarela basculante com vigas superiores em balanço")
            .Style("CoveredWood", "Passarela pedonal coberta de madeira")
            .Style("WoodenCovered", "Ponte de madeira coberta");
    }

    private static UiStrings RussianStyles(UiStrings text)
    {
        text.StyleNotAvailable = "{0} (недоступно)";
        text.StateStyleNotInstalled = "Ничто из установленного не предоставляет стиль «{0}». Выберите другой ниже.";
        return text
            .Style("Suspension", "Висячий мост (синий)")
            .Style("SuspensionDouble", "Двухъярусный висячий мост (синий)")
            .Style("Suspension01", "Висячий мост (серый)")
            .Style("Suspension02", "Двухъярусный висячий мост (серый)")
            .Style("SuspensionGolden", "Висячий мост (золотой)")
            .Style("GoldenGate", "Висячий мост Golden Gate")
            .Style("GoldenGateDouble", "Двухъярусный висячий мост Golden Gate")
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
            .Style("Grand", "Висячий мост (сверхбольшой)")
            .Style("Draw", "Разводной мост")
            .Style("Draw01", "Раскрывающийся мост с балками криволинейного очертания")
            .Style("Draw02", "Раскрывающийся мост со сплошностенчатыми балками")
            .Style("Draw03", "Раскрывающийся мост с фермами под настилом")
            .Style("Lift", "Подъёмный мост")
            .Style("Lift01", "Портальный подъёмный мост")
            .Style("Lift02", "Подъёмный мост с фермой арочного очертания")
            .Style("Lift03", "Двухъярусный подъёмный мост")
            .Style("Lift04", "Ферменный подъёмный мост с прямыми башнями")
            .Style("Lift05", "Подъёмный мост с изогнутыми подкосами башен")
            .Style("PedestrianDraw", "Пешеходный разводной мост")
            .Style("PedestrianDraw01", "Пешеходный разводной мост с веерными тросами")
            .Style("PedestrianDraw02", "Пешеходный разводной мост с верхними консолями")
            .Style("CoveredWood", "Крытый деревянный пешеходный мост")
            .Style("WoodenCovered", "Крытый деревянный мост");
    }
}
