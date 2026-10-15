import catalog from '../../data/materials.json';
import sourceAppearance from '../../data/appearances.json';
import type { SceneFeature } from './types';
import { buildingProfile } from './buildingProfiles';

export type MaterialKey = keyof typeof catalog.presets;
export type SurfaceRole = 'wall' | 'roof' | 'ground';
export const materialPresets = catalog.presets;
export const materialVersion = catalog.version;
export interface MaterialChoice {
  key: MaterialKey;
  status: 'generic' | 'source-tag' | 'reference-based';
  source?: string;
}
interface SourceRecord { tags: Record<string, string>; source: string; }
interface Override { wall?: MaterialKey; roof?: MaterialKey; ground?: MaterialKey; source?: string; }
const sourceTags = sourceAppearance.sourceTags as Record<string, SourceRecord>;
const overrides = sourceAppearance.overrides as Record<string, Override>;

/** Surface assignment is not proof of a building's real appearance. */
export function resolveMaterial(feature: SceneFeature, role: SurfaceRole): MaterialChoice {
  const override = overrides[feature.id] || (feature.entityId ? overrides[feature.entityId] : undefined);
  if (override?.[role] && materialPresets[override[role]!]) {
    return { key: override[role]!, status: override.source ? 'reference-based' : 'generic', source: override.source };
  }
  const record = sourceTags[feature.id];
  const surface = record?.tags.surface;
  if (role === 'wall') {
    if (record?.tags['building:material'] === 'concrete') return { key: 'wall-concrete', status: 'source-tag', source: record.source };
    const profile = buildingProfile(feature);
    return { key: profile.wall, status: profile.basis === 'photo-reference' && profile.referenceScope !== 'roof' ? 'reference-based' : 'generic', source: profile.referenceScope !== 'roof' ? profile.reference?.url : undefined };
  }
  if (role === 'roof') {
    const profile = buildingProfile(feature);
    return { key: profile.roof, status: profile.basis === 'photo-reference' ? 'reference-based' : 'generic', source: profile.reference?.url };
  }
  const sourceMap: Record<string, MaterialKey> = {
    asphalt: 'asphalt', paving_stones: 'paving', sett: 'paving',
    concrete: 'concrete-ground', 'concrete:plates': 'concrete-ground',
    'concrete:lanes': 'concrete-ground', grass: 'grass', artificial_turf: 'turf',
  };
  if (surface && sourceMap[surface]) return { key: sourceMap[surface], status: 'source-tag', source: record.source };
  switch (feature.kind) {
    case 'water': return { key: 'water', status: 'generic' };
    case 'green': return { key: 'grass', status: 'generic' };
    case 'road': return { key: ['footway', 'path', 'steps', 'pedestrian'].includes(feature.roadType || '') ? 'paving' : 'asphalt', status: 'generic' };
    case 'parking': return { key: 'asphalt', status: 'generic' };
    case 'sport': return {
      key: feature.sport === 'swimming' ? 'water'
        : ['soccer', 'football', 'rugby'].includes(feature.sport || '') ? 'turf'
        : feature.sport === 'tennis' ? 'rubber-green' : 'rubber-red',
      status: 'generic',
    };
    default: return { key: 'stone', status: 'generic' };
  }
}

export function appearanceSummary(features: SceneFeature[]) {
  const choices = features.flatMap(feature => (feature.kind === 'building' ? ['wall', 'roof'] : ['ground']).map(role => resolveMaterial(feature, role as SurfaceRole)));
  return {
    labels: [...new Set(choices.map(choice => materialPresets[choice.key].label))],
    generic: choices.filter(choice => choice.status === 'generic').length,
    sourced: choices.filter(choice => choice.status !== 'generic').length,
    references: [...new Map(features.filter(f => f.kind === 'building').map(buildingProfile).filter(p => p.reference).map(p => [p.reference!.url, p.reference!])).values()],
  };
}
