import { useMemo, useRef, type RefObject } from 'react';
import { useFrame, useThree } from '@react-three/fiber';
import * as THREE from 'three';
import type { Dataset } from '../../data/types';
import type { CameraFrame } from '../cameraState';
import { qualitySettings, type QualityTier } from '../quality';
import { buildAnnotationCandidates, type AnnotationCandidate, type AnnotationSelection, type MarkerPosition } from './model';
import { layoutAnnotations } from './layout';
import type { AnnotationStore } from './store';

interface Props extends AnnotationSelection {
  data: Dataset; enabled: boolean; tier: QualityTier; store: AnnotationStore;
  frame: RefObject<CameraFrame>; buildingRef: RefObject<THREE.Group | null>;
}

/** R3F adapter: projection/raycast only. Selection and temporal layout are pure functions. */
export default function AnnotationController({ data, enabled, tier, store, frame, buildingRef, selectedId, filters, layers, strictHeight }: Props) {
  const { camera, size } = useThree();
  const scale = useMemo(() => parseFloat(getComputedStyle(document.documentElement).fontSize) / 16, [size.width, size.height]);
  const candidates = useMemo(() => buildAnnotationCandidates(data, { selectedId, filters, layers, strictHeight }), [data, selectedId, filters, layers, strictHeight]);
  const context = useMemo(() => ({}), [candidates, size.width, size.height, scale, enabled]);
  const scratch = useMemo(() => new THREE.Vector3(), []);
  const raycaster = useMemo(() => new THREE.Raycaster(), []);
  const last = useRef({ context: null as object | null, revision: -1, time: -Infinity, phase: 'idle' as CameraFrame['phase'], tier });

  function project(candidate: AnnotationCandidate): MarkerPosition {
    scratch.set(...candidate.anchor).project(camera);
    return { x: (scratch.x + 1) * size.width / 2, y: (1 - scratch.y) * size.height / 2, visible: scratch.z >= -1 && scratch.z <= 1 };
  }

  useFrame(() => {
    if (!enabled) { store.publish([]); last.current.context = null; return; }
    // CameraController (-2) has finalized this exact frame before we update DOM (-1).
    for (const marker of store.getSnapshot()) store.position(marker.entity.id, project(marker));
    const now = performance.now(), current = frame.current, previous = last.current;
    const changed = previous.context !== context;
    const settled = current.phase === 'idle' && previous.phase !== 'idle';
    if (!changed && !settled && previous.revision === current.revision && previous.tier === tier) return;
    if (!changed && !settled && current.phase !== 'idle' && now - previous.time < 160) return;
    last.current = { context, revision: current.revision, time: now, phase: current.phase, tier };
    const occluders = current.distance < 1200 && buildingRef.current
      ? buildingRef.current.children.filter(child => child.visible && !child.userData.campusDetail) : [];
    const markers = layoutAnnotations(candidates.map(candidate => ({ ...candidate, position: project(candidate) })), changed ? [] : store.getSnapshot(), {
      ...size, scale, distance: current.distance,
      maxCount: Math.min(qualitySettings[tier].maxLabels, size.width < 500 ? 25 : current.distance < 950 ? 60 : 34),
      names: layers.names, icons: layers.icons, filtered: filters.length > 0, moving: !changed && current.phase !== 'idle',
    }, candidate => {
      if (!occluders.length) return false;
      scratch.set(...candidate.anchor).sub(camera.position);
      raycaster.far = scratch.length() - 4;
      raycaster.set(camera.position, scratch.normalize());
      return raycaster.intersectObjects(occluders, true).length > 0;
    });
    for (const marker of markers) store.position(marker.entity.id, project(marker));
    store.publish(markers);
  }, -1);
  return null;
}
