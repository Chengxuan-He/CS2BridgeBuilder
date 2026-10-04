using System;
using System.Collections.Generic;
using System.Globalization;

namespace BridgeBuilder.Settings;

/// <summary>Shared runtime UI translations. Never use translated text as an asset identifier.</summary>
internal static class RuntimeUiText
{
    // Column order is explicit and checked by tools/CheckRuntimeLocalization.mjs.
    internal static readonly string[] LocaleIds =
    {
        "en-US", "zh-HANS", "zh-HANT", "de-DE", "es-ES", "fr-FR",
        "it-IT", "ja-JP", "ko-KR", "pl-PL", "pt-BR", "ru-RU"
    };

    private static readonly Dictionary<string, string[]> Entries = new(StringComparer.Ordinal)
    {
        ["RecoveryLocationLabel"] = new[]
        {
            "Recovery copy location",
            "恢复副本位置",
            "復原副本位置",
            "Speicherort der Wiederherstellungskopien",
            "Ubicación de las copias de recuperación",
            "Emplacement des copies de récupération",
            "Percorso delle copie di recupero",
            "復元用コピーの保存先",
            "복구 사본 위치",
            "Lokalizacja kopii odzyskiwania",
            "Local das cópias de recuperação",
            "Расположение копий для восстановления"
        },
        ["RecoveryOpenLabel"] = new[]
        {
            "Open backup directory",
            "打开备份目录",
            "開啟備份目錄",
            "Sicherungsordner öffnen",
            "Abrir carpeta de copias de seguridad",
            "Ouvrir le dossier de sauvegarde",
            "Aprire la cartella di backup",
            "バックアップフォルダーを開く",
            "백업 폴더 열기",
            "Otwórz folder kopii zapasowych",
            "Abrir pasta de backup",
            "Открыть папку резервных копий"
        },
        ["RecoveryLocationDescription"] = new[]
        {
            "Select the folder for new recovery copies. Existing copies remain in their original location. An empty value restores the default folder.",
            "选择新恢复副本的保存目录。已有副本保留在原位置。留空将恢复默认目录。",
            "選擇新復原副本的儲存目錄。既有副本保留於原位置。留空將恢復預設目錄。",
            "Wählen Sie den Ordner für neue Wiederherstellungskopien. Vorhandene Kopien bleiben am bisherigen Ort. Ein leeres Feld stellt den Standardordner wieder her.",
            "Seleccione la carpeta para las nuevas copias de recuperación. Las copias existentes permanecen en su ubicación original. Dejar el campo vacío restaura la carpeta predeterminada.",
            "Sélectionnez le dossier des nouvelles copies de récupération. Les copies existantes restent à leur emplacement initial. Une valeur vide rétablit le dossier par défaut.",
            "Selezionare la cartella per le nuove copie di recupero. Le copie esistenti rimangono nel percorso originale. Un valore vuoto ripristina la cartella predefinita.",
            "新しい復元用コピーの保存先を選択します。既存のコピーは元の場所に残ります。空欄にすると既定のフォルダーに戻ります。",
            "새 복구 사본을 저장할 폴더를 선택합니다. 기존 사본은 원래 위치에 유지됩니다. 비워 두면 기본 폴더로 복원됩니다.",
            "Wybierz folder dla nowych kopii odzyskiwania. Istniejące kopie pozostaną w dotychczasowej lokalizacji. Puste pole przywraca folder domyślny.",
            "Selecione a pasta para novas cópias de recuperação. As cópias existentes permanecem no local original. Um valor vazio restaura a pasta padrão.",
            "Выберите папку для новых копий восстановления. Существующие копии останутся на прежнем месте. Пустое поле восстанавливает папку по умолчанию."
        },
        ["RecoveryInvalidLocation"] = new[]
        {
            "Select a valid absolute folder path. The previous recovery location has been retained.",
            "请选择有效的文件夹绝对路径。已保留原恢复副本位置。",
            "請選擇有效的資料夾絕對路徑。已保留原復原副本位置。",
            "Wählen Sie einen gültigen absoluten Ordnerpfad. Der bisherige Speicherort wurde beibehalten.",
            "Seleccione una ruta absoluta de carpeta válida. Se ha conservado la ubicación anterior.",
            "Sélectionnez un chemin absolu de dossier valide. L’emplacement précédent a été conservé.",
            "Selezionare un percorso assoluto di cartella valido. Il percorso precedente è stato mantenuto.",
            "有効なフォルダーの絶対パスを選択してください。以前の保存先は保持されています。",
            "유효한 폴더의 절대 경로를 선택하십시오. 이전 복구 위치가 유지되었습니다.",
            "Wybierz prawidłową bezwzględną ścieżkę folderu. Zachowano poprzednią lokalizację.",
            "Selecione um caminho absoluto de pasta válido. O local anterior foi mantido.",
            "Выберите допустимый абсолютный путь к папке. Прежнее расположение сохранено."
        },
        ["RecoveryOpenFailed"] = new[]
        {
            "The recovery folder could not be opened. Check the path and access permissions shown in the mod settings.",
            "无法打开恢复副本文件夹。请检查模组设置中显示的路径及其访问权限。",
            "無法開啟復原副本資料夾。請檢查模組設定顯示的路徑及其存取權限。",
            "Der Wiederherstellungsordner konnte nicht geöffnet werden. Prüfen Sie den Pfad und die Zugriffsrechte in den Mod-Einstellungen.",
            "No se pudo abrir la carpeta de recuperación. Compruebe la ruta y los permisos de acceso en los ajustes del mod.",
            "Impossible d’ouvrir le dossier de récupération. Vérifiez le chemin et les droits d’accès dans les paramètres du mod.",
            "Impossibile aprire la cartella di recupero. Verificare il percorso e i permessi di accesso nelle impostazioni della mod.",
            "復元用フォルダーを開けませんでした。MOD 設定に表示されているパスとアクセス権を確認してください。",
            "복구 폴더를 열 수 없습니다. 모드 설정에 표시된 경로와 접근 권한을 확인하십시오.",
            "Nie można otworzyć folderu odzyskiwania. Sprawdź ścieżkę i uprawnienia dostępu w ustawieniach moda.",
            "Não foi possível abrir a pasta de recuperação. Verifique o caminho e as permissões de acesso nas configurações do mod.",
            "Не удалось открыть папку восстановления. Проверьте путь и права доступа в настройках мода."
        },
        ["MissingBridgesDetected"] = new[]
        {
            "This save references {0} missing Bridge Builder bridge asset(s). Restore the assets and required dependencies before reloading the save, or create new bridges to replace the missing bridges.",
            "此存档引用的 {0} 种 Bridge Builder 桥梁资产缺失。请恢复相关资产及必要依赖后重新加载存档，或创建新桥梁以替换缺失桥梁。",
            "此存檔引用的 {0} 種 Bridge Builder 橋梁資產缺失。請恢復相關資產及必要相依內容後重新載入存檔，或建立新橋梁以替換缺失橋梁。",
            "Dieser Spielstand verweist auf {0} fehlende Bridge-Builder-Brückenassets. Stellen Sie die Assets und erforderlichen Abhängigkeiten wieder her und laden Sie den Spielstand erneut, oder erstellen Sie neue Brücken als Ersatz für die fehlenden.",
            "Esta partida hace referencia a {0} recursos de puentes de Bridge Builder que faltan. Restaure los recursos y las dependencias necesarias antes de volver a cargar la partida, o cree nuevos puentes para sustituir los que faltan.",
            "Cette sauvegarde fait référence à {0} ressources de ponts Bridge Builder manquantes. Restaurez les ressources et les dépendances requises avant de recharger la sauvegarde, ou créez de nouveaux ponts pour remplacer les ponts manquants.",
            "Questo salvataggio fa riferimento a {0} risorse di ponti Bridge Builder mancanti. Ripristinare le risorse e le dipendenze necessarie prima di ricaricare il salvataggio, oppure creare nuovi ponti per sostituire quelli mancanti.",
            "このセーブデータが参照する Bridge Builder の橋梁アセット {0} 種類が不足しています。該当アセットと必要な依存コンテンツを復元してからセーブデータを再読み込みするか、新規橋梁を作成して不足している橋梁を置き換えてください。",
            "이 저장 파일이 참조하는 Bridge Builder 교량 에셋 {0}종이 누락되었습니다. 관련 에셋과 필수 의존 콘텐츠를 복원한 후 저장 파일을 다시 불러오거나, 새 교량을 생성하여 누락된 교량을 교체하십시오.",
            "Ten zapis odwołuje się do {0} brakujących zasobów mostów Bridge Builder. Należy przywrócić zasoby i wymagane zależności przed ponownym wczytaniem zapisu lub utworzyć nowe mosty w celu zastąpienia brakujących.",
            "Este salvamento faz referência a {0} recursos de pontes do Bridge Builder ausentes. Restaure os recursos e as dependências necessárias antes de recarregar o salvamento ou crie novas pontes para substituir as ausentes.",
            "В сохранении обнаружены ссылки на отсутствующие ресурсы мостов Bridge Builder: {0}. Восстановите ресурсы и необходимые зависимости перед повторной загрузкой сохранения либо создайте новые мосты для замены отсутствующих."
        },
        ["DamagedBridgeAssetsRemoved"] = new[]
        {
            "Startup validation identified {0} damaged Bridge Builder asset groups. The affected bridge files have been moved to the backup directory, which is accessible from the mod settings.",
            "启动检查发现 {0} 组损坏的 Bridge Builder 桥梁资产，相关文件已移入备份目录。备份目录可通过模组设置访问。",
            "啟動檢查發現 {0} 組損壞的 Bridge Builder 橋梁資產，相關檔案已移至備份目錄。備份目錄可透過模組設定存取。",
            "Die Startprüfung hat {0} beschädigte Bridge-Builder-Assetgruppen erkannt. Die betroffenen Dateien wurden in den Sicherungsordner verschoben, der über die Mod-Einstellungen zugänglich ist.",
            "La comprobación inicial detectó {0} grupos de recursos de puentes de Bridge Builder dañados. Los archivos afectados se trasladaron a la carpeta de copias de seguridad, accesible desde los ajustes del mod.",
            "La vérification au démarrage a détecté {0} groupes de ressources de ponts Bridge Builder endommagés. Les fichiers concernés ont été déplacés vers le dossier de sauvegarde, accessible dans les paramètres du mod.",
            "Il controllo all’avvio ha rilevato {0} gruppi di risorse di ponti Bridge Builder danneggiati. I file interessati sono stati spostati nella cartella di backup, accessibile dalle impostazioni della mod.",
            "起動時の検証で、破損した Bridge Builder の橋梁アセットが {0} 組検出されました。該当ファイルはバックアップフォルダーに移動済みです。保存先は MOD 設定から確認できます。",
            "시작 시 검사에서 손상된 Bridge Builder 교량 에셋 {0}개 그룹이 발견되었습니다. 해당 파일은 백업 폴더로 이동되었습니다. 백업 위치는 모드 설정에서 확인할 수 있습니다.",
            "Podczas kontroli przy uruchamianiu wykryto {0} grup uszkodzonych zasobów mostów Bridge Builder. Pliki przeniesiono do folderu kopii zapasowych, dostępnego w ustawieniach moda.",
            "A verificação inicial identificou {0} grupos de recursos de pontes do Bridge Builder danificados. Os arquivos afetados foram movidos para a pasta de backup, acessível nas configurações do mod.",
            "При проверке во время запуска обнаружено повреждённых групп ресурсов мостов Bridge Builder: {0}. Соответствующие файлы перемещены в папку резервных копий, доступную в настройках мода."
        },
        ["BridgeReferencesDeferred"] = new[]
        {
            "Startup validation could not resolve dependencies for {0} Bridge Builder asset groups. The original files remain in place and have not been moved to the backup directory. Restore the required assets and restart the game.",
            "启动检查发现 {0} 组 Bridge Builder 桥梁资产的依赖无法解析。原文件已保留，未移入备份目录。请恢复所需资产后重新启动游戏。",
            "啟動檢查發現 {0} 組 Bridge Builder 橋梁資產的相依內容無法解析。原始檔案已保留，未移至備份目錄。請恢復所需資產後重新啟動遊戲。",
            "Bei der Startprüfung konnten Abhängigkeiten von {0} Bridge-Builder-Assetgruppen nicht aufgelöst werden. Die Originaldateien bleiben unverändert am bisherigen Ort und wurden nicht in den Sicherungsordner verschoben. Stellen Sie die benötigten Assets wieder her und starten Sie das Spiel neu.",
            "La comprobación inicial no pudo resolver las dependencias de {0} grupos de recursos de Bridge Builder. Los archivos originales permanecen en su ubicación y no se trasladaron a la carpeta de copias de seguridad. Restaure los recursos necesarios y reinicie el juego.",
            "La vérification au démarrage n’a pas pu résoudre les dépendances de {0} groupes de ressources Bridge Builder. Les fichiers originaux sont conservés à leur emplacement et n’ont pas été déplacés vers le dossier de sauvegarde. Restaurez les ressources requises et redémarrez le jeu.",
            "Il controllo all’avvio non ha risolto le dipendenze di {0} gruppi di risorse Bridge Builder. I file originali sono conservati nel percorso originale e non sono stati spostati nella cartella di backup. Ripristinare le risorse necessarie e riavviare il gioco.",
            "起動時の検証で、Bridge Builder のアセット {0} 組の依存関係を解決できませんでした。元のファイルはそのまま保持され、バックアップフォルダーには移動されていません。必要なアセットを復元してゲームを再起動してください。",
            "시작 시 검사에서 Bridge Builder 에셋 {0}개 그룹의 의존성을 확인할 수 없었습니다. 원본 파일은 기존 위치에 보존되었으며 백업 폴더로 이동되지 않았습니다. 필요한 에셋을 복원한 후 게임을 다시 시작하십시오.",
            "Podczas kontroli przy uruchamianiu nie udało się rozwiązać zależności {0} grup zasobów Bridge Builder. Oryginalne pliki pozostają na swoim miejscu i nie zostały przeniesione do folderu kopii zapasowych. Przywróć wymagane zasoby i uruchom grę ponownie.",
            "A verificação inicial não conseguiu resolver as dependências de {0} grupos de recursos do Bridge Builder. Os arquivos originais permanecem no local e não foram movidos para a pasta de backup. Restaure os recursos necessários e reinicie o jogo.",
            "При проверке во время запуска не удалось разрешить зависимости групп ресурсов Bridge Builder: {0}. Исходные файлы сохранены на прежнем месте и не перемещены в папку резервных копий. Восстановите необходимые ресурсы и перезапустите игру."
        },
        ["OK"] = new[]
        {
            "OK", "确定", "確定", "OK", "Aceptar", "OK",
            "OK", "確認", "확인", "OK", "OK", "ОК"
        },
        ["MissingBridgesBlocked"] = new[]
        {
            "Bridge cleanup was blocked by an invalid connecting road or track. Restore the missing dependencies and reload; keep a copy of the original save.",
            "桥梁清理因相连道路或轨道无有效原型而受阻。请恢复缺失依赖后重新加载，并保留原存档副本。",
            "橋梁清理因相連道路或軌道無有效原型而受阻。請恢復缺失依賴後重新載入，並保留原存檔副本。",
            "Brückenbereinigung durch eine ungültige angeschlossene Straße oder ein Gleis blockiert. Fehlende Abhängigkeiten wiederherstellen und neu laden; den Originalspielstand als Kopie behalten.",
            "La limpieza se bloqueó por una carretera o vía conectada no válida. Restaura las dependencias y vuelve a cargar; conserva una copia de la partida original.",
            "Le nettoyage est bloqué par une route ou voie raccordée non valide. Restaurez les dépendances et rechargez ; conservez une copie de la sauvegarde originale.",
            "Pulizia bloccata da una strada o un binario collegato non valido. Ripristina le dipendenze e ricarica; conserva una copia del salvataggio originale.",
            "接続道路または線路の原型が無効なため、橋の削除を中止しました。不足する依存アセットを復元して再ロードしてください。元のセーブのコピーを保管してください。",
            "연결된 도로나 선로의 원형이 유효하지 않아 교량 정리를 중단했습니다. 누락된 종속 에셋을 복구한 후 다시 불러오고 원본 저장의 사본을 보관하세요.",
            "Czyszczenie zablokowane przez niepoprawną połączoną drogę lub tor. Przywróć zależności i wczytaj ponownie; zachowaj kopię oryginalnego zapisu.",
            "A limpeza foi bloqueada por uma via ou trilho conectado inválido. Restaure as dependências e recarregue; mantenha uma cópia do salvamento original.",
            "Очистка заблокирована некорректной подключённой дорогой или путём. Восстановите зависимости и загрузите город заново; сохраните копию исходного файла."
        },
        ["MissingBridgesTimeout"] = new[]
        {
            "Bridge cleanup is taking longer than expected. {0} entities and {1} network references remain. Cleanup is not complete; do not overwrite the original save. See the mod log.",
            "桥梁清理耗时超过预期，仍有 {0} 个实体和 {1} 处路网引用待处理。清理尚未完成，请勿覆盖原存档。详情请查看模组日志。",
            "橋梁清理耗時超過預期，仍有 {0} 個實體與 {1} 處路網引用待處理。清理尚未完成，請勿覆寫原存檔。詳情請查看模組日誌。",
            "Die Brückenbereinigung dauert länger als erwartet. {0} Entitäten und {1} Netzverweise verbleiben. Noch nicht abgeschlossen; Originalspielstand nicht überschreiben. Siehe Mod-Protokoll.",
            "La limpieza tarda más de lo previsto. Quedan {0} entidades y {1} referencias de red. No ha finalizado; no sobrescribas la partida original. Consulta el registro del mod.",
            "Le nettoyage prend plus de temps que prévu. Il reste {0} entités et {1} références réseau. Il n’est pas terminé ; n’écrasez pas la sauvegarde originale. Consultez le journal du mod.",
            "La pulizia richiede più tempo del previsto. Restano {0} entità e {1} riferimenti di rete. Non è completa: non sovrascrivere il salvataggio originale. Consulta il registro della mod.",
            "橋の削除処理に時間がかかっています。残りはエンティティ {0} 個、ネットワーク参照 {1} 件です。処理は未完了です。元のセーブを上書きせず、MOD のログを確認してください。",
            "교량 정리가 예상보다 오래 걸립니다. 엔티티 {0}개와 네트워크 참조 {1}개가 남아 있습니다. 정리가 완료되지 않았으니 원본 저장 파일을 덮어쓰지 마세요. 모드 로그를 확인하세요.",
            "Czyszczenie trwa dłużej niż oczekiwano. Pozostało {0} encji i {1} odwołań do sieci. Czyszczenie nie jest zakończone; nie nadpisuj oryginalnego zapisu. Sprawdź dziennik moda.",
            "A limpeza está demorando mais que o esperado. Restam {0} entidades e {1} referências de rede. A limpeza não terminou; não sobrescreva o salvamento original. Consulte o log do mod.",
            "Очистка мостов занимает больше времени, чем ожидалось. Осталось сущностей: {0}, сетевых ссылок: {1}. Очистка не завершена; не перезаписывайте исходное сохранение. См. журнал мода."
        },
        ["MissingBridgesSuspended"] = new[]
        {
            "Automatic bridge cleanup is suspended because network initialization or startup recovery did not finish safely. Bridge files are retained. Check the mod log and restart the game after resolving the issue. Do not overwrite the original save.",
            "路网初始化或启动修复未安全完成，已暂停自动清理桥梁并保留桥梁文件。请查看模组日志，解决问题后重启游戏。请勿覆盖原存档。",
            "路網初始化或啟動修復未安全完成，已暫停自動清理橋梁並保留橋梁檔案。請查看模組日誌，解決問題後重新啟動遊戲。請勿覆寫原存檔。",
            "Automatische Brückenbereinigung angehalten: Netzinitialisierung oder Startreparatur nicht sicher abgeschlossen. Brückendateien bleiben erhalten. Mod-Protokoll prüfen, Problem beheben und Spiel neu starten. Originalspielstand nicht überschreiben.",
            "La limpieza automática se ha suspendido: la inicialización de redes o la recuperación inicial no terminó de forma segura. Se conservan los archivos. Revisa el registro, resuelve el problema y reinicia el juego. No sobrescribas la partida original.",
            "Nettoyage automatique suspendu : l’initialisation des réseaux ou la réparation au démarrage n’a pas abouti en toute sécurité. Les fichiers sont conservés. Consultez le journal, résolvez le problème et redémarrez le jeu. N’écrasez pas la sauvegarde originale.",
            "Pulizia automatica sospesa: l’inizializzazione delle reti o il ripristino iniziale non è terminato in sicurezza. I file sono conservati. Controlla il registro, risolvi il problema e riavvia il gioco. Non sovrascrivere il salvataggio originale.",
            "ネットワークの初期化または起動時の修復が安全に完了しなかったため、自動削除を停止しました。橋のファイルは保持されています。ログを確認して問題を解決し、ゲームを再起動してください。元のセーブを上書きしないでください。",
            "네트워크 초기화 또는 시작 시 복구가 안전하게 완료되지 않아 자동 정리를 중단했습니다. 교량 파일은 보존됩니다. 로그를 확인하고 문제를 해결한 후 게임을 다시 시작하세요. 원본 저장 파일을 덮어쓰지 마세요.",
            "Automatyczne czyszczenie wstrzymane: inicjalizacja sieci lub naprawa startowa nie zakończyła się bezpiecznie. Pliki mostów zachowano. Sprawdź dziennik, usuń problem i uruchom grę ponownie. Nie nadpisuj oryginalnego zapisu.",
            "Limpeza automática suspensa: a inicialização das redes ou a recuperação inicial não terminou com segurança. Os arquivos foram mantidos. Consulte o log, resolva o problema e reinicie o jogo. Não sobrescreva o salvamento original.",
            "Автоматическая очистка приостановлена: инициализация сетей или восстановление при запуске не завершились безопасно. Файлы мостов сохранены. Проверьте журнал, устраните проблему и перезапустите игру. Не перезаписывайте исходное сохранение."
        },
        ["CustomRoad"] = new[]
        {
            "+ Custom road", "+ 自定义道路", "+ 自訂道路", "+ Eigene Straße",
            "+ Vía personalizada", "+ Route personnalisée", "+ Strada personalizzata",
            "+ カスタム道路", "+ 사용자 지정 도로", "+ Własna droga", "+ Via personalizada", "+ Своя дорога"
        },
        ["RoadBuilderRequired"] = new[]
        {
            "Creating custom roads requires the optional Road Builder mod to be installed and enabled. Existing roads and tracks can be used without it.",
            "创建自定义道路需要安装并启用可选模组 Road Builder。使用已有道路和轨道不需要此模组。",
            "建立自訂道路需要安裝並啟用選用模組 Road Builder。使用現有道路與軌道不需要此模組。",
            "Eigene Straßen benötigen das optionale Mod Road Builder (installiert und aktiviert). Vorhandene Straßen und Gleise funktionieren ohne dieses Mod.",
            "Para crear vías personalizadas, instala y activa el mod opcional Road Builder. No es necesario para usar vías existentes.",
            "La création de routes personnalisées nécessite le mod facultatif Road Builder, installé et activé. Les routes et voies existantes restent utilisables sans lui.",
            "Per creare strade personalizzate, installa e attiva la mod opzionale Road Builder. Non serve per usare strade e binari esistenti.",
            "カスタム道路の作成には任意の MOD Road Builder のインストールと有効化が必要です。既存の道路や線路には不要です。",
            "사용자 지정 도로를 만들려면 선택 모드인 Road Builder를 설치하고 활성화하세요. 기존 도로와 선로에는 필요하지 않습니다.",
            "Tworzenie własnych dróg wymaga zainstalowanego i włączonego opcjonalnego moda Road Builder. Istniejące drogi i tory działają bez niego.",
            "Para criar vias personalizadas, instale e ative o mod opcional Road Builder. Vias e trilhos existentes podem ser usados sem ele.",
            "Для создания своих дорог установите и включите необязательный мод Road Builder. Существующие дороги и пути доступны без него."
        },
        ["RoadBuilderOpenFailed"] = new[]
        {
            "Road Builder is loaded, but its interface could not be opened. Try its own toolbar button. Your bridge selection has been kept.",
            "Road Builder 已加载，但无法打开其界面。请尝试使用该模组自己的工具栏按钮。当前桥梁选择已保留。",
            "Road Builder 已載入，但無法開啟其介面。請嘗試使用該模組自己的工具列按鈕。目前橋梁選擇已保留。",
            "Road Builder ist geladen, aber die Oberfläche konnte nicht geöffnet werden. Nutze dessen Symbolleiste. Die Brückenauswahl bleibt erhalten.",
            "Road Builder está cargado, pero no se pudo abrir. Prueba su botón de herramientas. La selección del puente se conserva.",
            "Road Builder est chargé, mais son interface ne s’ouvre pas. Essayez son bouton dans la barre d’outils. Votre sélection est conservée.",
            "Road Builder è caricato, ma non si apre. Prova il suo pulsante nella barra strumenti. La selezione del ponte è conservata.",
            "Road Builder は読み込み済みですが、画面を開けませんでした。専用のツールバーボタンをお試しください。橋の選択は保持されています。",
            "Road Builder가 로드되었지만 화면을 열 수 없습니다. 해당 모드의 도구 모음 버튼을 사용하세요. 교량 선택은 유지됩니다.",
            "Road Builder jest załadowany, ale nie można go otworzyć. Użyj jego przycisku na pasku narzędzi. Wybór mostu został zachowany.",
            "Road Builder está carregado, mas a interface não abriu. Tente o botão na barra de ferramentas do mod. A seleção da ponte foi mantida.",
            "Road Builder загружен, но интерфейс не открылся. Попробуйте его кнопку на панели инструментов. Выбор моста сохранён."
        },
        ["MissingBridgesRemoved"] = new[]
        {
            "Removed placed networks for {0} missing Bridge Builder bridge assets from this city. Other assets were not removed. The original save file is unchanged; save as a new file to keep this repair.",
            "已从当前城市移除 {0} 种缺失的 Bridge Builder 桥梁资产对应的已建路网，未移除其他资产。原存档文件未被覆盖；请另存为新存档以保留修复。",
            "已從目前城市移除 {0} 種缺失的 Bridge Builder 橋梁資產對應的已建路網，未移除其他資產。原存檔未被覆寫；請另存新檔以保留修復。",
            "Netze für {0} fehlende Bridge-Builder-Brücken entfernt. Andere Assets bleiben erhalten. Der ursprüngliche Spielstand bleibt unverändert; bitte unter neuem Namen speichern.",
            "Se eliminaron las redes de {0} puentes de Bridge Builder ausentes. Los demás recursos y el archivo original no se modificaron. Guarda en un archivo nuevo para conservar la reparación.",
            "Les réseaux de {0} ponts Bridge Builder manquants ont été supprimés. Les autres ressources et la sauvegarde originale sont conservées. Enregistrez dans un nouveau fichier pour conserver la réparation.",
            "Rimosse le reti di {0} ponti Bridge Builder mancanti. Le altre risorse e il salvataggio originale sono invariati. Salva in un nuovo file per conservare la riparazione.",
            "見つからない Bridge Builder 橋梁アセット {0} 種類の設置済みネットワークを削除しました。他のアセットと元のセーブファイルは変更していません。修復を保存するには別名で保存してください。",
            "누락된 Bridge Builder 교량 에셋 {0}종의 배치된 네트워크를 제거했습니다. 다른 에셋과 원본 저장 파일은 변경하지 않았습니다. 복구 결과를 새 파일로 저장하세요.",
            "Usunięto sieci {0} brakujących mostów Bridge Builder. Inne zasoby i oryginalny zapis pozostają bez zmian. Zapisz w nowym pliku, aby zachować naprawę.",
            "Removidas as redes de {0} pontes Bridge Builder ausentes. Outros recursos e o arquivo original permanecem intactos. Salve em um novo arquivo para manter o reparo.",
            "Удалены сети {0} отсутствующих мостов Bridge Builder. Другие ресурсы и исходное сохранение не изменены. Сохраните город в новом файле, чтобы оставить исправление."
        },
        ["MissingBridgesRepairFailed"] = new[]
        {
            "Missing bridge cleanup could not be completed. Check the mod log. Do not overwrite the original save.",
            "缺失桥梁清理未能完成，请查看模组日志。请勿覆盖原存档。",
            "缺失橋梁清理未能完成，請查看模組日誌。請勿覆寫原存檔。",
            "Bereinigung fehlgeschlagen. Mod-Protokoll prüfen. Originalspielstand nicht überschreiben.",
            "No se pudo completar la limpieza. Revisa el registro del mod. No sobrescribas la partida original.",
            "Nettoyage incomplet. Consultez le journal du mod. N’écrasez pas la sauvegarde originale.",
            "Pulizia incompleta. Controlla il registro della mod. Non sovrascrivere il salvataggio originale.",
            "削除処理が完了しませんでした。Mod のログを確認し、元のセーブを上書きしないでください。",
            "정리를 완료하지 못했습니다. 모드 로그를 확인하고 원본 저장 파일을 덮어쓰지 마세요.",
            "Nie ukończono czyszczenia. Sprawdź dziennik moda. Nie nadpisuj oryginalnego zapisu.",
            "Limpeza incompleta. Consulte o log do mod. Não sobrescreva o salvamento original.",
            "Очистка не завершена. Проверьте журнал мода. Не перезаписывайте исходное сохранение."
        },
        ["ActivateLocked"] = new[]
        {
            "Unlock the original bridge before building this bridge.",
            "需要先解锁原型桥梁，才能建造此桥梁。",
            "需要先解鎖原型橋梁，才能建造此橋梁。",
            "Schalte zuerst die ursprüngliche Brücke frei.",
            "Desbloquea primero el puente original.",
            "Débloquez d’abord le pont d’origine.",
            "Sblocca prima il ponte originale.",
            "建設するには元の橋をアンロックしてください。",
            "건설하려면 원형 교량을 먼저 잠금 해제하세요.",
            "Najpierw odblokuj oryginalny most.",
            "Desbloqueie primeiro a ponte original.",
            "Сначала разблокируйте исходный мост."
        },
        ["CreatedLocked"] = new[]
        {
            "Created “{0}”. Unlock the original bridge before building it.",
            "已创建“{0}”。需要先解锁原型桥梁才能建造。",
            "已建立「{0}」。需要先解鎖原型橋梁才能建造。",
            "„{0}“ erstellt. Schalte vor dem Bau die ursprüngliche Brücke frei.",
            "«{0}» creado. Desbloquea el puente original para construirlo.",
            "« {0} » créé. Débloquez le pont d’origine pour le construire.",
            "Creato “{0}”. Sblocca il ponte originale per costruirlo.",
            "「{0}」を作成しました。建設には元の橋のアンロックが必要です。",
            "‘{0}’ 생성 완료. 건설하려면 원형 교량을 잠금 해제하세요.",
            "Utworzono „{0}”. Odblokuj oryginalny most, aby go zbudować.",
            "“{0}” criada. Desbloqueie a ponte original para construí-la.",
            "Мост «{0}» создан. Для строительства разблокируйте исходный мост."
        },
        ["SameDirection"] = new[]
        {
            "Same direction", "同向", "同向", "Gleichgerichtet", "Mismo sentido", "Même sens",
            "Stesso senso", "同方向", "같은 방향", "Ten sam kierunek", "Mesmo sentido", "Одно направление"
        },
        ["OppositeDirection"] = new[]
        {
            "Opposite directions", "反向", "反向", "Gegenläufig", "Sentidos opuestos", "Sens opposés",
            "Sensi opposti", "逆方向", "반대 방향", "Przeciwne kierunki", "Sentidos opostos", "Противоположные направления"
        },
        ["LowerDeckOpposite"] = new[]
        {
            "Opposite directions on upper and lower decks", "上下层方向相反", "上下層方向相反",
            "Obere und untere Ebene in Gegenrichtung", "Sentidos opuestos en los dos niveles",
            "Sens opposés sur les deux niveaux", "Direzioni opposte sui due livelli", "上下層を逆方向にする",
            "상하층을 반대 방향으로", "Przeciwne kierunki na obu poziomach",
            "Sentidos opostos nos dois níveis", "Противоположные направления на ярусах"
        },
        ["SearchPlaceholder"] = new[]
        {
            "Search...", "搜索……", "搜尋……", "Suchen...", "Buscar...", "Rechercher...",
            "Cerca...", "検索...", "검색...", "Szukaj...", "Pesquisar...", "Поиск..."
        },
        ["SearchNetworks"] = new[]
        {
            "Search roads or tracks…", "搜索道路或轨道…", "搜尋道路或軌道…", "Straßen oder Gleise suchen…", "Buscar carreteras o vías…", "Rechercher routes ou voies…", "Cerca strade o binari…", "道路・軌道を検索…", "도로 또는 선로 검색…", "Szukaj dróg lub torów…", "Buscar vias ou trilhos…", "Поиск дорог или путей…"
        },
        ["SearchStyles"] = new[]
        {
            "Search bridge styles…", "搜索桥梁类型…", "搜尋橋梁類型…", "Brückentypen suchen…", "Buscar tipos de puente…", "Rechercher des types de pont…", "Cerca tipi di ponte…", "橋の種類を検索…", "교량 유형 검색…", "Szukaj typów mostów…", "Buscar tipos de ponte…", "Поиск типов мостов…"
        },
        ["SearchBridges"] = new[]
        {
            "Search bridges by name…",
            "按名称搜索桥梁……",
            "依名稱搜尋橋梁……",
            "Brücken nach Namen suchen…",
            "Buscar puentes por nombre…",
            "Rechercher des ponts par nom…",
            "Cerca ponti per nome…",
            "名前で橋を検索…",
            "이름으로 교량 검색…",
            "Szukaj mostów według nazwy…",
            "Buscar pontes por nome…",
            "Поиск мостов по названию…"
        },
        ["AllTypes"] = new[]
        {
            "All network types", "全部道路类型", "全部道路類型", "Alle Netztypen", "Todos los tipos de vía", "Tous les types de réseau", "Tutti i tipi di rete", "すべての道路・軌道", "모든 네트워크 유형", "Wszystkie typy sieci", "Todos os tipos de via", "Все типы сетей"
        },
        ["TypeRoad"] = new[]
        {
            "Roads", "普通道路", "一般道路", "Straßen", "Carreteras", "Routes", "Strade", "一般道路", "일반 도로", "Drogi", "Ruas", "Дороги"
        },
        ["TypeHighway"] = new[]
        {
            "Highways", "高速公路", "高速公路", "Autobahnen", "Autopistas", "Autoroutes", "Autostrade", "高速道路", "고속도로", "Autostrady", "Rodovias", "Автомагистрали"
        },
        ["TypePublicTransport"] = new[]
        {
            "Public transport roads", "公共交通道路", "公共運輸道路", "ÖPNV-Straßen", "Vías de transporte público", "Routes de transport public", "Strade per trasporto pubblico", "公共交通専用道路", "대중교통 도로", "Drogi transportu publicznego", "Vias de transporte público", "Дороги общественного транспорта"
        },
        ["TypePedestrian"] = new[]
        {
            "Pedestrian paths", "人行道", "人行道", "Fußwege", "Caminos peatonales", "Chemins piétons", "Percorsi pedonali", "歩道", "보행자 도로", "Ścieżki piesze", "Caminhos de pedestres", "Пешеходные дорожки"
        },
        ["TypeTrain"] = new[]
        {
            "Railways", "铁路", "鐵路", "Eisenbahnen", "Ferrocarriles", "Voies ferrées", "Ferrovie", "鉄道", "철도", "Kolej", "Ferrovias", "Железные дороги"
        },
        ["TypeSubway"] = new[]
        {
            "Subway tracks", "地铁", "捷運", "U-Bahn-Gleise", "Vías de metro", "Voies de métro", "Binari metro", "地下鉄", "지하철", "Tory metra", "Trilhos de metrô", "Пути метро"
        },
        ["TypeTram"] = new[]
        {
            "Tram tracks", "有轨电车", "有軌電車", "Straßenbahngleise", "Vías de tranvía", "Voies de tramway", "Binari del tram", "路面電車", "트램", "Tory tramwajowe", "Trilhos de bonde", "Трамвайные пути"
        },
        ["NoMatches"] = new[]
        {
            "No matching results", "没有匹配的结果", "沒有符合的結果", "Keine passenden Ergebnisse", "No hay resultados", "Aucun résultat", "Nessun risultato", "一致する結果がありません", "일치하는 결과 없음", "Brak wyników", "Nenhum resultado", "Нет подходящих результатов"
        },
        ["AllModes"] = new[]
        {
            "Single and double deck", "全部层数", "全部層數", "Alle Ebenen", "Todos los niveles", "Tous les niveaux", "Tutti i livelli", "すべての階層", "모든 층수", "Wszystkie poziomy", "Todos os níveis", "Все уровни"
        },
        ["AllStatuses"] = new[]
        {
            "All availability", "全部可用状态", "全部可用狀態", "Alle Verfügbarkeiten", "Cualquier disponibilidad", "Toutes les disponibilités", "Qualsiasi disponibilità", "すべての利用状態", "모든 사용 가능 상태", "Dowolna dostępność", "Qualquer disponibilidade", "Любая доступность"
        },
        ["Available"] = new[]
        {
            "Available", "可用", "可用", "Verfügbar", "Disponible", "Disponible", "Disponibile", "利用可能", "사용 가능", "Dostępne", "Disponível", "Доступно"
        },
        ["Unavailable"] = new[]
        {
            "Unavailable", "不可用", "不可用", "Nicht verfügbar", "No disponible", "Indisponible", "Non disponibile", "利用不可", "사용 불가", "Niedostępne", "Indisponível", "Недоступно"
        },
        ["AllStyles"] = new[]
        {
            "All bridge styles", "全部桥梁类型", "全部橋梁類型", "Alle Brückentypen", "Todos los tipos de puente", "Tous les types de pont", "Tutti i tipi di ponte", "すべての橋の種類", "모든 교량 유형", "Wszystkie typy mostów", "Todos os tipos de ponte", "Все типы мостов"
        },
        ["CreateTab"] = new[]
        {
            "Create bridge",
            "创建桥梁",
            "建立橋梁",
            "Brücke erstellen",
            "Crear puente",
            "Créer un pont",
            "Crea ponte",
            "橋を作成",
            "교량 생성",
            "Utwórz most",
            "Criar ponte",
            "Создать мост"
        },
        ["ManageTab"] = new[]
        {
            "Manage bridges",
            "管理桥梁",
            "管理橋梁",
            "Brücken verwalten",
            "Gestionar puentes",
            "Gérer les ponts",
            "Gestisci ponti",
            "橋を管理",
            "교량 관리",
            "Zarządzaj mostami",
            "Gerenciar pontes",
            "Управление мостами"
        },
        ["Refresh"] = new[]
        {
            "Refresh",
            "刷新",
            "重新整理",
            "Aktualisieren",
            "Actualizar",
            "Actualiser",
            "Aggiorna",
            "更新",
            "새로 고침",
            "Odśwież",
            "Atualizar",
            "Обновить"
        },
        ["Close"] = new[]
        {
            "Close",
            "关闭",
            "關閉",
            "Schließen",
            "Cerrar",
            "Fermer",
            "Chiudi",
            "閉じる",
            "닫기",
            "Zamknij",
            "Fechar",
            "Закрыть"
        },
        ["NetworkPicker"] = new[]
        {
            "Road or track selection",
            "道路或轨道选择",
            "道路或軌道選擇",
            "Straße oder Gleis wählen",
            "Seleccionar carretera o vía",
            "Choisir une route ou voie ferrée",
            "Seleziona strada o binario",
            "道路・軌道を選択",
            "도로 또는 선로 선택",
            "Wybór drogi lub toru",
            "Selecionar via ou trilho",
            "Выбор дороги или путей"
        },
        ["UpperPicker"] = new[]
        {
            "Upper road or track",
            "上层道路或轨道",
            "上層道路或軌道",
            "Obere Straße oder Gleis",
            "Carretera o vía superior",
            "Route ou voie supérieure",
            "Strada o binario superiore",
            "上層の道路・軌道",
            "상층 도로 또는 선로",
            "Górna droga lub tor",
            "Via ou trilho superior",
            "Верхняя дорога или пути"
        },
        ["LowerPicker"] = new[]
        {
            "Lower road or track",
            "下层道路或轨道",
            "下層道路或軌道",
            "Untere Straße oder Gleis",
            "Carretera o vía inferior",
            "Route ou voie inférieure",
            "Strada o binario inferiore",
            "下層の道路・軌道",
            "하층 도로 또는 선로",
            "Dolna droga lub tor",
            "Via ou trilho inferior",
            "Нижняя дорога или пути"
        },
        ["NoNetworks"] = new[]
        {
            "No roads or tracks available",
            "未发现可用道路或轨道",
            "未發現可用道路或軌道",
            "Keine Straßen oder Gleise verfügbar",
            "No hay carreteras ni vías disponibles",
            "Aucune route ni voie disponible",
            "Nessuna strada o binario disponibile",
            "利用可能な道路・軌道がありません",
            "사용 가능한 도로나 선로가 없습니다",
            "Brak dostępnych dróg i torów",
            "Nenhuma via ou trilho disponível",
            "Нет доступных дорог или путей"
        },
        ["NoLowerNetworks"] = new[]
        {
            "No lower networks available",
            "未发现可用下层网络",
            "未發現可用下層路網",
            "Keine unteren Netze verfügbar",
            "No hay redes inferiores disponibles",
            "Aucun réseau inférieur disponible",
            "Nessuna rete inferiore disponibile",
            "利用可能な下層ネットワークがありません",
            "사용 가능한 하층 네트워크가 없습니다",
            "Brak dostępnych dolnych sieci",
            "Nenhuma rede inferior disponível",
            "Нет доступных нижних сетей"
        },
        ["SingleDeck"] = new[]
        {
            "Single deck",
            "单层",
            "單層",
            "Eine Ebene",
            "Un nivel",
            "Un niveau",
            "Un livello",
            "単層",
            "단층",
            "Jeden poziom",
            "Um nível",
            "Один ярус"
        },
        ["DoubleDeck"] = new[]
        {
            "Double deck",
            "双层",
            "雙層",
            "Zwei Ebenen",
            "Dos niveles",
            "Deux niveaux",
            "Due livelli",
            "二層",
            "복층",
            "Dwa poziomy",
            "Dois níveis",
            "Два яруса"
        },
        ["NoStyles"] = new[]
        {
            "No bridge prototypes available",
            "没有可用的桥梁原型",
            "沒有可用的橋梁原型",
            "Keine Brückenvorlagen verfügbar",
            "No hay prototipos de puente disponibles",
            "Aucun modèle de pont disponible",
            "Nessun prototipo di ponte disponibile",
            "利用可能な橋の原型がありません",
            "사용 가능한 교량 원형이 없습니다",
            "Brak dostępnych pierwowzorów mostów",
            "Nenhum protótipo de ponte disponível",
            "Нет доступных прототипов мостов"
        },
        ["DisplayName"] = new[]
        {
            "Bridge name",
            "桥梁名称",
            "橋梁名稱",
            "Brückenname",
            "Nombre del puente",
            "Nom du pont",
            "Nome del ponte",
            "橋の名前",
            "교량 이름",
            "Nazwa mostu",
            "Nome da ponte",
            "Название моста"
        },
        ["Create"] = new[]
        {
            "Create",
            "创建",
            "建立",
            "Erstellen",
            "Crear",
            "Créer",
            "Crea",
            "作成",
            "생성",
            "Utwórz",
            "Criar",
            "Создать"
        },
        ["CreateBuild"] = new[]
        {
            "Create and build",
            "创建并建造",
            "建立並建造",
            "Erstellen und bauen",
            "Crear y construir",
            "Créer et construire",
            "Crea e costruisci",
            "作成して建設",
            "생성 및 건설",
            "Utwórz i buduj",
            "Criar e construir",
            "Создать и построить"
        },
        ["SingleBridge"] = new[]
        {
            "Single-deck bridge",
            "单层桥梁",
            "單層橋梁",
            "Einstöckige Brücke",
            "Puente de un nivel",
            "Pont à un niveau",
            "Ponte a un livello",
            "単層橋",
            "단층 교량",
            "Most jednopoziomowy",
            "Ponte de um nível",
            "Одноярусный мост"
        },
        ["DoubleBridge"] = new[]
        {
            "Double-deck bridge",
            "双层桥梁",
            "雙層橋梁",
            "Doppelstockbrücke",
            "Puente de dos niveles",
            "Pont à deux niveaux",
            "Ponte a due livelli",
            "二層橋",
            "복층 교량",
            "Most dwupoziomowy",
            "Ponte de dois níveis",
            "Двухъярусный мост"
        },
        ["NoBridges"] = new[]
        {
            "No bridges created yet",
            "尚未创建桥梁",
            "尚未建立橋梁",
            "Noch keine Brücken erstellt",
            "Aún no se han creado puentes",
            "Aucun pont créé",
            "Nessun ponte ancora creato",
            "作成済みの橋はありません",
            "생성된 교량이 없습니다",
            "Nie utworzono jeszcze mostów",
            "Nenhuma ponte criada",
            "Мосты ещё не созданы"
        },
        ["PrefabId"] = new[]
        {
            "Prefab unique identifier",
            "Prefab 唯一标识符",
            "Prefab 唯一識別碼",
            "Eindeutige Prefab-Kennung",
            "Identificador único del prefab",
            "Identifiant unique du prefab",
            "Identificatore univoco del prefab",
            "Prefab の一意識別子",
            "프리팹 고유 식별자",
            "Unikalny identyfikator prefabu",
            "Identificador único do prefab",
            "Уникальный идентификатор prefab"
        },
        ["RenameNote"] = new[]
        {
            "Renaming changes only the display name; the UUID and bridge structure stay unchanged.",
            "更名只修改注册名称；UUID 和桥梁构造保持不变。",
            "更名只修改註冊名稱；UUID 和橋梁構造保持不變。",
            "Umbenennen ändert nur den Anzeigenamen, nicht UUID oder Brückenkonstruktion.",
            "Cambiar el nombre no modifica el UUID ni la estructura del puente.",
            "Le renommage ne modifie que le nom affiché, pas l’UUID ni la structure du pont.",
            "La rinomina cambia solo il nome visualizzato, non l’UUID o la struttura del ponte.",
            "名前の変更は表示名のみです。UUID と橋の構造は変わりません。",
            "이름을 바꿔도 UUID와 교량 구조는 변경되지 않습니다.",
            "Zmiana nazwy nie zmienia UUID ani konstrukcji mostu.",
            "Renomear altera apenas o nome exibido, não o UUID nem a estrutura da ponte.",
            "Переименование не изменяет UUID и конструкцию моста."
        },
        ["Build"] = new[]
        {
            "Build",
            "建造",
            "建造",
            "Bauen",
            "Construir",
            "Construire",
            "Costruisci",
            "建設",
            "건설",
            "Buduj",
            "Construir",
            "Построить"
        },
        ["Rename"] = new[]
        {
            "Rename",
            "改名",
            "更名",
            "Umbenennen",
            "Renombrar",
            "Renommer",
            "Rinomina",
            "名前を変更",
            "이름 변경",
            "Zmień nazwę",
            "Renomear",
            "Переименовать"
        },
        ["Delete"] = new[]
        {
            "Delete",
            "删除",
            "刪除",
            "Löschen",
            "Eliminar",
            "Supprimer",
            "Elimina",
            "削除",
            "삭제",
            "Usuń",
            "Excluir",
            "Удалить"
        },
        ["Cancel"] = new[]
        {
            "Cancel",
            "取消",
            "取消",
            "Abbrechen",
            "Cancelar",
            "Annuler",
            "Annulla",
            "キャンセル",
            "취소",
            "Anuluj",
            "Cancelar",
            "Отмена"
        },
        ["SelectBridge"] = new[]
        {
            "Select a created bridge",
            "选择一座已创建桥梁",
            "選擇一座已建立橋梁",
            "Eine erstellte Brücke wählen",
            "Selecciona un puente creado",
            "Sélectionnez un pont créé",
            "Seleziona un ponte creato",
            "作成済みの橋を選択",
            "생성된 교량을 선택하세요",
            "Wybierz utworzony most",
            "Selecione uma ponte criada",
            "Выберите созданный мост"
        },
        ["Bridge"] = new[]
        {
            "Bridge",
            "桥梁",
            "橋梁",
            "Brücke",
            "Puente",
            "Pont",
            "Ponte",
            "橋",
            "교량",
            "Most",
            "Ponte",
            "Мост"
        },
        ["Network"] = new[]
        {
            "Network",
            "路网",
            "路網",
            "Netz",
            "Red",
            "Réseau",
            "Rete",
            "ネットワーク",
            "네트워크",
            "Sieć",
            "Rede",
            "Сеть"
        },
        ["PreviewAlt"] = new[]
        {
            "Isometric 3D model of the generated bridge",
            "生成桥梁的正等轴测三维模型",
            "生成橋梁的正等軸測三維模型",
            "Isometrisches 3D-Modell der erstellten Brücke",
            "Modelo 3D isométrico del puente generado",
            "Modèle 3D isométrique du pont généré",
            "Modello 3D isometrico del ponte generato",
            "生成した橋の等角投影 3D モデル",
            "생성된 교량의 등각 투영 3D 모델",
            "Izometryczny model 3D utworzonego mostu",
            "Modelo 3D isométrico da ponte gerada",
            "Изометрическая 3D-модель созданного моста"
        },
        ["PreviewSelect"] = new[]
        {
            "Select a road or track and a bridge style for a 3D preview",
            "选择道路或轨道及桥型以生成 3D 预览",
            "選擇道路或軌道及橋型以生成 3D 預覽",
            "Für die 3D-Vorschau ein Netz und einen Brückentyp wählen",
            "Selecciona una red y un tipo de puente para la vista 3D",
            "Choisissez un réseau et un type de pont pour l’aperçu 3D",
            "Seleziona una rete e un tipo di ponte per l’anteprima 3D",
            "道路・軌道と橋の種類を選択すると 3D プレビューを表示します",
            "도로 또는 선로와 교량 유형을 선택하면 3D 미리보기가 표시됩니다",
            "Wybierz sieć i typ mostu, aby zobaczyć podgląd 3D",
            "Selecione uma rede e um tipo de ponte para a prévia 3D",
            "Выберите сеть и тип моста для 3D-просмотра"
        },
        ["PreviewImageFailed"] = new[]
        {
            "Preview image failed to load. Select the bridge style again.",
            "预览图像加载失败，请重新选择桥型。",
            "預覽圖像載入失敗，請重新選擇橋型。",
            "Vorschaubild konnte nicht geladen werden. Brückentyp erneut wählen.",
            "No se pudo cargar la vista previa. Vuelve a seleccionar el puente.",
            "Échec du chargement de l’image. Resélectionnez le type de pont.",
            "Impossibile caricare l’anteprima. Seleziona di nuovo il tipo di ponte.",
            "プレビュー画像の読み込みに失敗しました。橋の種類を選び直してください。",
            "미리보기 이미지 로드 실패. 교량 유형을 다시 선택하세요.",
            "Nie udało się wczytać podglądu. Wybierz typ mostu ponownie.",
            "Falha ao carregar a prévia. Selecione o tipo de ponte novamente.",
            "Не удалось загрузить изображение. Выберите тип моста заново."
        },
        ["PreviewDisplaying"] = new[]
        {
            "Displaying preview…",
            "正在显示预览图像…",
            "正在顯示預覽圖像…",
            "Vorschau wird angezeigt…",
            "Mostrando vista previa…",
            "Affichage de l’aperçu…",
            "Visualizzazione anteprima…",
            "プレビューを表示中…",
            "미리보기 표시 중…",
            "Wyświetlanie podglądu…",
            "Exibindo prévia…",
            "Отображение предпросмотра…"
        },
        ["PreviewPreparing"] = new[]
        {
            "Preparing temporary bridge model…",
            "正在准备临时桥梁模型…",
            "正在準備臨時橋梁模型…",
            "Temporäres Brückenmodell wird vorbereitet…",
            "Preparando modelo temporal del puente…",
            "Préparation du modèle temporaire du pont…",
            "Preparazione del modello temporaneo…",
            "一時的な橋モデルを準備中…",
            "임시 교량 모델 준비 중…",
            "Przygotowywanie tymczasowego modelu mostu…",
            "Preparando modelo temporário da ponte…",
            "Подготовка временной модели моста…"
        },
        ["PreviewBuilding"] = new[]
        {
            "Generating temporary bridge model…",
            "正在生成临时桥梁模型…",
            "正在生成臨時橋梁模型…",
            "Temporäres Brückenmodell wird erstellt…",
            "Generando modelo temporal del puente…",
            "Création du modèle temporaire du pont…",
            "Generazione del modello temporaneo…",
            "一時的な橋モデルを生成中…",
            "임시 교량 모델 생성 중…",
            "Tworzenie tymczasowego modelu mostu…",
            "Gerando modelo temporário da ponte…",
            "Создание временной модели моста…"
        },
        ["PreviewReady"] = new[]
        {
            "Isometric 3D preview",
            "正等轴测 3D 预览",
            "正等軸測 3D 預覽",
            "Isometrische 3D-Vorschau",
            "Vista 3D isométrica",
            "Aperçu 3D isométrique",
            "Anteprima 3D isometrica",
            "等角投影 3D プレビュー",
            "등각 투영 3D 미리보기",
            "Izometryczny podgląd 3D",
            "Prévia 3D isométrica",
            "Изометрический 3D-просмотр"
        },
        ["BaseGame"] = new[]
        {
            "Base game",
            "基础版游戏",
            "基礎版遊戲",
            "Grundspiel",
            "Juego base",
            "Jeu de base",
            "Gioco base",
            "ベースゲーム",
            "기본 게임",
            "Gra podstawowa",
            "Jogo base",
            "Базовая игра"
        },
        ["SourceMod"] = new[]
        {
            "Mod: {0}",
            "模组：{0}",
            "模組：{0}",
            "Mod: {0}",
            "Mod: {0}",
            "Mod : {0}",
            "Mod: {0}",
            "Mod: {0}",
            "모드: {0}",
            "Mod: {0}",
            "Mod: {0}",
            "Мод: {0}"
        },
        ["SourceDlc"] = new[]
        {
            "DLC: {0}",
            "DLC：{0}",
            "DLC：{0}",
            "DLC: {0}",
            "DLC: {0}",
            "DLC : {0}",
            "DLC: {0}",
            "DLC: {0}",
            "DLC: {0}",
            "DLC: {0}",
            "DLC: {0}",
            "DLC: {0}"
        },
        ["SourcePack"] = new[]
        {
            "DLC / asset pack: {0}",
            "DLC／资产包：{0}",
            "DLC／資產包：{0}",
            "DLC / Asset-Paket: {0}",
            "DLC / paquete de recursos: {0}",
            "DLC / pack d’objets : {0}",
            "DLC / pacchetto di risorse: {0}",
            "DLC / アセットパック: {0}",
            "DLC / 에셋 팩: {0}",
            "DLC / pakiet zasobów: {0}",
            "DLC / pacote de recursos: {0}",
            "DLC / набор ресурсов: {0}"
        },
        ["SourceContent"] = new[]
        {
            "Content: {0}",
            "内容：{0}",
            "內容：{0}",
            "Inhalt: {0}",
            "Contenido: {0}",
            "Contenu : {0}",
            "Contenuto: {0}",
            "コンテンツ: {0}",
            "콘텐츠: {0}",
            "Zawartość: {0}",
            "Conteúdo: {0}",
            "Контент: {0}"
        },
        ["SourceUnreadable"] = new[]
        {
            "Content prerequisite unavailable",
            "无法读取前置内容",
            "無法讀取前置內容",
            "Inhaltsvoraussetzung nicht lesbar",
            "Requisito de contenido no disponible",
            "Dépendance de contenu inaccessible",
            "Requisito di contenuto non disponibile",
            "必要なコンテンツを読み取れません",
            "필수 콘텐츠를 읽을 수 없습니다",
            "Wymagana zawartość niedostępna",
            "Pré-requisito de conteúdo indisponível",
            "Не удалось прочитать зависимости"
        },
        ["Scanning"] = new[]
        {
            "Reading networks and bridge prototypes…",
            "正在读取路网与桥梁原型…",
            "正在讀取路網與橋梁原型…",
            "Netze und Brückenvorlagen werden gelesen…",
            "Leyendo redes y prototipos de puentes…",
            "Lecture des réseaux et modèles de pont…",
            "Lettura delle reti e dei prototipi di ponte…",
            "ネットワークと橋の原型を読み込み中…",
            "네트워크 및 교량 원형 읽는 중…",
            "Odczytywanie sieci i pierwowzorów mostów…",
            "Lendo redes e protótipos de pontes…",
            "Чтение сетей и прототипов мостов…"
        },
        ["Refreshing"] = new[]
        {
            "Refreshing networks and bridge prototypes…",
            "正在刷新路网与桥梁原型…",
            "正在重新整理路網與橋梁原型…",
            "Netze und Brückenvorlagen werden aktualisiert…",
            "Actualizando redes y prototipos de puentes…",
            "Actualisation des réseaux et modèles de pont…",
            "Aggiornamento delle reti e dei prototipi di ponte…",
            "ネットワークと橋の原型を更新中…",
            "네트워크 및 교량 원형 새로 고치는 중…",
            "Odświeżanie sieci i pierwowzorów mostów…",
            "Atualizando redes e protótipos de pontes…",
            "Обновление сетей и прототипов мостов…"
        },
        ["Creating"] = new[]
        {
            "Creating bridge prefab…",
            "正在创建桥梁 Prefab…",
            "正在建立橋梁 Prefab…",
            "Brücken-Prefab wird erstellt…",
            "Creando prefab del puente…",
            "Création du prefab du pont…",
            "Creazione del prefab del ponte…",
            "橋の Prefab を作成中…",
            "교량 프리팹 생성 중…",
            "Tworzenie prefabu mostu…",
            "Criando prefab da ponte…",
            "Создание prefab моста…"
        },
        ["Activating"] = new[]
        {
            "Activating bridge construction…",
            "正在激活桥梁建造工具…",
            "正在啟用橋梁建造工具…",
            "Brückenbau wird aktiviert…",
            "Activando construcción de puentes…",
            "Activation de la construction du pont…",
            "Attivazione della costruzione del ponte…",
            "橋の建設ツールを起動中…",
            "교량 건설 도구 활성화 중…",
            "Aktywowanie budowy mostu…",
            "Ativando construção da ponte…",
            "Включение инструмента строительства…"
        },
        ["Renaming"] = new[]
        {
            "Updating display name…",
            "正在更新显示名称…",
            "正在更新顯示名稱…",
            "Anzeigename wird aktualisiert…",
            "Actualizando nombre…",
            "Mise à jour du nom affiché…",
            "Aggiornamento del nome visualizzato…",
            "表示名を更新中…",
            "표시 이름 업데이트 중…",
            "Aktualizowanie nazwy…",
            "Atualizando nome exibido…",
            "Обновление названия…"
        },
        ["Deleting"] = new[]
        {
            "Deleting bridge…",
            "正在删除桥梁…",
            "正在刪除橋梁…",
            "Brücke wird gelöscht…",
            "Eliminando puente…",
            "Suppression du pont…",
            "Eliminazione del ponte…",
            "橋を削除中…",
            "교량 삭제 중…",
            "Usuwanie mostu…",
            "Excluindo ponte…",
            "Удаление моста…"
        },
        ["DeleteMissing"] = new[]
        {
            "Deletion failed: no bridge is registered for this UUID.",
            "删除失败：找不到该 UUID 对应的桥梁。",
            "刪除失敗：找不到該 UUID 對應的橋梁。",
            "Löschen fehlgeschlagen: Keine Brücke mit dieser UUID registriert.",
            "Error al eliminar: no hay puente registrado con este UUID.",
            "Suppression impossible : aucun pont enregistré avec cet UUID.",
            "Eliminazione fallita: nessun ponte registrato con questo UUID.",
            "削除失敗：この UUID に対応する橋が登録されていません。",
            "삭제 실패: 이 UUID로 등록된 교량이 없습니다.",
            "Usuwanie nie powiodło się: brak mostu o tym UUID.",
            "Falha ao excluir: nenhum registro para este UUID.",
            "Ошибка удаления: мост с таким UUID не зарегистрирован."
        },
        ["ConfirmDelete"] = new[]
        {
            "Delete “{0}” and every placed instance in the map? The bridge structure cannot be recovered.",
            "删除“{0}”及地图中使用它建造的桥梁？桥梁构造无法恢复。",
            "刪除「{0}」及地圖中使用它建造的橋梁？橋梁構造無法復原。",
            "„{0}“ und alle gebauten Exemplare auf der Karte löschen? Die Brückenkonstruktion kann nicht wiederhergestellt werden.",
            "¿Eliminar «{0}» y todas sus instancias del mapa? La estructura no se podrá recuperar.",
            "Supprimer « {0} » et tous ses exemplaires sur la carte ? La structure ne pourra pas être restaurée.",
            "Eliminare «{0}» e tutte le sue istanze sulla mappa? La struttura non sarà recuperabile.",
            "「{0}」とマップ上のすべての配置を削除しますか？橋の構造は復元できません。",
            "“{0}” 및 지도에 배치된 모든 인스턴스를 삭제할까요? 교량 구조는 복구할 수 없습니다.",
            "Usunąć „{0}” i wszystkie jego wystąpienia na mapie? Konstrukcji nie będzie można przywrócić.",
            "Excluir “{0}” e todas as instâncias no mapa? A estrutura não poderá ser recuperada.",
            "Удалить «{0}» и все его экземпляры на карте? Конструкцию нельзя будет восстановить."
        },
        ["DeleteDialogFailed"] = new[]
        {
            "Could not open the confirmation dialog. No bridges were deleted.",
            "无法打开删除确认框，未删除任何桥梁。",
            "無法開啟刪除確認框，未刪除任何橋梁。",
            "Bestätigung nicht verfügbar. Keine Brücken gelöscht.",
            "No se pudo abrir la confirmación. No se eliminó ningún puente.",
            "Impossible d’ouvrir la confirmation. Aucun pont supprimé.",
            "Impossibile aprire la conferma. Nessun ponte eliminato.",
            "確認画面を開けませんでした。橋は削除されていません。",
            "확인 창을 열 수 없습니다. 삭제된 교량은 없습니다.",
            "Nie można otworzyć potwierdzenia. Nie usunięto mostów.",
            "Não foi possível abrir a confirmação. Nenhuma ponte foi excluída.",
            "Не удалось открыть подтверждение. Мосты не удалены."
        },
        ["UiRefreshFailed"] = new[]
        {
            "UI refresh failed. See the BridgeBuilder log.",
            "界面数据刷新失败；请查看 BridgeBuilder 日志。",
            "介面資料重新整理失敗；請查看 BridgeBuilder 日誌。",
            "UI-Aktualisierung fehlgeschlagen. Siehe BridgeBuilder-Protokoll.",
            "Error al actualizar la interfaz. Consulta el registro de BridgeBuilder.",
            "Échec de l’actualisation. Consultez le journal BridgeBuilder.",
            "Aggiornamento interfaccia fallito. Consulta il log di BridgeBuilder.",
            "UI の更新に失敗しました。BridgeBuilder のログを確認してください。",
            "UI 새로 고침 실패. BridgeBuilder 로그를 확인하세요.",
            "Odświeżenie interfejsu nie powiodło się. Sprawdź log BridgeBuilder.",
            "Falha ao atualizar a interface. Consulte o log do BridgeBuilder.",
            "Ошибка обновления интерфейса. См. журнал BridgeBuilder."
        },
        ["PreviewInvalid"] = new[]
        {
            "Selected network or bridge prototype cannot be previewed.",
            "所选路网或桥梁原型不可用于预览。",
            "所選路網或橋梁原型不可用於預覽。",
            "Ausgewähltes Netz oder Brückenvorlage nicht für Vorschau verfügbar.",
            "No se puede previsualizar la red o el prototipo seleccionado.",
            "Aperçu indisponible pour ce réseau ou modèle de pont.",
            "Impossibile visualizzare la rete o il prototipo selezionato.",
            "選択したネットワークまたは橋の原型はプレビューできません。",
            "선택한 네트워크 또는 교량 원형을 미리 볼 수 없습니다.",
            "Nie można wyświetlić podglądu wybranej sieci lub mostu.",
            "Não é possível pré-visualizar a rede ou protótipo selecionado.",
            "Предпросмотр выбранной сети или прототипа недоступен."
        },
        ["PreviewBuildFailed"] = new[]
        {
            "Temporary bridge generation failed. See the mod log.",
            "临时桥梁生成失败，请查看模组日志。",
            "臨時橋梁生成失敗，請查看模組日誌。",
            "Temporäre Brücke konnte nicht erstellt werden. Siehe Mod-Protokoll.",
            "Error al generar el puente temporal. Consulta el registro del mod.",
            "Échec de création du pont temporaire. Consultez le journal du mod.",
            "Generazione del ponte temporaneo fallita. Consulta il log del mod.",
            "一時的な橋の生成に失敗しました。Mod のログを確認してください。",
            "임시 교량 생성 실패. 모드 로그를 확인하세요.",
            "Nie udało się utworzyć tymczasowego mostu. Sprawdź log moda.",
            "Falha ao gerar ponte temporária. Consulte o log do mod.",
            "Ошибка создания временного моста. См. журнал мода."
        },
        ["PreviewAssemblyFailed"] = new[]
        {
            "Could not assemble the complete preview model. See the mod log.",
            "无法完整组装桥梁预览模型，请查看模组日志。",
            "無法完整組裝橋梁預覽模型，請查看模組日誌。",
            "Vorschaumodell nicht vollständig zusammengesetzt. Siehe Mod-Protokoll.",
            "No se pudo ensamblar la vista previa completa. Consulta el registro.",
            "Assemblage du modèle incomplet. Consultez le journal du mod.",
            "Assemblaggio anteprima incompleto. Consulta il log del mod.",
            "プレビューモデルを完全に組み立てられません。Mod のログを確認してください。",
            "미리보기 모델 조립 실패. 모드 로그를 확인하세요.",
            "Nie udało się złożyć pełnego podglądu. Sprawdź log moda.",
            "Falha ao montar a prévia completa. Consulte o log do mod.",
            "Не удалось собрать модель целиком. См. журнал мода."
        },
        ["PreviewFailed"] = new[]
        {
            "Bridge preview failed. No permanent asset was created.",
            "桥梁预览失败，未创建正式资产。",
            "橋梁預覽失敗，未建立正式資產。",
            "Brückenvorschau fehlgeschlagen. Kein dauerhaftes Asset erstellt.",
            "Error de vista previa. No se creó ningún recurso permanente.",
            "Échec de l’aperçu. Aucun objet permanent créé.",
            "Anteprima fallita. Nessuna risorsa permanente creata.",
            "橋のプレビューに失敗しました。正式なアセットは作成されていません。",
            "교량 미리보기 실패. 정식 에셋은 생성되지 않았습니다.",
            "Podgląd nie powiódł się. Nie utworzono trwałego zasobu.",
            "Falha na prévia. Nenhum recurso permanente foi criado.",
            "Ошибка предпросмотра. Постоянный ресурс не создан."
        },
        ["RenderTimeout"] = new[]
        {
            "Preview camera could not complete HDRP rendering.",
            "预览相机未能完成 HDRP 渲染。",
            "預覽相機未能完成 HDRP 渲染。",
            "Vorschaukamera konnte HDRP-Rendering nicht abschließen.",
            "La cámara no pudo completar el renderizado HDRP.",
            "La caméra n’a pas pu terminer le rendu HDRP.",
            "La camera non ha completato il rendering HDRP.",
            "プレビューカメラが HDRP レンダリングを完了できませんでした。",
            "미리보기 카메라가 HDRP 렌더링을 완료하지 못했습니다.",
            "Kamera podglądu nie ukończyła renderowania HDRP.",
            "A câmera de prévia não concluiu a renderização HDRP.",
            "Камера предпросмотра не завершила рендеринг HDRP."
        },
        ["RenderEmpty"] = new[]
        {
            "Model built, but rendered image is empty. See the mod log.",
            "模型已构建，但渲染结果为空；请查看模组日志。",
            "模型已建構，但渲染結果為空；請查看模組日誌。",
            "Modell erstellt, aber Renderbild leer. Siehe Mod-Protokoll.",
            "Modelo creado, pero imagen vacía. Consulta el registro del mod.",
            "Modèle créé, mais image vide. Consultez le journal du mod.",
            "Modello creato, ma immagine vuota. Consulta il log del mod.",
            "モデルは生成されましたが画像が空です。Mod のログを確認してください。",
            "모델은 생성됐지만 렌더링 이미지가 비어 있습니다. 로그를 확인하세요.",
            "Model gotowy, ale obraz jest pusty. Sprawdź log moda.",
            "Modelo criado, mas a imagem está vazia. Consulte o log.",
            "Модель создана, но изображение пустое. См. журнал мода."
        },
        ["RenderReadFailed"] = new[]
        {
            "Could not read the bridge preview image.",
            "读取桥梁预览图像失败。",
            "讀取橋梁預覽圖像失敗。",
            "Brückenvorschaubild konnte nicht gelesen werden.",
            "No se pudo leer la imagen del puente.",
            "Impossible de lire l’image du pont.",
            "Impossibile leggere l’immagine di anteprima.",
            "橋のプレビュー画像を読み取れませんでした。",
            "교량 미리보기 이미지를 읽을 수 없습니다.",
            "Nie można odczytać obrazu podglądu.",
            "Não foi possível ler a imagem de prévia.",
            "Не удалось прочитать изображение моста."
        },
        ["RenderFailed"] = new[]
        {
            "Bridge mesh rendering failed.",
            "桥梁网格渲染失败。",
            "橋梁網格渲染失敗。",
            "Rendern des Brücken-Meshes fehlgeschlagen.",
            "Error al renderizar la malla del puente.",
            "Échec du rendu du maillage du pont.",
            "Rendering della mesh del ponte fallito.",
            "橋のメッシュのレンダリングに失敗しました。",
            "교량 메시 렌더링 실패.",
            "Renderowanie siatki mostu nie powiodło się.",
            "Falha ao renderizar a malha da ponte.",
            "Ошибка рендеринга сетки моста."
        },
        ["UpperUnavailable"] = new[]
        {
            "Creation failed: selected upper network is unavailable.",
            "创建失败：所选上层道路或轨道已不可用。",
            "建立失敗：所選上層道路或軌道已不可用。",
            "Erstellen fehlgeschlagen: Oberes Netz nicht verfügbar.",
            "Error al crear: la red superior no está disponible.",
            "Création impossible : réseau supérieur indisponible.",
            "Creazione fallita: rete superiore non disponibile.",
            "作成失敗：選択した上層ネットワークが利用できません。",
            "생성 실패: 선택한 상층 네트워크를 사용할 수 없습니다.",
            "Tworzenie nie powiodło się: górna sieć niedostępna.",
            "Falha ao criar: rede superior indisponível.",
            "Ошибка создания: верхняя сеть недоступна."
        },
        ["StyleUnavailable"] = new[]
        {
            "Creation failed: bridge prototype or required content is unavailable.",
            "创建失败：所选桥梁原型或前置内容已不可用。",
            "建立失敗：所選橋梁原型或前置內容已不可用。",
            "Erstellen fehlgeschlagen: Brückenvorlage oder Voraussetzung fehlt.",
            "Error al crear: faltan el prototipo o sus requisitos.",
            "Création impossible : modèle ou dépendance indisponible.",
            "Creazione fallita: prototipo o contenuto richiesto non disponibile.",
            "作成失敗：橋の原型または必要なコンテンツが利用できません。",
            "생성 실패: 교량 원형 또는 필수 콘텐츠를 사용할 수 없습니다.",
            "Tworzenie nie powiodło się: brak pierwowzoru lub wymaganej zawartości.",
            "Falha ao criar: protótipo ou conteúdo necessário indisponível.",
            "Ошибка создания: прототип или зависимость недоступны."
        },
        ["LowerUnavailable"] = new[]
        {
            "Creation failed: selected lower network is unavailable.",
            "创建失败：所选下层道路或轨道已不可用。",
            "建立失敗：所選下層道路或軌道已不可用。",
            "Erstellen fehlgeschlagen: Unteres Netz nicht verfügbar.",
            "Error al crear: la red inferior no está disponible.",
            "Création impossible : réseau inférieur indisponible.",
            "Creazione fallita: rete inferiore non disponibile.",
            "作成失敗：選択した下層ネットワークが利用できません。",
            "생성 실패: 선택한 하층 네트워크를 사용할 수 없습니다.",
            "Tworzenie nie powiodło się: dolna sieć niedostępna.",
            "Falha ao criar: rede inferior indisponível.",
            "Ошибка создания: нижняя сеть недоступна."
        },
        ["NoDoublePrototype"] = new[]
        {
            "Creation failed: this style has no double-deck prototype.",
            "创建失败：该桥型没有双层原型。",
            "建立失敗：該橋型沒有雙層原型。",
            "Erstellen fehlgeschlagen: Keine zweistöckige Vorlage.",
            "Error al crear: no hay prototipo de dos niveles.",
            "Création impossible : aucun modèle à deux niveaux.",
            "Creazione fallita: nessun prototipo a due livelli.",
            "作成失敗：この種類に二層の原型はありません。",
            "생성 실패: 이 유형에는 복층 원형이 없습니다.",
            "Tworzenie nie powiodło się: brak pierwowzoru dwupoziomowego.",
            "Falha ao criar: sem protótipo de dois níveis.",
            "Ошибка создания: нет двухъярусного прототипа."
        },
        ["NoSinglePrototype"] = new[]
        {
            "Creation failed: this style has no single-deck prototype.",
            "创建失败：该桥型没有单层原型。",
            "建立失敗：該橋型沒有單層原型。",
            "Erstellen fehlgeschlagen: Keine einstöckige Vorlage.",
            "Error al crear: no hay prototipo de un nivel.",
            "Création impossible : aucun modèle à un niveau.",
            "Creazione fallita: nessun prototipo a un livello.",
            "作成失敗：この種類に単層の原型はありません。",
            "생성 실패: 이 유형에는 단층 원형이 없습니다.",
            "Tworzenie nie powiodło się: brak pierwowzoru jednopoziomowego.",
            "Falha ao criar: sem protótipo de um nível.",
            "Ошибка создания: нет одноярусного прототипа."
        },
        ["UuidFailed"] = new[]
        {
            "Creation failed: could not allocate a unique UUID.",
            "创建失败：无法分配唯一 UUID。",
            "建立失敗：無法分配唯一 UUID。",
            "Erstellen fehlgeschlagen: Keine eindeutige UUID zugewiesen.",
            "Error al crear: no se pudo asignar un UUID único.",
            "Création impossible : échec d’attribution d’un UUID unique.",
            "Creazione fallita: impossibile assegnare un UUID univoco.",
            "作成失敗：一意の UUID を割り当てられませんでした。",
            "생성 실패: 고유 UUID를 할당할 수 없습니다.",
            "Tworzenie nie powiodło się: nie można nadać unikalnego UUID.",
            "Falha ao criar: não foi possível atribuir um UUID único.",
            "Ошибка создания: не удалось назначить уникальный UUID."
        },
        ["CreatedActive"] = new[]
        {
            "Created “{0}” and entered build mode.",
            "已创建“{0}”，并进入建造模式。",
            "已建立「{0}」，並進入建造模式。",
            "„{0}“ erstellt und Baumodus aktiviert.",
            "«{0}» creado; modo construcción activado.",
            "« {0} » créé, mode construction activé.",
            "«{0}» creato, modalità costruzione attivata.",
            "「{0}」を作成し、建設モードに入りました。",
            "“{0}” 생성 후 건설 모드 진입.",
            "Utworzono „{0}” i włączono budowanie.",
            "“{0}” criada e modo de construção ativado.",
            "«{0}» создан, включён режим строительства."
        },
        ["CreatedManage"] = new[]
        {
            "Created “{0}”. Build it from Manage bridges.",
            "已创建“{0}”，请从管理页建造。",
            "已建立「{0}」，請從管理頁建造。",
            "„{0}“ erstellt. Über die Verwaltung bauen.",
            "«{0}» creado. Constrúyelo desde Gestionar puentes.",
            "« {0} » créé. Construisez-le depuis la gestion.",
            "«{0}» creato. Costruiscilo dalla gestione ponti.",
            "「{0}」を作成しました。管理画面から建設してください。",
            "“{0}” 생성됨. 관리 화면에서 건설하세요.",
            "Utworzono „{0}”. Buduj z panelu zarządzania.",
            "“{0}” criada. Construa pelo gerenciamento.",
            "«{0}» создан. Стройте через управление мостами."
        },
        ["RegistrationFailed"] = new[]
        {
            "Could not save registration for bridge {0}. Creation did not complete.",
            "无法保存桥梁 {0} 的注册记录，创建未完成。",
            "無法儲存橋梁 {0} 的註冊記錄，建立未完成。",
            "Registrierung für Brücke {0} nicht gespeichert. Erstellung nicht abgeschlossen.",
            "No se pudo guardar el registro del puente {0}. La creación no se completó.",
            "Impossible d’enregistrer le pont {0}. La création n’est pas terminée.",
            "Impossibile registrare il ponte {0}. Creazione non completata.",
            "橋 {0} の登録を保存できませんでした。作成は完了していません。",
            "교량 {0}의 등록을 저장하지 못했습니다. 생성이 완료되지 않았습니다.",
            "Nie zapisano rejestracji mostu {0}. Tworzenie nie zostało ukończone.",
            "Não foi possível registrar a ponte {0}. A criação não foi concluída.",
            "Не удалось сохранить регистрацию моста {0}. Создание не завершено."
        },
        ["CreateFailed"] = new[]
        {
            "Bridge creation failed. Restart the game to recover unfinished assets.",
            "桥梁创建失败。请重启游戏以恢复处理未完成的资产。",
            "橋梁建立失敗。請重新啟動遊戲以復原處理未完成的資產。",
            "Brückenerstellung fehlgeschlagen. Spiel zur Wiederherstellung unvollständiger Assets neu starten.",
            "Error al crear el puente. Reinicia el juego para recuperar los recursos incompletos.",
            "Échec de création du pont. Redémarrez le jeu pour récupérer les ressources incomplètes.",
            "Creazione ponte fallita. Riavvia il gioco per recuperare le risorse incomplete.",
            "橋の作成に失敗しました。ゲームを再起動して未完了のアセットを復旧してください。",
            "교량 생성 실패. 게임을 다시 시작하여 미완성 에셋을 복구하세요.",
            "Tworzenie mostu nie powiodło się. Uruchom grę ponownie, aby odzyskać niekompletne zasoby.",
            "Falha ao criar a ponte. Reinicie o jogo para recuperar os recursos incompletos.",
            "Ошибка создания моста. Перезапустите игру для восстановления незавершённых ресурсов."
        },
        ["ActivateInvalid"] = new[]
        {
            "Activation failed: invalid or unregistered bridge UUID.",
            "激活失败：无效或未注册的桥梁 UUID。",
            "啟用失敗：無效或未註冊的橋梁 UUID。",
            "Aktivierung fehlgeschlagen: Ungültige oder unregistrierte UUID.",
            "Error al activar: UUID inválido o no registrado.",
            "Activation impossible : UUID invalide ou non enregistré.",
            "Attivazione fallita: UUID non valido o non registrato.",
            "起動失敗：橋の UUID が無効または未登録です。",
            "활성화 실패: UUID가 잘못되었거나 미등록 상태입니다.",
            "Aktywacja nie powiodła się: nieprawidłowy lub niezarejestrowany UUID.",
            "Falha ao ativar: UUID inválido ou não registrado.",
            "Ошибка активации: UUID неверен или не зарегистрирован."
        },
        ["Activated"] = new[]
        {
            "Bridge build mode activated.",
            "已进入桥梁建造模式。",
            "已進入橋梁建造模式。",
            "Brückenbaumodus aktiviert.",
            "Modo de construcción de puentes activado.",
            "Mode construction de pont activé.",
            "Modalità costruzione ponte attivata.",
            "橋の建設モードに入りました。",
            "교량 건설 모드로 진입했습니다.",
            "Włączono tryb budowania mostu.",
            "Modo de construção de ponte ativado.",
            "Включён режим строительства моста."
        },
        ["ActivateUnloaded"] = new[]
        {
            "Activation failed: this bridge prefab is not loaded.",
            "激活失败：该桥梁 Prefab 当前没有加载。",
            "啟用失敗：該橋梁 Prefab 目前未載入。",
            "Aktivierung fehlgeschlagen: Brücken-Prefab nicht geladen.",
            "Error al activar: el prefab no está cargado.",
            "Activation impossible : prefab du pont non chargé.",
            "Attivazione fallita: prefab del ponte non caricato.",
            "起動失敗：橋の Prefab が読み込まれていません。",
            "활성화 실패: 교량 프리팹이 로드되지 않았습니다.",
            "Aktywacja nie powiodła się: prefab mostu nie jest wczytany.",
            "Falha ao ativar: prefab da ponte não carregado.",
            "Ошибка активации: prefab моста не загружен."
        },
        ["RenameInvalid"] = new[]
        {
            "Renaming failed: invalid prefab UUID.",
            "改名失败：Prefab UUID 无效。",
            "更名失敗：Prefab UUID 無效。",
            "Umbenennen fehlgeschlagen: Ungültige Prefab-UUID.",
            "Error al renombrar: UUID del prefab inválido.",
            "Renommage impossible : UUID du prefab invalide.",
            "Rinomina fallita: UUID del prefab non valido.",
            "名前変更失敗：Prefab の UUID が無効です。",
            "이름 변경 실패: 프리팹 UUID가 잘못되었습니다.",
            "Zmiana nazwy nie powiodła się: nieprawidłowy UUID prefabu.",
            "Falha ao renomear: UUID do prefab inválido.",
            "Ошибка переименования: неверный UUID prefab."
        },
        ["NameRequired"] = new[]
        {
            "Renaming failed: display name cannot be empty.",
            "改名失败：注册名称不能为空。",
            "更名失敗：註冊名稱不能為空。",
            "Umbenennen fehlgeschlagen: Anzeigename darf nicht leer sein.",
            "Error al renombrar: el nombre no puede estar vacío.",
            "Renommage impossible : le nom ne peut pas être vide.",
            "Rinomina fallita: il nome non può essere vuoto.",
            "名前変更失敗：表示名は空にできません。",
            "이름 변경 실패: 표시 이름을 입력하세요.",
            "Zmiana nazwy nie powiodła się: nazwa nie może być pusta.",
            "Falha ao renomear: o nome não pode estar vazio.",
            "Ошибка переименования: название не может быть пустым."
        },
        ["RenameFailed"] = new[]
        {
            "Renaming failed: registration was not found or could not be saved.",
            "改名失败：找不到注册记录或无法保存。",
            "更名失敗：找不到註冊記錄或無法儲存。",
            "Umbenennen fehlgeschlagen: Registrierung fehlt oder konnte nicht gespeichert werden.",
            "Error al renombrar: registro ausente o imposible de guardar.",
            "Renommage impossible : enregistrement absent ou non sauvegardé.",
            "Rinomina fallita: registrazione assente o non salvabile.",
            "名前変更失敗：登録が見つからないか保存できません。",
            "이름 변경 실패: 등록 기록이 없거나 저장할 수 없습니다.",
            "Zmiana nazwy nie powiodła się: brak rejestracji lub błąd zapisu.",
            "Falha ao renomear: registro ausente ou não foi possível salvar.",
            "Ошибка переименования: запись не найдена или не сохранена."
        },
        ["Renamed"] = new[]
        {
            "Bridge name changed to “{0}”.",
            "桥梁名称已改为“{0}”。",
            "橋梁名稱已改為「{0}」。",
            "Brückenname in „{0}“ geändert.",
            "Nombre del puente cambiado a «{0}».",
            "Nom du pont changé en « {0} ».",
            "Nome del ponte cambiato in «{0}».",
            "橋の名前を「{0}」に変更しました。",
            "교량 이름을 “{0}”(으)로 변경했습니다.",
            "Zmieniono nazwę mostu na „{0}”.",
            "Nome da ponte alterado para “{0}”.",
            "Название моста изменено на «{0}»."
        },
        ["DeleteUnsafe"] = new[]
        {
            "Deletion failed: could not safely remove placed instances. Prefab and registration retained.",
            "删除失败：无法安全移除地图实例，因此保留了 Prefab 和 UUID 注册记录。",
            "刪除失敗：無法安全移除地圖實例，因此保留了 Prefab 和 UUID 註冊記錄。",
            "Löschen fehlgeschlagen: Gebaute Exemplare nicht sicher entfernbar. Prefab und Registrierung beibehalten.",
            "Error al eliminar instancias del mapa de forma segura. Se conservan prefab y registro.",
            "Suppression sûre des exemplaires impossible. Prefab et enregistrement conservés.",
            "Impossibile rimuovere le istanze in sicurezza. Prefab e registrazione conservati.",
            "配置済みの橋を安全に削除できません。Prefab と登録は保持されました。",
            "배치된 교량을 안전하게 삭제할 수 없습니다. 프리팹과 등록 기록을 유지했습니다.",
            "Nie można bezpiecznie usunąć wystąpień. Zachowano prefab i rejestrację.",
            "Não foi possível remover as instâncias com segurança. Prefab e registro mantidos.",
            "Нельзя безопасно удалить экземпляры. Prefab и регистрация сохранены."
        },
        ["Deleted"] = new[]
        {
            "Deleted “{0}”, its {1} placed instances and UUID registration.",
            "已删除“{0}”、其 {1} 个地图实例及 UUID 注册记录。",
            "已刪除「{0}」、其 {1} 個地圖實例及 UUID 註冊記錄。",
            "„{0}“, {1} gebaute Exemplare und UUID-Registrierung gelöscht.",
            "Se eliminaron «{0}», sus {1} instancias y el registro UUID.",
            "« {0} », ses {1} exemplaires et son UUID ont été supprimés.",
            "Eliminati «{0}», le sue {1} istanze e la registrazione UUID.",
            "「{0}」、配置済み {1} 個、UUID 登録を削除しました。",
            "“{0}”, 배치된 인스턴스 {1}개 및 UUID 등록을 삭제했습니다.",
            "Usunięto „{0}”, jego wystąpienia ({1}) i rejestrację UUID.",
            "“{0}”, suas {1} instâncias e o registro UUID foram excluídos.",
            "Удалены «{0}», его экземпляры ({1}) и регистрация UUID."
        },
        ["DeleteIncomplete"] = new[]
        {
            "Deletion incomplete: prefab assets or UUID registration remain. See the report.",
            "删除未完成：Prefab 资产或 UUID 注册记录仍然存在；请查看报告。",
            "刪除未完成：Prefab 資產或 UUID 註冊記錄仍然存在；請查看報告。",
            "Löschen unvollständig: Prefab-Assets oder UUID-Registrierung verbleiben. Siehe Bericht.",
            "Eliminación incompleta: quedan recursos o el registro UUID. Consulta el informe.",
            "Suppression incomplète : des objets ou l’UUID subsistent. Consultez le rapport.",
            "Eliminazione incompleta: restano risorse o registrazione UUID. Consulta il rapporto.",
            "削除未完了：アセットまたは UUID 登録が残っています。レポートを確認してください。",
            "삭제 미완료: 에셋 또는 UUID 등록이 남아 있습니다. 보고서를 확인하세요.",
            "Usuwanie niepełne: pozostały zasoby lub UUID. Sprawdź raport.",
            "Exclusão incompleta: restam recursos ou registro UUID. Consulte o relatório.",
            "Удаление не завершено: остались ресурсы или UUID. См. отчёт."
        },
    };

    private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> Cache = new(StringComparer.Ordinal);

    internal static IReadOnlyDictionary<string, string> ForLocale(string? localeId)
    {
        var locale = UiStringCatalog.Resolve(localeId);
        lock (Cache)
        {
            if (Cache.TryGetValue(locale, out var cached)) return cached;
            var column = Array.IndexOf(LocaleIds, locale);
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in Entries)
                result[entry.Key] = column >= 0 && column < entry.Value.Length &&
                    !string.IsNullOrEmpty(entry.Value[column]) ? entry.Value[column] : entry.Value[0];
            Cache[locale] = result;
            return result;
        }
    }

    internal static string Get(string key, params object[] arguments) =>
        Format(UiStringCatalog.Current.LocaleId, key, arguments);

    internal static string Format(string? localeId, string key, params object[] arguments)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        var table = ForLocale(localeId);
        if (!table.TryGetValue(key, out var text))
            return "Bridge Builder"; // Missing keys are rejected by the localization check.
        if (arguments.Length == 0) return text;
        try { return string.Format(CultureInfo.CurrentCulture, text, arguments); }
        catch (FormatException)
        {
            // A bad community translation must not unwind the simulation update.
            return string.Format(CultureInfo.InvariantCulture, Entries[key][0], arguments);
        }
    }
}

/// <summary>Retain the key and original arguments so existing status messages also change language.</summary>
internal sealed class RuntimeUiMessage
{
    private readonly string _key;
    private readonly object[] _arguments;

    internal RuntimeUiMessage(string key, params object[] arguments)
    {
        _key = key;
        _arguments = arguments;
    }

    internal string Text => RuntimeUiText.Get(_key, _arguments);
}
