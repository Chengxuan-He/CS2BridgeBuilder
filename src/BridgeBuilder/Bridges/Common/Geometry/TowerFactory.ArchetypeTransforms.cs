using Colossal.AssetPipeline;
using BridgeBuilder.Runtime;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using CS2Mods.Shared;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

internal sealed partial class TowerFactory
{
    private bool IsThroughArchSection(bool section) => section
        && BridgeStyleDefinitions.UsesOpenTrussTopology(_styleId);

    /// <summary>
    /// Identifies the base directly from the TrussArch01 archetype, including its separately authored
    /// LOD prefabs. Its vertices take the contract's rigid side mapping instead of generic component
    /// classification; no generated width or road boundary is used to infer the base.
    /// </summary>
    private bool IsBluePrototypeBase(RenderPrefab original) =>
        _styleId == "TrussArch01"
        && _towerKey == "TrussArchBridge01NetPillar"
        && (string.Equals(
                original.name, "TrussArchBridge01NetPillarBase Mesh", StringComparison.Ordinal)
            || string.Equals(
                original.name, "TrussArchBridge01NetPillarBase_LOD1 Mesh", StringComparison.Ordinal)
            || string.Equals(
                original.name, "TrussArchBridge01NetPillarBase_LOD2 Mesh", StringComparison.Ordinal));

    /// <summary>
    /// Identifies the TrussArch01 portal body and its LODs. This must not include the separately
    /// authored base: the base uses the contract's exact sign translation and a delta which preserves
    /// the TrussArchBridge01 prototype's base-minus-arch width difference.
    /// </summary>
    private bool IsBluePrototypeMainPier(RenderPrefab original) =>
        _styleId == "TrussArch01"
        && _towerKey == "TrussArchBridge01NetPillar"
        && (string.Equals(
                original.name, "TrussArchBridge01NetPillar Mesh", StringComparison.Ordinal)
            || string.Equals(
                original.name, "TrussArchBridge01NetPillar_LOD1 Mesh", StringComparison.Ordinal)
            || string.Equals(
                original.name, "TrussArchBridge01NetPillar_LOD2 Mesh", StringComparison.Ordinal));

    /// <summary>
    /// Identifies the three shipped TrussArchBridge01 section meshes. The names select immutable
    /// metaprogram output only; no runtime coordinate threshold or topology guess is involved.
    /// </summary>
    private bool IsBluePrototypeSection(RenderPrefab original) =>
        _styleId == "TrussArch01"
        && (string.Equals(
                original.name, "TrussArchBridge01Net Mesh", StringComparison.Ordinal)
            || string.Equals(
                original.name, "TrussArchBridge01Net_LOD1 Mesh", StringComparison.Ordinal)
            || string.Equals(
                original.name, "TrussArchBridge01Net_LOD2 Mesh", StringComparison.Ordinal));

    private float3[] WidenBluePrototypeSection(
        RenderPrefab original, float3[] source, float extra, string sectionName)
    {
        if (TrussArch01Geometry.TryWidenSection(original.name, source, extra, out var moved))
            return moved;

        _report.Defect(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: unsupported TrussArch01 section mesh '{1}' with {2} vertices; no geometry "
            + "fallback was used, so the prototype coordinates were kept unchanged.",
            sectionName, original.name, source.Length));
        return moved;
    }

    private float3[] WidenBluePrototypePier(
        RenderPrefab original, float3[] source, float extra, string towerName)
    {
        if (TrussArch01Geometry.TryWidenPier(original.name, source, extra, out var moved)) return moved;

        _report.Defect(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: unsupported TrussArch01 pier mesh '{1}' with {2} vertices; no geometry fallback "
            + "was used, so the prototype coordinates were kept unchanged.",
            towerName, original.name, source.Length));
        return moved;
    }

}
