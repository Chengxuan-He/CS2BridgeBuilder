using System;
using System.Collections.Generic;

namespace BridgeBuilder.Bridges;

// Native lane extents, not Voronoi regions inferred from car-lane centres.
internal static class MovableRoadLaneLayout
{
    [Flags]
    internal enum Kind { NonLaneGap = 0, Car = 1, Bicycle = 2, Pedestrian = 4, Parking = 8, Track = 16, Other = 32 }

    internal readonly struct Lane
    {
        internal readonly float Center, Width, Left, Right, StartZ, EndZ;
        internal readonly float StartWidth, EndWidth;
        internal readonly bool ConstantCrossSection;
        internal readonly Kind Type;
        internal readonly uint NativeFlags;
        private readonly double _ax, _az, _bx, _bz, _cx, _cz, _dx, _dz;
        private readonly float _firstWidth, _lastWidth, _transverseScale;
        private readonly bool _curve;
        internal Lane(float center, float width, float left, float right, float startZ, float endZ,
            Kind type, uint nativeFlags, float? startWidth = null, float? endWidth = null, bool straight = true)
        { Center = center; Width = width; Left = left; Right = right;
            _ax = _bx = _cx = _dx = center; _az = _bz = startZ; _cz = _dz = endZ;
            _firstWidth = startWidth ?? width; _lastWidth = endWidth ?? width; _transverseScale = 1; _curve = false;
            StartZ = Math.Min(startZ, endZ); EndZ = Math.Max(startZ, endZ); Type = type; NativeFlags = nativeFlags;
            StartWidth = (startZ <= endZ ? startWidth : endWidth) ?? width;
            EndWidth = (startZ <= endZ ? endWidth : startWidth) ?? width;
            ConstantCrossSection = straight && StartWidth == EndWidth; }

        internal Lane(float ax, float az, float bx, float bz, float cx, float cz, float dx, float dz,
            float width, float firstWidth, float lastWidth, float transverseScale, Kind type, uint nativeFlags)
        {
            _ax=ax; _az=az; _bx=bx; _bz=bz; _cx=cx; _cz=cz; _dx=dx; _dz=dz;
            _firstWidth=firstWidth; _lastWidth=lastWidth; _transverseScale=Math.Abs(transverseScale); _curve=true;
            Width=width; Type=type; NativeFlags=nativeFlags;
            StartZ=Math.Min(az,dz); EndZ=Math.Max(az,dz);
            StartWidth=az<=dz?firstWidth:lastWidth; EndWidth=az<=dz?lastWidth:firstWidth;
            ConstantCrossSection=ax==bx && ax==cx && ax==dx && firstWidth==lastWidth;
            Center=(ax+3*bx+3*cx+dx)/8;
            var half=(firstWidth+lastWidth)*.25f*_transverseScale;
            Left=Center-half; Right=Center+half;
        }

        // Native node lane widths are interpolated between the two CurveScale endpoint
        // factors. Solve the original monotone Bezier Z for t, not distance / endpoint span.
        private double Parameter(float z)
        {
            if (!_curve) return EndZ==StartZ?0:(z-StartZ)/(double)(EndZ-StartZ);
            if(z==_az)return 0; if(z==_dz)return 1;
            double lo=0,hi=1;
            while(true)
            {
                var t=(lo+hi)*.5;
                if(t==lo || t==hi)return t;
                var value=Bezier(_az,_bz,_cz,_dz,t);
                if(value==z)return t;
                if((value<z)==(_dz>_az))lo=t; else hi=t;
            }
        }
        private static double Bezier(double a,double b,double c,double d,double t)
        { var s=1-t; return s*s*s*a+3*s*s*t*b+3*s*t*t*c+t*t*t*d; }
        internal float WidthAt(float z)
        { var t=Parameter(z); return (float)(_firstWidth+(_lastWidth-_firstWidth)*t); }
        internal float CenterAt(float z) => _curve?(float)Bezier(_ax,_bx,_cx,_dx,Parameter(z)):Center;
        internal float LeftAt(float z) => _curve?CenterAt(z)-WidthAt(z)*.5f*_transverseScale:Left;
        internal float RightAt(float z) => _curve?CenterAt(z)+WidthAt(z)*.5f*_transverseScale:Right;
    }

    internal readonly struct Boundary
    {
        internal readonly float X;
        internal readonly Kind Left, Right;
        // Requires two native car-lane edges to coincide. A non-lane median/gap is never bridged.
        internal readonly bool CarCar;
        internal Boundary(float x, Kind left, Kind right, bool carCar)
        { X = x; Left = left; Right = right; CarCar = carCar; }
    }

    internal static Boundary[] BoundariesAt(IReadOnlyList<Lane> lanes, float z)
    {
        var active = new List<(float Left, float Right, Kind Type)>();
        var coordinates = new SortedSet<float>();
        foreach (var lane in lanes)
            if (z >= lane.StartZ && z <= lane.EndZ && lane.Right > lane.Left)
            {
                var left=lane.LeftAt(z); var right=lane.RightAt(z);
                active.Add((left,right,lane.Type)); coordinates.Add(left); coordinates.Add(right);
            }
        var result = new List<Boundary>();
        foreach (var x in coordinates)
        {
            Kind left = Kind.NonLaneGap, right = Kind.NonLaneGap;
            bool carEnd = false, carStart = false, carThrough = false;
            foreach (var lane in active)
            {
                if (lane.Left < x && lane.Right >= x) left |= lane.Type;
                if (lane.Left <= x && lane.Right > x) right |= lane.Type;
                if (lane.Type != Kind.Car) continue;
                carEnd |= lane.Right == x;
                carStart |= lane.Left == x;
                carThrough |= lane.Left < x && lane.Right > x;
            }
            var contact = left == Kind.Car && right == Kind.Car && carEnd && carStart && !carThrough;
            if (contact || (left != right && ((left | right) & Kind.Car) != 0))
                result.Add(new Boundary(x, left, right, contact));
        }
        return result.ToArray();
    }
}
