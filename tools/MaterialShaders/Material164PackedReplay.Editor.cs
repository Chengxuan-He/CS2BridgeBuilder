// Replays native packed road inputs; requires Material162BatchReplay.Editor.cs for optional BRG mode.
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.Rendering;using UnityEditor;
public static class Material164PackedReplay
{
    static string Root => (Environment.GetEnvironmentVariable("BB_MATERIAL_REPLAY") ?? throw new InvalidOperationException("Set BB_MATERIAL_REPLAY to the captured replay directory.")).TrimEnd('/', '\\') + "/";
    static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
    public static void Run()
    {
        bool dots=Environment.GetEnvironmentVariable("BB_REPLAY_DOTS")=="1";string previousHash=null;var results=new List<string>();var owned=new List<UnityEngine.Object>();var buffers=new List<ComputeBuffer>();int status=1;
        try
        {
            var bundle=AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("BB_MATERIAL_CANDIDATE") ?? throw new InvalidOperationException("Set BB_MATERIAL_CANDIDATE to the authored bundle."));owned.Add(bundle);
            var material=new Material(bundle.LoadAllAssets<Shader>()[0]);owned.Add(material);
            material.EnableKeyword("_GPU_ANIMATION_PROCEDURAL");material.EnableKeyword("_TANGENTSPACE_OCTO");material.EnableKeyword("DECALS_4RT");
            material.DisableKeyword("DOTS_INSTANCING_ON");material.SetVector("colossal_BoneParameters",Vector4.zero);
            material.SetFloat("_CullMode",0);material.SetFloat("_CullModeForward",0);material.SetFloat("_ZTestDepthEqualForOpaque",(float)CompareFunction.Always);material.SetFloat("_ZTestGBuffer",(float)CompareFunction.Always);
            material.SetColor("_BaseColor",Color.white);material.SetFloat("_WorldspaceUVScale",.4f);
            material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",1);material.SetFloat("_CoatStrength",1);
            material.SetFloat("_Snow",0);material.SetFloat("_Wetness",0);material.SetFloat("_Porosity",0);
            foreach(var name in new[]{"_BaseColorMap","_NormalMap","_MaskMap","_WorldspaceAlbedo","_WorldspaceNormalMap","_WorldspaceMaskMap"})
            {
                using(var reader=new BinaryReader(File.OpenRead(Root+name+".Texture2D")))
                {
                    int version=reader.ReadUInt16(),w=reader.ReadUInt16(),h=reader.ReadUInt16(),mips=reader.ReadByte();
                    string format=System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadByte()));
                    int filter=reader.ReadByte(),wrap=reader.ReadByte(),aniso=reader.ReadByte();
                    var texture=new Texture2D(w,h,TextureFormat.BC7,mips,!format.EndsWith("SRGB"));owned.Add(texture);
                    texture.LoadRawTextureData(reader.ReadBytes((int)(reader.BaseStream.Length-reader.BaseStream.Position)));texture.Apply(false,false);
                    texture.filterMode=(FilterMode)filter;texture.wrapMode=(TextureWrapMode)wrap;texture.anisoLevel=aniso;material.SetTexture(name,texture);
                }
            }
            var empty=new Texture2D(1,1,TextureFormat.RGBAFloat,false,true);empty.SetPixel(0,0,Color.clear);empty.Apply();owned.Add(empty);material.SetTexture("_EmissiveColorMap",empty);
            var mesh=new Mesh();owned.Add(mesh);mesh.indexFormat=IndexFormat.UInt32;
            using(var r=new BinaryReader(File.OpenRead(Root+"mesh0.bin")))
            {
                int count=r.ReadInt32(),indexCount=r.ReadInt32(),channels=r.ReadInt32();var index=new int[indexCount];for(int i=0;i<index.Length;i++)index[i]=r.ReadInt32();
                var desc=new VertexAttributeDescriptor[channels];var values=new byte[channels][];
                for(int i=0;i<channels;i++){int attr=r.ReadInt32(),fmt=r.ReadInt32(),dim=r.ReadInt32(),size=r.ReadInt32();desc[i]=new VertexAttributeDescriptor((VertexAttribute)attr,(VertexAttributeFormat)fmt,dim,0);values[i]=r.ReadBytes(size);}
                int stride=values.Sum(v=>v.Length/count);var packed=new byte[count*stride];int offset=0;for(int i=0;i<channels;i++){int size=values[i].Length/count;for(int v=0;v<count;v++)Buffer.BlockCopy(values[i],v*size,packed,v*stride+offset,size);offset+=size;} mesh.SetVertexBufferParams(count,desc);mesh.SetVertexBufferData(packed,0,0,packed.Length,0);
                mesh.SetIndices(index,MeshTopology.Triangles,0);mesh.bounds=new Bounds(new Vector3(0,10,0),new Vector3(35,2,200));
            }
            var g=new Vector4[183];g[20]=new Vector4(1f/18,0,0,0);g[21]=new Vector4(0,0,-1f/300,0);g[22]=new Vector4(0,1f/100,0,0);g[23]=new Vector4(0,0,.5f,1);g[44]=dots?Vector4.zero:new Vector4(20,-1000,30,0);
            var globals=new ComputeBuffer(g.Length,16,ComputeBufferType.Constant);buffers.Add(globals);globals.SetData(g);Shader.SetGlobalConstantBuffer("ShaderVariablesGlobal",globals,0,g.Length*16);
            var bones=new ComputeBuffer(48,4,ComputeBufferType.Raw);buffers.Add(bones);var matrix=new float[48];for(int b=0;b<3;b++)for(int i=0;i<4;i++)matrix[b*16+i*5]=1;bones.SetData(matrix);material.SetBuffer("_BoneTransforms",bones);Shader.SetGlobalBuffer("_BoneTransforms",bones);
            var targets=new RenderTexture[4];for(int i=0;i<4;i++){targets[i]=new RenderTexture(512,1024,i==0?24:0,RenderTextureFormat.ARGBFloat);targets[i].Create();owned.Add(targets[i]);RenderTexture.active=targets[i];GL.Clear(true,true,Color.clear);}
            for(int pose=0;pose<3;pose++)for(int keyword=0;keyword<2;keyword++)
            {
                if(keyword==0)material.DisableKeyword("_TANGENTSPACE_OCTO");else material.EnableKeyword("_TANGENTSPACE_OCTO");
                float degrees=pose==0?0:pose==1?31:62.075f;
                var skin=new Matrix4x4[3];skin[0]=Matrix4x4.identity;
                for(int b=1;b<3;b++){float sign=b==1?-1:1;var pivot=new Vector3(0,9.8f,sign*83.740234375f);skin[b]=Matrix4x4.Translate(pivot)*Matrix4x4.Rotate(Quaternion.Euler(sign*degrees,0,0))*Matrix4x4.Translate(-pivot);}
                for(int b=0;b<3;b++)for(int c=0;c<4;c++)for(int r=0;r<4;r++)matrix[b*16+c*4+r]=skin[b][r,c];bones.SetData(matrix);
                foreach(var rt in targets){RenderTexture.active=rt;GL.Clear(true,true,Color.clear);}
                Graphics.SetRenderTarget(targets.Select(t=>t.colorBuffer).ToArray(),targets[0].depthBuffer);
                if(dots) Material162Brg.Draw(mesh,material,targets,"GBuffer");
                else
                {
                    if(!material.SetPass(material.FindPass("GBuffer")))throw new Exception("Native GBuffer pass did not bind");
                    Graphics.DrawMeshNow(mesh,Matrix4x4.Translate(new Vector3(20,-1000,30)));
                }
                var hashes=new List<string>();int live=0;
                for(int channel=0;channel<targets.Length;channel++)
                {
                    RenderTexture.active=targets[channel];var check=new Texture2D(512,1024,TextureFormat.RGBAFloat,false,true);owned.Add(check);check.ReadPixels(new Rect(0,0,512,1024),0,0);check.Apply();
                    var pixels=check.GetPixels();int bad=pixels.Count(v=>!Finite(v.r)||!Finite(v.g)||!Finite(v.b)||!Finite(v.a));if(bad!=0)throw new Exception("Nonfinite native output pose="+pose+" keyword="+keyword+" channel="+channel+" count="+bad);
                    if(channel==0)live=pixels.Count(v=>v.a>.5f);
                    using(var sha=System.Security.Cryptography.SHA256.Create())hashes.Add(BitConverter.ToString(sha.ComputeHash(check.GetRawTextureData())).Replace("-",""));
                }
                if(live==0)throw new Exception("No rendered source pixels");
                string hash=string.Join(",",hashes);if(keyword==0)previousHash=hash;else if(hash!=previousHash)throw new Exception("Keyword changed native packed input protocol");
                results.Add("pose="+pose+" packedKeyword="+keyword+" pixels="+live+" all4MrtFinite=true hashes="+hash);
            }
            File.WriteAllLines(Root+(dots?"native164-packed-dots.txt":"native164-packed-replay.txt"),results);Debug.Log("PASS native164 packed native mesh: "+Root+"; closed/half/full-open; keyword off/on; non-DOTS uses nonzero world translation, BRG uses native test instance metadata; all four native GBuffer channels finite");status=0;
        }
        catch(Exception e){Debug.LogException(e);}
        finally{RenderTexture.active=null;foreach(var b in buffers)b.Dispose();foreach(var item in owned.AsEnumerable().Reverse())if(item!=null)UnityEngine.Object.DestroyImmediate(item);EditorApplication.Exit(status);}
    }
}
