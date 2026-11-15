using System;
using System.IO;
using System.Text;

namespace BridgeBuilder.Runtime;

// External native Surface prefix reader. It does not realize a Material or read owner programs.
internal static class BridgeCurrentProgramSurface
{
    internal static bool TryReadSurfaceIdentity(byte[] bytes, out int template, out int? queue,
        out float? drawOrder, out string? error)
    {
        template = 0; queue = null; drawOrder = null; error = null;
        try
        {
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8);
            ushort version = reader.ReadUInt16(); template = reader.ReadInt32();
            if (version != 1) return true;
            if (reader.ReadBoolean())
            {
                int stacks = reader.ReadInt32();
                if (stacks < 0 || stacks > 2) { error = "Invalid native Surface VT stack count."; return false; }
                if (reader.ReadBytes(272).Length != 272) { error = "Truncated native Surface VT header."; return false; }
                int present = reader.ReadByte();
                if (present != 0 && present != 255) { error = "Invalid native Surface VT null marker."; return false; }
                if (present != 0 && reader.ReadBytes(16).Length != 16) { error = "Truncated native Surface VT identity."; return false; }
            }
            for (int kind = 0; kind < 2; kind++)
            {
                int present = reader.ReadByte();
                if (present == 0) continue;
                if (present != 255) { error = "Invalid native Surface property null marker."; return false; }
                int count = reader.ReadInt32();
                if (count < 0 || count > stream.Length - stream.Position)
                { error = "Invalid native Surface property count."; return false; }
                for (int i = 0; i < count; i++)
                {
                    string property = reader.ReadString();
                    if (kind == 0)
                    {
                        float value = reader.ReadSingle();
                        if (property == "_DrawOrder") drawOrder = value;
                    }
                    else
                    {
                        int value = reader.ReadInt32();
                        if (property == BridgeNativeMaterialProgram.QueueProperty) queue = value;
                    }
                }
            }
            return true;
        }
        catch (Exception exception) { error = "Native Surface program identity: " + exception.Message; return false; }
    }
}
