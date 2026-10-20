"""Export measured Lift03 caches and the current production Plan for structural checks.

Usage: python tools/Lift03StructureCheck/ExportInputs.py RESEARCH_DIRECTORY
Requires NumPy and the reviewed native-decoding caches. Geometry source hashes
are verified; cache provenance still depends on the native decoding audit.
"""
from pathlib import Path
import hashlib
import sys
import numpy as np

root = Path(sys.argv[1])
project = Path(__file__).resolve().parent
hashes = (
    "f8bc2882fcba043d1d57a17fd27a43631b492ab4292f8fc6b9c3beeb00af53cd",
    "455d25f8ec83f91ea097551fb09a9f41abc79e9f15e836b19c0a527ad50ff9fa",
)
for lod, vertices in enumerate((474534, 349267)):
    name = "LiftBridge03" + ("_LOD1" if lod else "") + " Mesh.Geometry"
    assert hashlib.sha256((root / "movable-skin" / name).read_bytes()).hexdigest() == hashes[lod]
    data = np.load(root / f"lift03-main-{lod}-0.npz")
    positions, indices = data["positions"], data["indices"]
    assert positions.shape == (vertices, 3)
    assert indices.ndim == 2 and indices.shape[1] == 3
    assert indices.min() >= 0 and indices.max() < vertices
    assert np.isfinite(positions).all()
    # Independently reviewed pole assemblies, contact wires and droppers.
    ranges = ((701,707),(1414,1420),(2115,2121),(2815,2821),
              (3803,3806),(3982,4009),(4980,4981),(5152,5179),
              (6154,6157),(6326,6353),(7323,7324),(7490,7573))
    parts = [part for first,last in ranges for part in range(first,last+1)]
    assert len(parts) == 208
    selected = np.isin(data["component"], parts)
    assert np.array_equal(selected[indices].any(1), selected[indices].all(1))
    faces = np.flatnonzero(selected[indices].all(1))
    assert len(faces) == (13832,4921)[lod]
    np.savetxt(root / f"lift03-catenary-faces-{lod}.txt", faces, fmt="%d")
    with (root / f"lift03-widthcheck-{lod}.bin").open("wb") as output:
        np.array([vertices, indices.size], dtype="<i4").tofile(output)
        positions.astype("<f4").tofile(output)
        indices.astype("<i4").tofile(output)
    if lod == 1:
        components = data["component"]
        selected = np.flatnonzero(np.isin(components, (65, 66, 778, 779, 2826, 2827)))
        assert len(selected) == 2962
        with (root / "lift03-deck-ownership.txt").open("w", encoding="utf-8") as output:
            for vertex in selected:
                upper = components[vertex] in (65, 778, 2826)
                output.write(f"{vertex} {int(upper)} {int(np.sign(positions[vertex, 0]))}\n")

# Exercise the current production constructor, not a simplified Plan substitute.
# The runner also checks this snapshot so a later source edit cannot go unnoticed.
source = (project / "../../src/BridgeBuilder/Bridges/Common/Movable/MovableStructureModel.cs").read_text()
body = source[source.index("internal static class MovableStructureModel"):
              source.index("    internal static bool TryBuild")]
model = ("#nullable enable\nusing System;\nusing System.Collections.Generic;\nusing System.Linq;\n"
         "using UnityEngine;\nnamespace BridgeBuilder.Bridges;\n" + body + "}\n")
(project / "Model.generated.cs").write_text(model, encoding="utf-8")
print("PASS source hashes, two input meshes and 2962 independently classified base vertices")
