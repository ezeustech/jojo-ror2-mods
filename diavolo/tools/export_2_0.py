import json,re,zipfile,hashlib
from pathlib import Path
root=Path(str(Path(__file__).resolve().parents[2] / "diavolo"))
yaml=Path(str(Path(__file__).resolve().parents[1] / "local-inputs" / 'mods.yml')).read_text()
wanted=['bbepis-BepInExPack','RiskofThunder-RoR2BepInExPack','RiskofThunder-R2API_Core','RiskofThunder-R2API_ContentManagement','RiskofThunder-R2API_Prefab','RiskofThunder-R2API_Language','RiskofThunder-R2API_Networking']
deps=[]
for name in wanted:
 block=next(b for b in yaml.split('- manifestVersion:') if f'  name: {name}\n' in b)
 v=re.search(r'versionNumber:\s+major: (\d+)\s+minor: (\d+)\s+patch: (\d+)',block)
 deps.append(name+'-' + '.'.join(v.groups()))
manifest={'name':'Diavolo','version_number':'2.0.0','website_url':'','description':'Diavolo and King Crimson survivor. Local 2.0 snapshot before the spike ultimate.','dependencies':deps}
readme='''# Diavolo 2.0.0

Local import snapshot, saved before the spike ultimate is added.

Use Thunderstore Mod Manager Settings > Import local mod and select this ZIP. If prompted, use name Diavolo, author Local, version 2.0.0. Disable older Diavolo entries first. The manually installed BepInEx/plugins/Diavolo folder can also contain an older copy; keep only one active Diavolo DLL in the profile.

Includes all current models, audio, transparent time-skip overlay, red erasure tint, randomized grounded fate, owner-visible erasure, and updated impale/charge meter. The spike ultimate is not included.

M1 chain; M2 charge impale; Shift time skip; R erasure; F Epitaph; C barrage; V blood; X counter; G grab; B chop; T ground smash; Z Puttsun Cancel.

This is a local development package. Gameplay and multiplayer validation remain in progress.
'''
archive=root/'dist/Diavolo-2.0.0.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
 z.writestr('manifest.json',json.dumps(manifest,indent=2));z.writestr('README.md',readme)
 for f in (root/'dist/Diavolo').rglob('*'):
  if f.is_file():z.write(f,'plugins/Diavolo/'+f.relative_to(root/'dist/Diavolo').as_posix())
with zipfile.ZipFile(archive) as z:
 assert z.testzip() is None
 assert json.loads(z.read('manifest.json'))['version_number']=='2.0.0'
 assert len([n for n in z.namelist() if n.endswith('.dll')])==1
 assert len([n for n in z.namelist() if '/TimeOverlay/' in n and n.endswith('.png')])==20
 for f in (root/'dist/Diavolo').rglob('*'):
  if f.is_file():assert z.read('plugins/Diavolo/'+f.relative_to(root/'dist/Diavolo').as_posix())==f.read_bytes()
(root/'docs/export-2.0.json').write_text(json.dumps({'file':str(archive),'bytes':archive.stat().st_size,'sha256':hashlib.sha256(archive.read_bytes()).hexdigest(),'ultimateIncluded':False},indent=2))
print('Verified',archive,archive.stat().st_size,'bytes')
