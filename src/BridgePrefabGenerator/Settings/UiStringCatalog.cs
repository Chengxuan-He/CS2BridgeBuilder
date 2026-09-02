using Colossal.Localization;
using Game.SceneFlow;
using System;
using System.Collections.Generic;

namespace BridgePrefabGenerator.Settings;

/// <summary>
/// Resolves a locale id to its <see cref="UiStrings"/>: exact match first, then the language part
/// alone, then English. Community translation mods add locales the game does not ship, so an
/// unknown id has to degrade to readable English rather than to raw locale ids.
/// </summary>
internal static class UiStringCatalog
{
    private static readonly Dictionary<string, Func<UiStrings>> Builders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "en-US", UiStringTables.English },
            { "de-DE", UiStringTables.German },
            { "es-ES", UiStringTables.Spanish },
            { "fr-FR", UiStringTables.French },
            { "it-IT", UiStringTables.Italian },
            { "ja-JP", UiStringTables.Japanese },
            { "ko-KR", UiStringTables.Korean },
            { "pl-PL", UiStringTables.Polish },
            { "pt-BR", UiStringTables.Portuguese },
            { "ru-RU", UiStringTables.Russian },
            { "zh-HANS", UiStringTables.SimplifiedChinese },
            { "zh-HANT", UiStringTables.TraditionalChinese },
        };

    private static readonly Dictionary<string, UiStrings> Cache =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The locales this mod ships translations for.</summary>
    internal static IEnumerable<string> LocaleIds => Builders.Keys;

    internal static UiStrings ForLocale(string? localeId)
    {
        var key = Resolve(localeId);
        lock (Cache)
        {
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var built = UiStringTables.WithStyleNames(UiStringTables.WithBridgeText(Builders[key](), key), key);
            Cache[key] = built;
            return built;
        }
    }

    internal static UiStrings Current
    {
        get
        {
            try
            {
                LocalizationManager? manager = GameManager.instance?.localizationManager;
                return ForLocale(manager?.activeLocaleId);
            }
            catch (Exception)
            {
                return ForLocale("en-US");
            }
        }
    }

    private static string Resolve(string? localeId)
    {
        if (string.IsNullOrEmpty(localeId)) return "en-US";
        if (Builders.ContainsKey(localeId!)) return localeId!;

        var separator = localeId!.IndexOf('-');
        var language = separator > 0 ? localeId.Substring(0, separator) : localeId;
        foreach (var candidate in Builders.Keys)
        {
            var candidateSeparator = candidate.IndexOf('-');
            var candidateLanguage = candidateSeparator > 0 ? candidate.Substring(0, candidateSeparator) : candidate;
            if (string.Equals(candidateLanguage, language, StringComparison.OrdinalIgnoreCase)) return candidate;
        }

        return "en-US";
    }
}
