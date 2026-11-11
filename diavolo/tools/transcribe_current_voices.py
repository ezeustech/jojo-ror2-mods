import sys,json,os,wave
import numpy as np
from pathlib import Path
root=Path(__file__).resolve().parents[1];sys.path.insert(0,str(root/'.tools/speech'))
os.environ['HF_HOME']=str(root/'.tools/hf-cache')
from faster_whisper import WhisperModel
model=WhisperModel('small',device='cpu',compute_type='int8',cpu_threads=4,download_root=str(root/'.tools/whisper-models'),local_files_only=True)
out={}
for name in ['impale_voice','barrage_voice','blood_voice','erase_bass_sfx']:
 with wave.open(str(root/'assets/audio'/(name+'.wav')),'rb') as w:
  raw=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(np.float32).reshape(-1,w.getnchannels()).mean(axis=1)/32768;rate=w.getframerate()
 audio=np.interp(np.arange(int(len(raw)*16000/rate))*rate/16000,np.arange(len(raw)),raw).astype(np.float32)
 segments,_=model.transcribe(audio,language='ja',task='translate',beam_size=5,condition_on_previous_text=False,vad_filter=False)
 out[name]=[dict(start=s.start,end=s.end,text=s.text) for s in segments]
 (root/'docs/current-voice-translations.json').write_text(json.dumps(out,indent=2),encoding='utf-8');print(name,out[name],flush=True)
