import { describe, expect, it } from 'vitest';
import { layoutAnnotations, type LayoutOptions } from '../src/scene/annotations/layout';
import type { ProjectedCandidate } from '../src/scene/annotations/model';
import { createAnnotationStore } from '../src/scene/annotations/store';
import { advanceCameraMotion, createCameraFrame } from '../src/scene/cameraState';
import type { Entity } from '../src/data/types';

function candidate(id: string, x: number, y = 250, priority = 3): ProjectedCandidate {
  const entity = { id, name: 'Building A', named: true, priority } as Entity;
  return { entity, anchor: [0, 10, 0], selected: false, position: { x, y, visible: true } };
}
const options: LayoutOptions = { width: 1000, height: 700, scale: 1, distance: 800, maxCount: 20, names: true, icons: true, filtered: false, moving: false };

describe('temporal annotation layout', () => {
  it('preserves full labels when a gesture temporarily brings them together, then resolves overlap once', () => {
    const a = candidate('a', 300), b = candidate('b', 550);
    const initial = layoutAnnotations([a, b], [], options);
    expect(initial.map(marker => marker.label)).toEqual([true, true]);
    b.position.x = 340;
    const moving = layoutAnnotations([a, b], initial, { ...options, moving: true });
    expect(moving).toEqual(initial);
    const settled = layoutAnnotations([a, b], moving, options);
    expect(settled).toHaveLength(1);
  });

  it('fills newly exposed space during a long drag, without stealing existing label slots', () => {
    const a = candidate('a', 300), b = candidate('b', 750);
    const initial = layoutAnnotations([a], [], options);
    expect(layoutAnnotations([a, b], initial, { ...options, moving: true }).map(marker => marker.entity.id)).toEqual(['a', 'b']);
    a.position.x = -150;
    expect(layoutAnnotations([a, b], initial, { ...options, moving: true }).map(marker => marker.entity.id)).toEqual(['b']);
  });

  it('uses wider exit and density thresholds for existing labels', () => {
    const a = candidate('a', 300, 250, 0);
    const initial = layoutAnnotations([a], [], { ...options, distance: 1490 });
    expect(initial).toHaveLength(1);
    expect(layoutAnnotations([a], [], { ...options, distance: 1540 })).toHaveLength(0);
    expect(layoutAnnotations([a], initial, { ...options, distance: 1540 })).toHaveLength(1);
    expect(layoutAnnotations([a], initial, { ...options, distance: 1700 })).toHaveLength(0);
    a.position.x = -30;
    expect(layoutAnnotations([a], initial, { ...options, moving: true })).toHaveLength(1);
    expect(layoutAnnotations([a], [], { ...options, moving: true })).toHaveLength(0);
  });

  it('uses collision hysteresis instead of switching between a name and an icon around one boundary', () => {
    const a = candidate('a', 300), b = candidate('b', 510);
    const initial = layoutAnnotations([a, b], [], options);
    b.position.x = 496;
    expect(layoutAnnotations([a, b], initial, options).map(marker => marker.label)).toEqual([true, true]);
    expect(layoutAnnotations([a, b], [], options).map(marker => marker.label)).toEqual([true, false]);
  });

  it('keeps cluster membership fixed in motion and reconciles it without duplicate members after settling', () => {
    const a = candidate('a', 300, 250, 1), b = candidate('b', 310, 250, 1), c = candidate('c', 315, 250, 1);
    const initial = layoutAnnotations([a, b], [], options);
    expect(initial[0].ids).toEqual(['a', 'b']);
    const moving = layoutAnnotations([a, b, c], initial, { ...options, moving: true });
    expect(moving[0].ids).toEqual(['a', 'b']);
    const settled = layoutAnnotations([a, b, c], moving, options);
    expect(settled[0].ids).toEqual(['a', 'b', 'c']);
    b.position.x = 700;
    const split = layoutAnnotations([a, b, c], settled, options);
    expect(split).toHaveLength(2);
    expect(split.flatMap(marker => marker.ids).sort()).toEqual(['a', 'b', 'c']);
  });

  it('applies occlusion to new entries while moving too, without re-testing retained labels mid-gesture', () => {
    const a = candidate('a', 300), b = candidate('b', 750);
    const initial = layoutAnnotations([a], [], options);
    const tested: string[] = [];
    const occluded = (point: ProjectedCandidate) => { tested.push(point.entity.id); return true; };
    expect(layoutAnnotations([a, b], initial, { ...options, moving: true }, occluded)).toEqual(initial);
    expect(tested).toEqual(['b']);
    expect(layoutAnnotations([a, b], initial, options, occluded)).toHaveLength(0);
  });

  it('preserves the selected place when icons, names and density would otherwise hide everything', () => {
    const a = candidate('a', 300); a.selected = true;
    const layout = layoutAnnotations([a], [], { ...options, names: false, icons: false, maxCount: 0 }, () => true);
    expect(layout).toHaveLength(1);
    expect(layout[0].label).toBe(true);
  });
});

describe('frame/content ownership', () => {
  it('updates positions without notifying React and unsubscribes a detached DOM node', () => {
    const store = createAnnotationStore();
    const markers = layoutAnnotations([candidate('a', 300)], [], options);
    let notifications = 0;
    const unsubscribe = store.subscribe(() => notifications++);
    store.publish(markers);
    const element = { style: { transform: '', visibility: '' } } as HTMLButtonElement;
    store.bind('a', element);
    store.position('a', { x: 123, y: 456, visible: true });
    expect(element.style.transform).toContain('123px,456px');
    expect(element.style.visibility).toBe('visible');
    store.publish(markers.map(marker => ({ ...marker, ids: [...marker.ids] })));
    expect(notifications).toBe(1);
    store.bind('a', null);
    store.position('a', { x: 234, y: 567, visible: true });
    expect(element.style.transform).toContain('123px,456px');
    unsubscribe(); store.publish([]);
    expect(notifications).toBe(1);
    expect(store.getSnapshot()).toEqual([]);
  });

  it('does not settle during a held gesture and restarts the quiet period when movement resumes', () => {
    const frame = createCameraFrame();
    advanceCameraMotion(frame, 0, false);
    expect(frame.phase).toBe('idle');
    advanceCameraMotion(frame, 100, true);
    advanceCameraMotion(frame, 1000, true);
    expect(frame.phase).toBe('moving');
    advanceCameraMotion(frame, 1100, false);
    expect(frame.phase).toBe('settling');
    advanceCameraMotion(frame, 1170, true);
    advanceCameraMotion(frame, 1200, false);
    expect(frame.phase).toBe('settling');
    advanceCameraMotion(frame, 1350, false);
    expect(frame.phase).toBe('idle');
  });
});
