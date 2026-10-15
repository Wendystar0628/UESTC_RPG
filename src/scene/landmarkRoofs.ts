import * as THREE from 'three';
import type { Ring, SceneFeature } from '../data/types';
import { mergeOrEmpty, surfaceLevel } from './geometry';

export interface LandmarkRoofSurface {
  role: 'roof' | 'wall';
  materialKey: 'roof-metal' | 'window-frame' | 'glass' | 'facade-trim' | 'terracotta';
  geometry: THREE.BufferGeometry;
}

/**
 * Photo-informed silhouettes of the three southern sports venues.
 * Reference: https://news.uestc.edu.cn/info/1053/23081.htm (2023-07-19).
 * Only the gym's 26 m overall height is documented. Curvature, cladding, seam
 * spacing and the other two roof rises are visual approximations, not survey data.
 * Original OSM footprints and recorded heights are deliberately left unchanged.
 */
export function landmarkRoofDetails(feature: SceneFeature, strict: boolean): LandmarkRoofSurface[] {
  if (feature.kind !== 'building' || (strict && feature.heightStatus !== 'source')) return [];
  if (!['osm:way:217855275', 'osm:way:217855279', 'osm:way:217855276'].includes(feature.id)) return [];
  const input = feature.polygons[0]?.[0];
  if (!input || input.length < 4) return [];
  const ring = input.slice();
  if (ring[0][0] === ring.at(-1)![0] && ring[0][1] === ring.at(-1)![1]) ring.pop();
  return feature.id === 'osm:way:217855276'
    ? standCanopy(ring, feature.height + surfaceLevel.building)
    : drumRoof(ring, feature.height + surfaceLevel.building, feature.id === 'osm:way:217855275');
}

interface Frame { x: number; z: number; cosine: number; sine: number; shortRadius: number; }

/** PCA determines the roof axes; perimeter sampling avoids vertex-density bias. */
function roofFrame(ring: Ring): Frame {
  const samples = Array.from({ length: 96 }, (_, i) => samplePath([...ring, ring[0]], i / 96));
  const x = samples.reduce((sum, p) => sum + p[0], 0) / samples.length;
  const z = samples.reduce((sum, p) => sum + p[1], 0) / samples.length;
  let xx = 0, zz = 0, xz = 0;
  for (const [px, pz] of samples) { xx += (px - x) ** 2; zz += (pz - z) ** 2; xz += (px - x) * (pz - z); }
  const angle = .5 * Math.atan2(2 * xz, xx - zz), cosine = Math.cos(angle), sine = Math.sin(angle);
  const u = ring.map(p => (p[0] - x) * cosine + (p[1] - z) * sine);
  const v = ring.map(p => -(p[0] - x) * sine + (p[1] - z) * cosine);
  const midU = (Math.min(...u) + Math.max(...u)) / 2, midV = (Math.min(...v) + Math.max(...v)) / 2;
  return {
    x: x + midU * cosine - midV * sine, z: z + midU * sine + midV * cosine, cosine, sine,
    shortRadius: Math.min(Math.max(...u) - Math.min(...u), Math.max(...v) - Math.min(...v)) / 2,
  };
}

function mesh(position: number[], index: number[], frame?: Frame): THREE.BufferGeometry {
  const geometry = new THREE.BufferGeometry();
  geometry.setAttribute('position', new THREE.Float32BufferAttribute(position, 3));
  const uv: number[] = [];
  for (let i = 0; i < position.length; i += 3) {
    const x = position[i] - (frame?.x ?? 0), z = position[i + 2] - (frame?.z ?? 0);
    uv.push(frame ? x * frame.cosine + z * frame.sine : x, frame ? -x * frame.sine + z * frame.cosine : z);
  }
  geometry.setAttribute('uv', new THREE.Float32BufferAttribute(uv, 2));
  geometry.setIndex(index);
  geometry.computeVertexNormals();
  geometry.computeBoundingSphere();
  return geometry;
}

function ringArea(ring: Ring): number {
  return ring.reduce((sum, p, i) => { const q = ring[(i + 1) % ring.length]; return sum + p[0] * q[1] - q[0] * p[1]; }, 0) / 2;
}

function drumRoof(input: Ring, base: number, gym: boolean): LandmarkRoofSurface[] {
  const ring = ringArea(input) > 0 ? input : input.slice().reverse();
  const frame = roofFrame(ring), count = ring.length;
  const peak = gym ? surfaceLevel.building + 26 : base + frame.shortRadius * .13;
  const eaves = gym ? surfaceLevel.building + 17.2 : base + .9;
  const shoulder = gym ? .73 : .76;
  const height = (r: number) => r <= shoulder
    ? peak - .28 * (r / shoulder) ** 2
    : eaves + (peak - eaves - .28) * Math.cos((r - shoulder) / (1 - shoulder) * Math.PI / 2);
  const point = (edge: [number, number], r: number): THREE.Vector3 =>
    new THREE.Vector3(frame.x + (edge[0] - frame.x) * r, height(r), frame.z + (edge[1] - frame.z) * r);

  // Broad almost-flat centre and rounded rolled perimeter, rather than a sphere.
  const radial = [0, .24, .49, shoulder, shoulder + (1 - shoulder) * .2,
    shoulder + (1 - shoulder) * .4, shoulder + (1 - shoulder) * .6,
    shoulder + (1 - shoulder) * .8, 1];
  const topPositions: number[] = [frame.x, peak, frame.z], centralIndices: number[] = [], rimIndices: number[] = [];
  for (const r of radial.slice(1)) for (const edge of ring) topPositions.push(...point(edge, r).toArray());
  for (let i = 0; i < count; i++) centralIndices.push(0, 1 + (i + 1) % count, 1 + i);
  for (let band = 0; band < radial.length - 2; band++) {
    const indices = band < 2 ? centralIndices : rimIndices;
    for (let i = 0; i < count; i++) {
      const next = (i + 1) % count, a = 1 + band * count + i, b = 1 + band * count + next;
      const c = a + count, d = b + count;
      indices.push(a, b, c, b, d, c);
    }
  }
  const output: LandmarkRoofSurface[] = [
    { role: 'roof', materialKey: gym ? 'roof-metal' : 'window-frame', geometry: mesh(topPositions, centralIndices, frame) },
    { role: 'roof', materialKey: 'roof-metal', geometry: mesh(topPositions, rimIndices, frame) },
  ];
  const cladding: THREE.BufferGeometry[] = [], glazing: THREE.BufferGeometry[] = [], trim: THREE.BufferGeometry[] = [];
  const glassTop = gym ? surfaceLevel.building + 9.3 : base;
  if (gym && glassTop > base) glazing.push(verticalBand(ring, base, glassTop));
  cladding.push(verticalBand(ring, Math.max(base, glassTop), eaves));
  if (gym) {
    trim.push(verticalBand(ring, glassTop - .3, glassTop + .35, frame, 1.004));
    for (const [x, z] of ring) {
      const column = new THREE.BoxGeometry(.38, glassTop - base, .5);
      column.rotateY(-Math.atan2(z - frame.z, x - frame.x));
      column.translate(x, (base + glassTop) / 2, z);
      column.clearGroups(); trim.push(column);
    }
  }
  trim.push(verticalBand(ring, eaves - .2, eaves + .18, frame, 1.005));
  output.push({ role: 'wall', materialKey: 'roof-metal', geometry: mergeOrEmpty(cladding) });
  if (glazing.length) output.push({ role: 'wall', materialKey: 'glass', geometry: mergeOrEmpty(glazing) });
  output.push({ role: 'wall', materialKey: gym ? 'terracotta' : 'facade-trim', geometry: mergeOrEmpty(trim) });

  // Raised standing seams are real ribbons. Most stop at the central roof panel.
  const seams: THREE.BufferGeometry[] = [];
  for (let i = 0; i < count; i++) {
    const start = gym ? (i % 10 === 0 ? .24 : shoulder) : shoulder;
    const path = Array.from({ length: 9 }, (_, j) => point(ring[i], start + (1 - start) * j / 8).add(new THREE.Vector3(0, .055, 0)));
    seams.push(ribbon(path, .16));
  }
  // The pool has a distinct narrow metal ring around its darker central oval.
  if (!gym) {
    const path = [...ring, ring[0]].map(edge => point(edge, shoulder).add(new THREE.Vector3(0, .06, 0)));
    seams.push(ribbon(path, .36));
  }
  output.push({ role: 'roof', materialKey: 'facade-trim', geometry: mergeOrEmpty(seams) });
  return output;
}

function verticalBand(ring: Ring, bottom: number, top: number, frame?: Frame, scale = 1): THREE.BufferGeometry {
  const position: number[] = [], indices: number[] = [];
  const transform = ([x, z]: [number, number]): [number, number] => frame
    ? [frame.x + (x - frame.x) * scale, frame.z + (z - frame.z) * scale] : [x, z];
  for (let i = 0; i < ring.length; i++) {
    const p = transform(ring[i]), q = transform(ring[(i + 1) % ring.length]), n = position.length / 3;
    position.push(p[0], bottom, p[1], p[0], top, p[1], q[0], top, q[1], q[0], bottom, q[1]);
    indices.push(n, n + 1, n + 2, n, n + 2, n + 3);
  }
  return mesh(position, indices, frame);
}

/** Horizontal-width strips follow the curved roof, so no line renderer is needed. */
function ribbon(path: THREE.Vector3[], width: number): THREE.BufferGeometry {
  const position: number[] = [], indices: number[] = [];
  path.forEach((point, i) => {
    const direction = path[Math.min(i + 1, path.length - 1)].clone().sub(path[Math.max(0, i - 1)]);
    const side = new THREE.Vector3(-direction.z, 0, direction.x).normalize().multiplyScalar(width / 2);
    position.push(...point.clone().add(side).toArray(), ...point.clone().sub(side).toArray());
    if (i) { const n = i * 2; indices.push(n - 2, n, n - 1, n - 1, n, n + 1); }
  });
  return mesh(position, indices);
}

function samplePath(path: Ring, t: number): [number, number] {
  const lengths = path.slice(1).map((p, i) => Math.hypot(p[0] - path[i][0], p[1] - path[i][1]));
  let distance = Math.max(0, Math.min(1, t)) * lengths.reduce((sum, n) => sum + n, 0);
  for (let i = 0; i < lengths.length; i++) {
    if (distance <= lengths[i] || i === lengths.length - 1) {
      const ratio = lengths[i] ? distance / lengths[i] : 0;
      return [path[i][0] + (path[i + 1][0] - path[i][0]) * ratio, path[i][1] + (path[i + 1][1] - path[i][1]) * ratio];
    }
    distance -= lengths[i];
  }
  return path[0];
}

function standCanopy(ring: Ring, base: number): LandmarkRoofSurface[] {
  // The two short end edges separate the existing outer and inner C-shaped arcs.
  // These are anchor locations on this surveyed outline, not an invented ellipse.
  const edgeAt = (x: number, z: number) => ring.reduce((best, p, i) => {
    const q = ring[(i + 1) % ring.length], previous = ring[best], next = ring[(best + 1) % ring.length];
    return Math.hypot((p[0] + q[0]) / 2 - x, (p[1] + q[1]) / 2 - z) <
      Math.hypot((previous[0] + next[0]) / 2 - x, (previous[1] + next[1]) / 2 - z) ? i : best;
  }, 0);
  const north = edgeAt(116.8, 690.9), south = edgeAt(139.66, 850.66);
  const arc = (start: number, end: number) => {
    const points: Ring = [];
    for (let i = start; ; i = (i + 1) % ring.length) { points.push(ring[i]); if (i === end) break; }
    return points;
  };
  const outer = arc((north + 1) % ring.length, south);
  const inner = arc((south + 1) % ring.length, north).reverse();
  const along = 72, across = 8, positions: number[] = [], indices: number[] = [];
  const canopyPoint = (t: number, s: number) => {
    const a = samplePath(outer, t), b = samplePath(inner, t);
    const rise = 6.7 * (.72 + .28 * Math.sin(Math.PI * t));
    return new THREE.Vector3(a[0] + (b[0] - a[0]) * s, base + .15 + rise * Math.sin(s * Math.PI / 2), a[1] + (b[1] - a[1]) * s);
  };
  for (let i = 0; i <= along; i++) for (let j = 0; j <= across; j++) positions.push(...canopyPoint(i / along, j / across).toArray());
  for (let i = 0; i < along; i++) for (let j = 0; j < across; j++) {
    const a = i * (across + 1) + j, b = a + across + 1;
    indices.push(a, b, a + 1, b, b + 1, a + 1);
  }
  const roof = mesh(positions, indices);
  // Match winding to this open footprint independently from its input ordering.
  const normals = roof.getAttribute('normal');
  if (Array.from({ length: normals.count }, (_, i) => normals.getY(i)).reduce((a, b) => a + b, 0) < 0) {
    for (let i = 0; i < indices.length; i += 3) [indices[i + 1], indices[i + 2]] = [indices[i + 2], indices[i + 1]];
    roof.setIndex(indices); roof.computeVertexNormals();
  }
  const seams: THREE.BufferGeometry[] = [], edges: THREE.BufferGeometry[] = [];
  for (let i = 0; i <= along; i += 3) {
    seams.push(ribbon(Array.from({ length: across + 1 }, (_, j) => canopyPoint(i / along, j / across).add(new THREE.Vector3(0, .05, 0))), .15));
  }
  for (const s of [0, 1]) edges.push(ribbon(Array.from({ length: along + 1 }, (_, i) => canopyPoint(i / along, s).add(new THREE.Vector3(0, .07, 0))), .5));
  return [
    { role: 'roof', materialKey: 'roof-metal', geometry: roof },
    { role: 'roof', materialKey: 'facade-trim', geometry: mergeOrEmpty(seams) },
    { role: 'roof', materialKey: 'window-frame', geometry: mergeOrEmpty(edges) },
  ];
}
