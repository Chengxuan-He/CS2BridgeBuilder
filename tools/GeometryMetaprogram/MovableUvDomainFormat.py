"""Exact measured UV-domain recipes inside ordinary, uncompressed BBN1 arrays.

Schema -3 I32: [-3, domains, faces, distinctBounds], followed by domain columns
A increments, B-A, C-A, signed face counts; rectangular-domain bounds indexes;
and increments across the complete ordered face stream. F32 holds distinct bounds
quadruples in first-occurrence order. Floating point values are never recomputed.
"""
import hashlib
import struct
import numpy as np


def _i32(values):
    values = np.asarray(values, dtype=np.int64)
    if np.any(values < -2147483648) or np.any(values > 2147483647):
        raise ValueError('UV-domain integer reconstruction overflow')
    return values.astype('<i4')


def decode_arrays(ints, floats):
    """Return legacy schema-2 I32/F32 arrays, preserving every bit and ordering."""
    v, f = np.asarray(ints, dtype='<i4'), np.asarray(floats, dtype='<f4')
    if len(v) < 2:
        raise ValueError('Truncated UV-domain recipe')
    if v[0] == -2:
        return v, f
    if v[0] != -3 or len(v) < 4:
        raise ValueError('Unsupported UV-domain recipe')
    n, face_count, bounds_count = map(int, v[1:4])
    if min(n, face_count, bounds_count) < 0 or len(v) < 4 + n*4:
        raise ValueError('Invalid UV-domain recipe counts')
    a, db, dc, counts = v[4:4+n*4].reshape(4, n)
    lengths = np.abs(counts.astype(np.int64))
    rectangular = int(np.count_nonzero(counts > 0))
    if np.any(lengths == 0) or int(lengths.sum()) != face_count:
        raise ValueError('Invalid UV-domain face counts')
    at = 4+n*4
    if len(v) != at+rectangular+face_count or len(f) != bounds_count*4:
        raise ValueError('Invalid UV-domain recipe lengths')
    indexes = v[at:at+rectangular]
    if np.any(indexes < 0) or np.any(indexes >= bounds_count):
        raise ValueError('Invalid UV-domain bounds index')
    anchors = _i32(np.cumsum(a, dtype=np.int64))
    b, c = _i32(anchors.astype(np.int64)+db), _i32(anchors.astype(np.int64)+dc)
    faces = _i32(np.cumsum(v[at+rectangular:], dtype=np.int64))
    legacy = np.empty(2+n*4+face_count, dtype='<i4');legacy[:2]=[-2,n]
    out, face_at = 2, 0
    for i, length in enumerate(lengths):
        length=int(length)
        legacy[out:out+4]=[anchors[i],b[i],c[i],counts[i]];out+=4
        legacy[out:out+length]=faces[face_at:face_at+length];out+=length;face_at+=length
    bounds = f.view('<u4').reshape(-1,4)[indexes].reshape(-1).copy().view('<f4')
    return legacy, bounds


def encode_arrays(ints, floats):
    """Convert measured legacy arrays to the fixed exact schema-3 recipe."""
    v, f = decode_arrays(ints, floats)
    n=int(v[1]);columns=[[],[],[],[]];faces=[];indexes=[];bounds=[];seen={}
    at, coordinate, previous = 2, 0, 0
    for _ in range(n):
        a,b,c,count=map(int,v[at:at+4]);at+=4
        if count == 0:raise ValueError('Empty UV domain')
        columns[0].append(a-previous);previous=a
        columns[1].append(b-a);columns[2].append(c-a);columns[3].append(count)
        faces.extend(v[at:at+abs(count)].tolist());at+=abs(count)
        if count>0:
            key=f[coordinate:coordinate+4].tobytes();coordinate+=4
            if len(key)!=16:raise ValueError('Truncated measured bounds')
            if key not in seen:seen[key]=len(bounds);bounds.append(key)
            indexes.append(seen[key])
    if at!=len(v) or coordinate!=len(f):raise ValueError('Invalid legacy domain lengths')
    deltas=np.diff(np.asarray([0,*faces],dtype=np.int64))
    encoded=_i32([-3,n,len(faces),len(bounds),*columns[0],*columns[1],*columns[2],*columns[3],*indexes,*deltas])
    unique=np.frombuffer(b''.join(bounds),dtype='<f4').copy()
    restored_i,restored_f=decode_arrays(encoded,unique)
    if restored_i.tobytes()!=v.tobytes() or restored_f.tobytes()!=f.tobytes():
        raise ValueError('UV-domain exact reconstruction failed')
    return encoded,unique


def read_bbn(raw):
    if len(raw)<64 or raw[:8]!=struct.pack('<II',0x314e4242,1) or hashlib.sha256(raw[:-32]).digest()!=raw[-32:]:
        raise ValueError('Invalid UV-domain BBN1 resource')
    counts=struct.unpack_from('<6i',raw,8)
    if min(counts)<0 or any(counts[i] for i in (1,2,3,5)) or len(raw)!=64+4*(counts[0]+counts[4]):
        raise ValueError('Invalid UV-domain BBN1 arrays')
    return np.frombuffer(raw,dtype='<i4',count=counts[0],offset=32),np.frombuffer(raw,dtype='<f4',count=counts[4],offset=32+counts[0]*4)


def write_bbn(ints, floats):
    ints,floats=np.asarray(ints,dtype='<i4'),np.asarray(floats,dtype='<f4')
    raw=struct.pack('<II6i',0x314e4242,1,len(ints),0,0,0,len(floats),0)+ints.tobytes()+floats.tobytes()
    return raw+hashlib.sha256(raw).digest()
