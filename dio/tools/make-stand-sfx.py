"""Original short summon shimmer and guard clang, PCM16 mono at 48 kHz."""
from pathlib import Path
import numpy as np
import wave
root=Path(__file__).resolve().parents[1]
rate=48000
rng=np.random.default_rng(3400)
def write(name,data):
    data*=np.minimum(1,np.arange(len(data))/144)
    data[-480:]*=np.linspace(1,0,480)
    data=data/max(1,float(np.max(np.abs(data))))*.8
    with wave.open(str(root/'assets/audio'/f'{name}.wav'),'wb') as f:
        f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes((data*32767).astype('<i2').tobytes())
t=np.arange(int(rate*.55))/rate
noise=np.convolve(rng.normal(size=len(t)),np.ones(8)/8,mode='same')
summon=.28*np.sin(2*np.pi*(170*t+600*t*t))*np.exp(-t*7)
summon+=noise*.5*np.sin(np.pi*t/.55)**2
for hz in (1240,1860,2480):summon+=.08*np.sin(2*np.pi*hz*t)*np.exp(-t*5)
write('stand-summon',summon)
t=np.arange(int(rate*.32))/rate
hit=.3*np.sin(2*np.pi*110*t)*np.exp(-t*18)+rng.normal(size=len(t))*.25*np.exp(-t*75)
for hz,amp in ((740,.16),(1137,.13),(1831,.08),(2710,.05)):hit+=amp*np.sin(2*np.pi*hz*t)*np.exp(-t*12)
write('stand-block',hit)
print('Generated and bounded two original Stand effects.')
