using System.Text.Json;
using BridgeBuilder.Bridges;

using var mapping = JsonDocument.Parse(File.ReadAllBytes(args[0]));
int count = 0;
foreach (var row in mapping.RootElement.GetProperty("aliases").EnumerateArray())
{
    var sampler = row.GetProperty("alias").GetString()!;
    var key = MovableRepeatUnits.For("Draw02", sampler);
    if (key != row.GetProperty("unitKey").GetString()) throw new Exception("Production binding differs from corrected producer.");
    var descriptor = Path.Combine(args[1], "RepeatUnits", key!, "Repeat.Surface");
    if (!File.Exists(descriptor) || File.ReadAllText(descriptor + ".cid").Trim() != row.GetProperty("surfaceCid").GetString())
        throw new Exception("Production binding does not resolve to the verified descriptor.");
    using (var reader = new BinaryReader(File.OpenRead(descriptor)))
    {
        int expectedTemplate = row.TryGetProperty("template", out var template) ? template.GetInt32() : 14;
        if (reader.ReadUInt16() != 1 || reader.ReadInt32() != expectedTemplate || reader.ReadBoolean())
            throw new Exception("Cutout call lost its double-sided native template.");
        if (reader.ReadByte() != 255 || reader.ReadInt32() != 1 || reader.ReadString() != "_Snow" || reader.ReadSingle() != 0)
            throw new Exception("Repeat call lost its Snow override.");
    }
    if (MovableRepeatUnits.For("OtherStyle", sampler) != null) throw new Exception("Style scope escaped.");
    count++;
}
foreach (var unsupported in new[] { "unknown" })
    if (MovableRepeatUnits.For("Draw02", unsupported) != null) throw new Exception("Unprepared band recipe was silently replaced.");
if (count != 5) throw new Exception("Incomplete verified alias coverage.");
Console.WriteLine("PASS Draw02 production RepeatUnits: 5 aliases resolve with correct native templates; unknown keys remain unmapped.");
