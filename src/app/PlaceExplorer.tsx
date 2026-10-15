import { useEffect, useMemo, useRef, useState, type Dispatch, type RefObject } from 'react';
import { ArrowUpRight, ChevronDown, ChevronLeft, LoaderCircle, Map as MapIcon, Search, SlidersHorizontal, X } from 'lucide-react';
import type { Dataset, Entity } from '../data/types';
import { buildSearchIndex, categoryPath, layerVisible, matchesCategories, searchPlaces } from '../data/search';
import type { MapState, Action } from './mapState';
import PlaceIcon from '../ui/PlaceIcon';

interface Props {
  data: Dataset | null; error: string; state: MapState; dispatch: Dispatch<Action>;
  sidebarOpen: boolean; input: RefObject<HTMLInputElement | null>;
  onSelect: (id: string, focus?: boolean) => void;
  onClose: () => void; onAbout: () => void; onRetry: () => void;
}

export default function PlaceExplorer({ data, error, state, dispatch, sidebarOpen, input, onSelect, onClose, onAbout, onRetry }: Props) {
  const [query, setQuery] = useState('');
  const [activeIndex, setActiveIndex] = useState(0);
  const [limit, setLimit] = useState(40);
  const [expandedCategory, setExpandedCategory] = useState<string | null>(null);
  const composing = useRef(false);
  const index = useMemo(() => data ? buildSearchIndex(data.entities, data.categories) : [], [data]);
  const counts = useMemo(() => {
    const result: Record<string, number> = {};
    for (const entity of data?.entities || []) {
      result[entity.category] = (result[entity.category] || 0) + 1;
      if (entity.subcategory) { const key = `${entity.category}:${entity.subcategory}`; result[key] = (result[key] || 0) + 1; }
    }
    return result;
  }, [data]);
  const duplicateNames = useMemo(() => {
    const result = new Map<string, number>();
    data?.entities.forEach(e => result.set(e.name, (result.get(e.name) || 0) + 1));
    return result;
  }, [data]);
  const results = useMemo(() => searchPlaces(index, query, state.filters, state.scope), [index, query, state.filters, state.scope]);
  useEffect(() => { setLimit(40); setActiveIndex(0); }, [query, state.filters, state.scope]);

  const nearbyLabel = (entity: Entity) => entity.parentName || (entity.anchor ? `${entity.anchor[2] < -200 ? '校区北部' : entity.anchor[2] > 250 ? '校区南部' : '校区中部'}${(duplicateNames.get(entity.name) || 0) > 1 ? ` · 记录 ${entity.id.split(':').pop()?.slice(-4)}` : ''}` : '位置待核验');

  return (
      <aside className={`explorer ${sidebarOpen ? 'open' : ''}`} aria-label="地点检索与分类">
        <div className="explorer-intro"><div><span className="eyebrow">EXPLORE THE CAMPUS</span><h2>从这里，认识清水河。</h2></div><button className="icon-button mobile-only" onClick={() => onClose()} aria-label="收起搜索"><ChevronLeft size={20} /></button></div>
        <div className="search-section">
          <div className="search-box"><Search size={18} /><input ref={input} placeholder="搜索建筑、地点或设施" aria-label="搜索建筑、地点或设施" role="combobox" aria-expanded={!!data} aria-controls="place-results" aria-activedescendant={results[activeIndex] ? `result-${results[activeIndex].id}` : undefined} value={query}
            onChange={event => setQuery(event.target.value)} onCompositionStart={() => { composing.current = true; }} onCompositionEnd={() => { composing.current = false; }}
            onKeyDown={event => {
              if (composing.current || event.nativeEvent.isComposing) return;
              if (event.key === 'ArrowDown' || event.key === 'ArrowUp') { event.preventDefault(); const next = Math.max(0, Math.min(results.length - 1, activeIndex + (event.key === 'ArrowDown' ? 1 : -1))); setActiveIndex(next); if (next >= limit) setLimit(next + 1); document.getElementById(`result-${results[next]?.id}`)?.scrollIntoView({ block: 'nearest' }); }
              if (event.key === 'Enter' && results[activeIndex]) { event.preventDefault(); onSelect(results[activeIndex].id, true); }
              if (event.key === 'Escape') { event.stopPropagation(); input.current?.blur(); if (window.innerWidth < 1024) onClose(); }
            }} />{query && <button aria-label="清除搜索" className="search-clear" onClick={() => { setQuery(''); input.current?.focus(); }}><X size={15} /></button>}</div>
          <div className="search-scope"><span>检索范围</span><button aria-pressed={state.scope === 'all'} onClick={() => dispatch({ type: 'scope', value: 'all' })}>全校</button><button aria-pressed={state.scope === 'current'} onClick={() => dispatch({ type: 'scope', value: 'current' })}>当前分类</button></div>
        </div>
        <div className="category-section"><div className="section-caption"><span><SlidersHorizontal size={13} />按类型探索</span>{state.filters.length > 0 && <button onClick={() => dispatch({ type: 'filter' })}>清除筛选</button>}</div>
          <div className="categories"><button className={`category-button ${!state.filters.length ? 'active' : ''}`} aria-pressed={!state.filters.length} onClick={() => dispatch({ type: 'filter' })}><MapIcon size={17} /><span>全部地点</span><small>{data?.entities.length || '—'}</small></button>
            {data?.categories.filter(category => counts[category.id]).map(category => <div className="category-wrap" key={category.id}><button className={`category-button ${state.filters.includes(category.id) || state.filters.some(id => id.startsWith(category.id + ':')) ? 'active' : ''}`} aria-label={`筛选${category.label}`} aria-pressed={state.filters.includes(category.id)} onClick={() => { dispatch({ type: 'filter', id: category.id }); setExpandedCategory(expandedCategory === category.id ? null : category.id); }}><PlaceIcon name={category.icon} size={17} /><span>{category.label}</span><small>{counts[category.id]}</small></button></div>)}
          </div>
          {expandedCategory && data?.categories.find(c => c.id === expandedCategory)?.children.some(c => counts[`${expandedCategory}:${c.id}`]) && <div className="subcategories">{data.categories.find(c => c.id === expandedCategory)!.children.filter(c => counts[`${expandedCategory}:${c.id}`]).map(child => <button key={child.id} className={state.filters.includes(`${expandedCategory}:${child.id}`) ? 'active' : ''} onClick={() => dispatch({ type: 'filter', id: `${expandedCategory}:${child.id}` })}><PlaceIcon name={child.icon} size={14} />{child.label}</button>)}</div>}
        </div>
        <div className="results-heading"><span>{query ? '搜索结果' : state.filters.length ? '分类地点' : '校园地点'}</span><span>{data ? `${results.length} 个` : '加载中'}<span className="result-hint">点击跳转</span></span></div>
        <div className="result-scroll" id="place-results" role="listbox" aria-label="地点搜索结果">
          {!data && !error && <div className="list-loading"><LoaderCircle className="spin" size={22} />正在加载校园数据</div>}
          {error && <div className="empty-results"><strong>数据加载失败</strong><p>{error}</p><button onClick={() => onRetry()}>重新加载</button></div>}
          {data && results.length === 0 && <div className="empty-results"><Search size={28} /><strong>没有找到匹配的地点</strong><p>{state.scope === 'current' ? '试试扩大检索范围，或使用其他名称。' : '可尝试正式名称、别名或地点类型。'}</p>{state.scope === 'current' && <button onClick={() => dispatch({ type: 'scope', value: 'all' })}>改为全校搜索</button>}</div>}
          {data && results.slice(0, limit).map((entity, index) => <div id={`result-${entity.id}`} role="option" aria-selected={state.selectedId === entity.id} className={`result-row ${state.selectedId === entity.id ? 'selected' : ''} ${query && activeIndex === index ? 'keyboard-active' : ''}`} key={entity.id}>
            <button className="result-main" onClick={() => onSelect(entity.id, true)} title={`${entity.name} · ${categoryPath(entity, data.categories)}`}><span className={`result-icon category-${entity.category}`}><PlaceIcon name={entity.icon} size={19} /></span><span className="result-copy"><strong>{entity.name}</strong><small>{nearbyLabel(entity)}</small>{query && (!matchesCategories(entity, state.filters) || !layerVisible(entity, state.layers)) && <em>当前筛选外</em>}</span></button>
            <button className="result-jump" aria-label={`跳转到${entity.name}`} title="跳转到此处" disabled={!entity.anchor} onClick={() => onSelect(entity.id, true)}><ArrowUpRight size={17} /></button>
          </div>)}
          {results.length > limit && <button className="show-more" onClick={() => setLimit(n => n + 40)}>显示更多地点 <ChevronDown size={15} /></button>}
        </div>
        <div className="explorer-footer"><span className="live-dot" /><span>数据驱动的校园空间</span><button onClick={() => onAbout()}>关于数据 <ArrowUpRight size={12} /></button></div>
      </aside>
  );
}
