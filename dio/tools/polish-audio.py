"""Create reproducible, level-matched PCM16 audio without changing cue timing.

Original assets/audio files are read-only inputs. Outputs go to audio-polished.
Uses Blender's bundled numpy and the workspace's existing ffmpeg executable.
"""
from pathlib import Path
import hashlib, json, math, subprocess, wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
FFMPEG = ROOT / '.tools/video-python/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'

def db(value):
    return 20 * math.log10(max(float(value), 1e-9))

def active_rms(samples, rate):
    mono_power = np.mean(samples.astype(np.float64) ** 2, axis=1)
    block = max(1, int(rate * .02))
    count = len(mono_power) // block
    if not count:
        return math.sqrt(float(np.mean(mono_power)))
    energy = mono_power[:count * block].reshape(count, block).mean(axis=1)
    threshold = max(10 ** (-48 / 10), float(np.percentile(energy, 95)) * 10 ** (-24 / 10))
    active = energy[energy >= threshold]
    return math.sqrt(float(active.mean())) if len(active) else 0.0

def category(name):
    if name in ('erase_bass_sfx',):
        return 'ambience', -25.0
    if 'voice' in name or name in ('death',):
        return 'voice', -20.0
    if name in ('barrage', 'time-stop-main', 'time-stop-r', 'time-stop', 'time-resume'):
        return 'mixed', -22.0
    return 'effect', -23.0

def process(project):
    source = project / 'assets/audio'
    destination = project / 'assets/audio-polished'
    destination.mkdir(exist_ok=True)
    report = []
    for path in sorted(source.glob('*.wav')):
        with wave.open(str(path), 'rb') as audio:
            channels, width, rate, frames = audio.getnchannels(), audio.getsampwidth(), audio.getframerate(), audio.getnframes()
            assert width == 2 and audio.getcomptype() == 'NONE', path
            original = np.frombuffer(audio.readframes(frames), '<i2').reshape(-1, channels).astype(np.float64) / 32768
        kind, target_db = category(path.stem)
        filters = f'highpass=f={65 if kind == "voice" else 30},equalizer=f=240:t=q:w=0.8:g=-1.5,equalizer=f=3500:t=q:w=0.8:g=-1,lowpass=f={min(16000, int(rate * .45))}'
        filtered = subprocess.run([str(FFMPEG), '-v', 'error', '-i', str(path), '-af', filters,
                                   '-f', 'f32le', '-acodec', 'pcm_f32le', '-ar', str(rate), '-ac', str(channels), 'pipe:1'],
                                  stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True).stdout
        samples = np.frombuffer(filtered, '<f4').reshape(-1, channels).astype(np.float64)
        assert len(samples) == frames, f'Filter changed length: {path}'
        rms = active_rms(samples, rate)
        gain_db = max(-24, min(12, target_db - db(rms))) if rms > 1e-8 else 0
        samples *= 10 ** (gain_db / 20)
        # A gentle peak knee leaves roughly 2 dB of per-stream headroom.
        magnitude = np.abs(samples)
        over = magnitude > .63
        samples[over] = np.sign(samples[over]) * (.63 + .16 * np.tanh((magnitude[over] - .63) / .16))
        fade = min(int(rate * .003), frames // 2)
        if fade:
            samples[:fade] *= np.linspace(0, 1, fade)[:, None]
            samples[-fade:] *= np.linspace(1, 0, fade)[:, None]
        pcm = np.rint(np.clip(samples, -1, 32767 / 32768) * 32768).astype('<i2')
        output = destination / path.name
        with wave.open(str(output), 'wb') as audio:
            audio.setnchannels(channels); audio.setsampwidth(2); audio.setframerate(rate); audio.writeframes(pcm.tobytes())
        with wave.open(str(output), 'rb') as audio:
            assert (audio.getnframes(), audio.getframerate(), audio.getnchannels()) == (frames, rate, channels)
        assert np.max(np.abs(pcm.astype(np.int32))) < 32767
        report.append(dict(file=path.name, category=kind, frames=frames, rate=rate, channels=channels,
                           duration=frames / rate, input_active_rms_db=round(db(active_rms(original, rate)), 2),
                           output_active_rms_db=round(db(active_rms(pcm.astype(np.float64) / 32768, rate)), 2),
                           gain_db=round(gain_db, 2), input_peak_db=round(db(np.max(np.abs(original))), 2),
                           output_peak_db=round(db(np.max(np.abs(pcm.astype(np.int32))) / 32768), 2),
                           source_sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                           output_sha256=hashlib.sha256(output.read_bytes()).hexdigest()))
    (project / 'docs/audio-polish-validation.json').write_text(json.dumps(dict(
        method='Gated 20ms RMS matching by category; gentle high-pass and EQ; soft peak knee; 3ms edge fades. Original PCM frames, channels and sample rate preserved.',
        live_listening_verified=False, clips=report), indent=2), encoding='utf-8')
    print(f'{project.name}: {len(report)} clips processed; original timing preserved; no clipped output samples.', flush=True)

if __name__ == '__main__':
    for project in (ROOT, (Path(__file__).resolve().parents[2] / 'diavolo')):
        process(project)
