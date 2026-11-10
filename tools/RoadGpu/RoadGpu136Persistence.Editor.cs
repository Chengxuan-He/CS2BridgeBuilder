using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;using UnityEngine.Rendering;
public static class RoadGpu136Persistence {
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
 public static string Check(object model,string managed,string path,Dictionary<VertexAttribute,byte[]> expected){
 var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));var pipeline=Assembly.LoadFrom(Path.Combine(managed,"Colossal.AssetPipeline.dll"));
 var type=db.GetType("Colossal.IO.AssetDatabase.GeometryAsset",true);var dt=type.GetNestedType("Data",F);var lt=type.GetNestedType("Loading",F);
 var gt=pipeline.GetType("Colossal.AssetPipeline.Geometry",true);var models=Array.CreateInstance(model.GetType(),1);models.SetValue(model,0);var geometry=Activator.CreateInstance(gt,new object[]{models});
 object data=Activator.CreateInstance(dt),loaded=Activator.CreateInstance(dt);object[] args={data,geometry};type.GetMethod("CreateDataFromGeometry",F).Invoke(null,args);data=args[0];
 try{
 using(var writer=new BinaryWriter(File.Create(path))){writer.Write((ushort)1);object[] save={writer,data};db.GetType("Colossal.IO.AssetDatabase.GeometryAssetSave",true).GetMethod("SaveToFile",F).Invoke(null,save);data=save[1];}
 var desc=Activator.CreateInstance(db.GetType("Colossal.IO.AssetDatabase.AsyncReadDescriptor",true),new object[]{"GPU persistence check",path});object[] read={desc,(uint)16383,loaded,Activator.CreateInstance(lt)};type.GetMethod("LoadSync",F).Invoke(null,read);loaded=read[2];
 var get=type.GetMethods(F).Single(m=>m.Name=="GetAttributeData"&&m.IsStatic);
 foreach(var pair in expected){object[] a={loaded,0,pair.Key};var slice=get.Invoke(null,a);loaded=a[0];var bytes=(byte[])slice.GetType().GetMethod("ToArray",Type.EmptyTypes).Invoke(slice,null);if(!bytes.SequenceEqual(pair.Value))throw new Exception("Persisted native channel differs: "+pair.Key);}
 return "PASS native GeometryAsset.CreateDataFromGeometry -> GeometryAssetSave.SaveToFile -> GeometryAsset.LoadSync; all 6 channels byte exact, fileBytes="+new FileInfo(path).Length+"\\n";
 }finally{((IDisposable)data).Dispose();((IDisposable)loaded).Dispose();}
 }
}
