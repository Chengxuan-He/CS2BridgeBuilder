using CS2Mods.Shared.Discovery;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System;
using System.Globalization;

namespace CS2Mods.Shared.Conversion;

/// <summary>
/// Makes an exported road carry the speed limit the player set in Road Builder.
///
/// The clone copies <see cref="RoadPrefab.m_SpeedLimit"/> from the generated prefab, which is correct
/// only if Road Builder wrote the configured value there. This mod reaches Road Builder purely
/// through reflection and cannot depend on that: a generated prefab that kept its template's speed
/// would produce an export that looks right and drives wrong, and it would be wrong identically in
/// both exporters, since they share this conversion.
///
/// So the configuration wins, and any disagreement is written to the report - which also makes the
/// first export after this change say plainly whether the problem was ever there.
/// </summary>
internal static class SpeedLimitFix
{
    /// <summary>Below this a difference is float noise rather than a different setting.</summary>
    private const float Tolerance = 0.01f;

    internal static void Apply(RoadPrefab clone, RoadBuilderRoad road, ExportReport report)
    {
        var configured = road.ConfiguredSpeedLimit;
        if (configured <= 0f)
        {
            report.Note($"{clone.name}: no speed limit in the configuration; keeping the prefab's {Format(clone.m_SpeedLimit)}.");
            return;
        }

        var generated = clone.m_SpeedLimit;
        if (Math.Abs(generated - configured) <= Tolerance)
        {
            report.Note($"{clone.name}: speed limit {Format(configured)} (configuration and prefab agree).");
            return;
        }

        clone.m_SpeedLimit = configured;
        report.Warning(
            $"'{clone.name}': the generated prefab said {Format(generated)} but Road Builder is "
            + $"configured for {Format(configured)}. Exported with {Format(configured)}.");
    }

    private static string Format(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
}
