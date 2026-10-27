using BridgeBuilder.Bridges;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;
using Vertex = BridgeBuilder.Bridges.MovableRoadGeometry.Vertex;
using Triangle = BridgeBuilder.Bridges.MovableRoadGeometry.Triangle;

internal static class DomainCheck
{
    internal static string Run(HashSet<string> keys, string digest)
    {
        int faces = 0, points = 0, numericalSlivers = 0, edges = 0; double worst = 0, edgeWorst = 0;
        var counts = new List<int>();
        foreach (var centers in new[] { new[] {-1.5f,1.5f}, new[] {-4f,-.5f,3.1f}, new[] {-7.5f,-4.5f,-1.5f,5.5f,8.5f,11.5f} })
        foreach (float halfWidth in new[] { 10f, 20f, 40f })
        foreach (bool reflected in new[] { false, true })
        {
            var frame = Matrix4x4.identity;
            // A reflected/translated source frame exercises the seam's complete
            // affine equation without requiring a native Unity rotation call.
            frame.m00 = reflected ? -1f : 1f;
            frame.m30 = reflected ? 2f : 0f;
            float X(float x) => reflected ? 2f - x : x;
            var source = Enumerable.Range(0,4).Select(_ => new Patch(Vector2.zero,Vector2.one,
                new Vector4(0,0,1,0),new Vector4(1,0,0,0),repeat:true)).ToArray();
            Draw03RoadWear.Apply(source,centers,frame,new Vector2(-halfWidth,halfWidth));
            var original = source[0];
            var measured = Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32;
            float laneBottom = measured[3]/measured[4], laneTop = measured[7]/measured[8];
            float gapHeight = measured[9]/measured[10], gapBottom = laneBottom-gapHeight*measured[11];
            var main = new Partition(0); var walk = new Partition(2);
            var unchangedWalk = walk;
            var patches = Draw03RoadWearDomains.Apply(source,ref main,ref walk);
            if(!ReferenceEquals(walk,unchangedWalk))throw new Exception("Sidewalk partition changed.");
            if(patches.Length != source.Length+original.Bands!.Length)throw new Exception("Unexpected material domain count.");
            for(int i=source.Length;i<patches.Length;i++)
            {
                foreach(bool cutout in new[] {false,true})
                    if(!keys.Contains(MovableRepeatKey.For(digest,patches[i].Min,patches[i].Max,cutout,null,false)))
                        throw new Exception("Domain has no prepared unit.");
                var band=original.Bands[i-source.Length];var patch=patches[i];
                bool gap=band.Min.y==gapBottom;
                foreach(float q in new[]{-1e-6f,0f,1e-6f,.5f,1f-1e-6f,1f,1f+1e-6f})
                {
                    // Evaluate each side's own edge phase. Outside probes also
                    // verify continuous outer-gap sampling through roundoff: the
                    // full road layout must not restart at v=0 or v=1.
                    float v=band.Start+q*(band.End-band.Start);
                    float selectedX=(v-original.V.w)/original.V.x;
                    double local=((double)original.V.x*selectedX+original.V.w-band.Start)/(band.End-band.Start);
                    double expected=gap?local*band.RepeatY:
                        ((band.Min.y-laneBottom)+local*(band.Max.y-band.Min.y))/(laneTop-laneBottom);
                    double actual=(double)patch.V.x*selectedX+patch.V.w;
                    double phaseError=Math.Abs(expected-actual);
                    phaseError=Math.Abs(phaseError-Math.Round(phaseError));
                    edgeWorst=Math.Max(edgeWorst,phaseError*(patch.Max.y-patch.Min.y)*4096);edges++;
                }
            }
            Vertex V(float objectX,float z)
            {
                float x=X(objectX),y=.1f*x+.05f*z;
                return new Vertex(new Vector3(x,y,z),new Vector3(-.1f,1,-.05f).normalized,
                    new Vector4(0,0,1,1),new Vector4(.03f*x+.07f*z,.02f*x-.04f*z,3,4),
                    new Color(.2f+.001f*x,.3f+.002f*z,.4f,.7f));
            }
            var input = new[] {new Triangle(V(-halfWidth,0),V(halfWidth,20),V(halfWidth,0),0,2,7),
                new Triangle(V(-halfWidth,0),V(-halfWidth,20),V(halfWidth,20),0,2,8)};
            var seam=Matrix4x4.identity;seam.m23=-10f;
            var physical=new Partition(seam,new Partition(0),new Partition(1));
            if(!MovableRoadGeometry.TryBind(input,physical,2,1,out var physicalFaces,out var error))throw new Exception(error);
            int count=0; double sourceArea=0,outputArea=0;
            foreach(var bound in physicalFaces)
            {
                var triangle=bound.Geometry;sourceArea+=Area(triangle);
                if(!MovableRoadGeometry.TryBind(new[] {triangle},main,patches.Length,1,out var split,out error))throw new Exception(error);
                foreach(var face in split)
                {
                    count++;faces++;var t=face.Geometry;outputArea+=Area(t);
                    if(t.Material!=0 || t.Occurrence!=2 || t.SourceTriangle!=triangle.SourceTriangle)
                        throw new Exception("Appearance split changed source provenance.");
                    if(Area(t)<1e-6) { numericalSlivers++; continue; }
                    if(!MovableRoadGeometry.TryBind(new[]{t},physical,2,1,out var rebound,out error) || rebound.Any(r=>r.Bone!=bound.Bone && Area(r.Geometry)>=1e-6))
                        throw new Exception($"Appearance split changed physical bone ownership: width={halfWidth}, reflected={reflected}, sourceBone={bound.Bone}, rebound={string.Join(",",rebound.Select(r=>r.Bone))}, error={error}; z={t.A.Position.z:R}/{t.B.Position.z:R}/{t.C.Position.z:R}, areas={string.Join(",",rebound.Select(r=>Area(r.Geometry)))}.");
                    var patch=patches[face.Bone];
                    if(patch.Bands!=null)throw new Exception("Cross-band recipe survived domain assignment.");
                    foreach(var vertex in new[]{t.A,t.B,t.C})
                    {
                        var p=vertex.Position;
                        if(Math.Abs(p.y-(.1f*p.x+.05f*p.z))>1e-5f ||
                           Math.Abs(vertex.UV.x-(.03f*p.x+.07f*p.z))>1e-5f ||
                           Math.Abs(vertex.UV.y-(.02f*p.x-.04f*p.z))>1e-5f || vertex.UV.z!=3 || vertex.UV.w!=4 ||
                           Math.Abs(vertex.Color.r-(.2f+.001f*p.x))>1e-5f)
                            throw new Exception("Appearance split changed interpolated attributes.");
                    }
                    // Strict interior samples avoid assigning discontinuous seam
                    // samples to both neighbors. Shared-edge ownership is carried
                    // by the domain, not inferred by TryReduce after clipping.
                    for(int ia=1;ia<8;ia++)for(int ib=1;ib<8-ia;ib++)
                    {
                        var p=t.A.Position*(ia/8f)+t.B.Position*(ib/8f)+t.C.Position*((8-ia-ib)/8f);
                        float v=Vector3.Dot(p,original.V)+original.V.w;
                        var band=original.Bands.First(b=>v>=b.Start && v<b.End);
                        float expected=band.Min.y+Frac((v-band.Start)/(band.End-band.Start)*band.RepeatY)*(band.Max.y-band.Min.y);
                        float actual=patch.Min.y+Frac(Vector3.Dot(p,patch.V)+patch.V.w)*(patch.Max.y-patch.Min.y);
                        worst=Math.Max(worst,Math.Abs(expected-actual)*4096);points++;
                    }
                }
            }
            if(Math.Abs(sourceArea-outputArea)>sourceArea*1e-6)throw new Exception("Appearance domain area differs.");
            if(count>physicalFaces.Length*(2*original.Bands.Length+1))
                throw new Exception("Fragment count exceeds the appearance-domain bound.");
            counts.Add(count);
        }
        // Triangulation can create numerical slivers at already-shared edges;
        // its fragment count is bounded by appearance domains, never repetitions.
        if(worst>.02)throw new Exception("Appearance domain sampling differs: "+worst);
        if(edgeWorst>.02)throw new Exception("Appearance domain edge phase differs: "+edgeWorst);
        return $"PASS Draw03 material domains: {faces} fragments, {points} interior samples, worst={worst:R} source pixels; {edges} inclusive/both-side boundary phase probes, worst={edgeWorst:R} source pixels; bounded domain topology for 2/3/6-lane layouts across 20/40/80 m spans and reflected frames ({string.Join(",",counts)} fragments); physical bones, area, provenance and native UV/color interpolation retained; {numericalSlivers} floating-point slivers below 1e-6 square metres excluded from bone/sample assertions.";
    }
    static double Area(Triangle t)=>Vector3.Cross(t.B.Position-t.A.Position,t.C.Position-t.A.Position).magnitude*.5;
    static float Frac(float x)=>x-MathF.Floor(x);
}
