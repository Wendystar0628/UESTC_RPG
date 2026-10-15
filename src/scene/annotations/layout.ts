import type { AnnotationMarker, ProjectedCandidate } from './model';

export interface LayoutOptions {
  width: number; height: number; scale: number; distance: number; maxCount: number;
  names: boolean; icons: boolean; filtered: boolean; moving: boolean;
}
interface Box { x: number; y: number; width: number; }

/**
 * Temporal layout: keep existing presentations throughout a gesture/damping interval.
 * Admit newly exposed places without stealing slots; reconcile once after settling.
 * Occlusion is checked at admission and reconciliation, using the same policy in both.
 */
export function layoutAnnotations(candidates: ProjectedCandidate[], previous: readonly AnnotationMarker[], options: LayoutOptions,
  isOccluded: (candidate: ProjectedCandidate) => boolean = () => false): AnnotationMarker[] {
  const { width, height, scale, moving } = options;
  const visible: AnnotationMarker[] = [], occupied: Box[] = [];
  const claimed = new Set<string>(), retained = new Set<string>();
  const byId = new Map(candidates.map(candidate => [candidate.entity.id, candidate]));
  const previousById = new Map(previous.map(marker => [marker.entity.id, marker]));
  const previousOrder = new Map(previous.map((marker, index) => [marker.entity.id, index]));
  const previousGroup = new Map(previous.flatMap(marker => marker.ids.map(id => [id, marker.entity.id] as const)));
  const markerWidth = (candidate: ProjectedCandidate, label: boolean) =>
    (label ? Math.min(224, candidate.entity.name.length * (candidate.selected ? 15 : 14) + 46) : 36) * scale;
  const place = (candidate: ProjectedCandidate, label: boolean, ids: string[]) => {
    const { entity, anchor, selected, position: { x, y } } = candidate;
    visible.push({ entity, anchor, selected, label, ids });
    occupied.push({ x, y, width: markerWidth(candidate, label) });
    ids.forEach(id => claimed.add(id));
  };

  if (moving) for (const marker of previous) {
    const candidate = byId.get(marker.entity.id);
    if (!candidate) continue;
    const { x, y, visible: inDepth } = candidate.position;
    // Leave outside a wider boundary than the entry boundary. The overlay clips edges.
    const margin = 128 * scale;
    if (!inDepth || x < -margin || x > width + margin || y < -margin || y > height + margin) continue;
    place(candidate, marker.label, marker.ids.filter(id => byId.has(id)));
    retained.add(marker.entity.id);
  }

  const ordered = [...candidates].sort((a, b) => Number(b.selected) - Number(a.selected) ||
    b.entity.priority - a.entity.priority || (previousOrder.get(a.entity.id) ?? Infinity) - (previousOrder.get(b.entity.id) ?? Infinity) || a.entity.id.localeCompare(b.entity.id));
  for (const candidate of ordered) {
    const { entity, selected, position: { x, y, visible: inDepth } } = candidate;
    if (claimed.has(entity.id) || !inDepth) continue;
    if (!selected && !options.icons && !options.names) continue;
    const prior = previousById.get(entity.id);
    // Retain density through modest zoom around a threshold; new entries use the inner threshold.
    const distance = options.distance - (prior ? 100 : 0);
    if (!selected && !options.filtered && (distance > 2700 ? entity.priority < 2 : distance > 1500 ? entity.priority < 1 : false)) continue;
    const margin = (prior ? 8 : 24) * scale;
    if (x < margin || x > width - margin || y < (prior ? 24 : 50) * scale || y > height - margin) continue;
    const nearby = !selected && entity.priority < 3 ? visible.find(marker => {
      if (marker.selected || marker.entity.priority >= 3) return false;
      const point = byId.get(marker.entity.id)!.position;
      const radius = (previousGroup.get(entity.id) === marker.entity.id ? 48 : 36) * scale;
      return Math.abs(point.x - x) < radius && Math.abs(point.y - y) < radius;
    }) : undefined;
    if (nearby) {
      // A stationary badge must not change on every movement sample.
      if (!retained.has(nearby.entity.id) && !isOccluded(candidate)) { nearby.ids.push(entity.id); claimed.add(entity.id); }
      continue;
    }
    if (visible.length >= options.maxCount && !selected) continue;
    let label = selected || options.names && entity.named;
    const gap = (prior?.label ? 4 : 12) * scale;
    const intersects = (labelWidth: number) => occupied.some(box => Math.abs(x - box.x) < (labelWidth + box.width) / 2 + gap && Math.abs(y - box.y) < 46 * scale);
    const labelWidth = markerWidth(candidate, label);
    if (!selected && (x - labelWidth / 2 < 4 || x + labelWidth / 2 > width - 4 || intersects(labelWidth))) label = false;
    if (!selected && intersects(markerWidth(candidate, label))) continue;
    if (!selected && isOccluded(candidate)) continue;
    place(candidate, label, [entity.id]);
  }
  return visible;
}
