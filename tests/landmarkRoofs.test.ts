import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import type { Dataset } from '../src/data/types';
import { landmarkRoofDetails } from '../src/scene/landmarkRoofs';

const data = JSON.parse(readFileSync(new URL('../public/data/qingshuihe/campus.json', import.meta.url), 'utf8')) as Dataset;
const ids = ['osm:way:217855275', 'osm:way:217855279', 'osm:way:217855276'];

describe('photo-informed sports roof geometry', () => {
  it('keeps documented gym height, compatible indexed attributes and bounded complexity', () => {
    let triangles = 0;
    for (const id of ids) {
      const feature = data.features.find(f => f.id === id)!;
      const recordedHeight = feature.height;
      const parts = landmarkRoofDetails(feature, false);
      expect(parts.length).toBeGreaterThan(0);
      for (const { geometry } of parts) {
        const position = geometry.getAttribute('position');
        expect(geometry.index).not.toBeNull();
        expect(position.count).toBe(geometry.getAttribute('normal').count);
        expect(position.count).toBe(geometry.getAttribute('uv').count);
        expect(Array.from(position.array).every(Number.isFinite)).toBe(true);
        triangles += geometry.index!.count / 3;
        geometry.computeBoundingBox();
        if (id === ids[0]) expect(geometry.boundingBox!.max.y).toBeLessThanOrEqual(26.57);
        geometry.dispose();
      }
      expect(feature.height).toBe(recordedHeight);
      expect(landmarkRoofDetails(feature, true)).toEqual([]);
    }
    expect(triangles).toBeGreaterThan(2000);
    expect(triangles).toBeLessThan(6000);
  });

  it('leaves the field-side centre of the C-shaped stand open', () => {
    const feature = data.features.find(f => f.id === ids[2])!;
    const parts = landmarkRoofDetails(feature, false);
    const roof = parts[0].geometry, position = roof.getAttribute('position'), index = roof.index!;
    // Central and eastern field points must not be covered by any roof triangle.
    for (const [x, z] of [[80, 775], [100, 745], [130, 790]]) {
      let covered = false;
      for (let i = 0; i < index.count; i += 3) {
        const a = index.getX(i), b = index.getX(i + 1), c = index.getX(i + 2);
        const ax = position.getX(a), az = position.getZ(a), bx = position.getX(b), bz = position.getZ(b), cx = position.getX(c), cz = position.getZ(c);
        const edge = (px: number, pz: number, qx: number, qz: number) => (x - qx) * (pz - qz) - (px - qx) * (z - qz);
        const e1 = edge(ax, az, bx, bz), e2 = edge(bx, bz, cx, cz), e3 = edge(cx, cz, ax, az);
        if (!((e1 < 0 || e2 < 0 || e3 < 0) && (e1 > 0 || e2 > 0 || e3 > 0))) covered = true;
      }
      expect(covered).toBe(false);
    }
    parts.forEach(p => p.geometry.dispose());
  });
});
