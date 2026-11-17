using BridgeBuilder.Runtime;
using BridgeBuilder.Settings;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

// Map-only native aggregate names are independent of generated asset display names.
internal static class BridgeProperName
{
    internal static bool Applies(string? style) => style is "Nanpu" or "Lupu" or "Grand" or "GoldenGate" or "GoldenGateDouble";
    internal static string Label(string style, string? locale = null) =>
        (locale == null ? UiStringCatalog.Current : UiStringCatalog.ForLocale(locale)).StyleName(style);
    internal static string Key(string owner) => "Assets.NAME[" + owner + "_NamedAggregate]";

    internal static void CreateAggregate(NetGeometryPrefab target, TowerFactory factory)
    {
        var source = factory.ResolveOriginal<AggregateNetPrefab>("Bridge");
        var aggregate = (AggregateNetPrefab)source.Clone(target.name + " NamedAggregate");
        aggregate.components.RemoveAll(component => component is Localization);
        aggregate.AddComponent<Localization>().m_LocalizationID = Key(target.name);
        factory.AdoptMovableSections(new PrefabBase[] { aggregate });
        target.m_AggregateType = aggregate;
    }
}
