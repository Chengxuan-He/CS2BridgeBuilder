"""Stage a private Unity check of actual runtime domain mapping against original faces."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
target=ROOT/'artifacts/material-remap/environments/Lift01/unity/Assets/Editor'
for name in ('Lift01HalfFloorAppearance','Lift01RoadPartitions'):
    code=(ROOT/'src/BridgeBuilder/Bridges/Types/Lift01'/(name+'.cs')).read_text()
    (target/(name+'.cs')).write_text(code.replace('namespace BridgeBuilder.Bridges;','namespace BridgeBuilder.Bridges {')+'\n}')
code=(ROOT/'src/BridgeBuilder/Bridges/Common/Movable/MovableExistingRoadSurface.cs').read_text()
(target/'MovableExistingRoadSurface.cs').write_text(code.replace('namespace BridgeBuilder.Bridges;','namespace BridgeBuilder.Bridges {')+'\n}')
(target/'Lift01HalfFloorDomainCheck.cs').write_text((ROOT/'tools/PreparedMaterials/Lift01/CheckHalfFloorDomains.Editor.cs').read_text())
(target/'Lift01DomainCheckStubs.cs').write_text('''using System;using System.IO;using System.Linq;using UnityEngine;
namespace Colossal.IO.AssetDatabase { public sealed class SurfaceAsset {} }
namespace BridgeBuilder.Bridges {
internal sealed class HardcodedNumbers {
 internal static string Root;internal int[] I32;internal float[] F32;
 internal static HardcodedNumbers Load(string name) {
 var path=Directory.GetFiles(Path.Combine(Root,"assets/HardcodedNumbers/Lift01"),name+".bin").Single();
 using var r=new BinaryReader(File.OpenRead(path));if(r.ReadUInt32()!=0x314e4242||r.ReadInt32()!=1)throw new Exception("BBN");
 var c=new int[6];for(int i=0;i<6;i++)c[i]=r.ReadInt32();var d=new HardcodedNumbers();d.I32=new int[c[0]];
 for(int i=0;i<c[0];i++)d.I32[i]=r.ReadInt32();r.BaseStream.Position+=c[1]*4+c[2]*8+c[3]*8;
 d.F32=new float[c[4]];for(int i=0;i<c[4];i++)d.F32[i]=r.ReadSingle();return d;
 }
}
internal static class MovableRoadGeometry {
 internal sealed class Partition {internal static readonly Partition Gap=new Partition();internal Matrix4x4 ObjectToSeam;internal Partition Negative,Positive;internal int Bone;internal bool IsGap;
 private Partition(){IsGap=true;}internal Partition(int bone){Bone=bone;}internal Partition(Matrix4x4 m,Partition n,Partition p){ObjectToSeam=m;Negative=n;Positive=p;}}
}
internal static class MovableRoadAppearance {
 [Flags] internal enum Type { None=0,Ballast=16 }
 internal sealed class Level {internal MovableExistingRoadSurface.Patch[] Patches;internal Type[] SupportedTypes;internal MovableRoadGeometry.Partition Carriageway;
 internal Level(Colossal.IO.AssetDatabase.SurfaceAsset native,MovableExistingRoadSurface.Patch[] patches,Type[] supportedTypes,MovableRoadGeometry.Partition carriageway=null,MovableRoadGeometry.Partition sidewalk=null){Patches=patches;SupportedTypes=supportedTypes;Carriageway=carriageway;}}
}
}''')
frames=json.loads((ROOT/'artifacts/material-remap/Lift01/floor-native-frames.json').read_text())
audit=json.loads((ROOT/'artifacts/layer-decoupling/Lift01/outputs/native-repeat-mechanism/audit.json').read_text())
bones={(r['lod'],f['face']):f['bones'][0] for r in audit['reports'][:2] for f in r['sourceFaces']}
rows=[]
for f in frames['faces']:
    if f['lod']>=2:continue
    vec=lambda p,keys:{k:sum(q[i] for q in p)/len(p) for i,k in enumerate(keys)}
    rows.append(dict(lod=f['lod'],bone=bones[f['lod'],f['face']],position=vec(f['positions'],'xyz'),uv=vec(f['uv'],'xy'),tangent=vec(f['tangents'],'xyzw')))
(ROOT/'artifacts/material-remap/Lift01/half-floor-domain-fixture.json').write_text(json.dumps(dict(rows=rows)))
print(target)
