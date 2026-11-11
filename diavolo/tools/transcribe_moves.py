import sys,os,json,subprocess,wave
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'.tools/speech'))
os.environ['HF_HOME']=str(root/'.tools/hf-cache')
os.environ['HF_HUB_DISABLE_SYMLINKS_WARNING']='1'
from faster_whisper import WhisperModel
import numpy as np
catalog=json.loads((root/'docs/audio-catalog.json').read_text())
chosen=[r for r in catalog if (r['bank']=='asbr_sfx' and (19<=r['index']<=26 or 63<=r['index']<=66 or 71<=r['index']<=105)) or (r['bank']=='eoh_sfx' and 24<=r['index']<=51)]
out=root/'assets/audio-audit';out.mkdir(parents=True,exist_ok=True)
model=WhisperModel('small',device='cpu',compute_type='int8',cpu_threads=4,download_root=str(root/'.tools/whisper-models'))
report=[]
for r in chosen:
 path=out/(r['bank']+'-'+str(r['index'])+'.wav')
 subprocess.run([str(Path(__file__).resolve().parents[2] / "dio" / '.tools/vgmstream/vgmstream-cli.exe'),'-s',str(r['index']),'-o',str(path),str(root/r['path'])],check=True,capture_output=True)
 with wave.open(str(path),'rb') as wav:
  pcm=np.frombuffer(wav.readframes(wav.getnframes()),dtype='<i2').astype(np.float32).reshape(-1,wav.getnchannels()).mean(axis=1)/32768
  rate=wav.getframerate()
 audio=np.interp(np.arange(int(len(pcm)*16000/rate))*rate/16000,np.arange(len(pcm)),pcm).astype(np.float32)
 segments,_=model.transcribe(audio,language='ja',beam_size=3,condition_on_previous_text=False,vad_filter=False)
 segments=list(segments);text=' '.join(s.text for s in segments)
 report.append(dict(bank=r['bank'],index=r['index'],name=r['name'],text=text,segments=[dict(start=s.start,end=s.end,text=s.text) for s in segments]))
 (root/'docs/voice-transcripts.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
 print(r['bank'],r['index'],text.encode('ascii','backslashreplace').decode(),flush=True)
