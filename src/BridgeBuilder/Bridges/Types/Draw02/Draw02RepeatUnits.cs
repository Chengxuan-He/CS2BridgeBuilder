namespace BridgeBuilder.Bridges;

// Corrected full-mip native unit bindings; descriptor aliases do not own pixels.
internal static partial class MovableRepeatUnits
{
    static partial void Draw02(string samplerKey, ref string? unitKey)
    {
        unitKey = samplerKey switch
        {
            "d6e1b593dfbb1c928471674b41fceb536c555ee2b7e76993a0e94464a09428a8"
                => "25b939b0993d5045220bf8ce334d90460fa9fc8994e2dacefab613c52378213c",
            "66b06f046fc341d42ad3bda4e19d126f2da37a0675098d12c1df45d6a75a4a7d"
                => "e3d523acd046abcca2ae8ef8f02b56cb475a983eb22c82191192c8c3cf5b8aad",
            "6fe8104278ede99ba9d088d14b82f5a8c5e8fea04ff346d600d0854e9c58a957"
                => "acae6b2c353623893fc69513060433abe1b4ba9ac1f8b5dee270b2c137f94509",
            "4ceca541dd3558d9055d7340612abadb128343767e1c2c1f376a4510fce4adcc"
                => "842426e70617fc11a909f1254949dbb5379e54d46d4b3c60bca52497bf8768d4",
            "851b4c15e7f05da15bd71c82d8f0fb686fadd95722ab926d8f66856861cf1c44"
                => "a5d0e6858afcba243612d9c77388b757b8599bd9117362cd080afadbd6268613",
            _ => null
        };
    }
}
