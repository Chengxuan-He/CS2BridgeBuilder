namespace BridgeBuilder.Bridges;

// Embedded numerical arrays: StructurePrograms
// Binding identifiers only. Numerical implementation is immutable BBN input to
// the same common executor used by offline-compiled bridge-specific row tables.
internal static class StructurePrograms
{
    static StructurePrograms() { }
    internal static HardcodedNumbers Data => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.StructurePrograms.Data", () => HardcodedNumbers.Load("StructurePrograms"));
    internal const int HalfWidthTable = 0, DeltaTable = 1, ScaleX = 2, WidthScaleX = 3,
        TranslateX = 4, ConstantX = 5, Ornament = 6, EdgeXyz = 7, InteriorXyz = 8,
        PositiveHalfX = 9, NegativeHalfX = 10, NegativeTranslateX = 11,
        RowDelta = 12, SpanX = 13, SidewalkX = 14, DenseDelta = 15, ProfileScaleX = 16, DenseScaleX = 17,
        RowScaleX = 18, EdgeScaledScalar = 19, ScaledScalar = 20,
        RowOriginDelta = 21, OriginDelta = 22, TargetTranslation = 23,
        SignedSidewalk = 24, SignedSpan = 25, EdgeDeltaScalar = 26, RowSpanScalar = 27,
        AddScalar = 28, AddProductScalar = 29, CopyScalar = 30, TargetScalar = 31, ShearScalar = 32, ProductScalar = 33,
        BoundaryScalar = 34;
}
