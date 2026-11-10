using System;
using System.Collections.Generic;
using System.Linq;
static class Program {
 static void Need(bool b,string m){if(!b)throw new Exception(m);}
 static Material Make()=>new(){Flags=new(){"SOURCE_LAYER_TWO","SOURCE_PROJECTED_BLEND","COLOSSAL_GEOMETRY_DILATED"}};
 static void Check(Material m){Need(m.Flags.Contains("SOURCE_LAYER_TWO")&&m.Flags.Contains("SOURCE_PROJECTED_BLEND"),"appearance keyword lost");}
 static void Main(){int count=0;
  foreach(bool hasPose in new[]{false,true}){var m=Make();var prefab=hasPose?new RenderPrefab{Skeleton=new(){active=true,m_Bones=new object[2]}}:null;Policy.Run(m,false,true,prefab,false);Check(m);Need(m.Flags.Contains("COLOSSAL_GEOMETRY_DILATED")&&!m.Flags.Contains("COLOSSAL_GEOMETRY_DEFAULT"),"object pose incorrectly selected lane mode");Need(m.Flags.Contains(hasPose?"_GPU_ANIMATION_PROCEDURAL":"_GPU_ANIMATION_OFF"),"object animation input mode");count++;}
  foreach(bool tiling in new[]{false,true}){var m=Make();var prefab=new RenderPrefab{Curve=new(){m_GeometryTiling=tiling}};Policy.Run(m,false,true,prefab,true);Check(m);Need(m.Flags.Contains(tiling?"COLOSSAL_GEOMETRY_TILING":"COLOSSAL_GEOMETRY_DEFAULT")&&!m.Flags.Contains("COLOSSAL_GEOMETRY_DILATED"),"native lane mode");count++;}
  {var m=Make();Policy.Run(m,false,true,new RenderPrefab{Curve=new(){m_GeometryTiling=true}},false);Check(m);Need(m.Flags.Contains("COLOSSAL_GEOMETRY_TILING"),"actual curve tiling ignored");count++;}
  foreach(var bones in new[]{null,Array.Empty<object>(),new object[1]})foreach(bool active in new[]{false,true}){var m=Make();Policy.Run(m,false,true,new RenderPrefab{Skeleton=new(){active=active,m_Bones=bones}},false);Check(m);bool valid=active&&bones!=null&&bones.Length!=0;Need(m.Flags.Contains("_GPU_ANIMATION_PROCEDURAL")==valid&&m.Flags.Contains("_GPU_ANIMATION_OFF")==!valid,"empty/inactive bone mode");count++;}
  {var m=Make();m.Flags.Add("_GPU_ANIMATION_SHAPE");Policy.Run(m,true,true,null,false);Need(m.Flags.Contains("_GPU_ANIMATION_SHAPE")&&!m.Flags.Contains("_GPU_ANIMATION_OFF"),"native network protocol changed");Check(m);count++;}
  Console.WriteLine($"PASS {count} source-extracted preview instance-policy cases; appearance keywords retained; object pose vs lane, curve, nonempty active bones and network modes. Engine rendering not asserted.");
 }
}
class Material {public Shader shader=new();public HashSet<string> Flags=new();public int renderQueue;readonly Dictionary<string,float> values=new();public bool HasProperty(string n)=>values.ContainsKey(n);public float GetFloat(string n)=>values[n];public void SetFloat(string n,float v)=>values[n]=v;public void SetInt(string n,int v)=>values[n]=v;public void SetKeyword(Keyword k,bool on){if(on)Flags.Add(k.Name);else Flags.Remove(k.Name);}}
class Shader {public Space keywordSpace=new();public int renderQueue;}
class Space {public Keyword FindKeyword(string n)=>new(){Name=n,isValid=true};}
class Keyword {public string Name;public bool isValid;}
class RenderPrefab {public CurveProperties Curve;public DecalProperties Decal;public ProceduralAnimationProperties Skeleton;public bool TryGet<T>(out T value)where T:class {value=(typeof(T)==typeof(CurveProperties)?Curve:typeof(T)==typeof(DecalProperties)?(object)Decal:Skeleton)as T;return value!=null;}public bool Has<T>()where T:class=>TryGet<T>(out _);}
class CurveProperties {public bool m_GeometryTiling;}
class DecalProperties {public int m_RendererPriority;}
class ProceduralAnimationProperties {public bool active;public object[] m_Bones;}
static class math {public static float asfloat(int i)=>BitConverter.Int32BitsToSingle(i);}
enum CompareFunction {LessEqual=4}
namespace Game.Rendering {enum DecalLayers {Roads=1}}
