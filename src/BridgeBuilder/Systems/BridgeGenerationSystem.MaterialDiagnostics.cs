using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace BridgeBuilder.Systems;

public partial class BridgeGenerationSystem
{
    private float _nextMaterialInspection;
    private readonly Dictionary<int, string> _placedMaterialBindings = new();
    private HDAdditionalCameraData? _roadDiagnosticCamera;
    private float _nextRoadBufferCapture;
    private int _roadBufferCaptures;

    private static void RequestRoadBuffers(ref HDAdditionalCameraData.BufferAccess access) =>
        access.RequestAccess(HDAdditionalCameraData.BufferAccessType.Normal |
            HDAdditionalCameraData.BufferAccessType.Color);

    private void ReleaseRoadBufferDiagnostics()
    {
        RenderPipelineManager.endCameraRendering -= CaptureRoadBuffers;
        if (_roadDiagnosticCamera != null)
            _roadDiagnosticCamera.requestGraphicsBuffer -= RequestRoadBuffers;
        _roadDiagnosticCamera = null;
    }

    // Read the native camera outputs without changing materials or generating objects.
    // Binding logs alone cannot distinguish an albedo error from incorrect lighting.
    private void CaptureRoadBuffers(ScriptableRenderContext context, Camera camera)
    {
        if (_roadDiagnosticCamera == null || camera != _roadDiagnosticCamera.GetComponent<Camera>() ||
            UnityEngine.Time.realtimeSinceStartup < _nextRoadBufferCapture || _roadBufferCaptures >= 6) return;
        _nextRoadBufferCapture = UnityEngine.Time.realtimeSinceStartup + 30f;
        var capture = ++_roadBufferCaptures;
        var folder = Path.Combine(Application.persistentDataPath, "ModsData", "BridgeBuilder", "RenderDiagnostics");
        Directory.CreateDirectory(folder);
        Save("normal", _roadDiagnosticCamera.GetGraphicsBuffer(HDAdditionalCameraData.BufferAccessType.Normal)?.rt);
        Save("color", _roadDiagnosticCamera.GetGraphicsBuffer(HDAdditionalCameraData.BufferAccessType.Color)?.rt);
        // HDRP may expose these only while the deferred pass is active. Never assume
        // they exist: the retained normal/color buffers above are the public API.
        Save("albedo", Shader.GetGlobalTexture("_GBufferTexture0") as RenderTexture);
        Save("material", Shader.GetGlobalTexture("_GBufferTexture2") as RenderTexture);
        if (_roadBufferCaptures >= 6) ReleaseRoadBufferDiagnostics();

        void Save(string channel, RenderTexture? target)
        {
            if (target == null || !target.IsCreated())
            {
                Mod.Log.Info($"Road render diagnostic: capture={capture}; channel={channel}; unavailable.");
                return;
            }
            var width = target.width; var height = target.height;
            var format = target.graphicsFormat;
            AsyncGPUReadback.Request(target, 0, TextureFormat.RGBA32, request =>
            {
                if (request.hasError)
                {
                    Mod.Log.Warn($"Road render diagnostic readback failed: {channel}.");
                    return;
                }
                var pixels = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                try
                {
                    pixels.LoadRawTextureData(request.GetData<byte>());
                    pixels.Apply(false, false);
                    var path = Path.Combine(folder, $"{capture}-{channel}.png");
                    File.WriteAllBytes(path, pixels.EncodeToPNG());
                    Mod.Log.Info($"Road render diagnostic: {path}; source={format}; {width}x{height}; raw channels, no gamma conversion.");
                }
                finally { UnityEngine.Object.Destroy(pixels); }
            });
        }
    }

    // Inspect the actual native city batch, not SurfaceAsset.Load's separate material.
    // Read-only: never change shared textures, keywords or reference counts.
    private void InspectPlacedRoadMaterials()
    {
        if (UnityEngine.Time.realtimeSinceStartup < _nextMaterialInspection) return;
        _nextMaterialInspection = UnityEngine.Time.realtimeSinceStartup + 15f;
        foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
        {
            if (!material.name.StartsWith("Batch (", StringComparison.Ordinal) ||
                (!material.name.Contains(" Road Surface") && !material.HasProperty("_WorldspaceAlbedo"))) continue;
            if (_roadBufferCaptures < 6 && _roadDiagnosticCamera == null && material.name.Contains(" Road Surface") && Camera.main != null &&
                Camera.main.TryGetComponent<HDAdditionalCameraData>(out var cameraData))
            {
                _roadDiagnosticCamera = cameraData;
                cameraData.requestGraphicsBuffer += RequestRoadBuffers;
                RenderPipelineManager.endCameraRendering += CaptureRoadBuffers;
                _nextRoadBufferCapture = UnityEngine.Time.realtimeSinceStartup + 2f;
            }
            var details = new StringBuilder();
            details.Append($"name={material.name}; shader={material.shader.name}; queue={material.renderQueue}; keywords={string.Join(",", material.shaderKeywords)};");
            foreach (var property in new[] { "_BaseColorMap", "_NormalMap", "_MaskMap", "_ControlMask",
                "_WorldspaceAlbedo", "_WorldspaceNormalMap", "_WorldspaceMaskMap" })
            {
                if (!material.HasProperty(property)) continue;
                var texture = material.GetTexture(property);
                details.Append($" {property}={texture?.name}/{texture?.width}x{texture?.height}/{texture?.graphicsFormat}/filter={texture?.filterMode}/wrap={texture?.wrapMode}/bias={texture?.mipMapBias};");
            }
            foreach (var property in new[] { "_BaseColorMap_ST", "_UV0Scale", "_UV0Offset", "_BaseColor", "_DoubleSidedConstants", "colossal_ColorMask0", "colossal_ColorMask1", "colossal_ColorMask2" })
                if (material.HasProperty(property)) details.Append($" {property}={material.GetVector(property)};");
            foreach (var property in new[] { "_Snow", "_Wetness", "_Porosity", "_Metallic", "_Smoothness", "_CoatStrength",
                "_WorldspaceUVScale", "_AlphaCutoff", "_CullMode", "_CullModeForward", "_DoubleSidedEnable" })
                if (material.HasProperty(property)) details.Append($" {property}={material.GetFloat(property)};");
            var text = details.ToString();
            var id = material.GetInstanceID();
            if (_placedMaterialBindings.TryGetValue(id, out var previous) && previous == text) continue;
            _placedMaterialBindings[id] = text;
            Mod.Log.Info("Placed composite binding: " + text);
        }
    }
}
