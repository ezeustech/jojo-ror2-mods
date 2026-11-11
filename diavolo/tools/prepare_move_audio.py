"""Decode original game cues; keep reproducible, editable move assignments."""
import json,subprocess,wave,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[1]
catalog=json.loads((root/'docs/audio-catalog.json').read_text())
lookup={(r['bank'],r['index']):r for r in catalog}
out=root/'assets/audio';out.mkdir(exist_ok=True)
# Bank labels ending in _sfx are historical: the PL banks contain combat VOICES.
voices={'chain1':1,'chain2':3,'chain3':6,'chop':19,'charge':20,'impale':84,'barrage':85,'blood':104,'counter':103,'grab':22,'epitaph':75,'skip':81,'erase':63,'resume':102,'cancel':71,'groundsmash':20}
effects={'chain1':('asbr_battle',1),'chain2':('asbr_battle',2),'chain3':('asbr_battle',14),'chop':('asbr_battle',14),'charge':('asbr_battle',75),'impale':('asbr_battle',12),'barrage':('asbr_battle',68),'blood':('asbr_battle',242),'counter':('asbr_battle',35),'grab':('asbr_battle',29),'epitaph':('asbr_special',1),'skip':('asbr_battle',267),'erase':('asbr_battle',267),'resume':('asbr_battle',30),'cancel':('asbr_battle',55),'groundsmash':('asbr_battle',15),
'impact_light':('asbr_battle',19),'impact_heavy':('asbr_battle',21),'impact_chop':('asbr_battle',24),'impact_ground':('asbr_battle',60),'impact_grab':('asbr_battle',21),
# Use the game's generic stand-appearance cue, never DIO's custom The World WAV.
'summon':('asbr_battle',29),'jump':('asbr_battle',29)}
meanings={'chop':'Inochi wa moratta! (Your life is mine!)','erase':'King Crimson!','epitaph':'Epitaph!','skip':'I erased half a second of time.','resume':'Time begins moving again.','counter':'The pinnacle of truth lies in my ability.','grab':'I will make sure you disappear.','cancel':'There is no longer any problem.','charge':'Now, I sever it!','groundsmash':'Now, I sever it!','impale':'A finishing attack shout; transcription uncertain.','blood':'Short skill shout; provisional.','barrage':'Attack cry; provisional.'}
records=[]
for layer,mapping in [('voice',{k:('asbr_sfx',v) for k,v in voices.items()}),('sfx',effects)]:
 for move,key in mapping.items():
  r=lookup[key];name=move+'_'+layer+'.wav';path=out/name
  subprocess.run([str(Path(__file__).resolve().parents[2] / "dio" / '.tools/vgmstream/vgmstream-cli.exe'),'-s',str(r['index']),'-o',str(path),str(root/r['path'])],check=True,capture_output=True)
  with wave.open(str(path),'rb') as wav:
   assert wav.getsampwidth()==2 and wav.getnframes()>0
   duration=wav.getnframes()/wav.getframerate()
  records.append(dict(move=move,layer=layer,file=name,bank=r['bank'],source=r['path'],index=r['index'],cue=r['name'],seconds=duration,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),meaning=meanings.get(move,'Short combat exertion' if layer=='voice' else ''),confidence='King Crimson callout identified by cue and transcription' if move=='erase' and layer=='voice' else 'Best-fit/provisional; exact original move event linkage not verified'))
(root/'docs/move-audio-map.json').write_text(json.dumps(records,indent=2))
lines=['# Move audio assignments','', 'All audio is decoded from the local Steam ASBR installation. Combat PL banks contain voices; the old inventory label `asbr_sfx` is retained for provenance only. These are best-fit assignments, not a claim of complete original event timing. Transcriptions are machine-assisted and may mishear battle cries. No synthetic voice lines or effects are used.', '', '| Move | Voice cue / meaning | SFX cue |','|---|---|---|']
for move in voices:
 v=next(r for r in records if r['move']==move and r['layer']=='voice');s=next(r for r in records if r['move']==move and r['layer']=='sfx')
 lines.append(f"| {move} | {v['cue']} — {v['meaning']} ({v['seconds']:.2f}s) | {s['cue']} |")
lines+=['','Ground impact, chop, grab and ordinary hit effects are separate contact cues. One voice plays per body, replaced by the next move; voices and SFX have independent volume controls. Erase/skip effect choices and blood/barrage cries are provisional. The ground smash uses the actual ASBR `5kcr01_atk_s_6h` animation.']
(root/'docs/move-audio-map.md').write_text('\n'.join(lines),encoding='utf-8')
print('Decoded and validated',len(records),'voice/SFX assets')
