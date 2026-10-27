using System;

namespace BridgeBuilder.Bridges;

// Source objects carry their native Surface/VT/mip graph unchanged. This binder
// does not instantiate materials, decode images, or change texture-global settings.
internal static class Lift02NativeStructureBinding
{
    internal static T Bind<T>(T original, string sourceDigest, MovableStructurePacking.Unit sampler) where T : class
    {
        if (original == null) throw new ArgumentNullException(nameof(original));
        // The original prototype atlas, not the former derived crop's reflection,
        // is authoritative. Only map when the actual receiver remains representable
        // in the source region with both Float32 gradients preserved.
        if (sampler.CanUseSource) return original;

        if (!Lift02NativeStructureCatalogue.SourceDigests.Contains(sourceDigest))
            throw new InvalidOperationException("Lift02 native material source content is not recorded: " + sourceDigest);

        var key = MovableStructureKey.For(sourceDigest, sampler.Triangle, sampler.Bounds,
            sampler.A, sampler.B, sampler.C);
        if (!Lift02NativeStructureCatalogue.SamplerSources.TryGetValue(key, out var sourceCid))
            throw new InvalidOperationException("Lift02 structural sampling is not recorded: " + key);
        throw new NotSupportedException("Lift02 native source-region sampling is not implemented for receiver: source="
            + sourceCid + "; sampler=" + key + ". The original Surface and full mip graph require a supported UV mapping.");
    }
}
