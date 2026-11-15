using System;
using System.IO;
using System.Text;
using Colossal.Json;

namespace CS2Mods.Shared.Infrastructure;

/// <summary>Standard JSON configuration using the game's JSON serializer, without asset registration.</summary>
internal static class JsonConfigurationFile
{
    internal static T Read<T>(string path) where T : class, new()
    {
        var result = new T();
        JSON.WriteInto(JSON.Load(File.ReadAllText(path)), ref result);
        return result;
    }

    internal static void Write<T>(string path, T value) where T : class
    {
        var destination = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JSON.Dump(value), new UTF8Encoding(false));
            if (File.Exists(destination)) File.Replace(temporary, destination, null);
            else File.Move(temporary, destination);
        }
        finally { File.Delete(temporary); }
    }
}
