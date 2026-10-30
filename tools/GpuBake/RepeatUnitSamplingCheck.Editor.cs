using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class RepeatUnitSamplingCheck
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/RepeatUnit.compute");
            var method=typeof(PrepareRepeatUnits).GetMethod("Extract",BindingFlags.Static|BindingFlags.NonPublic);
            Texture2D Extract(Texture2D source,double x,double y,double sx,double sy,int w,int h,
                bool ru,bool rv,bool grid,bool normal=false,double[] frame=null,PrepareRepeatUnits.Band[] bands=null)
                =>(Texture2D)method.Invoke(null,new object[]{shader,source,x,y,sx,sy,w,h,1,1,bands,ru,rv,grid,normal,frame,false,null});
            var source=new Texture2D(16,16,TextureFormat.RGBA32,true,true){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};
            try
            {
                for(int mip=0;mip<source.mipmapCount;mip++)
                {
                    int n=Math.Max(1,16>>mip);var data=new Color32[n*n];
                    for(int y=0;y<n;y++)for(int x=0;x<n;x++)data[y*n+x]=new Color32((byte)(x*13),(byte)(y*13),77,255);
                    source.SetPixels32(data,mip);
                }
                source.Apply(false,false);
                var unit=new PrepareRepeatUnits.Unit{min=new[]{3.25/16,4d/16},max=new[]{9.5/16,12d/16},repeatAxes="V",finiteSourceGrid=true};
                var grid=PrepareRepeatUnits.Grid(unit,16,16,false);
                var fractional=new PrepareRepeatUnits.Unit{min=new[]{.1,.123},max=new[]{.4,.777},repeatAxes="V",finiteSourceGrid=true};
                var fractionalGrid=PrepareRepeatUnits.Grid(fractional,16,16,true);
                Require(fractionalGrid.Y==.123*16 && fractionalGrid.SY==(.777-.123)*16,"Fractional repeat bounds were rounded or rejected.");
                var lifted=new PrepareRepeatUnits.Unit{min=new[]{0d,188.875/4096},max=new[]{.125,222.75/4096},repeatAxes="U",finiteSourceGrid=true,finiteGridMip=9};
                var aligned=PrepareRepeatUnits.Grid(lifted,4096,4096,true);
                Require(aligned.Y==0 && aligned.Height==512 && aligned.Min[1]==188.875/512 && aligned.Max[1]==222.75/512,"Measured Lift02 finite grid alignment differs.");
                var actual=Extract(source,grid.X,grid.Y,grid.SX,grid.SY,grid.Width,grid.Height,false,true,true);
                try
                {
                    var pixels=actual.GetPixels32(0);var original=source.GetPixels32(0);
                    for(int y=0;y<actual.height;y++)for(int x=0;x<actual.width;x++)
                    {
                        var expected=original[Mathf.Clamp((int)grid.Y+y,0,15)*16+Mathf.Clamp((int)grid.X+x,0,15)];
                        Require(pixels[y*actual.width+x].Equals(expected),"Finite source-grid mip0 changed a source texel.");
                    }
                    Require(grid.Min[0]!=.25 || grid.Max[0]!=.75,"Finite grid silently retained resampling coordinates.");
                }
                finally {UnityEngine.Object.DestroyImmediate(actual);}

                for(int mip=0;mip<source.mipmapCount;mip++)
                {
                    int n=Math.Max(1,16>>mip);var data=new Color[n*n];
                    for(int i=0;i<data.Length;i++)data[i]=mip==0?Color.red:mip==1?Color.green:Color.blue;
                    source.SetPixels(data,mip);
                }
                source.Apply(false,false);
                actual=Extract(source,0,0,4,4,2,2,true,true,false);
                try{Require(actual.GetPixel(0,0).g>.99f,"Source LOD did not follow actual source/content density.");}
                finally {UnityEngine.Object.DestroyImmediate(actual);}
                for(int mip=0;mip<source.mipmapCount;mip++)
                {
                    int n=Math.Max(1,16>>mip);var data=new Color[n*n];
                    for(int i=0;i<data.Length;i++)data[i]=new Color(mip/4f,0,0,1);
                    source.SetPixels(data,mip);
                }
                source.Apply(false,false);
                actual=Extract(source,0,0,16,16,3,3,true,true,false);
                try{Require(actual.GetPixel(0,0,1).r>.99f,"NPOT mip floor was ignored in source footprint LOD.");}
                finally {UnityEngine.Object.DestroyImmediate(actual);}

                for(int mip=0;mip<source.mipmapCount;mip++)
                {
                    int n=Math.Max(1,16>>mip);var data=new Color[n*n];
                    for(int i=0;i<data.Length;i++)data[i]=new Color(.75f,.5f,1,1);
                    source.SetPixels(data,mip);
                }
                source.Apply(false,false);
                actual=Extract(source,0,0,16,16,16,16,false,false,true,true,new double[]{0,-1,0,1,0,0,0,0,1});
                try
                {
                    var c=actual.GetPixel(8,8);
                    Require(Math.Abs(c.r-.5)<.01 && Math.Abs(c.g-.75)<.01,"Measured normal frame was not applied.");
                    Require(actual.wrapMode==TextureWrapMode.Clamp,"Complete finite appearance does not use native Clamp.");
                }
                finally {UnityEngine.Object.DestroyImmediate(actual);}
                for(int mip=0;mip<source.mipmapCount;mip++)
                {
                    int n=Math.Max(1,16>>mip);var data=new Color[n*n];
                    for(int i=0;i<data.Length;i++)data[i]=new Color(1,.5f,1,.75f);
                    source.SetPixels(data,mip);
                }
                source.Apply(false,false);
                var band=new PrepareRepeatUnits.Band{start=0,end=1,min=new[]{0d,0},max=new[]{1d,1},flipU=true,normalTransform=new double[]{1,0,0,0,1,0,0,0,1}};
                actual=Extract(source,0,0,16,16,16,16,true,true,false,true,null,new[]{band});
                try
                {
                    var c=actual.GetPixel(8,8);
                    Require(Math.Abs(c.r-1)<.01 && Math.Abs(c.a-.75)<.01 && Math.Abs(c.g-.5)<.01,"Identity frame rewrote packed AG normal channels.");
                }
                finally {UnityEngine.Object.DestroyImmediate(actual);}
            }
            finally {UnityEngine.Object.DestroyImmediate(source);}
            var patterned=new Texture2D(64,64,TextureFormat.RGBA32,true,true){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};
            try
            {
                for(int mip=0;mip<patterned.mipmapCount;mip++)
                {
                    int n=Math.Max(1,64>>mip);var data=new Color32[n*n];
                    for(int y=0;y<n;y++)for(int x=0;x<n;x++)data[y*n+x]=new Color32((byte)((x*37+mip*11)%256),(byte)((y*53+mip*7)%256),(byte)((x*19+y*31)%256),255);
                    patterned.SetPixels32(data,mip);
                }
                patterned.Apply(false,false);
                var unit=new PrepareRepeatUnits.Unit{min=new[]{0d,18.875/64},max=new[]{1d,26.75/64},repeatAxes="U",finiteSourceGrid=true,finiteGridMip=3};
                var grid=PrepareRepeatUnits.Grid(unit,64,64,false);
                var actual=Extract(patterned,grid.X,grid.Y,grid.SX,grid.SY,grid.Width,grid.Height,true,false,true);
                try
                {
                    for(int mip=0;mip<=3;mip++)
                    {
                        int sourceSize=64>>mip,w=actual.width>>mip,h=actual.height>>mip;
                        var original=patterned.GetPixels32(mip);var pixels=actual.GetPixels32(mip);
                        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                        {
                            var expected=original[((int)grid.Y/(1<<mip)+y)*sourceSize+x];
                            var got=pixels[y*w+x];
                            Require(Math.Abs(got.r-expected.r)<=1&&Math.Abs(got.g-expected.g)<=1&&Math.Abs(got.b-expected.b)<=1,
                                "Aligned finite grid changed source sampling at mip "+mip+" texel "+x+","+y);
                        }
                    }
                }
                finally {UnityEngine.Object.DestroyImmediate(actual);}
                actual=Extract(patterned,0,grid.Y+1,grid.SX,grid.SY,grid.Width,grid.Height,true,false,true);
                try
                {
                    var pixels=actual.GetPixels32(1);var original=patterned.GetPixels32(1);int mismatches=0;
                    for(int y=0;y<(actual.height>>1);y++)for(int x=0;x<(actual.width>>1);x++)
                    {
                        var expected=original[((int)(grid.Y+1)/2+y)*32+x];
                        var got=pixels[y*(actual.width>>1)+x];
                        if(Math.Abs(got.g-expected.g)>1||Math.Abs(got.b-expected.b)>1)mismatches++;
                    }
                    Require(mismatches>0,"Odd-origin negative control failed to expose half-texel phase drift.");
                }
                finally {UnityEngine.Object.DestroyImmediate(actual);}
            }
            finally {UnityEngine.Object.DestroyImmediate(patterned);}
            Debug.Log("PASS offline unit sampling: patterned source-grid mips 0..3, odd-origin negative control, fractional repeat bounds, NPOT source LOD, identity AG preservation, measured normal frame, independent UV mirror, native finite Clamp. Coarser atlas boundary equivalence remains per-unit validation.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
