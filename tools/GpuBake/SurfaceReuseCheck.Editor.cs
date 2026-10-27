using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

// Exercise the compiled production composer without publishing a game prefab.
public static class SurfaceReuseCheck
{
    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        string Argument(string key) => args[Array.IndexOf(args, key) + 1];
        var managed = Argument("-gameManaged");
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
        {
            var path = Path.Combine(managed, new AssemblyName(e.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        try
        {
            var mod = Assembly.LoadFrom(Argument("-modAssembly"));
            const BindingFlags fields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Type Type(string name) => mod.GetType("BridgeBuilder.Bridges." + name, true);
            var composer = Type("MovableSurfaceAtlas");
            var inputType = Type("MovableSurfaceAtlas+Input");
            var triangleType = Type("MovableRoadGeometry+Triangle");
            var boundType = Type("MovableRoadGeometry+BoundTriangle");
            var vertexType = Type("MovableRoadGeometry+Vertex");
            var surfaceType = inputType.GetField("Materials", fields).FieldType.GetElementType();
            void Set(object o, string name, object value) => o.GetType().GetField(name, fields).SetValue(o, value);
            object Construct(Type t, params object[] values) => Activator.CreateInstance(t, fields, null, values, null);
            void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
            object Input(Array faces, Array materials)
            {
                var input = Activator.CreateInstance(inputType);
                Set(input, "Faces", faces); Set(input, "Materials", materials);
                return input;
            }
            (Array Faces, Array Materials) Compose(params object[] levels)
            {
                var inputs = Array.CreateInstance(inputType, levels.Length);
                for (int i = 0; i < levels.Length; i++) inputs.SetValue(levels[i], i);
                // Null owner/paint and absent patches make any unnecessary bake fail.
                object[] call = { inputs, null, null, null };
                var result = (Array)composer.GetMethod("Compose", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, call);
                return (result, (Array)call[3]);
            }
            Require(Compose().Faces.Length == 0, "Zero inputs");
            var emptyMaterials = Array.CreateInstance(surfaceType, 0);
            var empty = Input(Array.CreateInstance(boundType, 0), emptyMaterials);
            var emptyResult = Compose(empty, empty, empty);
            Require(emptyResult.Faces.Cast<Array>().All(a => a.Length == 0), "Empty LODs");

            var surface = FormatterServices.GetUninitializedObject(surfaceType);
            using (var reader = new BinaryReader(File.OpenRead(Argument("-surfaceFile"))))
                surfaceType.GetMethod("ReadHeader", fields).Invoke(surface, new object[] { reader });
            var originals = Array.CreateInstance(surfaceType, 3);
            originals.SetValue(surface, 0); originals.SetValue(surface, 2);
            object Vertex(float x, float z) => Construct(vertexType, new Vector3(x, 2, z), Vector3.up,
                new Vector4(1, 0, 0, -1), new Vector4(x * .31f, z * .47f, .25f, .75f), new Color(.2f, .3f, .4f, .5f));
            var triangle = Construct(triangleType, Vertex(-5, 3), Vertex(8, 3), Vertex(-5, 17), 2, 7, 19);
            var faces = Array.CreateInstance(boundType, 1);
            faces.SetValue(Construct(boundType, triangle, uint.MaxValue), 0);
            int cases = 0;
            foreach (int template in new[] { 0, 1, 11, 13, 14, 35, 36 })
            {
                Set(surface, "m_MaterialTemplateHash", template);
                var result = Compose(Input(faces, originals), empty, Input(faces, originals));
                foreach (int level in new[] { 0, 2 })
                {
                    var output = (Array)result.Faces.GetValue(level);
                    Require(output.Length == 1 && triangle.Equals(output.GetValue(0)), "Complete geometry/UV/provenance changed");
                    Require(ReferenceEquals(originals, result.Materials.GetValue(level)), "Original material slots changed");
                }
                Require(((Array)result.Faces.GetValue(1)).Length == 0, "Mixed empty LOD");
                cases++;
            }
            Debug.Log($"PASS original surface reuse: compiled composer; {cases} object templates; empty inputs/LODs; exact vertex/UV/color/material slot/provenance retention; no owner or GPU baking.");
            CheckAppearanceKeys(mod,surface);
            CheckConstantControlRouting(mod,surface);
            CheckAuthoredTextures(mod);
            CheckStructuralSelection(mod,surface);
        }
        catch (Exception e) { Debug.LogException(e); UnityEditor.EditorApplication.Exit(1); }
    }

    private static void CheckStructuralSelection(Assembly mod,object source)
    {
        const BindingFlags fields=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        const BindingFlags statics=BindingFlags.NonPublic|BindingFlags.Static;
        var type=mod.GetType("BridgeBuilder.Bridges.MovableStructureTiles",true);
        var unitType=mod.GetType("BridgeBuilder.Bridges.MovableStructurePacking+Unit",true);
        var unit=Activator.CreateInstance(unitType,true);
        unitType.GetField("Id",fields).SetValue(unit,17);
        unitType.GetField("Triangle",fields).SetValue(unit,true);
        unitType.GetField("B",fields).SetValue(unit,Vector2.right);
        unitType.GetField("C",fields).SetValue(unit,Vector2.up);
        var points=(System.Collections.Generic.List<Vector2>)unitType.GetField("Points",fields).GetValue(unit);
        points.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.up});
        var units=Array.CreateInstance(unitType,1);units.SetValue(unit,0);
        var instance=Activator.CreateInstance(type,fields,null,new object[]{"Draw01"},null);
        var build=type.GetMethod("Build",fields);
        // This header-only source has no file provider: unchanged original reuse
        // must not open it or require a prepared-material installation.
        var result=(System.Collections.IDictionary)build.Invoke(instance,new[]{source,units});
        var slot=result[17];
        if(result.Count!=1 || !ReferenceEquals(slot.GetType().GetField("Surface",fields).GetValue(slot),source))
            throw new Exception("Structural original material was replaced");
        var uv=new Vector2(.25f,.5f);
        if(!uv.Equals(slot.GetType().GetMethod("Map",fields).Invoke(slot,new object[]{uv})))
            throw new Exception("Structural original UV mapping changed");

          // Exercise the actual production catalogue selector, not only the
          // polygon predicate. This face crosses the old sampler's diagonal but
          // remains in its recorded native chart and needs no pixel resources.
          var chartType=mod.GetType("BridgeBuilder.Bridges.MovableUvChart",true);
          var chart=chartType.GetMethod("Read",statics).Invoke(null,new object[]{
              new[]{1,0,4},new double[]{0,0,4,0,4,4,0,4},0});
          var charts=Array.CreateInstance(chartType,1);charts.SetValue(chart,0);
          unitType.GetField("Charts",fields).SetValue(unit,charts);
          points.Clear();points.AddRange(new[]{Vector2.zero,Vector2.right*2,Vector2.up*2});
          result=(System.Collections.IDictionary)build.Invoke(instance,new[]{source,units});
          slot=result[17];
          if(!ReferenceEquals(slot.GetType().GetField("Surface",fields).GetValue(slot),source) ||
              !new Vector2(2,0).Equals(slot.GetType().GetMethod("Map",fields).Invoke(slot,new object[]{new Vector2(2,0)})))
              throw new Exception("True native chart did not reuse original Surface and atlas UVs");
          Debug.Log("PASS structural native charts: compiled production selector reuses original Surface outside the old single-face domain without source IO or pixel creation.");

        // Force a genuine repeat with a known source digest. A missing immutable
        // resource must fail before native registration or any replacement bake.
        unitType.GetField("RepeatOnly",fields).SetValue(unit,true);
        var digests=(System.Collections.IDictionary)type.GetField("_sourceDigests",fields).GetValue(instance);
        digests.Add(source,new string('0',64));
        var catalogue=mod.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials",true);
        var root=catalogue.GetField("_root",statics);var previous=root.GetValue(null);
        var missing=Path.Combine(Path.GetTempPath(),"BridgeBuilderAbsentPrepared-"+Guid.NewGuid().ToString("N"));
        root.SetValue(null,missing);
        try
        {
            try { build.Invoke(instance,new[]{source,units}); }
            catch(TargetInvocationException e) when(e.GetBaseException() is IOException)
            {
                if(Directory.Exists(missing))throw new Exception("Missing catalogue was written at runtime");
                Debug.Log("PASS structural material selection: compiled original identity/UV reuse without source IO; missing prepared repeat fails without creating resources.");
                return;
            }
            throw new Exception("Missing structural input did not fail");
        }
        finally{root.SetValue(null,previous);}
    }

    private static void CheckAuthoredTextures(Assembly mod)
    {
        const BindingFlags fields=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        var type=mod.GetType("BridgeBuilder.Bridges.MovableSurfaceReader",true);
        var instance=FormatterServices.GetUninitializedObject(type);
        var material=new Material(Shader.Find("Unlit/Texture"));
        var texture=new Texture2D(1,1);
        try
        {
            type.GetField("<Material>k__BackingField",fields).SetValue(instance,material);
            var owned=new System.Collections.Generic.HashSet<Texture>();
            type.GetField("_textures",fields).SetValue(instance,owned);
            bool Has(string property)=>(bool)type.GetMethod("HasAuthoredTexture",fields).Invoke(instance,new object[]{property});
            void Require(bool ok,string reason){if(!ok)throw new Exception(reason);}
            Require(!Has("_MissingProperty"),"Undeclared property cannot be authored");
            material.SetTexture("_MainTex",Texture2D.blackTexture);
            Require(!Has("_MainTex"),"Template/default black must not count as authored");
            owned.Add(texture);material.SetTexture("_MainTex",texture);
            Require(Has("_MainTex"),"Decoded active VT or ordinary source binding must count");
            material.SetTexture("_MainTex",Texture2D.whiteTexture);
            Require(!Has("_MainTex"),"Unbound owned texture must not affect another binding");
            material.SetTexture("_MainTex",null);
            Require(!Has("_MainTex"),"Null binding cannot be authored");
            Debug.Log("PASS authored texture detection: 5 compiled reader checks; source-owned bindings only, including active VT layers without ordinary bindings.");
        }
        finally{UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(texture);}
    }

    private static void CheckConstantControlRouting(Assembly mod,object source)
    {
        const BindingFlags fields=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        var atlas=mod.GetType("BridgeBuilder.Bridges.MovableSurfaceAtlas",true);
        var chartType=atlas.GetNestedType("Chart",BindingFlags.NonPublic);
        var predicate=atlas.GetMethod("UsesZeroControl",BindingFlags.Static|BindingFlags.NonPublic);
        var setType=typeof(System.Collections.Generic.HashSet<>).MakeGenericType(source.GetType());
        var controls=Activator.CreateInstance(setType);
        void Set(object c,string field,object value)=>chartType.GetField(field,fields).SetValue(c,value);
        object Chart()
        {
            var c=Activator.CreateInstance(chartType,true);
            Set(c,"Source",source);Set(c,"Periodic",true);Set(c,"W",768);Set(c,"H",772);return c;
        }
        bool Uses(object[] charts,bool paint=false)
        {
            var page=Array.CreateInstance(chartType,charts.Length);
            for(int i=0;i<charts.Length;i++)page.SetValue(charts[i],i);
            return (bool)predicate.Invoke(null,new object[]{page,768,772,paint,controls});
        }
        int checks=0;
        void Require(bool ok,string reason){if(!ok)throw new Exception(reason);checks++;}
        Require(Uses(new[]{Chart()}),"Full unpainted periodic page must reuse zero control");
        var repeat=Chart();Set(repeat,"Periodic",false);Set(repeat,"Repeat",true);
        Require(Uses(new[]{repeat}),"Full repeat page must reuse zero control");
        Require(!Uses(Array.Empty<object>()),"Empty page cannot reuse control");
        Require(!Uses(new[]{Chart(),Chart()}),"Multiple chart coverage needs separate proof");
        Require(!Uses(new[]{Chart()},true),"Paint must retain composed control");
        foreach(var field in new[]{"X","Y","W","H"})
        {
            var c=Chart();Set(c,field,1);
            Require(!Uses(new[]{c}),"Partial or offset chart must retain composed control: "+field);
        }
        var finite=Chart();Set(finite,"Periodic",false);
        Require(!Uses(new[]{finite}),"Finite VT page must retain composed control");
        setType.GetMethod("Add").Invoke(controls,new[]{source});
        Require(!Uses(new[]{Chart()}),"Authored control must never be replaced by zero");
        Require(!Uses(new[]{repeat}),"Authored repeat control must never be replaced by zero");
        Debug.Log($"PASS constant control routing: {checks} compiled production checks; full periodic/repeat pages only; paint, authored controls, partial coverage and VT remain composed.");
    }

    private static void CheckAppearanceKeys(Assembly mod,object source)
    {
        const BindingFlags fields=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        Type Type(string name)=>mod.GetType("BridgeBuilder.Bridges."+name,true);
        var chartType=Type("MovableSurfaceAtlas+Chart");
        var patchType=Type("MovableExistingRoadSurface+Patch");
        var method=chartType.GetMethod("NativePeriodicKey",fields);
        void Set(object o,string name,object value)=>o.GetType().GetField(name,fields).SetValue(o,value);
        object Patch(Vector4 u,Vector4 v)=>Activator.CreateInstance(patchType,fields,null,
            new object[]{Vector2.zero,Vector2.one,u,v,null,false,null,false},null);
        var pu=new Vector4(.25f,0,0,.125f);var pv=new Vector4(0,0,.125f,-.375f);
        object Chart(float y)
        {
            var c=Activator.CreateInstance(chartType,true);
            Set(c,"Source",source);Set(c,"Frame",Matrix4x4.identity);
            Set(c,"Origin",new Vector3(0,y,0));Set(c,"U",Vector3.right);Set(c,"V",Vector3.forward);
            Set(c,"Min",new Vector2(-13.25f,-27.5f));Set(c,"Max",new Vector2(13.25f,4.5f));
            Set(c,"W",1024);Set(c,"H",2048);Set(c,"Density",new Vector2(80,64));
            Set(c,"Patch",Patch(pu,pv));return c;
        }
        object Key(object c)=>method.Invoke(c,new object[]{8});
        int checks=0;
        void Require(bool ok,string reason){if(!ok)throw new Exception(reason);checks++;}
        object baseline=Key(Chart(2));
        var pages=new System.Collections.Generic.Dictionary<object,int>();
        foreach(float y in new[]{-64f,-1.25f,0f,2f,19.5f,64f})
        {
            var c=Chart(y);var key=Key(c);pages[key]=0;
            Require(baseline.Equals(key),"Unused LOD elevation prevented exact sharing");
            // Compare actual sampled coordinates, not a second implementation of the key.
            // Include pixel centres and both transverse guard bands of the native kernel.
            foreach(int x in new[]{0,7,8,511,1015,1023})
            foreach(int z in new[]{0,1,1024,2047})
            {
                Vector3 Position(float height)=>new Vector3(-13.25f-8f/80f,height,-27.5f)
                    +Vector3.right*((x+.5f)/80f)+Vector3.forward*((z+.5f)/64f);
                var a=Position(2);var b=Position(y);
                var ah=new Vector4(a.x,a.y,a.z,1);var bh=new Vector4(b.x,b.y,b.z,1);
                Require(Vector4.Dot(ah,pu).Equals(Vector4.Dot(bh,pu)) &&
                    Vector4.Dot(ah,pv).Equals(Vector4.Dot(bh,pv)) && a.x.Equals(b.x) && a.z.Equals(b.z),
                    "Shared LOD altered native UV or worldspace sample");
            }
        }
        Require(pages.Count==1,"Equal recipes allocated multiple dictionary pages");
        void Distinct(string label,Action<object> change)
        {
            var c=Chart(2);change(c);Require(!baseline.Equals(Key(c)),"Recipe collision: "+label);
        }
        var otherSource=FormatterServices.GetUninitializedObject(source.GetType());
        var sourceFields=new System.Collections.Generic.List<FieldInfo>();
        for(var t=source.GetType();t!=null;t=t.BaseType)
            sourceFields.AddRange(t.GetFields(fields|BindingFlags.DeclaredOnly));
        var idField=sourceFields.Single(f=>f.Name=="m_Id");
        var identifier=Activator.CreateInstance(idField.FieldType);
        var ids=idField.FieldType.GetFields(fields).Where(f=>f.FieldType.FullName=="Colossal.Hash128").ToArray();
        Require(ids.Length==1,"Expected one Identifier hash: "+string.Join(",",idField.FieldType.GetFields(fields).Select(f=>f.Name+":"+f.FieldType.FullName)));
        ids[0].SetValue(identifier,ids[0].FieldType.GetMethod("Parse",new[]{typeof(string)})
            .Invoke(null,new object[]{"7dc9f20b67894244bd7cd93e252ecded"}));
        idField.SetValue(otherSource,identifier);
        Distinct("source",c=>Set(c,"Source",otherSource));
        Distinct("width",c=>Set(c,"W",1028));Distinct("height",c=>Set(c,"H",2052));
        Distinct("density",c=>Set(c,"Density",new Vector2(80.00001f,64)));
        Distinct("orientation",c=>Set(c,"U",Vector3.left));
        Distinct("phase",c=>Set(c,"Min",new Vector2(-13.249999f,-27.5f)));
        Distinct("origin x",c=>Set(c,"Origin",new Vector3(.001f,2,0)));
        Distinct("origin z",c=>Set(c,"Origin",new Vector3(0,2,.001f)));
        Distinct("source scale",c=>Set(c,"Patch",Patch(new Vector4(.25000003f,0,0,.125f),pv)));
        Distinct("source phase ULP",c=>Set(c,"Patch",Patch(new Vector4(.25f,0,0,.125000015f),pv)));
        foreach(bool world in new[]{false,true})
        foreach(int channel in new[]{0,2})
        {
            var a=Chart(2);var b=Chart(3);
            if(world)
            {
                var f=Matrix4x4.identity;f[channel,1]=.0000001f;
                Set(a,"Frame",f);Set(b,"Frame",f);
            }
            else
            {
                var u=pu;var v=pv;if(channel==0)u.y=.0000001f;else v.y=.0000001f;
                Set(a,"Patch",Patch(u,v));Set(b,"Patch",Patch(u,v));
            }
            Require(!Key(a).Equals(Key(b)),"Nonzero height-dependent sampling was discarded");
        }
        var appearance=Type("MovableAppearanceUnit").GetMethod("Key",BindingFlags.NonPublic|BindingFlags.Static);
        var paint=Array.CreateInstance(Type("MovableNativeDecals+Region"),0);
        object Finite(float phase)=>appearance.Invoke(null,new object[]{Vector3.zero,Vector3.right,
            Vector3.forward,Vector2.one,Patch(new Vector4(.25f,0,0,phase),pv),paint});
        Require(!Finite(.125f).Equals(Finite(.125000015f)),"Finite key quantized different source phases");
        Require(Finite(.125f).Equals(Finite(.125f)),"Identical finite recipe changed identity");
        Debug.Log($"PASS appearance recipe keys: {checks} checks; native sample coordinates, dictionary sharing, source/phase/density/frame isolation and finite one-ULP distinction. Synthetic receivers; no game timing or full Compose coverage.");
    }
}
