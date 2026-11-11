from pathlib import Path
import zipfile,json,hashlib,sys
root=Path(__file__).resolve().parents[1]
version=sys.argv[1]
assert all(c.isdigit() or c=='.' for c in version)
with zipfile.ZipFile(root/'dist/Diavolo-2.0.0.zip') as old: manifest=json.loads(old.read('manifest.json'))
if tuple(int(part) for part in version.split('.')) >= (2,5,3):
 manifest['dependencies'].append('Rune580-Risk_Of_Options-2.8.6')
manifest.update(version_number=version,description='Diavolo and King Crimson: independent stand lock-on, five-hit combo, smoother poses, balanced multiplayer audio, impale and time erasure. Local test build.')
archive=root/f'dist/Diavolo-{version}.zip'
if archive.exists():raise RuntimeError('Refusing to overwrite an exported snapshot')
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
 z.writestr('manifest.json',json.dumps(manifest,indent=2))
 z.write(root/'README.md','README.md')
 for f in (root/'dist/Diavolo').rglob('*'):
  if f.is_file():z.write(f,'plugins/Diavolo/'+f.relative_to(root/'dist/Diavolo').as_posix())
with zipfile.ZipFile(archive) as z:
 assert z.testzip() is None
 assert len([n for n in z.namelist() if n.endswith('.dll')])==1
 for f in (root/'dist/Diavolo').rglob('*'):
  if f.is_file():assert z.read('plugins/Diavolo/'+f.relative_to(root/'dist/Diavolo').as_posix())==f.read_bytes()
data={'file':str(archive),'bytes':archive.stat().st_size,'sha256':hashlib.sha256(archive.read_bytes()).hexdigest(),'dependencies':manifest['dependencies']}
(root/f'docs/export-{version}.json').write_text(json.dumps(data,indent=2))
with zipfile.ZipFile(root/f'dist/Source-{version}.zip','w',zipfile.ZIP_DEFLATED) as z:
 for folder,pattern in [('src','*.cs'),('UnityProject/Assets','*.shader'),('UnityProject/Assets/Editor','*.cs'),('tests','*.cs'),('assets/skill-icons','*.svg'),('tools','generate_skill_icons.py')]:
  for f in (root/folder).glob(pattern):z.write(f,f.relative_to(root).as_posix())
 for f in ['README.md','VALIDATION.md','build.ps1']:z.write(root/f,f)
print(json.dumps(data,indent=2))
