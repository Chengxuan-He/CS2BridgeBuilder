namespace BridgePrefabGenerator.Settings;

internal static partial class UiStringTables
{
    internal static UiStrings German() => new UiStrings
    {
        Title = "Straßen-Prefab-Exporter",
        TabRoads = "Straßen",
        TabOptions = "Optionen",
        GroupStatus = "Status",
        GroupSelection = "Auswahl",
        GroupActions = "Aktionen",
        GroupRoads = "Straßenliste",
        DetailSummary = "Breite ~{0} m - Tempolimit {1}",
        DetailLastExport = "Zuletzt exportiert: {0}",
        GroupExport = "Export",
        GroupMaintenance = "Wartung",

        StatusNotExported = "nicht exportiert",
        StatusExported = "exportiert",
        StatusOutdated = "seit dem letzten Export geändert",
        StatusExportedPendingRestart = "gerade exportiert",
        StatusRemovedPendingRestart = "gerade entfernt - Neustart erforderlich",

        StateNoWorld = "Keine Welt geladen. Öffne den Editor, um Road-Builder-Straßen aufzulisten.",
        StateGameplayBlocked = "Export außerhalb des Editors ist aus. Öffne den Editor oder aktiviere \"Export außerhalb des Editors erlauben\".",
        StateScanning = "Warte, bis Road Builder seine Straßen erzeugt hat ...",
        StateNoRoads = "Keine Road-Builder-Straßen gefunden. Prüfe, ob Road Builder in diesem Playset aktiv ist.",
        StateBrokenRoads = "{0} Straße(n) übersprungen: Road Builder konnte sie nicht erzeugen (Konfiguration fehlt).",
        StateNameConflicts = "{0} Straße(n) wegen Namenskonflikt übersprungen. Benenne sie in Road Builder um.",
        StatePageIndicator = "Seite {0} von {1} - zeigt {2}-{3} von {4}.",
        StateReady = "{0} Straßen: {1} exportiert, {2} nicht exportiert, {3} seit dem Export geändert.",
        StateSelected = "{0} ausgewählt.",
        StateRestartHint = "Exportierte Straßen sind sofort registriert; kein Neustart nötig.",
        StateReportHint = "Vollständiger Bericht: ModsData\\RoadPrefabExporter\\last-export-report.txt",
        OperationSummary = "Letzter Lauf: {0} exportiert, {1} entfernt, {2} übersprungen, {3} fehlgeschlagen.",
        NothingSelected = "Nichts zu tun: keine Straße ausgewählt.",
    }
        .Option(nameof(BridgeSetting.StatusText), "Aktueller Stand",
            "Straßen erscheinen hier, sobald eine Welt mit Road-Builder-Straßen geladen ist.")
        .Option(nameof(BridgeSetting.RescanRoads), "Straßen neu einlesen",
            "Liest die Straßenliste und den Exportstatus erneut ein.")
        .Option(nameof(BridgeSetting.ExportSelected), "Ausgewählte Straßen exportieren",
            "Wandelt jede ausgewählte Straße in ein natives RoadPrefab-Asset um. Starte das Spiel neu, bevor du die Ergebnisse benutzt.")
        .Option(nameof(BridgeSetting.ArmRemoval), "Entfernen erlauben",
            "Sicherung. Das Entfernen löscht Asset-Dateien unwiderruflich, daher bleibt die Schaltfläche deaktiviert, bis dies aktiv ist.")
        .Option(nameof(BridgeSetting.RemoveSelected), "Exporte der ausgewählten Straßen entfernen",
            "Löscht die exportierten Assets. Bereits in einer Stadt platzierte Straßen gehen dabei kaputt.")
        .Option(nameof(BridgeSetting.OverwriteExisting), "Vorhandene Exporte überschreiben",
            "Exportiert eine Straße auch dann erneut, wenn ihr Asset bereits existiert.")
        .Option(nameof(BridgeSetting.AllowGameplayExport), "Export außerhalb des Editors erlauben",
            "Standardmäßig aus: Benutzer-Assets aus einem Stadt-Spielstand zu schreiben ist riskanter als im Editor.")
        .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "Unbenutzte Abhängigkeiten mitentfernen",
            "Löscht nach dem Entfernen exportierte Netzabschnitte und -teile, die keine verbleibende Straße mehr referenziert.")
        .Option(nameof(BridgeSetting.EmbedIcons), "Vorschaubilder in die Assets einbetten",
            "Macht eine exportierte Straße eigenständig, sodass ihr Vorschaubild auch beim Teilen oder ohne dieses Mod funktioniert. Kostet etwa 20-70 KB pro Straße. Aus: Vorschaubilder kommen aus dem Ordner dieses Mods und funktionieren nur auf deinem Rechner.");

    internal static UiStrings French() => new UiStrings
    {
        Title = "Exportateur de prefabs de route",
        TabRoads = "Routes",
        TabOptions = "Options",
        GroupStatus = "État",
        GroupSelection = "Sélection",
        GroupActions = "Actions",
        GroupRoads = "Liste des routes",
        DetailSummary = "Largeur ~{0} m - vitesse {1}",
        DetailLastExport = "Dernier export : {0}",
        GroupExport = "Export",
        GroupMaintenance = "Maintenance",

        StatusNotExported = "non exportée",
        StatusExported = "exportée",
        StatusOutdated = "modifiée depuis le dernier export",
        StatusExportedPendingRestart = "exportée à l'instant",
        StatusRemovedPendingRestart = "supprimée à l'instant - redémarrage requis",

        StateNoWorld = "Aucun monde chargé. Ouvrez l'Éditeur pour lister les routes Road Builder.",
        StateGameplayBlocked = "L'export hors de l'Éditeur est désactivé. Ouvrez l'Éditeur ou activez « Autoriser l'export hors de l'Éditeur ».",
        StateScanning = "En attente de la génération des routes par Road Builder...",
        StateNoRoads = "Aucune route Road Builder trouvée. Vérifiez que Road Builder est activé dans ce playset.",
        StateBrokenRoads = "{0} route(s) ignorée(s) : Road Builder n'a pas pu les générer (configuration manquante).",
        StateNameConflicts = "{0} route(s) ignorée(s) pour conflit de nom. Renommez-les dans Road Builder.",
        StatePageIndicator = "Page {0} sur {1} - {2}-{3} sur {4} affichées.",
        StateReady = "{0} routes : {1} exportées, {2} non exportées, {3} modifiées depuis l'export.",
        StateSelected = "{0} cochée(s).",
        StateRestartHint = "Les routes exportées sont enregistrées immédiatement ; aucun redémarrage nécessaire.",
        StateReportHint = "Rapport complet : ModsData\\RoadPrefabExporter\\last-export-report.txt",
        OperationSummary = "Dernière exécution : {0} exportées, {1} supprimées, {2} ignorées, {3} en échec.",
        NothingSelected = "Rien à faire : aucune route cochée.",
    }
        .Option(nameof(BridgeSetting.StatusText), "État actuel",
            "Les routes apparaissent ici lorsqu'un monde contenant des routes Road Builder est chargé.")
        .Option(nameof(BridgeSetting.RescanRoads), "Réanalyser les routes",
            "Relit la liste des routes et l'état d'export.")
        .Option(nameof(BridgeSetting.ExportSelected), "Exporter les routes cochées",
            "Convertit chaque route cochée en asset RoadPrefab natif. Redémarrez le jeu avant d'utiliser les résultats.")
        .Option(nameof(BridgeSetting.ArmRemoval), "Autoriser la suppression",
            "Sécurité. La suppression efface des fichiers d'asset et est irréversible ; le bouton reste désactivé tant que ceci est désactivé.")
        .Option(nameof(BridgeSetting.RemoveSelected), "Supprimer les exports des routes cochées",
            "Supprime les assets exportés. Les routes déjà posées dans une ville seront cassées.")
        .Option(nameof(BridgeSetting.OverwriteExisting), "Écraser les exports existants",
            "Réexporte une route même si son asset existe déjà.")
        .Option(nameof(BridgeSetting.AllowGameplayExport), "Autoriser l'export hors de l'Éditeur",
            "Désactivé par défaut : écrire des assets utilisateur depuis une sauvegarde de ville est plus risqué que depuis l'Éditeur.")
        .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "Supprimer aussi les dépendances inutilisées",
            "Après une suppression, efface les sections et pièces exportées qu'aucune route restante ne référence.")
        .Option(nameof(BridgeSetting.EmbedIcons), "Intégrer les vignettes dans les assets",
            "Rend une route exportée autonome : sa vignette fonctionne même partagée ou sans ce mod. Coût : environ 20-70 Ko par route. Désactivé, les vignettes viennent du dossier de ce mod et ne fonctionnent que sur votre machine.");

    internal static UiStrings Spanish() => new UiStrings
    {
        Title = "Exportador de prefabs de carretera",
        TabRoads = "Carreteras",
        TabOptions = "Opciones",
        GroupStatus = "Estado",
        GroupSelection = "Selección",
        GroupActions = "Acciones",
        GroupRoads = "Lista de carreteras",
        DetailSummary = "Anchura ~{0} m - límite {1}",
        DetailLastExport = "Última exportación: {0}",
        GroupExport = "Exportación",
        GroupMaintenance = "Mantenimiento",

        StatusNotExported = "no exportada",
        StatusExported = "exportada",
        StatusOutdated = "modificada desde la última exportación",
        StatusExportedPendingRestart = "recién exportada",
        StatusRemovedPendingRestart = "recién eliminada - requiere reinicio",

        StateNoWorld = "No hay ningún mundo cargado. Abre el Editor para listar las carreteras de Road Builder.",
        StateGameplayBlocked = "La exportación fuera del Editor está desactivada. Abre el Editor o activa «Permitir exportar fuera del Editor».",
        StateScanning = "Esperando a que Road Builder termine de generar sus carreteras...",
        StateNoRoads = "No se han encontrado carreteras de Road Builder. Comprueba que Road Builder esté activo en este playset.",
        StateBrokenRoads = "{0} carretera(s) omitida(s): Road Builder no pudo generarla(s) (falta la configuración).",
        StateNameConflicts = "{0} carretera(s) omitida(s) por conflicto de nombre. Renómbrala(s) en Road Builder.",
        StatePageIndicator = "Página {0} de {1}: mostrando {2}-{3} de {4}.",
        StateReady = "{0} carreteras: {1} exportadas, {2} sin exportar, {3} modificadas desde la exportación.",
        StateSelected = "{0} marcadas.",
        StateRestartHint = "Las carreteras exportadas se registran de inmediato; no hace falta reiniciar.",
        StateReportHint = "Informe completo: ModsData\\RoadPrefabExporter\\last-export-report.txt",
        OperationSummary = "Última ejecución: {0} exportadas, {1} eliminadas, {2} omitidas, {3} fallidas.",
        NothingSelected = "Nada que hacer: no hay ninguna carretera marcada.",
    }
        .Option(nameof(BridgeSetting.StatusText), "Estado actual",
            "Las carreteras aparecen aquí mientras haya cargado un mundo con carreteras de Road Builder.")
        .Option(nameof(BridgeSetting.RescanRoads), "Volver a analizar",
            "Vuelve a leer la lista de carreteras y el estado de exportación.")
        .Option(nameof(BridgeSetting.ExportSelected), "Exportar las carreteras marcadas",
            "Convierte cada carretera marcada en un recurso RoadPrefab nativo. Reinicia el juego antes de usar los resultados.")
        .Option(nameof(BridgeSetting.ArmRemoval), "Permitir la eliminación",
            "Seguro. La eliminación borra archivos de recursos y no se puede deshacer, por lo que el botón permanece desactivado hasta activar esto.")
        .Option(nameof(BridgeSetting.RemoveSelected), "Eliminar las exportaciones marcadas",
            "Borra los recursos exportados. Las carreteras ya colocadas en una ciudad se romperán.")
        .Option(nameof(BridgeSetting.OverwriteExisting), "Sobrescribir exportaciones existentes",
            "Exporta la carretera de nuevo aunque su recurso ya exista.")
        .Option(nameof(BridgeSetting.AllowGameplayExport), "Permitir exportar fuera del Editor",
            "Desactivado por defecto: escribir recursos de usuario desde una partida es más arriesgado que hacerlo en el Editor.")
        .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "Eliminar también las dependencias sin usar",
            "Tras una eliminación, borra las secciones y piezas exportadas que ninguna carretera restante referencia.")
        .Option(nameof(BridgeSetting.EmbedIcons), "Incrustar las miniaturas en los recursos",
            "Hace que la carretera exportada sea autónoma: su miniatura sigue funcionando al compartirla o sin este mod. Cuesta unos 20-70 KB por carretera. Desactivado, las miniaturas salen de la carpeta de este mod y solo funcionan en tu equipo.");

    internal static UiStrings Italian() => new UiStrings
    {
        Title = "Esportatore di prefab stradali",
        TabRoads = "Strade",
        TabOptions = "Opzioni",
        GroupStatus = "Stato",
        GroupSelection = "Selezione",
        GroupActions = "Azioni",
        GroupRoads = "Elenco strade",
        DetailSummary = "Larghezza ~{0} m - limite {1}",
        DetailLastExport = "Ultima esportazione: {0}",
        GroupExport = "Esportazione",
        GroupMaintenance = "Manutenzione",

        StatusNotExported = "non esportata",
        StatusExported = "esportata",
        StatusOutdated = "modificata dall'ultima esportazione",
        StatusExportedPendingRestart = "appena esportata",
        StatusRemovedPendingRestart = "appena rimossa - riavvio necessario",

        StateNoWorld = "Nessun mondo caricato. Apri l'Editor per elencare le strade di Road Builder.",
        StateGameplayBlocked = "L'esportazione fuori dall'Editor è disattivata. Apri l'Editor oppure attiva \"Consenti l'esportazione fuori dall'Editor\".",
        StateScanning = "In attesa che Road Builder finisca di generare le strade...",
        StateNoRoads = "Nessuna strada di Road Builder trovata. Verifica che Road Builder sia attivo in questo playset.",
        StateBrokenRoads = "{0} strada/e saltata/e: Road Builder non è riuscito a generarla/e (configurazione mancante).",
        StateNameConflicts = "{0} strada/e saltata/e per conflitto di nome. Rinominala/e in Road Builder.",
        StatePageIndicator = "Pagina {0} di {1} - mostra {2}-{3} di {4}.",
        StateReady = "{0} strade: {1} esportate, {2} non esportate, {3} modificate dall'esportazione.",
        StateSelected = "{0} selezionate.",
        StateRestartHint = "Le strade esportate sono registrate subito; non serve riavviare.",
        StateReportHint = "Report completo: ModsData\\RoadPrefabExporter\\last-export-report.txt",
        OperationSummary = "Ultima esecuzione: {0} esportate, {1} rimosse, {2} saltate, {3} fallite.",
        NothingSelected = "Niente da fare: nessuna strada selezionata.",
    }
        .Option(nameof(BridgeSetting.StatusText), "Stato attuale",
            "Le strade compaiono qui quando è caricato un mondo che contiene strade di Road Builder.")
        .Option(nameof(BridgeSetting.RescanRoads), "Rileggi le strade",
            "Rilegge l'elenco delle strade e lo stato di esportazione.")
        .Option(nameof(BridgeSetting.ExportSelected), "Esporta le strade selezionate",
            "Converte ogni strada selezionata in un asset RoadPrefab nativo. Riavvia il gioco prima di usare i risultati.")
        .Option(nameof(BridgeSetting.ArmRemoval), "Consenti la rimozione",
            "Sicura. La rimozione cancella file di asset e non è annullabile, quindi il pulsante resta disattivato finché questa non è attiva.")
        .Option(nameof(BridgeSetting.RemoveSelected), "Rimuovi le esportazioni selezionate",
            "Cancella gli asset esportati. Le strade già posate in una città si romperanno.")
        .Option(nameof(BridgeSetting.OverwriteExisting), "Sovrascrivi le esportazioni esistenti",
            "Esporta di nuovo una strada anche se il suo asset esiste già.")
        .Option(nameof(BridgeSetting.AllowGameplayExport), "Consenti l'esportazione fuori dall'Editor",
            "Disattivo per impostazione predefinita: scrivere asset utente da un salvataggio città è più rischioso che farlo nell'Editor.")
        .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "Rimuovi anche le dipendenze inutilizzate",
            "Dopo una rimozione, cancella sezioni e pezzi esportati che nessuna strada rimasta referenzia.")
        .Option(nameof(BridgeSetting.EmbedIcons), "Incorpora le miniature negli asset",
            "Rende la strada esportata autonoma: la miniatura funziona anche se condivisa o senza questa mod. Costa circa 20-70 KB per strada. Disattivo, le miniature provengono dalla cartella di questa mod e funzionano solo sul tuo computer.");

    internal static UiStrings Polish() => new UiStrings
    {
        Title = "Eksporter prefabrykatów dróg",
        TabRoads = "Drogi",
        TabOptions = "Opcje",
        GroupStatus = "Stan",
        GroupSelection = "Zaznaczenie",
        GroupActions = "Akcje",
        GroupRoads = "Lista dróg",
        DetailSummary = "Szerokość ~{0} m - limit {1}",
        DetailLastExport = "Ostatni eksport: {0}",
        GroupExport = "Eksport",
        GroupMaintenance = "Konserwacja",

        StatusNotExported = "niewyeksportowana",
        StatusExported = "wyeksportowana",
        StatusOutdated = "zmieniona od ostatniego eksportu",
        StatusExportedPendingRestart = "właśnie wyeksportowana",
        StatusRemovedPendingRestart = "właśnie usunięta - wymagany restart",

        StateNoWorld = "Nie wczytano świata. Otwórz Edytor, aby wyświetlić drogi Road Buildera.",
        StateGameplayBlocked = "Eksport poza Edytorem jest wyłączony. Otwórz Edytor albo włącz „Zezwól na eksport poza Edytorem”.",
        StateScanning = "Czekam, aż Road Builder wygeneruje swoje drogi...",
        StateNoRoads = "Nie znaleziono dróg Road Buildera. Sprawdź, czy Road Builder jest włączony w tym playsecie.",
        StateBrokenRoads = "Pominięto {0} drog(i): Road Builder nie mógł ich wygenerować (brak konfiguracji).",
        StateNameConflicts = "Pominięto {0} drog(i) z powodu konfliktu nazw. Zmień ich nazwy w Road Builderze.",
        StatePageIndicator = "Strona {0} z {1} - pokazano {2}-{3} z {4}.",
        StateReady = "Dróg: {0} - wyeksportowane: {1}, niewyeksportowane: {2}, zmienione: {3}.",
        StateSelected = "Zaznaczono: {0}.",
        StateRestartHint = "Wyeksportowane drogi są rejestrowane od razu; restart nie jest potrzebny.",
        StateReportHint = "Pełny raport: ModsData\\RoadPrefabExporter\\last-export-report.txt",
        OperationSummary = "Ostatnie uruchomienie: wyeksportowano {0}, usunięto {1}, pominięto {2}, błędów {3}.",
        NothingSelected = "Nie ma co robić: nie zaznaczono żadnej drogi.",
    }
        .Option(nameof(BridgeSetting.StatusText), "Bieżący stan",
            "Drogi pojawiają się tutaj, gdy wczytany jest świat zawierający drogi Road Buildera.")
        .Option(nameof(BridgeSetting.RescanRoads), "Skanuj ponownie",
            "Ponownie odczytuje listę dróg i stan eksportu.")
        .Option(nameof(BridgeSetting.ExportSelected), "Eksportuj zaznaczone drogi",
            "Zamienia każdą zaznaczoną drogę w natywny zasób RoadPrefab. Przed użyciem wyników uruchom grę ponownie.")
        .Option(nameof(BridgeSetting.ArmRemoval), "Zezwól na usuwanie",
            "Zabezpieczenie. Usuwanie kasuje pliki zasobów i jest nieodwracalne, więc przycisk pozostaje nieaktywny, dopóki to nie zostanie włączone.")
        .Option(nameof(BridgeSetting.RemoveSelected), "Usuń eksporty zaznaczonych dróg",
            "Kasuje wyeksportowane zasoby. Drogi już postawione w mieście przestaną działać.")
        .Option(nameof(BridgeSetting.OverwriteExisting), "Nadpisuj istniejące eksporty",
            "Eksportuje drogę ponownie, nawet jeśli jej zasób już istnieje.")
        .Option(nameof(BridgeSetting.AllowGameplayExport), "Zezwól na eksport poza Edytorem",
            "Domyślnie wyłączone: zapisywanie zasobów użytkownika z zapisu miasta jest bardziej ryzykowne niż w Edytorze.")
        .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "Usuwaj też nieużywane zależności",
            "Po usunięciu kasuje wyeksportowane sekcje i elementy, do których nie odwołuje się już żadna pozostała droga.")
        .Option(nameof(BridgeSetting.EmbedIcons), "Osadzaj miniatury w zasobach",
            "Sprawia, że wyeksportowana droga jest samowystarczalna, więc jej miniatura działa też po udostępnieniu lub bez tego moda. Kosztuje około 20-70 KB na drogę. Wyłączone: miniatury pochodzą z folderu tego moda i działają tylko na twoim komputerze.");

    internal static UiStrings Portuguese() => new UiStrings
    {
        Title = "Exportador de prefabs de estrada",
        TabRoads = "Estradas",
        TabOptions = "Opções",
        GroupStatus = "Estado",
        GroupSelection = "Seleção",
        GroupActions = "Ações",
        GroupRoads = "Lista de estradas",
        DetailSummary = "Largura ~{0} m - limite {1}",
        DetailLastExport = "Última exportação: {0}",
        GroupExport = "Exportação",
        GroupMaintenance = "Manutenção",

        StatusNotExported = "não exportada",
        StatusExported = "exportada",
        StatusOutdated = "alterada desde a última exportação",
        StatusExportedPendingRestart = "exportada agora",
        StatusRemovedPendingRestart = "removida agora - é preciso reiniciar",

        StateNoWorld = "Nenhum mundo carregado. Abra o Editor para listar as estradas do Road Builder.",
        StateGameplayBlocked = "A exportação fora do Editor está desligada. Abra o Editor ou ative \"Permitir exportar fora do Editor\".",
        StateScanning = "Aguardando o Road Builder terminar de gerar as estradas...",
        StateNoRoads = "Nenhuma estrada do Road Builder encontrada. Verifique se o Road Builder está ativo neste playset.",
        StateBrokenRoads = "{0} estrada(s) ignorada(s): o Road Builder não conseguiu gerá-la(s) (configuração em falta).",
        StateNameConflicts = "{0} estrada(s) ignorada(s) por conflito de nome. Renomeie-a(s) no Road Builder.",
        StatePageIndicator = "Página {0} de {1} - a mostrar {2}-{3} de {4}.",
        StateReady = "{0} estradas: {1} exportadas, {2} não exportadas, {3} alteradas desde a exportação.",
        StateSelected = "{0} marcadas.",
        StateRestartHint = "As estradas exportadas são registadas de imediato; não é preciso reiniciar.",
        StateReportHint = "Relatório completo: ModsData\\RoadPrefabExporter\\last-export-report.txt",
        OperationSummary = "Última execução: {0} exportadas, {1} removidas, {2} ignoradas, {3} com falha.",
        NothingSelected = "Nada a fazer: nenhuma estrada marcada.",
    }
        .Option(nameof(BridgeSetting.StatusText), "Estado atual",
            "As estradas aparecem aqui quando há um mundo com estradas do Road Builder carregado.")
        .Option(nameof(BridgeSetting.RescanRoads), "Reanalisar estradas",
            "Lê novamente a lista de estradas e o estado de exportação.")
        .Option(nameof(BridgeSetting.ExportSelected), "Exportar estradas marcadas",
            "Converte cada estrada marcada num recurso RoadPrefab nativo. Reinicie o jogo antes de usar os resultados.")
        .Option(nameof(BridgeSetting.ArmRemoval), "Permitir remoção",
            "Trava de segurança. A remoção apaga ficheiros de recursos e não pode ser desfeita, por isso o botão fica desativado até ligar isto.")
        .Option(nameof(BridgeSetting.RemoveSelected), "Remover exportações marcadas",
            "Apaga os recursos exportados. Estradas já colocadas numa cidade vão quebrar.")
        .Option(nameof(BridgeSetting.OverwriteExisting), "Substituir exportações existentes",
            "Exporta a estrada de novo mesmo que o recurso já exista.")
        .Option(nameof(BridgeSetting.AllowGameplayExport), "Permitir exportar fora do Editor",
            "Desligado por padrão: gravar recursos de utilizador a partir de um save de cidade é mais arriscado do que no Editor.")
        .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "Remover também dependências não usadas",
            "Após uma remoção, apaga secções e peças exportadas que nenhuma estrada restante referencia.")
        .Option(nameof(BridgeSetting.EmbedIcons), "Embutir miniaturas nos recursos",
            "Torna a estrada exportada autónoma: a miniatura funciona mesmo partilhada ou sem este mod. Custa cerca de 20-70 KB por estrada. Desligado, as miniaturas vêm da pasta deste mod e só funcionam no seu computador.");

    internal static UiStrings Russian() => new UiStrings
    {
        Title = "Экспорт префабов дорог",
        TabRoads = "Дороги",
        TabOptions = "Параметры",
        GroupStatus = "Состояние",
        GroupSelection = "Выбор",
        GroupActions = "Действия",
        GroupRoads = "Список дорог",
        DetailSummary = "Ширина ~{0} м - ограничение {1}",
        DetailLastExport = "Последний экспорт: {0}",
        GroupExport = "Экспорт",
        GroupMaintenance = "Обслуживание",

        StatusNotExported = "не экспортирована",
        StatusExported = "экспортирована",
        StatusOutdated = "изменена после последнего экспорта",
        StatusExportedPendingRestart = "только что экспортирована",
        StatusRemovedPendingRestart = "только что удалена - нужен перезапуск",

        StateNoWorld = "Мир не загружен. Откройте редактор, чтобы увидеть дороги Road Builder.",
        StateGameplayBlocked = "Экспорт вне редактора выключен. Откройте редактор или включите «Разрешить экспорт вне редактора».",
        StateScanning = "Ожидание, пока Road Builder создаст свои дороги...",
        StateNoRoads = "Дороги Road Builder не найдены. Проверьте, включён ли Road Builder в этом наборе модов.",
        StateBrokenRoads = "Пропущено дорог: {0}. Road Builder не смог их создать (отсутствует конфигурация).",
        StateNameConflicts = "Пропущено дорог из-за конфликта имён: {0}. Переименуйте их в Road Builder.",
        StatePageIndicator = "Страница {0} из {1} - показаны {2}-{3} из {4}.",
        StateReady = "Дорог: {0} - экспортировано {1}, не экспортировано {2}, изменено {3}.",
        StateSelected = "Отмечено: {0}.",
        StateRestartHint = "Экспортированные дороги регистрируются сразу; перезапуск не нужен.",
        StateReportHint = "Полный отчёт: ModsData\\RoadPrefabExporter\\last-export-report.txt",
        OperationSummary = "Последний запуск: экспортировано {0}, удалено {1}, пропущено {2}, ошибок {3}.",
        NothingSelected = "Нечего делать: не отмечена ни одна дорога.",
    }
        .Option(nameof(BridgeSetting.StatusText), "Текущее состояние",
            "Дороги появляются здесь, когда загружен мир с дорогами Road Builder.")
        .Option(nameof(BridgeSetting.RescanRoads), "Пересканировать",
            "Заново читает список дорог и состояние экспорта.")
        .Option(nameof(BridgeSetting.ExportSelected), "Экспортировать отмеченные дороги",
            "Превращает каждую отмеченную дорогу в родной ассет RoadPrefab. Перед использованием перезапустите игру.")
        .Option(nameof(BridgeSetting.ArmRemoval), "Разрешить удаление",
            "Предохранитель. Удаление стирает файлы ассетов без возможности отмены, поэтому кнопка неактивна, пока это выключено.")
        .Option(nameof(BridgeSetting.RemoveSelected), "Удалить экспорт отмеченных дорог",
            "Стирает экспортированные ассеты. Дороги, уже построенные в городе, сломаются.")
        .Option(nameof(BridgeSetting.OverwriteExisting), "Перезаписывать существующий экспорт",
            "Экспортировать дорогу заново, даже если её ассет уже есть.")
        .Option(nameof(BridgeSetting.AllowGameplayExport), "Разрешить экспорт вне редактора",
            "По умолчанию выключено: запись пользовательских ассетов из сохранения города рискованнее, чем в редакторе.")
        .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "Удалять и неиспользуемые зависимости",
            "После удаления стирает экспортированные секции и детали, на которые не ссылается ни одна оставшаяся дорога.")
        .Option(nameof(BridgeSetting.EmbedIcons), "Встраивать миниатюры в ассеты",
            "Делает экспортированную дорогу самодостаточной: миниатюра работает и при передаче другим, и без этого мода. Стоит примерно 20-70 КБ на дорогу. Если выключено, миниатюры берутся из папки этого мода и работают только на вашем компьютере.");
}
