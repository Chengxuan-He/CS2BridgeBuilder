using BridgeBuilder.Runtime;
using System.Diagnostics;

// This must run BEFORE launching Cities2: native ImportedData loading precedes IMod.OnLoad.
if (args.Length < 1 || args.Length > 2 || (args.Length == 2 && args[1] != "--apply" && args[1] != "--check"))
{
    Console.Error.WriteLine("Usage: UnlockRepair <Cities Skylines II user-data directory> [--check | --apply]");
    return 2;
}
if (Process.GetProcessesByName("Cities2").Length != 0)
{
    Console.Error.WriteLine("Exit Cities2 before checking or migrating bridge files. Nothing was changed.");
    return 2;
}
var apply = args.Contains("--apply");
if (!Directory.Exists(args[0]))
{
    Console.Error.WriteLine("User-data directory not found. Nothing was changed.");
    return 2;
}
if (!BridgeUnlockMigration.Run(args[0], apply, out var count, out var error))
{
    Console.Error.WriteLine($"Preflight incomplete; do not launch the game. {count} files completed. {error}");
    return 1;
}
Console.WriteLine(apply
    ? $"Migrated {count} bridge files. UUIDs, CIDs, geometry and save files unchanged; originals retained as .bbunlockbackup."
    : $"Preflight passed. {count} legacy bridge files require --apply BEFORE launching the game.");
return !apply && count > 0 ? 3 : 0;
