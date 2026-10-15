import type { SceneFeature } from '../data/types';

/** Conservative camera/label envelope including visible, photo-informed roof geometry. */
export function buildingEnvelope(feature: SceneFeature, strict: boolean): number {
  if (strict) return feature.heightStatus === 'source' ? feature.height : .25;
  if (feature.id === 'osm:way:217855275') return 26;
  if (feature.id === 'osm:way:217855276') return feature.height + 7;
  if (feature.id === 'osm:way:217855279') return feature.height + 8;
  if (feature.id === 'osm:way:806848395') return feature.height + 5;
  return feature.height + 1.2;
}
