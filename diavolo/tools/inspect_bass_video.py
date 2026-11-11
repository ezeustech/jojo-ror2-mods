from pathlib import Path
import sys
sys.path.insert(0,str(Path(__file__).resolve().parents[2] / "diavolo" / '.tools/speech'))
import av
c=av.open(str(Path(__file__).resolve().parents[1] / "local-inputs" / 'King Crimson Time Skip Bass.mp4'))
for s in c.streams: print(s.type,'duration',float(s.duration*s.time_base) if s.duration else None,'rate',getattr(s,'average_rate',None),'size',getattr(s,'width',None),getattr(s,'height',None))
for f in c.decode(video=0):
 if f.time>1:
  f.to_image().save(str(Path(__file__).resolve().parents[2] / "diavolo" / 'docs/bass-preview.jpg'));break
