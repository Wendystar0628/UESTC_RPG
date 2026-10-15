import { useEffect, useRef } from 'react';
import { Compass, ExternalLink, Layers3, LocateFixed, Search, X } from 'lucide-react';
import type { Dataset } from '../data/types';

export default function InfoDialog({ kind, data, onClose }: { kind: 'help' | 'data'; data: Dataset | null; onClose: () => void }) {
  const dialog = useRef<HTMLDivElement>(null);
  useEffect(() => { dialog.current?.querySelector<HTMLButtonElement>('button')?.focus(); }, [kind]);
  return (
    <div className="modal-backdrop" onClick={() => onClose()}><div className="info-dialog" role="dialog" aria-modal="true" aria-labelledby="info-dialog-title" ref={dialog} onClick={e => e.stopPropagation()} onKeyDown={event => { if (event.key !== 'Tab') return; const buttons = dialog.current?.querySelectorAll<HTMLElement>('button, a[href], input'); if (!buttons?.length) return; const first = buttons[0], last = buttons[buttons.length - 1]; if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); } else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); } }}>
      <div className="dialog-title"><span className="eyebrow">{kind === 'help' ? 'A SMALL FIELD GUIDE' : 'ABOUT THIS MAP'}</span><button className="icon-button" aria-label="关闭说明" onClick={() => onClose()}><X size={20} /></button></div>
      <h2 id="info-dialog-title">{kind === 'help' ? '探索校园，不必迷路。' : '真实轮廓，清楚的边界。'}</h2>
      {kind === 'help' ? <><p>拖动、旋转，从不同角度认识清水河。地图上的标记和左侧列表指向同一个地点。</p><div className="help-grid"><div><Compass size={22} /><strong>自由浏览</strong><span>左键平移，右键旋转，滚轮缩放；触摸设备单指平移、双指缩放旋转。</span></div><div><Search size={22} /><strong>检索与跳转</strong><span>输入名称、别名或类型，点击结果移动镜头；手动操作随时中止过渡。</span></div><div><Layers3 size={22} /><strong>按需查看</strong><span>分类筛选关注的地点；图层设置可独立切换建筑、道路、图标和名称。</span></div><div><LocateFixed size={22} /><strong>回到全校</strong><span>右侧定位按钮恢复总览。跳转是相机定位，不是步行路线导航。</span></div></div></> : <><p>校园轮廓、建筑、道路、水体与设施来自 OpenStreetMap 的实际地理对象，统一换算成米制坐标。本项目不是学校官方地图。</p>{data && <div className="data-summary"><div><strong>{data.meta.stats.buildings}</strong><span>建筑轮廓，含分部</span></div><div><strong>{data.meta.stats.sourceHeights}</strong><span>具有高度标签</span></div><div><strong>{data.meta.stats.levelHeights}</strong><span>按层数估算高度</span></div></div>}<p>无楼高记录时采用统一10米示意；有楼层记录时按每层3.3米估算。可在图层设置中切换为“仅显示来源楼高”。地面暂为相对平面。体育馆屋盖另参考校方公布的26米总高；严格高度模式仍以原地图高度标签为准。</p><p>建筑已按校园实拍整理为教学、科研、宿舍、场馆等外观家族，加入窗格、线脚、阳台及屋顶细节。部分地标参考校方照片重建；未核实区域采用同类校园风格，窗格尺寸和树木位置为示意。各地点可展开外观参考。静止画面按需绘制，画质可在图层设置中切换。</p><p>地点用途根据来源标签与名称归类；缺失或过时信息仍需核对。当前记录数量不代表现实校园完整覆盖率。</p><a className="source-link" href="https://www.openstreetmap.org/copyright" target="_blank" rel="noreferrer">数据许可：ODbL 1.0 <ExternalLink size={14} /></a><a className="source-link" href="https://polyhaven.com/license" target="_blank" rel="noreferrer">部分共享贴图：Poly Haven · CC0 <ExternalLink size={14} /></a></>}
    </div></div>
  );
}
