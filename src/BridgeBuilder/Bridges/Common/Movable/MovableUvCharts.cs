using System;

namespace BridgeBuilder.Bridges;

// Explicit data routing, analogous to MovableUvCatalogue. Looking up a style
// loads only its measured chart arrays; no prototype inspection occurs here.
internal static class MovableUvCharts
{
    internal static MovableUvChart[] For(string style,string render,int mesh,int domain)=>style switch
    {
        "Draw01"=>Draw01UvCharts.For(render,mesh,domain),
        "Draw02"=>Draw02UvCharts.For(render,mesh,domain),
        "Draw03"=>Draw03UvCharts.For(render,mesh,domain),
        "Lift01"=>Lift01UvCharts.For(render,mesh,domain),
        "Lift02"=>Lift02UvCharts.For(render,mesh,domain),
        "Lift03"=>Lift03UvCharts.For(render,mesh,domain),
        "Lift04"=>Lift04UvCharts.For(render,mesh,domain),
        "Lift05"=>Lift05UvCharts.For(render,mesh,domain),
        "PedestrianDraw01"=>PedestrianDraw01UvCharts.For(render,mesh,domain),
        "PedestrianDraw02"=>PedestrianDraw02UvCharts.For(render,mesh,domain),
        _=>Array.Empty<MovableUvChart>()
    };
}
