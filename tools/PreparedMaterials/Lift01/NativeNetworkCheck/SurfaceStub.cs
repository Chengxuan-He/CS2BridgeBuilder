// This harness checks identity dispatch and rejection of network-context leakage.
// Header values come from separately verified original Surface bytes below.
// It does not invoke native loading or establish renderer behavior.
namespace Colossal.IO.AssetDatabase;
internal sealed class SurfaceAsset
{
    internal sealed class Identifier
    {
        internal string guid = "";
        public override string ToString() => "stub://" + guid;
    }
    internal Identifier id = new();
    internal bool isBuiltin = false;
    internal int materialTemplateHash = 0;
    internal bool isVTMaterial = false;
    internal void LoadHeader() { }
}
