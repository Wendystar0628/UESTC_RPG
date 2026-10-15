"""Bind authored photo/family groups without assigning materials."""
from pathlib import Path
import argparse
import json

ROOT = Path(__file__).resolve().parents[1]
PIPELINE = Path(__file__).resolve().parent


def prepare():
    parser = argparse.ArgumentParser()
    parser.add_argument('--map', required=True)
    args = parser.parse_args()
    groups_by_map = json.loads((PIPELINE / 'building-profile-groups.json').read_text(encoding='utf-8'))
    if args.map not in groups_by_map:
        parser.error('No authored building-profile groups for this map.')
    source = groups_by_map[args.map]
    dataset = json.loads((ROOT / 'game/data/maps' / args.map / 'campus.json').read_text(encoding='utf-8'))
    entities = {entity['id']: entity for entity in dataset['entities']}
    buildings = {}
    for group in source['groups']:
        for feature_id in entities[group['entity']]['featureIds']:
            profile = {'style': group['style'], 'basis': 'photo-reference', 'reference': group['reference']}
            if 'referenceScope' in group:
                profile['referenceScope'] = group['referenceScope']
            buildings[feature_id] = profile
    for group in source['families']:
        for feature_id in entities[group['entity']]['featureIds']:
            buildings.setdefault(feature_id, {'style': group['style'], 'basis': 'campus-family'})
    output = {'version': source['version'],
              'note': 'Photos guide facade language and landmark silhouettes. Dimensions of details are inferred; campus GIS footprints and original height records are unchanged.',
              'buildings': buildings}
    (PIPELINE / 'building-profiles.json').write_text(json.dumps(output, ensure_ascii=False, indent=2) + '\n', encoding='utf-8', newline='\n')
    print(f'Prepared {len(buildings)} source feature profiles.')


if __name__ == '__main__':
    prepare()
