using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Game.SceneFlow;

namespace BridgeBuilder.Runtime;

internal static class BridgeGameRestart
{
    private static bool _scheduled;

    internal static void Run()
    {
        if (_scheduled) return;
        try
        {
            using var current = Process.GetCurrentProcess();
            var executable = current.MainModule?.FileName;
            if (string.IsNullOrEmpty(executable) || !File.Exists(executable))
            { Mod.Log.Critical("Game restart failed: executable unavailable; game left running."); return; }
            var arguments = string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Select(QuoteArgument));
            string Literal(string value) => "'" + value.Replace("'", "''") + "'";
            // The helper survives normal game shutdown; never launch a competing game process.
            var script = "$ErrorActionPreference='Stop'; "
                + "$gameProcess=Get-Process -Id " + current.Id + " -ErrorAction SilentlyContinue; "
                + "if($gameProcess -and !$gameProcess.WaitForExit(120000)){exit 1}; "
                + "$start=New-Object System.Diagnostics.ProcessStartInfo; "
                + "$start.FileName=" + Literal(executable!) + "; "
                + "$start.WorkingDirectory=" + Literal(Environment.CurrentDirectory) + "; "
                + "$start.Arguments=" + Literal(arguments) + "; "
                + "$start.UseShellExecute=$true; [System.Diagnostics.Process]::Start($start)|Out-Null";
            using var helper = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                    @"WindowsPowerShell\v1.0\powershell.exe"),
                Arguments = "-NoProfile -NonInteractive -EncodedCommand "
                    + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            if (helper == null) { Mod.Log.Critical("Game restart helper did not start; game left running."); return; }
            _scheduled = true;
            Mod.Log.Info("Game restart scheduled after normal process exit.");
            GameManager.QuitGame();
        }
        catch (Exception exception)
        {
            Mod.Log.Critical(exception, "Could not restart game.");
        }
    }

    // Windows command-line escaping preserves spaces, quotes and trailing backslashes.
    internal static string QuoteArgument(string argument)
    {
        var result = new StringBuilder("\"");
        var slashes = 0;
        foreach (var character in argument)
        {
            if (character == '\\') { slashes++; continue; }
            result.Append('\\', character == '"' ? slashes * 2 + 1 : slashes);
            result.Append(character);
            slashes = 0;
        }
        return result.Append('\\', slashes * 2).Append('"').ToString();
    }
}
