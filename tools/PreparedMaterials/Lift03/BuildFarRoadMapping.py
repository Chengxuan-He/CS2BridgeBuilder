"""Extract the four finite Lift03 far-road atlas traversals from original faces.

No texture is generated. The source faces already bound the fixed/moving regions;
this does not introduce cuts at texture periods or use prototype intersections.
"""
import hashlib
import json
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools/GeometryMetaprogram"))
from MovableUvDomainFormat import write_bbn
from ExportRecipes import numbers


def main():
    evidence = json.loads((ROOT / "artifacts/layer-decoupling/Lift03/outputs/native-repeat-mechanism/audit.json").read_text())
    source = ROOT / "artifacts/uv-density/native/LiftBridge03_LOD2 Mesh.npz"
    reference = next(r for r in evidence["reports"] if r["lod"] == 2 and r["deck"] == "Main")
    assert hashlib.sha256(source.read_bytes()).hexdigest() == reference["sourceNPZSHA256"]
    original = np.load(source)
    xyz, uv, faces = (original[k].astype(np.float64) for k in ("mesh0_positions", "mesh0_uv0", "mesh0_faces"))
    faces = faces.astype(np.int32)
    recorded = numbers(ROOT / "assets/HardcodedNumbers/Lift03/Types_Lift03_Lift03FarRoadTops_g_cs.bin")["I32"]
    selected = np.asarray(recorded[6:], dtype=np.int32).reshape(-1, 4)
    assert np.array_equal(faces[selected[:, 0]], selected[:, 1:])
    # Exact groups measured by Lift03FarRoadTops; ordered along the native Z axis.
    groups = ((598, 599, 600, 601), (642, 643, 644, 645),
              (238, 239, 240, 241), (150, 151, 152, 153))
    assert sorted(f for group in groups for f in group) == sorted(selected[:, 0])
    data, reports = [], []
    for group in groups:
        p, q = xyz[faces[list(group)]], uv[faces[list(group)]]
        xmin, xmax = p[:, :, 0].min(), p[:, :, 0].max()
        zmin, zmax = p[:, :, 2].min(), p[:, :, 2].max()
        assert xmin == -xmax
        umin, vmin = q.min(axis=(0, 1))
        umax, vmax = q.max(axis=(0, 1))
        du, dv = (umax-umin)/(xmax-xmin), (vmax-vmin)/(zmax-zmin)
        origin_u, origin_v = umin-du*xmin, vmin-dv*zmin
        exact = np.stack((p[:, :, 0]*du+origin_u, p[:, :, 2]*dv+origin_v), axis=-1)
        assert np.max(np.abs(exact-q)) < 1e-15, "Source contains a non-affine fold; do not fit it."
        values = np.asarray([xmax, zmin, zmax, umin, vmin, umax, vmax, du, dv, origin_u, origin_v], dtype=np.float32)
        rounded = np.stack((p[:, :, 0].astype(np.float32)*values[7]+values[9],
                            p[:, :, 2].astype(np.float32)*values[8]+values[10]), axis=-1)
        error = float(np.max(np.abs(rounded-q)))
        assert error <= np.finfo(np.float32).eps
        data.extend(values.tolist())
        reports.append(dict(faces=group, sourcePositionBounds=[xmin, xmax, zmin, zmax],
                            sourceUvBounds=[umin, vmin, umax, vmax],
                            sourceMapping="finite U and V; no wrap or local repetition",
                            float32MaxUvError=error, sourcePixels=512,
                            float32MaxPixelError=error*512))
    raw = write_bbn([1, len(groups)], data)
    target = ROOT / "assets/HardcodedNumbers/Lift03/Types_Lift03_Lift03FarRoadMapping_cs.bin"
    target.write_bytes(raw)
    out = ROOT / "artifacts/material-remap/Lift03"
    out.mkdir(parents=True, exist_ok=True)
    result = dict(sourceNpZ=str(source.relative_to(ROOT)), sourceNpZSha256=reference["sourceNPZSHA256"],
                  sourceSurface="5402741b305d5f85e66a54863f585b84", patches=reports,
                  resourceSha256=hashlib.sha256(raw).hexdigest(), resourceBytes=len(raw),
                  textureFilesCreated=0, originalTriangleCount=16,
                  scope="Exact original far mapping extraction, not rendered generated acceptance")
    (out / "far-road-mapping.json").write_text(json.dumps(result, indent=2)+"\n")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
