using System.Linq;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;

// Compatibility belongs to the actual shader/vertex protocol, not the source CID
// or template number. Custom templates using these native shaders retain every
// authored property and layer through direct reuse. Other shaders need their own
// complete native renderer integration; a keyword alone cannot establish it.
internal static class MovableSurfaceAssets
{
    internal static bool IsObjectSurface(SurfaceAsset surface)
    {
        var library = AssetDatabase.global.resources.materialLibrary;
        var shader = library.GetMaterial(surface.materialTemplateHash).shader;
        return shader != null && (shader == library.GetMaterial(0).shader ||
            shader == library.GetMaterial(11).shader);
    }
    // Descriptor availability is not receiver compatibility. Only the common
    // material conversion entry may authorize binding to evaluated objects.
    internal static bool TryReadSource(SurfaceAsset source, out SurfaceAsset result, out string? error)
    {
        result=null!;error=null;
        try
        {
            source.LoadHeader();
            var template=AssetDatabase.global.resources.materialLibrary.GetMaterial(source.materialTemplateHash);
            if(template==null || template.shader==null)
            {error="Original Surface template/shader is unavailable: "+source.materialTemplateHash;return false;}
            try
            {
                source.LoadProperties(true);
                // Explicitly unbound optional slots use the original shader defaults.
                // Only a non-null unresolved native handle is a missing dependency.
                // VT author texture IDs can be provenance only. Their actual backing
                // graph remains validated by the existing forward dependency path.
                if(!source.isVTMaterial)
                foreach(var pair in source.textures)
                    if(pair.Value!=null && pair.Value.isDummy)
                    {error="Original Surface texture is unavailable: "+pair.Key;return false;}
            }
            finally {source.UnloadProperties();}
            result=source;return true;
        }
        catch(System.Exception exception)
        {result=null!;error="Original Surface reuse failed: "+exception.GetType().Name+": "+exception.Message;return false;}
    }
    internal static bool TryValidateObjectProtocol(SurfaceAsset surface, out string? error)
    {
        error = null;
        try
        {
            var template = AssetDatabase.global.resources.materialLibrary.GetMaterial(surface.materialTemplateHash);
            try
            {
                surface.LoadProperties(true);
                // These modes consume additional instance/vertex buffers. Native
                // procedural bones alone do not supply shape, curve or flow data.
                foreach (var keyword in new[] { "_GPU_ANIMATION_NORMAL", "_GPU_ANIMATION_SHAPE",
                    "COLOSSAL_GEOMETRY_TILING", "COLOSSAL_GEOMETRY_DILATED" })
                    if (template.IsKeywordEnabled(keyword) || surface.keywords.Contains(keyword))
                    { error = "Original object material needs additional native buffers: " + keyword; return false; }
                return true;
            }
            finally { surface.UnloadProperties(); }
        }
        catch (System.Exception exception) { error = "Object material protocol read failed: " + exception; return false; }
    }
}
