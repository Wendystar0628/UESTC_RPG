import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import type { Dataset, SceneFeature } from '../src/data/types';
import { architecturalDetails, type ArchitectureStyle } from '../src/scene/architecture';
import { pointInFootprint } from '../src/scene/geometry';

const courtyard: SceneFeature = {
  id: 'test:courtyard', entityId: null, kind: 'building', category: 'education', height: 12, heightStatus: 'source',
  polygons: [[[[0, 0], [40, 0], [40, 30], [25, 30], [25, 20], [0, 20], [0, 0]], [[8, 6], [8, 14], [18, 14], [18, 6], [8, 6]]]],
};
const styles: ArchitectureStyle[] = ['academic', 'dormitory', 'research', 'modern', 'library', 'sports', 'service', 'residential', 'utility'];
const data = JSON.parse(readFileSync(new URL('../public/data/qingshuihe/campus.json', import.meta.url), 'utf8')) as Dataset;

describe('procedural architectural detail', () => {
  it('generates finite, indexed, metre-scale geometry for closed, open, and courtyard footprints', () => {
    for (const style of styles) for (const closed of [true, false]) {
      const feature = closed ? courtyard : { ...courtyard, polygons: courtyard.polygons.map(rings => rings.map(ring => ring.slice(0, -1))) };
      const details = architecturalDetails(feature, false, style);
      expect(details.some(detail => detail.materialKey === 'glass')).toBe(true);
      expect(details.length).toBeLessThanOrEqual(8);
      for (const { geometry } of details) {
        const positions = geometry.getAttribute('position'), normals = geometry.getAttribute('normal'), uv = geometry.getAttribute('uv');
        expect(normals.count).toBe(positions.count);
        expect(uv.count).toBe(positions.count);
        expect(geometry.index!.count % 3).toBe(0);
        for (let i = 0; i < positions.count; i++) {
          expect(Number.isFinite(positions.getX(i) + positions.getY(i) + positions.getZ(i))).toBe(true);
          expect(Math.hypot(normals.getX(i), normals.getY(i), normals.getZ(i))).toBeCloseTo(1, 5);
          // All inferred rooftop detail fits in the camera's 1.2 m vertical allowance.
          expect(positions.getY(i)).toBeGreaterThanOrEqual(.5);
          expect(positions.getY(i)).toBeLessThanOrEqual(13.701);
        }
        geometry.dispose();
      }
    }
  });

  it('faces courtyard windows into the hole and keeps roof upper faces upward', () => {
    const details = architecturalDetails(courtyard, false, 'research');
    const glass = details.find(detail => detail.materialKey === 'glass')!.geometry;
    const position = glass.getAttribute('position'), normal = glass.getAttribute('normal');
    const courtyardWall = Array.from({ length: position.count }, (_, i) => i).filter(i => Math.abs(position.getX(i) - 8.083) < .005);
    expect(courtyardWall.length).toBeGreaterThan(0);
    expect(courtyardWall.every(i => normal.getX(i) > .99)).toBe(true);
    const roof = details.find(detail => detail.role === 'roof')!.geometry;
    const roofNormals = roof.getAttribute('normal');
    for (let i = 0; i < roofNormals.count; i++) expect(roofNormals.getY(i)).toBeGreaterThanOrEqual(0);
    details.forEach(detail => detail.geometry.dispose());
  });

  it('respects strict source-height mode and leaves tiny structures undecorated', () => {
    expect(architecturalDetails({ ...courtyard, heightStatus: 'estimated' }, true, 'academic')).toEqual([]);
    expect(architecturalDetails({ ...courtyard, heightStatus: 'unknown' }, true, 'academic')).toEqual([]);
    expect(architecturalDetails({ ...courtyard, height: .25 }, false, 'academic')).toEqual([]);
    expect(architecturalDetails({ ...courtyard, kind: 'green' }, false, 'academic')).toEqual([]);
    expect(architecturalDetails({ ...courtyard, polygons: [[[[0, 0], [3, 0], [3, 4], [0, 4]]]] }, false, 'academic')).toEqual([]);
    const source = architecturalDetails(courtyard, true, 'academic');
    expect(source.length).toBeGreaterThan(0);
    source.forEach(detail => detail.geometry.dispose());
    const sports = architecturalDetails({ ...courtyard, id: 'osm:way:217855275' }, false, 'sports');
    expect(sports.some(detail => detail.role === 'roof')).toBe(false);
    sports.forEach(detail => detail.geometry.dispose());
  });

  it('adds real balcony and equipment depth while strict source-height views omit inferred rooftop volume', () => {
    const dorm = architecturalDetails(courtyard, false, 'dormitory');
    const balcony = dorm.find(detail => detail.materialKey === 'terracotta')!.geometry;
    expect(balcony.boundingBox!.min.z).toBeLessThan(-.75);
    dorm.forEach(detail => detail.geometry.dispose());
    const detailed = architecturalDetails(courtyard, false, 'research');
    const strict = architecturalDetails(courtyard, true, 'research');
    const maxHeight = (items: ReturnType<typeof architecturalDetails>) => Math.max(...items.map(item => item.geometry.boundingBox!.max.y));
    expect(maxHeight(detailed)).toBeGreaterThan(13.4);
    expect(maxHeight(strict)).toBeLessThan(12.531);
    detailed.concat(strict).forEach(detail => detail.geometry.dispose());
  });

  it('gives modern buildings continuous glazing, projected slabs, and restrained accent panels', () => {
    const details = architecturalDetails(courtyard, false, 'modern');
    const glass = details.find(detail => detail.materialKey === 'glass')!.geometry.getAttribute('position');
    // At least one glass panel spans a facade, instead of repeating classroom-size openings.
    let longestPanel = 0;
    for (let i = 0; i < glass.count; i += 4) longestPanel = Math.max(longestPanel, Math.hypot(glass.getX(i + 3) - glass.getX(i), glass.getZ(i + 3) - glass.getZ(i)));
    expect(longestPanel).toBeGreaterThan(35);
    const slab = details.find(detail => detail.materialKey === 'facade-trim')!.geometry;
    expect(slab.boundingBox!.min.z).toBeLessThan(-.3);
    const accents = details.find(detail => detail.materialKey === 'facade-olive')!.geometry;
    // Accent colour occupies a narrow vertical region of one facade.
    expect(accents.boundingBox!.max.x - accents.boundingBox!.min.x).toBeLessThan(3);
    expect(accents.boundingBox!.max.z - accents.boundingBox!.min.z).toBeLessThan(.01);
    details.forEach(detail => detail.geometry.dispose());
  });

  it('builds raised roof rims inward around outer and courtyard edges, and omits them in strict mode', () => {
    const detailed = architecturalDetails(courtyard, false, 'academic');
    const rim = detailed.find(detail => detail.role === 'roof' && detail.materialKey === 'facade-trim')!.geometry;
    expect(rim.boundingBox!.min.y).toBeGreaterThan(12.5);
    expect(rim.boundingBox!.max.y).toBeCloseTo(13.03, 4);
    const p = rim.getAttribute('position'), n = rim.getAttribute('normal');
    let upwardQuads = 0;
    for (let i = 0; i < p.count; i += 4) {
      if (n.getY(i) < .99) continue;
      upwardQuads++;
      const x = (p.getX(i) + p.getX(i + 1) + p.getX(i + 2) + p.getX(i + 3)) / 4;
      const z = (p.getZ(i) + p.getZ(i + 1) + p.getZ(i + 2) + p.getZ(i + 3)) / 4;
      expect(pointInFootprint(x, z, courtyard.polygons)).toBe(true);
    }
    expect(upwardQuads).toBeGreaterThanOrEqual(10);
    const strict = architecturalDetails(courtyard, true, 'academic');
    expect(strict.some(detail => detail.role === 'roof' && detail.materialKey === 'facade-trim')).toBe(false);
    const small = architecturalDetails({ ...courtyard, polygons: [[[[0, 0], [10, 0], [10, 10], [0, 10]]]] }, false, 'academic');
    expect(small.some(detail => detail.role === 'roof' && detail.materialKey === 'facade-trim')).toBe(false);
    detailed.concat(strict, small).forEach(detail => detail.geometry.dispose());
  });

  it('keeps full-campus detail within a practical merged-geometry triangle budget', () => {
    let triangles = 0, buildingCount = 0, parapetCount = 0, dormEquipmentCount = 0;
    for (const feature of data.features.filter(feature => feature.kind === 'building')) {
      const style: ArchitectureStyle = /^osm:way:14310906(?:3[6-9]|40)$/.test(feature.id) ? 'modern'
        : feature.category === 'residence' ? 'dormitory'
        : feature.category === 'research' ? 'research'
          : feature.category === 'sport' ? 'sports'
            : feature.category === 'library' ? 'library'
              : feature.category === 'unknown' ? 'utility' : 'academic';
      const details = architecturalDetails(feature, false, style);
      if (details.length) buildingCount++;
      if (details.some(detail => detail.role === 'roof' && detail.materialKey === 'facade-trim')) parapetCount++;
      if (style === 'dormitory' && details.some(detail => detail.role === 'roof' && detail.materialKey === 'plinth')) dormEquipmentCount++;
      for (const detail of details) {
        triangles += detail.geometry.index!.count / 3;
        expect(detail.geometry.boundingBox!.max.y).toBeLessThanOrEqual(feature.height + .5 + 1.201);
        if (detail.role === 'roof' && detail.materialKey === 'facade-trim') {
          const normals = detail.geometry.getAttribute('normal');
          for (let i = 0; i < normals.count; i++) expect(normals.getY(i)).toBeGreaterThanOrEqual(0);
        }
        detail.geometry.dispose();
      }
    }
    console.info(`Architectural coverage: ${buildingCount} buildings, ${triangles.toLocaleString()} added triangles`);
    console.info(`Raised parapets: ${parapetCount} buildings; dormitory roof equipment: ${dormEquipmentCount} buildings`);
    expect(buildingCount).toBeGreaterThan(220);
    expect(triangles).toBeLessThan(300_000);
  });
});
