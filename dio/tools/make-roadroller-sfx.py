"""Deterministic original impact layers; no external recordings required."""
from pathlib import Path
import numpy as np
import wave
root=Path(__file__).resolve().parents[1]
out=root/'assets/audio'; rate=48000
rng=np.random.default_rng(3101)
def write(name,data):
 data=data/max(1,float(np.max(np.abs(data))))*.88
 data[-240:]*=np.linspace(1,0,240)
 with wave.open(str(out/(name+'.wav')),'wb') as f:
  f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes((data*32767).astype('<i2').tobytes())
def noise(n,window):return np.convolve(rng.normal(0,1,n),np.ones(window)/window,mode='same')
t=np.arange(int(rate*.45))/rate
write('road-fall',(noise(len(t),8)*5+np.sin(2*np.pi*(180*t+360*t*t))*.15)*np.sin(np.pi*t/.45)**.6)
t=np.arange(int(rate*1.1))/rate
crack=noise(len(t),3)*np.exp(-t*8)
for start in [.0,.035,.075,.14,.23,.34]:
 dt=np.maximum(0,t-start);crack+=(t>=start)*noise(len(t),2)*np.exp(-dt*65)*.6
crack+=np.sin(2*np.pi*72*t)*np.exp(-t*8)*.4
write('road-crack',crack)
t=np.arange(int(rate*2.2))/rate
boom=np.sin(2*np.pi*(43*t+38*(1-np.exp(-5*t))/5))*np.exp(-t*2.3)*.8
boom+=noise(len(t),70)*8*np.exp(-t*2)+noise(len(t),3)*np.exp(-t*12)*.5
boom*=np.minimum(1,t/.003)
write('road-explosion',boom)
print('Created 3 PCM16 mono impact layers at 48 kHz.')
