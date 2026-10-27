namespace BridgeBuilder.Bridges;

// Bridge-specific partials contain reviewed source-recipe aliases, never receiver
// coordinates or generated-result caches. A missing alias keeps ordinary source
// selection; it does not invent a repeatable pattern or reinterpret a band recipe.
internal static partial class MovableRepeatUnits
{
    internal static string? For(string styleId,string samplerKey)
    {
        string? key=null;
        switch(styleId)
        {
            case "Draw01": Draw01(samplerKey,ref key);break;
            case "Draw02": Draw02(samplerKey,ref key);break;
            case "Draw03": Draw03(samplerKey,ref key);break;
            case "Lift01": Lift01(samplerKey,ref key);break;
            case "Lift02": Lift02(samplerKey,ref key);break;
            case "Lift03": Lift03(samplerKey,ref key);break;
            case "Lift04": Lift04(samplerKey,ref key);break;
            case "Lift05": Lift05(samplerKey,ref key);break;
            case "PedestrianDraw01": PedestrianDraw01(samplerKey,ref key);break;
            case "PedestrianDraw02": PedestrianDraw02(samplerKey,ref key);break;
        }
        return key;
    }
    static partial void Draw01(string samplerKey,ref string? unitKey);
    static partial void Draw02(string samplerKey,ref string? unitKey);
    static partial void Draw03(string samplerKey,ref string? unitKey);
    static partial void Lift01(string samplerKey,ref string? unitKey);
    static partial void Lift02(string samplerKey,ref string? unitKey);
    static partial void Lift03(string samplerKey,ref string? unitKey);
    static partial void Lift04(string samplerKey,ref string? unitKey);
    static partial void Lift05(string samplerKey,ref string? unitKey);
    static partial void PedestrianDraw01(string samplerKey,ref string? unitKey);
    static partial void PedestrianDraw02(string samplerKey,ref string? unitKey);
}
