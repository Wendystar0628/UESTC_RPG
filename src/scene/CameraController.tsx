import { useEffect, useMemo, useRef, type RefObject } from 'react';
import { useFrame, useThree } from '@react-three/fiber';
import * as THREE from 'three';
import { MapControls } from 'three/addons/controls/MapControls.js';
import type { Bounds, Dataset, ViewCommand } from '../data/types';
import { pointInFootprint } from './geometry';
import { buildingEnvelope } from './buildingEnvelope';
import { advanceCameraMotion, type CameraFrame, type CameraMetrics } from './cameraState';

interface Props {
  data: Dataset;
  command: ViewCommand;
  selectedId: string | null;
  view: '3d' | 'top';
  strictHeight: boolean;
  onMetrics: (metrics: CameraMetrics) => void;
  frame: RefObject<CameraFrame>;
}

export default function CameraController({ data, command, selectedId, view, strictHeight, onMetrics, frame }: Props) {
  const { camera, gl, size, invalidate } = useThree();
  const perspective = camera as THREE.PerspectiveCamera;
  const controls = useRef<MapControls | null>(null);
  const animation = useRef<{ start: number; duration: number; from: THREE.Vector3; to: THREE.Vector3; fromTarget: THREE.Vector3; toTarget: THREE.Vector3; lift: number } | null>(null);
  const initialized = useRef(false);
  const lastMetrics = useRef(0);
  const interacting = useRef(false);
  const controlsChanged = useRef(false);
  const settleTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const previousCamera = useRef(new THREE.Matrix4());
  const previousProjection = useRef(new THREE.Matrix4());
  const entityById = useMemo(() => new Map(data.entities.map(e => [e.id, e])), [data]);
  const featureById = useMemo(() => new Map(data.features.map(f => [f.id, f])), [data]);
  const collisions = useMemo(() => data.features.filter(f => f.kind === 'building').map(f => {
    const all = f.polygons.flatMap(p => p[0]);
    return { feature: f, minX: Math.min(...all.map(p => p[0])), maxX: Math.max(...all.map(p => p[0])), minZ: Math.min(...all.map(p => p[1])), maxZ: Math.max(...all.map(p => p[1])) };
  }), [data]);

  useEffect(() => {
    const control = new MapControls(camera, gl.domElement);
    controls.current = control;
    control.enableDamping = true;
    control.dampingFactor = .09;
    control.screenSpacePanning = false;
    control.minDistance = 45;
    control.maxDistance = 6500;
    control.minPolarAngle = .035;
    control.maxPolarAngle = Math.PI * .455;
    control.zoomSpeed = .85;
    const stop = () => { animation.current = null; lastMetrics.current = 0; invalidate(); };
    const start = () => { interacting.current = true; stop(); };
    const end = () => { interacting.current = false; invalidate(); };
    control.addEventListener('start', start);
    control.addEventListener('end', end);
    const onChange = () => { controlsChanged.current = true; invalidate(); };
    control.addEventListener('change', onChange);
    const onKey = (e: KeyboardEvent) => {
      if ((e.target as HTMLElement)?.closest('input, textarea, select, button, [contenteditable="true"]')) return;
      if (!['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'].includes(e.key)) return;
      e.preventDefault(); stop();
      const step = camera.position.distanceTo(control.target) * .035;
      const forward = control.target.clone().sub(camera.position).setY(0).normalize();
      const right = forward.clone().cross(new THREE.Vector3(0, 1, 0));
      const shift = e.key === 'ArrowUp' ? forward.multiplyScalar(step) : e.key === 'ArrowDown' ? forward.multiplyScalar(-step) : right.multiplyScalar(e.key === 'ArrowRight' ? step : -step);
      control.target.add(shift); camera.position.add(shift); control.update(); invalidate();
    };
    const visibility = () => { if (document.hidden) stop(); else invalidate(); };
    document.addEventListener('visibilitychange', visibility);
    window.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('visibilitychange', visibility); window.removeEventListener('keydown', onKey);
      control.removeEventListener('start', start); control.removeEventListener('end', end); control.removeEventListener('change', onChange);
      control.dispose(); controls.current = null; interacting.current = false;
      if (settleTimer.current !== null) clearTimeout(settleTimer.current);
    };
  }, [camera, gl, invalidate]);

  useEffect(() => {
    const control = controls.current;
    if (!control) return;
    if (command.kind === 'cancel') { animation.current = null; lastMetrics.current = 0; invalidate(); return; }
    control.enableDamping = false; control.update(); control.enableDamping = true;
    const fromTarget = control.target.clone();
    const direction = camera.position.clone().sub(control.target).normalize();
    let target = fromTarget.clone();
    let distance = camera.position.distanceTo(control.target);
    let bounds: Bounds | null = null;
    let topHeight = 0;
    const home = command.kind === 'home';
    if (home) { bounds = data.meta.bounds; direction.set(.24, 1.05, 1).normalize(); }
    if (command.kind === 'entity' || command.kind === 'cluster') {
      const targets = (command.ids || []).map(id => entityById.get(id)).filter(e => e?.bounds && e.anchor);
      if (!targets.length) return;
      bounds = [Math.min(...targets.map(e => e!.bounds![0])), Math.min(...targets.map(e => e!.bounds![1])), Math.max(...targets.map(e => e!.bounds![2])), Math.max(...targets.map(e => e!.bounds![3]))];
      topHeight = Math.max(10, ...targets.flatMap(e => e!.featureIds.map(id => featureById.get(id)).filter(f => f?.kind === 'building').map(f => buildingEnvelope(f!, strictHeight))), ...targets.map(e => e!.anchor![1]));
      if (bounds[2] - bounds[0] < 40) { bounds[0] -= 30; bounds[2] += 30; }
      if (bounds[3] - bounds[1] < 40) { bounds[1] -= 30; bounds[3] += 30; }
      direction.y = Math.max(direction.y, .9); direction.normalize();
    }
    if (command.kind === 'north') direction.set(0, Math.max(.8, direction.y), 1).normalize();
    if (command.kind === 'top' || view === 'top' && bounds) direction.set(0, 1, .035).normalize();
    if (command.kind === 'perspective') direction.set(.24, 1.05, 1).normalize();
    if (command.kind === 'zoomIn') distance *= .74;
    if (command.kind === 'zoomOut') distance *= 1.35;
    if (bounds) {
      const center = new THREE.Vector3((bounds[0] + bounds[2]) / 2, topHeight / 2, (bounds[1] + bounds[3]) / 2);
      const quaternion = new THREE.Quaternion().setFromRotationMatrix(new THREE.Matrix4().lookAt(direction, new THREE.Vector3(), new THREE.Vector3(0, 1, 0)));
      const inverse = quaternion.clone().invert();
      const small = window.innerWidth < 1024;
      const uiScale = parseFloat(getComputedStyle(document.documentElement).fontSize) / 16;
      const mapElement = gl.domElement.closest('.map-view');
      const mapRect = mapElement?.getBoundingClientRect();
      const panelRect = selectedId ? mapElement?.querySelector('.entity-details')?.getBoundingClientRect() : null;
      // Measure the responsive card instead of assuming a fixed desktop/mobile size.
      const panelWidth = !small && panelRect && mapRect ? mapRect.right - panelRect.left : 0;
      const panelHeight = small && panelRect && mapRect ? mapRect.bottom - panelRect.top : 0;
      const availableW = Math.max(150, size.width - panelWidth - 70 * uiScale);
      const availableH = Math.max(150, size.height - panelHeight - 130 * uiScale);
      const tanV = Math.tan(THREE.MathUtils.degToRad(perspective.fov / 2));
      const tanH = tanV * size.width / size.height;
      distance = 100;
      const fitPoints = home ? data.boundary[0].map(([x, z]) => new THREE.Vector3(x, 25, z)) : [bounds[0], bounds[2]].flatMap(x => [0, Math.max(25, topHeight)].flatMap(y => [bounds![1], bounds![3]].map(z => new THREE.Vector3(x, y, z))));
      for (const point of fitPoints) {
        const local = point.sub(center).applyQuaternion(inverse);
        distance = Math.max(distance, Math.abs(local.x) / (tanH * availableW / size.width) + local.z, Math.abs(local.y) / (tanV * availableH / size.height) + local.z);
      }
      distance *= home ? 1.02 : 1.2;
      const metresPerPixel = 2 * distance * tanV / size.height;
      const right = new THREE.Vector3(1, 0, 0).applyQuaternion(quaternion);
      const up = new THREE.Vector3(0, 1, 0).applyQuaternion(quaternion);
      target = center.addScaledVector(right, panelWidth / 2 * metresPerPixel).addScaledVector(up, -panelHeight / 2 * metresPerPixel);
    }
    distance = THREE.MathUtils.clamp(distance, 60, 6200);
    const to = target.clone().addScaledVector(direction, distance);
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    animation.current = { start: -1, duration: !initialized.current || reduced ? 0 : 760, from: camera.position.clone(), to, fromTarget, toTarget: target, lift: fromTarget.distanceTo(target) > 400 ? 160 : 0 };
    initialized.current = true;
    onMetrics({ azimuth: control.getAzimuthalAngle() * 180 / Math.PI, metresPerPixel: 2 * camera.position.distanceTo(control.target) * Math.tan(THREE.MathUtils.degToRad(perspective.fov / 2)) / size.height, target: control.target.toArray().map(n => n.toFixed(1)).join(','), moving: animation.current.duration > 0 });
    invalidate();
  }, [command, camera, data.meta.bounds, entityById, featureById, strictHeight, perspective.fov, size.width, size.height, selectedId, view, invalidate, gl]);

  useFrame((_, delta) => {
    const control = controls.current;
    if (!control) return;
    const now = performance.now();
    const move = animation.current;
    if (move) {
      if (move.start < 0) move.start = now;
      const t = move.duration ? Math.min(1, (now - move.start) / move.duration) : 1;
      const eased = t * t * (3 - 2 * t);
      camera.position.lerpVectors(move.from, move.to, eased);
      camera.position.y += Math.sin(Math.PI * t) * move.lift;
      control.target.lerpVectors(move.fromTarget, move.toTarget, eased);
      camera.lookAt(control.target);
      if (t === 1) { animation.current = null; lastMetrics.current = 0; }
    }
    control.enableDamping = !move;
    const changed = control.update(Math.min(delta, .05));
    control.enableDamping = true;
    const [minX, minZ, maxX, maxZ] = data.meta.bounds;
    const clampedX = THREE.MathUtils.clamp(control.target.x, minX - 450, maxX + 450);
    const clampedZ = THREE.MathUtils.clamp(control.target.z, minZ - 450, maxZ + 450);
    camera.position.x += clampedX - control.target.x;
    camera.position.z += clampedZ - control.target.z;
    control.target.x = clampedX; control.target.z = clampedZ;
    camera.position.y = Math.max(camera.position.y, 8);
    if (camera.position.y < 120) {
      for (const box of collisions) {
        const { x, y, z } = camera.position;
        const height = buildingEnvelope(box.feature, strictHeight);
        if (y < height + 8 && x >= box.minX && x <= box.maxX && z >= box.minZ && z <= box.maxZ && pointInFootprint(x, z, box.feature.polygons)) camera.position.y = height + 8;
      }
    }
    camera.lookAt(control.target);
    camera.updateMatrixWorld();
    // Controls also mutate the camera in DOM handlers, before this frame's update().
    // Compare the final rendered matrix, not just update()'s return value.
    const projectionChanged = !previousProjection.current.equals(camera.projectionMatrix);
    const cameraChanged = !previousCamera.current.equals(camera.matrixWorld) || projectionChanged;
    if (cameraChanged) {
      previousCamera.current.copy(camera.matrixWorld); previousProjection.current.copy(camera.projectionMatrix);
      frame.current.revision++;
    }
    const oldPhase = frame.current.phase;
    advanceCameraMotion(frame.current, now, interacting.current || !!animation.current || changed || controlsChanged.current || projectionChanged);
    controlsChanged.current = false;
    frame.current.distance = camera.position.distanceTo(control.target);
    if (settleTimer.current !== null) { clearTimeout(settleTimer.current); settleTimer.current = null; }
    // One wake-up completes settling even after demand rendering has gone quiet.
    if (frame.current.phase === 'settling') settleTimer.current = setTimeout(() => { settleTimer.current = null; invalidate(); }, Math.ceil(frame.current.settleAt - now));
    if (move || changed) invalidate();
    // Emit the final state as the demand loop goes idle, not from a polling timer.
    if (oldPhase !== frame.current.phase || !lastMetrics.current || now - lastMetrics.current > 100) {
      lastMetrics.current = now;
      onMetrics({
        azimuth: control.getAzimuthalAngle() * 180 / Math.PI,
        metresPerPixel: 2 * camera.position.distanceTo(control.target) * Math.tan(THREE.MathUtils.degToRad(perspective.fov / 2)) / size.height,
        target: control.target.toArray().map(n => n.toFixed(1)).join(','),
        moving: frame.current.phase !== 'idle',
      });
    }
  }, -2);
  return null;
}
