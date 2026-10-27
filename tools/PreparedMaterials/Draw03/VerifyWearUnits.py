"""Verify measured Draw03 atomic wear sampling without modifying runtime data.

The layouts below are arithmetic probes, not generated game assets. They establish
which band-local coordinate maps can reuse an immutable unit without receiver cuts.
They do not establish coverage of real road meshes or native visual acceptance.
"""
import argparse
import json
from pathlib import Path
from PrepareRecipes import numbers


def bands(values, centers, span):
    period = values[0]
    lane_min, lane_max = values[3] / values[4], values[7] / values[8]
    crop = values[9] / values[10]
    gap_min = lane_min - crop * values[11]
    left, right = min(centers[0] - period, span[0]), max(centers[-1] + period, span[1])
    result, cursor = [], left

    def gap(end):
        nonlocal cursor
        if end > cursor:
            result.append(dict(kind="BetweenLaneWear", start=cursor, end=end,
                               low=gap_min, high=gap_min + crop,
                               repeat=(end-cursor)*(lane_max-lane_min)/(period*crop)))
            cursor = end

    for index, center in enumerate(centers):
        start, end = center-period/2, center+period/2
        if index:
            start = max(start, (centers[index-1]+center)/2)
        if index+1 < len(centers):
            end = min(end, (center+centers[index+1])/2)
        gap(start)
        result.append(dict(kind="LaneWear", start=start, end=end,
                           low=lane_min+(start-(center-period/2))/period*(lane_max-lane_min),
                           high=lane_min+(end-(center-period/2))/period*(lane_max-lane_min), repeat=1))
        cursor=end
    gap(right)
    return result


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("repository",type=Path)
    parser.add_argument("output",type=Path)
    args=parser.parse_args()
    values=numbers(args.repository/"assets/HardcodedNumbers/Draw03/Types_Draw03_Draw03RoadWear_cs.bin")["F32"]
    native_density=(values[7]/values[8]-values[3]/values[4])*4096/values[0]
    lane=(values[3]/values[4],values[7]/values[8])
    gap=(lane[0]-values[9]/values[10]*values[11],lane[0]+values[9]/values[10]*(1-values[11]))
    probes=[("six_lane_reference",[-7.5,-4.5,-1.5,1.5,4.5,7.5],[-9,9]),
            ("wide_median",[-7.5,-4.5,-1.5,5.5,8.5,11.5],[-9,13]),
            ("asymmetric",[-4,-.5,3.1],[-8,7]),
            ("broad_empty_margin",[-1.5,1.5],[-25,25])]
    results=[]
    for name,centers,span in probes:
        rows=bands(values,centers,span)
        worst=0;density_error=0;samples=0
        for row in rows:
            original=(row["low"],row["high"])
            full=lane if row["kind"]=="LaneWear" else gap
            scale=(original[1]-original[0])/(full[1]-full[0])
            offset=(original[0]-full[0])/(full[1]-full[0])
            # Source V pixels per physical metre must remain the measured density.
            density=(full[1]-full[0])*scale*row["repeat"]/(row["end"]-row["start"])*4096
            density_error=max(density_error,abs(density-native_density))
            for index in range(1,258):
                local=index/258
                old=original[0]+((local*row["repeat"])%1)*(original[1]-original[0])
                # Lane clipping stays a subrange of the full immutable lane unit.
                # Empty margins repeat the immutable gap unit at native frequency.
                uv=offset+local*scale if row["kind"]=="LaneWear" else local*row["repeat"]
                new=full[0]+(uv%1)*(full[1]-full[0])
                worst=max(worst,abs(old-new)*4096);samples+=1
        assert worst<1e-9 and density_error<1e-9
        assert all(a["end"]==b["start"] for a,b in zip(rows,rows[1:]))
        results.append(dict(probe=name,bands=len(rows),samples=samples,maxSourcePixelError=worst,
                            maxDensityError=density_error,sourceTexelsPerMetre=native_density,
                            crossesDifferentBandTypes=any(a["kind"]!=b["kind"] for a,b in zip(rows,rows[1:]))))
    report=dict(scope="Double-precision algebra on measured BBN inputs; not runtime float32 or real-mesh coverage",
                laneUnitPixels=[296,376],gapUnitPixels=[296,32],results=results,
                requiredCommonHook="Resolve each existing triangle to one complete band without cutting; affine UV override and immutable full-unit lookup",
                unresolved="Triangles crossing lane/gap transitions or band-repeat seams need a compatible native variable representation; atomic recipes alone are insufficient")
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(f"PASS measured Draw03 wear-unit algebra: {len(results)} layouts; {sum(r['samples'] for r in results)} samples; no density change or receiver cuts")


if __name__=="__main__":
    main()
