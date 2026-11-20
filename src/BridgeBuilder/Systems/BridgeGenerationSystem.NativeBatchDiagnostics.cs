using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Systems;

public partial class BridgeGenerationSystem
{
    // Explicit finite diagnostic requests only. Read the actual registered native
    // batches, including GPU-only meshes, without changing their loading or materials.
    private void CaptureNativeBatches(RenderCaptureRequest request, string folder, int capture, Action complete)
    {
        var batches = World.GetOrCreateSystemManaged<BatchManagerSystem>().GetManagedBatches();
        var capturedMeshes = new HashSet<int>();
        var records = new List<NativeBatchRecord>();
        for (int i = 0; i < batches.BatchCount; i++)
        {
            if (batches.GetBatch(i) is not CustomBatch batch) continue;
            var material = batch.material;
            if (material == null || !material.HasProperty("_BaseColorMap") ||
                !(material.GetTexture("_BaseColorMap")?.name.Contains("[ImportedData/" + request.owner + "/") ?? false)) continue;
            var mesh = batch.mesh;
            var record = new NativeBatchRecord { batch = i, group = batch.groupIndex,
                batchIndex = batch.batchIndex, submesh = batch.subMeshIndex,
                sourceSubmesh = batch.sourceSubMeshIndex, sourceEntity = batch.sourceMeshEntity.ToString(),
                sharedEntity = batch.sharedMeshEntity.ToString(), flags = batch.sourceFlags.ToString(),
                generatedType = batch.generatedType.ToString(), material = material.name,
                shader = material.shader.name, keywords = material.shaderKeywords, queue = material.renderQueue,
                mesh = mesh?.name ?? "unavailable", meshId = mesh == null ? 0 : mesh.GetInstanceID() };
            records.Add(record);
            var properties = new List<NativePropertyRecord>();
            for (int p = 0; p < material.shader.GetPropertyCount(); p++)
            {
                var name = material.shader.GetPropertyName(p);
                var id = Shader.PropertyToID(name);
                var type = material.shader.GetPropertyType(p);
                var property = new NativePropertyRecord { name = name, type = type.ToString(),
                    custom = batch.customProps != null && batch.customProps.HasProperty(id) };
                switch (type)
                {
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        property.values = new[] { material.GetFloat(id) };
                        if (property.custom) property.customValues = new[] { batch.customProps.GetFloat(id) };
                        break;
                    case ShaderPropertyType.Int:
                        property.integer = material.GetInteger(id);
                        if (property.custom) property.customInteger = batch.customProps.GetInteger(id);
                        break;
                    case ShaderPropertyType.Color:
                    case ShaderPropertyType.Vector:
                        var value = type == ShaderPropertyType.Color ? (Vector4)material.GetColor(id) : material.GetVector(id);
                        property.values = new[] { value.x, value.y, value.z, value.w };
                        if (property.custom)
                        {
                            var custom = type == ShaderPropertyType.Color ? (Vector4)batch.customProps.GetColor(id) : batch.customProps.GetVector(id);
                            property.customValues = new[] { custom.x, custom.y, custom.z, custom.w };
                        }
                        break;
                    case ShaderPropertyType.Texture:
                        var texture = material.GetTexture(id);
                        property.texture = texture?.name ?? "unavailable";
                        property.textureFormat = texture?.graphicsFormat.ToString() ?? "";
                        property.width = texture?.width ?? 0; property.height = texture?.height ?? 0;
                        if (texture is Texture2D texture2D)
                        {
                            property.mipCount = texture2D.mipmapCount;
                            property.streaming = texture2D.streamingMipmaps;
                            property.loadedMip = texture2D.loadedMipmapLevel;
                            property.requestedMip = texture2D.requestedMipmapLevel;
                            property.desiredMip = texture2D.desiredMipmapLevel;
                            property.loadingMip = texture2D.loadingMipmapLevel;
                        }
                        property.customTexture = property.custom ? batch.customProps.GetTexture(id)?.name ?? "unavailable" : "";
                        break;
                }
                property.rawFloatBytes = RawFloats(property.values);
                property.rawCustomFloatBytes = RawFloats(property.customValues);
                properties.Add(property);
            }
            record.properties = properties.ToArray();
            if (mesh == null || !capturedMeshes.Add(mesh.GetInstanceID())) continue;
            var prefix = Path.Combine(folder, $"{capture}-mesh-{mesh.GetInstanceID()}");
            try
            {
                var attributes = mesh.GetVertexAttributes();
                var layout = new List<NativeAttributeRecord>();
                foreach (var attribute in attributes)
                    layout.Add(new NativeAttributeRecord { attribute = attribute.attribute.ToString(),
                        format = attribute.format.ToString(), dimension = attribute.dimension, stream = attribute.stream,
                        offset = mesh.GetVertexAttributeOffset(attribute.attribute) });
                var strides = new int[mesh.vertexBufferCount];
                for (int stream = 0; stream < strides.Length; stream++) strides[stream] = mesh.GetVertexBufferStride(stream);
                var submeshes = new NativeSubmeshRecord[mesh.subMeshCount];
                for (int submesh = 0; submesh < submeshes.Length; submesh++)
                {
                    var descriptor = mesh.GetSubMesh(submesh);
                    submeshes[submesh] = new NativeSubmeshRecord { indexStart = descriptor.indexStart,
                        indexCount = descriptor.indexCount, baseVertex = descriptor.baseVertex,
                        firstVertex = descriptor.firstVertex, vertexCount = descriptor.vertexCount,
                        topology = descriptor.topology.ToString(), bounds = descriptor.bounds };
                }
                var meshRecord = new NativeMeshRecord { name = mesh.name,
                    vertices = mesh.vertexCount, indexFormat = mesh.indexFormat.ToString(), attributes = layout.ToArray(),
                    strides = strides, submeshes = submeshes, utc = DateTime.UtcNow.ToString("O") };
                var meshJson = JsonUtility.ToJson(meshRecord, true);
                meshJson = WithArray(meshJson, "attributes", layout.Select(value => JsonUtility.ToJson(value)));
                meshJson = WithArray(meshJson, "submeshes", submeshes.Select(value => JsonUtility.ToJson(value)));
                File.WriteAllText(prefix + ".json", meshJson);
                for (int stream = 0; stream < strides.Length; stream++)
                    ReadBuffer(mesh.GetVertexBuffer(stream), prefix + "-vertex" + stream);
                ReadBuffer(mesh.GetIndexBuffer(), prefix + "-index");
            }
            catch (Exception error)
            {
                File.WriteAllText(prefix + "-error.json", JsonUtility.ToJson(new NativeReadbackRecord { status = "request failed", error = error.Message }));
                Mod.Log.Warn(error, "Unable to capture native batch mesh.");
            }
        }
        var recordJson = JsonUtility.ToJson(new NativeBatchRecords {
            owner = request.owner, utc = DateTime.UtcNow.ToString("O") }, true);
        File.WriteAllText(Path.Combine(folder, capture + "-batches.json"), WithArray(recordJson, "batches",
            records.Select(record => WithArray(JsonUtility.ToJson(record, true), "properties",
                record.properties.Select(property => JsonUtility.ToJson(property))))));

        void ReadBuffer(GraphicsBuffer buffer, string path)
        {
            request.pendingReadbacks++;
            try
            {
                AsyncGPUReadback.Request(buffer, readback =>
                {
                    try
                    {
                        if (readback.hasError)
                            File.WriteAllText(path + ".json", JsonUtility.ToJson(new NativeReadbackRecord { status = "readback failed" }));
                        else
                        {
                            var bytes = readback.GetData<byte>().ToArray();
                            File.WriteAllBytes(path + ".bin", bytes);
                            File.WriteAllText(path + ".json", JsonUtility.ToJson(new NativeReadbackRecord { status = "captured", bytes = bytes.Length }));
                        }
                    }
                    catch (Exception error) { Mod.Log.Warn(error, "Unable to save native mesh readback."); }
                    finally { buffer.Dispose(); request.pendingReadbacks--; complete(); }
                });
            }
            catch
            {
                buffer.Dispose(); request.pendingReadbacks--; throw;
            }
        }
    }

    // Unity's runtime serializer omits arrays of these nested diagnostic records.
    // Serialize each record through the supported native JSON API, then assemble
    // the JSON array explicitly; strings and scalar values remain API-escaped.
    private static string WithArray(string json, string name, IEnumerable<string> records)
        => json.Substring(0, json.LastIndexOf('}')) + ",\"" + name + "\":[" + string.Join(",", records) + "]}";

    private static string RawFloats(float[] values)
    {
        var bytes = new byte[values.Length * sizeof(float)];
        Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
        return Convert.ToBase64String(bytes);
    }

    [Serializable] private sealed class NativeBatchRecords { public string owner = "", utc = ""; }
    [Serializable] private sealed class NativeBatchRecord
    {
        public int batch, group, batchIndex, submesh, sourceSubmesh, meshId, queue;
        public string sourceEntity = "", sharedEntity = "", flags = "", generatedType = "", material = "", shader = "", mesh = "";
        public string[] keywords = Array.Empty<string>();
        [NonSerialized] public NativePropertyRecord[] properties = Array.Empty<NativePropertyRecord>();
    }
    [Serializable] private sealed class NativePropertyRecord
    {
        public string name = "", type = "", texture = "", textureFormat = "", customTexture = "", rawFloatBytes = "", rawCustomFloatBytes = "";
        public bool custom; public int integer, customInteger, width, height;
        public bool streaming; public int mipCount, loadedMip, requestedMip, desiredMip, loadingMip;
        public float[] values = Array.Empty<float>(), customValues = Array.Empty<float>();
    }
    [Serializable] private sealed class NativeAttributeRecord { public string attribute = "", format = ""; public int dimension, stream, offset; }
    [Serializable] private sealed class NativeMeshRecord
    {
        public string name = "", indexFormat = "", utc = ""; public int vertices;
        [NonSerialized] public NativeAttributeRecord[] attributes = Array.Empty<NativeAttributeRecord>();
        public int[] strides = Array.Empty<int>();
        [NonSerialized] public NativeSubmeshRecord[] submeshes = Array.Empty<NativeSubmeshRecord>();
    }
    [Serializable] private sealed class NativeSubmeshRecord
    {
        public int indexStart, indexCount, baseVertex, firstVertex, vertexCount;
        public string topology = ""; public Bounds bounds;
    }
    [Serializable] private sealed class NativeReadbackRecord { public string status = "", error = ""; public int bytes; }
}
