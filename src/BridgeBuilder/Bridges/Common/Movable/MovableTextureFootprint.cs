using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Pixel-space derivatives of every sampled channel, not only the color map.
internal sealed class MovableTextureFootprint
{
    private Vector2 _density;
    internal Vector2 Density => Vector2.Max(Vector2.one, _density);
    internal Vector2 NativeDensity => _density;

    internal void Include(Vector2 u, Vector2 v)
    {
        float x=u.magnitude,y=v.magnitude;
        // Bound the largest singular footprint, including oblique UV axes.
        float factor=x>0 && y>0?Mathf.Sqrt(1+Mathf.Min(1,Mathf.Abs(Vector2.Dot(u,v)/(x*y)))):1;
        _density=Vector2.Max(_density,new Vector2(x*factor,y*factor));
    }

    internal readonly struct Cycle
    {
        internal readonly double Frequency, Pixels;
        internal readonly TextureWrapMode Wrap;
        internal Cycle(double frequency,int pixels,TextureWrapMode wrap)
        { Frequency=frequency;Pixels=pixels;Wrap=wrap; }
    }

    // Numeric period evaluation of the shader's recorded projections. No mesh
    // classification or period cuts. Clamp and MirrorOnce are not periodic.
    internal static float Period(IReadOnlyList<Cycle> cycles,float extent,float maximum)
    {
        var active=cycles.Where(c=>Math.Abs(c.Frequency)*extent*c.Pixels>.01).ToArray();
        if(active.Length==0)return 0;
        if(active.Any(c=>c.Wrap!=TextureWrapMode.Repeat && c.Wrap!=TextureWrapMode.Mirror))return 0;
        double rate=active.Min(c=>Math.Abs(c.Frequency)/(c.Wrap==TextureWrapMode.Mirror?2:1));
        int count=(int)Math.Min(4096,Math.Floor(Math.Min(extent,maximum)*rate));
        for(int n=1;n<=count;n++)
        {
            double period=n/rate;
            bool match=true;
            foreach(var c in active)
            {
                double unit=c.Wrap==TextureWrapMode.Mirror?2:1;
                double phase=c.Frequency*period;
                // Bound accumulated float-roundoff drift over the entire receiver,
                // rather than accepting a small error once per repeated unit.
                if(Math.Abs(phase-Math.Round(phase/unit)*unit)*c.Pixels*Math.Max(1,extent/period)>.01)
                {match=false;break;}
            }
            if(match)return (float)period;
        }
        return 0;
    }
}
