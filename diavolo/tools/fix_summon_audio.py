"""Replace the two borrowed The World cues with the local ASBR stand cue."""
from pathlib import Path
import hashlib, importlib.util, json, shutil, subprocess, wave

root = Path(__file__).resolve().parents[1]
dio = (Path(__file__).resolve().parents[2] / 'dio')
catalog = json.loads((root / 'docs/audio-catalog.json').read_text())
cue = next(c for c in catalog if c['bank'] == 'asbr_battle' and c['index'] == 29)
assert cue['name'] == 'S_std_appear'
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
prior = {p.name: sha(p) for p in (root / 'assets/audio-polished').glob('*.wav')}
dio_hash = sha(dio / 'assets/audio-polished/stand-summon.wav')
source = root / cue['path']
summon = root / 'assets/audio/summon_sfx.wav'
subprocess.run([str(dio / '.tools/vgmstream/vgmstream-cli.exe'), '-s', '29', '-o', str(summon), str(source)], check=True, capture_output=True)
shutil.copy2(summon, root / 'assets/audio/jump_sfx.wav')
with wave.open(str(summon), 'rb') as wav:
    assert (wav.getnchannels(), wav.getsampwidth(), wav.getframerate(), wav.getnframes()) == (1, 2, 48000, 67023)
spec = importlib.util.spec_from_file_location('polish_audio', dio / 'tools/polish-audio.py')
polish = importlib.util.module_from_spec(spec)
spec.loader.exec_module(polish)
polish.process(root)
after = {p.name: sha(p) for p in (root / 'assets/audio-polished').glob('*.wav')}
changed = [name for name in prior if prior[name] != after[name]]
assert set(changed) <= {'summon_sfx.wav', 'jump_sfx.wav'}
assert after['summon_sfx.wav'] != dio_hash
assert sha(dio / 'assets/audio-polished/stand-summon.wav') == dio_hash
report = dict(cue=cue['name'], bank=str(source), bank_sha256=sha(source), index=29,
              provenance='Original ASBR generic stand-appearance cue; not a claim of a unique anime King Crimson recording.',
              seconds=67023/48000, replaced_files=['summon_sfx.wav','jump_sfx.wav'], changed_files=changed,
              sha256={name:after[name] for name in ('summon_sfx.wav','jump_sfx.wav')},
              dio_unchanged=True, other_diavolo_audio_unchanged=True, live_listening_verified=False)
(root / 'docs/summon-audio-fix.json').write_text(json.dumps(report, indent=2))
print(json.dumps(report, indent=2))
