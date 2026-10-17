"""Build the campus viewer's data from an OSM snapshot. Run --fetch to refresh it.

Only source geometry is used. Roads without widths are marked as estimates.
Building heights come from OSM tags, or from pipeline/building-levels.json where OSM
lacks or misrecords them (each entry records its basis); storey counts are turned
into heights with per-category storey heights. No fabricated campus objects are added.
"""
from __future__ import annotations

import argparse
import json
import math
import re
import urllib.request
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

from pyproj import Transformer
from shapely import make_valid
from shapely.geometry import Point, Polygon, MultiPolygon, LineString, mapping
from shapely.ops import polygonize, unary_union, transform

ROOT = Path(__file__).resolve().parents[1]
PIPELINE = Path(__file__).resolve().parent


def polygons(geometry):
    if geometry.geom_type == 'Polygon':
        return [geometry]
    if hasattr(geometry, 'geoms'):
        return [p for g in geometry.geoms for p in polygons(g)]
    return []


def render_polygons(geometry, kind, has_parent=False, has_children=False):
    # Independent ring simplification breaks shared parent/child interfaces. Keep the
    # actual difference vertices for every level of a physical building family.
    preserve_interfaces = kind == 'building' and (has_parent or has_children)
    result = []
    for p in polygons(geometry):
        if p.area < .05:
            continue
        if not preserve_interfaces:
            p = p.simplify(.08, preserve_topology=True)
        result.append([[[round(x, 2), round(z, 2)] for x, z in ring.coords] for ring in [p.exterior, *p.interiors]])
    return result


def facade_polygons(geometry):
    # Legacy facade layout is derived before centimetre quantization, from the same
    # post-difference geometry as the physical footprint. Preserve ring/point order.
    return render_polygons(geometry, 'building')


def number(value):
    if not value:
        return None
    match = re.fullmatch(r'\s*(\d+(?:\.\d+)?)\s*(m|meters|metres|ft)?\s*', str(value))
    if not match:
        return None
    result = float(match[1]) * (0.3048 if match[2] == 'ft' else 1)
    return result if math.isfinite(result) and result > 0 else None


def classify(tags):
    name = tags.get('name:zh') or tags.get('name', '')
    amenity = tags.get('amenity', '')
    leisure = tags.get('leisure', '')
    building = tags.get('building', '')
    if tags.get('barrier') in ('gate', 'entrance') or re.search(r'(东|西|南|北|正)门$', name):
        return 'entrance', None, 'name/tag'
    if amenity == 'library' or '图书' in name:
        return 'library', None, 'name/tag'
    if amenity in ('hospital', 'clinic', 'doctors', 'pharmacy') or '医院' in name:
        return 'medical', None, 'name/tag'
    if amenity in ('restaurant', 'fast_food', 'food_court', 'cafe', 'bar'):
        return 'dining', 'cafe' if amenity == 'cafe' else 'restaurant', 'amenity'
    if '食堂' in name or '餐厅' in name:
        return 'dining', 'restaurant', 'name'
    if building in ('dormitory', 'apartments') or re.search(r'(学知苑|硕丰苑|博翰苑|留学生|宿舍)', name):
        return 'residence', None, 'name/tag'
    if leisure in ('sports_centre', 'stadium', 'pitch', 'track', 'swimming_pool', 'fitness_station') or building in ('stadium', 'grandstand'):
        sub = 'swimming' if tags.get('sport') == 'swimming' or leisure == 'swimming_pool' else leisure if leisure in ('pitch', 'track') else 'gym'
        return 'sport', sub, 'leisure/building'
    if amenity in ('parking', 'parking_entrance'):
        return 'transport', 'parking', 'amenity'
    if amenity == 'bicycle_parking':
        return 'transport', 'bicycle', 'amenity'
    if tags.get('highway') == 'bus_stop' or amenity == 'bus_station' or tags.get('shelter_type') == 'public_transport':
        return 'transport', 'bus', 'tag'
    if amenity in ('toilets', 'atm', 'bank', 'post_office', 'parcel_locker'):
        sub = 'atm' if amenity in ('atm', 'bank') else 'parcel' if amenity in ('post_office', 'parcel_locker') else 'toilets'
        return 'service', sub, 'amenity'
    if tags.get('shop') or amenity == 'marketplace':
        return 'service', 'shop', 'shop/amenity'
    if tags.get('tourism') in ('museum', 'artwork', 'attraction') or tags.get('historic'):
        return 'landscape', 'landmark', 'tag'
    if re.search(r'科研|实验|研究|创新', name) or tags.get('office') == 'research':
        return 'research', None, 'name/tag'
    if tags.get('office') or re.search(r'行政|办公', name):
        return 'administration', None, 'name/tag'
    if tags.get('natural') == 'water' or tags.get('waterway'):
        return 'landscape', 'water', 'tag'
    if tags.get('tourism') in ('museum', 'artwork', 'attraction') or tags.get('historic'):
        return 'landscape', 'landmark', 'tag'
    if leisure in ('park', 'garden') or tags.get('landuse') in ('grass', 'forest', 'meadow'):
        return 'landscape', 'garden', 'tag'
    if tags.get('highway') and name:
        return 'transport', 'road', 'highway'
    if building in ('school', 'university'):
        return 'teaching', 'classroom', 'building'
    if building == 'commercial':
        return 'service', 'commercial', 'building'
    if tags.get('natural') == 'tree_row':
        return 'landscape', 'tree_row', 'natural'
    if tags.get('landuse') in ('construction', 'brownfield'):
        return 'other', tags['landuse'], 'landuse'
    if name == '器材室':
        return 'other', 'equipment', 'name'
    if amenity or tags.get('tourism'):
        return 'service', 'service', 'tag'
    return ('other', None, 'name') if name and not building else ('unknown', None, 'unknown')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--fetch', action='store_true')
    parser.add_argument('--map', required=True, help='Map ID from pipeline/maps.json')
    args = parser.parse_args()
    configs = json.loads((PIPELINE / 'maps.json').read_text(encoding='utf-8'))
    if args.map not in configs:
        parser.error('Unknown map; configure its source and projection in pipeline/maps.json first.')
    config = configs[args.map]
    source_url = config['source']
    raw_path = PIPELINE / 'raw' / args.map / 'osm-campus.json'
    meta_path = raw_path.with_suffix('.meta.json')
    if args.fetch:
        raw_path.parent.mkdir(parents=True, exist_ok=True)
        request = urllib.request.Request(source_url, headers={'User-Agent': 'UESTC-Map educational campus viewer'})
        with urllib.request.urlopen(request, timeout=60) as response:
            raw_path.write_bytes(response.read())
        meta_path.write_text(json.dumps({'retrievedDateUtc': datetime.now(timezone.utc).date().isoformat(),
            'source': source_url, 'queryFile': 'osm-campus.query.txt'}, indent=2) + '\n', encoding='utf-8', newline='\n')
        raw_path.with_name('osm-campus.query.txt').write_text(source_url + '\n', encoding='utf-8', newline='\n')
    if not raw_path.is_file() or not meta_path.is_file():
        raise FileNotFoundError('Committed snapshot and its metadata are required. Refresh explicitly with --fetch.')
    snapshot_meta = json.loads(meta_path.read_text(encoding='utf-8'))
    raw = json.loads(raw_path.read_text(encoding='utf-8'))
    elements = {(e['type'], e['id']): e for e in raw['elements']}
    categories = json.loads((PIPELINE / 'categories.json').read_text(encoding='utf-8'))
    cat_by_id = {c['id']: c for c in categories}
    enrichments = json.loads((PIPELINE / 'place-enrichments.json').read_text(encoding='utf-8'))
    enrichment_by_id = {e['sourceId']: e for e in enrichments['entries']}
    # Heights and storey counts that OSM lacks or records wrongly, plus storey heights by category.
    building_levels = json.loads((PIPELINE / 'building-levels.json').read_text(encoding='utf-8'))
    floor_heights = building_levels['floorHeights']
    levels_by_id = {e['sourceId']: e for e in building_levels['entries']}
    projector = Transformer.from_crs('EPSG:4326', f"EPSG:{config['epsg']}", always_xy=True)
    e0, n0 = projector.transform(*config['origin'])

    def project(lon, lat, z=None):
        e, n = projector.transform(lon, lat)
        try:
            return [v-e0 for v in e], [-(v-n0) for v in n]
        except TypeError:
            return e-e0, -(n-n0)

    def way_coords(element):
        node_ids = element.get('nodes', [])
        if any(('node', n) not in elements for n in node_ids):
            return []
        return [(elements['node', n]['lon'], elements['node', n]['lat']) for n in node_ids]

    geometry_issues = []
    member_ways = set()

    def geometry_for(element):
        typ = element['type']
        if typ == 'node':
            return transform(project, Point(element['lon'], element['lat']))
        if typ == 'way':
            coords = way_coords(element)
            if len(coords) < 2:
                return None
            area = len(coords) >= 4 and coords[0] == coords[-1]
            return transform(project, Polygon(coords) if area else LineString(coords))
        if element.get('tags', {}).get('type') != 'multipolygon':
            return None
        rings = {'outer': [], 'inner': []}
        for member in element.get('members', []):
            if member['type'] != 'way' or member.get('role', '') not in ('outer', 'inner', ''):
                continue
            way = elements.get(('way', member['ref']))
            coords = way_coords(way) if way else []
            if len(coords) < 2:
                geometry_issues.append(f"relation/{element['id']}: incomplete member {member['ref']}")
                return None
            rings[member.get('role') or 'outer'].append(LineString(coords))
        outside = list(polygonize(rings['outer']))
        if not outside:
            return None
        outer = unary_union(outside)
        inside = list(polygonize(rings['inner']))
        if inside:
            outer = outer.difference(unary_union(inside))
        for member in element.get('members', []):
            if member['type'] == 'way':
                member_ways.add(member['ref'])
        return transform(project, outer)

    boundary = geometry_for(elements[tuple(config['boundary'])])
    if not boundary or not boundary.is_valid:
        raise ValueError('Campus boundary is missing or invalid; refusing to create an empty map.')
    # Relations are assembled first, then their bare member ways are suppressed.
    records = []
    ordered = sorted(raw['elements'], key=lambda e: e['type'] != 'relation')
    for e in ordered:
        tags = dict(e.get('tags', {}))
        if not tags or (e['type'], e['id']) == tuple(config['boundary']):
            continue
        interesting = any(k in tags for k in ('building', 'building:part', 'highway', 'waterway', 'natural', 'landuse', 'leisure', 'amenity', 'shop', 'tourism', 'historic', 'office')) or tags.get('barrier') in ('gate', 'entrance')
        if not interesting or tags.get('building') == 'no':
            continue
        if e['type'] == 'way' and e['id'] in member_ways:
            continue
        geometry = geometry_for(e)
        if geometry is None or geometry.is_empty:
            continue
        if not geometry.is_valid:
            repaired = make_valid(geometry)
            if abs(repaired.area-geometry.area) > max(2, geometry.area * .01):
                geometry_issues.append(f"{e['type']}/{e['id']}: geometry repair exceeds tolerance; omitted")
                continue
            geometry = repaired
        if not geometry.intersects(boundary):
            continue
        is_building = bool(tags.get('building') or tags.get('building:part')) and geometry.area > 1
        if is_building:
            if geometry.intersection(boundary).area / geometry.area < .5:
                continue
            kind = 'building'
        elif tags.get('waterway') in ('dam', 'weir'):
            kind = tags['waterway']
        elif tags.get('natural') == 'water' or tags.get('waterway'):
            kind = 'water'
        elif tags.get('highway') and geometry.geom_type != 'Point':
            kind = 'road'
        elif tags.get('leisure') in ('pitch', 'track', 'stadium', 'swimming_pool', 'sports_centre') and geometry.area > 0:
            kind = 'sport'
        elif tags.get('landuse') in ('grass', 'forest', 'meadow', 'recreation_ground') or tags.get('leisure') in ('garden', 'park') or tags.get('natural') in ('wood', 'scrub', 'grassland'):
            kind = 'green'
        elif geometry.geom_type == 'Point':
            if not (tags.get('amenity') or tags.get('shop') or tags.get('tourism') or tags.get('barrier') in ('gate', 'entrance') or tags.get('highway') == 'bus_stop' or tags.get('historic')):
                continue
            kind = 'poi'
        elif tags.get('amenity') == 'parking':
            kind = 'parking'
        else:
            kind = 'poi'
        if not is_building:
            geometry = geometry.intersection(boundary)
        if geometry.is_empty:
            continue
        category, subcategory, category_basis = classify(tags)
        rec = {'sourceId': f"{e['type']}/{e['id']}", 'tags': tags, 'geometry': geometry, 'kind': kind, 'category': category, 'subcategory': subcategory, 'categoryBasis': category_basis}
        enrichment = enrichment_by_id.get(rec['sourceId'])
        if enrichment:
            rec['enrichment'] = enrichment
            if enrichment.get('name'):
                rec['originalName'] = tags.get('name:zh') or tags.get('name')
                tags['name'] = tags['name:zh'] = enrichment['name']
            if enrichment.get('category'):
                rec['category'] = enrichment['category']
                rec['subcategory'] = enrichment.get('subcategory')
                rec['categoryBasis'] = 'reviewed sources'
        records.append(rec)

    buildings = [r for r in records if r['kind'] == 'building']
    # A total outline and its individually mapped wings must not occupy the same volume.
    for child in buildings:
        candidates = [p for p in buildings if p is not child and p['geometry'].area > child['geometry'].area * 1.08 and p['geometry'].intersection(child['geometry']).area > child['geometry'].area * .98]
        if candidates:
            parent = min(candidates, key=lambda p: p['geometry'].area)
            child['parent'] = parent
    for r in records:
        tags = r['tags']
        if r['kind'] in ('road', 'green', 'water', 'dam', 'weir') and not tags.get('name'):
            r['entityId'] = None
        else:
            r['entityId'] = config['entityPrefix'] + ':' + r['sourceId'].replace('/', ':')

    # Geometry-only wings and podiums share their named building's semantic identity.
    # A named wing remains individually searchable; no fabricated building names are added.
    for r in buildings:
        if r['tags'].get('name') or any(r['tags'].get(k) for k in ('amenity', 'shop', 'office', 'leisure')):
            continue
        owner = r.get('parent')
        while owner and not owner['tags'].get('name'):
            owner = owner.get('parent')
        if not owner:
            named_children = [b for b in buildings if b['tags'].get('name') and r['geometry'].covers(b['geometry'].representative_point()) and b['geometry'].area < r['geometry'].area]
            owner = named_children[0] if len(named_children) == 1 else None
        if owner:
            r['entityId'], r['merged'] = owner['entityId'], True
            r['category'], r['subcategory'], r['categoryBasis'] = owner['category'], owner['subcategory'], 'building group'

    # POI and building with the same name at the same position represent one place.
    for r in records:
        if r['kind'] != 'poi' or not r['entityId']:
            continue
        # A site's representative point being indoors does not put the entire site
        # inside that building. Only points or fully contained areas get a host.
        hosts = [b for b in buildings if b['geometry'].covers(r['geometry'])]
        if hosts:
            host = min(hosts, key=lambda b: b['geometry'].area)
            r['locatedIn'] = host
            same_name = r['tags'].get('name') and r['tags'].get('name') == host['tags'].get('name')
            same_library = r['category'] == host['category'] == 'library'
            if same_name or same_library:
                r['entityId'] = host['entityId']
                r['merged'] = True

    # Explicitly reviewed identities join geometry and labels without changing
    # the raw snapshot or inventing individual names for a named site's wings.
    records_by_id = {r['sourceId']: r for r in records}
    for enrichment in enrichments['entries']:
        owner = records_by_id[enrichment['sourceId']]
        for source_id in enrichment.get('mergeSources', []):
            member = records_by_id[source_id]
            a, b = owner['geometry'], member['geometry']
            overlap = a.covers(b) if b.area == 0 else a.intersection(b).area / b.area >= .98
            if not overlap:
                raise ValueError(f"Reviewed association no longer fits: {source_id} -> {owner['sourceId']}")
            previous_id = member['entityId']
            for part in records:
                if part['entityId'] == previous_id and part is not owner:
                    part['entityId'], part['merged'] = owner['entityId'], True
                    part['category'], part['subcategory'], part['categoryBasis'] = owner['category'], owner['subcategory'], 'reviewed building group'
    entity_owners = {r['entityId']: r for r in records if r['entityId'] and not r.get('merged')}

    # Unclassified building wings inherit a known enclosing building's use.
    # Keep their names/identities and never override a wing's own known use or
    # apply this rule to shops, cafes, or other non-building POIs.
    for r in buildings:
        if r['category'] != 'unknown':
            continue
        parent = r.get('parent')
        while parent:
            owner = entity_owners.get(parent['entityId'])
            if owner and owner['category'] != 'unknown':
                r['category'], r['subcategory'] = owner['category'], owner['subcategory']
                r['categoryBasis'] = 'parent building'
                r['categorySourceId'] = owner.get('categorySourceId', owner['entityId'])
                break
            parent = parent.get('parent')

    features, entities = [], []
    for r in records:
        tags, geometry = r['tags'], r['geometry']
        height_value = number(tags.get('height'))
        levels = number(tags.get('building:levels'))
        levels_basis = 'osm' if levels else None
        extra = levels_by_id.get(r['sourceId']) if r['kind'] == 'building' else None
        if extra:
            if extra.get('height'):
                height_value = extra['height']
            if extra.get('levels'):
                levels, levels_basis = extra['levels'], extra['basis']
        r['finalHeight'], r['finalLevels'], r['levelsBasis'] = height_value, levels, levels_basis
        status = 'source' if height_value else 'estimated' if levels else 'unknown'
        floor_height = floor_heights.get(r['category'], floor_heights['unknown'])
        render_height = height_value or levels * floor_height if (height_value or levels) else 10.0
        if r['kind'] != 'building':
            render_height = 0
        render_geometry = geometry
        children = [b for b in buildings if b.get('parent') is r]
        if children:
            render_geometry = geometry.difference(unary_union([b['geometry'] for b in children]))
        width = None
        if r['kind'] in ('road', 'water', 'dam', 'weir') and render_geometry.area == 0:
            defaults = {'footway': 2.5, 'path': 2, 'steps': 2.5, 'cycleway': 3, 'service': 5, 'residential': 6, 'tertiary': 8, 'secondary': 10, 'pedestrian': 5}
            width = number(tags.get('width')) or defaults.get(tags.get('highway'), 4)
            render_geometry = render_geometry.buffer(width/2, cap_style=2, join_style=2).intersection(boundary)
        polys = render_polygons(render_geometry, r['kind'], r.get('parent') is not None, bool(children))
        feature_id = 'osm:' + r['sourceId'].replace('/', ':')
        r['featureId'] = feature_id
        anchor = geometry.representative_point() if geometry.area else geometry.interpolate(.5, normalized=True) if geometry.geom_type.endswith('LineString') else geometry.representative_point()
        r['anchor'] = [round(anchor.x, 2), round(render_height, 2), round(anchor.y, 2)]
        r['bounds'] = [round(v, 2) for v in geometry.bounds]
        if polys:
            features.append({'id': feature_id, 'entityId': r['entityId'], 'kind': r['kind'], 'polygons': polys, 'height': round(render_height, 2), 'heightStatus': status, 'levels': levels, 'levelsBasis': levels_basis, 'category': r['category'], 'sport': tags.get('sport'), 'roadType': tags.get('highway'), 'widthEstimated': width is not None and not tags.get('width')})
            water_tags = {key: tags[key] for key in ('water', 'waterway') if key in tags}
            if water_tags:
                features[-1]['sourceTags'] = water_tags
            facade = facade_polygons(render_geometry)
            if facade != polys:
                features[-1]['facadePolygons'] = facade

    def descendant_features(r):
        return [r['featureId']] + [fid for b in buildings if b.get('parent') is r for fid in descendant_features(b)]

    for r in records:
        if not r['entityId'] or r.get('merged'):
            continue
        tags = r['tags']
        category = cat_by_id[r['category']]
        subtype = next((c for c in category['children'] if c['id'] == r['subcategory']), None)
        original_name = tags.get('name:zh') or tags.get('name')
        fallback = '未命名建筑' if r['kind'] == 'building' else (subtype or category)['label']
        name = original_name or fallback + ' · ' + r['sourceId'].split('/')[1][-4:]
        aliases = list(dict.fromkeys(s for key in ('alt_name', 'alt_name:zh', 'short_name', 'name', 'name:en', 'ref') for s in tags.get(key, '').split(';') if s and s != name))
        enrichment = r.get('enrichment', {})
        aliases = list(dict.fromkeys(s for s in [*aliases, r.get('originalName'), *enrichment.get('aliases', [])] if s and s != name))
        parent = r.get('parent') or r.get('locatedIn')
        if parent:
            parent = entity_owners.get(parent['entityId'])
        if parent and parent['entityId'] == r['entityId']:
            parent = None
        grouped = [part for part in records if part['entityId'] == r['entityId']]
        entity_features = list(dict.fromkeys(fid for part in grouped for fid in descendant_features(part)))
        group_geometry = unary_union([part['geometry'] for part in grouped])
        entity_bounds = [round(v, 2) for v in group_geometry.bounds]
        description = enrichment.get('description') or tags.get('description:zh') or tags.get('description') or ''
        priority = 3 if r['category'] == 'library' or name in ('品学楼', '立人楼', '主楼') else 2 if original_name and r['category'] in ('entrance', 'dining', 'research', 'medical', 'sport') else 1 if original_name else 0
        entities.append({'id': r['entityId'], 'name': name, 'named': bool(original_name), 'aliases': aliases, 'category': r['category'], 'subcategory': r['subcategory'], 'icon': (subtype or category)['icon'], 'categoryBasis': r['categoryBasis'], 'kind': r['kind'], 'description': description, 'featureIds': entity_features, 'anchor': r['anchor'], 'bounds': entity_bounds, 'priority': priority, 'parentId': parent['entityId'] if parent else None, 'parentName': parent['tags'].get('name') if parent else None, 'locationStatus': 'point' if r['geometry'].geom_type == 'Point' else 'geometry', 'height': r.get('finalHeight'), 'levels': r.get('finalLevels'), 'levelsBasis': r.get('levelsBasis'), 'source': 'https://www.openstreetmap.org/' + r['sourceId'], 'updatedAt': elements[tuple([r['sourceId'].split('/')[0], int(r['sourceId'].split('/')[1])])].get('timestamp'), 'sourceTags': {k: tags[k] for k in ('building', 'amenity', 'leisure', 'sport', 'office', 'opening_hours', 'access', 'wheelchair', 'height', 'building:levels') if k in tags}})
        if r.get('categorySourceId'):
            entities[-1]['categorySourceId'] = r['categorySourceId']
        # Retain the tags that explain the additional source-based categories.
        entities[-1]['sourceTags'].update({k: tags[k] for k in ('building:part', 'natural', 'landuse') if k in tags})
        if enrichment:
            entities[-1]['enrichment'] = {'reviewedAt': enrichments['reviewedAt'], 'evidence': enrichment['evidence'], 'memberSources': ['https://www.openstreetmap.org/' + p['sourceId'] for p in grouped if p is not r]}
    bounds = list(boundary.bounds)
    stats = {'buildings': len(buildings), 'places': len(entities), 'namedPlaces': sum(e['named'] for e in entities), 'sourceHeights': sum(number(b['tags'].get('height')) is not None for b in buildings), 'levelHeights': sum(number(b['tags'].get('height')) is None and number(b['tags'].get('building:levels')) is not None for b in buildings), 'features': len(features), 'supplementedLevels': sum(1 for b in buildings if b.get('levelsBasis') not in (None, 'osm')), 'categories': dict(Counter(e['category'] for e in entities))}
    payload = {'meta': {'name': config['name'], 'date': snapshot_meta['retrievedDateUtc'], 'source': snapshot_meta['source'], 'license': 'ODbL 1.0 · © OpenStreetMap contributors', 'origin': config['origin'], 'crs': f"EPSG:{config['epsg']}", 'entityPrefix': config['entityPrefix'], 'axes': 'x east, y up, z south', 'bounds': bounds, 'areaM2': round(boundary.area), 'stats': stats, 'issues': geometry_issues, 'heightNote': '有实测高度的用实测值（OSM 标签，或 pipeline/building-levels.json 补的值）；只有层数的按类别层高估算（pipeline/building-levels.json 的 floorHeights）；OSM 缺层数或记错的楼按补充数据，来源记在 levelsBasis。地面为相对平面。'}, 'categories': categories, 'boundary': [[[round(x, 2), round(z, 2)] for x, z in boundary.exterior.coords]], 'features': features, 'entities': entities}
    assert entities and features and any(e['category'] == 'library' for e in entities), 'Expected campus objects missing'
    profiles = json.loads((PIPELINE / 'building-profiles.json').read_text(encoding='utf-8'))['buildings']
    for feature in payload['features']:
        profile = profiles.get(feature['id'])
        if feature['kind'] == 'building':
            feature['family'] = profile['style'] if profile else feature['category']
    from network import build_network
    network = build_network(elements, project, boundary, config['entityPrefix'], number)
    network['meta'] = {'date': snapshot_meta['retrievedDateUtc'], 'source': snapshot_meta['source'],
                       'license': payload['meta']['license'], 'axes': payload['meta']['axes']}
    output = ROOT / 'game' / 'data' / 'maps' / args.map / 'campus.json'
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(payload, ensure_ascii=False, separators=(',', ':')), encoding='utf-8', newline='\n')
    (output.parent / 'network.json').write_text(json.dumps(network, ensure_ascii=False, separators=(',', ':')), encoding='utf-8', newline='\n')
    print(json.dumps({'output': str(output), 'stats': stats, 'network': {k:len(v) for k,v in network.items() if isinstance(v,list)}, 'issues': geometry_issues}, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
