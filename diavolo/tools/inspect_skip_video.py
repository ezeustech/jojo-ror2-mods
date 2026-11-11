from pathlib import Path
import sys
sys.path.insert(0,str(Path(__file__).resolve().parents[2] / "diavolo" / '.tools/speech'))
import av
c=av.open(str(Path(__file__).resolve().parents[1] / "local-inputs" / 'King Crimson VFX Green Screen.mp4'))
for s in c.streams: print(s.type,s.duration,s.time_base, getattr(s,'width',None),getattr(s,'height',None))
for i,f in enumerate(c.decode(video=0)):
 if i in [0,15,30,60]:
  f.to_image().save(str(Path(__file__).resolve().parents[2] / "diavolo" / 'docs/skip-preview-')+str(i)+'.png')
print('frames',i+1)
