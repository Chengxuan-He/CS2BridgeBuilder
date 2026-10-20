"""Emit the native Lift02 deck crossmembers, retaining original per-face bones and all LODs.
Usage: python Lift02DeckCrossMembers.py RESEARCH_DIRECTORY OUTPUT_CS
"""
from pathlib import Path
import ctypes as c
import hashlib,json,struct,sys
import numpy as np
r,output=map(Path,sys.argv[1:])
audit=json.loads((r/'all-movable-audit.json').read_text())
# Reuse only native binary decoding; membership and spans below are Lift02 evidence.
decoder=(Path(__file__).with_name('Lift05Counterweights.py')).read_text()
exec(decoder[decoder.index('dll = c.CDLL'):decoder.index('tables=[]')])
levels=[]
components_by_lod=((4206, 4207, 4212, 4213, 4218, 4219, 4222, 4223, 4228, 4229, 4234, 4235, 4240, 4241, 4246, 4247, 4252, 4253, 4258, 4259, 4264, 4265, 4270, 4271, 4276, 4277, 4282, 4283, 4288, 4289, 4294, 4295, 4300, 4301, 4306, 4307, 4312, 4313, 4318, 4319, 4324, 4325, 4330, 4331, 4336, 4337, 4342, 4343, 4348, 4349, 4354, 4355, 4510), (4175, 4176, 4181, 4182, 4187, 4188, 4191, 4192, 4197, 4198, 4203, 4204, 4209, 4210, 4215, 4216, 4221, 4222, 4227, 4228, 4233, 4234, 4239, 4240, 4245, 4246, 4251, 4252, 4257, 4258, 4263, 4264, 4269, 4270, 4275, 4276, 4281, 4282, 4287, 4288, 4293, 4294, 4299, 4300, 4305, 4306, 4311, 4312, 4317, 4318, 4323, 4324, 4479), ())
expected_bones=(((('bone_0'),), (('bone_0'),), (('bone_0'),), (('bone_0'),), (('bone_0'),), (('bone_0'), ('bone_1')), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_0'), ('bone_1')), (('bone_0'),), (('bone_0'),), (('bone_0'),), (('bone_0'),), (('bone_0'),), (('bone_1'),)), ((('bone_0'),), (('bone_0'),), (('bone_0'),), (('bone_0'),), (('bone_0'),), (('bone_0'), ('bone_1')), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_1'),), (('bone_0'), ('bone_1')), (('bone_0'),), (('bone_0'),), (('bone_0'),), (('bone_0'),), (('bone_0'),), (('bone_1'),)), ())
for lod,components in enumerate(components_by_lod):
 name='LiftBridge02'+(f'_LOD{lod}' if lod else '')+' Mesh'
 raw=(r/'movable-skin'/(name+'.Geometry')).read_bytes();digest=hashlib.sha256(raw).hexdigest()
 assert any(a.get('sha256')==digest for a in audit['assets'].values())
 version,meshes,flags=struct.unpack_from('<HII',raw)
 assert version==1 and meshes==(1 if lod==2 else 2)
 headers=[struct.unpack_from('<QI14i5I',raw,10+i*88) for i in range(meshes)];h=headers[0]
 section=decoder[decoder.index('    def decode('):decoder.index('    cached=np.load')]
 exec('\n'.join(line[4:] if line.startswith('    ') else line for line in section.splitlines()))
 cached=np.load(r/f'lift02-main-{lod}-0.npz');assert np.array_equal(f,cached['indices'])
 assert np.allclose(v,cached['positions'],atol=1e-6,rtol=1e-7)
 bones=[];bone=''
 for line in (r/'movable-skin'/(name+'.mesh-0.obj')).read_text().splitlines():
  if line.startswith('g '):bone=line[2:]
  elif line.startswith('f '):bones.append(bone)
 weights=np.zeros(len(v),dtype=np.float32);selected=np.isin(cached['component'],components)
 assert selected.sum()==(1728,1522,0)[lod]
 for group in components:
  mask=cached['component']==group;ids=np.flatnonzero(mask);x=v[ids,0].astype(float)
  touched=mask[f].any(1);assert np.all(mask[f[touched]]) and set(np.array(bones)[touched])==set(expected_bones[lod][components.index(group)])
  left,right=-x.min(),x.max();assert left==3.96502685546875 and right==3.96502685546875
  weights[ids]=np.where(x<0,x/left,x/right)
 sf=f[selected[f].all(1)];p=v[sf].astype(float);n=np.cross(p[:,1]-p[:,0],p[:,2]-p[:,0]);active=np.linalg.norm(n,axis=1)>1e-9
 for delta in (-4.99,-4,-3,-.5,0,2,8):
  moved=v.astype(float).copy();moved[:,0]+=weights.astype(float)*delta
  assert np.array_equal(moved[~selected],v[~selected]) and np.array_equal(moved[:,1:],v[:,1:])
  q=moved[sf];after=np.cross(q[:,1]-q[:,0],q[:,2]-q[:,0]);assert np.isfinite(moved).all()
  if delta >= -.5: assert np.all((n[active]*after[active]).sum(1)>0)
  else: print(f"LOD{lod} delta={delta}: {np.count_nonzero((n[active]*after[active]).sum(1)<=0)} collapsed/reversed faces retained by signed mapping")
 levels.append((len(v),digest,[(int(i),float(weights[i])) for i in np.flatnonzero(selected)]))
 print(f'LOD{lod}: {selected.sum()} vertices, {len(sf)} unchanged triangles, original per-face bones; seven-width signed mapping and exclusion passed')
lines=['// Native LiftBridge02 deck crossmembers, bone 1; game 1.6.2f1.', '// Generated by Lift02DeckCrossMembers.py from exact decoded native positions.', 'using System;', 'using UnityEngine;', '', 'namespace BridgeBuilder.Bridges;', '', 'internal static class Lift02DeckCrossMemberGeometry', '{', '    internal static void Apply(int lod, Vector3[] source, Vector3[] positions, float delta)', '    {', '        var data = lod switch', '        {']
for lod,(count,_,_) in enumerate(levels):lines.append(f'            {lod} => (Count: {count}, Vertices: Vertices{lod}),')
lines+=['            _ => throw new ArgumentOutOfRangeException(nameof(lod))', '        };', '        if (source.Length != data.Count || positions.Length != data.Count || !float.IsFinite(delta))', '            throw new InvalidOperationException("Lift02 deck crossmembers require their native layout.");', '        foreach (var vertex in data.Vertices)', '            positions[vertex.Index].x = source[vertex.Index].x + vertex.Weight * delta;', '    }', '']
for lod,(_,digest,weights) in enumerate(levels):
 lines+=['    // Geometry SHA-256: '+digest,f'    private static readonly (int Index, float Weight)[] Vertices{lod} =', '    {']
 lines += [f'        ({i}, {w:.9g}f),' for i,w in weights]
 lines+=['    };','']
lines+=['}',''];output.write_text('\n'.join(lines),encoding='utf-8')

