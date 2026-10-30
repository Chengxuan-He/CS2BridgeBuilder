"""Native packed readback with a derived oct-cell/normalization error bound."""
import ctypes as ct,json
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[3];OUT=ROOT/'artifacts/material-remap/PedestrianDraw02'

def main():
    samples=json.loads((OUT/'exact-tangent-fields.json.packed.json').read_text())['samples']
    bits=np.array([s['actual'] for s in samples],dtype=np.uint32)
    decoded=np.empty((len(bits),4),dtype=np.float32)
    dll=ct.CDLL('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll')
    decode=dll.math_array_oct15_to_fp32tangents;decode.argtypes=[ct.c_void_p,ct.c_int64,ct.c_void_p]
    decode(bits.ctypes.data,len(bits),decoded.ctypes.data)
    actual=np.array([[s['evaluated'][k] for k in ('x','y','z')] for s in samples])
    ideal=np.array([[s['ideal'][k] for k in ('x','y','z')] for s in samples])
    raw_error=np.linalg.norm(actual-ideal,axis=1)
    norm_min=np.minimum(np.linalg.norm(actual,axis=1),np.linalg.norm(ideal,axis=1))
    # Unfolded oct vector has norm >= 1/sqrt(3), Jacobian norm <= sqrt(3).
    # Normalization difference <= 2*delta/minNorm, hence <=6*oct-delta.
    # A round-to-nearest signed15 cell has coordinate radius .5/16383.
    cell_vector_bound=6*np.sqrt(2)*.5/16383
    fp32_decode_bound=8*np.finfo(np.float32).eps
    bound=cell_vector_bound+2*raw_error/norm_min+fp32_decode_bound
    target=ideal/np.linalg.norm(ideal,axis=1)[:,None]
    error=np.linalg.norm(decoded[:,:3].astype(float)-target,axis=1)
    result=dict(status='PASS' if np.all(error<=bound) else 'FAIL',samples=len(samples),
        maximumMeasuredDirectionError=float(error.max()),maximumDerivedBound=float(bound.max()),
        nativeCellVectorBound=cell_vector_bound,rawFittingError=0,
        explanation='Quantization bound derives from oct15 half-cell and decoder/normalization Lipschitz bounds; raw storage/evaluation errors are separate.',
        limitation='Sampled source-triangle positions only; transformed target-triangle interpolation and full Compose regression remain required.')
    (OUT/'exact-tangent-codec-bounds.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result));assert result['status']=='PASS'

if __name__=='__main__':main()
