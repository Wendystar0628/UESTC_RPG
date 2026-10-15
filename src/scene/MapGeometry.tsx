import { useEffect, useMemo, useState, type RefObject } from 'react';
import { useThree, type ThreeEvent } from '@react-three/fiber';
import * as THREE from 'three';
import type { Dataset, Entity } from '../data/types';
import type { MapState } from '../app/mapState';
import { layerVisible, matchesCategories } from '../data/search';
import { compileScene, updateBatchEmphasis, type CompiledScene } from './batches';
import type { MaterialLibrary } from './materials';

interface Props {
  data: Dataset;
  state: MapState;
  library: MaterialLibrary;
  buildingRef: RefObject<THREE.Group | null>;
  onSelect: (id: string, focus?: boolean) => void;
}

export default function MapGeometry({ data, state, library, onSelect, buildingRef }: Props) {
  const { scene, gl, invalidate } = useThree();
  const byId = useMemo(() => new Map(data.entities.map(e => [e.id, e])), [data]);
  const effects = useMemo(() => ({
    edge: new THREE.LineBasicMaterial({ color: '#717a77', transparent: true, opacity: .13 }),
    selected: new THREE.MeshBasicMaterial({ color: '#379beb', transparent: true, opacity: .14, depthWrite: false, polygonOffset: true, polygonOffsetFactor: -2, polygonOffsetUnits: -20 }),
    hover: new THREE.MeshBasicMaterial({ color: '#9ac5e8', transparent: true, opacity: .07, depthWrite: false, polygonOffset: true, polygonOffsetFactor: -2, polygonOffsetUnits: -20 }),
    outline: new THREE.LineBasicMaterial({ color: '#1a76bb', transparent: true, opacity: .95, depthTest: true }),
  }), []);
  useEffect(() => () => Object.values(effects).forEach(material => material.dispose()), [effects]);
  const selected = state.selectedId ? byId.get(state.selectedId) : undefined;
  const selectedIds = useMemo(() => new Set(selected?.featureIds || []), [selected]);
  const [hoverId, setHoverId] = useState<string | null>(null);
  const variants = useMemo(() => new Map<boolean, CompiledScene>(), [data]);
  const compiled = useMemo(() => {
    let value = variants.get(state.strictHeight);
    if (!value) {
      value = compileScene(data, state.strictHeight);
      variants.set(state.strictHeight, value);
      scene.userData.campusBuilds = (scene.userData.campusBuilds || 0) + 1;
    }
    return value;
  }, [data, variants, state.strictHeight, scene]);
  useEffect(() => () => { variants.forEach(value => value.dispose()); }, [variants]);
  useEffect(() => {
    scene.userData.campusBatches = compiled.batches.length;
    scene.userData.campusTriangles = compiled.triangleCount;
    scene.userData.campusGeometryIds = compiled.batches.map(batch => batch.geometry.uuid);
    gl.shadowMap.needsUpdate = true;
    invalidate();
  }, [compiled, scene, gl, invalidate]);

  useEffect(() => {
    for (const batch of compiled.batches) {
      updateBatchEmphasis(batch, index => {
        const feature = data.features[index];
        const entity = feature.entityId ? byId.get(feature.entityId) : undefined;
        return !entity || selectedIds.has(feature.id) || matchesCategories(entity, state.filters);
      });
    }
    invalidate();
  }, [compiled, data, byId, state.filters, selectedIds, invalidate]);
  useEffect(() => { setHoverId(null); }, [state.filters, state.layers]);

  const wanted = selected || (hoverId ? byId.get(hoverId) : undefined);
  const wantedParts = useMemo(() => wanted ? [...new Set(wanted.featureIds)].flatMap(id => compiled.partsByFeature.get(id) || []) : [], [wanted, compiled]);
  const highlighted = useMemo(() => wantedParts.filter(part => !part.detail), [wantedParts]);
  const highlightedEdges = useMemo(() => highlighted.map(part => new THREE.EdgesGeometry(part.geometry, 40)), [highlighted]);
  useEffect(() => () => highlightedEdges.forEach(geometry => geometry.dispose()), [highlightedEdges]);

  const pick = (event: ThreeEvent<PointerEvent | MouseEvent>): Entity | undefined => {
    const geometry = (event.object as THREE.Mesh).geometry;
    if (!event.face || !geometry) return;
    const index = geometry.getAttribute('featureIndex')?.getX(event.face.a);
    const feature = index === undefined ? undefined : data.features[index];
    const entity = feature?.entityId ? byId.get(feature.entityId) : undefined;
    return entity && (selectedIds.has(feature!.id) || matchesCategories(entity, state.filters) && layerVisible(entity, state.layers)) ? entity : undefined;
  };
  const onClick = (event: ThreeEvent<MouseEvent>) => {
    if (event.delta > 6) return;
    const entity = pick(event);
    if (entity) { event.stopPropagation(); onSelect(entity.id); }
  };
  const onMove = (event: ThreeEvent<PointerEvent>) => {
    const entity = pick(event);
    if (entity) event.stopPropagation();
    setHoverId(entity?.id || null);
    document.body.style.cursor = entity ? 'pointer' : '';
  };
  const onOut = () => { setHoverId(null); document.body.style.cursor = ''; };
  const drawBatch = (batch: CompiledScene['batches'][number]) => {
    const visible = !(batch.kind in state.layers) || state.layers[batch.kind as keyof typeof state.layers];
    return <group key={batch.id} visible={visible} userData={{ campusDetail: batch.detail }}>
      <mesh geometry={batch.geometry} material={library[batch.materialKey]}
        name={`campus/${batch.id}`} castShadow={batch.kind === 'building' && (!batch.detail || batch.materialKey === 'roof-metal')} receiveShadow
        renderOrder={batch.kind === 'building' ? 6 : batch.kind === 'road' ? 5 : batch.kind === 'sport' ? 4 : batch.kind === 'water' ? 3 : 2}
        onClick={onClick} onPointerMove={onMove} onPointerOut={onOut} />
      {batch.edges && <lineSegments geometry={batch.edges} material={effects.edge} raycast={() => {}} />}
    </group>;
  };
  return <group dispose={null}>
    <group ref={buildingRef}>{compiled.batches.filter(batch => batch.kind === 'building').map(drawBatch)}</group>
    {compiled.batches.filter(batch => batch.kind !== 'building').map(drawBatch)}
    {selected && wantedParts.filter(part => part.feature.kind in state.layers && !state.layers[part.feature.kind as keyof typeof state.layers]).map((part, i) =>
      <mesh key={`reveal-${part.feature.id}-${part.role}-${i}`} geometry={part.geometry} material={library[part.materialKey]} castShadow={part.feature.kind === 'building' && !part.detail} receiveShadow onClick={onClick} />)}
    {highlighted.map((part, i) => <group key={`highlight-${part.feature.id}-${part.role}`}>
      <mesh geometry={part.geometry} material={selected ? effects.selected : effects.hover} position={[0, .08, 0]} raycast={() => {}} renderOrder={8} />
      <lineSegments geometry={highlightedEdges[i]} material={effects.outline} position={[0, .1, 0]} raycast={() => {}} renderOrder={9} />
    </group>)}
  </group>;
}
