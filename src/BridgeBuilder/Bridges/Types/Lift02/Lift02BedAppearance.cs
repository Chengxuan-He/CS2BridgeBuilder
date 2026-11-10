using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// Original finite LOD2 atlas charts; BuildBedAppearance.py proves the affine fits.
internal static class Lift02BedAppearance
{
    static Lift02BedAppearance() { }
    private static HardcodedNumbers Data => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Lift02BedAppearance.Data", () => HardcodedNumbers.Load("Types_Lift02_BedAppearance"));
    internal static MovableRoadAppearance.Level Detailed(RenderPrefab native, Pose roadToObject, float halfDelta)
        => Prepared(BridgePreparedMaterials.RepeatUnit("Lift02", "1c6aac78ba66e66ebd3f5bb395a04d5d0f8223a6bf34f1edb74ef3b873929a48"), roadToObject, halfDelta);

    // Native resource graph is complete; filtering acceptance remains a separate gate.
    internal static MovableRoadAppearance.Level Prepared(MovableExistingRoadSurface.SurfaceResult surface, Pose roadToObject, float halfDelta)
    {
        var frame = Matrix4x4.TRS(roadToObject.position,roadToObject.rotation,Vector3.one).transpose;
        var offset = Data.I32[2];
        var halfWidth = Data.F32[offset+4] + halfDelta;
        if (!float.IsFinite(halfWidth)) throw new System.InvalidOperationException("Lift02 bed width is non-finite.");
        var min = new Vector2(0,Data.F32[offset+7]);
        var max = new Vector2(1,Data.F32[offset+8]);
        var stripWidth = Data.F32[offset+9];
        var inner = halfWidth - stripWidth;
        var patches = new Patch[3];
        var support = new MovableRoadAppearance.Type[patches.Length];
        var nativeTangent = Quaternion.Inverse(roadToObject.rotation) * Vector3.back;
        for (var side=0;side<2;side++)
        {
            var direction = side==0 ? 1f : -1f;
            // Continuous unit U retains the original physical period and phase.
            // The native texture repeats; geometry receives no period cuts.
            var u = frame * new Vector4(0,0,-(max.x-min.x)/Data.F32[offset+5],min.x+(max.x-min.x)*Data.F32[offset+6]);
            var v = frame * new Vector4(direction*(max.y-min.y)/stripWidth,0,0,min.y+halfWidth*(max.y-min.y)/stripWidth);
            patches[side] = new Patch(min,max,u,v,surface,preparedUnit:true,repeatU:true,repeatV:false,
                tangent:new Vector4(nativeTangent.x,nativeTangent.y,nativeTangent.z,-direction))
                .WithFiniteSampling(Vector2.zero,Vector2.one);
            support[side] = MovableRoadAppearance.Type.Ballast;
        }
        // A valid unmatched leaf preserves the central track and outside receivers.
        patches[2] = patches[0];
        var pose = Matrix4x4.TRS(roadToObject.position,roadToObject.rotation,Vector3.one);
        Partition Cut(float x, Partition negative, Partition positive)
        {
            var seam = Matrix4x4.identity; seam.m00 = 0; seam.m02 = -1;
            seam.m20 = 1; seam.m22 = 0; seam.m23 = -x;
            return new Partition(seam * pose,negative,positive);
        }
        var original = new Partition(2);
        var regions = Cut(-halfWidth,original,Cut(-inner,new Partition(0),
            Cut(inner,original,Cut(halfWidth,new Partition(1),original))));
        return new MovableRoadAppearance.Level(surface,patches,support,regions,regions);
    }
    internal static MovableRoadAppearance.Level Far(RenderPrefab native, Pose roadToObject, float halfDelta)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        var patches = new Patch[Data.I32[0]];
        var supported = new MovableRoadAppearance.Type[patches.Length];
        var nativeTangent = Quaternion.Inverse(roadToObject.rotation) * Vector3.left;
        var regions = Partition.Gap;
        Partition Cut(float z, Partition negative, Partition positive)
        {
            var seam = Matrix4x4.identity; seam.m23 = -z;
            return new Partition(seam * pose, negative, positive);
        }
        for (var i = patches.Length - 1; i >= 0; i--)
        {
            var offset = i * Data.I32[1];
            float F(int k) => Data.F32[offset+k];
            var width = F(2) + halfDelta;
            // A collapsed projection has no inverse; retain the original route.
            // Every nonzero width keeps its signed reciprocal without a clamp.
            var scale = width == 0 ? 1f : F(2) / width;
            if (!float.IsFinite(scale)) throw new System.InvalidOperationException("Lift02 bed projection is non-finite.");
            var u = pose.transpose * new Vector4(F(7)*scale, 0, F(8), F(9));
            var v = pose.transpose * new Vector4(F(10)*scale, 0, F(11), F(12));
            patches[i] = new Patch(new Vector2(F(3),F(4)),new Vector2(F(5),F(6)),u,v,native.GetSurfaceAsset(0),sourceUv:true,
                tangent:new Vector4(nativeTangent.x,nativeTangent.y,nativeTangent.z,-1));
            supported[i] = width == 0 ? MovableRoadAppearance.Type.None : MovableRoadAppearance.Type.Ballast;
            regions = Cut(F(0),Partition.Gap,Cut(F(1),new Partition(i),regions));
        }
        return new MovableRoadAppearance.Level(native.GetSurfaceAsset(0),patches,supported,regions,regions);
    }
}

// Embedded numerical arrays: Types_Lift02_BedAppearance
