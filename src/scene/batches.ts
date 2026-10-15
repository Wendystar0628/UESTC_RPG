import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import type { Dataset, SceneFeature } from '../data/types';
import { materialPresets, resolveMaterial, type MaterialKey, type SurfaceRole } from '../data/appearance';
import { featureSurfaces } from './geometry';
import { architecturalDetails } from './architecture';
import { landmarkRoofDetails } from './landmarkRoofs';
import { libraryRoofDetails } from './libraryRoof';
import { buildingProfile } from '../data/buildingProfiles';

export interface SurfacePart {
  feature: SceneFeature;
  featureIndex: number;
  role: SurfaceRole;
  materialKey: MaterialKey;
  geometry: THREE.BufferGeometry;
  detail: boolean;
}
interface Range { featureIndex: number; start: number; count: number; active: boolean; }
export interface SceneBatch {
  id: string;
  kind: SceneFeature['kind'];
  materialKey: MaterialKey;
  geometry: THREE.BufferGeometry;
  edges: THREE.EdgesGeometry | null;
  detail: boolean;
  baseColors: Float32Array;
  ranges: Range[];
}
export interface CompiledScene {
  batches: SceneBatch[];
  partsByFeature: Map<string, SurfacePart[]>;
  triangleCount: number;
  dispose: () => void;
}

/** One immutable geometry compilation per dataset/height variant, never per selection. */
export function compileScene(data: Dataset, strict: boolean): CompiledScene {
  const partsByFeature = new Map<string, SurfacePart[]>();
  const buckets = new Map<string, SurfacePart[]>();
  data.features.forEach((feature, featureIndex) => {
    const parts: SurfacePart[] = [];
    const profile = buildingProfile(feature);
    const landmark = landmarkRoofDetails(feature, strict);
    const details = feature.kind === 'building' ? architecturalDetails(feature, strict, profile.style)
      .filter(surface => !landmark.length || surface.role !== 'roof') : [];
    const surfaces = [
      ...featureSurfaces(feature, strict).map(surface => ({ ...surface, materialKey: resolveMaterial(feature, surface.role).key, detail: false })),
      ...[...details, ...landmark, ...libraryRoofDetails(feature, strict)].map(surface => ({ ...surface, detail: true })),
    ];
    for (const surface of surfaces) {
      const materialKey = surface.materialKey;
      const geometry = surface.geometry;
      const positions = geometry.getAttribute('position');
      const uvs = geometry.getAttribute('uv');
      const repeatSize = materialPresets[materialKey].repeatSizeM;
      const colors = new Float32Array(positions.count * 3);
      for (let i = 0; i < positions.count; i++) {
        uvs.setXY(i, uvs.getX(i) / repeatSize, uvs.getY(i) / repeatSize);
        // Near-ground occlusion gradient ties separate facade details to the massing.
        const shade = surface.role === 'wall' ? .84 + .16 * Math.min(1, positions.getY(i) / 6) : 1;
        colors.set([shade, shade, shade], i * 3);
      }
      geometry.setAttribute('color', new THREE.BufferAttribute(colors, 3));
      geometry.setAttribute('featureIndex', new THREE.Float32BufferAttribute(new Float32Array(positions.count).fill(featureIndex), 1));
      geometry.computeBoundingBox();
      const center = geometry.boundingBox!.getCenter(new THREE.Vector3());
      // Coarse spatial grouping keeps nearby buildings independently cullable.
      const cell = `${Math.floor(center.x / 700)},${Math.floor(center.z / 700)}`;
      const key = `${feature.kind}/${materialKey}/${surface.detail ? 'detail' : 'shell'}/${cell}`;
      const part = { feature, featureIndex, role: surface.role, materialKey, geometry, detail: surface.detail };
      parts.push(part);
      const group = buckets.get(key) || [];
      group.push(part); buckets.set(key, group);
    }
    partsByFeature.set(feature.id, parts);
  });

  let triangleCount = 0;
  const batches: SceneBatch[] = [];
  for (const [id, parts] of buckets) {
    const geometry = mergeGeometries(parts.map(part => part.geometry), false);
    if (!geometry) throw new Error(`Cannot combine campus material batch: ${id}`);
    geometry.computeBoundingSphere();
    let start = 0;
    const ranges = parts.map(part => {
      const count = part.geometry.getAttribute('position').count;
      const range = { featureIndex: part.featureIndex, start, count, active: true };
      start += count;
      return range;
    });
    const kind = parts[0].feature.kind;
    const count = geometry.index?.count || geometry.getAttribute('position').count;
    triangleCount += count / 3;
    batches.push({
      id, kind, materialKey: parts[0].materialKey, geometry, ranges,
      detail: parts[0].detail,
      edges: kind === 'building' && !parts[0].detail ? new THREE.EdgesGeometry(geometry, 40) : null,
      baseColors: new Float32Array(geometry.getAttribute('color').array),
    });
  }
  return {
    batches, partsByFeature, triangleCount,
    dispose() {
      batches.forEach(batch => { batch.geometry.dispose(); batch.edges?.dispose(); });
      partsByFeature.forEach(parts => parts.forEach(part => part.geometry.dispose()));
    },
  };
}

/** Update only the color spans whose emphasis changed; position/normal/UV stay intact. */
export function updateBatchEmphasis(batch: SceneBatch, active: (featureIndex: number) => boolean): void {
  const colors = batch.geometry.getAttribute('color') as THREE.BufferAttribute;
  let changed = false;
  for (const range of batch.ranges) {
    const next = active(range.featureIndex);
    if (next === range.active) continue;
    range.active = next;
    for (let i = range.start * 3; i < (range.start + range.count) * 3; i++) {
      colors.array[i] = batch.baseColors[i] * (next ? 1 : .7);
    }
    colors.addUpdateRange(range.start * 3, range.count * 3);
    changed = true;
  }
  if (changed) colors.needsUpdate = true;
}
