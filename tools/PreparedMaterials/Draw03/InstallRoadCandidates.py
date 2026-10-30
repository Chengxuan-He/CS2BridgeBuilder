"""Install verified offline road candidates into Draw03's packaged resources."""
from pathlib import Path
import argparse
import shutil

ROOT = Path(__file__).resolve().parents[3]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--near', type=Path, required=True)
    parser.add_argument('--far', type=Path, required=True)
    parser.add_argument('--previous', type=Path, required=True)
    parser.add_argument('--backup', type=Path, required=True)
    args = parser.parse_args()
    target = ROOT / 'assets/Materials/Draw03'
    products = {}
    for directory in (args.near, args.far):
        for path in directory.rglob('*'):
            if path.is_file():
                relative = path.relative_to(directory)
                payload = path.read_bytes()
                assert relative not in products or products[relative] == payload
                products[relative] = payload
    assert len([p for p in products if p.suffix == '.Surface']) == 5
    def backup(path):
        dest = args.backup / path.relative_to(target)
        if dest.exists():
            assert dest.read_bytes() == path.read_bytes(), dest
        else:
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(path, dest)
    for relative, payload in products.items():
        path = target / relative
        if path.exists() and path.read_bytes() != payload:
            backup(path)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(payload)
    descriptors = [p.read_bytes() for p in target.rglob('*.Surface')]
    removed = 0
    for previous in (args.previous / 'Shared').glob('*.Texture'):
        relative = previous.relative_to(args.previous)
        if relative in products:
            continue
        cid = previous.stem
        native = bytes(int(cid[i:i + 2][::-1], 16) for i in range(0, 32, 2))
        if any(native in descriptor for descriptor in descriptors):
            continue
        path = target / relative
        if path.exists():
            assert path.read_bytes() == previous.read_bytes(), path
            for item in (path, Path(str(path) + '.cid')):
                assert item.resolve().is_relative_to(target.resolve())
                backup(item)
                item.unlink()
            removed += 1
    print('Installed 5 road Surfaces; retained dependency bytes:', sum(map(len, products.values())),
          '; removed unreferenced earlier candidate textures:', removed)


if __name__ == '__main__':
    main()
