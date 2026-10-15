import * as THREE from 'three';
import type { PolygonRings, Ring, SceneFeature } from '../data/types';
import { pointInFootprint, surfaceLevel } from './geometry';

/** Photo-informed families, not a claim that individual windows were surveyed. */
export type ArchitectureStyle = 'academic' | 'dormitory' | 'research' | 'modern' | 'library' | 'sports' | 'service' | 'residential' | 'utility';
export type DetailMaterialKey = 'glass' | 'window-frame' | 'plinth' | 'roof-metal' | 'terracotta' | 'facade-trim' | 'facade-olive';
export interface ArchitecturalDetail {
  role: 'wall' | 'roof';
  materialKey: DetailMaterialKey;
  geometry: THREE.BufferGeometry;
}

type Point = [number, number, number];
interface Edge {
  x: number; z: number; dx: number; dz: number;
  nx: number; nz: number; length: number; courtyard: boolean;
}
interface Profile {
  storey: number; bay: number; glazing: number; windowHeight: number;
  sill: number; continuous: boolean; frame: number;
}
const profiles: Record<ArchitectureStyle, Profile> = {
  academic: { storey: 3.6, bay: 4.2, glazing: .59, windowHeight: 1.9, sill: .95, continuous: false, frame: .12 },
  dormitory: { storey: 3.3, bay: 3.8, glazing: .53, windowHeight: 1.75, sill: 1.0, continuous: false, frame: .11 },
  research: { storey: 3.8, bay: 3.9, glazing: .8, windowHeight: 2.15, sill: .8, continuous: false, frame: .1 },
  modern: { storey: 3.8, bay: 7.5, glazing: .98, windowHeight: 2.7, sill: .38, continuous: true, frame: .075 },
  library: { storey: 4.0, bay: 2.8, glazing: .34, windowHeight: 3.0, sill: .7, continuous: false, frame: .08 },
  sports: { storey: 6.0, bay: 4.5, glazing: .86, windowHeight: 1.75, sill: 2.8, continuous: true, frame: .09 },
  service: { storey: 3.4, bay: 3.8, glazing: .74, windowHeight: 2.0, sill: .65, continuous: false, frame: .12 },
  residential: { storey: 3.1, bay: 3.5, glazing: .52, windowHeight: 1.7, sill: 1.0, continuous: false, frame: .1 },
  utility: { storey: 4.0, bay: 4.6, glazing: .4, windowHeight: 1.15, sill: 1.65, continuous: false, frame: .1 },
};
const landmarkRoofIds = new Set(['osm:way:217855275', 'osm:way:217855279', 'osm:way:217855276']);

/** A single indexed buffer per material/role keeps thousands of windows batchable. */
class Quads {
  positions: number[] = [];
  normals: number[] = [];
  uvs: number[] = [];
  indices: number[] = [];

  add(a: Point, b: Point, c: Point, d: Point, width: number, height: number): void {
    const ab = [b[0] - a[0], b[1] - a[1], b[2] - a[2]];
    const ac = [c[0] - a[0], c[1] - a[1], c[2] - a[2]];
    const nx = ab[1] * ac[2] - ab[2] * ac[1];
    const ny = ab[2] * ac[0] - ab[0] * ac[2];
    const nz = ab[0] * ac[1] - ab[1] * ac[0];
    const magnitude = Math.hypot(nx, ny, nz);
    if (magnitude < 1e-8) return;
    const start = this.positions.length / 3;
    for (const p of [a, b, c, d]) {
      this.positions.push(...p);
      this.normals.push(nx / magnitude, ny / magnitude, nz / magnitude);
    }
    this.uvs.push(0, 0, 0, height, width, height, width, 0);
    this.indices.push(start, start + 1, start + 2, start, start + 2, start + 3);
  }

  geometry(): THREE.BufferGeometry {
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(this.positions, 3));
    geometry.setAttribute('normal', new THREE.Float32BufferAttribute(this.normals, 3));
    geometry.setAttribute('uv', new THREE.Float32BufferAttribute(this.uvs, 2));
    geometry.setIndex(this.indices);
    geometry.computeBoundingBox();
    geometry.computeBoundingSphere();
    return geometry;
  }
}

function area(ring: Ring): number {
  return ring.reduce((sum, p, i) => {
    const next = ring[(i + 1) % ring.length];
    return sum + p[0] * next[1] - next[0] * p[1];
  }, 0) / 2;
}

function edgesFor(rings: PolygonRings, holesOnly = false): Edge[] {
  return rings.flatMap((input, index) => {
    const ring = input.filter((p, i) => i === 0 || p[0] !== input[i - 1][0] || p[1] !== input[i - 1][1]);
    if (ring.length > 1 && ring[0][0] === ring.at(-1)![0] && ring[0][1] === ring.at(-1)![1]) ring.pop();
    if (ring.length < 3 || Math.abs(area(ring)) < 1e-5) return [];
    // Outward normals point into the courtyard for inner rings.
    if ((area(ring) > 0) !== (index === 0 && !holesOnly)) ring.reverse();
    return ring.flatMap(([x, z], i) => {
      const next = ring[(i + 1) % ring.length];
      const length = Math.hypot(next[0] - x, next[1] - z);
      if (length < .05) return [];
      const dx = (next[0] - x) / length, dz = (next[1] - z) / length;
      return [{ x, z, dx, dz, nx: dz, nz: -dx, length, courtyard: index > 0 || holesOnly }];
    });
  });
}

function at(edge: Edge, along: number, y: number, outward: number): Point {
  return [edge.x + edge.dx * along + edge.nx * outward, y, edge.z + edge.dz * along + edge.nz * outward];
}

function panel(buffer: Quads, edge: Edge, left: number, right: number, bottom: number, top: number, outward: number): void {
  if (right - left < .01 || top - bottom < .01) return;
  buffer.add(at(edge, left, bottom, outward), at(edge, left, top, outward), at(edge, right, top, outward), at(edge, right, bottom, outward), right - left, top - bottom);
}

function ledge(buffer: Quads, edge: Edge, left: number, right: number, y: number, depth: number): void {
  if (right - left < .01) return;
  // Upper face and front lip supply depth without boxes for every bay.
  buffer.add(at(edge, left, y, .025), at(edge, right, y, .025), at(edge, right, y, depth), at(edge, left, y, depth), right - left, depth - .025);
  panel(buffer, edge, left, right, y - .10, y, depth);
}

/**
 * Architectural ornament follows existing footprints and storey-height estimates.
 * It adds no unsupported storeys and never decorates flattened strict-height models.
 */
export function architecturalDetails(feature: SceneFeature, strict: boolean, style: ArchitectureStyle): ArchitecturalDetail[] {
  if (feature.kind !== 'building' || feature.height < 2.8 || (strict && feature.heightStatus !== 'source')) return [];
  const footprintArea = feature.polygons.reduce((sum, rings) => sum + rings.reduce((net, ring, i) => net + Math.abs(area(ring)) * (i ? -1 : 1), 0), 0);
  if (footprintArea < 32) return [];
  const buffers = new Map<string, { role: 'wall' | 'roof'; materialKey: DetailMaterialKey; quads: Quads }>();
  const get = (key: DetailMaterialKey, role: 'wall' | 'roof' = 'wall'): Quads => {
    const id = `${role}/${key}`;
    if (!buffers.has(id)) buffers.set(id, { role, materialKey: key, quads: new Quads() });
    return buffers.get(id)!.quads;
  };
  const glass = get('glass'), frame = get('window-frame'), trim = get('facade-trim'), plinth = get('plinth');
  const profile = profiles[style];
  const base = surfaceLevel.building, top = base + feature.height;
  const landmarkRoof = landmarkRoofIds.has(feature.id);
  const floors = style === 'sports' || style === 'library' ? 1 : Math.max(1, Math.min(18, Math.round(feature.height / profile.storey)));
  const storey = feature.height / floors;

  for (const rings of feature.polygons) {
    const roofArea = rings.reduce((net, ring, index) => net + Math.abs(area(ring)) * (index ? -1 : 1), 0);
    const edges = edgesFor(rings);
    if (!edges.length) continue;
    const longest = edges.filter(edge => !edge.courtyard).reduce((best, edge) => edge.length > best.length ? edge : best, edges[0]);
    for (const edge of edges) {
      if (edge.length < 1.5) continue;
      const inset = Math.min(.36, edge.length * .08);
      const left = inset, right = edge.length - inset;
      panel(plinth, edge, left, right, base + .02, base + Math.min(.48, feature.height * .12), .035);
      // A cornice remains visible below the roofline in both height display modes.
      if (!landmarkRoof) {
        const roofTrim = style === 'dormitory' ? get('terracotta') : trim;
        panel(roofTrim, edge, left, right, top - .42, top - .02, .065);
        ledge(get('roof-metal', 'roof'), edge, left, right, top + .025, .16);
      }
      if (style === 'sports') panel(get('terracotta'), edge, left, right, base + .48, base + Math.min(feature.height * .52, feature.height - 2.5), .036);

      if (edge.length < 3.0) continue;
      const available = edge.length - 1.0;
      const bays = Math.max(1, Math.floor(available / profile.bay));
      const pitch = available / bays;
      const width = Math.min(pitch * profile.glazing, profile.continuous ? 5.2 : 3.1);
      for (let floor = 0; floor < floors; floor++) {
        const floorBottom = base + floor * storey;
        const sill = style === 'sports'
          ? base + Math.min(feature.height * .57, feature.height - 2.4)
          : floorBottom + Math.min(profile.sill, storey * .30);
        const windowTop = Math.min(sill + (style === 'library' ? feature.height - 1.4 : profile.windowHeight), floorBottom + storey - .50, top - .56);
        if (windowTop - sill < .65) continue;
        if (style === 'modern') {
          // Wenke's glazed ribbon stays continuous between the projecting white slabs.
          panel(frame, edge, left, right, sill, windowTop, .068);
          panel(glass, edge, left + .075, right - .075, sill + .075, windowTop - .075, .091);
          for (let mullion = left + profile.bay; mullion < right - .4; mullion += profile.bay) {
            panel(frame, edge, mullion - .038, mullion + .038, sill + .075, windowTop - .075, .106);
          }
          const slabTop = Math.min(floorBottom + .27, top - .55);
          panel(trim, edge, left, right, Math.max(base + .03, floorBottom - .02), slabTop, .29);
          ledge(trim, edge, left, right, slabTop, .31);
          if (edge === longest && edge.length >= 16) {
            const zones = edge.length > 65 ? [.23, .73] : [.23];
            const accentWidth = Math.min(2.7, edge.length * .09);
            for (const fraction of zones) {
              const center = edge.length * fraction;
              panel(get('facade-olive'), edge, center - accentWidth / 2, center + accentWidth / 2, sill + .03, windowTop - .035, .123);
            }
          }
          continue;
        }
        for (let bay = 0; bay < bays; bay++) {
          const center = .5 + pitch * (bay + .5);
          const u0 = center - width / 2, u1 = center + width / 2;
          // One backing quad is the window surround; glass covers its centre.
          panel(frame, edge, u0, u1, sill, windowTop, .062);
          panel(glass, edge, u0 + profile.frame, u1 - profile.frame, sill + profile.frame, windowTop - profile.frame, .083);
          if (width > 2.7 && (style === 'service' || (style === 'research' && bay % 2 === 0))) {
            panel(frame, edge, center - .035, center + .035, sill + profile.frame, windowTop - profile.frame, .097);
          }
          if (style === 'dormitory' && bays > 3 && bay % 4 === 0) {
            // Repeated brick piers echo the dormitory facade rhythm.
            panel(get('terracotta'), edge, Math.max(left, u0 - .48), u0 - .08, floorBottom + .08, Math.min(top - .44, floorBottom + storey), .035);
          }
          if (style === 'dormitory' && edge === longest && edge.length >= 22 && floor > 0 && bay % 5 === 2) {
            balcony(get('terracotta'), trim, edge, center, floorBottom + .12, Math.min(2.9, pitch - .4));
          }
        }
        if (floor > 0 && style !== 'utility') {
          const band = trim;
          const bandHeight = .14;
          panel(band, edge, left, right, floorBottom - bandHeight, floorBottom + .035, .105);
          if ((style === 'academic' || style === 'research') && floor % 2 === 0) ledge(band, edge, left, right, floorBottom + .04, .16);
        }
      }
      if ((style === 'academic' || style === 'research') && edge.length > 10) {
        for (let bay = 0; bay <= bays; bay += style === 'research' ? 2 : 3) {
          const center = .5 + pitch * bay;
          panel(trim, edge, Math.max(left, center - .14), Math.min(right, center + .14), base + .5, top - .45, .115);
        }
      }
    }

    // A restrained generic entrance on an exterior facade; no entrances in courtyards.
    if (longest.length >= 18 && footprintArea > 170 && style !== 'utility' && style !== 'sports') {
      const center = longest.length / 2, width = style === 'library' ? 5.2 : 3.4;
      panel(frame, longest, center - width / 2 - .16, center + width / 2 + .16, base + .03, base + 2.72, .12);
      panel(glass, longest, center - width / 2, center + width / 2, base + .08, base + 2.55, .145);
      panel(frame, longest, center - .045, center + .045, base + .08, base + 2.55, .16);
      ledge(trim, longest, center - width / 2 - .45, center + width / 2 + .45, base + 2.83, .72);
    }

    if (!landmarkRoof && (style === 'sports' || (style === 'research' && footprintArea > 500))) {
      addRoofSeams(rings, top + .03, get('roof-metal', 'roof'), style === 'sports' ? 2.6 : 5.5);
    }
    if (!strict && style === 'research' && footprintArea > 500) addRoofEquipment(rings, top, get('roof-metal', 'roof'), get('plinth', 'roof'));
    if (!strict && !landmarkRoof && roofArea >= 180) addParapets(rings, top, get('facade-trim', 'roof'));
    if (!strict && style === 'dormitory' && roofArea >= 350) addRoofEquipment(rings, top, get('roof-metal', 'roof'), get('plinth', 'roof'), 1);
  }

  return Array.from(buffers.values()).filter(item => item.quads.indices.length).map(item => ({ role: item.role, materialKey: item.materialKey, geometry: item.quads.geometry() }));
}

/** Mitered, inward roof rims preserve courtyard holes and the original footprint. */
function addParapets(rings: PolygonRings, roof: number, buffer: Quads): void {
  const thickness = .32, top = roof + .53, bottom = roof + .02;
  rings.forEach((ring, ringIndex) => {
    const edges = edgesFor([ring], ringIndex > 0);
    if (edges.length < 3) return;
    const inner = edges.map((edge, index): [number, number] | null => {
      const previous = edges[(index + edges.length - 1) % edges.length];
      const denominator = 1 + edge.nx * previous.nx + edge.nz * previous.nz;
      if (denominator < .06) return null;
      const dx = -(edge.nx + previous.nx) * thickness / denominator;
      const dz = -(edge.nz + previous.nz) * thickness / denominator;
      // Reduce sharp miters where narrow wings leave insufficient roof width.
      for (const factor of [1, .75, .5, .25]) {
        const candidate: [number, number] = [edge.x + dx * factor, edge.z + dz * factor];
        if (pointInFootprint(candidate[0], candidate[1], [rings])) return candidate;
      }
      return null;
    });
    edges.forEach((edge, index) => {
      const next = edges[(index + 1) % edges.length], a = inner[index], b = inner[(index + 1) % edges.length];
      if (!a || !b || edge.length < .5) return;
      if ((b[0] - a[0]) * edge.dx + (b[1] - a[1]) * edge.dz < .08) return;
      // Checking the inner edge avoids bridging another concave wing or courtyard.
      if (![.25, .5, .75].every(t => pointInFootprint(a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, [rings]))) return;
      buffer.add([edge.x, bottom, edge.z], [edge.x, top, edge.z], [next.x, top, next.z], [next.x, bottom, next.z], edge.length, top - bottom);
      buffer.add([b[0], bottom, b[1]], [b[0], top, b[1]], [a[0], top, a[1]], [a[0], bottom, a[1]], Math.hypot(b[0] - a[0], b[1] - a[1]), top - bottom);
      buffer.add([edge.x, top, edge.z], [a[0], top, a[1]], [b[0], top, b[1]], [next.x, top, next.z], edge.length, thickness);
    });
  });
}

function balcony(terracotta: Quads, trim: Quads, edge: Edge, center: number, floor: number, width: number): void {
  const left = center - width / 2, right = center + width / 2, depth = .80, rail = floor + .94;
  ledge(trim, edge, left, right, floor, depth + .035);
  panel(terracotta, edge, left, right, floor + .1, rail, depth);
  // Solid end panels and rail coping give visible thickness at oblique camera angles.
  terracotta.add(at(edge, left, floor, .05), at(edge, left, rail, .05), at(edge, left, rail, depth), at(edge, left, floor, depth), depth - .05, .94);
  terracotta.add(at(edge, right, floor, depth), at(edge, right, rail, depth), at(edge, right, rail, .05), at(edge, right, floor, .05), depth - .05, .94);
  trim.add(at(edge, left, rail, depth - .10), at(edge, right, rail, depth - .10), at(edge, right, rail, depth + .03), at(edge, left, rail, depth + .03), width, .13);
}

/** Small inferred plant enclosures: omitted from strict source-height views. */
function addRoofEquipment(rings: PolygonRings, roof: number, metal: Quads, plinth: Quads, limit = 2): void {
  const xs = rings[0].map(point => point[0]), zs = rings[0].map(point => point[1]);
  const x0 = Math.min(...xs), x1 = Math.max(...xs), z0 = Math.min(...zs), z1 = Math.max(...zs);
  if (x1 - x0 < 12 || z1 - z0 < 12) return;
  let placed = 0;
  for (const [fx, fz] of [[.32, .34], [.65, .6], [.35, .65], [.65, .35], [.5, .5]]) {
    const x = x0 + (x1 - x0) * fx, z = z0 + (z1 - z0) * fz;
    if (![-2.4, 0, 2.4].every(dx => [-1.9, 0, 1.9].every(dz => pointInFootprint(x + dx, z + dz, [rings])))) continue;
    roofBox(plinth, x, z, roof + .025, 3.5, 2.5, .20);
    roofBox(metal, x, z, roof + .225, 2.8, 1.8, .9);
    // Three shallow dark louvres suggest plant ventilation with six extra triangles.
    for (let slat = 0; slat < 3; slat++) {
      const left = x - 1.15 + slat * .78, right = left + .48;
      plinth.add([left, roof + 1.13, z - .60], [left, roof + 1.13, z + .6], [right, roof + 1.13, z + .6], [right, roof + 1.13, z - .6], .48, 1.2);
    }
    if (++placed === limit) break;
  }
}

function roofBox(buffer: Quads, x: number, z: number, base: number, width: number, depth: number, height: number): void {
  const ring: Ring = [[x - width / 2, z - depth / 2], [x + width / 2, z - depth / 2], [x + width / 2, z + depth / 2], [x - width / 2, z + depth / 2]];
  for (const edge of edgesFor([ring])) panel(buffer, edge, 0, edge.length, base, base + height, 0);
  buffer.add([x - width / 2, base + height, z - depth / 2], [x - width / 2, base + height, z + depth / 2], [x + width / 2, base + height, z + depth / 2], [x + width / 2, base + height, z - depth / 2], width, depth);
}

/** Scanline-clipped roof seams: concave wings and courtyard holes stay unbridged. */
function addRoofSeams(rings: PolygonRings, y: number, buffer: Quads, spacing: number): void {
  const xs = rings[0].map(point => point[0]), zs = rings[0].map(point => point[1]);
  const minX = Math.min(...xs), maxX = Math.max(...xs), minZ = Math.min(...zs), maxZ = Math.max(...zs);
  if (maxX - minX < 8 || maxZ - minZ < 8) return;
  // Fixed world axes keep adjacent roof parts visually consistent.
  for (let x = Math.ceil((minX + .7) / spacing) * spacing; x < maxX - .7; x += spacing) {
    const hits: number[] = [];
    for (const ring of rings) for (let i = 0; i < ring.length; i++) {
      const a = ring[i], b = ring[(i + 1) % ring.length];
      if ((a[0] <= x && b[0] > x) || (b[0] <= x && a[0] > x)) hits.push(a[1] + (x - a[0]) * (b[1] - a[1]) / (b[0] - a[0]));
    }
    hits.sort((a, b) => a - b);
    for (let i = 0; i + 1 < hits.length; i += 2) {
      const z0 = hits[i] + .5, z1 = hits[i + 1] - .5;
      if (z1 - z0 < 2) continue;
      const halfWidth = .055;
      // The centreline is clipped analytically; ensure its thin width also stays inside.
      if (![z0, (z0 + z1) / 2, z1].every(z => pointInFootprint(x - halfWidth, z, [rings]) && pointInFootprint(x + halfWidth, z, [rings]))) continue;
      buffer.add([x - halfWidth, y, z0], [x - halfWidth, y, z1], [x + halfWidth, y, z1], [x + halfWidth, y, z0], halfWidth * 2, z1 - z0);
    }
  }
}
