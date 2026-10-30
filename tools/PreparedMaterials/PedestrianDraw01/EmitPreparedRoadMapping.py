"""Bind completed native half-width units to original appearance partitions."""
import argparse
import hashlib
import json
import struct
from pathlib import Path


SOURCE='''using System;
using BridgeBuilder.Runtime;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Embedded numerical arrays: Types_PedestrianDraw01_PreparedRoadMapping
// Native x=0 UV boundaries remain distinct. Each finished half-width unit is
// shared by both LODs; its finite U guard and longitudinal repeat are independent.
internal static class PedestrianDraw01PreparedRoadMapping
{
    private static readonly string[] Keys = {
KEYS
    };

    internal static Patch[] Patches(Pose roadToObject, float halfWidthDelta)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var values = Data.Values.F32;
        var result = new Patch[Keys.Length];
        for (var index = 0; index < result.Length; index++)
        {
            var at = index * 9;
            var halfWidth = values[at] + halfWidthDelta;
            if (!(halfWidth > 0))
                throw new InvalidOperationException("PedestrianDraw01 walking unit has invalid finite width.");
            var u = new Vector4(values[at + 1] / halfWidth, 0, 0, values[at + 2]);
            var v = new Vector4(0, 0, values[at + 3], values[at + 4]);
            var tangent = Quaternion.Inverse(roadToObject.rotation) * new Vector3(values[at + 5], values[at + 6], values[at + 7]);
            result[index] = new Patch(Vector2.zero, Vector2.one, frame * u, frame * v,
                surface: BridgePreparedMaterials.RepeatUnit("PedestrianDraw01", Keys[index]),
                preparedUnit: true, repeatU: false, repeatV: true,
                tangent: new Vector4(tangent.x, tangent.y, tangent.z, values[at + 8]));
        }
        return result;
    }

    internal static Partition Regions(Partition appearance, Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        var transverse = Matrix4x4.identity;
        transverse.m00 = 0;
        transverse.m02 = -1;
        transverse.m20 = 1;
        transverse.m22 = 0;
        Partition Convert(Partition node)
        {
            if (node.IsGap) return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam, Convert(node.Negative), Convert(node.Positive));
            return new Partition(transverse * pose, new Partition(node.Bone * 2), new Partition(node.Bone * 2 + 1));
        }
        return Convert(appearance);
    }

    private static class Data
    {
        static Data()
        {
            if (Values.I32.Length != 2 || Values.I32[0] != 1 || Values.I32[1] != Keys.Length || Values.F32.Length != Keys.Length * 9)
                throw new InvalidOperationException("Invalid PedestrianDraw01 prepared walking mapping schema.");
        }
        internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw01_PreparedRoadMapping");
    }
}
'''


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('repository',type=Path);p.add_argument('stage',type=Path)
    a=p.parse_args()
    bindings=json.loads((a.stage/'bindings.json').read_text())['appearanceRegions']
    products={u['id']:u for u in json.loads((a.stage/'report.json').read_text())['units']}
    units={u['id']:u for u in json.loads((a.stage/'manifest.json').read_text())['units']}
    assert len(bindings)==8 and len(products)==3
    keys=[products[b['unit']]['key'] for b in bindings]
    for b in bindings:
        channels=products[b['unit']]['channels'];low=channels[0]['contentMin'];high=channels[0]['contentMax']
        assert all(c['contentMin']==low and c['contentMax']==high for c in channels)
        assert low[1]==0 and high[1]==1
        b['unitUScaleNumerator']=-(high[0]-low[0])
        b['unitUOffset']=low[0] if b['side']==0 else high[0]
    values=[v for b in bindings for v in [b['originalHalfWidth'],b['unitUScaleNumerator'],b['unitUOffset'],b['unitVScale'],b['unitVOffset'],*units[b['unit']]['normalFrameEvidence']['targetTangent']]]
    raw=b'BBN1'+struct.pack('<7i',1,2,0,0,0,len(values),0)+struct.pack('<2i',1,len(keys))+struct.pack('<'+'f'*len(values),*values)
    raw+=hashlib.sha256(raw).digest()
    (a.repository/'assets/HardcodedNumbers/PedestrianDraw01/Types_PedestrianDraw01_PreparedRoadMapping.bin').write_bytes(raw)
    code=SOURCE.replace('KEYS','\n'.join('        "'+key+'",' for key in keys))
    (a.repository/'src/BridgeBuilder/Bridges/Types/PedestrianDraw01/PedestrianDraw01PreparedRoadMapping.cs').write_text(code,encoding='utf-8')
    print('PASS: eight native prepared-unit bindings emitted.')


if __name__=='__main__':main()
