"""Decode original LOD2 albedo for inspection; no material output or resampling."""
import argparse
import hashlib
import importlib.util
import io
import json
import struct
import zipfile
from pathlib import Path
from PIL import Image


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('repository', type=Path)
    p.add_argument('game_data', type=Path)
    p.add_argument('output', type=Path)
    a = p.parse_args()
    module = importlib.util.spec_from_file_location('native_vt', a.repository/'tools/PrepareRepeatUnits/ExtractVtSources.py')
    native = importlib.util.module_from_spec(module)
    module.loader.exec_module(native)
    graph = json.loads((a.repository/'artifacts/prepared-materials/PedestrianDraw01/shared-sampling/native-bindings.json').read_text())
    source = graph['nativeDependencyGraph']['34be2519449b2273c236a8b8a89c3178']
    with zipfile.ZipFile(source['archive']) as archive:
        raw = archive.read(source['entry'])
    assert hashlib.sha256(raw).hexdigest() == source['sha256']
    tile = struct.unpack_from('<H',raw,21)[0]
    tail = struct.unpack_from('<i',raw,34)[0]
    count = struct.unpack_from('<i',raw,38+tail)[0]
    assert tile == 512 and count == 1
    end = struct.unpack_from('<i',raw,42+tail)[0]
    block = native.Decoder(a.game_data).decompress(raw[46+tail:46+tail+end])
    size = tile+16
    header = [124,0x81007,size,size,size*size,0,1]+[0]*11
    header += [32,4,int.from_bytes(b'DX10','little'),0,0,0,0,0,0x1000,0,0,0,0]
    dds = b'DDS '+struct.pack('<31I',*header)+struct.pack('<5I',98,3,0,1,0)+block[:size*size]
    image = Image.open(io.BytesIO(dds)).convert('RGBA').crop((8,8,tile+8,tile+8))
    a.output.mkdir(parents=True,exist_ok=True)
    image.transpose(Image.Transpose.FLIP_TOP_BOTTOM).save(a.output/'native-far-albedo.png')
    print(source['sha256'])


if __name__ == '__main__':
    main()
