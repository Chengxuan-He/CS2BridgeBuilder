using Colossal.Localization;
using Game.SceneFlow;
using System;
using System.Collections.Generic;

namespace BridgeBuilder.Settings;

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
            built.LocaleId = key;
            UiStringTables.WithSettingsText(built, key);
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

    [System.Runtime.InteropServices.DllImport("kernel32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode,
        ExactSpelling=true,SetLastError=true)]
    private static extern int CompareStringEx(string locale,uint flags,string left,int leftLength,
        string right,int rightLength,IntPtr version,IntPtr reserved,IntPtr parameter);

    internal static IComparer<string> NameOrder(string? locale)
    {
        var name=string.IsNullOrWhiteSpace(locale) ? "en-US" : locale!.Replace('_','-');
        System.Globalization.CultureInfo culture;
        try { culture=System.Globalization.CultureInfo.GetCultureInfo(name); }
        catch(System.Globalization.CultureNotFoundException) { culture=System.Globalization.CultureInfo.GetCultureInfo(Resolve(name)); }
        var fallback=StringComparer.Create(culture,true);
        if(Environment.OSVersion.Platform!=PlatformID.Win32NT) return fallback;
        // Game Mono orders Han characters by code point even for zh-CN. Use the native locale tables.
        return Comparer<string>.Create((left,right)=>{
            if(string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right)) return string.CompareOrdinal(left,right);
            int result=CompareStringEx(culture.Name,1,left,left.Length,right,right.Length,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);
            return result==0 ? fallback.Compare(left,right) : result-2;
        });
    }

    internal static string Resolve(string? localeId)
    {
        if (string.IsNullOrEmpty(localeId)) return "en-US";
        localeId = localeId!.Replace('_', '-');
        // Return canonical casing: the translation builders use exact switch labels.
        foreach (var candidate in Builders.Keys)
            if (string.Equals(candidate, localeId, StringComparison.OrdinalIgnoreCase)) return candidate;

        if (localeId.StartsWith("zh-", StringComparison.OrdinalIgnoreCase))
        {
            var parts = localeId.Split('-');
            // Explicit script wins over the region (e.g. zh-Hans-TW).
            foreach (var part in parts)
            {
                if (part.Equals("Hans", StringComparison.OrdinalIgnoreCase)) return "zh-HANS";
                if (part.Equals("Hant", StringComparison.OrdinalIgnoreCase)) return "zh-HANT";
            }
            foreach (var part in parts)
                if (part.Equals("TW", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("HK", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("MO", StringComparison.OrdinalIgnoreCase)) return "zh-HANT";
            return "zh-HANS";
        }

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
