import { useEffect, useRef, type RefObject } from 'react';
import { useFrame, useThree } from '@react-three/fiber';
import * as THREE from 'three';
import type { MapState } from '../app/mapState';
import { qualitySettings, type QualityTier } from './quality';

export interface RenderInspection {
  frames: number;
  geometryBuilds: number;
  batches: number;
  triangles: number;
  drawCalls: number;
  textures: number;
  geometries: number;
  geometryIds: string[];
  tier: string;
  landscape?: { trees: number; shrubs: number; drawCalls: number; triangles: number; surveyed: boolean };
  sportsMarkings?: { courts: number; goals: number; drawCalls: number; triangles: number };
  annotationCamera?: { viewProjection: number[]; width: number; height: number };
}
declare global { interface Window { __UESTC_RENDER__?: RenderInspection; } }

export default function SceneRuntime({ state, tier, onSlow, light }: {
  state: MapState;
  tier: QualityTier;
  onSlow: () => void;
  light: RefObject<THREE.DirectionalLight | null>;
}) {
  const { gl, scene, camera, invalidate, setDpr } = useThree();
  const sample = useRef({ last: 0, since: 0, intervals: [] as number[], position: new THREE.Vector3() });
  const inspection = useRef<RenderInspection>({ frames: 0, geometryBuilds: 0, batches: 0, triangles: 0, drawCalls: 0, textures: 0, geometries: 0, geometryIds: [], tier });
  inspection.current.tier = tier;
  const hiddenReveal = state.layers.building ? null : state.selectedId;

  useEffect(() => {
    setDpr(Math.min(window.devicePixelRatio || 1, qualitySettings[tier].dpr));
    invalidate();
  }, [tier, setDpr, invalidate]);

  useEffect(() => {
    const lamp = light.current;
    if (!lamp) return;
    const size = qualitySettings[tier].shadowSize;
    lamp.castShadow = size > 0;
    if (size > 0 && lamp.shadow.mapSize.x !== size) {
      lamp.shadow.map?.dispose(); lamp.shadow.map = null;
      lamp.shadow.mapSize.set(size, size);
    }
    gl.shadowMap.autoUpdate = false;
    lamp.shadow.autoUpdate = false;
    lamp.shadow.needsUpdate = true;
    gl.shadowMap.needsUpdate = true;
    invalidate();
  }, [tier, state.layers.building, state.layers.green, state.layers.sport, state.strictHeight, hiddenReveal, light, gl, invalidate]);

  useEffect(() => {
    const resume = () => { if (!document.hidden) invalidate(); };
    document.addEventListener('visibilitychange', resume);
    return () => document.removeEventListener('visibilitychange', resume);
  }, [invalidate]);

  // Opt-in inspection data is consumed by performance tests; no timers, uploads or reports.
  useEffect(() => {
    if (!import.meta.env.DEV && !new URLSearchParams(location.search).has('inspect')) return;
    const values = inspection.current;
    values.geometryBuilds = scene.userData.campusBuilds || 0;
    values.geometryIds = scene.userData.campusGeometryIds || [];
    window.__UESTC_RENDER__ = values;
    const previous = scene.onAfterRender;
    const viewProjection = new THREE.Matrix4();
    const viewport = new THREE.Vector2();
    const after: typeof scene.onAfterRender = (...args) => {
      previous.apply(scene, args);
      values.frames++;
      values.geometryBuilds = scene.userData.campusBuilds || 0;
      values.batches = scene.userData.campusBatches || 0;
      values.triangles = scene.userData.campusTriangles || 0;
      values.geometryIds = scene.userData.campusGeometryIds || [];
      values.drawCalls = gl.info.render.calls;
      values.textures = gl.info.memory.textures;
      values.geometries = gl.info.memory.geometries;
      values.landscape = scene.userData.campusLandscape;
      values.sportsMarkings = scene.userData.campusSportsMarkings;
      gl.getSize(viewport);
      values.annotationCamera = { viewProjection: viewProjection.multiplyMatrices(camera.projectionMatrix, camera.matrixWorldInverse).toArray(), width: viewport.x, height: viewport.y };
    };
    scene.onAfterRender = after;
    return () => { scene.onAfterRender = previous; if (window.__UESTC_RENDER__ === values) delete window.__UESTC_RENDER__; };
  }, [scene, gl, camera]);

  useFrame(() => {
    if (state.quality !== 'auto' || tier === 'light' || document.hidden) return;
    const now = performance.now();
    const data = sample.current;
    const moving = data.position.distanceToSquared(camera.position) > .005;
    data.position.copy(camera.position);
    const elapsed = now - data.last;
    data.last = now;
    if (!moving || elapsed > 200) { data.intervals.length = 0; data.since = now; return; }
    if (now - data.since < 1500) return; // Ignore cold shader/texture setup.
    data.intervals.push(elapsed);
    if (data.intervals.length >= 75) {
      const sorted = data.intervals.slice().sort((a, b) => a - b);
      if (sorted[Math.floor(sorted.length * .75)] > 34) onSlow();
      data.intervals.length = 0; data.since = now;
    }
  });
  return null;
}
