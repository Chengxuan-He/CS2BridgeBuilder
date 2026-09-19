using BridgeBuilder.Settings;

namespace BridgeBuilder.Runtime;

/// <summary>Latest selection only; unlike creation, preview changes are not a queue.</summary>
internal static class BridgePreviewState
{
    internal static int Revision { get; private set; }
    internal static int ResultRevision { get; private set; }
    internal static BridgeRuntimeRequest? Selection { get; private set; }
    internal static string Key { get; private set; } = string.Empty;
    internal static string Image { get; private set; } = string.Empty;
    private static string _statusKey = string.Empty;
    internal static string Status => RuntimeUiText.Get(_statusKey);
    internal static bool Loading => Selection != null &&
        (_statusKey == "PreviewBuilding" || _statusKey == "PreviewDisplaying");

    // UI bindings and the owning GameSystem both run on Unity's main thread.
    internal static void Select(string upper, string lower, string style, bool opposite = true)
    {
        Selection = new BridgeRuntimeRequest { UpperDeckId = upper, LowerDeckId = lower, StyleId = style,
            LowerDeckOpposite = opposite };
        Key = upper + "\n" + lower + "\n" + style + "\n" + (opposite ? "opposite" : "same");
        BeginSelection();
    }

    internal static void SelectExisting(string prefabName)
    {
        Selection = new BridgeRuntimeRequest { PrefabName = prefabName };
        // Keep existing asset identity separate from a new-bridge recipe, even
        // when both have exactly the same style and selected road networks.
        Key = "existing\n" + prefabName;
        BeginSelection("PreviewDisplaying");
    }

    private static void BeginSelection(string statusKey = "PreviewBuilding")
    {
        Revision++;
        Image = string.Empty;
        _statusKey = statusKey;
        ResultRevision++;
    }

    internal static void Clear()
    {
        Selection = null;
        Key = Image = _statusKey = string.Empty;
        Revision++;
        ResultRevision++;
    }

    internal static void Publish(int revision, string image, string statusKey)
    {
        if (revision != Revision || Selection == null) return;
        Image = image;
        _statusKey = statusKey;
        ResultRevision++;
    }
}
