using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using BridgeBuilder.Runtime;
using BridgeBuilder.Settings;
using Colossal.UI.Binding;
using Game.UI.Editor;
using Game.UI.Widgets;

namespace BridgeBuilder.UI;

public partial class BridgeBuilderUISystem
{
    private ValueBinding<string> _exportSuccessPath = null!;
    private ValueBinding<string> _exportPath = null!;
    private ValueBinding<string> _exportFolder = null!;
    private ValueBinding<bool> _exportBusy = null!;
    private string? _exportOwner;
    private WidgetBindings _exportBrowser = null!;
    private ValueBinding<bool> _exportBrowserOpen = null!;
    private Task<(bool Success, string Message)>? _exportTask;

    private void BindExport()
    {
        _exportSuccessPath = AddValue("ExportSuccessPath", "", new BridgeStringWriter());
        AddBinding(new TriggerBinding(Group, "DismissExportSuccess", () => _exportSuccessPath.Update("")));
        AddBinding(new TriggerBinding(Group, "OpenExportDirectory", OpenExportDirectory));
        AddUpdateBinding(_exportBrowser = new WidgetBindings("BridgeBuilderExportBrowser", "children"));
        _exportBrowser.AddDefaultBindings();
        _exportBrowser.AddBindings<IItemPicker.Bindings>();
        _exportBrowserOpen = AddValue("ExportBrowserOpen", false, new BridgeBoolWriter());
        AddBinding(new TriggerBinding(Group, "CloseExportBrowser", CloseExportBrowser));
        _exportFolder = AddValue("ExportFolder", "", new BridgeStringWriter());
        _exportPath = AddValue("ExportPath", "", new BridgeStringWriter());
        _exportBusy = AddValue("ExportBusy", false, new BridgeBoolWriter());
        AddBinding(new TriggerBinding<string>(Group, "BrowseExportDirectory", BrowseExportDirectory, new BridgeStringReader()));
        AddBinding(new TriggerBinding<string>(Group, "PrepareExport", PrepareExport, new BridgeStringReader()));
        AddBinding(new TriggerBinding<string, string>(Group, "ConfirmExport", ConfirmExport, new BridgeStringReader(), new BridgeStringReader()));
        AddBinding(new TriggerBinding(Group, "CancelExport", () => { _exportOwner = null; _exportPath.Update(""); }));
    }

    private void PrepareExport(string owner)
    {
        if (_exportTask != null) return;
        try
        {
            var bridge = BridgeAssetCatalog.Find(owner) ?? throw new IOException("Bridge not found.");
            var directory = Mod.Setting?.DefaultExportDirectory ?? BridgeZipExport.DefaultDirectory;
            _exportOwner = owner;
            _exportFolder.Update(owner);
            _exportPath.Update(Path.Combine(Path.GetFullPath(directory), BridgeZipExport.DefaultFileName(bridge.DisplayName)));
        }
        catch (Exception exception) { ExportFailure(exception.Message); }
    }

    private void BrowseExportDirectory(string destination)
    {
        if (_exportOwner == null || _exportTask != null) return;
        try
        {
            var fullPath = Path.GetFullPath(destination);
            var directory = Path.GetDirectoryName(fullPath)!;
            while (!Directory.Exists(directory) && Path.GetDirectoryName(directory) is string parent)
                directory = parent;
            var owner = _exportOwner;
            var browser = new DirectoryBrowserPanel(directory, null, watchChanges: false, pickFiles: false, selected =>
            {
                if (_exportOwner == owner)
                    _exportPath.Update(Path.Combine(selected, Path.GetFileName(fullPath)));
                CloseExportBrowser();
            }, CloseExportBrowser);
            _exportBrowser.children = ((IEditorPanel)browser).children;
            _exportBrowserOpen.Update(true);
        }
        catch (Exception exception) { ExportFailure(exception.Message); }
    }

    private void CloseExportBrowser()
    {
        _exportBrowserOpen.Update(false);
        _exportBrowser.children = Array.Empty<IWidget>();
    }

    private void ConfirmExport(string destination, string folder)
    {
        if (_exportTask != null || _exportOwner == null) return;
        if (!BridgeZipExport.IsValidFolderName(folder))
        {
            Mod.ShowMessage(UiStringCatalog.Current.Title, RuntimeUiText.Get("ExportFolderInvalid"));
            return;
        }
        var owner = _exportOwner;
        _exportOwner = null;
        _exportPath.Update("");
        try
        {
            if (BridgeAssetCatalog.Find(owner) == null) throw new IOException("Bridge not found.");
            if (!Path.IsPathRooted(destination)) throw new IOException("Choose an absolute export path.");
            var source = Path.Combine(BridgeAssetCatalog.Root, owner);
            _exportBusy.Update(true);
            _exportTask = Task.Run(() =>
            {
                try { return (true, BridgeZipExport.Write(source, destination, folder)); }
                catch (Exception exception) { return (false, exception.Message); }
            });
        }
        catch (Exception exception) { _exportBusy.Update(false); ExportFailure(exception.Message); }
    }

    private void PollExport()
    {
        if (_exportTask == null || !_exportTask.IsCompleted) return;
        var result = _exportTask.GetAwaiter().GetResult();
        _exportTask = null;
        _exportBusy.Update(false);
        if (result.Success) _exportSuccessPath.Update(result.Message);
        else ExportFailure(result.Message);
    }

    private void OpenExportDirectory()
    {
        var exportedPath = _exportSuccessPath.value;
        if (string.IsNullOrEmpty(exportedPath)) return;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = Path.GetDirectoryName(exportedPath)!, UseShellExecute = true });
            _exportSuccessPath.Update("");
        }
        catch (Exception exception)
        {
            Mod.Log.Warn("Could not open export directory: " + exception.Message);
            Mod.ShowMessage(UiStringCatalog.Current.Title, RuntimeUiText.Get("OpenExportDirectoryFailed"));
        }
    }

    private static void ExportFailure(string message)
    {
        Mod.Log.Warn("Bridge ZIP export failed: " + message);
        Mod.ShowMessage(UiStringCatalog.Current.Title, RuntimeUiText.Get("ExportFailed", message));
    }
}
