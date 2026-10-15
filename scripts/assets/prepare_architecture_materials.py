"""Original seamless, metre-scaled surface patterns; no campus photos are redistributed."""
from pathlib import Path
import json
import math
import random
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'public/assets/materials-v1'


def normal_from_height(height, strength=2.5):
    size = height.width
    pixels = height.load()
    result = Image.new('RGB', (size, size))
    normals = []
    for y in range(size):
        for x in range(size):
            dx = (pixels[(x + 1) % size, y] - pixels[(x - 1) % size, y]) / 255 * strength
            dy = (pixels[x, (y + 1) % size] - pixels[x, (y - 1) % size]) / 255 * strength
            length = math.sqrt(dx * dx + dy * dy + 1)
            normals.append(tuple(round((value / length * .5 + .5) * 255) for value in (-dx, -dy, 1)))
    result.putdata(normals)
    return result


def prepare():
    size = 512
    rng = random.Random(1709)
    maps = {}
    for family in ('stone-panel', 'brick', 'metal', 'glass'):
        color = Image.new('RGB', (size, size), (233, 233, 231))
        height = Image.new('L', (size, size), 200)
        draw, relief = ImageDraw.Draw(color), ImageDraw.Draw(height)
        if family in ('stone-panel', 'brick'):
            w, h = (128, 64) if family == 'stone-panel' else (64, 32)
            joint = 1 if family == 'stone-panel' else 2
            draw.rectangle((0, 0, size, size), fill=(199, 198, 192))
            relief.rectangle((0, 0, size, size), fill=135)
            for row, y in enumerate(range(0, size, h)):
                shift = w // 2 if family == 'brick' and row % 2 else 0
                for x in range(-w, size + w, w):
                    value = rng.randrange(224, 246)
                    draw.rectangle((x + shift + joint, y + joint, x + shift + w - joint, y + h - joint), fill=(value, value, value - 2))
                    relief.rectangle((x + shift + joint, y + joint, x + shift + w - joint, y + h - joint), fill=200)
            for _ in range(24000):
                x, y = rng.randrange(size), rng.randrange(size)
                value = color.getpixel((x, y))
                delta = rng.randrange(-6, 7)
                draw.point((x, y), fill=tuple(max(0, min(255, channel + delta)) for channel in value))
        elif family == 'metal':
            for x in range(size):
                stripe = x % 64
                value = 209 if stripe < 2 else 240 if stripe < 5 else 231 + int(3 * math.sin(x * math.pi / 32))
                draw.line((x, 0, x, size), fill=(value, value, value))
                relief.line((x, 0, x, size), fill=240 if stripe < 4 else 195)
        else:
            # Subtle broad reflection gradient; window geometry supplies actual frames.
            for y in range(size):
                value = round(227 + 14 * math.cos(y / size * 2 * math.pi))
                draw.line((0, y, size, y), fill=(value - 9, value - 3, value))
        maps[family] = {'color': color, 'normal': normal_from_height(height.filter(ImageFilter.GaussianBlur(.6)))}
    for family, textures in maps.items():
        for size in (512, 1024):
            (OUT / str(size)).mkdir(parents=True, exist_ok=True)
            for role, texture in textures.items():
                texture.resize((size, size), Image.Resampling.LANCZOS).save(OUT / str(size) / f'{family}-{role}.webp', quality=92, method=6)
    return [{'family': name, 'source': 'scripts/assets/prepare_architecture_materials.py', 'license': 'project-created',
             'use': 'Procedural campus visual language; not a measured or photographed facade texture.', 'maps': ['color', 'normal']} for name in maps]


if __name__ == '__main__':
    records = prepare()
    path = ROOT / 'data/material-sources.json'
    old = json.loads(path.read_text(encoding='utf-8'))
    names = {record['family'] for record in records}
    path.write_text(json.dumps([record for record in old if record['family'] not in names] + records, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print('Prepared 4 architectural material families, 16 textures.')
