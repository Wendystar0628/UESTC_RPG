import { useEffect, useMemo } from 'react';
import { useThree } from '@react-three/fiber';
import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import type { Dataset, Ring, SceneFeature } from '../data/types';
import { pointInFootprint } from './geometry';

type Point = [number, number];
type Segment = [Point, Point];
type Sport = 'soccer' | 'basketball' | 'tennis';
interface Court {
  feature: SceneFeature; sport: Sport; center: Point; u: Point; v: Point;
  length: number; width: number;
}
export interface MarkingRange { featureId: string; start: number; count: number; }
const LINE_WIDTH = .11;
const LINE_Y = .28;
const MIN_CLEARANCE = .4;
const NO_RAYCAST = () => {};

function distanceToRing(x: number, z: number, ring: Ring): number {
  let distance = Infinity;
  for (let i = 0; i < ring.length; i++) {
    const a = ring[i], b = ring[(i + 1) % ring.length], dx = b[0] - a[0], dz = b[1] - a[1];
    const squared = dx * dx + dz * dz;
    const t = squared ? Math.max(0, Math.min(1, ((x - a[0]) * dx + (z - a[1]) * dz) / squared)) : 0;
    distance = Math.min(distance, Math.hypot(x - a[0] - t * dx, z - a[1] - t * dz));
  }
  return distance;
}

function suitableCourt(feature: SceneFeature): Court | null {
  if (feature.kind !== 'sport' || !['soccer', 'basketball', 'tennis'].includes(feature.sport || '') || feature.polygons.length !== 1) return null;
  const rings = feature.polygons[0], ring = rings[0];
  if (rings.length !== 1 || ring.length < 4) return null;
  let longest = 0, u: Point = [1, 0];
  for (let index = 0; index < ring.length; index++) {
    const a = ring[index], b = ring[(index + 1) % ring.length], length = Math.hypot(b[0] - a[0], b[1] - a[1]);
    if (length > longest) { longest = length; u = [(b[0] - a[0]) / length, (b[1] - a[1]) / length]; }
  }
  let v: Point = [-u[1], u[0]];
  let us = ring.map(([x, z]) => x * u[0] + z * u[1]), vs = ring.map(([x, z]) => x * v[0] + z * v[1]);
  let length = Math.max(...us) - Math.min(...us), width = Math.max(...vs) - Math.min(...vs);
  if (width > length) { [u, v] = [v, u]; [us, vs] = [vs, us]; [length, width] = [width, length]; }
  const area = Math.abs(ring.reduce((sum, point, index) => {
    const next = ring[(index + 1) % ring.length]; return sum + point[0] * next[1] - next[0] * point[1];
  }, 0)) / 2;
  const sport = feature.sport as Sport;
  const minimum = sport === 'soccer' ? [32, 19] : sport === 'tennis' ? [24, 12] : [24, 13];
  if (length < minimum[0] || width < minimum[1] || area / (length * width) < .94 || length / width > 3.4) return null;
  const cu = (Math.max(...us) + Math.min(...us)) / 2, cv = (Math.max(...vs) + Math.min(...vs)) / 2;
  const dimensions = sport === 'soccer' ? [105, 68] : sport === 'tennis' ? [23.77, 10.97] : [28, 15];
  return { feature, sport, center: [cu * u[0] + cv * v[0], cu * u[1] + cv * v[1]], u, v,
    length: Math.min(dimensions[0], length - 2.4), width: Math.min(dimensions[1], width - 2.4) };
}

function courtSegments(court: Court): Segment[] {
  const segments: Segment[] = [], halfL = court.length / 2, halfW = court.width / 2;
  const line = (x1: number, z1: number, x2: number, z2: number) => segments.push([[x1, z1], [x2, z2]]);
  const box = (x1: number, z1: number, x2: number, z2: number) => {
    line(x1, z1, x2, z1); line(x2, z1, x2, z2); line(x2, z2, x1, z2); line(x1, z2, x1, z1);
  };
  const arc = (x: number, z: number, radius: number, start: number, end: number, count: number) => {
    for (let index = 0; index < count; index++) {
      const a = start + (end - start) * index / count, b = start + (end - start) * (index + 1) / count;
      line(x + Math.cos(a) * radius, z + Math.sin(a) * radius, x + Math.cos(b) * radius, z + Math.sin(b) * radius);
    }
  };
  box(-halfL, -halfW, halfL, halfW);
  if (court.sport === 'tennis') {
    const singles = halfW * 8.23 / 10.97, service = Math.min(6.4, halfL * .55);
    line(-halfL, -singles, halfL, -singles); line(-halfL, singles, halfL, singles);
    line(-service, -singles, -service, singles); line(service, -singles, service, singles);
    line(-service, 0, service, 0);
    // The center represents the net axis; all facilities are schematic, not surveyed.
    line(0, -halfW, 0, halfW);
  } else if (court.sport === 'soccer') {
    line(0, -halfW, 0, halfW);
    const scale = Math.min(court.length / 105, court.width / 68);
    arc(0, 0, 9.15 * scale, 0, Math.PI * 2, 32);
    for (const side of [-1, 1]) {
      const end = side * halfL, penalty = end - side * 16.5 * scale, goalArea = end - side * 5.5 * scale;
      line(end, -20.16 * scale, penalty, -20.16 * scale);
      line(penalty, -20.16 * scale, penalty, 20.16 * scale);
      line(penalty, 20.16 * scale, end, 20.16 * scale);
      line(end, -9.16 * scale, goalArea, -9.16 * scale);
      line(goalArea, -9.16 * scale, goalArea, 9.16 * scale);
      line(goalArea, 9.16 * scale, end, 9.16 * scale);
    }
  } else {
    const scale = Math.min(court.length / 28, court.width / 15);
    line(0, -halfW, 0, halfW); arc(0, 0, 1.8 * scale, 0, Math.PI * 2, 24);
    for (const side of [-1, 1]) {
      const end = side * halfL, freeThrow = end - side * 5.8 * scale, basket = end - side * 1.575 * scale;
      line(end, -2.45 * scale, freeThrow, -2.45 * scale);
      line(freeThrow, -2.45 * scale, freeThrow, 2.45 * scale);
      line(freeThrow, 2.45 * scale, end, 2.45 * scale);
      arc(freeThrow, 0, 1.8 * scale, side === -1 ? -Math.PI / 2 : Math.PI / 2, side === -1 ? Math.PI / 2 : Math.PI * 1.5, 12);
      const radius = 6.75 * scale, width = Math.min(6.6 * scale, halfW - .6), offset = Math.sqrt(radius * radius - width * width);
      const angle = Math.asin(width / radius);
      arc(basket, 0, radius, side === -1 ? -angle : Math.PI - angle, side === -1 ? angle : Math.PI + angle, 20);
      line(end, -width, basket - side * offset, -width); line(end, width, basket - side * offset, width);
    }
  }
  return segments;
}

function world(court: Court, [x, z]: Point): Point {
  return [court.center[0] + x * court.u[0] + z * court.v[0], court.center[1] + x * court.u[1] + z * court.v[1]];
}

function inside(court: Court, point: Point, clearance: number): boolean {
  return pointInFootprint(point[0], point[1], court.feature.polygons)
    && distanceToRing(point[0], point[1], court.feature.polygons[0][0]) >= clearance;
}

function goalGeometry(court: Court): THREE.BufferGeometry[] {
  if (court.sport !== 'soccer' || court.length < 80) return [];
  const output: THREE.BufferGeometry[] = [], up = new THREE.Vector3(0, 1, 0);
  function beam(a: THREE.Vector3, b: THREE.Vector3) {
    const direction = b.clone().sub(a);
    const geometry = new THREE.CylinderGeometry(.055, .055, direction.length(), 6, 1);
    geometry.applyQuaternion(new THREE.Quaternion().setFromUnitVectors(up, direction.normalize()));
    geometry.translate((a.x + b.x) / 2, (a.y + b.y) / 2, (a.z + b.z) / 2);
    output.push(geometry);
  }
  for (const side of [-1, 1]) {
    const left = world(court, [side * court.length / 2, -3.66]), right = world(court, [side * court.length / 2, 3.66]);
    const a = new THREE.Vector3(left[0], LINE_Y, left[1]), b = new THREE.Vector3(right[0], LINE_Y, right[1]);
    const at = a.clone().addScaledVector(up, 2.44), bt = b.clone().addScaledVector(up, 2.44);
    beam(a, at); beam(b, bt); beam(at, bt);
  }
  return output;
}

/** Paint interpretation inside source footprints; no claim of surveyed court dimensions. */
export function createSportsMarkings(data: Dataset) {
  const positions: number[] = [], ranges: MarkingRange[] = [], goals: THREE.BufferGeometry[] = [];
  const counts: Record<Sport, number> = { soccer: 0, basketball: 0, tennis: 0 };
  for (const feature of data.features) {
    const court = suitableCourt(feature);
    if (!court) continue;
    const segments = courtSegments(court).map(([a, b]) => [world(court, a), world(court, b)] as Segment);
    // Validate whole segments before accepting a court, including curved lines.
    if (!segments.every(([a, b]) => {
      const steps = Math.max(1, Math.ceil(Math.hypot(b[0] - a[0], b[1] - a[1]) / .5));
      for (let i = 0; i <= steps; i++) if (!inside(court, [a[0] + (b[0] - a[0]) * i / steps, a[1] + (b[1] - a[1]) * i / steps], MIN_CLEARANCE + LINE_WIDTH)) return false;
      return true;
    })) continue;
    const start = positions.length / 3;
    for (const [a, b] of segments) {
      const length = Math.hypot(b[0] - a[0], b[1] - a[1]);
      if (length < .001) continue;
      const dx = -(b[1] - a[1]) / length * LINE_WIDTH / 2, dz = (b[0] - a[0]) / length * LINE_WIDTH / 2;
      const points: Point[] = [[a[0] + dx, a[1] + dz], [b[0] + dx, b[1] + dz], [b[0] - dx, b[1] - dz], [a[0] - dx, a[1] - dz]];
      for (const index of [0, 1, 2, 0, 2, 3]) positions.push(points[index][0], LINE_Y, points[index][1]);
    }
    ranges.push({ featureId: feature.id, start, count: positions.length / 3 - start });
    counts[court.sport]++; goals.push(...goalGeometry(court));
  }
  const geometry = new THREE.BufferGeometry();
  geometry.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
  geometry.computeVertexNormals(); geometry.computeBoundingSphere();
  const goalMesh = goals.length ? mergeGeometries(goals, false) : null;
  goals.forEach(geometry => geometry.dispose());
  return { geometry, goalGeometry: goalMesh, ranges, counts, stats: { ...counts, courts: ranges.length,
    goals: goals.length / 3, drawCalls: goalMesh ? 2 : 1, triangles: positions.length / 9 + (goalMesh?.index?.count || 0) / 3 } };
}

export default function SportsMarkings({ data, visible }: { data: Dataset; visible: boolean }) {
  const { scene, invalidate } = useThree();
  const markings = useMemo(() => createSportsMarkings(data), [data]);
  const material = useMemo(() => new THREE.MeshStandardMaterial({ color: '#fff9e9', roughness: .9, side: THREE.DoubleSide }), []);
  useEffect(() => {
    scene.userData.campusSportsMarkings = markings.stats; invalidate();
    return () => { markings.geometry.dispose(); markings.goalGeometry?.dispose(); delete scene.userData.campusSportsMarkings; };
  }, [markings, scene, invalidate]);
  useEffect(() => () => material.dispose(), [material]);
  useEffect(() => { invalidate(); }, [visible, invalidate]);
  return <group visible={visible} dispose={null} name="campus-sports-markings">
    <mesh geometry={markings.geometry} material={material} raycast={NO_RAYCAST} receiveShadow />
    {markings.goalGeometry && <mesh geometry={markings.goalGeometry} material={material} raycast={NO_RAYCAST} castShadow receiveShadow />}
  </group>;
}
