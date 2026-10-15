import profiles from '../../data/building-profiles.json';
import type { SceneFeature } from './types';
import type { MaterialKey } from './appearance';
import type { ArchitectureStyle } from '../scene/architecture';

export interface BuildingProfile {
  style: ArchitectureStyle;
  wall: MaterialKey;
  roof: MaterialKey;
  basis: 'photo-reference' | 'campus-family';
  reference?: { title: string; url: string };
  referenceScope?: 'roof';
}

const overrides = profiles.buildings as Record<string, BuildingProfile>;
const families: Record<string, ArchitectureStyle> = {
  teaching: 'academic', research: 'research', library: 'library',
  residence: 'dormitory', sport: 'sports', dining: 'service',
  service: 'service', medical: 'research', administration: 'academic',
};
const walls: Record<ArchitectureStyle, MaterialKey> = {
  academic: 'wall-sandstone', dormitory: 'wall-residence', research: 'wall-concrete',
  library: 'wall-sandstone', sports: 'wall-concrete', service: 'wall-sandstone',
  residential: 'wall-residence', utility: 'wall-paint',
  modern: 'wall-concrete',
};

/** Photo references describe a visual family, not surveyed window positions. */
export function buildingProfile(feature: SceneFeature): BuildingProfile {
  if (overrides[feature.id]) return overrides[feature.id];
  const style = families[feature.category] || 'utility';
  return { style, wall: walls[style], roof: style === 'sports' ? 'roof-metal' : 'roof-concrete', basis: 'campus-family' };
}
