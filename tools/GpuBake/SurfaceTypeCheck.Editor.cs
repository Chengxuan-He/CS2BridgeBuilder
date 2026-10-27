using System;
using UnityEditor;
using UnityEngine;

// Executes the production generation kernel on mixed asphalt/non-asphalt receivers.
public static class SurfaceTypeCheck
{
    public static void Run()
    {
        var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/BridgeBake.compute");
        CheckClassification(shader);
        CheckTriangleClassification(shader);
        CheckNativeCrop(shader);
        CheckTriangularCrop(shader);

        CheckPeriodicComposite(shader);
        foreach (int metres in new[] { 8, 16 })
        {
            foreach (bool replace in new[] { false, true }) Check(shader, metres, replace);
            Check(shader, metres, false, true);
        }
        Debug.Log("PASS surface masks, original non-asphalt, unsupported road layers, prototype patch crops and fixed physical repeat scale at 8/16 metres.");
    }

    private static void CheckPeriodicComposite(ComputeShader shader)
    {
        int kernel=shader.FindKernel("SurfaceAtlas"),width=64,height=256;
        var targets=new RenderTexture[4];var inputs=new System.Collections.Generic.List<Texture2D>();
        var names=new[]{"AtlasColor","AtlasNormal","AtlasMask","AtlasControl"};
        Texture2D Make(int w,int h,Func<int,int,Color> value)
        {
            var t=new Texture2D(w,h,TextureFormat.RGBAFloat,true,true){wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};
            var p=new Color[w*h];for(int y=0;y<h;y++)for(int x=0;x<w;x++)p[y*w+x]=value(x,y);
            t.SetPixels(p);t.Apply();inputs.Add(t);return t;
        }
        var color=Make(16,64,(x,y)=>new Color(x%2,y%2,.2f,1));
        var mask=Make(16,64,(x,y)=>new Color(.3f,.6f,x<8?0:1,.5f));
        var normal=Make(16,64,(x,y)=>new Color(.5f+(x%2)*.1f,.5f+(y%2)*.1f,1,1));
        var road=Make(32,32,(x,y)=>new Color(.1f,(x%2)*.6f,(y%2)*.6f,1));
        var read=new Texture2D(width,height,TextureFormat.RGBAFloat,false,true);
        using var bands=new ComputeBuffer(2,16);
        bands.SetData(new[]{new Vector4(0,1,1,0),new Vector4(0,0,1,1)});
        try
        {
            for(int i=0;i<4;i++)
            {
                targets[i]=new RenderTexture(width,height,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear){enableRandomWrite=true};
                targets[i].Create();shader.SetTexture(kernel,names[i],targets[i]);
            }
            shader.SetTexture(kernel,"_BaseColorMap",color);shader.SetTexture(kernel,"_NormalMap",normal);
            shader.SetTexture(kernel,"_MaskMap",mask);shader.SetTexture(kernel,"_ControlMask",color);
            shader.SetTexture(kernel,"_WorldspaceAlbedo",road);shader.SetTexture(kernel,"_WorldspaceNormalMap",normal);
            shader.SetTexture(kernel,"_WorldspaceMaskMap",mask);shader.SetTexture(kernel,"_Road_BaseColorMap",road);
            shader.SetTexture(kernel,"_Road_NormalMap",normal);shader.SetTexture(kernel,"_Road_MaskMap",mask);
            shader.SetBuffer(kernel,"RoadBands",bands);shader.SetInt("RoadBandCount",1);shader.SetInt("RoadBandRows",0);
            shader.SetInt("OriginalRoad",1);
            shader.SetVector("PatchU",new Vector4(.25f,0,0,.17f));shader.SetVector("PatchV",new Vector4(0,0,-.125f,.23f));
            shader.SetVector("PatchArea",new Vector4(0,0,1,1));
            shader.SetVector("RoadU",new Vector4(.25f,0,0,.1f));shader.SetVector("RoadV",new Vector4(0,0,-.0625f,.3f));
            shader.SetVector("ChartU",Vector3.right);shader.SetVector("ChartV",Vector3.back);
            shader.SetVector("AtlasPixels",new Vector4(16,0,16,0));shader.SetVector("AtlasRect",new Vector4(0,0,width,height));
            shader.SetMatrix("Frame",Matrix4x4.identity);shader.SetFloat("WorldScale",.125f);
            shader.SetVector("BaseColor",Color.white);shader.SetVector("NativeValues",Vector4.one);
            foreach(bool replace in new[]{false,true})
            {
                shader.SetInt("ReplaceRoad",replace?1:0);
                var reference=new Color[4][];
                for(int tile=0;tile<5;tile++)
                {
                    shader.SetVector("ChartOrigin",new Vector3(.31f,0,-1.4f-16*tile));
                    shader.Dispatch(kernel,width/8,height/8,1);
                    for(int channel=0;channel<4;channel++)
                    {
                        var previous=RenderTexture.active;
                        try{RenderTexture.active=targets[channel];read.ReadPixels(new Rect(0,0,width,height),0,0);read.Apply();}
                        finally{RenderTexture.active=previous;}
                        var actual=read.GetPixels();
                        if(tile==0){reference[channel]=actual;continue;}
                        for(int pixel=0;pixel<actual.Length;pixel++)for(int component=0;component<4;component++)
                            if(Mathf.Abs(actual[pixel][component]-reference[channel][pixel][component])>.0002f)
                                throw new Exception($"Periodic mixed composite changed native phase: replace={replace}, tile={tile}, channel={channel}, pixel={pixel}");
                    }
                }
            }
            Debug.Log("PASS periodic mixed composite matches direct native sampling over 80 metres for color, normal, mask and control, with and without prototype asphalt.");
        }
        finally
        {
            foreach(var t in targets)if(t!=null){t.Release();UnityEngine.Object.DestroyImmediate(t);}
            foreach(var t in inputs)UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(read);
        }
    }

    private static void CheckNativeCrop(ComputeShader shader)
    {
        // Different channel sizes retain their own texels; no shared base-color
        // extent or whole-span resize may discard a higher-resolution normal map.
        foreach (int width in new[] { 32, 128 })
        foreach (bool srgb in new[] { false, true })
        {
            var input=new Texture2D(width,16,TextureFormat.RGBAFloat,false,true);
            var pixels=new Color[width*16];
            for(int y=0;y<16;y++)for(int x=0;x<width;x++)
                pixels[y*width+x]=new Color((x%2)*.5f,y/16f,.25f,(x%3)/3f);
            input.SetPixels(pixels);input.Apply();input.filterMode=FilterMode.Bilinear;
            var target=new RenderTexture(width/2,8,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear)
            {enableRandomWrite=true};target.Create();
            var read=new Texture2D(width/2,8,TextureFormat.RGBAFloat,false,true);
            try
            {
                int kernel=shader.FindKernel("CropNative");
                shader.SetTexture(kernel,"MipSource",input);shader.SetTexture(kernel,"AtlasColor",target);
                shader.SetVector("NativeCrop",new Vector4(.25f,.25f,.5f,.5f));shader.SetInt("NativeCropSrgb",srgb?1:0);
                shader.SetInt("NativeCropTriangle",0);shader.SetInt("NativeCropNormal",0);
                shader.Dispatch(kernel,(width/2+7)/8,1,1);
                var previous=RenderTexture.active;
                try {RenderTexture.active=target;read.ReadPixels(new Rect(0,0,width/2,8),0,0);read.Apply();}
                finally {RenderTexture.active=previous;}
                for(int y=0;y<8;y++)for(int x=0;x<width/2;x++)
                {
                    var expected=pixels[(y+4)*width+x+width/4];
                    if(srgb)expected=new Color(Mathf.LinearToGammaSpace(expected.r),Mathf.LinearToGammaSpace(expected.g),Mathf.LinearToGammaSpace(expected.b),expected.a);
                    var actual=read.GetPixel(x,y);
                    for(int c=0;c<4;c++)if(Mathf.Abs(expected[c]-actual[c])>.001f)
                        throw new Exception($"Native crop lost source texels: {width}/{srgb}, pixel={x},{y}, channel={c}.");
                }
            }
            finally {target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(input);UnityEngine.Object.DestroyImmediate(read);}
        }
        Debug.Log("PASS native structural crop source texels, independent channel dimensions, alpha and sRGB.");
    }

    private static void CheckTriangularCrop(ComputeShader shader)
    {
        var a=new Vector2(.125f,.25f);var b=new Vector2(.75f,.2f);var c=new Vector2(.3f,.875f);
        var input=new Texture2D(128,64,TextureFormat.RGBAFloat,false,true);
        var pixels=new Color[128*64];
        for(int y=0;y<64;y++)for(int x=0;x<128;x++)pixels[y*128+x]=new Color((x+.5f)/128,(y+.5f)/64,.7f,.6f);
        input.SetPixels(pixels);input.Apply();input.filterMode=FilterMode.Bilinear;
        var target=new RenderTexture(64,32,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear){enableRandomWrite=true};target.Create();
        var read=new Texture2D(64,32,TextureFormat.RGBAFloat,false,true);
        try
        {
            foreach(bool normal in new[]{false,true})
            {
                int kernel=shader.FindKernel("CropNative");
                shader.SetTexture(kernel,"MipSource",input);shader.SetTexture(kernel,"AtlasColor",target);
                shader.SetVector("NativeCropA",a);shader.SetVector("NativeCropB",b);shader.SetVector("NativeCropC",c);
                shader.SetInt("NativeCropTriangle",1);shader.SetInt("NativeCropNormal",normal?1:0);shader.SetInt("NativeCropSrgb",0);
                shader.Dispatch(kernel,8,4,1);
                var previous=RenderTexture.active;
                try {RenderTexture.active=target;read.ReadPixels(new Rect(0,0,64,32),0,0);read.Apply();}
                finally {RenderTexture.active=previous;}
                for(int y=0;y<32;y++)for(int x=0;x<64;x++)
                {
                    var q=new Vector2((x+.5f)/64,(y+.5f)/32);bool reversed=q.x+q.y>1;
                    if(reversed)q=Vector2.one-q;
                    var uv=a+q.x*(b-a)+q.y*(c-a);
                    var expected=new Color(uv.x,uv.y,.7f,.6f);
                    if(reversed && normal)expected=new Color(1-uv.x*.6f,1-uv.y,.7f,1);
                    var actual=read.GetPixel(x,y);
                    for(int channel=0;channel<4;channel++)if(Mathf.Abs(actual[channel]-expected[channel])>.001f)
                        throw new Exception($"Triangular crop mismatch {normal}/{x}/{y}/{channel}: {actual} != {expected}");
                }
            }
        }
        finally {target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(input);UnityEngine.Object.DestroyImmediate(read);}
        Debug.Log("PASS triangular crop samples only the recorded source triangle, preserves alpha, and rotates RG/AG normal directions.");
    }

    private static void CheckClassification(ComputeShader shader)
    {
        var texture=new Texture2D(64,64,TextureFormat.RGBAFloat,false,true);
        var pixels=new Color[64*64];
        for(int i=0;i<pixels.Length;i++)pixels[i]=Color.white;
        // A single non-asphalt texel inside a large region must not be missed.
        pixels[37*64+43]=Color.black;
        texture.SetPixels(pixels); texture.Apply();
        var rectangles=new[]{new Vector4(.1f,.1f,.2f,.2f),new Vector4(0,0,1,1),
            new Vector4(-2,-2,3,3),new Vector4(43.5f/64,37.5f/64,43.5f/64,37.5f/64)};
        using var bounds=new ComputeBuffer(rectangles.Length,16);
        using var results=new ComputeBuffer(rectangles.Length,4);
        try
        {
            bounds.SetData(rectangles);
            var kernel=shader.FindKernel("ClassifyAsphalt");
            shader.SetTexture(kernel,"_MaskMap",texture);
            shader.SetBuffer(kernel,"AsphaltBounds",bounds); shader.SetBuffer(kernel,"AsphaltOnly",results);
            shader.Dispatch(kernel,rectangles.Length,1,1);
            var actual=new uint[rectangles.Length]; results.GetData(actual);
            for(int i=0;i<actual.Length;i++)if(actual[i]!=(i==0?1u:0u))
                throw new Exception("Asphalt footprint classification failed at "+i);
            Debug.Log("PASS native-resolution routing: pure asphalt, interior mixed texel, repeated UVs and point footprint.");
        }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
    }

    private static void CheckTriangleClassification(ComputeShader shader)
    {
        var texture=new Texture2D(64,64,TextureFormat.RGBAFloat,false,true);
        var pixels=new Color[64*64];
        for(int i=0;i<pixels.Length;i++)pixels[i]=Color.white;
        // Inside the bounding rectangle, outside the lower-left triangle.
        pixels[50*64+50]=Color.black;
        texture.SetPixels(pixels);texture.Apply();
        var triangles=new[]{
            new Vector4(.1f,.1f),new Vector4(.9f,.1f),new Vector4(.1f,.9f),
            new Vector4(.1f,.9f),new Vector4(.9f,.1f),new Vector4(.9f,.9f),
            new Vector4(1.7f,.7f),new Vector4(1.9f,.7f),new Vector4(1.8f,.9f),
            new Vector4(50.5f/64,50.5f/64),new Vector4(50.5f/64,50.5f/64),new Vector4(50.5f/64,50.5f/64),
            // Bilinear support reaches the mixed texel even outside the face.
            new Vector4(49.75f/64,50.5f/64),new Vector4(49.75f/64,50.5f/64),new Vector4(49.75f/64,50.5f/64)};
        using var input=new ComputeBuffer(triangles.Length,16);
        using var results=new ComputeBuffer(triangles.Length/3,4);
        try
        {
            input.SetData(triangles);var kernel=shader.FindKernel("ClassifyAsphaltTriangles");
            shader.SetTexture(kernel,"_MaskMap",texture);
            shader.SetBuffer(kernel,"AsphaltTriangles",input);shader.SetBuffer(kernel,"AsphaltOnly",results);
            foreach(int mode in new[]{0,1})
            {
                shader.SetInt("AsphaltWrapU",mode);shader.SetInt("AsphaltWrapV",mode);
                shader.Dispatch(kernel,triangles.Length/3,1,1);
                var actual=new uint[triangles.Length/3];results.GetData(actual);
                var expected=new uint[]{1,0,mode==1?1u:0u,0,0};
                for(int i=0;i<actual.Length;i++)if(actual[i]!=expected[i])
                    throw new Exception($"Triangle footprint mismatch: wrap={mode}, face={i}, expected={expected[i]}, actual={actual[i]}");
            }
            Debug.Log("PASS actual triangle footprint, mixed interior, bilinear border, degenerate UVs and native clamp/repeat addressing.");
        }
        finally{UnityEngine.Object.DestroyImmediate(texture);}
    }

    private static void Check(ComputeShader shader, int metres, bool replace, bool prototypePatch = false)
    {
        int width = metres * 16, height = 8, kernel = shader.FindKernel("SurfaceAtlas");
        var outputs = new RenderTexture[4];
        var names = new[] { "AtlasColor", "AtlasNormal", "AtlasMask", "AtlasControl" };
        for (int i = 0; i < 4; i++)
        {
            outputs[i] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) { enableRandomWrite = true };
            outputs[i].Create(); shader.SetTexture(kernel, names[i], outputs[i]);
        }
        Texture2D Texture(Color a, Color b)
        {
            var t = new Texture2D(2, 1, TextureFormat.RGBAFloat, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            t.SetPixels(new[] { a, b }); t.Apply(); return t;
        }
        var red = Texture(Color.red, Color.red);
        var green = Texture(Color.green, Color.green);
        var mask = Texture(new Color(0, 0, 0, 1), new Color(0, 0, 1, 1));
        var road = Texture(Color.blue, Color.white);
        var normal = Texture(new Color(.5f, .5f, 1, 1), new Color(.5f, .5f, 1, 1));
        var black = Texture(Color.black, Color.black);
        var white = Texture(Color.white, Color.white);
        var prototype = new Texture2D(4,1,TextureFormat.RGBAFloat,false,true) {filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
        prototype.SetPixels(new[]{Color.red,Color.blue,Color.white,Color.green});prototype.Apply();
        using var bands = new ComputeBuffer(2, 16);
        bands.SetData(new[] { new Vector4(0, 1, 1, 0), new Vector4(0, 0, 1, 1) });
        var read = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);
        try
        {
            shader.SetTexture(kernel, "_BaseColorMap", prototypePatch ? prototype : red);
            shader.SetTexture(kernel, "_NormalMap", normal);
            shader.SetTexture(kernel, "_MaskMap", mask);
            shader.SetTexture(kernel, "_ControlMask", black);
            shader.SetTexture(kernel, "_WorldspaceAlbedo", green);
            shader.SetTexture(kernel, "_WorldspaceNormalMap", normal);
            shader.SetTexture(kernel, "_WorldspaceMaskMap", white);
            shader.SetTexture(kernel, "_Road_BaseColorMap", road);
            shader.SetTexture(kernel, "_Road_NormalMap", normal);
            shader.SetTexture(kernel, "_Road_MaskMap", white);
            shader.SetBuffer(kernel, "RoadBands", bands);
            shader.SetInt("OriginalRoad", prototypePatch ? 0 : 1); shader.SetInt("ReplaceRoad", replace ? 1 : 0);
            shader.SetInt("RoadBandCount", 1); shader.SetInt("RoadBandRows", 0);
            shader.SetVector("RoadU", new Vector4(1, 0, 0, 0));
            shader.SetVector("RoadV", new Vector4(0, 0, -1, 0));
            shader.SetVector("PatchU", new Vector4(prototypePatch ? 1 : 1f / metres, 0, 0, 0));
            shader.SetVector("PatchV", new Vector4(0, 0, -1, 0));
            shader.SetVector("PatchArea", prototypePatch ? new Vector4(.25f,0,.5f,1) : new Vector4(0, 0, 1, 1));
            shader.SetVector("ChartOrigin", Vector4.zero);
            shader.SetVector("ChartU", Vector3.right); shader.SetVector("ChartV", Vector3.back);
            shader.SetVector("AtlasPixels", new Vector4(16, 0, 16, 0));
            shader.SetVector("AtlasRect", new Vector4(0, 0, width, height));
            shader.SetMatrix("Frame", Matrix4x4.identity); shader.SetFloat("WorldScale", 1);
            shader.SetVector("BaseColor", Color.white); shader.SetVector("NativeValues", Vector4.one);
            shader.Dispatch(kernel, width / 8, 1, 1);
            var previous = RenderTexture.active;
            try { RenderTexture.active = outputs[0]; read.ReadPixels(new Rect(0, 0, width, height), 0, 0); read.Apply(); }
            finally { RenderTexture.active = previous; }
            for (int x = 0; x < width; x++)
            {
                var expected = prototypePatch ? (x % 16 < 8 ? Color.blue : Color.white) :
                    x < width / 2 ? Color.red : !replace ? Color.green : x % 16 < 8 ? Color.blue : Color.white;
                var actual = read.GetPixel(x, 0);
                if (Mathf.Abs(actual.r - expected.r) > .01f || Mathf.Abs(actual.g - expected.g) > .01f || Mathf.Abs(actual.b - expected.b) > .01f)
                    throw new Exception($"Surface sample mismatch: metres={metres}, replace={replace}, x={x}, expected={expected}, actual={actual}");
            }
        }
        finally
        {
            foreach (var output in outputs) { output.Release(); UnityEngine.Object.DestroyImmediate(output); }
            foreach (var texture in new[] { red, green, mask, road, normal, black, white, prototype, read }) UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
