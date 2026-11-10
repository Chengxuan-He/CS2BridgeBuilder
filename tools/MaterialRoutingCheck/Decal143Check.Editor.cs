using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class Decal143Check
{
 const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static object Get(object o,string n)=>o.GetType().GetProperty(n,I)?.GetValue(o)??o.GetType().GetField(n,I).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,I).SetValue(o,v);
 static void Need(bool b,string m){if(!b)throw new Exception(m);}
 // Only fixture CID allocation is substituted: the game's Burst hash initializer
 // is incompatible with this Editor host. Every database IO and Surface method
 // remains native; production has no allocator injection or this proxy.
 sealed class FixtureDatabase:System.Runtime.Remoting.Proxies.RealProxy
 {
  readonly object database; int identity=1000;
  internal FixtureDatabase(Type type,object value):base(type){database=value;}
  public override System.Runtime.Remoting.Messaging.IMessage Invoke(System.Runtime.Remoting.Messaging.IMessage message)
  {
   var call=(System.Runtime.Remoting.Messaging.IMethodCallMessage)message;
   try { var arguments=call.Args;var method=(MethodInfo)call.MethodBase;
    if(method.Name=="AddAsset")for(int i=0;i<arguments.Length;i++)
     if(method.GetParameters()[i].ParameterType.FullName=="Colossal.Hash128")arguments[i]=method.GetParameters()[i].ParameterType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{(++identity).ToString("x32")});
    var value=method.Invoke(database,arguments);
    if(method.Name=="AddAsset" && value!=null)value.GetType().GetProperty("database",I).SetValue(value,GetTransparentProxy());
    return new System.Runtime.Remoting.Messaging.ReturnMessage(value,arguments,arguments.Length,call.LogicalCallContext,call);
   }catch(Exception e){return new System.Runtime.Remoting.Messaging.ReturnMessage(e is TargetInvocationException?e.InnerException:e,call);}
  }
 }
 sealed class MaterialScope:IDisposable{internal Material Value;internal MaterialScope(Material v){Value=v;}public void Dispose(){UnityEngine.Object.DestroyImmediate(Value);}}
 public static void Run(){try{
 var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];var managed=Arg("-gameManaged");
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
 var native=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));Type N(string n)=>native.GetType("Colossal.IO.AssetDatabase."+n,true);
 var mod=Assembly.LoadFrom(Arg("-modAssembly"));Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true);
 var root=Arg("-outputRoot");Directory.CreateDirectory(root);var dt=mod.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials+Descriptor",true);
 var descriptor=Activator.CreateInstance(dt,I,null,new object[]{root},null);var dbType=N("AssetDatabase`1").MakeGenericType(dt);var db=dbType.GetMethod("GetInstance",S).Invoke(null,new[]{descriptor});
 object global;try{global=N("AssetDatabase").GetProperty("global",S).GetValue(null);}catch(TargetInvocationException){global=N("AssetDatabase").GetField("s_Instance",S).GetValue(null);}
 var resources=ScriptableObject.CreateInstance(N("AssetDatabaseResources"));var library=Activator.CreateInstance(N("MaterialLibrary"));var listField=N("MaterialLibrary").GetField("m_Materials",I);var list=(IList)Activator.CreateInstance(listField.FieldType);var mt=N("MaterialLibrary").GetNestedType("MaterialDescription");
 Material Template(int id,string shader,int queue=2225){var d=Activator.CreateInstance(mt);Set(d,"m_Hash",id);var st=mt.GetField("m_Stacks",I);st.SetValue(d,Array.CreateInstance(st.FieldType.GetElementType(),0));var material=new Material(id==1?AssetBundle.GetAllLoadedAssetBundles().Single(b=>b.LoadAllAssets<Shader>().Any(s=>s.name=="BH/SG_DefaultShader")).LoadAllAssets<Shader>().Single():Shader.Find(shader));Need(material.shader!=null,"fixture shader missing");material.name=id==1?"DefaultCutout":id==0?"Default":id==13?"DefaultDoubleSided":id==14?"DefaultCutoutDoubleSided":id==35?"DefaultHashedCutout":"Fixture"+id;if(id==1){material.SetFloat("INFOVIEW", 0.0f);
material.SetFloat("_AORemapMax", 1.0f);
material.SetFloat("_AORemapMin", 0.0f);
material.SetFloat("_ATDistance", 1.0f);
material.SetFloat("_AddPrecomputedVelocity", 0.0f);
material.SetFloat("_AddVelocityChange", 0.0f);
material.SetFloat("_AlbedoAffectEmissive", 0.0f);
material.SetFloat("_AlphaCutoff", 0.5f);
material.SetFloat("_AlphaCutoffEnable", 1.0f);
material.SetFloat("_AlphaCutoffPostpass", 0.5f);
material.SetFloat("_AlphaCutoffPrepass", 0.5f);
material.SetFloat("_AlphaCutoffShadow", 0.5f);
material.SetFloat("_AlphaDstBlend", 0.0f);
material.SetFloat("_AlphaSrcBlend", 1.0f);
material.SetFloat("_AlphaToMask", 1.0f);
material.SetFloat("_AlphaToMaskInspectorValue", 1.0f);
material.SetFloat("_Anisotropy", 0.0f);
material.SetFloat("_BlendMode", 0.0f);
material.SetFloat("_CoatMask", 0.0f);
material.SetFloat("_CoatStrength", 1.0f);
material.SetFloat("_ConservativeDepthOffsetEnable", 0.0f);
material.SetFloat("_CullMode", 2.0f);
material.SetFloat("_CullModeForward", 2.0f);
material.SetFloat("_Cutoff", 0.5f);
material.SetFloat("_DepthOffsetEnable", 0.0f);
material.SetFloat("_DetailAlbedoScale", 1.0f);
material.SetFloat("_DetailNormalScale", 1.0f);
material.SetFloat("_DetailSmoothnessScale", 1.0f);
material.SetFloat("_DiffusionProfile", 0.0f);
material.SetFloat("_DiffusionProfileHash", 0.0f);
material.SetFloat("_DisplacementLockObjectScale", 1.0f);
material.SetFloat("_DisplacementLockTilingScale", 1.0f);
material.SetFloat("_DisplacementMode", 0.0f);
material.SetFloat("_DistortionBlendMode", 0.0f);
material.SetFloat("_DistortionBlurBlendMode", 0.0f);
material.SetFloat("_DistortionBlurDstBlend", 1.0f);
material.SetFloat("_DistortionBlurRemapMax", 1.0f);
material.SetFloat("_DistortionBlurRemapMin", 0.0f);
material.SetFloat("_DistortionBlurScale", 1.0f);
material.SetFloat("_DistortionBlurSrcBlend", 1.0f);
material.SetFloat("_DistortionDepthTest", 1.0f);
material.SetFloat("_DistortionDstBlend", 1.0f);
material.SetFloat("_DistortionEnable", 0.0f);
material.SetFloat("_DistortionScale", 1.0f);
material.SetFloat("_DistortionSrcBlend", 1.0f);
material.SetFloat("_DistortionVectorBias", -1.0f);
material.SetFloat("_DistortionVectorScale", 2.0f);
material.SetFloat("_DoubleSidedEnable", 0.0f);
material.SetFloat("_DoubleSidedGIMode", 0.0f);
material.SetFloat("_DoubleSidedNormalMode", 1.0f);
material.SetFloat("_DstBlend", 0.0f);
material.SetFloat("_EmissiveColorMode", 1.0f);
material.SetFloat("_EmissiveExposureWeight", 0.800000011920929f);
material.SetFloat("_EmissiveIntensity", 0.0f);
material.SetFloat("_EmissiveIntensityUnit", 0.0f);
material.SetFloat("_EnableBlendModePreserveSpecularLighting", 1.0f);
material.SetFloat("_EnableFogOnTransparent", 1.0f);
material.SetFloat("_EnableGeometricSpecularAA", 0.0f);
material.SetFloat("_EnableSpecularOcclusion", 0.0f);
material.SetFloat("_EnergyConservingSpecularColor", 1.0f);
material.SetFloat("_HdrpVersion", 2.0f);
material.SetFloat("_HeightAmplitude", 0.019999999552965164f);
material.SetFloat("_HeightCenter", 0.5f);
material.SetFloat("_HeightMapParametrization", 0.0f);
material.SetFloat("_HeightMax", 1.0f);
material.SetFloat("_HeightMin", -1.0f);
material.SetFloat("_HeightOffset", 0.0f);
material.SetFloat("_HeightPoMAmplitude", 2.0f);
material.SetFloat("_HeightTessAmplitude", 2.0f);
material.SetFloat("_HeightTessCenter", 0.5f);
material.SetFloat("_InvTilingScale", 1.0f);
material.SetFloat("_Ior", 1.0f);
material.SetFloat("_IridescenceMask", 1.0f);
material.SetFloat("_IridescenceThickness", 1.0f);
material.SetFloat("_IsWindEnabled", 0.0f);
material.SetFloat("_LinkDetailsWithBase", 1.0f);
material.SetFloat("_MaterialID", 1.0f);
material.SetFloat("_Metallic", 1.0f);
material.SetFloat("_NormalMapSpace", 0.0f);
material.SetFloat("_NormalScale", 1.0f);
material.SetFloat("_OpaqueCullMode", 2.0f);
material.SetFloat("_PPDLodThreshold", 5.0f);
material.SetFloat("_PPDMaxSamples", 15.0f);
material.SetFloat("_PPDMinSamples", 5.0f);
material.SetFloat("_PPDPrimitiveLength", 1.0f);
material.SetFloat("_PPDPrimitiveWidth", 1.0f);
material.SetFloat("_Porosity", 0.0f);
material.SetFloat("_RayTracing", 0.0f);
material.SetFloat("_ReceivesSSR", 1.0f);
material.SetFloat("_ReceivesSSRTransparent", 0.0f);
material.SetFloat("_RefractionModel", 0.0f);
material.SetFloat("_RenderQueueType", 1.0f);
material.SetFloat("_RequireSplitLighting", 0.0f);
material.SetFloat("_SSRefractionProjectionModel", 0.0f);
material.SetFloat("_Smoothness", 1.0f);
material.SetFloat("_SmoothnessRemapMax", 1.0f);
material.SetFloat("_SmoothnessRemapMin", 0.0f);
material.SetFloat("_Snow", 1.0f);
material.SetFloat("_SpecularAAScreenSpaceVariance", 0.10000000149011612f);
material.SetFloat("_SpecularAAThreshold", 0.20000000298023224f);
material.SetFloat("_SrcBlend", 1.0f);
material.SetFloat("_StencilRef", 0.0f);
material.SetFloat("_StencilRefDepth", 8.0f);
material.SetFloat("_StencilRefDistortionVec", 4.0f);
material.SetFloat("_StencilRefGBuffer", 10.0f);
material.SetFloat("_StencilRefMV", 40.0f);
material.SetFloat("_StencilWriteMask", 6.0f);
material.SetFloat("_StencilWriteMaskDepth", 9.0f);
material.SetFloat("_StencilWriteMaskDistortionVec", 4.0f);
material.SetFloat("_StencilWriteMaskGBuffer", 15.0f);
material.SetFloat("_StencilWriteMaskMV", 41.0f);
material.SetFloat("_SubsurfaceMask", 1.0f);
material.SetFloat("_SupportDecals", 1.0f);
material.SetFloat("_SurfaceType", 0.0f);
material.SetFloat("_TexWorldScale", 1.0f);
material.SetFloat("_TexWorldScaleEmissive", 1.0f);
material.SetFloat("_Thickness", 1.0f);
material.SetFloat("_ThicknessMultiplier", 1.0f);
material.SetFloat("_TransmissionEnable", 1.0f);
material.SetFloat("_TransparentBackfaceEnable", 0.0f);
material.SetFloat("_TransparentCullMode", 2.0f);
material.SetFloat("_TransparentDepthPostpassEnable", 0.0f);
material.SetFloat("_TransparentDepthPrepassEnable", 0.0f);
material.SetFloat("_TransparentSortPriority", 0.0f);
material.SetFloat("_TransparentWritingMotionVec", 0.0f);
material.SetFloat("_TransparentZWrite", 0.0f);
material.SetFloat("_UVBase", 0.0f);
material.SetFloat("_UVDetail", 0.0f);
material.SetFloat("_UVEmissive", 0.0f);
material.SetFloat("_UseEmissiveIntensity", 0.0f);
material.SetFloat("_UseShadowThreshold", 0.0f);
material.SetFloat("_UseStack0", 0.0f);
material.SetFloat("_UseStack1", 0.0f);
material.SetFloat("_Wetness", 0.0f);
material.SetFloat("_WindElasticityLvl0", 1.0f);
material.SetFloat("_WindElasticityLvl1", 1.0f);
material.SetFloat("_WindElasticityLvlB", 0.20000000298023224f);
material.SetFloat("_WindFakeSingleObjectPivot", 0.0f);
material.SetFloat("_WindFlutterElasticity", 0.10000000149011612f);
material.SetFloat("_WindFlutterPeriodScale", 1.0f);
material.SetFloat("_WindFlutterPhase", 50.0f);
material.SetFloat("_WindFlutterScale", 0.10000000149011612f);
material.SetFloat("_WindRangeLvl0", 2.0f);
material.SetFloat("_WindRangeLvl1", 0.5f);
material.SetFloat("_WindRangeLvlB", 5.0f);
material.SetFloat("_ZTestDepthEqualForOpaque", 3.0f);
material.SetFloat("_ZTestGBuffer", 3.0f);
material.SetFloat("_ZTestModeDistortion", 4.0f);
material.SetFloat("_ZTestTransparent", 4.0f);
material.SetFloat("_ZWrite", 1.0f);
material.SetFloat("colossal_DecalLayerMask", 0.0f);
material.SetFloat("colossal_LodFade", 1.0f);
material.SetFloat("colossal_MetaParameters", 0.0f);
material.SetFloat("colossal_SingleLightsOffset", 0.0f);
material.SetVector("DefaultPVTStack_atlasParams0", new Vector4(0.0f,0.0f,0.0f,0.0f));
material.SetVector("DefaultPVTStack_atlasParams1", new Vector4(0.0f,0.0f,0.0f,0.0f));
material.SetVector("ExtendedPVTStack_atlasParams0", new Vector4(0.0f,0.0f,0.0f,0.0f));
material.SetVector("ExtendedPVTStack_atlasParams1", new Vector4(0.0f,0.0f,0.0f,0.0f));
material.SetVector("_BaseColor", new Vector4(1.0f,1.0f,1.0f,1.0f));
material.SetVector("_BaseColorMap_MipInfo", new Vector4(0.0f,0.0f,0.0f,0.0f));
material.SetVector("_Color", new Vector4(1.0f,1.0f,1.0f,1.0f));
material.SetVector("_ColorMask0", new Vector4(1.0f,1.0f,1.0f,1.0f));
material.SetVector("_ColorMask1", new Vector4(1.0f,1.0f,1.0f,1.0f));
material.SetVector("_ColorMask2", new Vector4(1.0f,1.0f,1.0f,1.0f));
material.SetVector("_DiffusionProfileAsset", new Vector4(0.0f,0.0f,0.0f,0.0f));
material.SetVector("_DoubleSidedConstants", new Vector4(1.0f,1.0f,-1.0f,0.0f));
material.SetVector("_EmissionColor", new Vector4(1.0f,1.0f,1.0f,1.0f));
material.SetVector("_EmissiveColor", new Vector4(0.0f,0.0f,0.0f,1.0f));
material.SetVector("_EmissiveColorLDR", new Vector4(0.0f,0.0f,0.0f,1.0f));
material.SetVector("_InvPrimScale", new Vector4(1.0f,1.0f,0.0f,0.0f));
material.SetVector("_IridescenceThicknessRemap", new Vector4(0.0f,1.0f,0.0f,0.0f));
material.SetVector("_RustOffset", new Vector4(0.0f,0.0f,0.0f,0.0f));
material.SetVector("_RustTiling", new Vector4(1.0f,1.0f,0.0f,0.0f));
material.SetVector("_SpecularColor", new Vector4(1.0f,1.0f,1.0f,1.0f));
material.SetVector("_TextureCoordinate", new Vector4(0.0f,0.0f,0.0f,0.0f));
material.SetVector("_ThicknessRemap", new Vector4(0.0f,1.0f,0.0f,0.0f));
material.SetVector("_TransmittanceColor", new Vector4(1.0f,1.0f,1.0f,1.0f));
material.SetVector("_UV0Offset", new Vector4(0.0f,0.0f,0.0f,0.0f));
material.SetVector("_UV0Scale", new Vector4(1.0f,1.0f,0.0f,0.0f));
material.SetVector("_UVDetailsMappingMask", new Vector4(1.0f,0.0f,0.0f,0.0f));
material.SetVector("_UVMappingMask", new Vector4(1.0f,0.0f,0.0f,0.0f));
material.SetVector("_UVMappingMaskEmissive", new Vector4(1.0f,0.0f,0.0f,0.0f));
material.SetVector("_VTUVs0", new Vector4(0.0f,0.0f,0.0f,0.0f));
material.SetVector("_VTUVs1", new Vector4(0.0f,0.0f,0.0f,0.0f));
material.SetVector("colossal_BoneParameters", new Vector4(0.0f,0.0f,0.0f,0.0f));
material.SetVector("colossal_ColorMask0", new Vector4(1.0f,1.0f,1.0f,1.0f));
material.SetVector("colossal_ColorMask1", new Vector4(1.0f,1.0f,1.0f,1.0f));
material.SetVector("colossal_ColorMask2", new Vector4(1.0f,1.0f,1.0f,1.0f));
material.SetVector("colossal_CurveParams", new Vector4(0.0f,0.0f,0.0f,0.0f));
material.SetVector("colossal_InfoviewColor", new Vector4(0.47058814764022827f,0.47058814764022827f,0.47058814764022827f,1.0f));
material.SetVector("colossal_LightParameters", new Vector4(0.0f,0.0f,0.0f,0.0f));
material.SetVector("colossal_ShapeParameters1", new Vector4(0.0f,0.0f,0.0f,0.0f));
material.SetVector("colossal_ShapeParameters2", new Vector4(0.0f,0.0f,0.0f,0.0f));
material.SetVector("colossal_Wetness", new Vector4(0.0f,0.0f,0.0f,0.0f));}
material.renderQueue=queue;Set(d,"m_Material",material);list.Add(d);return material;}
 var nativeBundle=AssetBundle.LoadFromFile("E:/CS2BridgeBuilder/work/decal143/native-default.bundle");var nativeShader=nativeBundle.LoadAllAssets<Shader>().Single();
 var target=Template(0,"Hidden/BB129/Object");Template(11,"Unlit/Color");var sourceTemplate=Template(16,"Hidden/BB129/Curve");Template(17,"Unlit/Texture");Template(19,"Unlit/Transparent");Template(881140,"Unlit/Transparent");Template(32,"Unlit/Texture");Template(20,"Unlit/Texture",2475).EnableKeyword("_MATERIAL_AFFECTS_ALBEDO");Template(881130,"Unlit/Texture");Template(30,"Hidden/BB129/Curve",3000);
 foreach(var id in new[]{1,13,14,35,36})Template(id,"Hidden/BB129/Object",id==1||id==14||id==35||id==36?2475:2225);
 var custom=Template(991129,"Hidden/BB129/Curve");sourceTemplate.SetFloat("_Snow",0);custom.SetFloat("_Snow",.25f);custom.SetFloat("_EmissiveIntensity",2.75f);custom.SetColor("_BaseColor",new Color(.2f,.4f,.6f,.8f));
 listField.SetValue(library,list);Set(resources,"m_MaterialLibrary",library);Set(global,"m_Resources",resources);
 var pathType=N("AssetDataPath");var pathCreate=pathType.GetMethods(S).Single(m=>m.Name=="Create"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(string)&&m.GetParameters()[1].ParameterType==typeof(string));
 object PathFor(string name)=>pathCreate.Invoke(null,new object[]{"Fixture",name,Enum.Parse(pathCreate.GetParameters()[2].ParameterType,"None")});
 var add=dbType.GetMethods(I).Single(m=>m.Name=="AddAsset"&&!m.IsGenericMethod&&m.GetParameters().Length==3&&m.GetParameters()[1].ParameterType==typeof(Type));var hashType=add.GetParameters()[2].ParameterType;
 var surfaceType=N("SurfaceAsset");int next=0;object Source(int template,string label="source"){var cid=(++next).ToString("x32");var sf=add.Invoke(db,new[]{PathFor(label+next),(object)surfaceType,hashType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{cid})});var meta=sf.GetType().GetMethod("GetMeta",I).Invoke(sf,null);var path=(string)Get(meta,"path");Directory.CreateDirectory(Path.GetDirectoryName(path));
 // Native version-1 descriptor with real parser/writer, no synthetic Surface bypass.
 using(var w=new BinaryWriter(File.Create(path))){w.Write((ushort)1);w.Write(template);w.Write(false);w.Write((byte)255);w.Write(2);w.Write("_Smoothness");w.Write(.375f);w.Write("_TargetOnly");w.Write(99f);for(int i=0;i<4;i++){w.Write((byte)255);w.Write(0);}w.Write((byte)255);w.Write(1);w.Write("_TEST_APPEARANCE");}return sf;}
 // Seed a bounded prepared-control catalogue for all ten styles. This tests the
 // consumer contract, not native package bootstrap (the real package is checked separately).
 var neutral=Source(0);surfaceType.GetMethod("LoadProperties",I).Invoke(neutral,new object[]{true});
 var preparedType=mod.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials",true);preparedType.GetField("_root",S).SetValue(null,root);
 var catalogue=(IDictionary)preparedType.GetField("Surfaces",S).GetValue(null);
 foreach(var style in new[]{"Draw01","Draw02","Draw03","Lift01","Lift02","Lift03","Lift04","Lift05","PedestrianDraw01","PedestrianDraw02"})catalogue.Add(style+"/Road/NativeAsphalt.Surface",neutral);
 var preview=Activator.CreateInstance(T("PreviewGeometry"),true);Set(preview,"_nativeAssets",new FixtureDatabase(N("ILocalAssetDatabase"),db).GetTransparentProxy());
 var ownedType=typeof(System.Collections.Generic.List<>).MakeGenericType(N("AssetData"));object Owned()=>Activator.CreateInstance(ownedType);
 var geometryType=T("MovableMaterialConversion+GeometryInput");object Geometry(bool evaluated=true,int bones=2)=>Activator.CreateInstance(geometryType,I,null,new object[]{evaluated,true,bones},null);
 var run=T("MovableMaterialConversion").GetMethod("TryCreate",S);object[] Call(object source,object geometry=null)=>new object[]{source,geometry??Geometry(),"tmp11111111-2222-4333-8444-555555555555","Draw02",preview,Owned(),null,null};
 void Coverage(Texture texture,string label){Need(texture!=null,"missing native decoded texture");var cs=UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Decal142.compute");int k=cs.FindKernel("Sample");var rt=new RenderTexture(1024,512,0,RenderTextureFormat.ARGBFloat){enableRandomWrite=true};rt.Create();var counters=new ComputeBuffer(3,4);counters.SetData(new uint[3]);cs.SetTexture(k,"Source",texture);cs.SetTexture(k,"Output",rt);cs.SetBuffer(k,"Counts",counters);cs.SetFloat("Mip",0);cs.SetInt("Mode",2);cs.Dispatch(k,128,64,1);var counts=new uint[3];counters.GetData(counts);Need(counts[1]>0,"all artwork disappeared: "+label);Debug.Log("GPU native TextureAsset "+label+" size="+texture.width+"x"+texture.height+" originalNonzero="+counts[0]+" deterministic="+counts[1]+" partial="+counts[2]);counters.Dispose();rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
 int count=0;var originalSources=new System.Collections.Generic.List<object>();
 var sources=Arg("-sourceRoot");
 foreach(var family in new[]{"TramRails","TramRailsBase","LaneLine"})
 {
  var file=Directory.GetFiles(sources,family+"_*.Material").Single();var sf=Source(20,family);
  var path=(string)Get(sf.GetType().GetMethod("GetMeta",I).Invoke(sf,null),"path");File.Copy(file,path,true);
  surfaceType.GetMethod("LoadProperties",I).Invoke(sf,new object[]{true});
  var original=(IDictionary)Get(sf,"textures");
  var bindings=(IDictionary)Activator.CreateInstance(typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(typeof(string),N("TextureAsset")));
  foreach(var channel in new[]{"BaseColor","Normal","MaskMap"})
  {
   var key=channel=="BaseColor"?"_BaseColorMap":channel=="Normal"?"_NormalMap":"_MaskMap";
   var cid=Get(original[key],"id");var hash=Get(cid,"guid");
   var tx=add.Invoke(db,new[]{PathFor(family+channel),(object)N("TextureAsset"),hash});
   var tp=(string)Get(tx.GetType().GetMethod("GetMeta",I).Invoke(tx,null),"path");Directory.CreateDirectory(Path.GetDirectoryName(tp));
   File.Copy(Directory.GetFiles(sources,family+"_"+channel+"_*.Texture2D").Single(),tp,true);bindings.Add(key,tx);
  }
  surfaceType.GetMethod("UpdateTextures",I).Invoke(sf,new object[]{bindings});
  var before=File.ReadAllBytes(path);var call=Call(sf,Activator.CreateInstance(geometryType,I,null,new object[]{true,true,2,true},null));Need((bool)run.Invoke(null,call),family+" failed: "+call[7]);
  originalSources.Add(sf);var output=call[6];surfaceType.GetMethod("LoadProperties",I).Invoke(output,new object[]{true});
  var after=(IDictionary)Get(output,"textures");foreach(DictionaryEntry pair in bindings)Need(after.Contains(pair.Key)&&Get(after[pair.Key],"id").Equals(Get(pair.Value,"id")),family+" lost original "+pair.Key);
  Need((int)Get(output,"materialTemplateHash")==1,"source alpha must remain represented"); var unload=surfaceType.GetMethod("UnloadProperties",I); unload.Invoke(output,unload.GetParameters().Select(p=>p.DefaultValue).ToArray()); surfaceType.GetMethod("LoadProperties",I).Invoke(output,new object[]{true}); var keys=((IEnumerable)Get(output,"keywords")).Cast<string>().ToArray(); Need(keys.Contains("_ALPHATEST_ON")&&!keys.Contains("_HASHED_ALPHATEST"),"persisted alpha keywords missing");
  var load=surfaceType.GetMethod("Load",I);var loadArgs=load.GetParameters().Select(x=>x.DefaultValue).ToArray();loadArgs[1]=true;var nativeLoaded=(Material)load.Invoke(output,loadArgs);Need(nativeLoaded.shader.name=="BH/SG_DefaultShader"&&nativeLoaded.IsKeywordEnabled("_ALPHATEST_ON")&&!nativeLoaded.IsKeywordEnabled("_HASHED_ALPHATEST")&&nativeLoaded.GetFloat("_AlphaCutoff")==.5f,"actual shader cold-loaded coverage state");Need(nativeLoaded.GetFloat("_ZWrite")==1&&nativeLoaded.GetFloat("_SrcBlend")==1&&nativeLoaded.GetFloat("_DstBlend")==0,"native deterministic object pass state");Coverage(nativeLoaded.GetTexture("_BaseColorMap"),family);Debug.Log("PASS actual native shader Surface.Load: "+family+" queue="+nativeLoaded.renderQueue+" cutoff="+nativeLoaded.GetFloat("_AlphaCutoff"));surfaceType.GetMethod("Unload",I).Invoke(output,new object[]{false});
  var outputFloats=(IDictionary)Get(output,"floats"); Need(!outputFloats.Contains("_AlphaCutoff"),"projected conversion persisted an override of native cutoff"); Need(target.GetFloat("_AlphaCutoff")==.5f,"template native cutoff missing");
  using(var binding=new MaterialScope(new Material(target))) {
   // Native Surface starts with its template; no serialized cutoff override.
   binding.Value.shaderKeywords=keys;var clone=new Material(binding.Value);
   var configure=mod.GetType("BridgeBuilder.Runtime.BridgePreviewRenderResources",true).GetMethod("ConfigurePreviewDepth",S);
   configure.Invoke(null,new object[]{clone,false,true,null,false});
   Need(clone.GetFloat("_AlphaCutoff")==.5f&&!clone.IsKeywordEnabled("_HASHED_ALPHATEST"),"initial preview binding lost native coverage");
   clone.CopyPropertiesFromMaterial(binding.Value);configure.Invoke(null,new object[]{clone,false,true,null,false});
   Need(clone.GetFloat("_AlphaCutoff")==.5f&&!clone.IsKeywordEnabled("_HASHED_ALPHATEST"),"refreshed preview binding lost native coverage");UnityEngine.Object.DestroyImmediate(clone);
  }
  Need(before.SequenceEqual(File.ReadAllBytes(path)),"source mutated");
  Debug.Log("PASS actual native "+family+" descriptor -> object Surface; three original texture identities retained; source bytes unchanged; output="+Get(output,"id"));count++;
 }
 foreach(var label in new[]{"bikeTexture","busTexture"}){var tx=add.Invoke(db,new[]{PathFor(label),(object)N("TextureAsset"),hashType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{(++next+5000).ToString("x32")})});var tp=(string)Get(tx.GetType().GetMethod("GetMeta",I).Invoke(tx,null),"path");Directory.CreateDirectory(Path.GetDirectoryName(tp));File.Copy(Arg("-"+label),tp,true);var load=tx.GetType().GetMethods(I).Single(x=>x.Name=="Load"&&x.GetParameters().Length==1);var pixels=(Texture)load.Invoke(tx,load.GetParameters().Select(x=>x.DefaultValue).ToArray());Coverage(pixels,label);}
 // Real production clipping: a partial receiver footprint, two overlapping
 // segments, two native bones, and a gap. Area must not count the overlap twice.
 var vertex=T("MovableRoadGeometry+Vertex");var triangle=T("MovableRoadGeometry+Triangle");var partition=T("MovableRoadGeometry+Partition");var region=T("MovableNativeDecals+Region");
 object New(Type t,params object[] values)=>Activator.CreateInstance(t,I,null,values,null);
 object V(float x,float z)=>New(vertex,new Vector3(x,0,z),Vector3.up,new Vector4(1,0,0,1),Vector4.zero,Color.white);
 var receivers=Array.CreateInstance(triangle,2);receivers.SetValue(New(triangle,V(-1,-1),V(1,-1),V(1,1),0,0,0),0);receivers.SetValue(New(triangle,V(-1,-1),V(1,1),V(-1,1),0,0,1),1);
 var projectionSource=Source(32);var regions=Array.CreateInstance(region,2);
 regions.SetValue(New(region,new Vector3(-.25f,0,0),Quaternion.identity,new Vector3(.5f,1,1),false,projectionSource,new Vector4(0,0,1,1)),0);
 regions.SetValue(New(region,new Vector3(.25f,0,0),Quaternion.identity,new Vector3(.5f,1,1),false,projectionSource,new Vector4(0,0,1,1)),1);
 var slots=(IDictionary)Activator.CreateInstance(typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(surfaceType,typeof(int)));slots.Add(projectionSource,0);
 var split=New(partition,Matrix4x4.identity,New(partition,0),New(partition,1));
 object[] projection={receivers,regions,slots,split,2,1,null,null};var project=T("MovableProjectedColor").GetMethod("TryBuild",S);
 Need((bool)project.Invoke(null,projection),"projected color assembly failed: "+projection[7]);
 double area=0;var boneSet=new System.Collections.Generic.HashSet<uint>();
 foreach(var face in (IEnumerable)projection[6]){boneSet.Add((uint)Get(face,"Bone"));var tri=Get(face,"Geometry");var a=(Vector3)Get(Get(tri,"A"),"Position");var b=(Vector3)Get(Get(tri,"B"),"Position");var c=(Vector3)Get(Get(tri,"C"),"Position");area+=Math.Abs((b.x-a.x)*(c.z-a.z)-(b.z-a.z)*(c.x-a.x))/2;foreach(var key in new[]{"A","B","C"}){var v=Get(tri,key);var position=(Vector3)Get(v,"Position");var uv=(Vector4)Get(v,"UV");Need(Math.Abs(position.y-.0005f)<1e-7&&Math.Abs(position.x)<=.75001f&&uv.x>=-.0001f&&uv.x<=1.0001f&&uv.y>=-.0001f&&uv.y<=1.0001f,"color footprint/UV/clearance");}}
 Need(Math.Abs(area-3)<1e-5&&boneSet.SetEquals(new uint[]{0,1}),"overlap duplicated area or bone assignment lost: "+area);count++;
 var gap=partition.GetField("Gap",S)?.GetValue(null)??partition.GetProperty("Gap",S).GetValue(null);projection[3]=New(partition,Matrix4x4.identity,New(partition,0),gap);
 Need((bool)project.Invoke(null,projection),"physical gap must be skipped");foreach(var face in (IEnumerable)projection[6])Need((uint)Get(face,"Bone")==0,"gap generated an unsupported bone");count++;
 var elevated=Array.CreateInstance(triangle,4);Array.Copy(receivers,elevated,2);
 object High(float x,float z)=>New(vertex,new Vector3(x,5,z),Vector3.up,new Vector4(1,0,0,1),Vector4.zero,Color.white);
 elevated.SetValue(New(triangle,High(-1,-1),High(1,-1),High(1,1),0,1,0),2);elevated.SetValue(New(triangle,High(-1,-1),High(1,1),High(-1,1),0,1,1),3);
 projection[0]=elevated;projection[3]=split;Need((bool)project.Invoke(null,projection),"height-separated receiver test failed");
 foreach(var face in (IEnumerable)projection[6])foreach(var key in new[]{"A","B","C"})Need(((Vector3)Get(Get(Get(face,"Geometry"),key),"Position")).y<1,"color projected onto unrelated elevated deck");count++;

 // All source names take the same full texture interval, including former excluded rail bases.
 var batchType=T("MovableLaneVisuals+Batch");var game=Assembly.LoadFrom(Path.Combine(managed,"Game.dll"));
 var renderType=game.GetType("Game.Prefabs.RenderPrefab",true);var decalType=game.GetType("Game.Prefabs.DecalProperties",true);
 foreach(var name in new[]{"TramRails","TramRailsBase","LaneLine","Unknown source"})
 {
  var batch=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(batchType);var render=ScriptableObject.CreateInstance(renderType);var prop=ScriptableObject.CreateInstance(decalType);
  renderType.GetMethods(I).Single(m=>m.Name=="AddComponentFrom"&&m.GetParameters().Length==1&&!m.IsGenericMethod).Invoke(render,new object[]{prop});
  var src=Source(20,name);
  Set(batch,"Source",render);var arr=Array.CreateInstance(surfaceType,1);arr.SetValue(src,0);Set(batch,"Surfaces",arr);Set(batch,"Decal",true);Set(batch,"Transform",Matrix4x4.identity);
  var curve=Matrix4x4.zero;curve.SetColumn(0,new Vector4(0,0,-2,0));curve.SetColumn(1,new Vector4(0,0,-.6666667f,1f/3));curve.SetColumn(2,new Vector4(0,0,.6666667f,2f/3));curve.SetColumn(3,new Vector4(0,0,2,1));Set(batch,"CurveMatrix",curve);Set(batch,"Size",new Vector4(2,1,0,0));Set(batch,"Scale",Vector4.one);
  var rr=(IList)T("MovableNativeDecals").GetMethod("Regions",S).Invoke(null,new object[]{batch,new Pose(Vector3.zero,Quaternion.identity),32f});
  Need(rr.Count==1,"source filtered or artwork split: "+name);Need(ReferenceEquals(Get(rr[0],"SourceColor"),src),"source identity lost");
  Need(((Vector4)Get(rr[0],"UvRect")).Equals(new Vector4(0,0,1,1)),"source UV interval lost");count++;
 }

 // Execute the actual final production attachment consumer, including native
 // Model encoding, ToUnityMesh capture and retained private Surface identities.
 var factoryType=T("TowerFactory");var factory=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(factoryType);
 Set(factory,"_styleId","Draw01");Set(factory,"_previewGeometry",preview);Set(factory,"_created",Activator.CreateInstance(factoryType.GetField("_created",I).FieldType));
 var mechanism=ScriptableObject.CreateInstance(game.GetType("Game.Prefabs.StaticObjectPrefab",true));mechanism.name="tmp11111111-2222-4333-8444-555555555555";
 var rootRender=ScriptableObject.CreateInstance(renderType);var animation=ScriptableObject.CreateInstance(game.GetType("Game.Prefabs.ProceduralAnimationProperties",true));
 var boneField=animation.GetType().GetField("m_Bones",I);var boneType=boneField.FieldType.GetElementType();var bones=Array.CreateInstance(boneType,3);
 for(int i=0;i<3;i++){var b=Activator.CreateInstance(boneType);Set(b,"parentId",i==0?-1:i==1?0:1);Set(b,"m_Type",Enum.Parse(boneType.GetField("m_Type",I).FieldType,i==1?"OperatingRotation":"None"));bones.SetValue(b,i);}boneField.SetValue(animation,bones);
 var addComponent=renderType.GetMethods(I).Single(m=>m.Name=="AddComponentFrom"&&m.GetParameters().Length==1&&!m.IsGenericMethod);var attachedAnimation=addComponent.Invoke(rootRender,new object[]{animation});boneField.SetValue(attachedAnimation,boneField.GetValue(animation));
 var meshField=mechanism.GetType().GetField("m_Meshes",I);var infoType=meshField.FieldType.GetElementType();var info=Activator.CreateInstance(infoType);Set(info,"m_Mesh",rootRender);Set(info,"m_Rotation",infoType.GetField("m_Rotation",I).FieldType.GetField("identity",S).GetValue(null));var infoArray=Array.CreateInstance(infoType,1);infoArray.SetValue(info,0);meshField.SetValue(mechanism,infoArray);
 var batches=Array.CreateInstance(batchType,3);
 for(int j=0;j<3;j++){
  var batch=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(batchType);var render=ScriptableObject.CreateInstance(renderType);var prop=ScriptableObject.CreateInstance(decalType);Set(prop,"m_RendererPriority",j);var attachedDecal=addComponent.Invoke(render,new object[]{prop});Set(attachedDecal,"m_RendererPriority",j);
  Set(batch,"Source",render);var sf=Array.CreateInstance(surfaceType,1);sf.SetValue(j<2?originalSources[j]:Source(881140,"renamed third-party wear"),0);Set(batch,"Surfaces",sf);Set(batch,"Decal",true);Set(batch,"Transform",Matrix4x4.identity);
  var curve=Matrix4x4.zero;curve.SetColumn(0,new Vector4(0,0,-2,0));curve.SetColumn(1,new Vector4(0,0,-.6666667f,1f/3));curve.SetColumn(2,new Vector4(0,0,.6666667f,2f/3));curve.SetColumn(3,new Vector4(0,0,2,1));Set(batch,"CurveMatrix",curve);Set(batch,"Size",new Vector4(2,1,0,0));Set(batch,"Scale",Vector4.one);batches.SetValue(batch,j);
 }
 var wearBatch=batches.GetValue(2);var actualWear=Source(19,"arbitrary-original-source-label");var actualPath=(string)Get(actualWear.GetType().GetMethod("GetMeta",I).Invoke(actualWear,null),"path");File.Copy(Arg("-wearSurface"),actualPath,true);var actualSurfaces=Array.CreateInstance(surfaceType,1);actualSurfaces.SetValue(actualWear,0);var savedSurfaces=Get(wearBatch,"Surfaces");Set(wearBatch,"Surfaces",actualSurfaces);Need((bool)T("MovableNativeDecals").GetMethod("IsSelectedRoadWear",S).Invoke(null,new[]{wearBatch}),"actual original wear descriptor not recognized");Set(wearBatch,"Surfaces",savedSurfaces);count++;
 object[] attach={mechanism,batches,new Pose(Vector3.zero,Quaternion.identity),split,receivers,null};
 Need((bool)factoryType.GetMethods(I).Single(m=>m.Name=="TryAttachMovablePaint"&&m.GetParameters().Length==6).Invoke(factory,attach),"actual attachment failed: "+attach[5]);
 var outputs=(Array)meshField.GetValue(mechanism);Need(outputs.Length==3,"overlapping original rail and base were collapsed or replaced");
 double totalArea=0;for(int j=1;j<outputs.Length;j++){
  var render=Get(outputs.GetValue(j),"m_Mesh");object[] captured={render,null};Need((bool)T("PreviewGeometry").GetMethod("TryGetMeshes",I).Invoke(preview,captured),"native mesh missing");
  foreach(var mesh in (Mesh[])captured[1]){if(j==3)Need(mesh.boneWeights.All(w=>w.boneIndex0==0),"wear leaked to moving bone");var uv=mesh.uv;Need(uv.Length>0&&uv.Select(v=>v.x).Distinct().Count()>1&&uv.Select(v=>v.y).Distinct().Count()>1,"output uses constant white UV");var vertices=mesh.vertices;var indices=mesh.triangles;for(int k=0;k<indices.Length;k+=3)totalArea+=Vector3.Cross(vertices[indices[k+1]]-vertices[indices[k]],vertices[indices[k+2]]-vertices[indices[k]]).magnitude*.5;}
  object[] surfaces={render,null};Need((bool)T("PreviewGeometry").GetMethod("TryGetSurfaces",I).Invoke(preview,surfaces),"native Surface missing");var material=((Array)surfaces[1]).GetValue(0);Need((int)Get(material,"materialTemplateHash")==1,"source projection must use native deterministic alpha");
 }
 Need(Math.Abs(totalArea-8)<1e-5,"transparent original layers lost: "+totalArea);count++;
 Debug.Log("PASS production TryAttachMovablePaint: two original rail layers survive; mechanism wear excluded on moving and fixed end domains; varying original UV; native meshes remain after source Model disposal; area="+totalArea);
 foreach(var b in bones)Set(b,"m_Type",Enum.Parse(boneType.GetField("m_Type",I).FieldType,"None"));
 Need((bool)factoryType.GetMethods(I).Single(m=>m.Name=="TryAttachMovablePaint"&&m.GetParameters().Length==6).Invoke(factory,attach),"all-fixed mechanism attachment failed");Need(((Array)meshField.GetValue(mechanism)).Length==5,"fixed end wear was incorrectly retained");count++;
 Debug.Log("PASS "+count+" source, inherited-motion and mixed fixed/moving wear and projected receiver cases; actual native assemblies, fixture protocol shaders, no game visual claim.");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
