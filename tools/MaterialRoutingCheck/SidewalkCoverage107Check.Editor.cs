using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class SidewalkCoverage107Check
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    public static void Run()
    {
        try
        {
            string Arg(string name)=>Environment.GetCommandLineArgs().SkipWhile(a=>a!=name).Skip(1).First();
            var managed=Arg("-gameManaged");
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
            var asm=Assembly.LoadFrom(Arg("-candidate"));
            Type T(string n)=>asm.GetType("BridgeBuilder.Bridges."+n,true);
            object New(Type t,params object[] a)=>Activator.CreateInstance(t,F,null,a,null);
            object Get(object o,string f)=>o.GetType().GetField(f,F).GetValue(o);
            void Check(bool ok,string m){if(!ok)throw new Exception(m);}
            var vertex=T("MovableRoadGeometry+Vertex");var triangle=T("MovableRoadGeometry+Triangle");
            object V(Vector3 p)=>New(vertex,p,Vector3.up,new Vector4(1,0,0,1),new Vector4(p.x/17,p.z/16,3,4),new Color(.2f,.4f,.8f,1));
            object Face(Vector3 a,Vector3 b,Vector3 c,int occurrence,int index)=>New(triangle,V(a),V(b),V(c),0,occurrence,index);
            Array Resolve(Array src)=>(Array)T("MovableRoadCoverage").GetMethod("Resolve",F).Invoke(null,new object[]{src,new[]{false,true}});
            double Area(object f)
            {Vector3 P(string n)=>(Vector3)Get(Get(f,n),"Position");var a=P("A");var b=P("B");var c=P("C");return Math.Abs(((double)b.x-a.x)*(c.z-a.z)-((double)b.z-a.z)*(c.x-a.x))*.5;}
            int Coverage(Array faces,float x,float z)
            {
                int hits=0;
                foreach(var f in faces){
                    Vector3 P(string n)=>(Vector3)Get(Get(f,n),"Position");
                    var a=P("A");var b=P("B");var c=P("C");
                    double Side(Vector3 l,Vector3 r)=>((double)r.x-l.x)*(z-l.z)-((double)r.z-l.z)*(x-l.x);
                    double e=Side(a,b),g=Side(b,c),h=Side(c,a);
                    if((e>0&&g>0&&h>0)||(e<0&&g<0&&h<0))hits++;
                }
                return hits;
            }
            using(var bin=new BinaryReader(File.OpenRead(Arg("-coverageFixture"))))
            for(int lod=0;lod<3;lod++){
                int n=bin.ReadInt32();var src=Array.CreateInstance(triangle,n);
                for(int i=0;i<n;i++){int occurrence=bin.ReadInt32(),index=bin.ReadInt32();
                    Vector3 Read()=>new Vector3(bin.ReadSingle(),bin.ReadSingle(),bin.ReadSingle());
                    src.SetValue(Face(Read(),Read(),Read(),occurrence,index),i);}
                var output=Resolve(src);
                foreach(float z in new[]{-87.3f,-20.3f,20.3f,87.3f})
                foreach(float x in new[]{6.53f,7.13f,7.63f,8.13f}){
                    Check(Coverage(src,x,z)==(x>7&&x<7.75f?2:1),"control at "+x+","+z);
                    Check(Coverage(output,x,z)==1,"overlap/gap at "+x+","+z);}
                var srcArea=src.Cast<object>().Sum(Area); var dstArea=output.Cast<object>().Sum(Area);
                foreach(var f in output)foreach(string key in new[]{"A","B","C"}){
                    var v=Get(f,key);var p=(Vector3)Get(v,"Position");var uv=(Vector4)Get(v,"UV");
                    Check(Math.Abs(uv.x-p.x/17)<1e-5&&Math.Abs(uv.y-p.z/16)<1e-5&&uv.z==3&&uv.w==4,"UV clipping");
                    Check(((Color)Get(v,"Color"))==new Color(.2f,.4f,.8f,1),"clip color");}
                Check(dstArea<srcArea&&dstArea>srcArea-150,"removed area");
                Check(src.Cast<object>().Where(f=>(int)Get(f,"Occurrence")==1).SequenceEqual(output.Cast<object>().Where(f=>(int)Get(f,"Occurrence")==1)),"sidewalk stream changed");
                Debug.Log($"PASS saved Draw01 LOD {lod} faces {n}->{output.Length}; duplicate area {srcArea-dstArea:F4}m2; fixed/moving both ends; attributes");
            }
            var region=T("MovableNativeDecals+Region");
            var sample=New(region,Vector3.zero,Quaternion.identity,new Vector3(1,1,4),false,null,new Vector4(0,0,1,10));
            var overlaps=region.GetMethod("Overlaps",F);
            Check((bool)overlaps.Invoke(sample,new[]{Face(new Vector3(-1,0,-1),new Vector3(1,0,1),new Vector3(1,0,-1),0,0)}),"interior tint lost");
            Check(!(bool)overlaps.Invoke(sample,new[]{Face(new Vector3(1,0,-1),new Vector3(3,0,1),new Vector3(3,0,-1),0,0)}),"adjacent sidewalk tinted");
            var halfFace=Face(new Vector3(-2,0,-1),new Vector3(2,0,-1),new Vector3(0,0,1),0,13);
            var bound=New(T("MovableRoadGeometry+BoundTriangle"),halfFace,(uint)5);
            var bands=Array.CreateInstance(region,1); bands.SetValue(sample,0);
            var pieces=(System.Collections.IEnumerable)T("MovableRoadCoverage").GetMethod("SplitColors",F).Invoke(null,new object[]{bound,bands});
            double tintedArea=0,originalArea=0;
            foreach(var piece in pieces){var f=Get(piece,"Item1");Check((uint)Get(f,"Bone")==5,"split bone");var t=Get(f,"Geometry");Check((int)Get(t,"SourceTriangle")==13,"split provenance");if((bool)Get(piece,"Item2"))tintedArea+=Area(t);else originalArea+=Area(t);}
            Check(Math.Abs(tintedArea-3)<1e-6 && Math.Abs(originalArea-1)<1e-6,"partial tint area");
            Debug.Log("PASS partial color 3m2 and plain 1m2; source/bone retained");
            var uv0=(Vector4)region.GetMethod("SourceUv",F).Invoke(sample,new object[]{new Vector3(-1,0,-4)});
            var uv1=(Vector4)region.GetMethod("SourceUv",F).Invoke(sample,new object[]{new Vector3(1,0,4)});
            Check(uv0==Vector4.zero&&uv1==new Vector4(1,10,0,0),"source decal UV");
            Debug.Log("PASS native lane footprint and continuous original UV"); EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
