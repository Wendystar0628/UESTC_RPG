import { describe, expect, it } from 'vitest';
import { readFileSync } from 'node:fs';
import type { Dataset } from '../src/data/types';
import { createLandscapeLayout } from '../src/scene/landscape';
import { pointInFootprint } from '../src/scene/geometry';

const data = JSON.parse(readFileSync(new URL('../public/data/qingshuihe/campus.json', import.meta.url), 'utf8')) as Dataset;

describe('procedural campus planting', () => {
  it('keeps reproducible tree and shrub envelopes on green land, clear of all occupied surfaces', () => {
    const layout = createLandscapeLayout(data);
    expect(layout).toEqual(createLandscapeLayout(data));
    expect(layout.trees.length).toBeGreaterThanOrEqual(700);
    expect(layout.trees.length).toBeLessThanOrEqual(1300);
    expect(layout.shrubs.length).toBeGreaterThan(100);
    const green = data.features.filter(feature => feature.kind === 'green');
    const occupied = data.features.filter(feature => ['building', 'water', 'road', 'sport', 'parking'].includes(feature.kind))
      .flatMap(feature => feature.polygons.map(rings => {
        const xs = rings[0].map(point => point[0]), zs = rings[0].map(point => point[1]);
        return { rings, minX: Math.min(...xs), maxX: Math.max(...xs), minZ: Math.min(...zs), maxZ: Math.max(...zs) };
      }));
    const started = performance.now();
    createLandscapeLayout(data);
    const generationMs = performance.now() - started;
    for (const plant of [...layout.trees, ...layout.shrubs]) {
      expect(green.some(feature => pointInFootprint(plant.x, plant.z, feature.polygons))).toBe(true);
      for (let sample = 0; sample < 9; sample++) {
        const radius = sample === 0 ? 0 : plant.radius;
        const angle = sample / 8 * Math.PI * 2;
        const x = plant.x + Math.cos(angle) * radius, z = plant.z + Math.sin(angle) * radius;
        expect(occupied.some(item => x >= item.minX && x <= item.maxX && z >= item.minZ && z <= item.maxZ
          && pointInFootprint(x, z, [item.rings]))).toBe(false);
      }
    }
    expect(new Set(layout.trees.map(tree => tree.variant)).size).toBe(3);
    expect(layout.trees.every(tree => tree.height >= 5 && tree.height <= 11)).toBe(true);
    console.log(`Landscape: ${layout.trees.length} trees, ${layout.shrubs.length} shrubs; generation ${Math.round(generationMs)}ms`);
  });
});
