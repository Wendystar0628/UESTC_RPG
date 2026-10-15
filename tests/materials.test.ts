import { describe, expect, it } from 'vitest';
import { readFileSync, existsSync } from 'node:fs';
import type { Dataset, SceneFeature } from '../src/data/types';
import { materialPresets, resolveMaterial, type MaterialKey } from '../src/data/appearance';
import { compileScene, updateBatchEmphasis } from '../src/scene/batches';
import { featureSurfaces } from '../src/scene/geometry';

const data = JSON.parse(readFileSync(new URL('../public/data/qingshuihe/campus.json', import.meta.url), 'utf8')) as Dataset;
const fixture: SceneFeature = {
  id: 'test:rectangle', entityId: null, kind: 'building', category: 'unknown', height: 12, heightStatus: 'unknown',
  polygons: [[[[0, 0], [20, 0], [20, 10], [0, 10], [0, 0]], [[5, 3], [5, 7], [15, 7], [15, 3], [5, 3]]]],
};

describe('surface coverage and assets', () => {
  it('assigns a valid preset to every renderable surface without inventing facade evidence', () => {
    for (const feature of data.features) {
      for (const role of feature.kind === 'building' ? ['wall', 'roof'] as const : ['ground'] as const) {
        const appearance = resolveMaterial(feature, role);
        expect(materialPresets[appearance.key]).toBeDefined();
        if (appearance.status !== 'generic') expect(appearance.source).toBeTruthy();
      }
    }
    expect(resolveMaterial(fixture, 'wall').status).toBe('generic');
    const concrete = data.features.find(f => f.id === 'osm:way:844548980')!;
    expect(resolveMaterial(concrete, 'wall')).toMatchObject({ key: 'wall-concrete', status: 'source-tag' });
  });

  it('ships both texture sizes for every material family', () => {
    const families = new Set(Object.values(materialPresets).map(preset => preset.family));
    for (const family of families) for (const size of [512, 1024]) for (const role of ['color', 'normal']) {
      expect(existsSync(new URL(`../public/assets/materials-v1/${size}/${family}-${role}.webp`, import.meta.url))).toBe(true);
    }
  });
});

describe('geometry lifecycle and metre-scale UVs', () => {
  it('gives walls perimeter UVs without changing metre proportions or normals', () => {
    const surfaces = featureSurfaces(fixture, false);
    const wall = surfaces.find(surface => surface.role === 'wall')!.geometry;
    const uv = wall.getAttribute('uv');
    expect(uv.getX(0)).toBeCloseTo(0);
    expect(uv.getY(1)).toBeCloseTo(12);
    expect(uv.getX(2)).toBeCloseTo(20);
    expect(wall.getAttribute('normal').getZ(0)).toBeCloseTo(-1);
    const roof = surfaces.find(surface => surface.role === 'roof')!.geometry;
    expect(roof.getAttribute('normal').getY(0)).toBeCloseTo(1);
    surfaces.forEach(surface => surface.geometry.dispose());
  });

  it('keeps roof UVs fixed when changing display height', () => {
    const normal = featureSurfaces(fixture, false);
    const flat = featureSurfaces(fixture, true);
    expect(Array.from(normal[0].geometry.getAttribute('uv').array)).toEqual(Array.from(flat[0].geometry.getAttribute('uv').array));
    normal.concat(flat).forEach(surface => surface.geometry.dispose());
  });

  it('compiles once, then emphasis changes only color buffers', () => {
    const compiled = compileScene(data, false);
    // Facades and landmark roofs share six detail materials across coarse cells.
    expect(compiled.batches.length).toBeLessThan(190);
    expect(compiled.triangleCount).toBeLessThan(350_000);
    expect(compiled.triangleCount).toBeGreaterThan(0);
    for (const batch of compiled.batches) {
      const geometry = batch.geometry;
      const position = geometry.getAttribute('position').array;
      const normal = geometry.getAttribute('normal').array;
      const uv = geometry.getAttribute('uv').array;
      const index = geometry.index;
      updateBatchEmphasis(batch, () => false);
      updateBatchEmphasis(batch, () => true);
      expect(batch.geometry).toBe(geometry);
      expect(geometry.getAttribute('position').array).toBe(position);
      expect(geometry.getAttribute('normal').array).toBe(normal);
      expect(geometry.getAttribute('uv').array).toBe(uv);
      expect(geometry.index).toBe(index);
      expect(Array.from(geometry.getAttribute('color').array)).toEqual(Array.from(batch.baseColors));
      expect(materialPresets[batch.materialKey as MaterialKey]).toBeDefined();
    }
    compiled.dispose();
  });
});
