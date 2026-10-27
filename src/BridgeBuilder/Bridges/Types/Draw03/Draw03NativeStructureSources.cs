using System;
using System.IO;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;

// Original Draw03 native sources, independently identified from the shipped game.
// Texture identities and full VT/mip graphs remain owned by the native Surface.
// Receiver samplers remain in measured geometry data, not texture identity.
internal static class Draw03NativeStructureSources
{
    internal static string? SourceDigest(string sourceCid) => sourceCid switch
    {
        "09d384bac9fa877358ef7791a879cf60" => "bd76e7f55f6c05fa327cd5f81fb025e263bc27d26913182ce97851944360fe28",
        "25627b171c4bafc49229e18d08fc7939" => "662f9af6c135d03e374f7cd4f9cb5720fe6dbcbffd12d07a1af98345c76240a1",
        "326e721264be0f3253f6342c2d66386c" => "bd76e7f55f6c05fa327cd5f81fb025e263bc27d26913182ce97851944360fe28",
        "3405c6dec952aa0ced1de6a74b9d75dd" => "6b9344e70d8ff45b1d121385ad58cdb07507338d319ba07ac487becb9251bf82",
        "350e0f2d943a7ffb6b2e88e40d3eb4f6" => "296b67278317ce69218743cd3ad7ef5867636bf8639e3834caf8cd9b11997d15",
        "3afa75210adf6176ad365cd63bb0539e" => "6b9344e70d8ff45b1d121385ad58cdb07507338d319ba07ac487becb9251bf82",
        "4051f5abf32f10690118a132c9e9b5cb" => "f43572ffba554ed8a9726d364f88da8991a9b423917be417e599a0bc7248aefe",
        "4d9fc727d7b33c6634c749f9366374af" => "d2485becc959e32eeada0eac9cb97997faf585c785133c07b81664bcf05c3bc1",
        "6704b71480669ef830c4a98b195ab105" => "296b67278317ce69218743cd3ad7ef5867636bf8639e3834caf8cd9b11997d15",
        "792a855e9d0c5c34d2c41d48b062139d" => "fb159828e8d73c22dc96cdf7e82c74b36a53483d0126124a6eacbd0fda4a7e46",
        "7daf1691b38921d037a72de87093e521" => "662f9af6c135d03e374f7cd4f9cb5720fe6dbcbffd12d07a1af98345c76240a1",
        "81d1460e8651ade51c7446562a728aaa" => "296b67278317ce69218743cd3ad7ef5867636bf8639e3834caf8cd9b11997d15",
        "8c0e869e9e26251f2df1f613097dff35" => "baa6e51a9d9511d1a143b615c12a74f089bf6804970d19fddd0628e6b72d71a6",
        "9212c7ed45a80abe105853ffe71bc8c4" => "19f98d6bc888c66389d17b4f26ac145571410c991f500cb63b56db3fb0aee793",
        "bc8ade3c4aee9dddf394bba30387bbd4" => "1089fe0fde8e6b4616b03ea976a884cb31c417d32f8fb0f6a90c8c7e942fae71",
        "c54ae1d613c0300cfe4679b0d195acfa" => "bd76e7f55f6c05fa327cd5f81fb025e263bc27d26913182ce97851944360fe28",
        "d03892b7da3a2d59c54f291688a97768" => "296b67278317ce69218743cd3ad7ef5867636bf8639e3834caf8cd9b11997d15",
        "e13305b0eabb281072c2eb0ad8113c14" => "1917718371b1923b536ea58dced7d5427a3570a12d1244ba2ea047bc610e5f43",
        "e965aed500cf3353b1bcc3056028a4f6" => "296b67278317ce69218743cd3ad7ef5867636bf8639e3834caf8cd9b11997d15",
        "f8ac595e9c25af6d5017a62bcd37c980" => "88bd1b7047945443929d0a3b9756ca45c00766f7258c8f65b9b9ac2ea4fb8004",
        "fdfd44f97b07c61262c0e6ffdaf20ec4" => "662f9af6c135d03e374f7cd4f9cb5720fe6dbcbffd12d07a1af98345c76240a1",
        _ => null
    };

    // Unknown sources and unsafe UV mappings remain inapplicable. Identical source
    // contents under different built-in CIDs retain the actual original identity.
    internal static bool TryBind(SurfaceAsset source, string sourceDigest,
        MovableStructurePacking.Unit unit, out MovableStructureTiles.Slot slot)
    {
        slot = null!;
        var expected = SourceDigest(source.id.guid.ToString());
        if (expected == null || !unit.CanUseSource) return false;
        if (!source.isBuiltin || expected != sourceDigest)
            throw new InvalidDataException("Draw03 native structural source identity/content differs.");
        slot = new MovableStructureTiles.Slot { Surface = source, Original = unit };
        return true;
    }

    // No fallback to a generated bridge's copy and no new material or texture.
    internal static SurfaceAsset Resolve(string sourceCid)
    {
        var expected = SourceDigest(sourceCid)
            ?? throw new InvalidDataException("Unknown Draw03 native structural source identity.");
        if (!AssetDatabase.global.TryGetAsset(Colossal.Hash128.Parse(sourceCid), out SurfaceAsset source)
            || !source.isBuiltin)
            throw new InvalidDataException("Draw03 native structural source is unavailable: " + sourceCid);
        using var stream = source.GetReadStream();
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        if (MovableStructureKey.Digest(bytes.ToArray()) != expected)
            throw new InvalidDataException("Draw03 native structural source content differs: " + sourceCid);
        return source;
    }
}
