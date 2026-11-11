from pathlib import Path
import zipfile,json,hashlib
root=Path(str(Path(__file__).resolve().parents[2] / "diavolo"))
with zipfile.ZipFile(root/'dist/Diavolo-2.0.0.zip') as old: manifest=json.loads(old.read('manifest.json'))
manifest.update(version_number='2.1.2',description='Diavolo with spike ultimate, bass erasure audio, cursor skip and real decoy skills. Layered time-erasure audio and skill activation hotfix; pre-cosmic-scene snapshot.')
readme='''# Diavolo 2.1.2

Import locally through Thunderstore Mod Manager Settings > Import local mod. Disable older Diavolo copies first. Keep one active Diavolo DLL per profile.

Hotfix: versioned assembly identity and safe combo cleanup. Keep only one active Diavolo DLL. Snapshot before the cosmic Time Erase scene and revised aura/large-enemy grounding work.

H: spike ultimate (45-second cooldown). Includes 22.9-second bass erasure audio, opening/closing overlay, faster Shift overlay, crosshair teleport, conditional stand visibility, faint aura, hidden item models, actual zero-damage decoy skills, and repaired ground-smash animation.

Build and 23 combat-rule checks pass. Spike meshes passed 12 editor checks; packaged ground-smash animation was rendered and verified. Gameplay and multiplayer still need testing. See included README and VALIDATION files for controls and limitations.
'''
archive=root/'dist/Diavolo-2.1.2.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
 z.writestr('manifest.json',json.dumps(manifest,indent=2));z.writestr('README.md',readme)
 for f in (root/'dist/Diavolo').rglob('*'):
  if f.is_file():z.write(f,'plugins/Diavolo/'+f.relative_to(root/'dist/Diavolo').as_posix())
with zipfile.ZipFile(archive) as z:
 assert z.testzip() is None
 for f in (root/'dist/Diavolo').rglob('*'):
  if f.is_file():assert z.read('plugins/Diavolo/'+f.relative_to(root/'dist/Diavolo').as_posix())==f.read_bytes()
(root/'docs/export-2.1.2.json').write_text(json.dumps({'file':str(archive),'bytes':archive.stat().st_size,'sha256':hashlib.sha256(archive.read_bytes()).hexdigest(),'ultimateIncluded':True,'cosmicSceneIncluded':False},indent=2))
print('Verified',archive,archive.stat().st_size,'bytes')
