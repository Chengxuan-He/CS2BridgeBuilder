using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class Nested161Check
{
    [System.Runtime.InteropServices.DllImport("kernel32", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern IntPtr LoadLibraryW(string path);
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct PackedVertex { public Vector3 Position; public uint Normal; public float Tangent; public Vector2 Uv; public uint Bone; }
    const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static Assembly game, mod;
    static Type G(string name) => game.GetType("Game.Prefabs." + name, true);
    static object Get(object o, string n) => o.GetType().GetField(n, F).GetValue(o);
    static void Set(object o, string n, object v) => o.GetType().GetField(n, F).SetValue(o, v);
    static object Add(object p, string n) => p.GetType().GetMethods(F).Single(m => m.Name == "AddComponent" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0).MakeGenericMethod(G(n)).Invoke(p, null);
    static object Prefab(string name, bool color = false)
    {
        var p = ScriptableObject.CreateInstance(G("StaticObjectPrefab")); p.name = name;
        var render = ScriptableObject.CreateInstance(G("RenderPrefab")); render.name = name + " render";
        render.GetType().GetProperty("meshCount").SetValue(render, 1);
        render.GetType().GetProperty("indexCount").SetValue(render, 3);
        render.GetType().GetProperty("vertexCount").SetValue(render, 3);
        if (color) Add(render, "ColorProperties");
        var mesh = Activator.CreateInstance(G("ObjectMeshInfo")); Set(mesh, "m_Mesh", render);
        var array = Array.CreateInstance(mesh.GetType(), 1); array.SetValue(mesh, 0); Set(p, "m_Meshes", array);
        return p;
    }
    static object Record(object p, Vector3 position, Quaternion rotation, int probability = 100, int parent = 0)
    {
        var r = Activator.CreateInstance(G("ObjectSubObjectInfo")); Set(r, "m_Object", p);
        Set(r, "m_Position", Activator.CreateInstance(r.GetType().GetField("m_Position").FieldType, new object[] { position.x, position.y, position.z }));
        Set(r, "m_Rotation", Activator.CreateInstance(r.GetType().GetField("m_Rotation").FieldType, new object[] { rotation.x, rotation.y, rotation.z, rotation.w }));
        Set(r, "m_Probability", probability); Set(r, "m_ParentMesh", parent); Set(r, "m_GroupIndex", 7);
        return r;
    }
    static void Children(object p, params object[] records)
    {
        var c = Add(p, "ObjectSubObjects"); var a = Array.CreateInstance(G("ObjectSubObjectInfo"), records.Length);
        for (int i = 0; i < records.Length; i++) a.SetValue(records[i], i); Set(c, "m_SubObjects", a);
    }
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        try
        {
            string Arg(string n) => Environment.GetCommandLineArgs().SkipWhile(x => x != n).Skip(1).First();
            var managed = Arg("-gameManaged");
            Require(LoadLibraryW(Path.GetFullPath(Path.Combine(managed, "../Plugins/x86_64/PipelinePlugin.dll"))) != IntPtr.Zero, "native pipeline unavailable");
            AppDomain.CurrentDomain.AssemblyResolve += (_, e) => { var p = Path.Combine(managed, new AssemblyName(e.Name).Name + ".dll"); return File.Exists(p) ? Assembly.LoadFrom(p) : null; };
            game = Assembly.Load("Game"); mod = Assembly.LoadFrom(Arg("-candidate"));
            var planner = mod.GetType("BridgeBuilder.Bridges.MovableNestedRoadObjects", true);
            var root = Prefab("root"); var child = Prefab("child"); var leaf = Prefab("leaf");
            var colored = Prefab("color", true); var random = Prefab("probability");
            var group = ScriptableObject.CreateInstance(G("GroupPrefab"));
            var spawn = Add(child, "SpawnableObject"); Set(spawn, "m_RandomizationGroup", group);
            Children(child, Record(leaf, new Vector3(2, 3, 4), Quaternion.Euler(20, 0, 30), parent: 100));
            Children(root, Record(child, new Vector3(4, 5, 6), Quaternion.Euler(0, 90, 0)),
                Record(colored, Vector3.one, Quaternion.identity), Record(random, Vector3.zero, Quaternion.identity, 37),
                Record(child, new Vector3(-4, 0, 0), Quaternion.identity));
            var plan = planner.GetMethod("Plan", F).Invoke(null, new[] { root });
            var visuals = ((IEnumerable)planner.GetMethod("Visuals", F).Invoke(null, new[] { plan })).Cast<object>().ToArray();
            Require(visuals.Length == 5, "repeated ordinary branch lost or exceptional branch merged");
            var expected = Matrix4x4.TRS(new Vector3(4, 5, 6), Quaternion.Euler(0, 90, 0), Vector3.one) * Matrix4x4.TRS(new Vector3(2, 3, 4), Quaternion.Euler(20, 0, 30), Vector3.one);
            Require(((Matrix4x4)Get(visuals[2], "Transform")).Equals(expected), "nested native local TR composed incorrectly");
            var created = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(G("PrefabBase")));
            var cache = Activator.CreateInstance(planner.GetMethod("Carrier", F).GetParameters()[3].ParameterType);
            var carrier = planner.GetMethod("Carrier", F).Invoke(null, new[] { (object)"tmp11111111-2222-4333-8444-555555555555", plan, created, cache });
            object Component(object p, string name) => p.GetType().GetMethods(F).Single(m => m.Name == "GetComponent" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0).MakeGenericMethod(G(name)).Invoke(p, null);
            var entries = (Array)Get(Component(carrier, "ObjectSubObjects"), "m_SubObjects");
            Require(ReferenceEquals(Get(entries.GetValue(1), "m_Object"), colored), "colored native branch changed");
            Require(ReferenceEquals(Get(entries.GetValue(2), "m_Object"), random) && (int)Get(entries.GetValue(2), "m_Probability") == 37 && (int)Get(entries.GetValue(2), "m_GroupIndex") == 7, "native probability/group changed");
            Require(ReferenceEquals(Get(entries.GetValue(0), "m_Object"), Get(entries.GetValue(3), "m_Object")), "same source carrier not shared");
            var childCarrier = Get(entries.GetValue(0), "m_Object");
            Require(ReferenceEquals(Get(Component(childCarrier, "SpawnableObject"), "m_RandomizationGroup"), group), "explicit native random group changed");
            foreach (var p in created.Cast<object>().Where(p => G("RenderPrefab").IsInstanceOfType(p)))
                Require((int)p.GetType().GetProperty("meshCount").GetValue(p) == 0, "carrier kept duplicate visible geometry");
            Require(ReferenceEquals(Get(((Array)Get(Component(root, "ObjectSubObjects"), "m_SubObjects")).GetValue(0), "m_Object"), child), "source hierarchy mutated");
            var again = planner.GetMethod("Carrier", F).Invoke(null, new[] { (object)"same-owner", plan, created, cache });
            Require(ReferenceEquals(carrier, again), "request carrier identity not reused");
            var cycle = Prefab("cycle"); Children(cycle, Record(cycle, Vector3.zero, Quaternion.identity));
            bool rejected = false; try { planner.GetMethod("Plan", F).Invoke(null, new[] { cycle }); } catch (TargetInvocationException e) { rejected = e.InnerException is InvalidOperationException; }
            Require(rejected, "ancestry cycle was not rejected");
            var empty = Prefab("meshless"); Set(empty, "m_Meshes", null);
            Children(empty, Record(leaf, Vector3.one, Quaternion.identity));
            var emptyPlan = planner.GetMethod("Plan", F).Invoke(null, new[] { empty });
            Require((bool)Get(emptyPlan, "Merged") && ((IEnumerable)planner.GetMethod("Visuals", F).Invoke(null, new[] { emptyPlan })).Cast<object>().Count() == 2, "meshless container lost ordinary descendant");
            Children(colored, Record(leaf, Vector3.one, Quaternion.identity));
            var input = Array.CreateInstance(G("ObjectSubObjectInfo"), 2);
            input.SetValue(Record(colored, Vector3.zero, Quaternion.identity), 0); input.SetValue(Record(leaf, Vector3.zero, Quaternion.identity), 1);
            var sharedPlans = (Array)planner.GetMethod("Plans", F).Invoke(null, new object[] { input, null });
            Require((bool)Get(sharedPlans.GetValue(1), "Merged"), "unrelated deterministic leaf unnecessarily retained with native branch");
            var nativeSharing = Prefab("implicit group with conditional child");
            Children(nativeSharing, Record(random, Vector3.zero, Quaternion.identity, 37));
            input.SetValue(Record(nativeSharing, Vector3.zero, Quaternion.identity), 0);
            input.SetValue(Record(nativeSharing, Vector3.one, Quaternion.identity), 1);
            var stationarySharing = (Array)planner.GetMethod("Plans", F).Invoke(null, new object[] { input, new[] { 0, 1 } });
            Require(!(bool)Get(stationarySharing.GetValue(1), "Merged"), "stationary source identity forked an implicit conditional random group");
            var alternate = planner.GetMethod("Plan", F).Invoke(null, new[] { nativeSharing });
            var alternateCarrier = planner.GetMethod("Carrier", F).Invoke(null, new[] { (object)"other-plan", alternate, created, cache });
            var retained = stationarySharing.GetValue(1);
            Require(ReferenceEquals(planner.GetMethod("Carrier", F).Invoke(null, new[] { (object)"retained-plan", retained, created, cache }), nativeSharing), "different plan reused a metadata-only carrier");
            foreach (var componentName in new[] { "EmissiveProperties", "ColorProperties" })
            {
                var exception = Prefab(componentName);
                Add(Get(((Array)Get(exception, "m_Meshes")).GetValue(0), "m_Mesh"), componentName);
                Require(!(bool)Get(planner.GetMethod("Plan", F).Invoke(null, new[] { exception }), "Merged"), componentName + " exception merged");
            }
            var stationary = Prefab("empty animation component");
            var staticAnimation = Add(Get(((Array)Get(stationary, "m_Meshes")).GetValue(0), "m_Mesh"), "ProceduralAnimationProperties");
            Require((bool)Get(planner.GetMethod("Plan", F).Invoke(null, new[] { stationary }), "Merged"), "component presence incorrectly classified as independent animation");
            var boneField = staticAnimation.GetType().GetField("m_Bones"); var staticBones = Array.CreateInstance(boneField.FieldType.GetElementType(), 1);
            var staticBone = Activator.CreateInstance(boneField.FieldType.GetElementType());
            Set(staticBone, "position", Vector3.right); Set(staticBone, "rotation", Quaternion.identity); Set(staticBone, "scale", new Vector3(2, 1, 1));
            Set(staticBone, "parentId", -1); Set(staticBone, "m_ConnectionID", 100); Set(staticBone, "bindPose", Matrix4x4.Translate(Vector3.right * .5f));
            staticBones.SetValue(staticBone, 0); boneField.SetValue(staticAnimation, staticBones);
            Require((bool)Get(planner.GetMethod("Plan", F).Invoke(null, new[] { stationary }), "Merged"), "nonidentity stationary rest classified as independent animation");
            var connected = Record(leaf, Vector3.right * 5, Quaternion.identity, parent: 100);
            var connectedTransform = (Matrix4x4)planner.GetMethod("ChildTransform", F).Invoke(null, new[] { stationary, connected });
            Require((connectedTransform.MultiplyPoint3x4(Vector3.zero) - Vector3.right * 9).sqrMagnitude < 1e-10f, "static parent connection missed native inverse-bind/current-pose composition");
            var dependencies = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(G("PrefabBase")));
            var childComponent = Component(carrier, "ObjectSubObjects");
            childComponent.GetType().GetMethod("GetDependencies", F).Invoke(childComponent, new object[] { dependencies });
            Require(dependencies.Cast<object>().Contains(childCarrier) && dependencies.Cast<object>().Contains(colored) && dependencies.Cast<object>().Contains(random), "native persistence dependency graph omitted a retained or merged child");
            var previewLayout = mod.GetType("BridgeBuilder.Runtime.BridgePreviewObjectLayout", true);
            object[] layoutArgs = { carrier, null };
            Require((bool)previewLayout.GetMethod("TryCreate", F).Invoke(null, layoutArgs), "metadata-only carrier broke preview layout");
            NativeModels(planner, visuals);
            Debug.Log("PASS nested native-type production plan: recursive rotated TR, repeated references, parent connection records, conditional-color/probability branches, metadata-only carrier graph reuse, unchanged source, cycle detection. No live-game animation acceptance claimed.");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    static void NativeModels(Type planner, object[] visuals)
    {
        var modelType = mod.GetType("BridgeBuilder.Bridges.MovableStructureModel", true);
        for (int lod = 0; lod < 3; lod++)
        {
            var mesh = new Mesh();
            var positions = Enumerable.Range(0, 3 * (lod + 1)).Select(i => new Vector3(i % 3, i / 3, i % 2)).ToArray();
            var math = Assembly.Load("Unity.Mathematics");
            var utility = Assembly.Load("Colossal.Mathematics").GetType("Colossal.Mathematics.MathUtils");
            var normal = Convert.ToUInt32(utility.GetMethod("NormalToOctahedral", F).Invoke(null, new[] { Activator.CreateInstance(math.GetType("Unity.Mathematics.float3"), new object[] { .70710677f, .70710677f, 0f }) }));
            var tangent = Convert.ToUInt32(utility.GetMethod("TangentToOctahedral", F).Invoke(null, new[] { Activator.CreateInstance(math.GetType("Unity.Mathematics.float4"), new object[] { .70710677f, -.70710677f, 0f, 1f }) }));
            mesh.SetVertexBufferParams(positions.Length,
                new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Position, UnityEngine.Rendering.VertexAttributeFormat.Float32, 3),
                new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Normal, UnityEngine.Rendering.VertexAttributeFormat.SNorm16, 2),
                new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Tangent, UnityEngine.Rendering.VertexAttributeFormat.Float32, 1),
                new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.TexCoord0, UnityEngine.Rendering.VertexAttributeFormat.Float32, 2),
                new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.BlendIndices, UnityEngine.Rendering.VertexAttributeFormat.UInt32, 1));
            mesh.SetVertexBufferData(positions.Select((p, i) => new PackedVertex { Position = p, Normal = normal,
                Tangent = BitConverter.ToSingle(BitConverter.GetBytes(tangent), 0), Uv = new Vector2(p.x * .37f, p.y * .21f), Bone = 0xAABBCC00u | (uint)(i % 2) }).ToArray(), 0, 0, positions.Length);
            mesh.subMeshCount = 2; mesh.SetIndices(Enumerable.Range(0, positions.Length).ToArray(), UnityEngine.MeshTopology.Triangles, 0);
            mesh.SetIndices(Enumerable.Range(0, positions.Length).Reverse().ToArray(), UnityEngine.MeshTopology.Triangles, 1);
            object animation = null; Matrix4x4[] skin = null;
            if (lod > 0)
            {
                animation = ScriptableObject.CreateInstance(G("ProceduralAnimationProperties"));
                var field = animation.GetType().GetField("m_Bones"); var bones = Array.CreateInstance(field.FieldType.GetElementType(), 2);
                var objects = new[] { new GameObject("rest parent"), new GameObject("rest child") };
                objects[1].transform.SetParent(objects[0].transform, false);
                for (int i = 0; i < 2; i++)
                {
                    var bone = Activator.CreateInstance(field.FieldType.GetElementType());
                    var position = new Vector3(i + 1, 2 - i, 3); var rotation = Quaternion.Euler(10 * i, 20 + i * 30, 5);
                    var scale = new Vector3(1 + .25f * i, 1, 1);
                    Set(bone, "position", position); Set(bone, "rotation", rotation); Set(bone, "scale", scale);
                    Set(bone, "parentId", i - 1); Set(bone, "bindPose", Matrix4x4.Translate(new Vector3(.5f, 0, 0)));
                    bones.SetValue(bone, i); objects[i].transform.localPosition = position; objects[i].transform.localRotation = rotation; objects[i].transform.localScale = scale;
                }
                field.SetValue(animation, bones); skin = (Matrix4x4[])planner.GetMethod("StaticSkin", F).Invoke(null, new[] { animation });
                for (int i = 0; i < 2; i++)
                    Require((skin[i].GetColumn(3) - (objects[i].transform.localToWorldMatrix * (Matrix4x4)Get(bones.GetValue(i), "bindPose")).GetColumn(3)).sqrMagnitude < 1e-10f, "static rest differs from native debug Transform hierarchy");
                UnityEngine.Object.DestroyImmediate(objects[0]);
            }
            foreach (var visual in visuals)
            {
                var transform = (Matrix4x4)Get(visual, "Transform");
                var plans = (Array)planner.GetMethod("MeshPlans", F).Invoke(null, new object[] { mesh, transform, animation });
                for (int slot = 0; slot < 2; slot++)
                {
                    object[] args = { "nested-lod", mesh, plans.GetValue(slot), 3, null, null, false, (int?)2, (int?)slot };
                    Require((bool)modelType.GetMethod("TryBuild", F).Invoke(null, args), "native nested model: " + args[5]);
                    var model = args[4]; var convert = model.GetType().GetMethod("ToUnityMesh", F);
                    var output = (Mesh)convert.Invoke(model, new object[] { true });
                    try
                    {
                        Require(output.vertices.SequenceEqual(positions.Select((p, i) => (skin == null ? transform : transform * skin[i % 2]).MultiplyPoint3x4(p))), "nested encoded position changed");
                        if (skin != null)
                        {
                            var decodedNormals = DecodeNormals(output);
                            for (int i = 0; i < output.vertexCount; i++)
                            {
                                var expectedNormal = (transform * skin[i % 2]).MultiplyVector(new Vector3(.70710677f, .70710677f, 0)).normalized;
                                Require((decodedNormals[i] - expectedNormal).sqrMagnitude < 1e-7f,
                                    "native forward bone normal semantics changed lod=" + lod + " vertex=" + i + " actual=" + decodedNormals[i].ToString("R") + " expected=" + expectedNormal.ToString("R"));
                            }
                        }
                        Require(output.uv.SequenceEqual(mesh.uv) && output.triangles.SequenceEqual(mesh.GetIndices(slot)), "nested UV/index changed");
                        using (var data = Mesh.AcquireReadOnlyMeshData(output))
                        {
                            var stream = output.GetVertexAttributeStream(UnityEngine.Rendering.VertexAttribute.BlendIndices);
                            var offset = output.GetVertexAttributeOffset(UnityEngine.Rendering.VertexAttribute.BlendIndices);
                            var stride = output.GetVertexBufferStride(stream); var bytes = data[0].GetVertexData<byte>(stream).ToArray();
                            for (int i = 0; i < output.vertexCount; i++) Require(BitConverter.ToUInt32(bytes, i * stride + offset) == 2, "nested bone changed");
                        }
                    }
                    finally { UnityEngine.Object.DestroyImmediate(output); ((IDisposable)model).Dispose(); }
                }
            }
            UnityEngine.Object.DestroyImmediate(mesh);
        }
        Debug.Log("PASS nested production MeshPlans/native model roundtrip: three distinct LOD sizes, repeated recursive transforms, two opposite-winding material slots, exact positions/UV/indices and rigid mechanism bone2.");
    }

    static Vector3[] DecodeNormals(Mesh mesh)
    {
        int stream = mesh.GetVertexAttributeStream(UnityEngine.Rendering.VertexAttribute.Normal);
        int offset = mesh.GetVertexAttributeOffset(UnityEngine.Rendering.VertexAttribute.Normal), stride = mesh.GetVertexBufferStride(stream);
        var packed = new byte[mesh.vertexCount * 4];
        using (var data = Mesh.AcquireReadOnlyMeshData(mesh))
        {
            var bytes = data[0].GetVertexData<byte>(stream);
            for (int i = 0; i < mesh.vertexCount; i++) for (int k = 0; k < 4; k++) packed[i * 4 + k] = bytes[i * stride + offset + k];
        }
        var input = System.Runtime.InteropServices.Marshal.AllocHGlobal(packed.Length);
        var output = System.Runtime.InteropServices.Marshal.AllocHGlobal(mesh.vertexCount * 12);
        try
        {
            System.Runtime.InteropServices.Marshal.Copy(packed, 0, input, packed.Length);
            Assembly.Load("Colossal.AssetPipeline.Native").GetType("Colossal.AssetPipeline.Native.NativeMath").GetMethod("ArrayOctahedralToNormals", F).Invoke(null, new object[] { input, mesh.vertexCount, output });
            var values = new float[mesh.vertexCount * 3]; System.Runtime.InteropServices.Marshal.Copy(output, values, 0, values.Length);
            return Enumerable.Range(0, mesh.vertexCount).Select(i => new Vector3(values[i * 3], values[i * 3 + 1], values[i * 3 + 2])).ToArray();
        }
        finally { System.Runtime.InteropServices.Marshal.FreeHGlobal(input); System.Runtime.InteropServices.Marshal.FreeHGlobal(output); }
    }
}
