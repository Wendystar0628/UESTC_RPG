import type { Bounds, Dataset, PolygonRings, Ring } from '../data/types';
import { pointInFootprint, pointInRing } from './geometry';

export interface PlantPlacement {
  x: number;
  z: number;
  height: number;
  radius: number;
  rotation: number;
  variant: 0 | 1 | 2;
  tone: number;
}

export interface LandscapeLayout { trees: PlantPlacement[]; shrubs: PlantPlacement[]; }

interface Parcel { rings: PolygonRings; bounds: Bounds; area: number; }
const GRID_SIZE = 48;
const TREE_TARGET = 1120;
const SHRUB_TARGET = 360;

function ringArea(ring: Ring): number {
  return Math.abs(ring.reduce((sum, point, index) => {
    const next = ring[(index + 1) % ring.length];
    return sum + point[0] * next[1] - next[0] * point[1];
  }, 0)) / 2;
}

function parcel(rings: PolygonRings): Parcel {
  const x = rings[0].map(point => point[0]), z = rings[0].map(point => point[1]);
  return {
    rings, bounds: [Math.min(...x), Math.min(...z), Math.max(...x), Math.max(...z)],
    area: ringArea(rings[0]) - rings.slice(1).reduce((sum, ring) => sum + ringArea(ring), 0),
  };
}

function distanceToRing(x: number, z: number, ring: Ring): number {
  let distance = Infinity;
  for (let index = 0; index < ring.length; index++) {
    const a = ring[index], b = ring[(index + 1) % ring.length];
    const dx = b[0] - a[0], dz = b[1] - a[1];
    const lengthSquared = dx * dx + dz * dz;
    const t = lengthSquared ? Math.max(0, Math.min(1, ((x - a[0]) * dx + (z - a[1]) * dz) / lengthSquared)) : 0;
    distance = Math.min(distance, Math.hypot(x - a[0] - t * dx, z - a[1] - t * dz));
  }
  return distance;
}

function randomSequence(seed: number): () => number {
  return () => {
    seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0;
    return seed / 4294967296;
  };
}

/** A footprint index keeps placement work bounded despite hundreds of path polygons. */
function exclusionIndex(data: Dataset): (x: number, z: number, radius: number) => boolean {
  const cells = new Map<string, Parcel[]>();
  for (const feature of data.features) {
    if (!['building', 'water', 'road', 'parking', 'sport'].includes(feature.kind)) continue;
    for (const rings of feature.polygons) {
      if (!rings[0]?.length) continue;
      const item = parcel(rings), [minX, minZ, maxX, maxZ] = item.bounds;
      for (let x = Math.floor((minX - 5) / GRID_SIZE); x <= Math.floor((maxX + 5) / GRID_SIZE); x++) {
        for (let z = Math.floor((minZ - 5) / GRID_SIZE); z <= Math.floor((maxZ + 5) / GRID_SIZE); z++) {
          const key = `${x}:${z}`;
          const items = cells.get(key) || [];
          items.push(item); cells.set(key, items);
        }
      }
    }
  }
  return (x, z, radius) => {
    const items = cells.get(`${Math.floor(x / GRID_SIZE)}:${Math.floor(z / GRID_SIZE)}`) || [];
    return items.some(item => {
      const [minX, minZ, maxX, maxZ] = item.bounds;
      if (x < minX - radius || x > maxX + radius || z < minZ - radius || z > maxZ + radius) return false;
      return pointInFootprint(x, z, [item.rings]) || item.rings.some(ring => distanceToRing(x, z, ring) < radius);
    });
  };
}

/**
 * Photo-informed planting language, not surveyed tree coordinates/species.
 * The OSM green polygons are the factual planting envelope. The open lawns,
 * mixed broad/columnar crowns and shrub layers are deterministic interpretation.
 * Photo references: UESTC news /info/1132/37781.htm (2026-03-14) and
 * /info/1002/2048.htm (2014-12-02), viewed 2026-09-17.
 */
export function createLandscapeLayout(data: Dataset): LandscapeLayout {
  const green = data.features.filter(feature => feature.kind === 'green')
    .flatMap(feature => feature.polygons.filter(rings => rings[0]?.length > 2).map(parcel))
    .filter(item => item.area > 35);
  const trees: PlantPlacement[] = [], shrubs: PlantPlacement[] = [];
  if (!green.length) return { trees, shrubs };
  const excluded = exclusionIndex(data), random = randomSequence(20260917);
  const cumulative: number[] = [];
  // Sublinear weighting retains pocket gardens without overfilling big central lawns.
  let totalWeight = 0;
  for (const item of green) { totalWeight += Math.pow(item.area, .84); cumulative.push(totalWeight); }
  const occupied = new Map<string, PlantPlacement[]>();
  function clearSpacing(x: number, z: number, radius: number): boolean {
    const cx = Math.floor(x / 12), cz = Math.floor(z / 12);
    for (let dx = -1; dx <= 1; dx++) for (let dz = -1; dz <= 1; dz++) {
      for (const other of occupied.get(`${cx + dx}:${cz + dz}`) || []) {
        if (Math.hypot(x - other.x, z - other.z) < radius + other.radius + 1.2) return false;
      }
    }
    return true;
  }
  function place(target: PlantPlacement[], count: number, isShrub: boolean) {
    const maximumAttempts = count * 90;
    for (let attempt = 0; attempt < maximumAttempts && target.length < count; attempt++) {
      const choice = random() * totalWeight;
      const item = green[cumulative.findIndex(value => value >= choice)];
      const [minX, minZ, maxX, maxZ] = item.bounds;
      const x = minX + random() * (maxX - minX), z = minZ + random() * (maxZ - minZ);
      if (!pointInFootprint(x, z, [item.rings]) || !pointInRing(x, z, data.boundary[0])) continue;
      const edgeDistance = Math.min(...item.rings.map(ring => distanceToRing(x, z, ring)));
      const clustering = (Math.sin(x / 53 + Math.cos(z / 47)) + Math.cos(z / 59 - x / 139) + 2) / 4;
      if (item.area > 8000 && edgeDistance > 24 && clustering < .51) continue;
      if (isShrub && edgeDistance > 20 && clustering < .65) continue;
      const variant = (random() < .24 ? 2 : random() < .5 ? 0 : 1) as PlantPlacement['variant'];
      const height = isShrub ? .8 + random() * .95 : variant === 2 ? 7.5 + random() * 3.4 : 5.7 + random() * 4.5;
      const radius = isShrub ? 1.15 + random() * .7 : variant === 2 ? 1.55 + random() * .45 : 2.2 + random() * .9;
      // Include the crown envelope, rather than checking only the trunk position.
      if (edgeDistance < radius * .7 || excluded(x, z, radius + .7) || !clearSpacing(x, z, radius)) continue;
      const plant: PlantPlacement = { x, z, height, radius, variant, rotation: random() * Math.PI * 2, tone: random() };
      target.push(plant);
      const key = `${Math.floor(x / 12)}:${Math.floor(z / 12)}`;
      const neighbours = occupied.get(key) || [];
      neighbours.push(plant); occupied.set(key, neighbours);
    }
  }
  // Scale down for small synthetic datasets / fixtures; the campus uses the cap.
  const area = green.reduce((sum, item) => sum + item.area, 0);
  place(trees, Math.min(TREE_TARGET, Math.round(area / 400)), false);
  place(shrubs, Math.min(SHRUB_TARGET, Math.round(area / 1800)), true);
  return { trees, shrubs };
}
