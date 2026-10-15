"""Regression checks for the source migration and reproducible offline rebuild."""
from pathlib import Path
import hashlib
import json
import subprocess
import sys
import unittest

ROOT = Path(__file__).resolve().parents[2]


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
        # Golden canonical hashes of the pre-migration data; no dependency on removed legacy paths.
        golden = {'entities': '77735dc7f66e1fdda61b0b469093be859865c13ccb56d51e59ecf645b1103757',
                  'boundary': '4fece2373778f8510feb2ae0e5158f118161533b77ff01e8d5d2de8962aae3dc',
                  'features': '088f589eb4eb71bc88c7fe924370dc9ef529ff262d24821009bed7e2bce07061'}
        for key, expected in golden.items():
            value = migrated[key]
            if key == 'features':
                value = [{k: v for k, v in feature.items() if k != 'family'} for feature in value]
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


if __name__ == '__main__':
    unittest.main()
