using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace BridgeBuilder.Runtime;

/// <summary>
/// An actual temporary bridge object in a separate scene. Like the game's
/// RenderPrefabRenderer, use MeshFilter/MeshRenderer so Unity supplies the
/// per-object matrices (including HDRP camera-relative matrices) and light data.
/// There are no net entities, registered prefabs or save-game objects here.
/// </summary>
internal sealed class BridgePreviewStage : IDisposable
{
    internal const int Layer = 31;
    private const uint LightingLayer = 128;
    private const float AmbientFillLux = 120f;
    private Scene _scene;
    private GameObject _root = null!;
    private readonly List<Renderer> _renderers = new();
    private readonly List<Light> _lights = new();
    private bool _disposed;
    internal Vector3 Origin => _root.transform.position;
    internal float ShadowDistance { get; private set; }

    internal void Initialize(string prefix, Camera camera, BridgePreviewDrawList draws)
    {
        _scene = SceneManager.CreateScene(prefix + "_preview_scene");
        _root = new GameObject(prefix + "_bridge_segment")
        { hideFlags = HideFlags.HideAndDontSave, layer = Layer };
        // Isolate through the preview scene, rendering layer and distant finite
        // light volumes. Never toggle lights per camera: HDRP collects multiple
        // cameras before executing their render requests and shadow preparation.
        _root.transform.position = new Vector3(0f, -100000f, 0f);
        SceneManager.MoveGameObjectToScene(_root, _scene);
        SceneManager.MoveGameObjectToScene(camera.gameObject, _scene);
        camera.scene = _scene;
        camera.cullingMask = 1 << Layer;
        // This bounds-derived distance is only for the isolated light/camera rig,
        // never a bridge-generation measurement or geometry correction.
        ShadowDistance = Mathf.Max(20f, draws.Bounds.size.magnitude * 8f);

        var groups = new Dictionary<(Mesh, Matrix4x4), Material[]>();
        var properties = new Dictionary<(Mesh, Matrix4x4), MaterialPropertyBlock>();
        foreach (var draw in draws.Draws)
        {
            var key = (draw.Mesh, draw.Transform);
            if (!groups.TryGetValue(key, out var materials))
            {
                groups.Add(key, materials = new Material[draw.Mesh.subMeshCount]);
                properties.Add(key, draw.Properties);
            }
            materials[draw.SubMesh] = draw.Material;
        }
        foreach (var group in groups)
        {
            var obj = new GameObject(prefix + "_part_" + _renderers.Count)
            { hideFlags = HideFlags.HideAndDontSave, layer = Layer };
            obj.transform.SetParent(_root.transform, false);
            var matrix = group.Key.Item2;
            var x = (Vector3)matrix.GetColumn(0);
            var y = (Vector3)matrix.GetColumn(1);
            var z = (Vector3)matrix.GetColumn(2);
            obj.transform.localPosition = matrix.GetColumn(3);
            obj.transform.localRotation = Quaternion.LookRotation(z, y);
            obj.transform.localScale = new Vector3(
                x.magnitude * (matrix.determinant < 0f ? -1f : 1f), y.magnitude, z.magnitude);
            obj.AddComponent<MeshFilter>().sharedMesh = group.Key.Item1;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.enabled = false;
            renderer.sharedMaterials = group.Value;
            renderer.SetPropertyBlock(properties[group.Key]);
            renderer.renderingLayerMask = LightingLayer;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _renderers.Add(renderer);
        }

        // Neutral daylight presentation, independent of the city's time/weather.
        // Keep specular response on the key; the fill approximates diffuse sky
        // illumination instead of producing a second hard white sun reflection.
        // The point key casts into the punctual atlas; box spots supply diffuse fill.
        // A Directional light would compete with the game's sun
        // for its single cascade atlas, even with separate scenes/light layers.
        // Neither light is a reference to (or mutation of) the city's sun.
        AddLight(prefix + "_key", new Vector3(50f, -35f, 0f), 4000f, true, draws.Bounds);
        AddLight(prefix + "_fill", new Vector3(35f, 145f, 0f), 1000f, false, draws.Bounds);
        // A weak neutral diffuse environment approximation, including the underside.
        // Opposing box spots avoid a completely unlit normal without changing material
        // colours, exposure, city RenderSettings or the transparent background. Each
        // non-key light has no specular or shadow contribution; only the key owns shadows.
        AddLight(prefix + "_ambient_front", Vector3.zero, AmbientFillLux, false, draws.Bounds);
        AddLight(prefix + "_ambient_back", new Vector3(0f, 180f, 0f), AmbientFillLux, false, draws.Bounds);
        AddLight(prefix + "_ambient_left", new Vector3(0f, 90f, 0f), AmbientFillLux, false, draws.Bounds);
        AddLight(prefix + "_ambient_right", new Vector3(0f, -90f, 0f), AmbientFillLux, false, draws.Bounds);
        AddLight(prefix + "_ambient_top", new Vector3(90f, 0f, 0f), AmbientFillLux, false, draws.Bounds);
        AddLight(prefix + "_ambient_bottom", new Vector3(-90f, 0f, 0f), AmbientFillLux, false, draws.Bounds);
        SetVisible(true);
    }

    internal void MoveToScene(GameObject obj) => SceneManager.MoveGameObjectToScene(obj, _scene);

    private void AddLight(string name, Vector3 angles, float lux, bool key, Bounds bounds)
    {
        var obj = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave, layer = Layer };
        obj.transform.SetParent(_root.transform, false);
        var rotation = Quaternion.Euler(angles);
        var inverse = Quaternion.Inverse(rotation);
        var extent = Vector3.zero;
        for (var x = -1; x <= 1; x += 2)
        for (var y = -1; y <= 1; y += 2)
        for (var z = -1; z <= 1; z += 2)
        {
            var corner = inverse * Vector3.Scale(bounds.extents, new Vector3(x, y, z));
            extent = Vector3.Max(extent,
                new Vector3(Mathf.Abs(corner.x), Mathf.Abs(corner.y), Mathf.Abs(corner.z)));
        }
        var margin = Mathf.Max(1f, bounds.size.magnitude * .01f);
        obj.transform.localRotation = rotation;
        obj.transform.localPosition = bounds.center - rotation * Vector3.forward * (extent.z + margin);
        var light = obj.AddComponent<Light>();
        light.enabled = false;
        light.type = LightType.Spot;
        light.color = Color.white;
        light.useColorTemperature = false;
        light.cullingMask = 1 << Layer;
        var hd = obj.AddComponent<HDAdditionalLightData>();
        // The game's DOTS light merge sorts ProjectorBox after city Point lights,
        // but updates shadow requests only in its Unity-light-count prefix. A
        // shadow-casting box can reserve a slot and then never populate it.
        // Unity Point lights sort before DOTS points (their source indices are
        // lower), so the single preview shadow owner stays in that prefix.
        hd.SetLightTypeAndShape(key ? HDLightTypeAndShape.Point : HDLightTypeAndShape.BoxSpot);
        var keyDistance = Mathf.Max(10f, bounds.size.magnitude * 4f);
        if (key)
            obj.transform.localPosition = bounds.center - rotation * Vector3.forward * keyDistance;
        hd.SetRange(key ? keyDistance + bounds.extents.magnitude + margin : (extent.z + margin) * 2f);
        if (!key)
            hd.SetBoxSpotSize(new Vector2((extent.x + margin) * 2f, (extent.y + margin) * 2f));
        hd.applyRangeAttenuation = false;
        hd.SetLightLayer((LightLayerEnum)LightingLayer, (LightLayerEnum)LightingLayer);
        hd.fadeDistance = key ? keyDistance * 2f : ShadowDistance;
        hd.EnableShadows(key);
        if (key)
        {
            hd.SetShadowUpdateMode(ShadowUpdateMode.EveryFrame);
            hd.SetShadowResolutionOverride(true);
            hd.SetShadowResolution(2048);
            hd.SetShadowNearPlane(.1f);
            hd.SetShadowFadeDistance(keyDistance * 2f);
        }
        hd.affectSpecular = key;
        hd.affectsVolumetric = false;
        // Preserve centre illuminance for the point key: candela = lux * distance².
        hd.SetIntensity(key ? lux * keyDistance * keyDistance : lux,
            key ? LightUnit.Candela : LightUnit.Lux);
        _lights.Add(light);
    }

    internal void SetVisible(bool visible)
    {
        // Called only during stage setup or completion on a later engine frame,
        // never from camera callbacks while HDRP holds queued culling results.
        foreach (var renderer in _renderers) renderer.enabled = visible;
        foreach (var light in _lights) light.enabled = visible;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnityEngine.Object.DestroyImmediate(_root);
        // The caller disposes camera/pass first, so only an empty scene remains.
        if (_scene.IsValid() && _scene.isLoaded) SceneManager.UnloadSceneAsync(_scene);
    }
}
