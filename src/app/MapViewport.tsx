import { lazy, Suspense, useCallback, useMemo, useState, type Dispatch, type SetStateAction } from 'react';
import { ArrowUpRight, Box, Layers3, LoaderCircle, LocateFixed, Map as MapIcon, MapPinned, Minus, Navigation, Plus, Search, Tags, X } from 'lucide-react';
import type { Dataset, Entity, LayerKey } from '../data/types';
import { layerVisible, matchesCategories } from '../data/search';
import type { MapState, Action } from './mapState';
import type { CameraMetrics } from '../scene/cameraState';
import type { QualityMode } from '../scene/quality';
import PlaceIcon from '../ui/PlaceIcon';
import EntityDetails from '../ui/EntityDetails';

const CampusScene = lazy(() => import('../scene/CampusScene'));
const layerLabels: [LayerKey, string][] = [['building', '建筑体量'], ['road', '道路与步道'], ['water', '湖泊与水系'], ['green', '绿地'], ['sport', '运动场地'], ['icons', '类型图标'], ['names', '名称标注']];

interface Props {
  data: Dataset | null;
  error: string;
  state: MapState;
  dispatch: Dispatch<Action>;
  onSelect: (id: string, focus?: boolean) => void;
  onClear: () => void;
  onCluster: (ids: string[]) => void;
  clusterIds: string[];
  setClusterIds: Dispatch<SetStateAction<string[]>>;
  popover: 'layers' | 'help' | 'data' | null;
  setPopover: Dispatch<SetStateAction<'layers' | 'help' | 'data' | null>>;
  onOpenSearch: () => void;
}

/** Camera telemetry stays in the viewport; dragging never re-renders the explorer. */
export default function MapViewport({ data, error, state, dispatch, onSelect, onClear, onCluster, clusterIds, setClusterIds, popover, setPopover, onOpenSearch }: Props) {
  const [metrics, setMetrics] = useState<CameraMetrics>({ azimuth: 0, metresPerPixel: 3, target: '', moving: false });
  const onMetrics = useCallback((next: CameraMetrics) => setMetrics(previous => previous.target === next.target && Math.abs(previous.azimuth - next.azimuth) < .15 && previous.moving === next.moving && Math.abs(previous.metresPerPixel - next.metresPerPixel) < .002 ? previous : next), []);
  const byId = useMemo(() => new Map(data?.entities.map(entity => [entity.id, entity]) || []), [data]);
  const selected = state.selectedId ? byId.get(state.selectedId) || null : null;
  const hiddenSelection = selected ? !matchesCategories(selected, state.filters) || !layerVisible(selected, state.layers) : false;
  const scaleMetres = [10, 20, 50, 100, 200, 500, 1000].reduce((best, value) => Math.abs(value / metrics.metresPerPixel - 85) < Math.abs(best / metrics.metresPerPixel - 85) ? value : best, 100);
  return (
      <section className={`map-view ${selected ? 'has-selection' : ''}`} aria-label="三维校园地图" data-camera-target={metrics.target} data-camera-status={metrics.moving ? 'moving' : 'idle'}>
        {data ? <Suspense fallback={<div className="scene-loading"><LoaderCircle className="spin" size={30} /><span>正在构建校园空间</span></div>}><CampusScene data={data} state={state} onSelect={onSelect} onClear={onClear} onCluster={onCluster} onMetrics={onMetrics} /></Suspense> : <div className="scene-loading"><MapPinned size={38} /><span>{error ? '请先加载校园数据' : '清水河，即将展开'}</span></div>}
        <div className="map-context"><span className="context-line">QINGSHUIHE / CHENGDU</span><h2>{selected ? '地点探索' : state.filters.length ? '分类探索' : '校园总览'}</h2><p>{data ? `${data.meta.stats.buildings} 个建筑轮廓 · ${data.meta.stats.places} 个地点记录` : '从真实地理轮廓开始'}</p></div>
        <button className="mobile-search mobile-only" onClick={onOpenSearch}><Search size={18} />搜索校园</button>
        <div className="view-switch" role="group" aria-label="地图视角"><button aria-pressed={state.view === '3d'} onClick={() => dispatch({ type: 'command', kind: 'perspective' })}><Box size={15} />3D</button><button aria-pressed={state.view === 'top'} onClick={() => dispatch({ type: 'command', kind: 'top' })}><MapIcon size={15} />俯视</button></div>
        <button className="map-label-toggle" aria-label="建筑标签" aria-pressed={state.labelsVisible} title={state.labelsVisible ? '隐藏建筑与地点标识' : '显示建筑与地点标识'} onClick={() => { dispatch({ type: 'labels' }); setClusterIds([]); }}><Tags size={17} /><span className="label-toggle-long">建筑标签</span><span className="label-toggle-short" aria-hidden="true">标签</span><i aria-hidden="true" /></button>
        {selected && data && <EntityDetails entity={selected} data={data} hidden={hiddenSelection} onClose={onClear} onSelect={onSelect} />}
        {clusterIds.length > 0 && <aside className="cluster-panel" aria-label="相邻地点列表"><div><strong>这里有 {clusterIds.length} 个地点</strong><button className="icon-button" aria-label="关闭相邻地点" onClick={() => setClusterIds([])}><X size={16} /></button></div>{clusterIds.map(id => byId.get(id)).filter((e): e is Entity => !!e).map(entity => <button className="cluster-item" key={entity.id} onClick={() => onSelect(entity.id, true)}><PlaceIcon name={entity.icon} size={16} /><span>{entity.name}</span><ArrowUpRight size={15} /></button>)}</aside>}
        <div className="map-tools"><button className="compass-button" aria-label="恢复北向" title="恢复北向（网格北）" onClick={() => dispatch({ type: 'command', kind: 'north' })}><span>N</span><Navigation size={22} fill="currentColor" strokeWidth={1.3} style={{ transform: `rotate(${-metrics.azimuth}deg)` }} /></button><div className="tool-stack"><button aria-label="放大地图" title="放大" onClick={() => dispatch({ type: 'command', kind: 'zoomIn' })}><Plus size={20} /></button><button aria-label="缩小地图" title="缩小" onClick={() => dispatch({ type: 'command', kind: 'zoomOut' })}><Minus size={20} /></button></div><div className="tool-stack"><button aria-label="返回全校总览" title="返回全校" onClick={() => { setClusterIds([]); dispatch({ type: 'command', kind: 'home' }); }}><LocateFixed size={20} /></button><button aria-label="图层设置" title="图层设置" aria-expanded={popover === 'layers'} onClick={() => setPopover(popover === 'layers' ? null : 'layers')}><Layers3 size={20} /></button></div></div>
        {popover === 'layers' && <div className="layer-popover"><div className="popover-title"><strong>地图图层</strong><button className="icon-button" aria-label="关闭图层设置" onClick={() => setPopover(null)}><X size={17} /></button></div>{layerLabels.map(([key, label]) => <label key={key}><span>{label}</span><input type="checkbox" checked={state.layers[key]} onChange={() => dispatch({ type: 'layer', key })} /><span className="switch" /></label>)}<div className="layer-divider" /><div className="quality-control"><label htmlFor="quality-mode">画面质量</label><select id="quality-mode" aria-label="画面质量" value={state.quality} onChange={event => dispatch({ type: 'quality', value: event.target.value as QualityMode })}><option value="auto">自动</option><option value="light">轻量</option><option value="standard">标准</option><option value="high">高清</option></select></div><label><span>表面贴图<small>共享材质，不改变地理形状</small></span><input type="checkbox" checked={state.textures} onChange={() => dispatch({ type: 'textures' })} /><span className="switch" /></label><label><span>仅显示来源楼高<small>其余建筑保留平面轮廓</small></span><input type="checkbox" checked={state.strictHeight} onChange={() => dispatch({ type: 'height' })} /><span className="switch" /></label></div>}
        <div className="map-bottom"><div className="scale-bar" title="视图中心附近的近似水平比例尺"><span>{scaleMetres >= 1000 ? `${scaleMetres / 1000} km` : `${scaleMetres} m`}</span><i style={{ width: Math.min(150, scaleMetres / metrics.metresPerPixel) }} /></div><span className="gesture-hint">左键平移 · 右键旋转 · 滚轮缩放</span></div>
        <div className="map-attribution"><a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noreferrer">© OpenStreetMap contributors</a><span>·</span><button onClick={() => setPopover('data')}>数据与精度</button>{data && <span className="data-date">{data.meta.date}</span>}</div>
      </section>
  );
}
