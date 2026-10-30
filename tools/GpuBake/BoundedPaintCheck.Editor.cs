using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class BoundedPaintCheck
{
 [Serializable]class Recipe{public PrepareRepeatUnits.PaintMip[] sourceMipRules;}
 public static void Run(){try{
 var recipe=JsonUtility.FromJson<Recipe>(File.ReadAllText(@"C:\Users\admin\Documents\Codex\CS2BridgeBuilder\artifacts\material-remap\Draw02\fixed-car\paint-composition-recipe.json"));
 var validate=typeof(PrepareRepeatUnits).GetMethod("ValidatePaint",BindingFlags.NonPublic|BindingFlags.Static);
 validate.Invoke(null,new object[]{recipe.sourceMipRules,4096,4096,13});
 var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/RepeatUnit.compute");
 var method=typeof(PrepareRepeatUnits).GetMethod("ExtractCore",BindingFlags.NonPublic|BindingFlags.Static);
 var source=new Texture2D(16,16,TextureFormat.RGBA32,true,true){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear,anisoLevel=1};
 var rules=new PrepareRepeatUnits.PaintMip[5];
 for(int mip=0;mip<5;mip++){
 int n=Math.Max(1,16>>mip);var pixels=new Color32[n*n];
 for(int y=0;y<n;y++)for(int x=0;x<n;x++)pixels[y*n+x]=new Color32((byte)(x*11+mip*23),(byte)(y*13+mip*17),(byte)(x*7+y*3+mip*19),(byte)(180+x*2));
 source.SetPixels32(pixels,mip);
 int start=n>1?1:0,end=n>2?n/2+1:n;
 rules[mip]=new PrepareRepeatUnits.PaintMip{sourceMip=mip,width=n,height=n,masks=new[]{new PrepareRepeatUnits.PaintMask{sourceTexelRectHalfOpen=new[]{start,0,end,n},sourceUvRect=new[]{(double)start/n,0d,(double)end/n,1d},donorSourceTexelX=0,donorSourceUvX=.5/n}}};
 }source.Apply(false,false);
 Texture2D Bake(int w,PrepareRepeatUnits.PaintMip[] masks,bool grad=false)=> (Texture2D)method.Invoke(null,new object[]{shader,source,0d,0d,16d,16d,w,w,1,1,null,true,true,false,false,null,grad,null,masks});
 int outside=0,changed=0,checkedSamples=0;
 foreach(int width in new[]{16,12}){
 var plain=Bake(width,null);var painted=Bake(width,rules);
 for(int mip=0;mip<painted.mipmapCount;mip++){
 int n=Math.Max(1,width>>mip);double level=Math.Min(4,Math.Log(16d/n,2));int lo=(int)Math.Floor(level),hi=Math.Min(4,lo+1);float blend=(float)(level-lo);
 var a=plain.GetPixels32(mip);var b=painted.GetPixels32(mip);
 for(int y=0;y<n;y++)for(int x=0;x<n;x++){
 float u=(x+.5f)/n,v=(y+.5f)/n;
 bool Gate(int m){var r=rules[m].masks[0].sourceUvRect;return u>=r[0]&&u<r[2]&&v>=r[1]&&v<r[3];}
 Color Sample(int m){int size=Math.Max(1,16>>m);float su=Gate(m)?(float)rules[m].masks[0].donorSourceUvX:u;
 float px=su*size-.5f,py=v*size-.5f;int ix=Mathf.FloorToInt(px),iy=Mathf.FloorToInt(py);float fx=px-ix,fy=py-iy;
 Color C(int xx,int yy)=>source.GetPixels32(m)[Mathf.Clamp(yy,0,size-1)*size+Mathf.Clamp(xx,0,size-1)];
 return Color.LerpUnclamped(Color.LerpUnclamped(C(ix,iy),C(ix+1,iy),fx),Color.LerpUnclamped(C(ix,iy+1),C(ix+1,iy+1),fx),fy);}
 var expected=(Color32)Color.LerpUnclamped(Sample(lo),Sample(hi),blend);int i=y*n+x;
 if(!Gate(lo)&&!Gate(hi)){if(!a[i].Equals(b[i]))throw new Exception("Outside-mask filtered byte changed");outside++;}
 if(!a[i].Equals(b[i]))changed++;
 var av=new[]{b[i].r,b[i].g,b[i].b,b[i].a};var ev=new[]{expected.r,expected.g,expected.b,expected.a};
 for(int c=0;c<4;c++)if((Gate(lo)||Gate(hi))&&Math.Abs(av[c]-ev[c])>1)throw new Exception($"Integer-mip donor/LOD mismatch {width}/{mip}/{x}/{y}/{c} actual={av[c]} expected={ev[c]} plain={a[i]} level={level}");checkedSamples++;
 }}UnityEngine.Object.DestroyImmediate(plain);UnityEngine.Object.DestroyImmediate(painted);
 }
 bool rejected=false;try{Bake(16,rules,true);}catch(TargetInvocationException e){rejected=e.InnerException is InvalidDataException;}if(!rejected)throw new Exception("Unproved SampleGrad paint accepted");
 if(outside==0||changed==0)throw new Exception("Paint fixture did not exercise isolation and replacement");
 var panelUnit=new PrepareRepeatUnits.Unit{min=new[]{0d,0d},max=new[]{1d,1d},repeatAxes="UV",panels=new[]{new PrepareRepeatUnits.Panel{target=new[]{0d,0d,1d,0d,0d,1d},source=new[]{0d,0d,.5d,0d,.5d,.5d}}}};
 var grid=PrepareRepeatUnits.Grid(panelUnit,16,16,false);
 var jac=typeof(PrepareRepeatUnits).GetMethod("PanelJacobians",BindingFlags.NonPublic|BindingFlags.Static);
 var sigma=typeof(PrepareRepeatUnits).GetMethod("MaxSingular",BindingFlags.NonPublic|BindingFlags.Static);
 double S(int w,int h)=>(double)sigma.Invoke(null,new object[]{((double[][])jac.Invoke(null,new object[]{panelUnit,16,16,w,h}))[0]});
 if(S(grid.Width,grid.Height)<=1)throw new Exception("Sheared panel negative control missing");
 panelUnit.fullDirectionalDensity=true;var dense=PrepareRepeatUnits.Grid(panelUnit,16,16,false);
 if(S(dense.Width,dense.Height)>1||dense.Width<=grid.Width||dense.Height<=grid.Height)throw new Exception("Full-directional density did not bound singular value");
 Debug.Log($"PASS full Jacobian density: sigma {S(grid.Width,grid.Height):R} -> {S(dense.Width,dense.Height):R}; original source affine retained.");
 Debug.Log($"PASS bounded paint: real 13-mip recipe validated; {checkedSamples} GPU samples, {outside} exact outside-mask samples, {changed} changed donor samples; integer/fractional LOD; SampleGrad rejected.");
 UnityEngine.Object.DestroyImmediate(source);RepeatUnitSamplingCheck.Run();
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
