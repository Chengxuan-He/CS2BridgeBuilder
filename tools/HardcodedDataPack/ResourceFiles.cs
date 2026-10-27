using System.Text.RegularExpressions;

internal static class ResourceFiles
{
    internal static string Category(string name)
    {
        if(name.StartsWith("Types_", StringComparison.Ordinal))return name.Split('_')[1];
        // Authored repeat regions use a style-owned resource just like UV charts.
        // These short names are emitted directly by the semantic-unit tools.
        var repeat=Regex.Match(name,@"^(PedestrianDraw\d{2}|Draw\d{2}|Lift\d{2})(?:RepeatRegions|_StructureRepeatUnits|_?UvSampling)$");
        if(repeat.Success)return repeat.Groups[1].Value;
        var chart=Regex.Match(name,@"^(?:(PedestrianDraw\d{2}|Draw\d{2}|Lift\d{2})(?:_Native)?UvCharts|UvCharts_(PedestrianDraw\d{2}|Draw\d{2}|Lift\d{2}))$");
        if(chart.Success)return chart.Groups[1].Success?chart.Groups[1].Value:chart.Groups[2].Value;
        var native=Regex.Match(name,@"^NativeUvCharts_(PedestrianDraw|Draw|Lift)Bridge(\d{2})");
        if(native.Success)return native.Groups[1].Value+native.Groups[2].Value;
        var uv=Regex.Match(name,@"^UvDomain_(PedestrianDraw|Draw|Lift)Bridge(\d{2})");
        if(uv.Success)return uv.Groups[1].Value+uv.Groups[2].Value;
        return name=="GoldenBridgeRailings_cs"?"SuspensionGolden":"Shared";
    }
    internal static string PathFor(string root,string name)=>Path.Combine(root,Category(name),name+".bin");
    internal static byte[] Read(string path)=>File.ReadAllBytes(path);
    internal static void Write(string path,byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path,bytes);
    }
}
