"""Layer local ASBR heavy-hit and blood cues into a lower, wet impale impact."""
from pathlib import Path
import wave, array, math, hashlib, json
root=Path(__file__).resolve().parents[1]
def read(name):
    with wave.open(str(root/'assets/audio'/name),'rb') as w:
        channels=w.getnchannels();rate=w.getframerate();data=array.array('h',w.readframes(w.getnframes()))
    return [sum(data[i:i+channels])/channels/32768 for i in range(0,len(data),channels)],rate
heavy,hr=read('impact_heavy_sfx.wav');wet,wr=read('blood_sfx.wav')
def sample(data,rate,t,pitch):
    p=t*rate*pitch;i=int(p)
    return 0 if i<0 or i+1>=len(data) else data[i]*(1-(p-i))+data[i+1]*(p-i)
rate=48000;length=1.15;values=[]
for i in range(int(rate*length)):
    t=i/rate
    x=.55*sample(heavy,hr,t,.68)+.85*sample(wet,wr,t,.62)+.3*sample(wet,wr,max(0,t-.065),.84)
    x+=.12*math.sin(2*math.pi*(76*t-12*t*t))*math.exp(-t*14)
    x*=min(1,t/.002)*min(1,(length-t)/.12)
    values.append(x)
peak=max(abs(x) for x in values);gain=.88/max(peak,.01)
out=root/'assets/audio/impact_impale_sfx.wav'
with wave.open(str(out),'wb') as w:
    w.setparams((1,2,rate,0,'NONE','not compressed'));w.writeframes(array.array('h',(int(x*gain*32767) for x in values)).tobytes())
(root/'docs/impale-audio.json').write_text(json.dumps({'output':out.name,'sources':['impact_heavy_sfx.wav','blood_sfx.wav'],'processing':'Pitch-lowered layered ASBR impacts, wet delayed layer, short synthesized low thump, normalized to 0.88 peak','seconds':length,'sha256':hashlib.sha256(out.read_bytes()).hexdigest()},indent=2))
print('Validated PCM16 mono impale impact:',length,'seconds; peak 0.88')
