from pathlib import Path
import sys
sys.path.insert(0,str(Path(__file__).resolve().parents[2] / "diavolo" / '.tools/speech'))
import av
from PIL import Image,ImageDraw
p=str(Path(__file__).resolve().parents[1] / "local-inputs" / 'YourOnlyMoveIsHUSTLE 2026.10.18 - 22.26.10.01.mp4')
c=av.open(p);s=c.streams.video[0];duration=float(s.duration*s.time_base);print('duration',duration,'fps',s.average_rate)
frames=[];targets=[duration*i/15 for i in range(16)]
for f in c.decode(video=0):
 t=float(f.pts*f.time_base)
 if targets and t>=targets[0]:
  im=f.to_image();im.thumbnail((480,270));frames.append((t,im));targets.pop(0)
 if not targets:break
sheet=Image.new('RGB',(1920,4*300))
d=ImageDraw.Draw(sheet)
for i,(t,im) in enumerate(frames):
 x=(i%4)*480;y=(i//4)*300;sheet.paste(im,(x,y));d.text((x+5,y+272),f'{t:.2f}s',fill='white')
sheet.save(str(Path(__file__).resolve().parents[2] / "diavolo" / 'docs/ultimate-reference.jpg'))
