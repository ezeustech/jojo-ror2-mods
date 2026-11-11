from pathlib import Path
import sys,json,hashlib
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'.tools/speech'))
import av,numpy as np
from PIL import Image
source=Path(str(Path(__file__).resolve().parents[1] / "local-inputs" / 'King Crimson VFX Green Screen.mp4'))
out=root/'assets/time-overlay';out.mkdir(exist_ok=True)
c=av.open(str(source));stream=c.streams.video[0];fps=float(stream.average_rate)
for i,frame in enumerate(c.decode(video=0)):
 rgb=np.asarray(frame.to_image().resize((960,540))).astype(np.float32)/255
 green=rgb[:,:,1];other=np.maximum(rgb[:,:,0],rgb[:,:,2])
 alpha=1-np.clip((green-other-.035)/.22,0,1)
 rgb[:,:,1]=np.minimum(green,other+.04)
 rgba=np.dstack([rgb,alpha]);Image.fromarray((np.clip(rgba,0,1)*255).astype('uint8'),'RGBA').save(out/f'{i:03}.png')
(out/'timing.txt').write_text(f'{fps:.8f}\n')
(root/'docs/time-overlay.json').write_text(json.dumps({'source':str(source),'sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'frames':i+1,'fps':fps,'duration':(i+1)/fps,'teammateSpeed':1.5,'processing':'Chroma key green and suppress green edge spill; 960x540 RGBA frames; existing game audio retained'},indent=2))
# Preview keyed frame over a neutral background for edge inspection.
im=Image.open(out/'015.png');bg=Image.new('RGBA',im.size,(45,45,48,255));bg.alpha_composite(im);bg.convert('RGB').save(root/'docs/time-overlay-keyed.jpg')
print(i+1,'keyed frames at',fps,'fps')
