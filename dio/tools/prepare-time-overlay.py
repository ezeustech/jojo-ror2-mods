"""Split the supplied effect and bake keyed GPU atlases; no runtime video decoding."""
from pathlib import Path
import subprocess
import sys
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / '.tools/video-python'))
import imageio_ffmpeg

SOURCE = Path(str(Path(__file__).resolve().parents[1] / "local-inputs" / 'Custom Time StopZa Warudo Effect (Green Screen) - SMX (1080p).mp4'))
ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
width, height = 960, 540
decoded = subprocess.run([ffmpeg, '-i', str(SOURCE), '-vf', f'scale={width}:{height}',
    '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-'], capture_output=True, check=True)
frames = np.frombuffer(decoded.stdout, dtype=np.uint8).reshape(-1, height, width, 3)
output = ROOT / 'UnityProject/Assets/Feedback'
preview = ROOT / 'assets/video'
preview.mkdir(parents=True, exist_ok=True)
for name, start, end in [('Stop', 13, 33), ('Resume', 47, 66)]:
    atlas = Image.new('RGBA', (width * 5, height * 4), (0, 0, 0, 0))
    for index, frame in enumerate(frames[start:end]):
        rgb = frame.astype(np.float32) / 255
        # Chroma key against the measured pure green background; recover colors
        # from partially transparent edges so they do not retain a green fringe.
        difference = np.maximum(np.maximum(rgb[..., 0], rgb[..., 2]), 1 - rgb[..., 1])
        alpha = np.clip((difference - .055) / .32, 0, 1)
        alpha = alpha * alpha * (3 - 2 * alpha)
        key = np.array([0., 1., 0.], dtype=np.float32)
        color = np.clip((rgb - (1-alpha[..., None]) * key) / np.maximum(alpha[..., None], .001), 0, 1)
        rgba = np.dstack([color, alpha])
        tile = Image.fromarray(np.round(rgba * 255).astype(np.uint8), 'RGBA')
        atlas.paste(tile, (index % 5 * width, index // 5 * height))
    atlas.save(output / f'Dio{name}Atlas.png')
    subprocess.run([ffmpeg, '-y', '-i', str(SOURCE), '-vf',
        f'trim=start_frame={start}:end_frame={end},setpts=PTS-STARTPTS', '-an',
        '-c:v', 'libx264', '-crf', '16', str(preview / f'time-{name.lower()}.mp4')], capture_output=True, check=True)
    # Review the actual keyed atlas over a grey checkerboard.
    tile = atlas.crop((2*width, height, 3*width, 2*height)).resize((640, 360))
    background = Image.new('RGBA', tile.size, (80, 80, 80, 255))
    from PIL import ImageDraw
    draw = ImageDraw.Draw(background)
    for y in range(0, 360, 40):
        for x in range(0, 640, 40):
            if (x//40+y//40)%2: draw.rectangle((x,y,x+39,y+39), fill=(125,125,125,255))
    background.alpha_composite(tile)
    background.convert('RGB').save(ROOT / f'docs/time-{name.lower()}-key-preview.png')
    print(name, 'frames', end-start, 'duration', (end-start)/24, 'atlas', atlas.size)
