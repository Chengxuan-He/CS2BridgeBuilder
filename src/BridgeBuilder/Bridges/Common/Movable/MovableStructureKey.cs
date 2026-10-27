using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// The key identifies an immutable source sampler, not generated receiver geometry.
// Shared by offline preparation and runtime lookup; every numeric bit is retained.
internal static class MovableStructureKey
{
    internal const string Recipe = "BridgeBuilder/StructuralRepeat/1";
    internal static string Digest(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    internal static string For(string sourceDigest, bool triangle, Vector4 bounds, Vector2 a, Vector2 b, Vector2 c)
    {
        using var bytes = new MemoryStream();
        using var writer = new BinaryWriter(bytes);
        writer.Write(Recipe); writer.Write(sourceDigest); writer.Write(triangle);
        if (triangle)
        {
            foreach (var value in new[] { a, b, c }) { writer.Write(value.x); writer.Write(value.y); }
        }
        else { writer.Write(bounds.x); writer.Write(bounds.y); writer.Write(bounds.z); writer.Write(bounds.w); }
        writer.Flush();
        return Digest(bytes.ToArray());
    }
}
