"""Decode user-supplied ASBR textures without changing the original mod."""
import io,sys,json
from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'third_party/xfbin-importer/Blender-XFBIN-Importer'))
from xfbin_lib.xfbin import read_xfbin,NuccChunkTexture
from xfbin_lib.xfbin.structure.dds import NutTexture_to_DDS
source=Path(str(Path(__file__).resolve().parents[1] / "local-inputs" / 'shadow-variant'))
dest=root/'UnityProject/Assets/ShadowTextures';dest.mkdir(exist_ok=True)
report=[]
for p in sorted(source.rglob('*.xfbin')):
 for c in read_xfbin(str(p)).get_chunks_by_type(NuccChunkTexture):
  if not hasattr(c,'nut'):continue
  for i,t in enumerate(c.nut.textures):
   out=dest/(c.name+f'_{i}.png')
   im=Image.open(io.BytesIO(NutTexture_to_DDS(t))).convert('RGBA');im.save(out)
   report.append({'source':str(p),'texture':out.name,'size':im.size})
(root/'docs/shadow-textures.json').write_text(json.dumps(report,indent=2))
print('Decoded',len(report),'textures')
