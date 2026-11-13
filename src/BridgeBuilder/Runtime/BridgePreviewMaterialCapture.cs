using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Runtime;

// Opt-in, one completed preview only. This observes private preview resources;
// it does not change materials, publish assets or alter the saved bridge graph.
internal sealed class BridgePreviewMaterialCapture
{
    private readonly string _folder;
    private bool _written;
    private BridgePreviewMaterialCapture(string folder) => _folder = folder;

    internal static BridgePreviewMaterialCapture? Begin(string prefix, BridgePreviewDrawList draws)
    {
        var root = Path.Combine(Application.persistentDataPath, "ModsData", "BridgeBuilder");
        var request = Path.Combine(root, "PreviewMaterialCapture.request");
        try
        {
            if (!File.Exists(request)) return null;
            // A source-name filter can target native fixed geometry. Empty requests retain
            // the historical generated-material filter; unrelated catalogue previews do not consume it.
            var match = File.ReadAllText(request).Trim();
            bool generated = false;
            foreach (var draw in draws.Draws)
                generated |= match.Length != 0
                    ? draw.Source.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0
                    : draw.Material.shader.name.StartsWith("BridgeBuilder/", StringComparison.Ordinal);
            if (!generated) return null;
            var folder = Path.Combine(root, "PreviewMaterialCaptures", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            File.Move(request, Path.Combine(folder, "request.txt"));
            File.WriteAllText(Path.Combine(folder, "request-identity.txt"), prefix);
            Mod.Log.Info("Preview material capture armed: " + folder);
            return new BridgePreviewMaterialCapture(folder);
        }
        catch (Exception exception)
        {
            Mod.Log.Warn("Preview material capture arm failed: " + exception.Message);
            return null;
        }
    }

    internal void Write(BridgePreviewDrawList draws, Camera camera, Vector3 origin,
        Texture2D shaded, Texture2D coverage)
    {
        if (_written) return;
        _written = true;
        try
        {
            // Already completed render readbacks: no extra draw, no reconfiguration.
            File.WriteAllBytes(Path.Combine(_folder, "shaded.exr"), shaded.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat));
            File.WriteAllBytes(Path.Combine(_folder, "coverage.png"), coverage.EncodeToPNG());
            using var text = new StreamWriter(Path.Combine(_folder, "bindings.txt"), false, Encoding.UTF8);
            text.WriteLine("Native preview capture; values are invariant-culture floats; shaded.exr is linear HDR.");
            text.WriteLine("frame=" + Time.frameCount + " origin=" + Vector(origin));
            Matrix(text, "cameraLocalToWorld", camera.transform.localToWorldMatrix);
            Matrix(text, "worldToCamera", camera.worldToCameraMatrix);
            Matrix(text, "projection", camera.projectionMatrix);
            draws.WriteDiagnosticSources(text);
            var meshes = new Dictionary<Mesh, int>();
            var materials = new Dictionary<Material, int>();
            int drawIndex = 0;
            foreach (var draw in draws.Draws)
            {
                if (!meshes.TryGetValue(draw.Mesh, out var meshId))
                {
                    meshes.Add(draw.Mesh, meshId = meshes.Count);
                    Mesh(draw.Mesh, meshId);
                }
                if (!materials.TryGetValue(draw.Material, out var materialId))
                {
                    materials.Add(draw.Material, materialId = materials.Count);
                    Material(text, draw.Material, materialId);
                }
                text.WriteLine($"draw={drawIndex++} mesh={meshId} material={materialId} submesh={draw.SubMesh} meshName={draw.Mesh.name}");
                text.WriteLine("source=" + draw.Source);
                Matrix(text, "drawTransform", draw.Transform);
                text.WriteLine("boundsCenter=" + Vector(draw.LocalBounds.center) + " boundsSize=" + Vector(draw.LocalBounds.size));
                var shader = draw.Material.shader;
                for (int i = 0; i < shader.GetPropertyCount(); i++)
                {
                    var name = shader.GetPropertyName(i);
                    if (!draw.Properties.HasProperty(Shader.PropertyToID(name))) continue;
                    var type = shader.GetPropertyType(i);
                    text.WriteLine("block " + name + " " + type + " " +
                        (type == ShaderPropertyType.Texture ? Texture(draw.Properties.GetTexture(name)) :
                        type == ShaderPropertyType.Float || type == ShaderPropertyType.Range ? Float(draw.Properties.GetFloat(name)) :
                        type == ShaderPropertyType.Int ? draw.Properties.GetInt(name).ToString(CultureInfo.InvariantCulture) :
                        Vector(draw.Properties.GetVector(name))));
                }
                text.WriteLine("block colossal_BoneParameters=" + Vector(draw.Properties.GetVector("colossal_BoneParameters")));
                for (var i = 0; i < 8; i++)
                {
                    var name = "colossal_CompositionMatrix" + i;
                    if (draw.Properties.HasProperty(Shader.PropertyToID(name))) Matrix(text, name, draw.Properties.GetMatrix(name));
                }
                for (var i = 0; i < 4; i++)
                    text.WriteLine("block colossal_CompositionSync" + i + "=" + Vector(draw.Properties.GetVector("colossal_CompositionSync" + i)));
            }
            foreach (var pose in draws.DiagnosticPoses)
            {
                text.WriteLine("pose=" + pose.Key.name + " count=" + pose.Value.Skin.Length);
                for (int i = 0; i < pose.Value.Skin.Length; i++) Matrix(text, "skin[" + i + "]", pose.Value.Skin[i]);
            }
            Mod.Log.Info($"Preview material capture complete: {_folder}; draws={drawIndex}; meshes={meshes.Count}; materials={materials.Count}.");
        }
        catch (Exception exception)
        {
            Mod.Log.Warn("Preview material capture failed (preview remains unchanged): " + exception);
        }
    }

    private void Mesh(Mesh mesh, int id)
    {
        using var stream = new BinaryWriter(File.Create(Path.Combine(_folder, "mesh-" + id + ".bin")));
        stream.Write(0x314D5042); // BPM1, raw channels and original index topology.
        stream.Write(mesh.vertexCount);
        var attributes = mesh.GetVertexAttributes();
        stream.Write(attributes.Length);
        foreach (var attribute in attributes)
        {
            stream.Write((int)attribute.attribute); stream.Write((int)attribute.format);
            stream.Write(attribute.dimension); stream.Write(attribute.stream);
            stream.Write(mesh.GetVertexAttributeOffset(attribute.attribute));
        }
        using var data = UnityEngine.Mesh.AcquireReadOnlyMeshData(mesh);
        stream.Write(mesh.vertexBufferCount);
        for (int i = 0; i < mesh.vertexBufferCount; i++)
        {
            var bytes = data[0].GetVertexData<byte>(i).ToArray();
            stream.Write(mesh.GetVertexBufferStride(i)); stream.Write(bytes.Length); stream.Write(bytes);
        }
        stream.Write(mesh.subMeshCount);
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            var indices = mesh.GetIndices(i, true);
            stream.Write((int)mesh.GetTopology(i)); stream.Write(indices.Length);
            foreach (var index in indices) stream.Write(index);
        }
    }

    private static void Material(TextWriter text, Material material, int id)
    {
        text.WriteLine($"material={id} instance={material.GetInstanceID()} name={material.name} shader={material.shader.name} queue={material.renderQueue} instancing={material.enableInstancing}");
        text.WriteLine("keywords=" + string.Join(",", material.shaderKeywords));
        for (int i = 0; i < material.passCount; i++)
        {
            var pass = material.GetPassName(i);
            text.WriteLine("pass=" + pass + " enabled=" + material.GetShaderPassEnabled(pass));
        }
        var shader = material.shader;
        for (int i = 0; i < shader.GetPropertyCount(); i++)
        {
            var name = shader.GetPropertyName(i); var type = shader.GetPropertyType(i);
            text.WriteLine(name + " " + type + " " + (type == ShaderPropertyType.Texture ?
                Texture(material.GetTexture(name)) + " scale=" + Vector(material.GetTextureScale(name)) + " offset=" + Vector(material.GetTextureOffset(name)) :
                type == ShaderPropertyType.Float || type == ShaderPropertyType.Range ? Float(material.GetFloat(name)) :
                type == ShaderPropertyType.Int ? material.GetInt(name).ToString(CultureInfo.InvariantCulture) : Vector(material.GetVector(name))));
        }
    }

    private static string Texture(Texture? value) => value == null ? "null" :
        $"{value.name} {value.width}x{value.height} {value.graphicsFormat} filter={value.filterMode} wrap={value.wrapMode} bias={Float(value.mipMapBias)}";
    private static string Float(float value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static string Vector(Vector4 value) => $"{Float(value.x)},{Float(value.y)},{Float(value.z)},{Float(value.w)}";
    private static void Matrix(TextWriter text, string name, Matrix4x4 matrix)
    {
        text.Write(name + " columns=");
        for (int i = 0; i < 4; i++) text.Write((i == 0 ? "" : ";") + Vector(matrix.GetColumn(i)));
        text.WriteLine();
    }
}
