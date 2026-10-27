using System;
using System.Linq;
using BridgeBuilder.Bridges;
using UnityEditor;
using UnityEngine;

// Offline-only test of the production atlas map and native
// crop sampler. This does not build or install the runtime shader bundle.
public static class PackingCheck
{
    public static void Run()
    {
        var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/PackingProbe.compute");
        if(shader==null)throw new Exception("Missing packing probe shader.");
        var units=new[]{
            new MovableStructurePacking.Unit{Id=0,Bounds=new Vector4(.125f,.125f,.875f,.625f),Min=new Vector2(-2,-1),Max=new Vector2(3,2)},
            new MovableStructurePacking.Unit{Id=1,Triangle=true,A=new Vector2(.125f,.25f),B=new Vector2(.75f,.2f),C=new Vector2(.3f,.875f),Min=new Vector2(-.2f,-.3f),Max=new Vector2(4,3.9f)},
            new MovableStructurePacking.Unit{Id=2,Triangle=true,A=new Vector2(.1f,.2f),B=new Vector2(.5f,.6f),C=new Vector2(.5001f,.60015f),Min=Vector2.zero,Max=Vector2.one},
            new MovableStructurePacking.Unit{Id=3,Triangle=true,A=new Vector2(.25f,.25f),B=new Vector2(.5f,.5f),C=new Vector2(.75f,.75f),Min=Vector2.zero,Max=Vector2.one},
            new MovableStructurePacking.Unit{Id=4,Bounds=new Vector4(.2f,.2f,.4f,.4f),Min=new Vector2(-1000,-1000),Max=new Vector2(1000,1000)},
            new MovableStructurePacking.Unit{Id=5,Bounds=new Vector4(.7f,.25f,.75f,.3f),Min=new Vector2(-1,-1),Max=new Vector2(2,2)},
            new MovableStructurePacking.Unit{Id=6,Triangle=true,A=new Vector2(.125f,.25f),B=new Vector2(.2f,.2f),C=new Vector2(.3f,.375f),Min=new Vector2(-.2f,-.3f),Max=new Vector2(1.1f,1.2f)}
        };
        var pages=MovableStructurePacking.Pack(units,new[]{new Vector2Int(256,128),new Vector2Int(64,64)},4096);
        if(pages.Any(p=>!p.Repeat || p.Cells.Count!=1))throw new Exception("Every measured island must use one independent repeating page.");
        var dimensions=pages.Select(p=>(p.Width,p.Height)).ToArray();
        foreach(var unit in units){unit.Min=new Vector2(-100000,-100000);unit.Max=new Vector2(100000,100000);}
        var expanded=MovableStructurePacking.Pack(units,new[]{new Vector2Int(256,128),new Vector2Int(64,64)},4096);
        if(!dimensions.SequenceEqual(expanded.Select(p=>(p.Width,p.Height))))
            throw new Exception("Receiver expansion changed repeat-unit resolution.");
        int checks=0,seamProbes=0;
        foreach(var size in new[]{new Vector2Int(256,128),new Vector2Int(64,64)})
        {
            var input=new Texture2D(size.x,size.y,TextureFormat.RGBAFloat,false,true){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[size.x*size.y];
            for(int y=0;y<size.y;y++)for(int x=0;x<size.x;x++)pixels[y*size.x+x]=new Color((x+.5f)/size.x,(y+.5f)/size.y,.7f,.6f);
            input.SetPixels(pixels);input.Apply(false,false);
            try
            {
                foreach(var page in pages)
                {
                    var target=new RenderTexture(page.Width,page.Height,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear){enableRandomWrite=true};
                    var read=new Texture2D(page.Width,page.Height,TextureFormat.RGBAFloat,false,true);
                    target.Create();
                    try
                    {
                        foreach(bool normal in new[]{false,true})
                        {
                            int kernel=shader.FindKernel("CropNativeAtlas");
                            shader.SetTexture(kernel,"MipSource",input);shader.SetTexture(kernel,"AtlasColor",target);
                            shader.SetInt("NativeCropNormal",normal?1:0);shader.SetInt("NativeCropSrgb",0);
                            foreach(var cell in page.Cells)
                            {
                                var u=cell.Unit;
                                shader.SetInt("NativeCropTriangle",u.Triangle?1:0);
                                shader.SetVector("NativeCrop",new Vector4(u.Bounds.x,u.Bounds.y,u.Bounds.z-u.Bounds.x,u.Bounds.w-u.Bounds.y));
                                shader.SetVector("NativeCropA",u.A);shader.SetVector("NativeCropB",u.B);shader.SetVector("NativeCropC",u.C);
                                shader.SetVector("NativeCropDestination",new Vector4(cell.X,cell.Y,cell.W,cell.H));
                                shader.SetVector("NativeCropOrigin",cell.SampleOrigin);shader.SetVector("NativeCropStepX",cell.SampleX);shader.SetVector("NativeCropStepY",cell.SampleY);
                                shader.Dispatch(kernel,(cell.W+7)/8,(cell.H+7)/8,1);
                            }
                            var old=RenderTexture.active;
                            try{RenderTexture.active=target;read.ReadPixels(new Rect(0,0,page.Width,page.Height),0,0);read.Apply(false,false);}
                            finally{RenderTexture.active=old;}
                            foreach(var cell in page.Cells)
                            for(int y=0;y<cell.H;y+=7)
                            for(int x=0;x<cell.W;x+=11)
                            {
                                var u=cell.Unit;var q=cell.SampleOrigin+cell.SampleX*(x+.5f)+cell.SampleY*(y+.5f);
                                q=new Vector2(q.x-Mathf.Floor(q.x),q.y-Mathf.Floor(q.y));
                                // CPU multiply/add and GPU fused arithmetic can
                                // choose opposite sides of an exact discontinuity.
                                // Record those probes separately, not as passes.
                                if(Mathf.Min(q.x,1-q.x,q.y,1-q.y)<.00001f || (u.Triangle && Mathf.Abs(q.x+q.y-1)<.00001f))
                                {seamProbes++;continue;}
                                bool reverse=u.Triangle && q.x+q.y>1;
                                if(reverse)q=Vector2.one-q;
                                var uv=u.Triangle?u.A+q.x*(u.B-u.A)+q.y*(u.C-u.A):new Vector2(u.Bounds.x+q.x*(u.Bounds.z-u.Bounds.x),u.Bounds.y+q.y*(u.Bounds.w-u.Bounds.y));
                                var expected=new Color(uv.x,uv.y,.7f,.6f);
                                if(reverse && normal)expected=new Color(1-uv.x*.6f,1-uv.y,.7f,1);
                                var actual=read.GetPixel(cell.X+x,cell.Y+y);
                                for(int channel=0;channel<4;channel++)
                                {
                                    if(Mathf.Abs(expected[channel]-actual[channel])>.0001f)
                                        throw new Exception($"Atlas crop mismatch: unit={u.Id}, source={size}, normal={normal}, pixel={x},{y}, channel={channel}, actual={actual}, expected={expected}.");
                                    checks++;
                                }
                            }
                        }
                    }
                    finally{target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(read);}
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(input);}
        }
        Debug.Log($"PASS structural atlas GPU crop: {checks} channel samples; {seamProbes} exact-seam probes excluded; {pages.Count(p=>!p.Repeat)} finite pages, {pages.Count(p=>p.Repeat)} repeating pages; rectangle, triangle, oblique and rank-one UVs; negative repeats; two source resolutions; RG/AG normal rotation.");
    }
}
