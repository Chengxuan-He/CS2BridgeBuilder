using System.Text.Json;
using BridgeBuilder.Bridges;

using var mapping = JsonDocument.Parse(File.ReadAllBytes(args[0]));
int count = 0;
foreach (var row in mapping.RootElement.GetProperty("aliases").EnumerateArray())
{
    var sampler = row.GetProperty("alias").GetString()!;
    if (MovableRepeatUnits.For("Draw02", sampler) != null)
        throw new Exception("Retired Draw02 band alias remains reachable.");
    count++;
}
if (count != 5) throw new Exception("Incomplete historical alias coverage.");
if (MovableRepeatUnits.For("Draw02", "unknown") != null)
    throw new Exception("Unknown recipe was silently substituted.");
Console.WriteLine("PASS: all five historical Draw02 band aliases remain retired.");
