import type { AnnotationMarker, MarkerPosition } from './model';

function positionElement(element: HTMLButtonElement, position: MarkerPosition) {
  element.style.transform = `translate3d(${position.x}px,${position.y}px,0) translate(-50%,-100%)`;
  element.style.visibility = position.visible ? 'visible' : 'hidden';
}

function sameContent(a: readonly AnnotationMarker[], b: readonly AnnotationMarker[]): boolean {
  return a.length === b.length && a.every((marker, i) => {
    const next = b[i];
    return marker.entity === next.entity && marker.selected === next.selected && marker.label === next.label &&
      marker.anchor.every((value, axis) => value === next.anchor[axis]) &&
      marker.ids.length === next.ids.length && marker.ids.every((id, index) => id === next.ids[index]);
  });
}

/** Per-scene bridge: React owns content; the frame loop alone owns DOM transforms. */
export function createAnnotationStore() {
  let snapshot: readonly AnnotationMarker[] = [];
  const subscribers = new Set<() => void>();
  const elements = new Map<string, HTMLButtonElement>();
  const positions = new Map<string, MarkerPosition>();
  return {
    getSnapshot: () => snapshot,
    subscribe(listener: () => void) { subscribers.add(listener); return () => { subscribers.delete(listener); }; },
    publish(markers: AnnotationMarker[]) {
      if (sameContent(snapshot, markers)) return;
      snapshot = markers;
      const ids = new Set(markers.map(marker => marker.entity.id));
      for (const id of positions.keys()) if (!ids.has(id)) positions.delete(id);
      subscribers.forEach(listener => listener());
    },
    position(id: string, position: MarkerPosition) {
      positions.set(id, position);
      const element = elements.get(id);
      if (element) positionElement(element, position);
    },
    bind(id: string, element: HTMLButtonElement | null) {
      if (!element) { elements.delete(id); return; }
      elements.set(id, element);
      const position = positions.get(id);
      if (position) positionElement(element, position);
    },
  };
}
export type AnnotationStore = ReturnType<typeof createAnnotationStore>;
