import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import type { PolygonRings, Ring, SceneFeature } from '../data/types';
import type { SurfaceRole } from '../data/appearance';

export function makeShape(rings: PolygonRings): THREE.Shape {
  const shape = new THREE.Shape(rings[0].map(([x, z]) => new THREE.Vector2(x, -z)));
  shape.holes = rings.slice(1).map(ring => new THREE.Path(ring.map(([x, z]) => new THREE.Vector2(x, -z))));
  return shape;
}

function signedArea(ring: Ring): number {
  return ring.reduce((area, [x, z], i) => {
    const next = ring[(i + 1) % ring.length];
    return area + x * next[1] - next[0] * z;
  }, 0) / 2;
}

function openRing(input: Ring, outside: boolean): Ring {
  const ring = input.slice();
  if (ring.length > 1 && ring[0][0] === ring.at(-1)![0] && ring[0][1] === ring.at(-1)![1]) ring.pop();
  if ((signedArea(ring) > 0) !== outside) ring.reverse();
  return ring;
}

/** Construct hard-edged walls with continuous perimeter-distance UVs in metres. */
function walls(rings: PolygonRings, base: number, height: number): THREE.BufferGeometry {
  const position: number[] = [], normal: number[] = [], uv: number[] = [], indices: number[] = [];
  rings.forEach((input, ringIndex) => {
    const ring = openRing(input, ringIndex === 0);
    let perimeter = 0;
    ring.forEach(([x, z], i) => {
      const [nextX, nextZ] = ring[(i + 1) % ring.length];
      const dx = nextX - x, dz = nextZ - z;
      const length = Math.hypot(dx, dz);
      if (length < .001) return;
      const start = position.length / 3;
      position.push(x, base, z, x, base + height, z, nextX, base + height, nextZ, nextX, base, nextZ);
      for (let k = 0; k < 4; k++) normal.push(dz / length, 0, -dx / length);
      uv.push(perimeter, 0, perimeter, height, perimeter + length, height, perimeter + length, 0);
      indices.push(start, start + 1, start + 2, start, start + 2, start + 3);
      perimeter += length;
    });
  });
  const geometry = new THREE.BufferGeometry();
  geometry.setAttribute('position', new THREE.Float32BufferAttribute(position, 3));
  geometry.setAttribute('normal', new THREE.Float32BufferAttribute(normal, 3));
  geometry.setAttribute('uv', new THREE.Float32BufferAttribute(uv, 2));
  geometry.setIndex(indices);
  return geometry;
}

export interface FeatureSurface { role: SurfaceRole; geometry: THREE.BufferGeometry; }
export const surfaceLevel = { building: .5, green: .02, water: .1, road: .22, sport: .15, parking: .18, poi: .12 };

export function featureSurfaces(feature: SceneFeature, strict: boolean): FeatureSurface[] {
  const building = feature.kind === 'building';
  const height = building ? (strict && feature.heightStatus !== 'source' ? .25 : feature.height) : 0;
  const horizontal: THREE.BufferGeometry[] = [], vertical: THREE.BufferGeometry[] = [];
  for (const rings of feature.polygons) {
    if (!rings[0] || rings[0].length < 3) continue;
    const top = new THREE.ShapeGeometry(makeShape(rings));
    top.rotateX(-Math.PI / 2);
    // OSM can include a stadium envelope, running track and pitch at the same XY.
    // Separate their surfaces by centimetres so batch order cannot hide the grass.
    const level = feature.kind === 'sport'
      ? ['soccer', 'football', 'rugby', 'basketball', 'tennis', 'volleyball', 'badminton'].includes(feature.sport || '') ? .20 : feature.sport === 'running' ? .16 : .13
      : surfaceLevel[feature.kind];
    top.translate(0, level + height, 0);
    // Shape coordinates are already in projected metres. UVs remain independent of camera/height.
    const positions = top.getAttribute('position');
    const uvs = top.getAttribute('uv');
    for (let i = 0; i < positions.count; i++) uvs.setXY(i, positions.getX(i), -positions.getZ(i));
    top.clearGroups();
    horizontal.push(top);
    if (building && height > 0) vertical.push(walls(rings, surfaceLevel.building, height));
  }
  const output: FeatureSurface[] = [];
  if (horizontal.length) output.push({ role: building ? 'roof' : 'ground', geometry: mergeOrEmpty(horizontal) });
  if (vertical.length) output.push({ role: 'wall', geometry: mergeOrEmpty(vertical) });
  return output;
}

// Retained as the simple whole-feature helper used by data/geometry tests.
export function featureGeometry(feature: SceneFeature, strict: boolean): THREE.BufferGeometry | null {
  const parts = featureSurfaces(feature, strict);
  return parts.length ? mergeOrEmpty(parts.map(p => p.geometry)) : null;
}

export function mergeOrEmpty(parts: THREE.BufferGeometry[]): THREE.BufferGeometry {
  if (!parts.length) return new THREE.BufferGeometry();
  const merged = mergeGeometries(parts, false);
  parts.forEach(geometry => geometry.dispose());
  if (!merged) throw new Error('Incompatible geometry attributes in campus batch');
  merged.computeBoundingSphere();
  return merged;
}

export function pointInRing(x: number, z: number, ring: Ring): boolean {
  let inside = false;
  for (let i = 0, j = ring.length - 1; i < ring.length; j = i++) {
    const [xi, zi] = ring[i], [xj, zj] = ring[j];
    if ((zi > z) !== (zj > z) && x < (xj - xi) * (z - zi) / (zj - zi) + xi) inside = !inside;
  }
  return inside;
}

export function pointInFootprint(x: number, z: number, polygons: PolygonRings[]): boolean {
  return polygons.some(rings => pointInRing(x, z, rings[0]) && !rings.slice(1).some(ring => pointInRing(x, z, ring)));
}
