"""Bound the actual uploaded target-triangle tangent interpolant everywhere."""
import ctypes as ct,json
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3];OUT=ROOT/'artifacts/material-remap/PedestrianDraw02'

def main():
    triangles=json.loads((OUT/'continuous-17-target-failures.json.tangent.json').read_text())['triangles']
    bits=np.array([v for t in triangles for v in t['packed']],dtype=np.uint32)
    decoded=np.empty((len(bits),4),dtype=np.float32)
    dll=ct.CDLL('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll')
    fn=dll.math_array_oct15_to_fp32tangents;fn.argtypes=[ct.c_void_p,ct.c_int64,ct.c_void_p]
    fn(bits.ctypes.data,len(bits),decoded.ctypes.data)
    rows=[];cell=6*np.sqrt(2)*.5/16383;decode_rounding=8*np.finfo(np.float32).eps
    for index,t in enumerate(triangles):
        raw=np.array(t['raw']).reshape(3,3);native=decoded[index*3:index*3+3,:3].astype(float)
        # Normalizing each evaluated vertex before native interpolation changes
        # its raw magnitude. Keep this separate from oct15 quantization.
        normalization=float(np.max(abs(np.linalg.norm(raw,axis=1)-1)))
        measured=float(np.linalg.norm(native-raw,axis=1).max())
        derived=cell+decode_rounding+normalization
        assert measured<=derived
        # For every barycentric weight b>=0, sum(b)=1:
        # |sum b*(native-raw)| <= sum b*|native-raw| <= max vertex error.
        rows.append(dict(delta=t['delta'],triangle=index,maxVertexDifference=measured,
            allInteriorDifferenceBound=measured,codecDerivedBound=derived,
            vertexNormalizationContribution=normalization))
    result=dict(status='PASS',triangles=len(rows),rows=rows,
        maxInteriorBound=max(r['allInteriorDifferenceBound'] for r in rows),
        scope='Actual uploaded target native tangent to actual raw affine triangle interpolant; convex bound holds everywhere inside each tested triangle.',
        limits=['This bound excludes additional source coefficient/position Float32 errors, recorded separately.',
            'No native fragment PBR appearance or non-affine domain equivalence is claimed.'])
    (OUT/'target-tangent-interpolation-bound.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(dict(status=result['status'],triangles=len(rows),maxInteriorBound=result['maxInteriorBound'])))

if __name__=='__main__':main()
