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
    [Serializable]
    private sealed class RenderCaptureRequest
    {
        public string requestId = "";
        public string owner = "";
        public int captureCount = 4;
        public float intervalSeconds = 15f;
        public bool captureNativeMeshes;
        [NonSerialized] public int pendingReadbacks;
        [NonSerialized] public bool submitted;
    }
    private RenderCaptureRequest? _renderCaptureRequest;
    private DateTime _renderCaptureRequestWrite;
    private string _lastRenderCaptureRequest = "";

    private void ReadRenderCaptureRequest()
    {
        var path = Path.Combine(Application.persistentDataPath, "ModsData", "BridgeBuilder", "RenderDiagnostics", "request.json");
        if (!File.Exists(path)) return;
        var written = File.GetLastWriteTimeUtc(path);
        if (written == _renderCaptureRequestWrite) return;
        _renderCaptureRequestWrite = written;
        try
        {
            var request = JsonUtility.FromJson<RenderCaptureRequest>(File.ReadAllText(path));
            if (!Guid.TryParse(request.requestId, out var id) || request.owner.Length != 37 || request.owner[0] != 'b'
                || !Guid.TryParse(request.owner.Substring(1), out _) || request.captureCount < 1 || request.captureCount > 8
                || float.IsNaN(request.intervalSeconds) || request.intervalSeconds < 5f || request.intervalSeconds > 120f)
                throw new InvalidDataException("Render capture request requires UUID requestId, bUUID owner, 1..8 captures and 5..120 seconds interval.");
            request.requestId = id.ToString("D");
            if (File.Exists(Path.Combine(Path.GetDirectoryName(path)!, request.requestId, "complete.json"))) return;
            if (request.requestId == _lastRenderCaptureRequest) return;
            ReleaseRoadBufferDiagnostics();
            _lastRenderCaptureRequest = request.requestId;
            _renderCaptureRequest = request;
            _roadBufferCaptures = 0;
            Mod.Log.Info($"Render capture request accepted: {request.requestId}; owner={request.owner}; captures={request.captureCount}; interval={request.intervalSeconds}s.");
        }
        catch (Exception error) { Mod.Log.Warn(error, "Unable to read render capture request."); }
    }

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
            UnityEngine.Time.realtimeSinceStartup < _nextRoadBufferCapture || _roadBufferCaptures >= (_renderCaptureRequest?.captureCount ?? 6)) return;
        _nextRoadBufferCapture = UnityEngine.Time.realtimeSinceStartup + (_renderCaptureRequest?.intervalSeconds ?? 30f);
        var capture = ++_roadBufferCaptures;
        var folder = Path.Combine(Application.persistentDataPath, "ModsData", "BridgeBuilder", "RenderDiagnostics");
        var request = _renderCaptureRequest;
        if (request != null) folder = Path.Combine(folder, request.requestId);
        Directory.CreateDirectory(folder);
        if (request != null)
        {
            var record = new RenderCaptureRecord { owner = request.owner, requestId = request.requestId,
                capture = capture, utc = DateTime.UtcNow.ToString("O"), realtime = UnityEngine.Time.realtimeSinceStartup,
                position = camera.transform.position, rotation = camera.transform.rotation,
                fieldOfView = camera.fieldOfView, nearClip = camera.nearClipPlane, farClip = camera.farClipPlane };
            File.WriteAllText(Path.Combine(folder, capture + "-camera.json"), JsonUtility.ToJson(record, true));
        }
        Save("normal", _roadDiagnosticCamera.GetGraphicsBuffer(HDAdditionalCameraData.BufferAccessType.Normal)?.rt);
        Save("color", _roadDiagnosticCamera.GetGraphicsBuffer(HDAdditionalCameraData.BufferAccessType.Color)?.rt);
        // HDRP may expose these only while the deferred pass is active. Never assume
        // they exist: the retained normal/color buffers above are the public API.
        Save("albedo", Shader.GetGlobalTexture("_GBufferTexture0") as RenderTexture);
        Save("material", Shader.GetGlobalTexture("_GBufferTexture2") as RenderTexture);
        if (request != null)
        {
            Save("lighting", Shader.GetGlobalTexture("_GBufferTexture3") as RenderTexture);
            Save("occlusion", Shader.GetGlobalTexture("_ScreenSpaceOcclusionTexture") as RenderTexture);
            for (int i = 0; i < 4; i++)
                Save("decal" + i, Shader.GetGlobalTexture("_DBufferTexture" + i) as RenderTexture);
            if (request.captureNativeMeshes)
            {
                try { CaptureNativeBatches(request, folder, capture, CompleteRequest); }
                catch (Exception error)
                {
                    File.WriteAllText(Path.Combine(folder, capture + "-batches-error.json"),
                        JsonUtility.ToJson(new NativeReadbackRecord { status = "capture failed", error = error.Message }));
                    Mod.Log.Warn(error, "Unable to capture native batches.");
                }
            }
        }
        if (_roadBufferCaptures >= (request?.captureCount ?? 6))
        {
            ReleaseRoadBufferDiagnostics();
            if (request != null)
            {
                request.submitted = true;
                File.WriteAllText(Path.Combine(folder, "submitted.json"), JsonUtility.ToJson(request, true));
                CompleteRequest();
            }
        }

        void CompleteRequest()
        {
            if (request != null && request.submitted && request.pendingReadbacks == 0)
                File.WriteAllText(Path.Combine(folder, "complete.json"), JsonUtility.ToJson(request, true));
        }

        void Save(string channel, RenderTexture? target)
        {
            if (target == null || !target.IsCreated())
            {
                Mod.Log.Info($"Road render diagnostic: capture={capture}; channel={channel}; unavailable.");
                if (request != null) File.WriteAllText(Path.Combine(folder, $"{capture}-{channel}.json"), "{\"status\":\"unavailable\"}");
                return;
            }
            var width = target.width; var height = target.height;
            var format = target.graphicsFormat;
            if (request != null) request.pendingReadbacks++;
            try
            {
            AsyncGPUReadback.Request(target, 0, TextureFormat.RGBA32, readback =>
            {
                try
                {
                if (readback.hasError)
                {
                    Mod.Log.Warn($"Road render diagnostic readback failed: {channel}.");
                    File.WriteAllText(Path.Combine(folder, $"{capture}-{channel}.json"), "{\"status\":\"readback failed\"}");
                    return;
                }
                var pixels = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                try
                {
                    var bytes = readback.GetData<byte>();
                    if (request != null) File.WriteAllBytes(Path.Combine(folder, $"{capture}-{channel}.bin"), bytes.ToArray());
                    pixels.LoadRawTextureData(bytes);
                    pixels.Apply(false, false);
                    var path = Path.Combine(folder, $"{capture}-{channel}.png");
                    File.WriteAllBytes(path, pixels.EncodeToPNG());
                    File.WriteAllText(Path.Combine(folder, $"{capture}-{channel}.json"),
                        $"{{\"status\":\"captured\",\"width\":{width},\"height\":{height},\"sourceFormat\":\"{format}\",\"utc\":\"{DateTime.UtcNow:O}\",\"publicCameraBuffer\":{(channel == "normal" || channel == "color" ? "true" : "false")}}}");
                    Mod.Log.Info($"Road render diagnostic: {path}; source={format}; {width}x{height}; raw channels, no gamma conversion.");
                }
                finally { UnityEngine.Object.Destroy(pixels); }
                }
                catch (Exception error) { Mod.Log.Warn(error, $"Unable to save render diagnostic {capture}-{channel}."); }
                finally
                {
                    if (request != null) request.pendingReadbacks--;
                    CompleteRequest();
                }
            });
            }
            catch (Exception error)
            {
                if (request != null) request.pendingReadbacks--;
                File.WriteAllText(Path.Combine(folder, $"{capture}-{channel}.json"), "{\"status\":\"readback request failed\"}");
                Mod.Log.Warn(error, $"Unable to request render diagnostic {capture}-{channel}.");
                CompleteRequest();
            }
        }
    }

    [Serializable]
    private sealed class RenderCaptureRecord
    {
        public string owner = "", requestId = "", utc = "";
        public int capture;
        public float realtime, fieldOfView, nearClip, farClip;
        public Vector3 position;
        public Quaternion rotation;
    }

    // Inspect the actual native city batch, not SurfaceAsset.Load's separate material.
    // Read-only: never change shared textures, keywords or reference counts.
    private void InspectPlacedRoadMaterials()
    {
        if (UnityEngine.Time.realtimeSinceStartup < _nextMaterialInspection) return;
        _nextMaterialInspection = UnityEngine.Time.realtimeSinceStartup + 15f;
        ReadRenderCaptureRequest();
        foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
        {
            if (!material.name.StartsWith("Batch (", StringComparison.Ordinal) ||
                (!material.name.Contains(" Road Surface") && !material.HasProperty("_WorldspaceAlbedo"))) continue;
            var requested = _renderCaptureRequest;
            bool selectedCapture = requested == null ? material.name.Contains(" Road Surface") :
                material.name.Contains("GOArch-") && material.HasProperty("_BaseColorMap") &&
                (material.GetTexture("_BaseColorMap")?.name.Contains("[ImportedData/" + requested.owner + "/") ?? false);
            if (_roadBufferCaptures < (requested?.captureCount ?? 6) && _roadDiagnosticCamera == null && selectedCapture && Camera.main != null &&
                Camera.main.TryGetComponent<HDAdditionalCameraData>(out var cameraData))
            {
                _roadDiagnosticCamera = cameraData;
                cameraData.requestGraphicsBuffer += RequestRoadBuffers;
                RenderPipelineManager.endCameraRendering += CaptureRoadBuffers;
                _nextRoadBufferCapture = UnityEngine.Time.realtimeSinceStartup + (requested == null ? 2f : 15f);
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
