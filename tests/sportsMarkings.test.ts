import { describe, expect, it } from 'vitest';
import { readFileSync } from 'node:fs';
import type { Dataset } from '../src/data/types';
import { createSportsMarkings } from '../src/scene/SportsMarkings';
import { pointInFootprint } from '../src/scene/geometry';

const data = JSON.parse(readFileSync(new URL('../public/data/qingshuihe/campus.json', import.meta.url), 'utf8')) as Dataset;

describe('sports markings', () => {
  it('draws only suitable source courts, with every painted vertex clear of its source boundary', () => {
    const markings = createSportsMarkings(data), positions = markings.geometry.getAttribute('position');
    expect(markings.counts).toEqual({ soccer: 7, basketball: 40, tennis: 21 });
    expect(markings.stats.triangles).toBeLessThan(11000);
    expect(markings.stats.goals).toBe(4);
    for (const range of markings.ranges) {
      const feature = data.features.find(feature => feature.id === range.featureId)!;
      const ring = feature.polygons[0][0];
      for (let index = range.start; index < range.start + range.count; index++) {
        const x = positions.getX(index), z = positions.getZ(index);
        expect(positions.getY(index)).toBeCloseTo(.28);
        expect(pointInFootprint(x, z, feature.polygons)).toBe(true);
        for (let edge = 0; edge < ring.length; edge++) {
          const a = ring[edge], b = ring[(edge + 1) % ring.length], dx = b[0] - a[0], dz = b[1] - a[1];
          const squared = dx * dx + dz * dz;
          const t = squared ? Math.max(0, Math.min(1, ((x - a[0]) * dx + (z - a[1]) * dz) / squared)) : 0;
          expect(Math.hypot(x - a[0] - t * dx, z - a[1] - t * dz)).toBeGreaterThanOrEqual(.4);
        }
      }
    }
    console.log('Sports markings:', markings.stats);
    markings.geometry.dispose(); markings.goalGeometry?.dispose();
  });
});
