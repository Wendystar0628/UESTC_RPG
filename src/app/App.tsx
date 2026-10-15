import { useCallback, useEffect, useMemo, useReducer, useRef, useState } from 'react';
import { CircleHelp, MapPinned } from 'lucide-react';
import { loadCampusDataset, type Dataset } from '../data/types';
import { initialState, mapReducer } from './mapState';
import MapViewport from './MapViewport';
import PlaceExplorer from './PlaceExplorer';
import InfoDialog from '../ui/InfoDialog';

export default function App() {
  const [data, setData] = useState<Dataset | null>(null);
  const [error, setError] = useState('');
  const [retry, setRetry] = useState(0);
  const [state, dispatch] = useReducer(mapReducer, initialState);
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [popover, setPopover] = useState<'layers' | 'help' | 'data' | null>(null);
  const [clusterIds, setClusterIds] = useState<string[]>([]);
  const input = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const controller = new AbortController(); setError('');
    loadCampusDataset(controller.signal).then(setData).catch(e => { if (e.name !== 'AbortError') setError(e.message); });
    return () => controller.abort();
  }, [retry]);

  const byId = useMemo(() => new Map(data?.entities.map(e => [e.id, e]) || []), [data]);
  const onSelect = useCallback((id: string, focus = false) => {
    const entity = byId.get(id); if (!entity) return;
    dispatch({ type: 'select', id, focus, canFocus: !!entity.anchor && entity.locationStatus !== 'unlocated' });
    setClusterIds([]); setPopover(null);
    if (focus || window.innerWidth < 1024) setSidebarOpen(false);
  }, [byId]);
  const onClear = useCallback(() => { dispatch({ type: 'clear' }); setClusterIds([]); }, []);
  const onCluster = useCallback((ids: string[]) => { setClusterIds(ids); dispatch({ type: 'command', kind: 'cluster', ids }); }, []);

  useEffect(() => {
    const escape = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return;
      if (popover) setPopover(null);
      else if (clusterIds.length) setClusterIds([]);
      else if (sidebarOpen) setSidebarOpen(false);
      else onClear();
    };
    window.addEventListener('keydown', escape);
    return () => window.removeEventListener('keydown', escape);
  }, [popover, clusterIds, sidebarOpen, onClear]);

  return <div className="app-shell">
    <header className="site-header">
      <div className="brand"><span className="brand-mark"><MapPinned size={26} strokeWidth={1.6} /></span><div><span className="brand-en">UESTC CAMPUS ATLAS</span><h1>清水河<span>校园地图</span></h1></div></div>
      <div className="header-right"><span className="campus-status"><i />电子科技大学 · 清水河校区</span><button className="header-help" onClick={() => setPopover('help')} aria-label="操作帮助"><CircleHelp size={18} /><span>操作指南</span></button></div>
    </header>
    <main className="workspace">
      {sidebarOpen && <div className="sidebar-scrim" onClick={() => setSidebarOpen(false)} />}
      <PlaceExplorer data={data} error={error} state={state} dispatch={dispatch} sidebarOpen={sidebarOpen} input={input}
        onSelect={onSelect} onClose={() => setSidebarOpen(false)} onAbout={() => setPopover('data')} onRetry={() => setRetry(n => n + 1)} />
      <MapViewport data={data} error={error} state={state} dispatch={dispatch} onSelect={onSelect} onClear={onClear}
        onCluster={onCluster} clusterIds={clusterIds} setClusterIds={setClusterIds} popover={popover} setPopover={setPopover}
        onOpenSearch={() => { setSidebarOpen(true); setTimeout(() => input.current?.focus(), 60); }} />
    </main>
    {(popover === 'help' || popover === 'data') && <InfoDialog kind={popover} data={data} onClose={() => setPopover(null)} />}
  </div>;
}
