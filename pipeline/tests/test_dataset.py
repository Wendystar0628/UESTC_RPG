"""Regression checks for the source migration and reproducible offline rebuild."""
from pathlib import Path
import hashlib
import json
import subprocess
import sys
import unittest

from shapely.geometry import Polygon
from shapely.ops import unary_union

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

    def test_source_geometry_and_entity_ids_survive_migration(self):
        migrated = json.loads((ROOT / 'game/data/maps/qingshuihe/campus.json').read_bytes())
        # Entity/boundary goldens retain the pre-migration baseline. Feature geometry now
        # preserves source parent-family interfaces. Optional facade coordinates are
        # checked separately; all other fields retain the prior golden.
        golden = {'entities': '77735dc7f66e1fdda61b0b469093be859865c13ccb56d51e59ecf645b1103757',
                  'boundary': '4fece2373778f8510feb2ae0e5158f118161533b77ff01e8d5d2de8962aae3dc',
                  'features': '6307012d4ce9de0a2be509107affe89e8a33694621d4ce9ede71ad956b2e513e'}
        for key, expected in golden.items():
            value = migrated[key]
            if key == 'features':
                value = [{k: v for k, v in feature.items() if k not in ('family', 'facadePolygons')} for feature in value]
            digest = hashlib.sha256(json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=False).encode()).hexdigest()
            self.assertEqual(expected, digest, key)
        metadata = json.loads((ROOT / 'pipeline/raw/qingshuihe/osm-campus.meta.json').read_bytes())
        self.assertEqual(metadata['retrievedDateUtc'], migrated['meta']['date'])

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
