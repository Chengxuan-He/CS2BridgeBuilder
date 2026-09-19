using System;
using System.Linq;
using BridgeBuilder.Settings;
using BridgeBuilder.Runtime;

// Tests only localization/state. No game binaries, geometry or persistent assets.
internal static class Program
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Main()
    {
        var freshPrefabName = BridgeRegistration.NewPrefabName();
        Check(freshPrefabName.StartsWith("b", StringComparison.Ordinal)
            && Guid.TryParseExact(freshPrefabName.Substring(1), "D", out _), "New prefab must be b{uuid}");
        Check(BridgeRegistration.IsPrefabName(freshPrefabName), "New bridge identity rejected");
        Check(!BridgeRegistration.IsPrefabName("r11111111-2222-3333-4444-555555555555"),
            "Legacy r-prefixed bridge compatibility must not return");
        Check(!BridgeRegistration.IsPrefabName("r11111111-2222-3333-4444-555555555555-76561199197854251"),
            "Road Builder road identity is not a BridgeBuilder bridge");
        Check(!BridgeRegistration.IsPrefabName("bnot-a-uuid"), "Malformed bridge identity accepted");
        Check(!BridgeRegistration.IsPrefabName("tmp11111111-2222-3333-4444-555555555555"),
            "Temporary previews must not enter the permanent registry");
        Check(BridgeBuilder.Bridges.BridgeStyleDefinitions.AcceptsSource("Extradosed01", true),
            "V-pylon double deck must accept its base-game source");
        Check(!BridgeBuilder.Bridges.BridgeStyleDefinitions.AcceptsSource("Extradosed01", false),
            "V-pylon double deck must not accept a mod/DLC duplicate");
        Check(BridgeBuilder.Bridges.BridgeStyleDefinitions.AcceptsSource("SuspensionDouble", false),
            "Mod-only blue suspension source must remain supported");
        Check(BridgeBuilder.Bridges.BridgeStyleDefinitions.GenerationUnsupportedReason("GoldenGate") == null,
            "Golden Gate single deck must remain supported");
        Check(BridgeBuilder.Bridges.BridgeStyleDefinitions.GenerationUnsupportedReason("GoldenGate", true) != null,
            "Golden Gate double deck must be rejected");
        Check(BridgeBuilder.Bridges.BridgeStyleDefinitions.CanGenerate("GoldenGateDouble"),
            "BXP double Golden Gate must be admitted to the catalogue");
        Check(BridgeBuilder.Bridges.BridgeStyleDefinitions.GenerationUnsupportedReason("GoldenGateDouble", true) == null,
            "BXP double Golden Gate must support two decks");
        Check(!BridgeBuilder.Bridges.BridgeStyleDefinitions.SupportsDeckMode("GoldenGateDouble", false),
            "BXP double archetype must not be used for a single deck");
        foreach (var donor in new[] { "BXP Golden Gate Bridge Train", "BXP Golden Gate Bridge Subway" })
            Check(BridgeBuilder.Bridges.BridgeStyleDefinitions.Match(donor)?.Id == "GoldenGateDouble",
                "BXP donor routed to the single-deck landmark");
        Check(BridgeBuilder.Bridges.BridgeStyleDefinitions.Match("Golden Gate Bridge")?.Id == "GoldenGate",
            "DLC landmark identity must remain unchanged");
        Check(RuntimeUiText.ForLocale("zh-HANS")["DisplayName"] == "桥梁名称", "Bridge name placeholder");
        foreach (var style in new[] { "SuspensionDouble", "Suspension02", "Extradosed01", "Extradosed02" })
            Check(BridgeBuilder.Bridges.BridgeStyleDefinitions.SupportsDeckMode(style, true),
                "Golden Gate restriction must not affect " + style);
        var aliases = new (string? Input, string Expected)[]
        {
            (null, "en-US"), ("", "en-US"), ("unregistered", "en-US"),
            ("de-de", "de-DE"), ("fr_CA", "fr-FR"), ("EN-gb", "en-US"),
            ("pt_PT", "pt-BR"), ("zh_CN", "zh-HANS"), ("zh_TW", "zh-HANT"),
            ("zh-HK", "zh-HANT"), ("zh-Hans-TW", "zh-HANS"), ("zh-Hant-CN", "zh-HANT")
        };
        foreach (var (input, expected) in aliases)
        {
            Check(UiStringCatalog.Resolve(input) == expected, "Locale alias: " + input);
            Check(UiStringCatalog.ForLocale(input).LocaleId == expected, "Wrong builder: " + input);
            Check(RuntimeUiText.ForLocale(input)["CreateTab"] == RuntimeUiText.ForLocale(expected)["CreateTab"], "Runtime fallback");
        }

        var name = "My bridge 我的桥 {0}";
        var uuid = "b11111111-2222-3333-4444-555555555555";
        BridgeRuntimeRequests.Complete("Renamed", name, uuid);
        BridgePreviewState.Select("Train", "", "style");
        BridgePreviewState.Publish(BridgePreviewState.Revision, "image-bytes", "PreviewReady");
        var requestRevision = BridgeRuntimeRequests.Revision;
        var previewRevision = BridgePreviewState.Revision;
        var resultRevision = BridgePreviewState.ResultRevision;
        var englishKeys = RuntimeUiText.ForLocale("en-US").Keys.OrderBy(x => x).ToArray();
        foreach (var locale in UiStringCatalog.LocaleIds)
        {
            Game.SceneFlow.GameManager.instance.localizationManager.activeLocaleId = locale;
            var text = UiStringCatalog.Current;
            Check(text.LocaleId == locale, "Active locale not followed");
            Check(RuntimeUiText.ForLocale(locale).Keys.OrderBy(x => x).SequenceEqual(englishKeys), "Incomplete table");
            foreach (var entry in RuntimeUiText.ForLocale(locale))
            {
                Check(!string.IsNullOrWhiteSpace(entry.Value), "Blank translation");
                _ = RuntimeUiText.Format(locale, entry.Key, name, uuid);
            }
            foreach (var kind in new[] { "Road", "Pedestrian", "Train", "Subway", "Tram" })
                Check(!string.IsNullOrEmpty(text.DeckKindName(kind)), "Missing network type");
            Check(text.StyleName("GoldenGate") != "GoldenGate", "Untranslated style");
            Check(text.StyleName("GoldenGateDouble") != "GoldenGateDouble", "Untranslated BXP double style");
            Check(BridgeRuntimeRequests.Status == RuntimeUiText.Get("Renamed", name, uuid), "Status did not change language");
            Check(BridgeRuntimeRequests.Status.Contains(name) && !BridgeRuntimeRequests.Status.Contains(uuid),
                "UI rename status must show the name without exposing UUID");
            Check(BridgePreviewState.Status == RuntimeUiText.Get("PreviewReady"), "Preview status did not change language");
            Check(BridgePreviewState.Image == "image-bytes", "Language switch discarded image");
            Check(requestRevision == BridgeRuntimeRequests.Revision && previewRevision == BridgePreviewState.Revision &&
                resultRevision == BridgePreviewState.ResultRevision, "Language switch enqueued bridge work");
            Console.WriteLine("PASS " + locale + ": actual C# tables, formatting, live status/preview translation, identity preserved");
        }
        BridgePreviewState.Publish(previewRevision - 1, "stale", "PreviewFailed");
        Check(BridgePreviewState.Image == "image-bytes", "Stale preview replaced current result");
        BridgePreviewState.Clear();
        Check(BridgePreviewState.Status == "" && BridgePreviewState.Image == "", "Preview clear failed");
        BridgePreviewState.Select("Road", "Train", "SuspensionDouble", true);
        var oppositeKey = BridgePreviewState.Key;
        var oppositeRevision = BridgePreviewState.Revision;
        BridgePreviewState.Select("Road", "Train", "SuspensionDouble", false);
        Check(BridgePreviewState.Key != oppositeKey && !BridgePreviewState.Selection!.LowerDeckOpposite,
            "Direction must be part of the preview recipe/key");
        BridgePreviewState.Publish(oppositeRevision, "old-opposite-preview", "PreviewReady");
        Check(BridgePreviewState.Image == "", "Opposite-direction stale image was accepted");
        BridgePreviewState.Clear();
        Check(!new BridgeRuntimeRequest().BuildAfterCreate, "Default creation must not activate placement");
        foreach (var buildAfterCreate in new[] { false, true })
        {
            BridgeRuntimeRequests.Enqueue(new BridgeRuntimeRequest
            {
                Action = BridgeRuntimeAction.Create,
                BuildAfterCreate = buildAfterCreate,
                RegistrationName = name,
                UpperDeckId = "Train",
            }, "Creating");
            Check(BridgeRuntimeRequests.TryTake(out var request) && request != null &&
                request.Action == BridgeRuntimeAction.Create && request.BuildAfterCreate == buildAfterCreate &&
                request.RegistrationName == name && request.UpperDeckId == "Train", "Creation intent changed in queue");
        }
        Check(!BridgeRuntimeRequests.TryTake(out _), "Creation queue retained a duplicate");
        foreach (var draft in new[] { "B", "Br", "Bridge" })
            BridgeRuntimeRequests.Enqueue(new BridgeRuntimeRequest
            {
                Action = BridgeRuntimeAction.Rename, PrefabName = uuid, RegistrationName = draft
            }, "Updating");
        Check(BridgeRuntimeRequests.TryTake(out var renamed) && renamed?.RegistrationName == "Bridge",
            "Typing must persist the newest name");
        Check(!BridgeRuntimeRequests.TryTake(out _), "Typing queued redundant writes");
        BridgeRuntimeRequests.Enqueue(new BridgeRuntimeRequest
        {
            Action = BridgeRuntimeAction.Rename, PrefabName = uuid, RegistrationName = "Before"
        }, "Updating");
        BridgeRuntimeRequests.Enqueue(new BridgeRuntimeRequest { Action = BridgeRuntimeAction.Activate, PrefabName = uuid }, "Updating");
        BridgeRuntimeRequests.Enqueue(new BridgeRuntimeRequest
        {
            Action = BridgeRuntimeAction.Rename, PrefabName = uuid, RegistrationName = "After"
        }, "Updating");
        Check(BridgeRuntimeRequests.TryTake(out var before) && before?.RegistrationName == "Before", "Rename crossed action barrier");
        Check(BridgeRuntimeRequests.TryTake(out var activate) && activate?.Action == BridgeRuntimeAction.Activate, "Build order changed");
        Check(BridgeRuntimeRequests.TryTake(out var after) && after?.RegistrationName == "After", "Latest rename lost");
        Console.WriteLine("PASS aliases, English fallback, stale-preview rejection and cleanup; no game was started.");
    }
}

// Narrow adapters for the game-owned locale accessor and nameof-only settings.
namespace Colossal.Localization
{
    internal sealed class LocalizationManager { internal string activeLocaleId = "en-US"; }
}
namespace Game.SceneFlow
{
    internal sealed class GameManager
    {
        internal static readonly GameManager instance = new();
        internal readonly Colossal.Localization.LocalizationManager localizationManager = new();
    }
}
namespace BridgeBuilder.Settings
{
    internal sealed class BridgeSetting
    {
        public string AllowGameplayExport = "", ArmRemoval = "", BridgeName = "", BridgeStyleId = "",
            BuildStyleOverride = "", DeckSpacing = "", EmbedIcons = "", ExportSelected = "", LowerDeckId = "",
            LowerDeckOpposite = "", OverwriteExisting = "", RemoveSelected = "", RemoveUnusedDependencies = "",
            RescanRoads = "", StatusText = "", UpperDeckId = "";
    }
}
