namespace BridgeBuilder.Settings;

internal static partial class UiStringTables
{
    internal static void WithSettingsText(UiStrings text, string locale)
    {
        var labels = locale switch
        {
            "zh-HANS" => ("快捷键", "打开或关闭桥梁建造器。", "移除发展点数限制", "允许建造本模组生成的所有桥梁，不再提示桥梁未解锁。取消勾选后恢复原型桥梁的解锁条件。"),
            "zh-HANT" => ("快捷鍵", "開啟或關閉橋梁建造器。", "移除發展點數限制", "允許建造本模組產生的所有橋梁，不再提示橋梁未解鎖。取消勾選後恢復原型橋梁的解鎖條件。"),
            "de-DE" => ("Tastenkürzel", "Bridge Builder öffnen oder schließen.", "Entwicklungspunkte-Beschränkungen aufheben", "Alle mit diesem Mod erstellten Brücken ohne Freischaltwarnung bauen. Deaktivieren stellt die Freischaltbedingungen des Vorbilds wieder her."),
            "es-ES" => ("Atajo de teclado", "Abrir o cerrar Bridge Builder.", "Eliminar restricciones de puntos de desarrollo", "Construye todos los puentes creados por este mod sin avisos de bloqueo. Al desactivar, se restauran los requisitos del puente original."),
            "fr-FR" => ("Raccourci clavier", "Ouvrir ou fermer Bridge Builder.", "Supprimer les restrictions de points de développement", "Construire tous les ponts créés par ce mod sans avertissement de verrouillage. Désactiver rétablit les conditions du pont d’origine."),
            "it-IT" => ("Scorciatoia da tastiera", "Apri o chiudi Bridge Builder.", "Rimuovi le restrizioni dei punti sviluppo", "Costruisci tutti i ponti creati da questa mod senza avvisi di blocco. Disattivando, vengono ripristinati i requisiti del ponte originale."),
            "ja-JP" => ("ショートカットキー", "Bridge Builder を開く／閉じる。", "開発ポイントの制限を解除", "この MOD で作成したすべての橋を、未解除の警告なしで建設できます。オフにすると元の橋の解除条件に戻ります。"),
            "ko-KR" => ("단축키", "Bridge Builder를 열거나 닫습니다.", "개발 포인트 제한 해제", "이 모드로 만든 모든 다리를 잠금 경고 없이 건설합니다. 끄면 원본 다리의 잠금 해제 조건이 복원됩니다."),
            "pl-PL" => ("Skrót klawiszowy", "Otwórz lub zamknij Bridge Builder.", "Usuń ograniczenia punktów rozwoju", "Buduj wszystkie mosty utworzone przez ten mod bez ostrzeżeń o blokadzie. Wyłączenie przywraca wymagania oryginalnego mostu."),
            "pt-BR" => ("Atalho de teclado", "Abrir ou fechar o Bridge Builder.", "Remover restrições de pontos de desenvolvimento", "Construa todas as pontes criadas por este mod sem avisos de bloqueio. Desativar restaura os requisitos da ponte original."),
            "ru-RU" => ("Горячая клавиша", "Открыть или закрыть Bridge Builder.", "Снять ограничения очков развития", "Позволяет строить все мосты этого мода без предупреждений о блокировке. Отключение восстанавливает условия разблокировки исходного моста."),
            _ => ("Hotkey", "Open or close Bridge Builder.", "Remove development point restrictions", "Build all bridges created by this mod without locked-bridge warnings. Disable to restore each prototype’s unlock requirements."),
        };
        text.Option(nameof(BridgeSetting.TogglePanel), labels.Item1, labels.Item2);
        text.Option(nameof(BridgeSetting.RemoveDevelopmentRestrictions), labels.Item3, labels.Item4);
    }
}
