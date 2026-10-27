// Offline tests use real translation tables without a loaded game world.
namespace Colossal.Localization
{
    internal sealed class LocalizationManager { internal string activeLocaleId = "en-US"; }
}
namespace BridgeBuilder.Settings
{
    internal sealed class BridgeSetting
    {
        public void TogglePanel() { }
        public bool RemoveDevelopmentRestrictions { get; set; }
        public string RecoveryCopyLocation { get; set; } = "";
        public void OpenRecoveryCopies() { }
        public void BridgeSelfCheck() { }
        public void RemoveAllBridges() { }
        public string AllowGameplayExport = "", ArmRemoval = "", BridgeName = "", BridgeStyleId = "",
            BuildStyleOverride = "", DeckSpacing = "", EmbedIcons = "", ExportSelected = "", LowerDeckId = "",
            LowerDeckOpposite = "", OverwriteExisting = "", RemoveSelected = "", RemoveUnusedDependencies = "",
            RescanRoads = "", StatusText = "", UpperDeckId = "";
    }
}
namespace Game.SceneFlow
{
    internal sealed class GameManager
    {
        internal static GameManager? instance;
        internal Colossal.Localization.LocalizationManager? localizationManager;
    }
}
