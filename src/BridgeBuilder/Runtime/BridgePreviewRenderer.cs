using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.RendererUtils;

namespace BridgeBuilder.Runtime;

/// <summary>Bounded HDRP render of an explicit, city-independent draw list.</summary>
internal sealed class BridgePreviewRenderer : IDisposable
{
    private BridgePreviewCamera _camera = null!;
    private BridgePreviewStage _stage = null!;
    private GameObject _volumeObject = null!;
    private VolumeProfile _lightingProfile = null!;
    private RenderTexture _capture = null!;
    private RenderTexture _shadedCapture = null!;
    private RTHandle _shadedHandle = null!;
    private PreviewPass _pass = null!;
    private Action<string, string>? _completion;
    private bool _disposed;
    private int _startedFrame;
    private int _renderedFrame = -1;
    private const int TextureWarmupFrames = 8;
    private const int PreviewVolumeLayer = BridgePreviewStage.Layer;
    private const float StudioEV100 = 11f;
    // A static transparent capture has no useful TAA history and our custom
    // target bypasses HDRP's post-process AA. Rasterize at 2x in each dimension,
    // then resolve coverage ourselves instead of publishing aliased one-pixel wires.
    private const int ImageWidth = 1536;
    private const int ImageHeight = 768;
    private const int SampleScale = 2;

    internal void Initialize(string prefix, BridgePreviewDrawList draws)
    {
        _camera = new BridgePreviewCamera(prefix, ImageWidth * SampleScale, ImageHeight * SampleScale);
        _stage = new BridgePreviewStage();
        _stage.Initialize(prefix, _camera.Camera, draws);
        var additional = _camera.Camera.gameObject.AddComponent<HDAdditionalCameraData>();
        additional.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        additional.backgroundColorHDR = Color.clear;
        additional.clearDepth = true;
        additional.xrRendering = false;
        additional.volumeLayerMask = 1 << PreviewVolumeLayer;
        additional.probeLayerMask = 0;
        additional.customRenderingSettings = true;
        ConfigureFrame(additional, FrameSettingsField.CustomPass, true);
        ConfigureFrame(additional, FrameSettingsField.ExposureControl, false);
        ConfigureFrame(additional, FrameSettingsField.Postprocess, false);
        ConfigureFrame(additional, FrameSettingsField.ColorGrading, false);
        ConfigureFrame(additional, FrameSettingsField.Tonemapping, false);
        ConfigureFrame(additional, FrameSettingsField.Antialiasing, false);
        ConfigureFrame(additional, FrameSettingsField.MotionBlur, false);
        ConfigureFrame(additional, FrameSettingsField.DepthOfField, false);
        ConfigureFrame(additional, FrameSettingsField.Bloom, false);
        ConfigureFrame(additional, FrameSettingsField.AtmosphericScattering, false);
        ConfigureFrame(additional, FrameSettingsField.VolumetricClouds, false);
        ConfigureFrame(additional, FrameSettingsField.LightLayers, true);
        // The private box-spot key uses punctual shadows, never sun cascades.
        ConfigureFrame(additional, FrameSettingsField.ShadowMaps, true);
        // Lane markings are native curved decal meshes. HDRP must build this
        // camera's DBuffer before the forward thumbnail pass shades its road.
        ConfigureFrame(additional, FrameSettingsField.Decals, true);
        ConfigureFrame(additional, FrameSettingsField.OpaqueObjects, true);
        ConfigureFrame(additional, FrameSettingsField.DecalLayers, true);
        ConfigureFrame(additional, FrameSettingsField.SSR, false);
        ConfigureFrame(additional, FrameSettingsField.SSAO, false);
        additional.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
        var framingBounds = draws.Bounds;
        framingBounds.center += _stage.Origin;
        _camera.Frame(framingBounds);
        _volumeObject = new GameObject(prefix + "_renderpass")
        { hideFlags = HideFlags.HideAndDontSave, layer = PreviewVolumeLayer };
        _stage.MoveToScene(_volumeObject);
        // Long fixed-span bridges can exceed the default shadow fade distance.
        // Use a LOCAL volume around the far-away preview camera. Layer isolation
        // alone is insufficient: another camera may have an Everything mask.
        _volumeObject.transform.position = _camera.Camera.transform.position;
        _lightingProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        _lightingProfile.hideFlags = HideFlags.HideAndDontSave;
        var shadowSettings = _lightingProfile.Add<HDShadowSettings>(false);
        shadowSettings.maxShadowDistance.Override(_stage.ShadowDistance);
        var lightingBounds = _volumeObject.AddComponent<BoxCollider>();
        lightingBounds.isTrigger = true;
        lightingBounds.size = Vector3.one * 2f;
        var lightingVolume = _volumeObject.AddComponent<Volume>();
        lightingVolume.isGlobal = false;
        lightingVolume.blendDistance = 0f;
        lightingVolume.priority = float.MaxValue;
        lightingVolume.sharedProfile = _lightingProfile;
        // Keep the normal HDRP camera result (including its DBuffer lane decals).
        // Redrawing Forward alone is not a substitute for the native decal pass.
        // A separate geometry coverage target supplies alpha: HDRP's opaque
        // camera background is not a usable transparent silhouette.
        _capture = new RenderTexture(ImageWidth * SampleScale, ImageHeight * SampleScale, 24,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
        { name = prefix + "_coverage", hideFlags = HideFlags.HideAndDontSave };
        _capture.Create();
        _shadedCapture = new RenderTexture(ImageWidth * SampleScale, ImageHeight * SampleScale, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
        { name = prefix + "_native_hdr", hideFlags = HideFlags.HideAndDontSave };
        _shadedCapture.Create();
        _shadedHandle = RTHandles.Alloc(_shadedCapture);
        var volume = _volumeObject.AddComponent<CustomPassVolume>();
        volume.isGlobal = true;
        volume.targetCamera = _camera.Camera;
        volume.injectionPoint = CustomPassInjectionPoint.BeforePostProcess;
        _pass = new PreviewPass { PreviewCamera = _camera.Camera, Draws = draws,
            Target = _capture, ShadedTarget = _shadedHandle };
        volume.customPasses.Add(_pass);
    }

    private static void ConfigureFrame(HDAdditionalCameraData camera, FrameSettingsField field, bool enabled)
    {
        camera.renderingPathCustomFrameSettings.SetEnabled(field, enabled);
        camera.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
    }

    internal void Start(Action<string, string> completion)
    {
        if (_disposed || _completion != null) return;
        _completion = completion;
        _startedFrame = Time.frameCount;
        RenderPipelineManager.endCameraRendering += OnRendered;
        _camera.Camera.enabled = true;
    }

    internal void Tick()
    {
        if (_disposed || _completion == null) return;
        // In the shipped HDRP, EndCameraRendering occurs BEFORE ExecuteCommandBuffer
        // and Submit. Reading in that event captured the untouched/clear target and
        // reported it as a successful preview. Read only on a later engine frame;
        // ReadPixels then synchronizes the already submitted GPU work.
        if (_renderedFrame >= 0 && Time.frameCount > _renderedFrame)
        {
            ReadCompletedFrame();
            return;
        }
        if (Time.frameCount - _startedFrame > 180)
            Finish(string.Empty, "RenderTimeout");
    }

    private void OnRendered(ScriptableRenderContext context, Camera camera)
    {
        if (_disposed || camera != _camera.Camera || _pass.RenderCount == 0) return;
        // Allow the explicitly requested VT tiles to stream before publishing.
        if (_pass.Error.Length == 0 && _pass.RenderCount < TextureWarmupFrames) return;
        _renderedFrame = Time.frameCount;
        _camera.Camera.enabled = false;
        RenderPipelineManager.endCameraRendering -= OnRendered;
    }

    private void ReadCompletedFrame()
    {
        if (_pass.Error.Length != 0)
        {
            Finish(string.Empty, _pass.Error);
            return;
        }
        Texture2D? pixels = null;
        Texture2D? coverage = null;
        var previous = RenderTexture.active;
        try
        {
            var target = _shadedCapture;
            RenderTexture.active = target;
            pixels = new Texture2D(target.width, target.height, TextureFormat.RGBAFloat, false, true)
            { hideFlags = HideFlags.HideAndDontSave };
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0, false);
            pixels.Apply(false, false);
            var linear = pixels.GetPixels();
            RenderTexture.active = _capture;
            coverage = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false, true)
            { hideFlags = HideFlags.HideAndDontSave };
            coverage.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0, false);
            coverage.Apply(false, false);
            var mask = coverage.GetPixels32();
            for (var i = 0; i < linear.Length; i++) linear[i].a = mask[i].a / 255f;
            if (!HasVisibleContent(linear))
            {
                Finish(string.Empty, "RenderEmpty");
                return;
            }
            var output = ResolveCoverage(linear, target.width);
            var png = new Texture2D(ImageWidth, ImageHeight, TextureFormat.RGBA32, false);
            try
            {
                png.SetPixels32(output);
                png.Apply(false, false);
                Finish("data:image/png;base64," + Convert.ToBase64String(ImageConversion.EncodeToPNG(png)), string.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(png); }
        }
        catch (Exception)
        {
            Finish(string.Empty, "RenderReadFailed");
        }
        finally
        {
            RenderTexture.active = previous;
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            if (coverage != null) UnityEngine.Object.DestroyImmediate(coverage);
        }
    }

    private static Color32[] ResolveCoverage(Color[] linear, int sourceWidth)
    {
        var exposure = 1f / (1.2f * Mathf.Pow(2f, StudioEV100));
        var output = new Color32[ImageWidth * ImageHeight];
        for (var y = 0; y < ImageHeight; y++)
        for (var x = 0; x < ImageWidth; x++)
        {
            var sum = Color.clear;
            for (var sy = 0; sy < SampleScale; sy++)
            for (var sx = 0; sx < SampleScale; sx++)
            {
                var p = linear[(y * SampleScale + sy) * sourceWidth + x * SampleScale + sx];
                if (float.IsNaN(p.a) || float.IsInfinity(p.a) || p.a <= 0f) continue;
                var alpha = Mathf.Clamp01(p.a);
                // Resolve in premultiplied linear space. Averaging straight RGB
                // with clear black first would put dark fringes around the cables.
                sum.r += FiniteRadiance(p.r) * alpha;
                sum.g += FiniteRadiance(p.g) * alpha;
                sum.b += FiniteRadiance(p.b) * alpha;
                sum.a += alpha;
            }
            if (sum.a <= 0f) continue;
            var unpremultiply = exposure / sum.a;
            output[y * ImageWidth + x] = Display(new Color(
                sum.r * unpremultiply, sum.g * unpremultiply,
                sum.b * unpremultiply, sum.a / (SampleScale * SampleScale)));
        }
        return output;
    }

    private static float FiniteRadiance(float value)
        => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);

    private static Color32 Display(Color linear)
    {
        // One exposure/shoulder multiplier for the entire RGB vector. Applying
        // r/(1+r), g/(1+g), b/(1+b) independently changed their ratios: saturated
        // red/copper turned pink and coloured metal highlights became white.
        // A peak-channel shoulder preserves linear chromaticity and fits every
        // channel into the output gamut without independent clipping. Neutral
        // colours retain the original shoulder. Alpha is coverage, not radiance.
        var shoulder = 1f / (1f + Mathf.Max(linear.r, Mathf.Max(linear.g, linear.b)));
        return (Color32)new Color(
            Mathf.LinearToGammaSpace(linear.r * shoulder),
            Mathf.LinearToGammaSpace(linear.g * shoulder),
            Mathf.LinearToGammaSpace(linear.b * shoulder), linear.a);
    }

    private static bool HasVisibleContent(Color[] pixels)
    {
        if (pixels.Length == 0) return false;
        foreach (var pixel in pixels)
        {
            if (pixel.a > 0f && (pixel.r > 0f || pixel.g > 0f || pixel.b > 0f) &&
                !float.IsNaN(pixel.r) && !float.IsNaN(pixel.g) && !float.IsNaN(pixel.b)) return true;
        }
        return false;
    }

    private void Finish(string image, string error)
    {
        _camera.Camera.enabled = false;
        _stage.SetVisible(false);
        RenderPipelineManager.endCameraRendering -= OnRendered;
        var completion = _completion;
        _completion = null;
        completion?.Invoke(image, error);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _completion = null;
        RenderPipelineManager.endCameraRendering -= OnRendered;
        if (_camera != null) _camera.Camera.enabled = false;
        // Destroy the pass before its camera and before the caller releases meshes.
        UnityEngine.Object.DestroyImmediate(_volumeObject);
        if (_lightingProfile != null)
        {
            foreach (var component in _lightingProfile.components)
                UnityEngine.Object.DestroyImmediate(component);
            UnityEngine.Object.DestroyImmediate(_lightingProfile);
        }
        if (_capture != null) _capture.Release();
        UnityEngine.Object.DestroyImmediate(_capture);
        _shadedHandle?.Release();
        if (_shadedCapture != null) _shadedCapture.Release();
        UnityEngine.Object.DestroyImmediate(_shadedCapture);
        _camera?.Dispose();
        _stage?.Dispose();
    }

    [Serializable]
    private sealed class PreviewPass : CustomPass
    {
        private static readonly ShaderTagId[] ShaderTags =
        {
            new("Forward"), new("ForwardOnly"), new("SRPDefaultUnlit")
        };
        internal Camera PreviewCamera = null!;
        internal RenderTexture Target = null!;
        internal RTHandle ShadedTarget = null!;
        internal BridgePreviewDrawList Draws = null!;
        internal int RenderCount;
        internal string Error = string.Empty;

        protected override void Execute(CustomPassContext ctx)
        {
            if (ctx.hdCamera.camera != PreviewCamera || Error.Length != 0) return;
            RenderCount++;
            var infoview = Shader.GetGlobalInt("colossal_InfoviewOn");
            var infoviewKeyword = Shader.IsKeywordEnabled("INFOVIEW_ON");
            try
            {
                Draws.RefreshMaterialBindings();
                // BeforePostProcess still has unclipped linear HDR. Use HDRP's
                // scaled RTHandle copy, not Blit over the backing allocation.
                // The Forward redraw below is used for coverage only.
                HDUtils.BlitCameraTexture(ctx.cmd, ctx.cameraColorBuffer, ShadedTarget);
                ctx.cmd.SetRenderTarget(Target);
                ctx.cmd.SetViewport(new Rect(0, 0, Target.width, Target.height));
                ctx.cmd.ClearRenderTarget(true, true, Color.clear);
                ctx.cmd.DisableShaderKeyword("INFOVIEW_ON");
                ctx.cmd.SetGlobalInt("colossal_InfoviewOn", 0);
                // Match ThumbnailCustomPass: native renderer lists, per-object
                // data and depth state. DrawMesh with arbitrary world matrices
                // bypassed the renderers' HDRP camera-relative setup.
                var desc = new RendererListDesc(ShaderTags, ctx.cullingResults, PreviewCamera)
                {
                    rendererConfiguration = PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume |
                        PerObjectData.Lightmaps,
                    renderQueueRange = RenderQueueRange.all,
                    sortingCriteria = SortingCriteria.BackToFront,
                    excludeObjectMotionVectors = true,
                    layerMask = 1 << PreviewVolumeLayer,
                    stateBlock = new RenderStateBlock(RenderStateMask.Depth | RenderStateMask.Stencil)
                    {
                        depthState = new DepthState(true, CompareFunction.LessEqual)
                    }
                };
                CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, ctx.renderContext.CreateRendererList(desc));
            }
            catch (Exception)
            {
                Error = "RenderFailed";
            }
            finally
            {
                ctx.cmd.SetGlobalInt("colossal_InfoviewOn", infoview);
                if (infoviewKeyword) ctx.cmd.EnableShaderKeyword("INFOVIEW_ON");
                else ctx.cmd.DisableShaderKeyword("INFOVIEW_ON");
            }
        }
    }
}
