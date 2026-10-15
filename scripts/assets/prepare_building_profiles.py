"""Bind documented photo groups to existing OSM feature ids; do not change GIS facts."""
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[2]
ALBUM = {'title': '成电！成电！ · 2023 校园实拍', 'url': 'https://news.uestc.edu.cn/info/1013/5994.htm'}
SPORTS = {'title': '最美场馆 · 2023 大运会体育场馆实拍', 'url': 'https://news.uestc.edu.cn/info/1053/23081.htm'}


def prepare():
    dataset = json.loads((ROOT / 'public/data/qingshuihe/campus.json').read_text(encoding='utf-8'))
    groups = [
        ('qsh:way:687370083', 'academic', 'wall-concrete', ALBUM),
        ('qsh:way:806848396', 'library', 'wall-sandstone', ALBUM),
        ('qsh:relation:11043355', 'academic', 'wall-concrete', ALBUM),
        ('qsh:relation:11185938', 'academic', 'wall-concrete', ALBUM),
        ('qsh:relation:11665640', 'research', 'wall-sandstone', ALBUM),
        ('qsh:relation:14153711', 'research', 'wall-concrete', ALBUM),
        ('qsh:relation:11032065', 'research', 'wall-sandstone', ALBUM),
        ('qsh:way:844548980', 'academic', 'wall-sandstone', {'title': '宣邦楼 · 2023 在建实拍参考', 'url': ALBUM['url']}),
        ('qsh:way:883187548', 'modern', 'wall-concrete', {'title': '文科楼群 · 2026 年 7 月施工实拍', 'url': 'https://news.uestc.edu.cn/info/1162/42221.htm'}),
        ('qsh:way:552816777', 'academic', 'wall-concrete', ALBUM),
        ('qsh:way:217855275', 'sports', 'wall-concrete', SPORTS),
        ('qsh:way:217855279', 'sports', 'wall-concrete', SPORTS),
        ('qsh:way:217855276', 'sports', 'wall-concrete', SPORTS),
    ]
    buildings = {}
    entities = {entity['id']: entity for entity in dataset['entities']}
    for entity_id, style, wall, reference in groups:
        if entity_id not in entities:
            raise ValueError('Missing reference target: ' + entity_id)
        for feature_id in entities[entity_id]['featureIds']:
            buildings[feature_id] = {'style': style, 'wall': wall, 'roof': 'roof-metal' if style == 'sports' else 'roof-concrete',
                                    'basis': 'photo-reference', 'reference': reference}
            if entity_id == 'qsh:way:552816777':
                buildings[feature_id]['referenceScope'] = 'roof'
    # Match usage across all parts of the international research centre; no photo claim.
    for feature_id in entities['qsh:relation:17732622']['featureIds']:
        buildings.setdefault(feature_id, {'style': 'research', 'wall': 'wall-sandstone', 'roof': 'roof-concrete', 'basis': 'campus-family'})
    output = {'version': '2026-09-17', 'note': 'Photos guide facade language and landmark silhouettes. Dimensions of details are inferred; campus GIS footprints and original height records are unchanged.', 'buildings': buildings}
    (ROOT / 'data/building-profiles.json').write_text(json.dumps(output, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'Prepared {len(buildings)} feature profiles; {sum(p["basis"] == "photo-reference" for p in buildings.values())} photo-reference assignments.')


if __name__ == '__main__':
    prepare()
