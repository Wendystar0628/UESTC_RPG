import { Component, memo, useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { Canvas } from '@react-three/fiber';
import * as THREE from 'three';
import type { Dataset } from '../data/types';
import type { MapState } from '../app/mapState';
import { makeShape } from './geometry';
import MapGeometry from './MapGeometry';
import SceneRuntime from './SceneRuntime';
import { useSharedMaterials, type TextureStatus } from './materials';
import { initialQuality, qualitySettings, type QualityTier } from './quality';
import CameraController from './CameraController';
import { createCameraFrame, type CameraMetrics } from './cameraState';
import CampusLandscape from './CampusLandscape';
import AnnotationController from './annotations/AnnotationController';
import AnnotationOverlay from './annotations/AnnotationOverlay';
import { createAnnotationStore, type AnnotationStore } from './annotations/store';
import SportsMarkings from './SportsMarkings';

const CAMERA_OPTIONS = { position: [100, 2600, 2100] as [number, number, number], fov: 42, near: 5, far: 12000 };
const GL_OPTIONS = { antialias: true, powerPreference: 'high-performance' as const };

interface Props {
  data: Dataset;
  state: MapState;
  onSelect: (id: string, focus?: boolean) => void;
  onClear: () => void;
  onCluster: (ids: string[]) => void;
  onMetrics: (metrics: CameraMetrics) => void;
}

class SceneBoundary extends Component<{ children: ReactNode }, { error: string | null }> {
  state = { error: null as string | null };
  static getDerivedStateFromError(error: Error) { return { error: error.message }; }
  render() {
    return this.state.error ? <div className="scene-failure"><strong>三维视图暂不可用</strong><p>地点搜索和资料仍然可用。请检查浏览器硬件加速后刷新页面。</p><details><summary>查看原因</summary>{this.state.error}</details></div> : this.props.children;
  }
}

function Ground({ data }: { data: Dataset }) {
  const ground = useMemo(() => {
    const geometry = new THREE.ExtrudeGeometry(makeShape(data.boundary), { depth: 5, bevelEnabled: false, steps: 1 });
    geometry.rotateX(-Math.PI / 2); geometry.translate(0, -5, 0);
    return geometry;
  }, [data]);
  const boundary = useMemo(() => {
    const geometry = new THREE.BufferGeometry().setFromPoints(data.boundary[0].map(([x, z]) => new THREE.Vector3(x, .4, z)));
    const line = new THREE.Line(geometry, new THREE.LineDashedMaterial({ color: '#709ab0', dashSize: 9, gapSize: 7, transparent: true, opacity: .75 }));
    line.computeLineDistances(); return line;
  }, [data]);
  useEffect(() => () => { ground.dispose(); boundary.geometry.dispose(); (boundary.material as THREE.Material).dispose(); }, [ground, boundary]);
  return <>
    <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, -5.2, 0]} receiveShadow raycast={() => {}}><planeGeometry args={[16000, 16000]} /><meshStandardMaterial color="#e6edf0" roughness={1} /></mesh>
    <mesh geometry={ground} receiveShadow raycast={() => {}}><meshStandardMaterial color="#ced6be" roughness={1} /></mesh>
    <primitive object={boundary} raycast={() => {}} />
  </>;
}

function SceneContents({ data, state, onSelect, onMetrics, tier, onSlow, retry, onStatus, annotations }: Pick<Props, 'data' | 'state' | 'onSelect' | 'onMetrics'> & {
  tier: QualityTier; onSlow: () => void; retry: number;
  onStatus: (status: TextureStatus) => void; annotations: AnnotationStore;
}) {
  const buildingRef = useRef<THREE.Group | null>(null);
  const frame = useRef(createCameraFrame());
  const light = useRef<THREE.DirectionalLight | null>(null);
  const library = useSharedMaterials(tier, state.textures, retry, onStatus);
  return <>
    <hemisphereLight args={['#e0efff', '#a8a087', 1.5]} />
    <directionalLight ref={light} position={[-900, 1500, 800]} color="#fff5e5" intensity={2.3} castShadow
      shadow-mapSize={[2048, 2048]} shadow-camera-left={-1500} shadow-camera-right={1500}
      shadow-camera-top={1500} shadow-camera-bottom={-1500} shadow-camera-near={10}
      shadow-camera-far={5000} shadow-bias={-.00025} shadow-normalBias={.4} />
    <Ground data={data} />
    <MapGeometry data={data} state={state} library={library} onSelect={onSelect} buildingRef={buildingRef} />
    <CampusLandscape data={data} visible={state.layers.green} />
    <SportsMarkings data={data} visible={state.layers.sport} />
    <CameraController data={data} command={state.command} selectedId={state.selectedId} view={state.view} strictHeight={state.strictHeight} onMetrics={onMetrics} frame={frame} />
    <AnnotationController data={data} selectedId={state.selectedId} filters={state.filters} layers={state.layers} strictHeight={state.strictHeight}
      enabled={state.labelsVisible} buildingRef={buildingRef} tier={tier} store={annotations} frame={frame} />
    <SceneRuntime state={state} tier={tier} onSlow={onSlow} light={light} />
  </>;
}

const CampusScene = memo(function CampusScene({ data, state, onSelect, onClear, onCluster, onMetrics }: Props) {
  const [annotations] = useState(createAnnotationStore);
  const [tier, setTier] = useState<QualityTier>(initialQuality);
  const [status, setStatus] = useState<TextureStatus>({ pending: 0, loaded: 0, failed: [] });
  const [retry, setRetry] = useState(0);
  const pointer = useRef<[number, number]>([0, 0]);
  useEffect(() => { setTier(state.quality === 'auto' ? initialQuality() : state.quality); }, [state.quality]);
  const onSlow = useCallback(() => setTier('light'), []);
  const updateStatus = useCallback((next: TextureStatus) => setStatus(next), []);
  return <SceneBoundary>
    <div className="scene-canvas" data-quality={tier} data-textures-ready={status.pending === 0 && status.loaded > 0}
      onContextMenu={event => event.preventDefault()} onPointerDownCapture={event => { pointer.current = [event.clientX, event.clientY]; }}>
      <Canvas frameloop="demand" shadows dpr={Math.min(window.devicePixelRatio || 1, qualitySettings[tier].dpr)} camera={CAMERA_OPTIONS} gl={GL_OPTIONS}
        fallback={<div className="scene-failure">浏览器不支持 WebGL，仍可使用地点列表。</div>}
        onCreated={({ gl }) => { gl.setClearColor('#e6edf0'); gl.shadowMap.type = THREE.PCFSoftShadowMap; }}
        onPointerMissed={event => { if (Math.hypot(event.clientX - pointer.current[0], event.clientY - pointer.current[1]) <= 6) onClear(); }}>
        <SceneContents data={data} state={state} onSelect={onSelect} onMetrics={onMetrics} tier={tier}
          onSlow={onSlow} retry={retry} onStatus={updateStatus} annotations={annotations} />
      </Canvas>
      <AnnotationOverlay store={annotations} visible={state.labelsVisible} icons={state.layers.icons} pointer={pointer} onSelect={onSelect} onCluster={onCluster} />
      <div className="material-status" role="status" data-ready={status.pending === 0 && status.failed.length === 0}>
        {status.pending ? `正在加载表面贴图 ${status.loaded}/${status.loaded + status.pending + status.failed.length}` : status.failed.length ? `${status.failed.length}项贴图未加载，保留基础材质` : '表面材质已加载'}
        {status.failed.length > 0 && !status.pending && <button onClick={() => setRetry(value => value + 1)}>重试</button>}
      </div>
    </div>
  </SceneBoundary>;
});
export default CampusScene;
