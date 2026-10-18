"""从 OSM 元素导出道路、水系、桥、涵洞、树与过街点等中心线数据（只保留源线与源 ID，边界裁剪留给使用方）。

用法：不单独运行，由 pipeline/build_dataset.py 调用 build_network
"""
from __future__ import annotations


def build_network(elements, project, boundary, entity_prefix, number):
    from shapely.geometry import LineString

    output = {key: [] for key in ('roads', 'waterways', 'bridges', 'culverts', 'trees')}
    output['treeRows'] = []
    output['crossings'] = []
    widths = {'footway': 2.5, 'path': 2, 'steps': 2.5, 'cycleway': 3, 'service': 5,
              'residential': 6, 'tertiary': 8, 'secondary': 10, 'pedestrian': 5}
    for (kind, identifier), element in sorted(elements.items()):
        tags = element.get('tags', {})
        feature_id = f'{entity_prefix}:source:{kind}:{identifier}'
        if kind == 'node':
            x, z = project(element['lon'], element['lat'])
            position = [round(x, 2), 0, round(z, 2)]
            if tags.get('natural') == 'tree':
                output['trees'].append({'id': feature_id, 'position': position,
                                        'species': tags.get('species:zh') or tags.get('species') or tags.get('genus')})
            if tags.get('highway') == 'crossing':
                output['crossings'].append({'id': feature_id, 'position': position, 'props': tags})
            continue
        if kind != 'way':
            continue
        keys = []
        if tags.get('highway'): keys.append('roads')
        if tags.get('waterway'): keys.append('waterways')
        if tags.get('bridge') not in (None, 'no'): keys.append('bridges')
        if tags.get('tunnel') == 'culvert': keys.append('culverts')
        if tags.get('natural') == 'tree_row': keys.append('treeRows')
        if not keys:
            continue
        nodes = [elements.get(('node', n)) for n in element.get('nodes', [])]
        if len(nodes) < 2 or any(n is None for n in nodes):
            continue
        points = [project(n['lon'], n['lat']) for n in nodes]
        line = LineString(points)
        # Preserve the source line and source ID. Boundary clipping remains the consumer's job.
        width = number(tags.get('width')) or widths.get(tags.get('highway'), 4)
        entry = {'id': feature_id, 'line': {'points': [[round(x, 2), round(z, 2)] for x, z in points]},
                 'width': width, 'props': {**tags, 'widthEstimated': number(tags.get('width')) is None,
                                           'intersectsCampus': bool(boundary.intersects(line))}}
        for key in keys:
            output[key].append(entry)
    return output
