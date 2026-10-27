"""Report source delivery, a Paradox Mods content package and one bridge's physical files."""
import argparse
import collections
import json
import hashlib
from pathlib import Path
import re
import subprocess
import zipfile


def summarize(files, base, component):
    groups = collections.defaultdict(lambda: dict(files=0, bytes=0))
    for path in files:
        group = groups[component(path.relative_to(base).as_posix())]
        group['files'] += 1
        group['bytes'] += path.stat().st_size
    size = sum(group['bytes'] for group in groups.values())
    return dict(files=sum(group['files'] for group in groups.values()), bytes=size,
                MiB=round(size / 1024**2, 4), components=dict(sorted(groups.items())))


def measure(repo, package, game_data=None, owner=None):
    def git_paths(*args):
        output = subprocess.check_output(['git', 'ls-files', '-z', *args], cwd=repo)
        return {repo / path.decode('utf-8') for path in output.split(b'\0') if path}
    tracked = git_paths('--cached')
    pending = git_paths('--others', '--exclude-standard')
    selected = {p for p in tracked | pending if p.is_file()}
    # Never silently label an unexpanded submodule as a complete source delivery.
    omitted = {p for p in tracked if p.exists() and not p.is_file()}
    if omitted:
        raise ValueError('Expand submodule/directory source entries before reporting: ' + str(omitted))
    source = summarize(selected, repo, lambda p: '/'.join(p.split('/')[:2])
                       if p.startswith(('assets/', 'src/', 'vendor/')) else p.split('/')[0])
    source.update(trackedFiles=sum(p in tracked for p in selected),
                  pendingAdditionFiles=sum(p not in tracked for p in selected),
                  scope='Current working-tree bytes of tracked and non-ignored pending source files; excludes deleted files, Git history, ignored build output and authoring artifacts. Not a claim of a completed upload.')
    with zipfile.ZipFile(package) as archive:
        if archive.testzip() is not None:
            raise ValueError('Package CRC verification failed.')
        entries = [e for e in archive.infolist() if not e.is_dir()]
        package_size = dict(path=str(package), bytes=package.stat().st_size,
                            MiB=round(package.stat().st_size / 1024**2, 4), files=len(entries),
                            sha256=hashlib.sha256(package.read_bytes()).hexdigest(),
                            scope='Complete Paradox Mods content ZIP, including archive overhead. Supply an official-SDK local package or a downloaded published package; retain provenance separately. Does not imply publication.',
                            contents=[dict(path=e.filename, compressedBytes=e.compress_size) for e in entries])
    bridge = None
    if owner:
        if not re.fullmatch(r'b[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}', owner):
            raise ValueError('Expected one exact bridge UUID owner.')
        if game_data is None:
            raise ValueError('--game-data is required with --owner.')
        token = re.compile(re.escape(owner) + r'(?![0-9a-f-])', re.I)
        files = [p for section in ('ImportedData', 'BridgeBuilder')
                 for p in (game_data / section).rglob('*')
                 if p.is_file() and token.search(p.relative_to(game_data).as_posix())]
        if not files:
            raise ValueError('No persisted files for bridge ' + owner)
        bridge = summarize(files, game_data, lambda p: Path(p).suffix or '(no extension)')
        bridge.update(owner=owner,
                      scope='All UUID-owned physical files, including dependency snapshots and CID sidecars. Built-in referenced files are not copied; unresolved logical references and correspondence to the current build still require asset-graph verification.')
    return dict(sourceDelivery=source, paradoxModsPackage=package_size, singleBridge=bridge,
                singleBridgeStatus='Measured owned physical files; verify graph completeness separately.' if bridge else 'No current-build persisted bridge supplied; not measured and not accepted.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--package', type=Path, required=True)
    parser.add_argument('--game-data', type=Path)
    parser.add_argument('--owner')
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    repo = args.repo.resolve()
    result = measure(repo, args.package.resolve(),
                     args.game_data.resolve() if args.game_data else None, args.owner)
    text = json.dumps(result, indent=2) + '\n'
    if args.output:
        args.output.write_text(text, encoding='utf-8')
    print(text, end='')
