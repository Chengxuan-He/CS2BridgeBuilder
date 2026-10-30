"""Exercise missing/orphan rejection without modifying prepared material inputs."""
import argparse
from pathlib import Path
import subprocess
import sys


def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ['game-data','materials','work']:p.add_argument('--'+name,required=True,type=Path)
    a=p.parse_args();work=a.work.resolve();work.mkdir(parents=True,exist_ok=True)
    cases=[('missing','Missing prepared MidMips file:'),('orphan-file','Orphan MidMips files:'),('orphan-directory','Orphan MidMips directories:')]
    for name,message in cases:
        output=work/name
        cache=output/'Shared/MidMips';cache.mkdir(parents=True,exist_ok=True)
        if name=='orphan-file':(cache/'unexpected.bin').write_bytes(b'test')
        if name=='orphan-directory':(cache/'unexpected-empty').mkdir(exist_ok=True)
        command=[sys.executable,str(Path(__file__).with_name('PrepareMidMips.py')),
                 '--game-data',str(a.game_data),'--materials',str(a.materials),
                 '--output',str(output),'--report',str(work/(name+'.json')),
                 '--shared','--per-surface','--check']
        result=subprocess.run(command,capture_output=True,text=True)
        assert result.returncode!=0 and message in result.stderr,(name,result.stdout,result.stderr)
        assert not list(output.rglob('*.MidMips'))
        (work/(name+'.log')).write_text(result.stderr,encoding='utf-8')
    print('PASS missing, orphan-file and orphan-directory fail before writing payloads')


if __name__=='__main__':main()
