using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BridgeBuilder.Bridges;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

// Offline only. One source semantic unit, independent of all receiving triangles,
// roads, LODs and sampler choices. Each output level samples its authored source mip.
public static class PrepareRepeatUnits
{
    public const string Recipe="BridgeBuilder/RepeatUnits/3";
    [Serializable] public sealed class Channel { public string name,cid,sha256,path,encoding; }
    [Serializable] public sealed class Evidence { public string path,sha256; }
    [Serializable] public sealed class Band { public double start,end,repeatY=1;public double[] min,max,normalTransform;public bool flipU,flipV; }
    [Serializable] public sealed class Panel { public double[] target,source,normalTransform; internal Panel mappingSource; internal bool textureDomain; }
    [Serializable] public sealed class GuardPanel { public int adjacentPanel; public double[] target,source; }
    [Serializable] public sealed class GuardPolygon { public int adjacentPanel; public double[] target,source; }
    [Serializable] public sealed class PaintMask { public int[] sourceTexelRectHalfOpen; public double[] sourceUvRect; public int donorSourceTexelX; public double donorSourceUvX; }
    [Serializable] public sealed class PaintMip { public int sourceMip,width,height; public PaintMask[] masks; }
    [Serializable] public sealed class Unit
    {
        public string id,sourceCid,sourceDigest;
        public double[] min,max,physicalSize;
        public Evidence evidence;
        public Evidence descriptor;
        public Evidence paintEvidence;
        public PaintMip[] paintMipRules;
        public Channel[] channels;
        public string repeatAxes;
        public string twoPeriodAxis;
        public bool finiteSourceGrid;
        public bool sourceGradientSampling;
        public bool fullDirectionalDensity;
        public int finiteGridMip;
        // Measured source packed frame -> actual target mesh frame, row-major 3x3.
        // Never infer this matrix from UV mirroring alone.
        public double[] normalTransform;
        internal bool RepeatU=>string.IsNullOrEmpty(repeatAxes)||repeatAxes.Contains("U");
        internal bool RepeatV=>string.IsNullOrEmpty(repeatAxes)||repeatAxes.Contains("V");
        // An authored composite semantic unit, never receiver-derived strips.
        public Band[] bands;
        public Panel[] panels;
        public GuardPanel[] guardPanels;
        public GuardPolygon[] guardPolygons;
        public bool allowClampedSourceGuards;
        public double[] panelContentMin,panelContentMax;
        internal bool TextureDomain=>panelContentMin!=null;
        // Aliases are use metadata only and are never included in a pixel key.
        public string[] aliases;
    }
    [Serializable] public sealed class Manifest { public string recipe,style;public Unit[] units; }
    [Serializable] public sealed class Product
    {
        public string name,cid,key;public int width,height,mips;public long bytes;
        public double sourceSpanX,sourceSpanY,densityRatioX,densityRatioY;
        public double[] contentMin,contentMax;
        public double[] finiteSamplingMin,finiteSamplingMax;
        public string[] axisSampling;
        public double[] sourceLodByMip;
        public double[] sourcePanelJacobianByMip,sourcePanelMaxSingularByMip;
        public string sourceSampling,sourceWrap,sourceFilter;
        public int sourceAnisotropy;
        public double[] sourceTexelsPerOutputX,sourceTexelsPerOutputY;
        public int sourceLodBandCount,sourcePanelCount,sourceGuardPanelCount,sourceGuardPolygonCount;
        public int[] mipWidths,mipHeights;
        public double[] sourceOriginPixels,sourceRegionPixels;
    }
    [Serializable] public sealed class Result { public string id,key,surfaceCid;public int[] periods;public string[] aliases;public Product[] channels; }
    [Serializable] public sealed class Report { public string recipe,style;public int producedTextures,reusedTextures;public Result[] units; }
    static readonly string[] Names={"_BaseColorMap","_NormalMap","_MaskMap","_ControlMask","_EmissiveColorMap"};
    static string Hash(Action<BinaryWriter> write)
    {using var s=new MemoryStream();using(var w=new BinaryWriter(s,Encoding.UTF8,true))write(w);return PreparePaint.Digest(s.ToArray());}
    static string Cid(byte[] bytes)
    {string digest=PreparePaint.Digest(bytes);return PreparePaint.HashText(Enumerable.Range(0,16).Select(i=>Convert.ToByte(digest.Substring(i*2,2),16)).ToArray(),0);}
    static void Write(string path,byte[] bytes,string cid)
    {
        if(File.Exists(path))
        {if(!File.ReadAllBytes(path).SequenceEqual(bytes)||File.ReadAllText(path+".cid").Trim()!=cid)throw new IOException("Conflicting unit output: "+path);return;}
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,bytes);File.WriteAllText(path+".cid",cid,new UTF8Encoding(false));
    }
    static string Resolve(string root,string path)=>Path.GetFullPath(Path.IsPathRooted(path)?path:Path.Combine(root,path));
    static byte[] Verified(string root,string path,string digest)
    {
        var bytes=File.ReadAllBytes(Resolve(root,path));
        if(PreparePaint.Digest(bytes)!=digest)throw new InvalidDataException("Source/evidence hash differs: "+path);
        return bytes;
    }
    static void ValidatePaint(PaintMip[] rules,int width,int height,int mips)
    {
        if(rules==null)return;
        if(rules.Length!=mips)throw new InvalidDataException("Paint rules must cover the complete original source mip chain.");
        for(int i=0;i<rules.Length;i++)
        {
            var r=rules[i];int w=Math.Max(1,width>>i),h=Math.Max(1,height>>i);
            if(r.sourceMip!=i||r.width!=w||r.height!=h||r.masks==null)
                throw new InvalidDataException("Paint rule source mip dimensions differ.");
            foreach(var mask in r.masks)
            {
                var t=mask.sourceTexelRectHalfOpen;var uv=mask.sourceUvRect;
                if(t?.Length!=4||uv?.Length!=4||t[0]<0||t[1]<0||t[2]>w||t[3]>h||t[0]>=t[2]||t[1]>=t[3]||
                    mask.donorSourceTexelX<0||mask.donorSourceTexelX>=w||mask.donorSourceUvX!=(mask.donorSourceTexelX+.5)/w||
                    uv[0]!=(double)t[0]/w||uv[1]!=(double)t[1]/h||uv[2]!=(double)t[2]/w||uv[3]!=(double)t[3]/h)
                    throw new InvalidDataException("Paint UV mask/donor must match the evidenced original integer-mip texels.");
            }
        }
    }

    static void Validate(Unit u,string root)
    {
        if(u.min?.Length!=2||u.max?.Length!=2||u.physicalSize?.Length!=2||u.channels==null||u.evidence==null||u.descriptor==null)
            throw new InvalidDataException("Incomplete genuine unit measurement: "+u.id);
        for(int i=0;i<2;i++)
            if(double.IsNaN(u.min[i])||double.IsInfinity(u.min[i])||double.IsNaN(u.max[i])||double.IsInfinity(u.max[i])||
               u.min[i]<0||u.max[i]>1||u.max[i]<=u.min[i]||double.IsNaN(u.physicalSize[i])||double.IsInfinity(u.physicalSize[i])||u.physicalSize[i]<=0)
                throw new InvalidDataException("Invalid genuine unit extent: "+u.id);
        Verified(root,u.evidence.path,u.evidence.sha256);
        if(u.paintMipRules!=null)
        {
            if(u.paintEvidence==null||u.paintMipRules.Length==0||u.sourceGradientSampling)
                throw new InvalidDataException("Masked paint requires evidenced integer source-mip rules and scalar source LOD; SampleGrad equivalence is not established.");
            Verified(root,u.paintEvidence.path,u.paintEvidence.sha256);
        }
        if(u.channels.Select(c=>c.name).Distinct().Count()!=u.channels.Length||u.channels.Any(c=>!Names.Contains(c.name)))
            throw new InvalidDataException("Unsupported or duplicate channel.");
        foreach(string name in Names.Take(3))if(!u.channels.Any(c=>c.name==name))throw new InvalidDataException("Missing unit channel: "+name);
        if(u.channels.Any(c=>!string.IsNullOrEmpty(c.encoding)&&c.encoding!="BC7"))
            throw new InvalidDataException("Unsupported native unit encoding.");
        if(!string.IsNullOrEmpty(u.repeatAxes)&&u.repeatAxes!="UV"&&u.repeatAxes!="U"&&u.repeatAxes!="V"&&u.repeatAxes!="None")
            throw new InvalidDataException("Invalid measured repeat axes.");
        if(u.finiteSourceGrid&&u.bands?.Length>0)throw new InvalidDataException("Source-grid locking requires one measured affine source region; split existing physical domains first.");
        if(u.finiteGridMip<0||u.finiteGridMip>13||(!u.finiteSourceGrid&&u.finiteGridMip!=0))throw new InvalidDataException("Invalid finite source-grid mip alignment.");
        Matrix(u.normalTransform);
        if(u.panels?.Length>0)
        {
            if(u.finiteSourceGrid||u.bands?.Length>0)throw new InvalidDataException("Piecewise units cannot combine affine source-grid locking or bands.");
            foreach(var panel in u.panels)
            {
                Matrix(panel.normalTransform);
                if(panel.target?.Length!=6||panel.source?.Length!=6||panel.target.Concat(panel.source).Any(v=>!double.IsFinite(v)||v<0||v>1))
                    throw new InvalidDataException("Invalid offline panel coordinates.");
                PanelMap(panel);
            }
        }
        if(u.guardPanels?.Length>0||u.guardPolygons?.Length>0)
        {
            if(u.panels==null||u.panels.Length==0)throw new InvalidDataException("Guard polygons require authored content panels.");
            SamplingPanels(u);
        }
        if(u.TextureDomain||u.panelContentMax!=null)
        {
            if(u.panels?.Length<1||u.panels==null||u.panelContentMin?.Length!=2||u.panelContentMax?.Length!=2||u.finiteSourceGrid)
                throw new InvalidDataException("Explicit panel texture domain requires two content bounds and piecewise panels.");
            for(int axis=0;axis<2;axis++)
            {
                double lo=u.panelContentMin[axis],hi=u.panelContentMax[axis];
                if(!double.IsFinite(lo)||!double.IsFinite(hi)||lo<0||hi>1||lo>=hi)
                    throw new InvalidDataException("Invalid explicit panel content window.");
                foreach(var panel in u.panels)for(int v=0;v<3;v++)
                    if(panel.target[v*2+axis]<lo||panel.target[v*2+axis]>hi)
                        throw new InvalidDataException("Content panel lies outside its explicit content window.");
            }
        }
        if(string.IsNullOrEmpty(u.sourceCid)||string.IsNullOrEmpty(u.sourceDigest))throw new InvalidDataException("Missing source provenance.");
        if(u.bands!=null&&u.bands.Length!=0)
        {
            double previous=0;
            foreach(var band in u.bands)
            {
                Matrix(band.normalTransform);
                if(!u.RepeatV&&band.repeatY!=1)throw new InvalidDataException("A finite band axis cannot introduce extra repetitions.");
                if((band.flipU||band.flipV)&&band.normalTransform==null)
                    throw new InvalidDataException("Mirrored bands require the measured source-to-target normal frame, including explicit identity when appropriate.");
                if(band.start!=previous||!double.IsFinite(band.end)||band.end<=band.start||band.end>1||
                   !double.IsFinite(band.repeatY)||band.repeatY<1||band.repeatY!=Math.Floor(band.repeatY)||
                   band.min?.Length!=2||band.max?.Length!=2)
                    throw new InvalidDataException("Invalid authored composite unit bands.");
                for(int i=0;i<2;i++)if(!double.IsFinite(band.min[i])||!double.IsFinite(band.max[i])||
                    band.min[i]<0||band.max[i]>1||band.min[i]>=band.max[i])throw new InvalidDataException("Invalid composite source region.");
                previous=band.end;
            }
            if(previous!=1)throw new InvalidDataException("Composite bands must cover one complete semantic unit.");
        }
    }
    static Panel Guard(Unit u,GuardPanel guard)=>GuardShape(u,guard.adjacentPanel,guard.target,guard.source).Single();
    static Panel[] GuardShape(Unit u,int adjacentPanel,double[] target,double[] source)
    {
        if(adjacentPanel<0||adjacentPanel>=u.panels.Length||target==null||target.Length<6||target.Length%2!=0||source?.Length!=target.Length||
            target.Any(v=>!double.IsFinite(v)||v<0||v>1)||source.Any(v=>!double.IsFinite(v)||(!u.allowClampedSourceGuards&&(v<0||v>1))))
            throw new InvalidDataException("Invalid explicit finite guard polygon.");
        var parent=u.panels[adjacentPanel];var t=target;int count=t.Length/2;
        var shared=Enumerable.Range(0,count).Where(i=>Enumerable.Range(0,3).Any(j=>t[2*i]==parent.target[2*j]&&t[2*i+1]==parent.target[2*j+1])).ToArray();
        if(shared.Length!=2)throw new InvalidDataException("Guard polygon must share a measured complete outer panel edge.");
        int a=shared[0],b=shared[1];
        if((a+1)%count!=b&&(b+1)%count!=a)throw new InvalidDataException("Shared guard edge must be an actual polygon edge.");
        double Edge(int i,int j,double x,double y)=>(t[2*j]-t[2*i])*(y-t[2*i+1])-(t[2*j+1]-t[2*i+1])*(x-t[2*i]);
        double outside=Edge(a,b,t[2*Enumerable.Range(0,count).First(i=>i!=a&&i!=b)],t[2*Enumerable.Range(0,count).First(i=>i!=a&&i!=b)+1]);
        if(outside==0)throw new InvalidDataException("Finite guard polygon is degenerate.");
        for(int i=0;i<count;i++)if(i!=a&&i!=b)
            if(Edge(a,b,t[2*i],t[2*i+1])*outside<=0||
                !((!u.RepeatU&&(t[2*i]==0||t[2*i]==1))||(!u.RepeatV&&(t[2*i+1]==0||t[2*i+1]==1))))
                throw new InvalidDataException("Guard polygon must extend only outside the finite boundary.");
        foreach(var panel in u.panels)for(int j=0;j<3;j++)
            if(Edge(a,b,panel.target[2*j],panel.target[2*j+1])*outside>0)
                throw new InvalidDataException("Guard edge is not exterior; internal gaps cannot be filled.");
        double orientation=0;
        for(int i=0;i<count;i++)
        {
            double turn=Edge(i,(i+1)%count,t[2*((i+2)%count)],t[2*((i+2)%count)+1]);
            if(turn==0)throw new InvalidDataException("Guard polygon has a redundant or degenerate corner.");
            if(orientation==0)orientation=turn;
            if(turn*orientation<=0)throw new InvalidDataException("Guard polygon must be convex.");
            for(int j=0;j<count;j++)if(Edge(i,(i+1)%count,t[j*2],t[j*2+1])*orientation<0)
                throw new InvalidDataException("Guard polygon is not a simple convex exterior region.");
        }
        var m=PanelMap(parent);
        for(int i=0;i<count;i++)
            if(Math.Abs(source[2*i]-(m.OX+m.UX*t[2*i]+m.VX*t[2*i+1]))>1e-12||
               Math.Abs(source[2*i+1]-(m.OY+m.UY*t[2*i]+m.VY*t[2*i+1]))>1e-12)
                throw new InvalidDataException("Guard source differs from its adjacent affine projection.");
        var result=new List<Panel>();
        for(int i=1;i+1<count;i++)
        {
            var ids=new[]{0,i,i+1};
            result.Add(new Panel{target=ids.SelectMany(j=>new[]{t[j*2],t[j*2+1]}).ToArray(),
                source=ids.SelectMany(j=>new[]{source[j*2],source[j*2+1]}).ToArray(),normalTransform=parent.normalTransform,mappingSource=parent});
        }
        return result.ToArray();
    }
    static Panel[] SamplingPanels(Unit u)
    {
        if(u.panels==null)return null;
        var result=new List<Panel>(u.panels);
        foreach(var g in u.guardPanels??Array.Empty<GuardPanel>())result.Add(Guard(u,g));
        foreach(var g in u.guardPolygons??Array.Empty<GuardPolygon>())result.AddRange(GuardShape(u,g.adjacentPanel,g.target,g.source));
        if(u.TextureDomain)result=result.Select(p=>new Panel{target=p.target,source=p.source,normalTransform=p.normalTransform,
            mappingSource=p.mappingSource,textureDomain=true}).ToList();
        return result.ToArray();
    }
    static (TextureWrapMode Wrap,FilterMode Filter,int Aniso) SourceSampler(byte[] bytes)
    {
        using var r=new BinaryReader(new MemoryStream(bytes));int version=r.ReadUInt16();
        if(version<1||version>5)throw new InvalidDataException("Unsupported source sampler header version.");
        r.ReadUInt16();r.ReadUInt16();if(version>=4)r.ReadUInt16();r.ReadByte();
        if(version>=3)r.ReadByte();else r.ReadString();if(version>=4)r.ReadByte();
        var filter=version>=2?(FilterMode)r.ReadByte():FilterMode.Trilinear;
        var wrap=version>=2?(TextureWrapMode)r.ReadByte():TextureWrapMode.Repeat;
        return(wrap,filter,version>=2?r.ReadByte():16);
    }
    static void VerifyGuardSampler(Unit u,TextureWrapMode wrap)
    {
        if((SamplingPanels(u)??Array.Empty<Panel>()).Any(p=>p.source.Any(v=>v<0||v>1))&&
            (!u.allowClampedSourceGuards||wrap!=TextureWrapMode.Clamp))
            throw new InvalidDataException("Out-of-atlas finite guards require evidenced original native Clamp sampling.");
    }
    static void Matrix(double[] m)
    {
        if(m==null)return;
        if(m.Length!=9||m.Any(v=>!double.IsFinite(v)))throw new InvalidDataException("Invalid measured normal frame.");
        for(int row=0;row<3;row++)for(int other=0;other<3;other++)
        {
            double dot=0;for(int c=0;c<3;c++)dot+=m[row*3+c]*m[other*3+c];
            if(Math.Abs(dot-(row==other?1:0))>1e-5)throw new InvalidDataException("Normal frame must preserve lengths and angles.");
        }
    }
    static (double OX,double OY,double UX,double UY,double VX,double VY) PanelMap(Panel panel)
    {
        if(panel.mappingSource!=null)return PanelMap(panel.mappingSource);
        var t=panel.target;var s=panel.source;
        double ax=t[2]-t[0],ay=t[3]-t[1],bx=t[4]-t[0],by=t[5]-t[1],det=ax*by-ay*bx;
        if(det==0)throw new InvalidDataException("Degenerate offline panel.");
        double ux=((s[2]-s[0])*by-(s[4]-s[0])*ay)/det,uy=((s[3]-s[1])*by-(s[5]-s[1])*ay)/det;
        double vx=(ax*(s[4]-s[0])-bx*(s[2]-s[0]))/det,vy=(ax*(s[5]-s[1])-bx*(s[3]-s[1]))/det;
        return(s[0]-ux*t[0]-vx*t[1],s[1]-uy*t[0]-vy*t[1],ux,uy,vx,vy);
    }
    static (double X,double Y) Span(Unit u,int width,int height)
    {
        if(u.panels?.Length>0)
        {
            var maps=u.panels.Select(PanelMap).ToArray();
            return(maps.Max(m=>Math.Sqrt(m.UX*m.UX*width*width+m.UY*m.UY*height*height)),
                maps.Max(m=>Math.Sqrt(m.VX*m.VX*width*width+m.VY*m.VY*height*height)));
        }
        return u.bands==null||u.bands.Length==0?((u.max[0]-u.min[0])*width,(u.max[1]-u.min[1])*height)
            :(u.bands.Max(b=>(b.max[0]-b.min[0])*width/(b.end-b.start)),u.bands.Max(b=>(b.max[1]-b.min[1])*height*b.repeatY));
    }
    static (int,int) StoragePeriods(Unit unit)
    {
        int px=unit.twoPeriodAxis=="U"?2:1,py=unit.twoPeriodAxis=="V"?2:1;
        if(!string.IsNullOrEmpty(unit.twoPeriodAxis) &&
            ((unit.twoPeriodAxis!="U"&&unit.twoPeriodAxis!="V") ||
             (px==2&&!unit.RepeatU)||(py==2&&!unit.RepeatV)||
             unit.bands?.Length>0||unit.panels?.Length>0))
            throw new InvalidDataException("Bounded two-period storage requires one evidenced rectangular repeat axis without bands or panels.");
        return(px,py);
    }
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
        string path=Arg("-unitManifest"),root=Path.GetDirectoryName(path),output=Arg("-unitOutput"),style=Arg("-style");
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
        if(manifest.recipe!=Recipe||manifest.style!=style||manifest.units==null||manifest.units.Length==0||Path.GetFileName(style)!=style)
            throw new InvalidDataException("Invalid unit manifest/style.");
        if(QualitySettings.activeColorSpace!=ColorSpace.Linear)throw new InvalidOperationException("Use Linear authoring color space.");
        var program=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/RepeatUnit.compute");
        var result=new Report{recipe=Recipe,style=style};var units=new List<Result>();
        var products=new Dictionary<string,(byte[] Bytes,Product Product)>();
        var module=PreparePaint.OpenCodec(Arg("-gameData"));
        try
        {
            foreach(var unit in manifest.units)
            {
                Validate(unit,root);var bindings=new Dictionary<string,string>();var channels=new List<Product>();
                var (px,py)=StoragePeriods(unit);
                var inputs=unit.channels.ToDictionary(c=>c.name,c=>Verified(root,c.path,c.sha256));
                // One semantic unit, even when its measured edges fall between
                // source texels. Ceil chooses a sampling grid no coarser than the
                // source; it never changes the region or its physical period.
                // Requiring rational-grid phase equality caused huge repeated
                // images and was not a requirement of native Repeat sampling.
                foreach(var channel in unit.channels.OrderBy(c=>c.name,StringComparer.Ordinal))
                {
                    var sourceBytes=inputs[channel.name];
                    var sampler=SourceSampler(sourceBytes);VerifyGuardSampler(unit,sampler.Wrap);
                    var paint=channel.name=="_ControlMask"?null:unit.paintMipRules;
                    ValidatePaint(paint,BitConverter.ToUInt16(sourceBytes,2),BitConverter.ToUInt16(sourceBytes,4),sourceBytes[8]);
                    if(paint!=null&&sampler.Filter!=FilterMode.Trilinear)throw new InvalidDataException("Integer-mip paint blending requires the evidenced native Trilinear sampler.");
                    string key=Hash(w=>{w.Write(Recipe);w.Write(Arg("-unitPipeline"));w.Write(channel.sha256);
                        if(unit.bands==null||unit.bands.Length==0)
                        {w.Write(0);for(int i=0;i<2;i++){w.Write(unit.min[i]);w.Write(unit.max[i]);}}
                        else
                        {w.Write(unit.bands.Length);foreach(var b in unit.bands){w.Write(b.start);w.Write(b.end);w.Write(b.repeatY);for(int i=0;i<2;i++){w.Write(b.min[i]);w.Write(b.max[i]);}}}
                        if(px!=1||py!=1)w.Write("BoundedTwoPeriodStorage/1");w.Write(px);w.Write(py);w.Write(channel.encoding??"");w.Write(unit.RepeatU);w.Write(unit.RepeatV);
                        w.Write(unit.finiteSourceGrid);
                        w.Write(unit.finiteGridMip);
                        w.Write(unit.sourceGradientSampling);
                        if(unit.fullDirectionalDensity)w.Write("FullDirectionalPanelDensity/1");
                        if(paint!=null)
                        {
                            w.Write("MaskedIntegerMipUvDonor/1");w.Write(unit.paintEvidence.sha256);
                            foreach(var mip in paint){w.Write(mip.sourceMip);w.Write(mip.width);w.Write(mip.height);w.Write(mip.masks.Length);
                                foreach(var mask in mip.masks){foreach(var v in mask.sourceUvRect)w.Write(v);w.Write(mask.donorSourceUvX);}}
                        }
                        w.Write(channel.name=="_NormalMap");
                        void Frame(double[] matrix){w.Write(matrix?.Length??0);if(matrix!=null)foreach(var value in matrix)w.Write(value);}
                        Frame(unit.normalTransform);
                        foreach(var band in unit.bands??Array.Empty<Band>()){w.Write(band.flipU);w.Write(band.flipV);Frame(band.normalTransform);}
                        if(unit.TextureDomain){w.Write("ExplicitPanelTextureDomain/1");foreach(double v in unit.panelContentMin)w.Write(v);foreach(double v in unit.panelContentMax)w.Write(v);}
                        w.Write(SamplingPanels(unit)?.Length??0);
                        foreach(var panel in SamplingPanels(unit)??Array.Empty<Panel>()){foreach(double v in panel.target)w.Write(v);foreach(double v in panel.source)w.Write(v);Frame(panel.normalTransform);}
                    });
                    if(!products.TryGetValue(key,out var entry))
                    {
                        string stored=Path.Combine(Arg("-unitStore"),key+".Texture");
                        if(File.Exists(stored))
                        {
                            var bytes=File.ReadAllBytes(stored);string cid=Cid(bytes);
                            if(File.ReadAllText(stored+".cid").Trim()!=cid)throw new InvalidDataException("Prepared unit product checksum differs.");
                            var p=new Product{name=channel.name,cid=cid,key=key,width=BitConverter.ToUInt16(bytes,2),height=BitConverter.ToUInt16(bytes,4),mips=bytes[8],bytes=bytes.Length};
                            entry=(bytes,p);products.Add(key,entry);result.reusedTextures++;
                        }
                        else
                        {
                        using var stream=new MemoryStream(sourceBytes);
                        var source=MovableTextureSource.Read(stream,b=>PreparePaint.DecodeRaw(b,NativeRawSize(sourceBytes)));
                        try
                        {
                            int complete=1+(int)Math.Floor(Math.Log(Math.Max(source.width,source.height),2));
                            if(source.mipmapCount!=complete)throw new InvalidDataException("Source must contain its complete authored mip chain: "+channel.path);
                            bool bc7=channel.encoding=="BC7";
                            var grid=Grid(unit,source.width,source.height,bc7);
                            int width=checked(grid.Width*px),height=checked(grid.Height*py);
                            if(width>SystemInfo.maxTextureSize||height>SystemInfo.maxTextureSize)throw new InvalidDataException("Guarded unit exceeds native dimensions.");
                            var texture=ExtractCore(program,source,grid.X,grid.Y,grid.SX,grid.SY,width,height,px,py,unit.bands,
                                unit.RepeatU,unit.RepeatV,unit.finiteSourceGrid,channel.name=="_NormalMap",unit.normalTransform,unit.sourceGradientSampling,SamplingPanels(unit),paint);
                            try
                            {
                                Texture2D encoded=null;
                                byte[] bytes;
                                try{if(bc7)encoded=NativeTextureEncoding.Bc7(texture);bytes=PreparePaint.Texture(encoded??texture);}
                                finally{if(encoded!=null)UnityEngine.Object.DestroyImmediate(encoded);}
                                string cid=Cid(bytes);
                                var p=new Product{name=channel.name,cid=cid,key=key,width=width,height=height,mips=texture.mipmapCount,bytes=bytes.Length};
                                entry=(bytes,p);products.Add(key,entry);Write(stored,bytes,cid);result.producedTextures++;
                            }
                            finally{UnityEngine.Object.DestroyImmediate(texture);}
                        }
                        finally{UnityEngine.Object.DestroyImmediate(source);}
                        }
                    }
                    else result.reusedTextures++;
                    Write(Path.Combine(output,style,"Shared",entry.Product.cid+".Texture"),entry.Bytes,entry.Product.cid);
                    bindings.Add(channel.name,entry.Product.cid);
                    var product=entry.Product;
                    int sourceWidth=BitConverter.ToUInt16(sourceBytes,2),sourceHeight=BitConverter.ToUInt16(sourceBytes,4);
                    var sourceSpan=Span(unit,sourceWidth,sourceHeight);
                    var mapping=Grid(unit,sourceWidth,sourceHeight,channel.encoding=="BC7");
                    double spanX=sourceSpan.X,spanY=sourceSpan.Y;
                    channels.Add(new Product{name=channel.name,cid=product.cid,key=product.key,width=product.width,height=product.height,mips=product.mips,bytes=product.bytes,
                        sourceSpanX=spanX,sourceSpanY=spanY,densityRatioX=unit.finiteSourceGrid&&!unit.RepeatU?1:product.width/(px*(unit.RepeatU||unit.TextureDomain?spanX:spanX*2)),densityRatioY=unit.finiteSourceGrid&&!unit.RepeatV?1:product.height/(py*(unit.RepeatV||unit.TextureDomain?spanY:spanY*2)),
                        contentMin=mapping.Min,contentMax=mapping.Max,
                        axisSampling=new[]{unit.finiteSourceGrid&&!unit.RepeatU?"source-grid-through-mip-"+unit.finiteGridMip:"unit-resample",unit.finiteSourceGrid&&!unit.RepeatV?"source-grid-through-mip-"+unit.finiteGridMip:"unit-resample"},
                        mipWidths=Enumerable.Range(0,product.mips).Select(mip=>Math.Max(1,product.width>>mip)).ToArray(),
                        mipHeights=Enumerable.Range(0,product.mips).Select(mip=>Math.Max(1,product.height>>mip)).ToArray(),
                        sourceOriginPixels=new[]{mapping.X,mapping.Y},sourceRegionPixels=new[]{mapping.SX,mapping.SY},
                        sourceSampling=paint!=null?"MaskedIntegerMipUvDonor/1; scalar max-axis LOD; original sampler":unit.sourceGradientSampling?"SampleGrad":"SampleLevel-max-axis",
                        sourceWrap=sampler.Wrap.ToString(),sourceFilter=sampler.Filter.ToString(),sourceAnisotropy=sampler.Aniso,
                        sourceTexelsPerOutputX=Enumerable.Range(0,product.mips).SelectMany(m=>unit.panels?.Length>0?unit.panels.Select(p=>
                        {var a=PanelMap(p);return Math.Sqrt(a.UX*a.UX*sourceWidth*sourceWidth+a.UY*a.UY*sourceHeight*sourceHeight)/Math.Max(1,product.width>>m)*(unit.RepeatU||unit.TextureDomain?1:2);}):
                        new[]{px*mapping.SX/Math.Max(1,product.width>>m)*(unit.RepeatU||unit.finiteSourceGrid?1:2)}).ToArray(),
                        sourceTexelsPerOutputY=Enumerable.Range(0,product.mips).SelectMany(m=>unit.panels?.Length>0?unit.panels.Select(p=>
                        {var a=PanelMap(p);return Math.Sqrt(a.VX*a.VX*sourceWidth*sourceWidth+a.VY*a.VY*sourceHeight*sourceHeight)/Math.Max(1,product.height>>m)*(unit.RepeatV||unit.TextureDomain?1:2);}):
                        new[]{py*mapping.SY/Math.Max(1,product.height>>m)*(unit.RepeatV||unit.finiteSourceGrid?1:2)}).ToArray(),
                        finiteSamplingMin=new[]{0d,0d},finiteSamplingMax=new[]{1d,1d},
                        sourcePanelJacobianByMip=Enumerable.Range(0,product.mips).SelectMany(m=>
                            PanelJacobians(unit,sourceWidth,sourceHeight,Math.Max(1,product.width>>m),Math.Max(1,product.height>>m)).SelectMany(j=>j)).ToArray(),
                        sourcePanelMaxSingularByMip=Enumerable.Range(0,product.mips).SelectMany(m=>
                            PanelJacobians(unit,sourceWidth,sourceHeight,Math.Max(1,product.width>>m),Math.Max(1,product.height>>m)).Select(MaxSingular)).ToArray(),
                        sourcePanelCount=unit.panels?.Length??0,
                        sourceGuardPanelCount=unit.guardPanels?.Length??0,sourceGuardPolygonCount=unit.guardPolygons?.Length??0,
                        sourceLodBandCount=Math.Max(1,Math.Max(unit.bands?.Length??0,unit.panels?.Length??0)),
                        sourceLodByMip=Enumerable.Range(0,product.mips).SelectMany(mip=>
                            unit.panels?.Length>0?unit.panels.Select(p=>
                            {
                                var m=PanelMap(p);double x=(unit.RepeatU||unit.TextureDomain?1:2)/((double)Math.Max(1,product.width>>mip));
                                double y=(unit.RepeatV||unit.TextureDomain?1:2)/((double)Math.Max(1,product.height>>mip));
                                return Math.Log(Math.Max(Math.Sqrt(m.UX*m.UX*sourceWidth*sourceWidth+m.UY*m.UY*sourceHeight*sourceHeight)*x,
                                    Math.Sqrt(m.VX*m.VX*sourceWidth*sourceWidth+m.VY*m.VY*sourceHeight*sourceHeight)*y),2);
                            }):unit.bands?.Length>0?unit.bands.Select(b=>Math.Log(Math.Max(
                                (b.max[0]-b.min[0])*sourceWidth/(Math.Max(1,product.width>>mip)*(b.end-b.start)*(unit.RepeatU?1:.5)),
                                (b.max[1]-b.min[1])*sourceHeight*b.repeatY/(Math.Max(1,product.height>>mip)*(unit.RepeatV?1:.5))),2)):
                            new[]{Math.Log(Math.Max(px*mapping.SX/Math.Max(1,product.width>>mip)*(unit.RepeatU||unit.finiteSourceGrid?1:2),
                                py*mapping.SY/Math.Max(1,product.height>>mip)*(unit.RepeatV||unit.finiteSourceGrid?1:2)),2)})
                            .Select(lod=>Math.Max(0,Math.Min(sourceBytes[8]-1,lod))).ToArray()});
                }
                if(channels.Skip(1).Any(c=>!c.contentMin.SequenceEqual(channels[0].contentMin)||!c.contentMax.SequenceEqual(channels[0].contentMax)))
                    throw new InvalidDataException("Aligned PBR channels require the same normalized finite-domain mapping; use a common source grid.");
                if(!bindings.ContainsKey("_ControlMask"))
                {
                    const string cid="729893d593df5d52ba911bc1e7eab77b";
                    var bytes=Verified(Arg("-preparedMaterials"),Path.Combine(style,"Shared",cid+".Texture"),"d002cc9d2fb6777d63a7b16bb306e05dca09bf521552371106c5d8c12c2bcdf4");
                    Write(Path.Combine(output,style,"Shared",cid+".Texture"),bytes,cid);bindings.Add("_ControlMask",cid);
                }
                var descriptor=Surface(Verified(root,unit.descriptor.path,unit.descriptor.sha256),bindings,px,py);string surfaceCid=Cid(descriptor);
                string unitKey=Hash(w=>{w.Write(Recipe);w.Write(surfaceCid);});
                Write(Path.Combine(output,style,"RepeatUnits",unitKey,"Repeat.Surface"),descriptor,surfaceCid);
                units.Add(new Result{id=unit.id,key=unitKey,surfaceCid=surfaceCid,periods=new[]{px,py},aliases=unit.aliases,channels=channels.ToArray()});
            }
            result.units=units.ToArray();File.WriteAllText(Arg("-unitReport"),JsonUtility.ToJson(result,true));
            Debug.Log($"PASS genuine repeat units: {units.Count}; produced textures={result.producedTextures}; shared uses={result.reusedTextures}; full source mips.");
        }
        finally{PreparePaint.CloseCodec(module);}
    }
    static double[][] PanelJacobians(Unit u,int sw,int sh,int w,int h)
    {
        return (u.panels??Array.Empty<Panel>()).Select(p=>
        {
            var m=PanelMap(p);double dx=(u.RepeatU||u.TextureDomain?1d:2d)/w,dy=(u.RepeatV||u.TextureDomain?1d:2d)/h;
            return new[]{m.UX*sw*dx,m.VX*sw*dy,m.UY*sh*dx,m.VY*sh*dy};
        }).ToArray();
    }
    static double MaxSingular(double[] j)
    {
        double a=j[0]*j[0]+j[2]*j[2],b=j[0]*j[1]+j[2]*j[3],d=j[1]*j[1]+j[3]*j[3];
        return Math.Sqrt((a+d+Math.Sqrt((a-d)*(a-d)+4*b*b))*.5);
    }
    internal static (double X,double Y,double SX,double SY,int Width,int Height,double[] Min,double[] Max)
        Grid(Unit u,int sourceWidth,int sourceHeight,bool bc7)
    {
        var span=Span(u,sourceWidth,sourceHeight);
        int width=Pixel(span.X,bc7),height=Pixel(span.Y,bc7);
        if(!u.RepeatU&&!u.TextureDomain)width=checked(width*2);
        if(!u.RepeatV&&!u.TextureDomain)height=checked(height*2);
        if(u.fullDirectionalDensity)
        {
            if(u.panels==null||u.panels.Length==0)throw new InvalidDataException("Full-directional panel density requires measured panels.");
            double scale=PanelJacobians(u,sourceWidth,sourceHeight,width,height).Max(MaxSingular);
            if(scale>1){width=Pixel(width*scale,bc7);height=Pixel(height*scale,bc7);}
        }
        double x=u.min[0]*sourceWidth,y=u.min[1]*sourceHeight;
        double sx=(u.max[0]-u.min[0])*sourceWidth,sy=(u.max[1]-u.min[1])*sourceHeight;
        var min=new[]{u.RepeatU?0d:.25d,u.RepeatV?0d:.25d};
        var max=new[]{u.RepeatU?1d:.75d,u.RepeatV?1d:.75d};
        if(u.TextureDomain){min=(double[])u.panelContentMin.Clone();max=(double[])u.panelContentMax.Clone();}
        if(u.finiteSourceGrid)
        {
            int quantum=1<<u.finiteGridMip;
            void Snap(ref double origin,ref double span,ref int size,int axis)
            {
                double guard=(size-span)/2;
                double start=Math.Floor((origin-guard)/quantum)*quantum;
                int alignment=Math.Max(quantum,bc7?4:1);
                size=checked((int)Math.Ceiling((origin+span+guard-start)/alignment)*alignment);
                min[axis]=(origin-start)/size;max[axis]=(origin+span-start)/size;
                origin=start;span=size;
            }
            if(!u.RepeatU)Snap(ref x,ref sx,ref width,0);
            if(!u.RepeatV)Snap(ref y,ref sy,ref height,1);
        }
        return(x,y,sx,sy,width,height,min,max);
    }
    static int Pixel(double value,bool blockAligned=false)
    {
        int p=checked((int)Math.Ceiling(value));
        if(blockAligned)p=checked((p+3)/4*4);
        if(p<=0||p>SystemInfo.maxTextureSize)throw new InvalidDataException("One genuine unit exceeds native texture dimensions: "+value);
        return p;
    }
    static int NativeRawSize(byte[] bytes)
    {
        using var r=new BinaryReader(new MemoryStream(bytes));int version=r.ReadUInt16(),w=r.ReadUInt16(),h=r.ReadUInt16();
        if(version>=4)r.ReadUInt16();int mips=r.ReadByte();var format=version>=3?(GraphicsFormat)r.ReadByte():(GraphicsFormat)Enum.Parse(typeof(GraphicsFormat),r.ReadString());
        int bw=(int)GraphicsFormatUtility.GetBlockWidth(format),bh=(int)GraphicsFormatUtility.GetBlockHeight(format),b=(int)GraphicsFormatUtility.GetBlockSize(format),size=0;
        for(int i=0;i<mips;i++){size=checked(size+((w+bw-1)/bw)*((h+bh-1)/bh)*b);w=Math.Max(1,w/2);h=Math.Max(1,h/2);}return size;
    }
    static Texture2D Extract(ComputeShader program,Texture2D source,double x,double y,double sx,double sy,int width,int height,int px,int py,Band[] bands,bool repeatU=true,bool repeatV=true,
        bool finiteSourceGrid=false,bool normalChannel=false,double[] normalTransform=null,bool sourceGradientSampling=false,Panel[] panels=null)
    => ExtractCore(program,source,x,y,sx,sy,width,height,px,py,bands,repeatU,repeatV,finiteSourceGrid,normalChannel,normalTransform,sourceGradientSampling,panels,null);

    static Texture2D ExtractCore(ComputeShader program,Texture2D source,double x,double y,double sx,double sy,int width,int height,int px,int py,Band[] bands,bool repeatU,bool repeatV,
        bool finiteSourceGrid,bool normalChannel,double[] normalTransform,bool sourceGradientSampling,Panel[] panels,PaintMip[] paint)
    {
        string format=source.graphicsFormat.ToString();
        if(format.Contains("16")||format.Contains("32")||format.Contains("BC6H"))
            throw new InvalidDataException("This source requires a higher-precision unit output: "+format);
        bool srgb=GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat);
        var output=new Texture2D(width,height,TextureFormat.RGBA32,true,!srgb){wrapMode=repeatU||repeatV?TextureWrapMode.Repeat:TextureWrapMode.Clamp,filterMode=source.filterMode,anisoLevel=source.anisoLevel};
        try
        {
            int kernel=program.FindKernel("Extract");program.SetTexture(kernel,"Source",source);
            ValidatePaint(paint,source.width,source.height,source.mipmapCount);
            if(paint!=null&&source.filterMode!=FilterMode.Trilinear)throw new InvalidDataException("Paint requires original Trilinear source filtering.");
            if(paint!=null&&sourceGradientSampling)throw new InvalidDataException("Paint mip UV gating does not support SampleGrad.");
            var paintData=new List<Vector4>();
            foreach(var mip in paint??Array.Empty<PaintMip>())foreach(var mask in mip.masks)
            {
                var r=mask.sourceUvRect;paintData.Add(new Vector4((float)r[0],(float)r[1],(float)r[2],(float)r[3]));
                paintData.Add(new Vector4(mip.sourceMip,(float)mask.donorSourceUvX,0,0));
            }
            int paintCount=paintData.Count/2;if(paintData.Count==0)paintData.Add(Vector4.zero);
            using var paintBuffer=new ComputeBuffer(paintData.Count,16);paintBuffer.SetData(paintData);
            program.SetBuffer(kernel,"PaintMasks",paintBuffer);program.SetInt("PaintMaskCount",paintCount);
            program.SetInt("SourceMipCount",source.mipmapCount);
            int count=bands?.Length??0;
            var bandData=new Vector4[Math.Max(2,count*2)];
            var bandFrames=new Vector4[Math.Max(3,count*3)];
            var bandOptions=new Vector4[Math.Max(1,count)];
            bool Transform(double[] matrix)=>matrix!=null&&Enumerable.Range(0,9).Any(i=>matrix[i]!=(i%4==0?1d:0d));
            Vector4 Row(double[] matrix,int row)=>matrix==null
                ? new Vector4(row==0?1:0,row==1?1:0,row==2?1:0,0)
                : new Vector4((float)matrix[row*3],(float)matrix[row*3+1],(float)matrix[row*3+2],0);
            for(int i=0;i<count;i++)
            {
                var b=bands[i];bandData[i*2]=new Vector4((float)b.start,(float)b.end,(float)b.repeatY,0);
                bandData[i*2+1]=new Vector4((float)b.min[0],(float)b.min[1],(float)(b.max[0]-b.min[0]),(float)(b.max[1]-b.min[1]));
                for(int row=0;row<3;row++)bandFrames[i*3+row]=Row(b.normalTransform??normalTransform,row);
                double dx=(b.max[0]-b.min[0])*source.width/(width*(b.end-b.start)*(repeatU?1:.5));
                double dy=(b.max[1]-b.min[1])*source.height*b.repeatY/(height*(repeatV?1:.5));
                bandOptions[i]=new Vector4(b.flipU?1:0,b.flipV?1:0,(float)Math.Log(Math.Max(dx,dy),2),Transform(b.normalTransform??normalTransform)?1:0);
            }
            using var bandBuffer=new ComputeBuffer(bandData.Length,16);bandBuffer.SetData(bandData);
            using var frameBuffer=new ComputeBuffer(bandFrames.Length,16);frameBuffer.SetData(bandFrames);
            using var optionBuffer=new ComputeBuffer(bandOptions.Length,16);optionBuffer.SetData(bandOptions);
            program.SetBuffer(kernel,"Bands",bandBuffer);program.SetInt("BandCount",count);
            program.SetBuffer(kernel,"BandFrames",frameBuffer);program.SetBuffer(kernel,"BandOptions",optionBuffer);
            for(int row=0;row<3;row++)program.SetVector("NormalRow"+row,Row(normalTransform,row));
            program.SetInt("TransformNormal",normalChannel&&Transform(normalTransform)?1:0);
            program.SetInt("NormalChannel",normalChannel?1:0);
            program.SetInt("SourceGradientSampling",sourceGradientSampling?1:0);
            int panelCount=panels?.Length??0;
            var panelData=new Vector4[Math.Max(4,panelCount*4)];var panelFrames=new Vector4[Math.Max(3,panelCount*3)];
            for(int i=0;i<panelCount;i++)
            {
                var panel=panels[i];var t=panel.target;var m=PanelMap(panel);
                panelData[i*4]=new Vector4((float)t[0],(float)t[1],(float)t[2],(float)t[3]);
                panelData[i*4+1]=new Vector4((float)t[4],(float)t[5],normalChannel&&Transform(panel.normalTransform??normalTransform)?1:0,0);
                panelData[i*4+2]=new Vector4((float)m.OX,(float)m.OY,(float)m.UX,(float)m.UY);
                panelData[i*4+3]=new Vector4((float)m.VX,(float)m.VY,0,0);
                for(int row=0;row<3;row++)panelFrames[i*3+row]=Row(panel.normalTransform??normalTransform,row);
            }
            using var panelBuffer=new ComputeBuffer(panelData.Length,16);panelBuffer.SetData(panelData);
            using var panelFrameBuffer=new ComputeBuffer(panelFrames.Length,16);panelFrameBuffer.SetData(panelFrames);
            using var missingBuffer=new ComputeBuffer(2,4);
            program.SetInt("PanelCount",panelCount);program.SetBuffer(kernel,"Panels",panelBuffer);
            program.SetBuffer(kernel,"PanelFrames",panelFrameBuffer);program.SetBuffer(kernel,"MissingSamples",missingBuffer);
            program.SetInts("SourceSize",source.width,source.height);
            program.SetVector("Region",new Vector4((float)x/source.width,(float)y/source.height,(float)sx/source.width,(float)sy/source.height));
            program.SetVector("Periods",new Vector4(px,py,0,0));
            bool textureDomain=panels?.Any(p=>p.textureDomain)==true;
            program.SetInts("RepeatAxes",repeatU||finiteSourceGrid||textureDomain?1:0,repeatV||finiteSourceGrid||textureDomain?1:0);
            program.SetInt("EncodeSrgb",srgb?1:0);
            for(int mip=0;mip<output.mipmapCount;mip++)
            {
                int w=Math.Max(1,width>>mip),h=Math.Max(1,height>>mip);
                var target=new RenderTexture(w,h,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){enableRandomWrite=true};
                try
                {
                    double densityX=px*sx/w*(repeatU||finiteSourceGrid||textureDomain?1:2),densityY=py*sy/h*(repeatV||finiteSourceGrid||textureDomain?1:2);
                    target.Create();program.SetTexture(kernel,"Result",target);program.SetInts("Extent",w,h);
                    program.SetFloat("SourceMip",(float)Math.Log(Math.Max(densityX,densityY),2));
                    program.SetFloat("OutputMip",0);
                    for(int i=0;i<count;i++)
                    {
                        var b=bands[i];
                        double dx=(b.max[0]-b.min[0])*source.width/(w*(b.end-b.start)*(repeatU?1:.5));
                        double dy=(b.max[1]-b.min[1])*source.height*b.repeatY/(h*(repeatV?1:.5));
                        bandOptions[i].z=(float)Math.Log(Math.Max(dx,dy),2);
                    }
                    optionBuffer.SetData(bandOptions);
                    missingBuffer.SetData(new uint[]{0,uint.MaxValue});
                    program.Dispatch(kernel,(w+7)/8,(h+7)/8,1);
                    var missing=new uint[2];missingBuffer.GetData(missing);
                    if(missing[0]!=0)throw new InvalidDataException($"Offline panels leave uncovered samples: mip={mip}; size={w}x{h}; count={missing[0]}; first=({missing[1]%(uint)w},{missing[1]/(uint)w}); no fallback source crop is allowed.");
                    var read=AsyncGPUReadback.Request(target,0,TextureFormat.RGBA32);read.WaitForCompletion();
                    if(read.hasError)throw new IOException("Repeat unit mip GPU readback failed.");
                    output.SetPixelData(read.GetData<byte>(),mip);
                }
                finally{target.Release();UnityEngine.Object.DestroyImmediate(target);}
            }
            output.Apply(false,false);return output;
        }
        catch{UnityEngine.Object.DestroyImmediate(output);throw;}
    }
    static byte[] Surface(byte[] original,Dictionary<string,string> bindings,int px,int py)
    {
        using var input=new MemoryStream(original);using var r=new BinaryReader(input);
        if(r.ReadUInt16()!=1)throw new InvalidDataException("Unsupported source descriptor version.");
        int template=r.ReadInt32();bool vt=r.ReadBoolean();
        if(template!=0&&template!=1&&template!=14)throw new InvalidDataException("Provide a native Default/Cutout object descriptor.");
        bool Present(){byte marker=r.ReadByte();if(marker!=0&&marker!=255)throw new InvalidDataException("Invalid descriptor marker.");return marker!=0;}
        int Count(){int count=Present()?r.ReadInt32():0;if(count<0)throw new InvalidDataException("Invalid descriptor count.");return count;}
        if(vt){if(r.ReadBytes(276).Length!=276)throw new EndOfStreamException();if(Present()&&r.ReadBytes(16).Length!=16)throw new EndOfStreamException();}
        int maps=checked((int)input.Position);
        foreach(int size in new[]{4,4,16})
        {
            int count=Count();
            for(int i=0;i<count;i++){r.ReadString();if(r.ReadBytes(size).Length!=size)throw new EndOfStreamException();}
        }
        int colorsStart=checked((int)input.Position);
        var colors=new Dictionary<string,Vector4>();
        for(int count=Count();count>0;count--)colors.Add(r.ReadString(),new Vector4(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle()));
        if(px!=1||py!=1)
        {
            if(!colors.TryGetValue("_UV0Scale",out var scale))scale=new Vector4(1,1,0,0);
            colors.TryGetValue("_UV0Offset",out var offset);
            scale.x/=px;scale.y/=py;offset.x/=px;offset.y/=py;
            colors["_UV0Scale"]=scale;colors["_UV0Offset"]=offset;
        }
        int prefix=checked((int)input.Position);
        int textures=Count();var merged=new Dictionary<string,string>(bindings);
        for(int i=0;i<textures;i++)
        {
            string name=r.ReadString();if(!Present())continue;
            var cid=r.ReadBytes(16);if(cid.Length!=16)throw new EndOfStreamException();
            if(name=="_ControlMask"&&merged.TryGetValue(name,out var target)&&target=="729893d593df5d52ba911bc1e7eab77b"&&PreparePaint.HashText(cid,0)!=target)
                throw new InvalidDataException("An authored control map must be provided as a unit channel; transparent control cannot replace it.");
            if(!merged.ContainsKey(name))
            {
                if(vt&&Names.Contains(name))throw new InvalidDataException("Provide the authored VT channel or verified constant channel: "+name);
                merged.Add(name,PreparePaint.HashText(cid,0));
            }
        }
        int suffix=checked((int)input.Position);
        using var s=new MemoryStream();using var w=new BinaryWriter(s);
        w.Write((ushort)1);w.Write(template);w.Write(false);w.Write(original,maps,colorsStart-maps);
        w.Write((byte)255);w.Write(colors.Count);foreach(var p in colors){w.Write(p.Key);w.Write(p.Value.x);w.Write(p.Value.y);w.Write(p.Value.z);w.Write(p.Value.w);}
        w.Write((byte)255);w.Write(merged.Count);
        foreach(var p in merged.OrderBy(p=>p.Key,StringComparer.Ordinal)){w.Write(p.Key);w.Write((byte)255);w.Write(PreparePaint.HashBytes(p.Value));}
        w.Write(original,suffix,original.Length-suffix);return s.ToArray();
    }
}
