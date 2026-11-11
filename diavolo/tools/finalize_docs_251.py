from pathlib import Path
import re,wave,json
r=Path(__file__).resolve().parents[1]
p=r/'README.md';s=p.read_text(encoding='utf-8')
s=re.sub(r'\n{3,}', '\n\n',s)
s=re.sub(r'(?<=\|)\n\n+(?=\|)','\n',s)
s=s.replace('— 2.5 development build','— 2.5.1 development build')
s=s.replace('Current combat/audio revision: `docs/CHANGELOG-2.5.md`.','Current changes: `docs/CHANGELOG-2.5.1.md` and `docs/CHANGELOG-2.5.md`.')
a=s.index('Locked fate chooses fixed attack positions');b=s.index('Enemy animation poses',a)
s=s[:a]+"Locked Fate chooses independently randomized positions near each enemy's original location (roughly 4–8 m), with grounded footprint checks, body clearance, continuous supported paths, wall sweeps, map-zone rejection and separation between marks. Unsafe placements are omitted. Flying enemy families keep airborne positions. "+s[b:]
p.write_text(s,encoding='utf-8')
rows=[]
for p in (r/'assets/audio').glob('*.wav'):
 with wave.open(str(p),'rb') as w:
  assert w.getsampwidth()==2 and w.getcomptype()=='NONE',p
  rows.append(dict(file=p.name,channels=w.getnchannels(),rate=w.getframerate(),seconds=w.getnframes()/w.getframerate()))
(r/'docs/audio-format-validation-2.5.1.json').write_text(json.dumps(rows,indent=2))
print(f'Validated {len(rows)} PCM16 WAV files')
