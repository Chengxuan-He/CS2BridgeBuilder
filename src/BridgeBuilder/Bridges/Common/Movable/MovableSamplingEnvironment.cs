using System;
using Game.City;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

// Inputs for synchronous native network calculations in a private world. A flat
// height field prevents the current city's terrain from deforming an asset recipe.
// Theme and traffic rules still come from the same native configuration as LaneSystem.
internal sealed class MovableSamplingEnvironment : IDisposable
{
    internal readonly Entity Theme;
    internal readonly bool LeftHandTraffic;
    internal readonly BuildingConfigurationData BuildingConfiguration;
    internal readonly TerrainHeightData Terrain;
    private NativeArray<ushort> _heights, _downscaled;

    internal MovableSamplingEnvironment(World source)
    {
        if (source == null || !source.IsCreated)
            throw new InvalidOperationException("The native world is unavailable for road sampling.");
        var city = source.GetExistingSystemManaged<CityConfigurationSystem>()
            ?? throw new InvalidOperationException("Native city configuration is unavailable for road sampling.");
        source.EntityManager.CompleteAllTrackedJobs();
        using var query = source.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
        BuildingConfiguration = query.GetSingleton<BuildingConfigurationData>();
        Theme = city.defaultTheme;
        LeftHandTraffic = city.leftHandTraffic;
        // Native TerrainUtils clamps samples to the field boundary. Every sample
        // remains zero, including outside this small field. Both levels have at
        // least a 2x2 grid so native interpolation retains valid neighbors.
        var side = TerrainSystem.kDownScaledHeightmapScale * 2;
        try
        {
            _heights = new NativeArray<ushort>(side * side, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _downscaled = new NativeArray<ushort>(4, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            Terrain = new TerrainHeightData(_heights, _downscaled, new int3(side, 65536, side),
                new float3(1f), float3.zero, false);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_downscaled.IsCreated) _downscaled.Dispose();
        if (_heights.IsCreated) _heights.Dispose();
    }
}
