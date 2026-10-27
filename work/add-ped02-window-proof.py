from pathlib import Path
p=Path('tools/PreparedMaterials/PedestrianDraw02/BuildUvSampling.py');s=p.read_text()
s=s.replace("assert template['material']['m_Shader']['m_PathID']==571", """assert template['material']['m_Shader']['m_PathID']==571
    window_template_path=semantics/'template-2.json'
    window_template=json.loads(window_template_path.read_text())
    assert window_template['m_Shader']['m_PathID']==550
    assert next(t for t in current if t['hash']==2)['verifiedAgainstCurrentResources']""")
s=s.replace("at=2;flags=[];groups=collections.defaultdict(list)", """window=None;local_domains=set();admitted_windows=[]
        if src['template']==2:
            assert binding['mesh']==1 and src['cid']=='3b81df0257792cb8f7641e88a51c2d1e'
            assert all(not group for group in src['descriptorOverrides'].values())
            window_path=semantics.parent/'windows'/(STYLE+'-'+binding['render'].replace(' ','_')+'-1.json')
            window=json.loads(window_path.read_text())
            assert sha(Path(window['geometry']))==window['geometrySha256']
            assert (window['style'],window['render'],window['mesh'])==(STYLE,binding['render'],binding['mesh'])
            full_uv=np.asarray(window['uv0'],dtype=np.float32);colors=np.asarray(window['colors'])
            assert full_uv.shape==(len(uv),4) and np.isfinite(full_uv).all() and np.isfinite(colors).all()
            assert np.array_equal(full_uv[:,:2],uv) and np.array_equal(window['indices'],faces)
            local=full_uv[faces,1:3]
            local_faces=np.flatnonzero(((local>=0)&(local<=1)).all(axis=(1,2)) &
                (full_uv[faces,3]==full_uv[faces,3][:,:1]).all(axis=1) &
                (colors[faces]==colors[faces][:,:1,:]).all(axis=(1,2))).tolist()
            assert local_faces==window['sourceLocalFaces']
            local_domains=set(window['sourceLocalDomains'])
        at=2;flags=[];groups=collections.defaultdict(list)""")
s=s.replace("flags.append(int(flag))\n            groups['native-uv0-finite-authored-mapping' if flag else ';'.join(sorted(reasons-{'native-uv0-finite-authored-mapping'}))].append(di)","""reason='native-uv0-finite-authored-mapping' if flag else ';'.join(sorted(reasons-{'native-uv0-finite-authored-mapping'}))
            if window is not None:
                eligible=all(int(f) in local_faces for f in members)
                assert eligible==(di in local_domains)
                if eligible:
                    flag=True;reason='native-window-local-yz';admitted_windows.append(di)
            flags.append(int(flag))
            groups[reason].append(di)""")
s=s.replace("nativeIslands=proof))", """nativeIslands=proof,
            windowEvidence=None if window is None else dict(report=str(window_path.relative_to(ROOT)),sha256=sha(window_path),
                geometrySha256=window['geometrySha256'],sourceLocalDomains=admitted_windows,localAxes='UV0.YZ',
                preservedChannels=['UV0.XYZW','COLOR'],nativeTemplateSha256=sha(window_template_path),shader=550)))""")
s=s.replace("semantics/'shader-571-parsed.json',", "semantics/'shader-571-parsed.json',semantics/'shader-550-parsed.json',semantics/'550-184.asm',")
p.write_text(s)
