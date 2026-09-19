using BridgeBuilder.Bridges;

// Pure price arithmetic only: no mesh generation, game objects or visual assertions.
void Check(long bridge, long roads, long upper, long lower, long expectedOffset, long expectedTotal,
    long elevatedFloor = 0)
{
    if (!BridgeBasePrice.TryCalculate(bridge, roads, upper, lower, elevatedFloor, out var offset, out var total)
        || offset != expectedOffset || total != expectedTotal)
        throw new Exception($"Unexpected quote: offset={offset}, total={total}");
}
Check(148, 48, 48, 0, 4, 148); // Recorded Golden Gate prototype, original deck.
Check(148, 48, 60, 0, 4, 184); // Single deck.
Check(200, 100, 60, 40, -100, 200); // Negative offset retained, original double deck.
Check(200, 100, 60, 30, -100, 170); // Different roads.
Check(200, 100, 30, 60, -100, 170); // Main/aux role swap has no price effect.
Check(0, 0, 0, 0, 0, 0);
Check(108, 24, 12, 0, 36, 72, 60); // 1500/km road => 9000/km blue suspension, not 18000.
Check(108, 24, 12, 0, 36, 100, 100); // More expensive elevated road sets the floor.
Check(80, 40, 6, 0, -40, 38, 38); // Signed offset retained; negative formula raised to floor.
Check(200, 100, 6, 4, -100, 80, 80); // Both elevated decks contribute to the floor.
if (BridgeBasePrice.TryCalculate(0, 0, 0, 0, -1, out _, out _))
    throw new Exception("Invalid floor must be rejected");
if (BridgeBasePrice.TryCalculate(0, 0, int.MaxValue, 0, 0, out _, out _))
    throw new Exception("Native int overflow must be rejected");
if (BridgeBasePrice.TryCalculate(0, 0, long.MaxValue, 1, 0, out _, out _))
    throw new Exception("Intermediate overflow must be rejected");
Console.WriteLine("PASS: 1500/km -> 9000/km, elevated floor, single/double decks, signed offsets, range guards");
