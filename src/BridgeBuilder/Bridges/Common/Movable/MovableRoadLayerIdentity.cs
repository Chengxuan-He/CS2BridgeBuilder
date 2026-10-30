namespace BridgeBuilder.Bridges;

// Source appearance identity is independent of the destination bridge style.
// Evidence: material-remap/Lift04/track-bed-layer-evidence.json and
// PedestrianDraw01/pathway-layer-evidence.json. Worldspace identifies one family;
// WholeReceiver additionally requires the measured base channels and UV footprint.
internal static class MovableRoadLayerIdentity
{
    internal static MovableRoadAppearance.Type WholeReceiver(string surface, bool builtin,
        string baseColor, string normal, string mask, MovableRoadAppearance.Type worldspace, float minU, float maxU)
    {
        if (!builtin) return MovableRoadAppearance.Type.None;
        if (surface == "a3522ead5e61e02569f0884fb4607eec" && worldspace == MovableRoadAppearance.Type.Ballast &&
            baseColor == "6bc6397af4c4779558d46a66beaf6f64" &&
            normal == "42cf28936b3ef6d5f8bfa78a0eba2d86" && mask == "fe820b176007ab650be5168cae1b46a6")
            return MovableRoadAppearance.Type.Ballast;
        if (surface == "16be9bdc3ead50651a57c398efa6ed2e" && worldspace == MovableRoadAppearance.Type.Pathway &&
            baseColor == "ecdec3f86d6792856b92044582832fb9" &&
            normal == "1e09bb5978cb13953988206f636cc6f5" && mask == "1c50af8d69f824751a0e29d32c2ea92c" &&
            minU >= 0f && maxU <= .5f)
            return MovableRoadAppearance.Type.Pathway;
        if (surface == "1d7dad9f64c9fe55a809f1080c39fb92" && worldspace == MovableRoadAppearance.Type.Asphalt &&
            baseColor == "9a6506aaede935752b5c4c0362c95f6e" &&
            normal == "a142c18b42577de52815d1afd054660b" && mask == "37ceea0e8c2bd24599e237236e6ae338" &&
            minU >= 0f && maxU <= .249755859375f)
            return MovableRoadAppearance.Type.Asphalt;
        return MovableRoadAppearance.Type.None;
    }

    internal static MovableRoadAppearance.Type Worldspace(string surface, bool builtin,
        string albedo, string normal, string mask, string asphalt)
    {
        if (builtin && surface == "1d7dad9f64c9fe55a809f1080c39fb92")
            return albedo == "b00176216b0ce36579bce6fc675cfb77" &&
                normal == "9f446790360787756b0e5909db12e200" && mask == "375b871b66aaf675dacef141eb2a9810"
                ? MovableRoadAppearance.Type.Asphalt : MovableRoadAppearance.Type.None;
        if (albedo == asphalt && asphalt.Length != 0) return MovableRoadAppearance.Type.Asphalt;
        if (!builtin) return MovableRoadAppearance.Type.None;
        if (surface == "a3522ead5e61e02569f0884fb4607eec" &&
            albedo == "18a5b9c996ce7c551bc422cd96a324ad" &&
            normal == "346c601cafab351569d7d0a4fcac777c" &&
            mask == "646901d94c691865ba8a1a647f57f7c3")
            return MovableRoadAppearance.Type.Ballast;
        if (surface == "16be9bdc3ead50651a57c398efa6ed2e" &&
            albedo == "81526dc63dccf7656b7cdef1b67aca93" &&
            normal == "c54fc222ccadf095c827c13b3b046dbe" &&
            mask == "d3893b93b2107e75196ce844817e762c")
            return MovableRoadAppearance.Type.Pathway;
        return MovableRoadAppearance.Type.None;
    }
}
