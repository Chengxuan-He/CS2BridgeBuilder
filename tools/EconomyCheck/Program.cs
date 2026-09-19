using BridgeBuilder.Bridges;

// Pure price arithmetic only: no mesh generation, game objects or visual assertions.
void Check(long bridge, long roads, long upper, long lower, long expectedOffset, long expectedTotal)
{
    if (!BridgeBasePrice.TryCalculate(bridge, roads, upper, lower, out var offset, out var total)
        || offset != expectedOffset || total != expectedTotal)
        throw new Exception($"Unexpected quote: offset={offset}, total={total}");
}
Check(148, 48, 48, 0, 4, 148); // Recorded Golden Gate prototype, original deck.
Check(148, 48, 60, 0, 4, 184); // Single deck.
Check(200, 100, 60, 40, -100, 200); // Negative offset retained, original double deck.
Check(200, 100, 60, 30, -100, 170); // Different roads.
Check(200, 100, 30, 60, -100, 170); // Main/aux role swap has no price effect.
Check(0, 0, 0, 0, 0, 0);
if (BridgeBasePrice.TryCalculate(100, 100, 1, 0, out _, out _))
    throw new Exception("Negative total must not wrap into uint");
if (BridgeBasePrice.TryCalculate(0, 0, int.MaxValue, 0, out _, out _))
    throw new Exception("Native int overflow must be rejected");
if (BridgeBasePrice.TryCalculate(0, 0, long.MaxValue, 1, out _, out _))
    throw new Exception("Intermediate overflow must be rejected");
Console.WriteLine("PASS: single/double deck formula, signed offsets, role symmetry, native range and overflow guards");
