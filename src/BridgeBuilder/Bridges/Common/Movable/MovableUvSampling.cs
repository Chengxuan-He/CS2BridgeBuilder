namespace BridgeBuilder.Bridges;

// Offline classifications of the prototype's authored mapping, independent of
// deformation, packing success and texture-global Repeat settings. Only exact
// measured domains opt in; selected-road projection does not use this catalogue.
internal static class MovableUvSampling
{
    internal static int Axes(string style,string render,int mesh,int domain)
        =>style switch
        {
            "Lift02"=>Lift02UvSampling.Axes(render,mesh,domain),
            "Lift04"=>Lift04UvSampling.Axes(render,mesh,domain),
            "Lift05"=>Lift05UvSampling.Axes(render,mesh,domain),
            _=>For(style,render,mesh,domain)?3:0
        };
    internal static bool For(string style,string render,int mesh,int domain)=>style switch
    {
        "Draw01"=>Draw01UvSampling.For(render,mesh,domain),
        "Draw02"=>Draw02UvSampling.For(render,mesh,domain),
        "Draw03"=>Draw03UvSampling.For(render,mesh,domain),
        "Lift01"=>Lift01UvSampling.For(render,mesh,domain),
        "Lift02"=>Lift02UvSampling.For(render,mesh,domain),
        "Lift03"=>Lift03UvSampling.For(render,mesh,domain),
        "Lift04"=>Lift04UvSampling.For(render,mesh,domain),
        "Lift05"=>Lift05UvSampling.For(render,mesh,domain),
        "PedestrianDraw01"=>PedestrianDraw01UvSampling.For(render,mesh,domain),
        "PedestrianDraw02"=>PedestrianDraw02UvSampling.For(render,mesh,domain),
        _=>false
    };
}
