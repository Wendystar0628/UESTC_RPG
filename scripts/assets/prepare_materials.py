"""Prepare the viewer's small shared texture library (development only).

Three CC0 scan materials are fetched from Poly Haven. Grass, rubber and water
are generic procedural surfaces, not photos or surveyed campus appearances.
Run with the developer Python environment: python scripts/assets/prepare_materials.py
"""
from pathlib import Path
import io
import json
import math
import random
import urllib.request
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageStat, ImageChops
from prepare_architecture_materials import prepare as architecture_materials

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'public/assets/materials-v1'
RAW = ROOT / 'data/raw/materials'
SCAN_ASSETS = {
    'concrete': 'concrete_wall_006',
    'asphalt': 'aerial_asphalt_01',
    'paving': 'pavement_03',
}
USER_AGENT = 'UESTCMap/0.2 educational campus material preparation'


def fetch(url):
    request = urllib.request.Request(url, headers={'User-Agent': USER_AGENT})
    with urllib.request.urlopen(request, timeout=35) as response:
        return response.read()


def write_family(name, maps):
    for size in (512, 1024):
        folder = OUT / str(size)
        folder.mkdir(parents=True, exist_ok=True)
        for role, image in maps.items():
            output = image.resize((size, size), Image.Resampling.LANCZOS)
            output.save(folder / f'{name}-{role}.webp', quality=87 if role == 'color' else 94, method=6)


def scanned():
    RAW.mkdir(parents=True, exist_ok=True)
    records = []
    for family, asset in SCAN_ASSETS.items():
        manifest_path = RAW / f'{asset}.json'
        if not manifest_path.exists():
            manifest_path.write_bytes(fetch('https://api.polyhaven.com/files/' + asset))
        files = json.loads(manifest_path.read_text(encoding='utf-8'))
        maps = {}
        for role, key in [('color', 'Diffuse'), ('normal', 'nor_gl')]:
            record = files[key]['1k']['jpg']
            source = RAW / f'{asset}-{role}.jpg'
            if not source.exists():
                source.write_bytes(fetch(record['url']))
            maps[role] = Image.open(source).convert('RGB')
        # Reduce broad dirt patches rather than making every surface look weathered.
        gray = maps['color'].convert('L')
        if family == 'concrete':
            # Remove captured broad illumination bands; retain the scan's fine surface detail.
            gray = ImageChops.subtract(gray, gray.filter(ImageFilter.GaussianBlur(24)), scale=1, offset=128)
        mean = ImageStat.Stat(gray).mean[0]
        target, contrast = {'concrete': (229, .38), 'asphalt': (177, .55), 'paving': (217, .62)}[family]
        adapted = gray.point([max(0, min(255, round(target + (value - mean) * contrast))) for value in range(256)])
        maps['color'] = Image.merge('RGB', (adapted, adapted, adapted))
        write_family(family, maps)
        records.append({'family': family, 'asset': asset, 'source': 'https://polyhaven.com/a/' + asset, 'license': 'CC0-1.0', 'use': 'generic surface; diffuse levels adapted for readable campus visualization, not a calibrated campus photograph', 'maps': ['color', 'normal']})
        print('Prepared', family, flush=True)
    return records


def procedural():
    size = 512
    rng = random.Random(27)
    grain = Image.new('RGB', (size, size))
    grain.putdata([(v, v, v) for v in [rng.randint(195, 236) for _ in range(size * size)]])
    grass = Image.new('RGB', (size, size)); draw = ImageDraw.Draw(grass)
    for y in range(size):
        for x in range(size):
            variation = rng.uniform(-10, 10) + 4 * math.sin(x * math.pi / 32) * math.cos(y * math.pi / 64)
            draw.point((x, y), (int(131 + variation), int(156 + variation), int(112 + variation)))
    draw = ImageDraw.Draw(grass)
    for _ in range(32000):
        x, y = rng.randrange(size), rng.randrange(size)
        draw.line((x, y, x + rng.choice([-1, 0, 1]), y + rng.randrange(2, 6)), fill=(rng.randrange(122, 153), rng.randrange(148, 172), rng.randrange(101, 129)))
    grass = grass.filter(ImageFilter.GaussianBlur(.3))
    flat_normal = Image.new('RGB', (size, size), (128, 128, 255))
    water = Image.new('RGB', (size, size)); normal = Image.new('RGB', (size, size))
    water_pixels, normal_pixels = [], []
    for y in range(size):
        for x in range(size):
            u, v = 2 * math.pi * x / size, 2 * math.pi * y / size
            wave = .6 * math.sin(u * 5 + v * 2) + .25 * math.sin(v * 9 - u * 3) + .15 * math.cos(u * 12 + v * 7)
            dx = .6 * 5 * math.cos(u * 5 + v * 2) - .25 * 3 * math.cos(v * 9 - u * 3) - .15 * 12 * math.sin(u * 12 + v * 7)
            dy = .6 * 2 * math.cos(u * 5 + v * 2) + .25 * 9 * math.cos(v * 9 - u * 3) - .15 * 7 * math.sin(u * 12 + v * 7)
            nx, ny, nz = -dx * .045, -dy * .045, 1
            length = math.sqrt(nx * nx + ny * ny + nz * nz)
            normal_pixels.append(tuple(round((a / length * .5 + .5) * 255) for a in (nx, ny, nz)))
            t = round(237 + wave * 1.8)
            water_pixels.append((t, t, t))
    water.putdata(water_pixels); normal.putdata(normal_pixels)
    write_family('grass', {'color': grass, 'normal': flat_normal})
    write_family('rubber', {'color': grain.filter(ImageFilter.GaussianBlur(.25)), 'normal': flat_normal})
    write_family('water', {'color': water, 'normal': normal})
    return [{'family': name, 'source': 'scripts/assets/prepare_materials.py', 'license': 'project-created', 'use': 'generic procedural surface', 'maps': ['color', 'normal']} for name in ('grass', 'rubber', 'water')]


if __name__ == '__main__':
    records = scanned() + procedural() + architecture_materials()
    (ROOT / 'data/material-sources.json').write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding='utf-8')
    raw_map = json.loads((ROOT / 'data/raw/osm-campus.json').read_text(encoding='utf-8'))
    campus = json.loads((ROOT / 'public/data/qingshuihe/campus.json').read_text(encoding='utf-8'))
    ids = {feature['id'] for feature in campus['features']}
    appearance_path = ROOT / 'data/appearances.json'
    appearance = json.loads(appearance_path.read_text(encoding='utf-8')) if appearance_path.exists() else {'overrides': {}}
    sources = {}
    for element in raw_map['elements']:
        feature_id = f"osm:{element['type']}:{element['id']}"
        tags = {k: v for k, v in element.get('tags', {}).items() if k in ('building:material', 'building:colour', 'roof:material', 'roof:colour', 'surface')}
        if feature_id in ids and tags:
            sources[feature_id] = {'tags': tags, 'source': f"https://www.openstreetmap.org/{element['type']}/{element['id']}"}
    appearance['sourceTags'] = sources
    appearance_path.write_text(json.dumps(appearance, ensure_ascii=False, indent=2), encoding='utf-8')
    files = list(OUT.rglob('*.webp'))
    print(f'Prepared {len(files)} textures; {sum(p.stat().st_size for p in files)/1024:.0f} KiB total. Sources: data/material-sources.json')
