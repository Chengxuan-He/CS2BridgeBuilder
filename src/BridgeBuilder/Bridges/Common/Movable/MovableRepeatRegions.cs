using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static partial class MovableRepeatUnits
{
    // Only a bridge's measured genuine repeating source region may register here.
    // A descriptor identity includes cutout/normal semantics; its shared texture
    // identity does not depend on a receiver, domain, LOD or sampler.
    internal readonly struct Region
    {
        internal readonly string BindingKey;
        internal readonly double MinU,MinV,MaxU,MaxV;
        internal readonly double CoverageMinU,CoverageMinV,CoverageMaxU,CoverageMaxV;
        internal readonly bool RepeatU,RepeatV;
        private readonly MovableUvChart? _coverageChart;
        internal Region(string bindingKey,double minU,double minV,double maxU,double maxV)
            :this(bindingKey,minU,minV,maxU,maxV,minU,minV,maxU,maxV) { }
        internal Region(string bindingKey,double minU,double minV,double maxU,double maxV,MovableUvChart coverage,bool repeatU=true,bool repeatV=true)
            :this(bindingKey,minU,minV,maxU,maxV,minU,minV,maxU,maxV,repeatU,repeatV)
        { _coverageChart=coverage??throw new ArgumentNullException(nameof(coverage)); }
        // Coverage identifies the original authored material domain; the smaller
        // measured period supplies its sampling unit. Neither depends on receivers.
        internal Region(string bindingKey,double minU,double minV,double maxU,double maxV,
            double coverageMinU,double coverageMinV,double coverageMaxU,double coverageMaxV,bool repeatU=true,bool repeatV=true)
        {
            if(string.IsNullOrEmpty(bindingKey)||!double.IsFinite(minU)||!double.IsFinite(minV)||
               !double.IsFinite(maxU)||!double.IsFinite(maxV)||minU>=maxU||minV>=maxV||
               !double.IsFinite(coverageMinU)||!double.IsFinite(coverageMinV)||
               !double.IsFinite(coverageMaxU)||!double.IsFinite(coverageMaxV)||
               coverageMinU>=coverageMaxU||coverageMinV>=coverageMaxV)
                throw new ArgumentException("Invalid measured repeat region.");
            BindingKey=bindingKey;MinU=minU;MinV=minV;MaxU=maxU;MaxV=maxV;
            CoverageMinU=coverageMinU;CoverageMinV=coverageMinV;
            CoverageMaxU=coverageMaxU;CoverageMaxV=coverageMaxV;
            RepeatU=repeatU;RepeatV=repeatV;
            _coverageChart=null;
        }
        private bool Contains(Vector2 uv)=>uv.x>=CoverageMinU&&uv.x<=CoverageMaxU&&uv.y>=CoverageMinV&&uv.y<=CoverageMaxV;
        internal bool ContainsDomain(MovableStructurePacking.Unit unit)
        {
            if(_coverageChart!=null)
                return unit.Triangle ? _coverageChart.ContainsTriangle(unit.A,unit.B,unit.C)
                    : _coverageChart.ContainsTriangle(new(unit.Bounds.x,unit.Bounds.y),new(unit.Bounds.z,unit.Bounds.y),new(unit.Bounds.z,unit.Bounds.w))&&
                      _coverageChart.ContainsTriangle(new(unit.Bounds.x,unit.Bounds.y),new(unit.Bounds.z,unit.Bounds.w),new(unit.Bounds.x,unit.Bounds.w));
            return unit.Triangle ? Contains(unit.A)&&Contains(unit.B)&&Contains(unit.C)
                : unit.Bounds.x>=CoverageMinU&&unit.Bounds.y>=CoverageMinV&&unit.Bounds.z<=CoverageMaxU&&unit.Bounds.w<=CoverageMaxV;
        }
        internal double[] MapExact(MovableStructurePacking.Unit unit,Vector2 uv)
        {
            double x=uv.x,y=uv.y;
            if(unit.ExactCoordinates!=null&&unit.ExactCoordinates.TryGetValue(uv,out var exact))
            {x=exact[0];y=exact[1];}
            double u=unit.Triangle?unit.A.x+((double)unit.B.x-unit.A.x)*x+((double)unit.C.x-unit.A.x)*y
                :unit.Bounds.x+((double)unit.Bounds.z-unit.Bounds.x)*x;
            double v=unit.Triangle?unit.A.y+((double)unit.B.y-unit.A.y)*x+((double)unit.C.y-unit.A.y)*y
                :unit.Bounds.y+((double)unit.Bounds.w-unit.Bounds.y)*y;
            return new[]{(u-MinU)/(MaxU-MinU),(v-MinV)/(MaxV-MinV)};
        }
    }

    internal sealed class RegionMapping
    {
        internal readonly Region Region;
        private readonly Dictionary<Vector2,Vector2> _points;
        private RegionMapping(Region region,Dictionary<Vector2,Vector2> points){Region=region;_points=points;}
        internal Vector2 Map(Vector2 uv)=>_points[uv];

        internal static RegionMapping? Create(Region region,MovableStructurePacking.Unit unit)
        {
            if(!region.ContainsDomain(unit)||unit.Points.Count==0||unit.Points.Count%3!=0||
               (unit.RepeatOnly&&unit.ExactCoordinates==null))return null;
            var exact=unit.Points.Select(p=>region.MapExact(unit,p)).ToArray();
            if(exact.Any(q=>!double.IsFinite(q[0])||!double.IsFinite(q[1])))return null;
            // Nonperiodic axes retain their authored profile. A repeated base
            // pattern cannot authorize sampling beyond a bevel or flange edge.
            if(exact.Any(q=>(!region.RepeatU&&(q[0]<0||q[0]>1))||(!region.RepeatV&&(q[1]<0||q[1]>1))))return null;
            foreach(var q in exact)
            {
                if(!region.RepeatU)q[0]=q[0]*.5+.25;
                if(!region.RepeatV)q[1]=q[1]*.5+.25;
            }
            // Change only whole genuine-unit phases. The old triangular-domain
            // phase is not generally a period of this rectangular texture.
            double ox=region.RepeatU?Math.Floor(exact[0][0]):0,oy=region.RepeatV?Math.Floor(exact[0][1]):0;
            var points=exact.Select(q=>new Vector2((float)(q[0]-ox),(float)(q[1]-oy))).ToArray();
            if(points.Any(p=>!float.IsFinite(p.x)||!float.IsFinite(p.y)))return null;
            for(int i=0;i<points.Length;i+=3)
            {
                double ex=exact[i+1][0]-exact[i][0],ey=exact[i+1][1]-exact[i][1];
                double fx=exact[i+2][0]-exact[i][0],fy=exact[i+2][1]-exact[i][1];
                double rx=(double)points[i+1].x-points[i].x,ry=(double)points[i+1].y-points[i].y;
                double sx=(double)points[i+2].x-points[i].x,sy=(double)points[i+2].y-points[i].y;
                double determinant=ex*fy-fx*ey;
                if(determinant==0)
                {if(rx!=ex||ry!=ey||sx!=fx||sy!=fy)return null;continue;}
                double a00=(rx*fy-sx*ey)/determinant-1,a01=(sx*ex-rx*fx)/determinant;
                double a10=(ry*fy-sy*ey)/determinant,a11=(sy*ex-ry*fx)/determinant-1;
                if(a00*a00+a01*a01+a10*a10+a11*a11>1e-8)return null;
            }
            var mapping=new Dictionary<Vector2,Vector2>();
            for(int i=0;i<points.Length;i++)
            {
                if(mapping.TryGetValue(unit.Points[i],out var prior)&&!prior.Equals(points[i]))return null;
                mapping[unit.Points[i]]=points[i];
            }
            return new RegionMapping(region,mapping);
        }
    }

    internal static IReadOnlyList<Region> Regions(string styleId,string sourceDigest)
    {
        var regions=new List<Region>();
        switch(styleId)
        {
            case "Draw01":Draw01Regions(sourceDigest,regions);break;
            case "Draw02":Draw02Regions(sourceDigest,regions);break;
            case "Draw03":Draw03Regions(sourceDigest,regions);break;
            case "Lift01":Lift01Regions(sourceDigest,regions);break;
            case "Lift02":Lift02Regions(sourceDigest,regions);break;
            case "Lift03":Lift03Regions(sourceDigest,regions);break;
            case "Lift04":Lift04Regions(sourceDigest,regions);break;
            case "Lift05":Lift05Regions(sourceDigest,regions);break;
            case "PedestrianDraw01":PedestrianDraw01Regions(sourceDigest,regions);break;
            case "PedestrianDraw02":PedestrianDraw02Regions(sourceDigest,regions);break;
        }
        return regions;
    }
    static partial void Draw01Regions(string sourceDigest,List<Region> regions);
    static partial void Draw02Regions(string sourceDigest,List<Region> regions);
    static partial void Draw03Regions(string sourceDigest,List<Region> regions);
    static partial void Lift01Regions(string sourceDigest,List<Region> regions);
    static partial void Lift02Regions(string sourceDigest,List<Region> regions);
    static partial void Lift03Regions(string sourceDigest,List<Region> regions);
    static partial void Lift04Regions(string sourceDigest,List<Region> regions);
    static partial void Lift05Regions(string sourceDigest,List<Region> regions);
    static partial void PedestrianDraw01Regions(string sourceDigest,List<Region> regions);
    static partial void PedestrianDraw02Regions(string sourceDigest,List<Region> regions);
}
