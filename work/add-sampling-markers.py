import re,json
from pathlib import Path
root=Path('.').resolve()
producers={'Draw01':'tools/PreparedMaterials/Draw01/BuildUvSampling.py','Draw02':'tools/PreparedMaterials/Draw02/EmitUvSampling.py',
'Draw03':'artifacts/prepared-materials/Draw03/non-tiling/Generate.py','Lift01':'tools/PreparedMaterials/Lift01/BuildUvSampling.py',
'Lift02':'tools/PreparedMaterials/Lift02/ExportUvSampling.py','Lift03':'tools/PreparedMaterials/Lift03/BuildUvSampling.py',
'Lift05':'tools/PreparedMaterials/Lift05/BuildUvSampling.py','PedestrianDraw02':'tools/PreparedMaterials/PedestrianDraw02/BuildUvSampling.py'}
changed=[]
for cls in sorted((root/'src/BridgeBuilder/Bridges/Types').glob('*/*UvSampling.cs')):
 raw=cls.read_bytes();resource=re.search(rb'HardcodedNumbers.Load\("([^"]+)"\)',raw).group(1)
 marker=b'// Embedded numerical arrays: '+resource
 if marker not in raw:
  nl=b'\r\n' if b'\r\n' in raw else b'\n'
  assert raw.count(b'using System;')==1
  cls.write_bytes(raw.replace(b'using System;',marker+nl+b'using System;',1));changed.append(str(cls.relative_to(root)))
 style=cls.parent.name
 if style in producers:
  p=root/producers[style];raw=p.read_bytes()
  if marker not in raw:
   nl=b'\r\n' if b'\r\n' in raw else b'\n'
   assert raw.count(b'using System;')==1,(style,raw.count(b'using System;'))
   p.write_bytes(raw.replace(b'using System;',marker+nl+b'using System;',1));changed.append(str(p.relative_to(root)))
print(json.dumps(changed,indent=2))
