import { ArrowUpRight, ExternalLink, LocateFixed, MapPin, X } from 'lucide-react';
import type { Dataset, Entity } from '../data/types';
import { categoryPath } from '../data/search';
import PlaceIcon from './PlaceIcon';
import { appearanceSummary } from '../data/appearance';

export default function EntityDetails({ entity, data, hidden, onClose, onSelect }: { entity: Entity; data: Dataset; hidden: boolean; onClose: () => void; onSelect: (id: string, focus?: boolean) => void }) {
  const features = data.features.filter(f => entity.featureIds.includes(f.id));
  const appearance = appearanceSummary(features);
  const hasBuilding = features.some(feature => feature.kind === 'building');
  const bounds = entity.bounds;
  const width = bounds ? Math.max(bounds[2] - bounds[0], 40) : 100;
  const height = bounds ? Math.max(bounds[3] - bounds[1], 40) : 100;
  const pad = Math.max(width, height) * .12;
  const children = data.entities.filter(e => e.parentId === entity.id);
  const categorySource = data.entities.find(e => e.id === entity.categorySourceId);
  const usageTags = ['building', 'building:part', 'amenity', 'natural', 'landuse']
    .filter(key => entity.sourceTags[key] && !['yes', 'no'].includes(entity.sourceTags[key]))
    .map(key => `${key}=${entity.sourceTags[key]}`);
  const sourceHeight = entity.height !== null;
  const heightText = sourceHeight ? `${entity.height} m` : entity.levels ? `≈ ${(entity.levels * 3.3).toFixed(1)} m` : '未录入';
  return <aside className="entity-details" aria-label="地点详情" data-entity-id={entity.id}>
    <div className="details-topline"><span><PlaceIcon name={entity.icon} size={17} />{categoryPath(entity, data.categories)}</span><button className="icon-button" aria-label="关闭地点详情" onClick={onClose}><X size={19} /></button></div>
    <div className="details-heading"><h2>{entity.name}</h2>{entity.aliases.length > 0 && <p>{entity.aliases.slice(0, 3).join(' · ')}</p>}</div>
    <div className="footprint-preview" aria-label="数据中的地点轮廓">
      {bounds && <svg viewBox={`${bounds[0] - pad} ${bounds[1] - pad} ${width + pad * 2} ${height + pad * 2}`} preserveAspectRatio="xMidYMid meet">
        {features.length ? features.flatMap(feature => feature.polygons.map((rings, index) => <path key={`${feature.id}-${index}`} d={rings.map(ring => `M${ring.map(([x, z]) => `${x},${z}`).join('L')}Z`).join('')} fillRule="evenodd" fill={feature.kind === 'water' ? '#8dcbdc' : '#bed7f0'} stroke="#4382b8" strokeWidth={Math.max(width, height) / 140} />)) : <circle cx={(bounds[0] + bounds[2]) / 2} cy={(bounds[1] + bounds[3]) / 2} r={Math.max(width, height) * .08} fill="#368ae0" />}
      </svg>}
      <span className="preview-caption">{features.length ? '地理轮廓' : '设施点位'}<span>N ↑</span></span>
    </div>
    <div className="details-body">
      <p className="location-line"><MapPin size={15} />{entity.parentName || '电子科技大学 · 清水河校区'}</p>
      {hidden && <p className="inline-notice">已临时显示筛选外目标；关闭详情后恢复。</p>}
      {entity.description && <p className="description">{entity.description}</p>}
      <dl className="entity-facts">
        {entity.kind === 'building' && <><div><dt>{features.length > 1 ? '主体高度' : '建筑高度'}</dt><dd>{heightText}</dd><small>{sourceHeight ? '来源标签，非独立实测' : entity.levels ? `按 ${entity.levels} 层 × 3.3 m 估算` : '场景使用示意体量'}</small></div><div><dt>{features.length > 1 ? '主体层数' : '楼层记录'}</dt><dd>{entity.levels ? `${entity.levels} 层` : '未录入'}</dd><small>OpenStreetMap 数据</small></div></>}
        {entity.kind !== 'building' && <div className="wide-fact"><dt>位置依据</dt><dd>{entity.locationStatus === 'parent' ? '所在建筑' : entity.locationStatus === 'unlocated' ? '待核验' : entity.locationStatus === 'point' ? '来源点位' : '来源轮廓'}</dd><small>位置与名称来自公开地图记录</small></div>}
      </dl>
      {entity.id === 'qsh:way:217855275' && <p className="description">场馆总高 26 m，依据校方大运会场馆介绍；场景屋盖按此高度重建，上方仍保留原地图的楼层估算记录。</p>}
      {features.length > 0 && <section className="appearance-info" aria-label="地点材质"><span className="appearance-label">{hasBuilding ? '建筑与材质' : '场地材质'}</span><p>{appearance.labels.slice(0, 4).join(' · ')}</p><small>{!hasBuilding ? '场地材质与设施细节为示意' : appearance.references.length ? '参考校园实拍的外观重建 · 窗格尺寸为程序化表达' : appearance.sourced ? '来源材料标签与校园通用风格 · 细部为示意' : '校园统一风格 · 窗格与屋面细部为示意'}</small>{appearance.references.length > 0 && <details><summary>外观参考</summary>{appearance.references.map(reference => <p key={reference.url}><a href={reference.url} target="_blank" rel="noreferrer">{reference.title} <ExternalLink size={12} /></a></p>)}</details>}</section>}
      {children.length > 0 && <div className="contained-places"><h3>这里的地点 <span>{children.length}</span></h3>{children.slice(0, 6).map(child => <button key={child.id} onClick={() => onSelect(child.id, true)}><PlaceIcon name={child.icon} size={15} /><span>{child.name}</span><ArrowUpRight size={15} /></button>)}</div>}
      <details className="source-details"><summary>数据来源与备注</summary><p>{entity.enrichment ? '此地点已补充来源核对；名称、用途与建筑归属的依据分别列于下方。' : entity.named ? '名称采用来源记录；用途按公开标签及名称归类。' : '来源尚无正式名称，当前显示类型与记录尾号。'}{features.length > 1 && '各楼体保留各自的几何和高度记录；楼群归属不代表各分楼已命名。'}</p>{entity.updatedAt && <p>OSM 对象更新：{entity.updatedAt.slice(0, 10)}</p>}<a href={entity.source} target="_blank" rel="noreferrer">查看 OpenStreetMap 源对象 <ExternalLink size={12} /></a>
        {categorySource && <p>用途分类沿用所属建筑：{categorySource.name}（{categoryPath(categorySource, data.categories)}）。<a href={categorySource.source} target="_blank" rel="noreferrer">查看分类依据 <ExternalLink size={12} /></a></p>}
        {usageTags.length > 0 && <p>原始用途标签：{usageTags.join(' · ')}</p>}
        {entity.enrichment && <div className="enrichment-sources"><p>补充核验：{entity.enrichment.reviewedAt}</p>{entity.enrichment.evidence.map(source => <div className="enrichment-source" key={source.url}><a href={source.url} target="_blank" rel="noreferrer">{source.title} <ExternalLink size={12} /></a><p>{source.fields.join(' · ')}{source.publishedAt && ` · 发布于 ${source.publishedAt}`}</p><p>{source.note}</p></div>)}{entity.enrichment.memberSources.length > 0 && <details><summary>关联的原始记录（{entity.enrichment.memberSources.length}）</summary>{entity.enrichment.memberSources.map(url => <a className="member-source" key={url} href={url} target="_blank" rel="noreferrer">{url.replace('https://www.openstreetmap.org/', '')} <ExternalLink size={12} /></a>)}</details>}</div>}
      </details>
    </div>
    <div className="details-actions"><button className="primary-button" disabled={!entity.anchor || entity.locationStatus === 'unlocated'} onClick={() => onSelect(entity.id, true)}><LocateFixed size={17} />{entity.locationStatus === 'parent' ? '跳转到所在建筑' : '跳转到此处'}<ArrowUpRight size={17} /></button></div>
  </aside>;
}
