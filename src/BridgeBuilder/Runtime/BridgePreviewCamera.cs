using System;
using UnityEngine;

namespace BridgeBuilder.Runtime;

/// <summary>Owns an offscreen, orthographic true-isometric camera and its target.</summary>
internal sealed class BridgePreviewCamera : IDisposable
{
    private readonly GameObject _object;
    internal Camera Camera { get; }
    internal RenderTexture Target { get; }
    private bool _disposed;

    internal BridgePreviewCamera(string prefix, int width, int height)
    {
        _object = new GameObject(prefix + "_camera") { hideFlags = HideFlags.HideAndDontSave };
        Camera = _object.AddComponent<Camera>();
        // Rendering must be requested explicitly; this camera never joins the main view.
        Camera.enabled = false;
        // HDRP gathers camera culling requests before executing render requests.
        // Cull this isolated preview after the ordinary city/UI cameras.
        Camera.depth = float.MaxValue;
        Camera.orthographic = true;
        Camera.clearFlags = CameraClearFlags.SolidColor;
        Camera.backgroundColor = Color.clear;
        // Keep scene radiance in HDR until the preview's own exposure and
        // tonemapping have run; the final PNG target remains ordinary SDR.
        Camera.allowHDR = true;
        Camera.allowMSAA = false;
        Camera.useOcclusionCulling = false;
        Camera.aspect = (float)width / height;
        Target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            name = prefix + "_color",
            hideFlags = HideFlags.HideAndDontSave,
            antiAliasing = 1,
        };
        Target.Create();
        Camera.targetTexture = Target;
    }

    internal void Frame(Bounds bounds)
    {
        // atan(1/sqrt(2)), not a 30-degree perspective approximation.
        var pitch = Mathf.Atan(1f / Mathf.Sqrt(2f)) * Mathf.Rad2Deg;
        var rotation = Quaternion.Euler(pitch, 45f, 0f);
        Camera.transform.rotation = rotation;
        var inverse = Quaternion.Inverse(rotation);
        var extent = bounds.extents;
        var viewExtent = Vector3.zero;
        for (var x = -1; x <= 1; x += 2)
        for (var y = -1; y <= 1; y += 2)
        for (var z = -1; z <= 1; z += 2)
        {
            var corner = inverse * Vector3.Scale(extent, new Vector3(x, y, z));
            viewExtent = Vector3.Max(viewExtent,
                new Vector3(Mathf.Abs(corner.x), Mathf.Abs(corner.y), Mathf.Abs(corner.z)));
        }
        Camera.orthographicSize = Mathf.Max(0.01f,
            Mathf.Max(viewExtent.y, viewExtent.x / Camera.aspect) * 1.1f);
        var distance = Mathf.Max(1f, viewExtent.z * 2f + 1f);
        Camera.transform.position = bounds.center - Camera.transform.forward * distance;
        Camera.nearClipPlane = 0.01f;
        Camera.farClipPlane = distance + viewExtent.z + 1f;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Camera.targetTexture = null;
        Target.Release();
        UnityEngine.Object.DestroyImmediate(Target);
        UnityEngine.Object.DestroyImmediate(_object);
    }
}
