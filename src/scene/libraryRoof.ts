import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import type { SceneFeature } from '../data/types';
import type { MaterialKey } from '../data/appearance';

/** The central octagonal lantern is photo-informed; its dimensions are illustrative. */
export function libraryRoofDetails(feature: SceneFeature, strict: boolean): { role: 'roof'; materialKey: MaterialKey; geometry: THREE.BufferGeometry }[] {
  if (feature.id !== 'osm:way:806848395' || strict && feature.heightStatus !== 'source') return [];
  const ring = feature.polygons[0][0];
  const minX = Math.min(...ring.map(p => p[0])), maxX = Math.max(...ring.map(p => p[0]));
  const minZ = Math.min(...ring.map(p => p[1])), maxZ = Math.max(...ring.map(p => p[1]));
  const cx = (minX + maxX) / 2, cz = (minZ + maxZ) / 2;
  const radius = Math.min(maxX - minX, maxZ - minZ) * .34;
  const base = feature.height + .8;
  const rise = radius * .15;
  const positions: number[] = [], uv: number[] = [], indices: number[] = [];
  const bars: THREE.BufferGeometry[] = [];
  const point = (angle: number, r: number, y: number) => new THREE.Vector3(cx + Math.cos(angle) * r, y, cz + Math.sin(angle) * r);
  function strut(a: THREE.Vector3, b: THREE.Vector3, width: number) {
    const direction = b.clone().sub(a);
    const geometry = new THREE.CylinderGeometry(width, width, direction.length(), 5);
    geometry.applyQuaternion(new THREE.Quaternion().setFromUnitVectors(new THREE.Vector3(0, 1, 0), direction.normalize()));
    geometry.translate(...a.clone().add(b).multiplyScalar(.5).toArray());
    bars.push(geometry);
  }
  const apex = new THREE.Vector3(cx, base + rise, cz);
  for (let i = 0; i < 8; i++) {
    const angle = Math.PI / 8 - .13 + i * Math.PI / 4;
    const a = point(angle, radius, base), b = point(angle + Math.PI / 4, radius, base);
    const offset = positions.length / 3;
    for (const p of [a, apex, b]) { positions.push(...p.toArray()); uv.push(p.x, p.z); }
    indices.push(offset, offset + 1, offset + 2);
    strut(a, apex, .16); strut(a, b, .22);
    for (const fraction of [.25, .5, .75]) {
      const aa = apex.clone().lerp(a, fraction), bb = apex.clone().lerp(b, fraction);
      strut(aa, bb, .09);
    }
    strut(a.clone().lerp(b, .5), apex, .075);
  }
  const glass = new THREE.BufferGeometry();
  glass.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
  glass.setAttribute('uv', new THREE.Float32BufferAttribute(uv, 2));
  glass.setIndex(indices); glass.computeVertexNormals();
  const frame = mergeGeometries(bars, false)!;
  bars.forEach(geometry => geometry.dispose());
  return [{ role: 'roof', materialKey: 'roof-glass', geometry: glass }, { role: 'roof', materialKey: 'window-frame', geometry: frame }];
}
