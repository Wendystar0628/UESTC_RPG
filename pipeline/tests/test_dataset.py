"""Regression checks for the source migration and reproducible offline rebuild."""
from pathlib import Path
import hashlib
import json
import subprocess
import sys
import unittest

from shapely.geometry import Polygon
from shapely.ops import unary_union
from pyproj import Transformer

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'pipeline'))
from build_dataset import render_polygons, facade_polygons


class DatasetTests(unittest.TestCase):
    def test_offline_rebuild_is_byte_identical(self):
        command = [sys.executable, '-X', 'utf8', str(ROOT / 'pipeline/build_dataset.py'), '--map', 'qingshuihe']
        subprocess.run(command, check=True, capture_output=True)
        paths = [ROOT / 'game/data/maps/qingshuihe' / name for name in ('campus.json', 'network.json')]
        first = [hashlib.sha256(path.read_bytes()).hexdigest() for path in paths]
        subprocess.run(command, check=True, capture_output=True)
        self.assertEqual(first, [hashlib.sha256(path.read_bytes()).hexdigest() for path in paths])

    def test_independent_building_outers_keep_member_geometry_under_the_relation_entity(self):
        campus = json.loads((ROOT / 'game/data/maps/qingshuihe/campus.json').read_bytes())
        raw = json.loads((ROOT / 'pipeline/raw/qingshuihe/osm-campus.json').read_bytes())
        elements = {(e['type'], e['id']): e for e in raw['elements']}
        relation = next(e for e in raw['elements'] if e['type'] == 'relation' and e.get('tags', {}).get('name') == '南门')
        entity = next(e for e in campus['entities'] if e['source'].endswith(f"/relation/{relation['id']}"))
        expected_ids = [f"osm:way:{member['ref']}" for member in relation['members']]
        parts = [f for f in campus['features'] if f['entityId'] == entity['id']]
        self.assertCountEqual(expected_ids, entity['featureIds'])
        self.assertCountEqual(expected_ids, [part['id'] for part in parts])
        self.assertNotIn(f"osm:relation:{relation['id']}", {f['id'] for f in campus['features']})
        self.assertEqual(relation['tags']['name'], entity['name'])
        projector = Transformer.from_crs('EPSG:4326', campus['meta']['crs'], always_xy=True)
        e0, n0 = projector.transform(*campus['meta']['origin'])
        for part in parts:
            way = elements['way', int(part['id'].split(':')[-1])]
            nodes = [elements['node', node] for node in way['nodes']]
            projected = [projector.transform(node['lon'], node['lat']) for node in nodes]
            expected = Polygon([(round(e-e0, 2), round(-(n-n0), 2)) for e, n in projected])
            actual = unary_union([Polygon(poly[0], poly[1:]) for poly in part['polygons']])
            self.assertTrue(actual.equals(expected), part['id'])
        metadata = json.loads((ROOT / 'pipeline/raw/qingshuihe/osm-campus.meta.json').read_bytes())
        self.assertEqual(metadata['retrievedDateUtc'], campus['meta']['date'])

    def test_water_tags_are_exact_source_values_and_do_not_guess_a_centreline(self):
        raw = json.loads((ROOT / 'pipeline/raw/qingshuihe/osm-campus.json').read_bytes())
        source = {f"osm:{entry['type']}:{entry['id']}": entry.get('tags', {}) for entry in raw['elements']}
        campus = json.loads((ROOT / 'game/data/maps/qingshuihe/campus.json').read_bytes())
        for feature in campus['features']:
            expected = {key: source[feature['id']][key] for key in ('water', 'waterway') if key in source[feature['id']]}
            self.assertEqual(expected, feature.get('sourceTags', {}), feature['id'])
            if expected.get('waterway') in ('dam', 'weir'):
                self.assertEqual(expected['waterway'], feature['kind'], feature['id'])
        river = next(feature for feature in campus['features'] if feature['id'] == 'osm:way:851437317')
        self.assertEqual({'water': 'river'}, river['sourceTags'])
        network = json.loads((ROOT / 'game/data/maps/qingshuihe/network.json').read_bytes())
        self.assertNotIn('qsh:source:way:851437317', {line['id'] for line in network['waterways']})

    def test_network_preserves_snapshot_counts(self):
        network = json.loads((ROOT / 'game/data/maps/qingshuihe/network.json').read_bytes())
        for key, count in [('bridges', 77), ('culverts', 15), ('trees', 146), ('treeRows', 13), ('crossings', 67)]:
            self.assertEqual(count, len(network[key]), key)
            self.assertEqual(count, len({entry['id'] for entry in network[key]}), key)

    def test_unknown_map_fails_instead_of_using_default_campus(self):
        run = subprocess.run([sys.executable, str(ROOT / 'pipeline/build_dataset.py'), '--map', 'unknown-fixture'], capture_output=True)
        self.assertNotEqual(0, run.returncode)


class MultipartGeometryTests(unittest.TestCase):
    def test_facade_uses_unquantized_legacy_simplification_with_holes_and_order(self):
        geometry = Polygon([(0, 0), (1, .084), (2, .007), (2, 4), (0, 4)],
                           [[(.5, 1), (.5, 2), (1.5, 2), (1.5, 1)]])
        expected = [[[[round(x, 2), round(z, 2)] for x, z in ring.coords]
                     for ring in [geometry.simplify(.08, preserve_topology=True).exterior,
                                  *geometry.simplify(.08, preserve_topology=True).interiors]]]
        self.assertEqual(expected, facade_polygons(geometry))
        physical = render_polygons(geometry, 'building', True)
        self.assertNotEqual(expected, facade_polygons(Polygon(physical[0][0], physical[0][1:])),
                            'Fixture must distinguish simplification before and after centimetre rounding.')

    @staticmethod
    def _family(reverse=False, legacy=False):
        records = {
            'root': {'geometry': Polygon([(-1, -1), (21, -1), (21, 6), (-1, 6)])},
            'left': {'geometry': Polygon([(0, 0), (10, .02), (10, 5), (0, 5)])},
            'right': {'geometry': Polygon([(10, .02), (20, 0), (20, 5), (10, 5)])},
            'leaf': {'geometry': Polygon([(2, 1), (3, 1.02), (4, 1), (4, 3), (2, 3)])},
        }
        records['left']['parent'] = records['root']
        records['right']['parent'] = records['root']
        records['leaf']['parent'] = records['left']
        ordered = list(records.items())
        if reverse:
            ordered.reverse()
        output = {}
        for source, record in ordered:
            children = [child for _, child in ordered if child.get('parent') is record]
            geometry = record['geometry']
            if children:
                geometry = geometry.difference(unary_union([child['geometry'] for child in children]))
            output[source] = render_polygons(geometry, 'building',
                False if legacy else record.get('parent') is not None,
                False if legacy else bool(children))
        return records, output

    @staticmethod
    def _union(output):
        return unary_union([Polygon(poly[0], poly[1:]) for raw in output.values() for poly in raw])

    def test_parent_family_keeps_the_shared_vertex_instead_of_creating_a_false_hole(self):
        records, fixed = self._family()
        _, legacy = self._family(legacy=True)
        before = self._union(legacy)
        self.assertEqual(1, len(before.interiors), 'The fixture must expose the independent-simplification failure.')
        after = self._union(fixed)
        self.assertTrue(after.equals(records['root']['geometry']))
        self.assertEqual(0, len(after.interiors))
        for source in ('root', 'left', 'right'):
            self.assertIn([10.0, .02], [point for poly in fixed[source] for ring in poly for point in ring])

    def test_middle_and_leaf_of_a_multilevel_parent_family_keep_their_actual_vertices(self):
        _, fixed = self._family()
        self.assertIn([3.0, 1.02], [point for poly in fixed['left'] for ring in poly for point in ring])
        self.assertIn([3.0, 1.02], fixed['leaf'][0][0])
        self.assertEqual(6, len(fixed['leaf'][0][0]))  # Five source vertices and the closing point.

    def test_standalone_building_and_nonbuilding_keep_the_existing_simplified_output(self):
        geometry = Polygon([(0, 0), (10, .02), (20, 0), (20, 5), (0, 5)])
        expected = [[[[0.0, 0.0], [20.0, 0.0], [20.0, 5.0], [0.0, 5.0], [0.0, 0.0]]]]
        self.assertEqual(expected, render_polygons(geometry, 'building'))
        for kind in ('road', 'water', 'green'):
            self.assertEqual(expected, render_polygons(geometry, kind, True, True), kind)

    def test_reversing_source_order_keeps_every_serialized_coordinate_in_order(self):
        _, forward = self._family()
        _, reverse = self._family(reverse=True)
        self.assertEqual(json.dumps(forward, sort_keys=True, separators=(',', ':')),
                         json.dumps(reverse, sort_keys=True, separators=(',', ':')))


if __name__ == '__main__':
    unittest.main()
