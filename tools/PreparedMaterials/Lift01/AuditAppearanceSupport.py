"""Tie Lift01's supported input appearance to the actual native receiver graph."""
import hashlib
import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
source = ROOT.parent/'2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research/all-movable-audit.json'
raw = source.read_bytes()
assert hashlib.sha256(raw).hexdigest() == '47ef0f1723e8643e74a17751aa480f97260bf6e549aa60ec9f14f8e003b12070'
audit = json.loads(raw)
records = audit['selected']
network = records['118602']['data']
assert network['m_Name'] == 'LiftBridge01' and network['m_TrackType'] == 1
sections = sorted({s['m_Section']['m_PathID'] for s in network['m_Sections']})
assert sections == [121088,121089,121091]
section = records['121091']['data']
assert 121077 in {p['m_Piece']['m_PathID'] for p in section['m_Pieces']}
receiver = records['121077']['data']
ref = receiver['m_SurfaceAssets'][0]
cid = ''.join(format(b&15,'x')+format(b>>4,'x') for b in struct.pack('<4i',*(ref['m_GuidPart'+str(i)] for i in range(1,5))))
assert cid == 'a3522ead5e61e02569f0884fb4607eec'
layer_path = ROOT/'artifacts/material-remap/Lift04/track-bed-layer-evidence.json'
layers = json.loads(layer_path.read_text())
assert layers['surface'] == cid and layers['actualInputAppearanceType'] == 'Ballast'
report = dict(sourceAuditSHA256=hashlib.sha256(raw).hexdigest(), network=118602, section=121091,
    receiver=121077, receiverSurface=cid, supportedInputAppearance='Ballast', geometryRole='TrackBed',
    layerEvidencePath=str(layer_path), layerEvidenceSHA256=hashlib.sha256(layer_path.read_bytes()).hexdigest(),
    sourceLayerInterpretation='The exact shared native material has Ballast base and worldspace layers; neither layer is asphalt or an independent rail/sleeper motif.',
    prototypeOutput='Lift01 authored floor appearance, per LOD. Support is evidenced by original network receiver graph, not a texture-name guess.',
    unrelatedTypes='No evidence here authorizes Asphalt, Sidewalk, WheelTrack, or Pathway composition.',
    runtimeReady=False, reason='Near/mid candidate still needs source-footprint/guard/normal checks; far negative trapezoid needs a finite parameterized mapping.')
out = ROOT/'artifacts/material-remap/Lift01/appearance-support.json'
out.write_text(json.dumps(report,indent=2))
print(out)
