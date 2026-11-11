import json,re,subprocess,concurrent.futures
from pathlib import Path
root=Path(__file__).resolve().parents[1]
exe=str(Path(__file__).resolve().parents[2] / "dio" / '.tools/vgmstream/vgmstream-cli.exe')
banks={'eoh_voice':root/'assets/audio-source/v_btl_5dvl01.awb','eoh_sfx':root/'assets/audio-source/5dvl01_pl.nus3bank','asbr_voice':root/'assets/asbr-source/data/sound/PC/JP/v_btl_5dvl01.awb','asbr_sfx':root/'assets/asbr-source/data/sound/PC/JP/5dvl01_pl.nus3bank'}
banks.update({'asbr_battle':root/'assets/asbr-source/data/sound/PC/battle.nus3bank','asbr_common':root/'assets/asbr-source/data/sound/PC/common.nus3bank','asbr_special':root/'assets/asbr-source/data/sound/PC/JP/5dvl01_spl1.nus3bank','eoh_special':root/'assets/audio-source/5dvl01_spl1.nus3bank'})
def meta(args):
 label,path,index=args
 result=subprocess.run([exe,'-m','-s',str(index),str(path)],capture_output=True,text=True)
 data=dict(re.findall(r'^([^:\r\n]+): (.*)$',result.stdout,re.M))
 return dict(bank=label,path=str(path.relative_to(root)),index=index,name=data.get('stream name',''),samples=data.get('stream total samples',''),metadata=data)
all=[]
for label,path in banks.items():
 if not path.exists():continue
 first=meta((label,path,1));count=int(first['metadata'].get('stream count','1'))
 with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:all.extend(pool.map(meta,[(label,path,i) for i in range(1,count+1)]))
(root/'docs/audio-catalog.json').write_text(json.dumps(all,indent=2))
(root/'docs/audio-catalog.txt').write_text('\n'.join(f"{r['bank']} {r['index']:03} {r['name']} {r['samples']}" for r in all))
print('Indexed',len(all),'streams')
