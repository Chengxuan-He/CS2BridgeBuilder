"""Measure native far mapping and quantization; never invent a repeat or threshold."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import zipfile
import numpy as np


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[3]
    evidence = repo / 'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism'
    prior = json.loads((evidence / 'lod2-analysis.json').read_text())
    decoder = runpy.run_path(str(evidence / 'audit.py'))['decode']
    archive_path = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content/BridgesAndPorts/Blob.cok')
    with zipfile.ZipFile(archive_path) as archive:
        raw = archive.read(prior['archiveEntry'])
    assert hashlib.sha256(raw).hexdigest() == prior['sha256']
    mesh = decoder(raw)[0]
    retained = np.load(evidence / 'lod2-native.npz')
    for key, value in mesh.items():
        assert np.array_equal(value, retained['mesh0_' + key])
    spans = json.loads((evidence / 'far-spans.json').read_text())
    positions, uvs, faces = (mesh[key] for key in ('positions', 'uv', 'indices'))
    results = []
    for group in spans['groups']:
        groups = [('complete-domain', group['faces'])]
        if group['bone']:
            # These are actual authored longitudinal panel boundaries. Report
            # them separately, without cutting any selected/generated geometry.
            stations = {}
            for face in group['faces']:
                points = positions[faces[face]]
                key = (float(points[:, 2].min()), float(points[:, 2].max()))
                stations.setdefault(key, []).append(face)
            groups.extend((str(stations_range), ids) for stations_range, ids in stations.items())
        for label, selected in groups:
            ids = np.unique(faces[selected])
            p = positions[ids].astype(np.float64)
            uv = uvs[ids].astype(np.float64)
            xz1 = np.column_stack((p[:, 0], p[:, 2], np.ones(len(p))))
            coefficients = np.linalg.lstsq(xz1, uv, rcond=None)[0]
            errors = xz1 @ coefficients - uv
            triangle_gradients = []
            for face in selected:
                triangle = positions[faces[face]].astype(np.float64)
                basis = np.column_stack((triangle[:, 0], triangle[:, 2], np.ones(3)))
                exact = np.linalg.solve(basis, uvs[faces[face]].astype(np.float64))
                # Compare gradients over the entire authored triangle, not a
                # sampling grid. An affine residual attains its extrema on
                # these vertices, so this also bounds every interior point.
                triangle_gradients.append(exact[:2])
            triangle_gradients = np.asarray(triangle_gradients)
            gradient_difference = triangle_gradients - coefficients[:2]
            span = np.ptp(p[:, (0, 2)], axis=0)
            gradient_drift = np.abs(gradient_difference) * span[:, None] * 512
            half = uv.astype(np.float16)
            previous = np.nextafter(half, np.float16(-np.inf)).astype(np.float64)
            following = np.nextafter(half, np.float16(np.inf)).astype(np.float64)
            lower, upper = (uv + previous) * .5, (uv + following) * .5
            fits_storage_intervals = np.all((xz1 @ coefficients >= lower) & (xz1 @ coefficients <= upper))
            # A storage interval is evidence about the original vertex encoding,
            # not authorization to replace its decoded authored values.
            row = dict(bone=group['bone'], side=group['side'], end=group['end'],
                domain=label, faces=selected, vertices=ids.tolist(),
                sourceUV=uv.tolist(), fittedUV=(xz1 @ coefficients).tolist(),
                affineXZ1=coefficients.tolist(),
                maximumBaseMipPixelError=np.max(np.abs(errors), axis=0).tolist(),
                fitsOriginalHalfStorageIntervals=bool(fits_storage_intervals),
                originalTriangleGradientsXZ=triangle_gradients.tolist(),
                maximumGradientErrorPixelsPerMetre=np.max(np.abs(gradient_difference), axis=0).tolist(),
                maximumGradientDriftOverDomainPixels=np.max(gradient_drift, axis=0).tolist(),
                farEndpointMaximumPixelError=np.max(np.abs(errors[np.isclose(np.abs(p[:,2]), np.max(np.abs(p[:,2])), rtol=0, atol=1e-7)]),axis=0).tolist(),
                originalHalfStorageIntervalPixels=np.stack((uv-lower, upper-uv),axis=2).tolist(),
                mipMaximumPixelErrors=[(np.max(np.abs(errors),axis=0)*(512 >> mip)).tolist() for mip in range(10)])
            row['maximumBaseMipPixelError'] = (np.max(np.abs(errors),axis=0)*512).tolist()
            row['maximumGradientErrorPixelsPerMetre'] = (np.max(np.abs(gradient_difference),axis=0)*512).tolist()
            row['farEndpointMaximumPixelError'] = (np.asarray(row['farEndpointMaximumPixelError'])*512).tolist()
            row['originalHalfStorageIntervalPixels'] = (np.stack((uv-lower,upper-uv),axis=2)*512).tolist()
            if group['bone'] == 0:
                # Native cap coordinates suggest independent transverse U and
                # longitudinal V. Test that hypothesis without borrowing the
                # general fit's small cross-axis terms.
                axis_rows = []
                for axis, channel in ((0, 0), (2, 1)):
                    basis = np.column_stack((p[:,axis], np.ones(len(p))))
                    gradient_offset = np.linalg.lstsq(basis, uv[:,channel], rcond=None)[0]
                    mapped = basis @ gradient_offset
                    axis_rows.append(dict(positionAxis=axis, uvChannel=channel,
                        gradientOffset=gradient_offset.tolist(),
                        maximumBaseMipPixelError=float(np.max(np.abs(mapped-uv[:,channel]))*512),
                        fitsOriginalHalfStorageIntervals=bool(np.all(
                            (mapped>=lower[:,channel]) & (mapped<=upper[:,channel])))))
                row['independentCapAxesHypothesis'] = axis_rows
            results.append(row)
    result = dict(schema='BridgeBuilder/Draw02FarMappingAudit/1', sourceGeometrySHA256=prior['sha256'],
        archiveEntry=prior['archiveEntry'], surface='436d1828c36d1336cf115c85def2c62c',
        textureSize=512, results=results,
        limits=['An affine fit is not an accepted replacement for decoded authored UVs.',
                'Mip-coordinate bounds do not measure anisotropic texture filtering or material appearance.',
                'No period, runtime mapping, subdivision or source pixel mutation is authorized by this audit.'])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    for row in results:
        print(row['bone'],row['side'],row['end'],row['domain'],row['maximumBaseMipPixelError'],row['fitsOriginalHalfStorageIntervals'])


if __name__ == '__main__':
    main()
