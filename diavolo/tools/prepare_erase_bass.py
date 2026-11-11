import sys,wave,json
from pathlib import Path
root=Path(str(Path(__file__).resolve().parents[2] / "diavolo"));sys.path.insert(0,str(root/'.tools/speech'))
import av,numpy as np
c=av.open(str(Path(__file__).resolve().parents[1] / "local-inputs" / 'King Crimson Time Skip Bass.mp4'))
res=av.AudioResampler(format='s16',layout='stereo',rate=48000)
chunks=[]
for frame in c.decode(audio=0):
 for f in res.resample(frame):chunks.append(f.to_ndarray().tobytes())
for f in res.resample(None):chunks.append(f.to_ndarray().tobytes())
data=b''.join(chunks)
with wave.open(str(root/'assets/audio/erase_bass_sfx.wav'),'wb') as w:w.setparams((2,2,48000,0,'NONE','not compressed'));w.writeframes(data)
length=len(data)/(48000*4)
(root/'docs/erase-bass.json').write_text(json.dumps({'source':str(Path(__file__).resolve().parents[1] / "local-inputs" / 'King Crimson Time Skip Bass.mp4'),'seconds':length,'audioOnly':True,'sampleRate':48000},indent=2))
print(length)
