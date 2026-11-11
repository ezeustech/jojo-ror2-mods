"""Extract cries verified by matching the EOH dialogue cue IDs to the voice bank."""
import json,subprocess,struct,sys,hashlib
from pathlib import Path
from inventory_cpk import utf_table
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'third_party/xfbin-importer/Blender-XFBIN-Importer'))
from xfbin_lib.xfbin import read_xfbin
bank=root/'assets/audio-source/v_btl_3dio01.awb'
acb=(root/'assets/audio-source/v_btl_3dio01.acb').read_bytes()
def table(data):
    # ACB UTF v1 has a 16-bit version before its 16-bit row offset.
    data=bytearray(data);data[8:10]=b'\0\0';return utf_table(data)
top=table(acb)[0]
def nested(key):
    d=top[key];return table(acb[d['offset']:d['offset']+d['length']])
cues=nested('CueTable');names=nested('CueNameTable')
x=read_xfbin(str(root/'assets/source/data_sim_dataRegion0/data/message/PS3/jpn/EoH_battleChara_3dio.bin.xfbin'))
b=next(c.data for p in x.pages for c in p.chunks if c.type=='nuccChunkBinary')
dialogue={}
for i in range(struct.unpack_from('>I',b,8)[0]):
    pos=16+24*i;record=struct.unpack_from('>6I',b,pos);offset=record[2]+pos+8
    dialogue[record[5]>>16]=b[offset:b.index(b'\0',offset)].decode('utf-8')
index=json.loads((root/'docs/dio-voice-index.json').read_text())
report=[]
for cue_id,output,expected in [(338,'voice-muda-01','無駄無駄無駄無駄！'),(355,'voice-muda-02','無駄無駄無駄ァッ！'),(39,'voice-wryy','ＷＲＹＹＹＹＹ')]:
    assert expected in dialogue[cue_id]
    cue_index=next(i for i,c in enumerate(cues) if c['CueId']==cue_id)
    name=next(n['CueName'] for n in names if n['CueIndex']==cue_index)
    stream=next(s for s in index if s['stream name']==name)
    dest=root/f'assets/audio/{output}.wav'
    subprocess.run([str(root/'.tools/vgmstream/vgmstream-cli.exe'),'-s',str(stream['index']),'-o',str(dest),str(bank)],check=True,stdout=subprocess.DEVNULL)
    report.append({'file':dest.name,'cue':name,'cue_id':cue_id,'stream_index':stream['index'],'japanese':dialogue[cue_id],'sha256':hashlib.sha256(dest.read_bytes()).hexdigest()})
(root/'docs/attack-voice-provenance.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('Extracted three dialogue-verified attack cries.')
