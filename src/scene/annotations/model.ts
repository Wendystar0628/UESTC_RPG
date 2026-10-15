import type { Dataset, Entity, Layers, Vec3 } from '../../data/types';
import { layerVisible, matchesCategories } from '../../data/search';
import { buildingEnvelope } from '../buildingEnvelope';

export interface AnnotationCandidate { entity: Entity; anchor: Vec3; selected: boolean; }
export interface AnnotationMarker extends AnnotationCandidate { label: boolean; ids: string[]; }
export interface MarkerPosition { x: number; y: number; visible: boolean; }
export interface ProjectedCandidate extends AnnotationCandidate { position: MarkerPosition; }

export interface AnnotationSelection {
  selectedId: string | null;
  filters: string[];
  layers: Layers;
  strictHeight: boolean;
}

/** Dataset/selection -> world anchors. No camera, DOM or React state in this model. */
export function buildAnnotationCandidates(data: Dataset, selection: AnnotationSelection): AnnotationCandidate[] {
  const features = new Map(data.features.map(feature => [feature.id, feature]));
  return data.entities.filter(entity => entity.anchor && (entity.id === selection.selectedId ||
    matchesCategories(entity, selection.filters) && layerVisible(entity, selection.layers)))
    .map(entity => {
      const selected = entity.id === selection.selectedId;
      const height = Math.max(0, ...entity.featureIds.map(id => features.get(id))
        .filter(feature => feature?.kind === 'building').map(feature => buildingEnvelope(feature!, selection.strictHeight)));
      const [x, y, z] = entity.anchor!;
      const anchor: Vec3 = [x, Math.max(selection.strictHeight && !entity.height ? 0 : y, height) + (selected ? 16 : 10), z];
      return { entity, anchor, selected };
    }).sort((a, b) => Number(b.selected) - Number(a.selected) || b.entity.priority - a.entity.priority || a.entity.id.localeCompare(b.entity.id));
}
