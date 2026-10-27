from pathlib import Path
p=Path('tools/PreparedMaterials/PedestrianDraw02/BuildUvSampling.py');s=p.read_text()
s=s.replace("if src['sha256']==repeat_digest and any(overlaps(t,region) for t in tri):\n                    reason='density-existing-road-repeat-overlap'\n                else:\n                    proof.append", "proof.append")
# Reduce only the now-unconditional proof continuation indentation.
s=s.replace("                        mappingProof='Every authored", "                    mappingProof='Every authored")
s=s.replace("flag=reasons=={'native-uv0-finite-authored-mapping'}", """# Repeat membership belongs to this original domain's source faces.
            # A neighboring face in the same authored chart cannot veto retention.
            if src['sha256']==repeat_digest and any(overlaps(t,region) for t in uv[faces[members]].astype(float)):
                reasons.add('density-existing-road-repeat-overlap')
            flag=reasons=={'native-uv0-finite-authored-mapping'}""")
s=s.replace("existing road repeat overlaps and unknown shaders remain density mappings.", "exact original-domain road repeat overlaps and unknown shaders remain density mappings.")
Path('work/ped02-domain-fix-candidate.py').write_text(s)
compile(s,str(p),'exec')
print('Domain-only classification candidate syntax verified; production unchanged.')
