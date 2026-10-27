using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using System.Text;

// Exercise the actual resolver, native surface parser and persistence traversal used by self-check.
internal static class MaterialSnapshotCheck
{
    internal static void Run(string imported)
    {
        var owner = "b" + Guid.NewGuid();
        var rootCid = new string('1', 32);
        var materialCid = new string('2', 32);
        var textureCid = new string('3', 32);
        var builtinCid = new string('4', 32);
        var rootBytes = Encoding.UTF8.GetBytes("CID:" + materialCid + " CID:" + materialCid);
        var materialBytes = Surface(textureCid, builtinCid);
        var textureBytes = new byte[] { 12, 34, 56, 78 };
        var rootPath = Path.Combine(imported, owner, "Prefabs", "Bridge.Prefab");
        Directory.CreateDirectory(Path.GetDirectoryName(rootPath)!);
        File.WriteAllBytes(rootPath, rootBytes); File.WriteAllText(rootPath + ".cid", rootCid);
        var root = new AssetData(rootCid, () => File.OpenRead(rootPath));
        root.Metadata.path = rootPath;
        root.Metadata.packaged = false;
        var material = Asset(materialCid, ".Surface", "SubscribedMod/Appearance/Surface", materialBytes);
        var texture = Asset(textureCid, ".Texture", "SubscribedMod/Appearance/Texture", textureBytes);
        var builtin = new AssetData(builtinCid, () => throw new Exception("Built-in bytes must not be copied")) { isBuiltin = true };
        AssetDatabase.global.Assets.Clear();
        AssetDatabase.global.Assets.AddRange(new[] { root, material, texture, builtin });
        Check(BridgeDependencyPersistence.Save(owner, new[] { rootCid }, out var count, out var error, out var written)
            && count == 2 && written == 4, "healthy bridge supplements external material + texture: " + error);
        var folder = BridgeDependencyCopies.Folder(UnityEngine.Application.persistentDataPath, owner);
        var materialPath = Path.Combine(folder, materialCid + ".Surface");
        var texturePath = Path.Combine(folder, textureCid + ".Texture");
        Check(File.ReadAllBytes(materialPath).SequenceEqual(materialBytes)
            && File.ReadAllBytes(texturePath).SequenceEqual(textureBytes)
            && File.ReadAllText(materialPath + ".cid") == materialCid
            && File.ReadAllText(texturePath + ".cid") == textureCid, "original material/texture bytes and CIDs retained");
        var stamp = File.GetLastWriteTimeUtc(materialPath);
        Check(BridgeDependencyPersistence.Save(owner, new[] { rootCid }, out count, out error, out written)
            && count == 2 && written == 0 && File.GetLastWriteTimeUtc(materialPath) == stamp,
            "repeated self-check leaves complete copies untouched");
        File.Delete(texturePath + ".cid");
        Check(BridgeDependencyPersistence.Save(owner, new[] { rootCid }, out _, out error, out written) && written == 1,
            "missing texture sidecar supplemented without rewriting payload");
        // Simulate the next native index after removing the source mod: only owner snapshots remain.
        AssetDatabase.global.Assets.Clear();
        var localMaterial = new AssetData(materialCid, () => File.OpenRead(materialPath));
        localMaterial.Metadata.path = materialPath; localMaterial.Metadata.extension = ".Surface"; localMaterial.Metadata.packaged = false;
        var localTexture = new AssetData(textureCid, () => File.OpenRead(texturePath));
        localTexture.Metadata.path = texturePath; localTexture.Metadata.extension = ".Texture"; localTexture.Metadata.packaged = false;
        AssetDatabase.global.Assets.AddRange(new[] { root, localMaterial, localTexture, builtin });
        Check(BridgeDependencyPersistence.Save(owner, new[] { rootCid }, out count, out error, out written)
            && count == 2 && written == 0, "current-owner snapshots resolve with source mod absent: " + error);
        Check(!BridgeDependencyPersistence.Save("b" + Guid.NewGuid(), new[] { materialCid }, out _, out error)
            && error.Contains(materialCid), "another bridge cannot borrow these snapshots");
        Check(File.ReadAllBytes(rootPath).SequenceEqual(rootBytes) && File.ReadAllText(rootPath + ".cid") == rootCid,
            "root serialized references and identity remain unchanged");
    }

    private static AssetData Asset(string cid, string extension, string path, byte[] bytes)
    {
        var asset = new AssetData(cid, () => new MemoryStream(bytes, writable: false));
        asset.Metadata.extension = extension; asset.Metadata.path = path;
        return asset;
    }

    private static byte[] Surface(params string[] textures)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)1); writer.Write(0); writer.Write(false);
        for (var i = 0; i < 4; i++) writer.Write((byte)0);
        writer.Write((byte)255); writer.Write(textures.Length);
        for (var i = 0; i < textures.Length; i++)
        {
            writer.Write(i == 0 ? "_BaseColorMap" : "_NormalMap"); writer.Write((byte)255);
            for (var n = 0; n < 32; n += 2)
                writer.Write((byte)(Convert.ToInt32(textures[i][n].ToString(), 16)
                    | Convert.ToInt32(textures[i][n + 1].ToString(), 16) << 4));
        }
        writer.Write((byte)0);
        return stream.ToArray();
    }

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        Console.WriteLine("PASS " + message);
    }
}
