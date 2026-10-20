/* 观察台共用图组件（Apache ECharts，vendor/echarts.min.js）。各页只传数据，主题色、尺寸自适应、空数据与加载中都在这里统一处理。
   每个组件返回一个节点，直接 append；节点进到可见的页面里才初始化，容器变宽变窄自动重排；面板重绘时 core.js 调 release 释放旧图。
   颜色一律经 palette.js：图里的项可带 tone: [组, 键]（如 ['queue', '进行中']、['tag', '需定']），不写颜色字面量。
   通用 opts：height 高度（像素，省略按数据算）、onClick(项) 点了哪一项、legend: [[组, 键们?], ...] 图上方的图例、empty 空数据时的话、key 同一张图的稳定名（只第一次画时有动画）。

   Dash.charts.swimlane({ columns:[{id,label}], lanes:[{id,label,sub?,side?,head?}], cells:[{lane,col,status,at?,note?}] }, opts)
       泳道矩阵：一行一条道（head 为真是分组标题行，如"一批推送"），一列一个阶段；格子按状态着色、写时刻。opts.selected 高亮一道，opts.labelWidth 左侧标签宽。
   Dash.charts.dag({ nodes:[{id,label,tone,side?,title?,mark?,markNote?}], edges:[{from,to,strong?,critical?}], critical? }, opts)
       有向依赖图：有边的行按前置分层从左到右排，节点里写行号，按 tone 着色、按负责方分形状（Claude 胶囊、Codex 方块、其他六边形）；
       mark 加红色虚线外圈（opts.markText 是它的图例名），opts.selected 加主色外圈；critical 边用主色加粗（spec.critical 为真时出图例），strong 边用正文色加粗。
       没有前后置关系的行不进图，在图下排成一行行签条，同样可点。
   Dash.charts.layered({ columns:[{kind,label}], nodes:[{id,label,kind,group?,title?}], edges:[{from,to,kind,title?}] }, opts)
       分层关系图：按 columns 从左到右分栏，栏内按 group 分段，栏宽按本栏最长的标签分配（截断的标签悬停看全文）；节点色取 node 组、边色取 edge 组；
       图上方可按边种开关；点节点高亮上下游，再点或点空白取消。opts.fold 是可按段折叠的节点种类（节点多于 opts.foldOver 默认折起，点段名展开），
       opts.through 是高亮时继续往下走的节点种类（默认全部），opts.state 是调用方保存的交互状态（选中、折叠、关掉的边种，重画后保留）；
       返回的节点带 pick(id)，从图外选中一个节点。
   Dash.charts.heatmap({ x:[名], y:[名], cells:[{x,y,value?,tone?,title?}], mode:'count'|'tone' }, opts)
       热力矩阵：count 按数值深浅（单色渐变），tone 每格按语义色（检查矩阵、历史条带）。x、y 是下标。
   Dash.charts.treemap({ nodes:[{name,value,tone?,children?,title?}] }, opts)
       矩形树图：面积为 value，顶层按顺序取 layer 组的蓝（或 tone）；opts.unit 数值单位。点格子下钻。
   Dash.charts.timeline({ lanes:[{id,label}], items:[{lane,at,end?,tone,label,title?}], from?, to? }, opts)
       时间线：横轴时刻，一道一行；有 end 画成一段，否则画成点；竖线标出现在。
   Dash.charts.bars({ categories:[名], series:[{name,tone,values:[数]}], stack?, horizontal? }, opts)
       条形图（可堆叠）：近 24 小时各型号的个数、各文档注入次数一类。
   Dash.charts.loading(height) 加载中占位；Dash.charts.release(root) 释放 root 里的图。 */
(function () {
  'use strict';

  var Dash = window.Dash;
  var h = Dash.h;
  var live = [];          // { el, inst, ro }
  var drawnKeys = {};     // 画过的图：之后重画不再放动画

  function css(v) { return Dash.cssVar(v); }
  function tone(t) { return t ? Dash.color(t[0], t[1]) : css('--muted'); }
  function esc(s) { return Dash.esc(s); }
  function dark() { return matchMedia('(prefers-color-scheme: dark)').matches; }

  /* 十六进制色与底色按比例混合，给浅底、描边用 */
  function rgb(hex) {
    var s = String(hex).replace('#', '');
    if (s.length === 3) s = s.split('').map(function (c) { return c + c; }).join('');
    var n = parseInt(s, 16);
    return [(n >> 16) & 255, (n >> 8) & 255, n & 255];
  }
  function mix(fg, bg, t) {
    var a = rgb(fg), b = rgb(bg);
    return '#' + [0, 1, 2].map(function (i) { return ('0' + Math.round(a[i] * t + b[i] * (1 - t)).toString(16)).slice(-2); }).join('');
  }
  function tint(c, t) { return mix(c, css('--surface'), t); }

  function base() {
    var font = css('--font');
    return {
      animation: true,
      animationDuration: 400,
      textStyle: { fontFamily: font, color: css('--text-2'), fontSize: 12 },
      tooltip: {
        confine: true, borderWidth: 1, borderColor: css('--line'), backgroundColor: css('--surface'),
        padding: [8, 10], textStyle: { color: css('--text'), fontSize: 12, fontFamily: font },
        extraCssText: 'box-shadow: 0 8px 24px -10px rgba(20,38,64,.35); border-radius: 8px; max-width: 360px; white-space: normal;'
      }
    };
  }

  function tip(title, lines) {
    return '<div class="ct-title">' + esc(title) + '</div>' + (lines || []).filter(Boolean).map(function (l) { return '<div class="ct-line">' + esc(l) + '</div>'; }).join('');
  }

  /* ---------- 宿主：等进到可见页面、有了宽度才初始化 ---------- */

  function host(build, opts, bind) {
    opts = opts || {};
    var el = h('div.chart', { style: 'height:' + Math.round(opts.height || 300) + 'px', role: 'img', 'aria-label': opts.label || '' });
    var rec = { el: el, inst: null, ro: null };
    function draw(first) {
      var size = { width: el.clientWidth, height: el.clientHeight };
      var opt = Object.assign(base(), build(size));
      var key = opts.key || (el.closest('[data-panel]') ? el.closest('[data-panel]').dataset.panel + ':' + (opts.kind || '') : '');
      if (!first || (key && drawnKeys[key])) opt.animation = false;
      if (key) drawnKeys[key] = true;
      rec.inst.setOption(opt, true);
    }
    rec.ro = new ResizeObserver(function () {
      if (!el.clientWidth) return;
      if (!rec.inst) {
        if (!window.echarts) { el.replaceChildren(Dash.empty('图表库没有加载', 'vendor/echarts.min.js 读不到', true)); return; }
        rec.inst = window.echarts.init(el, null, { renderer: 'canvas' });
        draw(true);
        if (bind) bind(rec.inst, el, draw);
      } else {
        rec.inst.resize();
        if (opts.relayout) draw(false);
      }
    });
    rec.ro.observe(el);
    live.push(rec);
    return el;
  }

  function wrap(chart, opts) {
    var legends = (opts && opts.legend || []).map(function (l) { return Dash.legend(l[0], l[1]); });
    if (!legends.length && !(opts && opts.top)) return chart;
    return h('div.chart-wrap', opts.top || null, legends.length ? h('div.chart-legends', legends) : null, chart);
  }

  function emptyNode(opts, defaultText) {
    return Dash.empty(opts && opts.empty || defaultText || '没有数据', '', true);
  }

  var C = Dash.charts = {};

  C.release = function (root) {
    live = live.filter(function (r) {
      if (!root || root.contains(r.el)) {
        r.ro.disconnect();
        if (r.inst) r.inst.dispose();
        return false;
      }
      return true;
    });
  };

  C.loading = function (height) { return h('div.skeleton', { style: 'height:' + (height || 160) + 'px' }); };

  /* 主题切换：所有面板重画（图随之按新色重建） */
  matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function () { Dash.redrawAll(); });

  function clickTo(inst, opts, pick) {
    if (!opts.onClick) return;
    inst.on('click', function (p) { var item = pick(p); if (item) opts.onClick(item, p); });
  }

  /* ================= 泳道矩阵 ================= */

  C.swimlane = function (spec, opts) {
    opts = Object.assign({ kind: 'swimlane' }, opts);
    if (!spec || !spec.lanes || !spec.lanes.length) return emptyNode(opts, '没有在途的行');
    var cols = spec.columns, lanes = spec.lanes;
    var rowH = opts.compact ? 24 : 32, top = 30;
    var labelW = opts.labelWidth || 220;
    opts.height = opts.height || lanes.length * rowH + top + 6;
    var ci = {}, li = {};
    cols.forEach(function (c, i) { ci[c.id] = i; });
    lanes.forEach(function (l, i) { li[l.id] = i; });
    var cells = (spec.cells || []).filter(function (c) { return c.lane in li && c.col in ci; });
    var bands = lanes.map(function (l, i) { return { i: i, lane: l }; }).filter(function (b) { return b.lane.head || b.lane.id === opts.selected; });

    function build() {
      var S = { done: css('--st-done'), doing: css('--st-doing'), fail: css('--st-fail'), todo: css('--st-todo'), na: css('--st-na') };
      var on = css('--on-strong'), text3 = css('--text-3'), surface2 = css('--surface-2');
      function look(st) {
        switch (st) {
          case 'done': return { fill: tint(S.done, dark() ? .32 : .22), stroke: null, color: dark() ? S.done : mix(S.done, '#000000', .78), bold: false };
          case 'doing': return { fill: S.doing, stroke: null, color: on, bold: true };
          case 'fail': return { fill: S.fail, stroke: null, color: on, bold: true };
          case 'na': return { fill: 'transparent', stroke: null, color: text3, bold: false };
          default: return { fill: surface2, stroke: S.todo, color: text3, bold: false, dash: [3, 3] };
        }
      }
      var sideRich = {};
      ['claude', 'codex', 'author'].forEach(function (k) { sideRich[k] = { color: Dash.color('side', k), fontSize: 10, padding: [0, 4, 0, 0] }; });
      return {
        grid: { left: labelW, right: 6, top: top, bottom: 2 },
        tooltip: {
          trigger: 'item',
          formatter: function (p) {
            if (p.seriesId !== 'cells') return '';
            var c = cells[p.dataIndex], l = lanes[li[c.lane]], col = cols[ci[c.col]];
            return tip(l.label + ' · ' + col.label, [Dash.label('status', c.status), c.at ? '时刻 ' + Dash.at(c.at) : '', c.note]);
          }
        },
        xAxis: { type: 'category', position: 'top', data: cols.map(function (c) { return c.label; }), axisTick: { show: false }, axisLine: { show: false },
          axisLabel: { color: css('--text-2'), fontSize: 12, fontWeight: 600, interval: 0 }, splitLine: { show: false } },
        yAxis: { type: 'category', inverse: true, data: lanes.map(function (l) { return l.id; }), axisTick: { show: false }, axisLine: { show: false },
          triggerEvent: true,
          axisLabel: {
            interval: 0, align: 'left', margin: labelW - 4, width: labelW - 12, overflow: 'truncate',
            formatter: function (v, i) {
              var l = lanes[i];
              if (l.head) return '';   // 分组标题画在横带上（见 bands）
              return (l.side && sideRich[l.side] ? '{' + l.side + '|●}' : '') + '{id|' + l.id + '}' + (l.sub ? '  {t|' + l.sub + '}' : '');
            },
            rich: Object.assign({
              h: { color: css('--text'), fontWeight: 700, fontSize: 12 },
              id: { color: css('--text'), fontWeight: 600, fontFamily: css('--mono'), fontSize: 12 },
              t: { color: css('--text-3'), fontSize: 12 }
            }, sideRich)
          } },
        series: [
          { id: 'bands', type: 'custom', silent: true, z: -1, data: bands.map(function (b) { return [0, b.i]; }),
            renderItem: function (params, api) {
              var b = bands[params.dataIndex], y = api.coord([0, b.i])[1], hh = api.size([0, 1])[1];
              var cs = params.coordSys, x0 = Math.max(0, cs.x - labelW + 2);
              var band = { type: 'rect', shape: { x: x0, y: y - hh / 2 + 1, width: cs.x + cs.width - x0, height: hh - 2, r: 6 },
                style: { fill: b.lane.head ? css('--surface-2') : tint(css('--accent'), .12) } };
              if (!b.lane.head) return band;
              return { type: 'group', children: [band, { type: 'text', style: { text: b.lane.label, x: x0 + 8, y: y, verticalAlign: 'middle',
                fill: css('--text'), font: '650 12px ' + css('--font') } }] };
            } },
          { id: 'cells', type: 'custom', z: 2, data: cells.map(function (c) { return [ci[c.col], li[c.lane]]; }),
            renderItem: function (params, api) {
              var c = cells[params.dataIndex], p = api.coord([api.value(0), api.value(1)]), sz = api.size([1, 1]);
              var w = sz[0] - 6, hh = sz[1] - 6, lk = look(c.status);
              var label = c.status === 'na' ? '—' : c.at ? Dash.at(c.at).replace(/^\d+-\d+ /, '') : (c.status === 'todo' ? '' : Dash.label('status', c.status));
              if (c.status === 'fail') label = '失败' + (c.at ? ' ' + label : '');
              return { type: 'group', children: [
                { type: 'rect', shape: { x: p[0] - w / 2, y: p[1] - hh / 2, width: w, height: hh, r: 5 },
                  style: { fill: lk.fill, stroke: lk.stroke, lineWidth: lk.stroke ? 1 : 0, lineDash: lk.dash || null } },
                { type: 'text', style: { text: w > 34 ? label : '', x: p[0], y: p[1], align: 'center', verticalAlign: 'middle',
                  fill: lk.color, font: (lk.bold ? '600 ' : '') + '12px ' + css('--mono') } }
              ] };
            } }
        ]
      };
    }
    return wrap(host(build, opts, function (inst) {
      clickTo(inst, opts, function (p) {
        if (p.componentType === 'yAxis') { var l = lanes.filter(function (x) { return x.id === p.value; })[0]; return l && !l.head ? { lane: l } : null; }
        if (p.seriesId === 'cells') { var c = cells[p.dataIndex]; return { lane: lanes[li[c.lane]], cell: c }; }
        return null;
      });
    }), opts);
  };

  /* ================= 有向依赖图 ================= */

  function rankNodes(nodes, edges) {
    var preds = {}, rank = {};
    nodes.forEach(function (n) { preds[n.id] = []; });
    edges.forEach(function (e) { if (preds[e.to] && preds[e.from]) preds[e.to].push(e.from); });
    function r(id, seen) {
      if (id in rank) return rank[id];
      if (seen[id]) return 0;   // 前置成环时就地截断，按 0 层排
      seen[id] = 1;
      var v = preds[id].length ? 1 + Math.max.apply(null, preds[id].map(function (p) { return r(p, seen); })) : 0;
      return (rank[id] = v);
    }
    nodes.forEach(function (n) { r(n.id, {}); });
    return { rank: rank, preds: preds };
  }

  /* 相对亮度（WCAG），给实色块挑对比更大的字色 */
  function lum(hex) {
    return rgb(hex).map(function (v) { v /= 255; return v <= .03928 ? v / 12.92 : Math.pow((v + .055) / 1.055, 2.4); })
      .reduce(function (a, v, i) { return a + v * [.2126, .7152, .0722][i]; }, 0);
  }
  function contrast(a, b) { var x = lum(a), y = lum(b); return (Math.max(x, y) + .05) / (Math.min(x, y) + .05); }
  function inkOn(fill) { var a = css('--text'), b = css('--on-strong'); return contrast(fill, a) >= contrast(fill, b) ? a : b; }

  var NODE_W = 66, NODE_H = 28;
  var SHAPE_NAME = { claude: 'Claude 胶囊', codex: 'Codex 方块', other: '其他 六边形' };
  function shapeOf(side) { return side === 'claude' || side === 'codex' ? side : 'other'; }

  function nodeShape(shape, x, y, w, hh) {
    if (shape === 'other') {
      var k = hh / 2;
      return { type: 'polygon', shape: { points: [[x - w / 2, y], [x - w / 2 + k, y - hh / 2], [x + w / 2 - k, y - hh / 2], [x + w / 2, y], [x + w / 2 - k, y + hh / 2], [x - w / 2 + k, y + hh / 2]] } };
    }
    return { type: 'rect', shape: { x: x - w / 2, y: y - hh / 2, width: w, height: hh, r: shape === 'claude' ? hh / 2 : 4 } };
  }

  C.dag = function (spec, opts) {
    opts = Object.assign({ kind: 'dag', relayout: true }, opts);
    if (!spec || !spec.nodes || !spec.nodes.length) return emptyNode(opts, '没有队列行');
    var nodes = spec.nodes, ids = {};
    nodes.forEach(function (n) { ids[n.id] = n; });
    var edges = (spec.edges || []).filter(function (e) { return ids[e.from] && ids[e.to] && e.from !== e.to; });
    var linked = {};
    edges.forEach(function (e) { linked[e.from] = linked[e.to] = 1; });
    var graphNodes = nodes.filter(function (n) { return linked[n.id]; });
    var loose = nodes.filter(function (n) { return !linked[n.id]; });

    /* 有边的行：按前置分层从左到右，层内按前置的平均位置排，减少交叉 */
    var rk = rankNodes(graphNodes, edges), layers = [], slot = {};
    graphNodes.forEach(function (n) { (layers[rk.rank[n.id]] = layers[rk.rank[n.id]] || []).push(n); });
    layers = layers.filter(Boolean);
    layers.forEach(function (layer, li) {
      var bc = {};
      layer.forEach(function (n) {
        var ps = rk.preds[n.id].map(function (p) { return slot[p] ? slot[p].y : 0; });
        bc[n.id] = ps.length ? ps.reduce(function (a, b) { return a + b; }, 0) / ps.length : 0;
      });
      layer.sort(function (a, b) { return bc[a.id] - bc[b.id] || (a.id < b.id ? -1 : 1); });
      layer.forEach(function (n, i) { slot[n.id] = { x: li, y: i }; });
    });
    var tall = layers.reduce(function (m, l) { return Math.max(m, l.length); }, 0);
    var rowH = 44, top = 26;
    opts.height = opts.height || tall * rowH + top + 10;

    function shapeLegend() {
      var used = {};
      nodes.forEach(function (n) { used[shapeOf(n.side)] = 1; });
      return h('div.legend', ['claude', 'codex', 'other'].filter(function (k) { return used[k]; }).map(function (k) {
        return h('span.legend-item', h('i.dag-shape.is-' + k), SHAPE_NAME[k]);
      }).concat(spec.critical ? [h('span.legend-item', h('i.dag-line.is-critical'), '关键路径')] : [],
        nodes.some(function (n) { return n.mark; }) ? [h('span.legend-item', h('i.dag-shape.is-ring'), opts.markText || '要留意')] : []));
    }

    function looseStrip() {
      if (!loose.length) return null;
      return h('div.dag-loose',
        h('div.dag-loose-title', (graphNodes.length ? '没有前后置关系的 ' : '队列行之间没有前置关系，共 ') + Dash.num(loose.length) + ' 行'),
        h('div.dag-loose-list', loose.map(function (n) {
          var fill = tone(n.tone);
          var b = h('button.dag-chip.is-' + shapeOf(n.side) + (n.id === opts.selected ? '.is-selected' : '') + (n.mark ? '.is-mark' : ''),
            { type: 'button', title: [n.id, n.title, n.tone ? Dash.label(n.tone[0], n.tone[1]) : '', n.side ? '负责方 ' + Dash.label('side', n.side) : ''].filter(Boolean).join('\n'),
              style: 'background:' + fill + ';color:' + inkOn(fill) }, n.id);
          if (opts.onClick) b.addEventListener('click', function () { opts.onClick(n); });
          return b;
        })));
    }

    if (!graphNodes.length) return h('div.chart-wrap', opts.legend ? h('div.chart-legends', opts.legend.map(function (l) { return Dash.legend(l[0], l[1]); }), shapeLegend()) : shapeLegend(), looseStrip());

    var placed = {};
    function build(size) {
      var pad = 12, usable = Math.max(160, size.width - pad * 2);
      var colW = layers.length > 1 ? Math.max(NODE_W + 18, Math.min(240, (usable - NODE_W) / (layers.length - 1))) : 0;
      var span = colW * (layers.length - 1) + NODE_W;
      var x0 = pad + Math.max(0, (usable - span) / 2) + NODE_W / 2;
      graphNodes.forEach(function (n) {
        var s = slot[n.id], col = layers[s.x].length;
        placed[n.id] = { x: x0 + s.x * colW, y: top + (s.y + (tall - col) / 2) * rowH + rowH / 2 };
      });
      var text = css('--text'), text3 = css('--text-3'), accent = css('--accent'), risk = css('--risk');
      var order = edges.slice().sort(function (a, b) { return (a.strong || a.critical ? 1 : 0) - (b.strong || b.critical ? 1 : 0); });
      var heads = layers.length > 1 ? layers.map(function (l, i) {
        return { type: 'text', left: x0 + i * colW - NODE_W / 2, top: 4,
          style: { text: i === 0 ? '先做' : '第 ' + (i + 1) + ' 步', fill: text3, font: '600 11px ' + css('--font') } };
      }) : [];
      return {
        graphic: heads,
        tooltip: { trigger: 'item', formatter: function (p) {
          if (p.seriesId === 'edges') { var e = order[p.dataIndex]; return tip(e.from + ' → ' + e.to, [e.critical ? '关键路径上的前置' : '前置']); }
          var n = graphNodes[p.dataIndex];
          return tip(n.id + (n.label && n.label !== n.id ? '  ' + n.label : ''),
            [n.tone ? Dash.label(n.tone[0], n.tone[1]) : '', n.side ? '负责方 ' + Dash.label('side', n.side) : '', n.markNote || '', n.title && n.title !== n.label ? n.title : '']);
        } },
        series: [
          { id: 'edges', type: 'custom', coordinateSystem: 'none', z: 1, data: order.map(function (e, i) { return i; }),
            renderItem: function (params) {
              var e = order[params.dataIndex], a = placed[e.from], b = placed[e.to];
              var sx = a.x + NODE_W / 2, sy = a.y, tx = b.x - NODE_W / 2 - 2, ty = b.y;
              var c = e.strong ? text : e.critical ? accent : text3, w = e.strong || e.critical ? 2.6 : 1.6;
              var dx = Math.max(24, (tx - sx) / 2);
              return { type: 'group', children: [
                { type: 'bezierCurve', shape: { x1: sx, y1: sy, x2: tx - 7, y2: ty, cpx1: sx + dx, cpy1: sy, cpx2: tx - dx, cpy2: ty },
                  style: { stroke: c, lineWidth: w, fill: 'none', opacity: e.strong || e.critical ? 1 : .85 } },
                { type: 'polygon', shape: { points: [[tx, ty], [tx - 9, ty - 5], [tx - 9, ty + 5]] }, style: { fill: c } }
              ] };
            } },
          { id: 'nodes', type: 'custom', coordinateSystem: 'none', z: 2, data: graphNodes.map(function (n, i) { return i; }),
            renderItem: function (params) {
              var n = graphNodes[params.dataIndex], p = placed[n.id], fill = tone(n.tone), shape = shapeOf(n.side);
              var kids = [];
              if (n.id === opts.selected || n.mark) {
                var ring = nodeShape(shape, p.x, p.y, NODE_W + 8, NODE_H + 8);
                ring.style = { fill: 'none', stroke: n.id === opts.selected ? accent : risk, lineWidth: n.id === opts.selected ? 3 : 2, lineDash: n.id === opts.selected ? null : [4, 3] };
                kids.push(ring);
              }
              var body = nodeShape(shape, p.x, p.y, NODE_W, NODE_H);
              body.style = { fill: fill, stroke: css('--surface'), lineWidth: 1 };
              kids.push(body);
              kids.push({ type: 'text', style: { text: n.id, x: p.x, y: p.y, align: 'center', verticalAlign: 'middle', fill: inkOn(fill), font: '650 12px ' + css('--mono') } });
              return { type: 'group', children: kids };
            } }
        ]
      };
    }
    var chart = host(build, opts, function (inst) {
      clickTo(inst, opts, function (p) { return p.seriesId === 'nodes' ? graphNodes[p.dataIndex] : null; });
    });
    var legends = (opts.legend || []).map(function (l) { return Dash.legend(l[0], l[1]); });
    return h('div.chart-wrap', h('div.chart-legends', legends, shapeLegend()), chart, looseStrip());
  };

  /* ================= 分层关系图 ================= */

  /* 文字的大致像素宽：汉字记一个字号，其余记 0.6 个 */
  function textW(s, px) {
    var w = 0;
    String(s == null ? '' : s).split('').forEach(function (ch) { w += ch.charCodeAt(0) >= 0x2e80 ? px : px * .6; });
    return w;
  }

  C.layered = function (spec, opts) {
    opts = Object.assign({ kind: 'layered', relayout: true }, opts);
    if (!spec || !spec.nodes || !spec.nodes.length) return emptyNode(opts, '没有关系数据');
    var st = opts.state || {};
    st.off = st.off || {}; st.open = st.open || {}; st.fold = st.fold || {};
    var foldable = opts.fold || [], through = opts.through || null;
    var rowH = opts.rowHeight || 22, maxRows = opts.maxRows || 30, top = 40, maxLabel = opts.maxLabel || 150;
    var byId = {};
    spec.nodes.forEach(function (n) { byId[n.id] = n; });
    var edges = (spec.edges || []).filter(function (e) { return byId[e.from] && byId[e.to] && e.from !== e.to; });
    var kinds = Object.keys(Dash.palette.edge).filter(function (k) { return edges.some(function (e) { return e.kind === k; }); });
    var deg = {};
    edges.forEach(function (e) {
      (deg[e.from] = deg[e.from] || { i: 0, o: 0 }).o++;
      (deg[e.to] = deg[e.to] || { i: 0, o: 0 }).i++;
    });
    /* 栏内按 group 第一次出现的顺序分段，段内保持数据给的顺序 */
    var cols = spec.columns.map(function (c) {
      var groups = [], gi = {};
      spec.nodes.forEach(function (n) {
        if (n.kind !== c.kind) return;
        var g = n.group || '';
        if (!(g in gi)) { gi[g] = groups.length; groups.push({ name: g, list: [] }); }
        groups[gi[g]].list.push(n);
      });
      var count = groups.reduce(function (a, g) { return a + g.list.length; }, 0);
      var grouped = groups.some(function (g) { return g.name; });
      if (foldable.indexOf(c.kind) >= 0 && grouped && !(c.kind in st.fold)) st.fold[c.kind] = count > (opts.foldOver || 24);
      return { c: c, groups: groups, grouped: grouped, count: count };
    }).filter(function (col) { return col.count; });

    function gkey(kind, name) { return kind + '\u0001' + name; }
    function isFolded(kind, name) { return !!(st.fold[kind] && name && !st.open[gkey(kind, name)]); }
    function vid(id) { var n = byId[id]; return n && isFolded(n.kind, n.group) ? 'grp:' + gkey(n.kind, n.group) : id; }

    /* 排布：每栏一组槽位（段名、节点、折起来的段）；槽位多于 maxRows 拆成子栏；栏宽按本栏最长的标签分配，放不下时等比缩 */
    function layout(width) {
      var cs = cols.map(function (col) {
        var slots = [];
        col.groups.forEach(function (g) {
          var key = gkey(col.c.kind, g.name);
          if (isFolded(col.c.kind, g.name)) { slots.push({ fold: g, key: key, id: 'grp:' + key, text: g.name + ' · ' + g.list.length }); return; }
          if (col.grouped) slots.push({ head: g.name || '其他', key: key, canFold: !!(st.fold[col.c.kind] && g.name) });
          g.list.forEach(function (n) { slots.push({ node: n }); });
        });
        var subs = Math.max(1, Math.ceil(slots.length / maxRows)), per = Math.ceil(slots.length / subs);
        var need = slots.reduce(function (m, s) { return Math.max(m, textW(s.node ? s.node.label : s.fold ? s.text : s.head, 11)); }, 0);
        var min = textW(col.c.label + ' ' + col.count, 12) + 26;
        return { col: col, slots: slots, subs: subs, per: per, min: min, w: Math.max(min, Math.min(maxLabel, need) + 24) };
      });
      var usable = Math.max(240, width - 20);
      var total = cs.reduce(function (a, c) { return a + c.w * c.subs; }, 0);
      var units = cs.reduce(function (a, c) { return a + c.subs; }, 0);
      /* 放不下：只缩标签超出栏名的那一截，栏名不被压；放得下：多出的宽度平分给各栏 */
      var room = cs.reduce(function (a, c) { return a + (c.w - c.min) * c.subs; }, 0);
      var cut = total > usable && room ? Math.min(1, (total - usable) / room) : 0, extra = total < usable ? (usable - total) / units : 0;
      var rows = cs.reduce(function (m, c) { return Math.max(m, c.per); }, 1);
      var x = 10;
      cs.forEach(function (c) {
        c.x = x; c.sw = c.w - (c.w - c.min) * cut + extra;
        c.y0 = top + (rows - c.per) / 2 * rowH;
        x += c.sw * c.subs;
      });
      return { cs: cs, rows: rows, height: top + rows * rowH + 12 };
    }

    /* 选中节点的上下游：沿边向两头走；through 给定时，只穿过这些种类的节点继续走（其余种类到此为止） */
    function reach(id, list) {
      var hitN = {}, hitE = {};
      hitN[id] = 1;
      ['down', 'up'].forEach(function (dir) {
        var seen = {}, stack = [id];
        seen[id] = 1;
        while (stack.length) {
          var cur = stack.pop();
          list.forEach(function (e, i) {
            var a = dir === 'down' ? e.from : e.to, b = dir === 'down' ? e.to : e.from;
            if (a !== cur) return;
            hitE[i] = 1; hitN[b] = 1;
            if (seen[b]) return;
            seen[b] = 1;
            if (!through || through.indexOf(byId[b].kind) >= 0) stack.push(b);
          });
        }
      });
      return { n: hitN, e: hitE };
    }

    var first = layout(1200);
    opts.height = opts.height || first.height;
    var el;

    function build(size) {
      var L = layout(size.width);
      if (el && Math.abs(L.height - size.height) > 1) el.style.height = L.height + 'px';   // 折叠变了：撑高或收矮，ResizeObserver 接着重画
      var raw = edges.filter(function (e) { return !st.off[e.kind]; });
      var hit = st.pick && byId[st.pick] ? reach(st.pick, raw) : null;
      var focus = null;
      if (hit) { focus = {}; Object.keys(hit.n).forEach(function (id) { focus[vid(id)] = 1; }); }
      var pickV = st.pick ? vid(st.pick) : null;
      var text = css('--text'), text2 = css('--text-2'), text3 = css('--text-3');
      var data = [], graphics = [];
      L.cs.forEach(function (c) {
        var kc = Dash.color('node', c.col.c.kind);
        graphics.push({ type: 'circle', shape: { cx: c.x + 5, cy: 14, r: 4 }, style: { fill: kc }, silent: true });
        graphics.push({ type: 'text', left: c.x + 14, top: 6, silent: true,
          style: { text: c.col.c.label + ' ' + c.col.count, fill: text, font: '600 12px ' + css('--font') } });
        c.slots.forEach(function (s, i) {
          var sub = Math.floor(i / c.per), row = i % c.per;
          var x = c.x + sub * c.sw, y = c.y0 + row * rowH + rowH / 2;
          var labelW = Math.max(30, c.sw - 22);
          if (s.head) {
            graphics.push({ type: 'text', left: x, top: y - 7, cursor: s.canFold ? 'pointer' : 'default', silent: !s.canFold,
              style: { text: (s.canFold ? '▾ ' : '') + s.head, fill: text3, font: '600 11px ' + css('--font'), width: labelW + 14, overflow: 'truncate' },
              onclick: s.canFold ? function () { st.open[s.key] = false; redraw(); } : undefined });
            return;
          }
          var id = s.node ? s.node.id : s.id;
          var dim = focus && !focus[id], me = id === pickV;
          data.push({ id: id, name: s.node ? s.node.label : s.text, x: x + 6, y: y, fold: s.fold ? s.key : null,
            symbol: s.fold ? 'roundRect' : 'circle', symbolSize: s.fold ? [11, 11] : (me ? 12 : 8),
            itemStyle: { color: s.fold ? tint(kc, .35) : kc, borderColor: me ? text : (s.fold ? kc : 'transparent'), borderWidth: me ? 2 : 1.2, opacity: dim ? .18 : 1 },
            label: { show: true, width: me ? labelW + 80 : labelW, overflow: me ? 'none' : 'truncate', color: dim ? tint(text3, .45) : (me ? text : s.fold ? text : text2),
              fontWeight: me || s.fold ? 650 : 400, formatter: s.fold ? '▸ ' + s.text : undefined } });
        });
      });
      /* 两个不可见的角点把节点外框撑满画布，图就按像素原样摆放，栏名与节点对得齐 */
      data.push({ id: ' a', x: 0, y: 0, symbolSize: 0, label: { show: false }, silent: true, tooltip: { show: false } });
      data.push({ id: ' b', x: size.width, y: size.height, symbolSize: 0, label: { show: false }, silent: true, tooltip: { show: false } });
      /* 折起来的段把边并成一条，粗细随条数 */
      var agg = {}, links = [];
      raw.forEach(function (e, i) {
        var a = vid(e.from), b = vid(e.to);
        if (a === b) return;
        var k = a + '|' + b + '|' + e.kind;
        if (!agg[k]) { agg[k] = { source: a, target: b, kind: e.kind, n: 0, on: false, sample: e }; links.push(agg[k]); }
        agg[k].n++;
        if (hit && hit.e[i]) agg[k].on = true;
      });
      return {
        graphic: graphics,
        tooltip: { trigger: 'item', formatter: function (p) {
          if (p.dataType === 'edge') {
            var g = p.data.raw;
            var nameOf = function (v) { return byId[v] ? byId[v].label : v.replace(/^grp:[^\u0001]*\u0001/, '') + '（整段）'; };
            return tip(Dash.label('edge', g.kind) + (g.n > 1 ? ' · ' + Dash.num(g.n) + ' 条' : ''), [nameOf(g.source) + ' → ' + nameOf(g.target), g.n === 1 ? g.sample.title : '']);
          }
          var d = p.data;
          if (d.fold) return tip(d.name, ['折起来的一段，点一下展开']);
          var n = byId[d.id];
          if (!n) return '';
          var dg = deg[n.id] || { i: 0, o: 0 };
          return tip(n.label, [Dash.label('node', n.kind) + (n.group ? ' · ' + n.group : ''), n.title, '上游 ' + Dash.num(dg.i) + ' 条边 · 下游 ' + Dash.num(dg.o) + ' 条边']);
        } },
        series: [{
          type: 'graph', layout: 'none', coordinateSystem: null, roam: false,
          left: 0, top: 0, width: size.width, height: size.height,
          edgeSymbol: ['none', 'arrow'], edgeSymbolSize: [0, 5],
          label: { show: true, position: 'right', distance: 5, fontSize: 11 },
          data: data,
          links: links.map(function (g) {
            var w = Math.min(3.2, 1 + Math.log(g.n) / Math.LN2 * .5);
            return { source: g.source, target: g.target, raw: g,
              lineStyle: { color: Dash.color('edge', g.kind), width: g.on ? w + .6 : w, opacity: hit ? (g.on ? .9 : .04) : .3, curveness: .16 } };
          })
        }]
      };
    }

    var redraw = function () {};
    var filters = kinds.length > 1 ? kinds.map(function (k) {
      var b = h('button.filter-chip', { type: 'button', 'aria-pressed': String(!st.off[k]), style: '--tone: var(' + Dash.colorVar('edge', k) + ')' }, h('i.legend-swatch'), Dash.label('edge', k));
      b.addEventListener('click', function () {
        st.off[k] = !st.off[k];
        b.setAttribute('aria-pressed', String(!st.off[k]));
        redraw();
      });
      return b;
    }) : [];
    var folds = cols.filter(function (col) { return foldable.indexOf(col.c.kind) >= 0 && col.grouped; }).map(function (col) {
      var k = col.c.kind;
      var b = h('button.btn', { type: 'button' });
      function sync() { b.textContent = Dash.label('node', k) + (st.fold[k] ? '：全部展开' : '：按段折叠'); }
      sync();
      b.addEventListener('click', function () {
        st.fold[k] = !st.fold[k];
        col.groups.forEach(function (g) { delete st.open[gkey(k, g.name)]; });
        sync();
        redraw();
      });
      return b;
    });
    el = host(build, opts, function (inst, node, draw) {
      redraw = function () { draw(false); };
      inst.on('click', function (p) {
        if (p.dataType !== 'node') return;
        if (p.data.fold) { st.open[p.data.fold] = true; redraw(); return; }
        if (!byId[p.data.id]) return;
        st.pick = st.pick === p.data.id ? null : p.data.id;
        redraw();
        if (opts.onClick) opts.onClick(st.pick ? byId[p.data.id] : null, p);
      });
      inst.getZr().on('click', function (e) {
        if (e.target || !st.pick) return;
        st.pick = null;
        redraw();
        if (opts.onClick) opts.onClick(null);
      });
    });
    /* 栏名都放不下时不再压：图保持最小宽度，由外层容器横向滚动 */
    el.style.minWidth = Math.round(first.cs.reduce(function (a, c) { return a + c.min * c.subs; }, 20)) + 'px';
    var top_ = filters.length || folds.length ? h('div.chart-filters', filters, folds.length ? h('span.grow') : null, folds) : null;
    var out = wrap(el, Object.assign({}, opts, { top: top_, legend: opts.legend || [] }));
    /* 从图外选中一个节点（例如侧栏里点了上下游）：所在的段折着就先展开 */
    out.pick = function (id) {
      var n = byId[id];
      if (!n) return;
      if (isFolded(n.kind, n.group)) st.open[gkey(n.kind, n.group)] = true;
      st.pick = id;
      redraw();
      if (opts.onClick) opts.onClick(n);
    };
    return out;
  };

  /* ================= 热力矩阵 ================= */

  C.heatmap = function (spec, opts) {
    opts = Object.assign({ kind: 'heatmap' }, opts);
    if (!spec || !spec.x || !spec.x.length || !spec.y || !spec.y.length) return emptyNode(opts);
    var mode = spec.mode || 'count';
    var cellH = opts.cellHeight || 24, labelW = opts.labelWidth || 120;
    var bottom = opts.xLabels === false ? 8 : (spec.x.length > 10 ? 64 : 28);
    opts.height = opts.height || spec.y.length * cellH + bottom + 10;
    var cells = spec.cells || [];
    var max = cells.reduce(function (m, c) { return Math.max(m, c.value || 0); }, 0) || 1;

    function build(size) {
      var cw = (size.width - labelW - 10) / spec.x.length;
      var s0 = Dash.color('scale', 0), s1 = Dash.color('scale', 1);
      return {
        grid: { left: labelW, right: 10, top: 6, bottom: bottom },
        tooltip: { trigger: 'item', formatter: function (p) {
          var c = cells[p.dataIndex];
          return tip(spec.y[c.y] + ' × ' + spec.x[c.x], [mode === 'count' ? Dash.num(c.value) + (opts.unit ? ' ' + opts.unit : '') : (c.tone ? Dash.label(c.tone[0], c.tone[1]) : ''), c.title]);
        } },
        xAxis: { type: 'category', data: spec.x, splitArea: { show: false }, axisTick: { show: false }, axisLine: { show: false },
          axisLabel: { show: opts.xLabels !== false, interval: opts.xInterval == null ? 0 : opts.xInterval, rotate: spec.x.length > 10 ? 45 : 0, color: css('--text-2'), fontSize: 11 } },
        yAxis: { type: 'category', data: spec.y, inverse: true, axisTick: { show: false }, axisLine: { show: false },
          axisLabel: { color: css('--text-2'), fontSize: 11, width: labelW - 10, overflow: 'truncate' } },
        series: [{
          type: 'heatmap',
          data: cells.map(function (c) {
            var color = mode === 'tone' ? tone(c.tone) : (c.value ? mix(s1, s0, .15 + .85 * c.value / max) : css('--surface-2'));
            return { value: [c.x, c.y, c.value || 0], itemStyle: { color: color, borderColor: css('--surface'), borderWidth: mode === 'tone' && cw < 10 ? 1 : 2, borderRadius: 3 } };
          }),
          label: { show: mode === 'count' && cw >= 26, color: css('--text'), fontSize: 11,
            formatter: function (p) { return p.value[2] ? Dash.num(p.value[2]) : ''; } },
          emphasis: { itemStyle: { borderColor: css('--text'), borderWidth: 2 } }
        }]
      };
    }
    return wrap(host(build, Object.assign({ relayout: true }, opts), function (inst) {
      clickTo(inst, opts, function (p) { return cells[p.dataIndex]; });
    }), opts);
  };

  /* ================= 矩形树图 ================= */

  C.treemap = function (spec, opts) {
    opts = Object.assign({ kind: 'treemap' }, opts);
    if (!spec || !spec.nodes || !spec.nodes.length) return emptyNode(opts);
    opts.height = opts.height || 420;
    function conv(n, color) {
      var kids = (n.children || []).filter(function (k) { return k.value > 0; });
      return { name: n.name, value: n.value, raw: n, itemStyle: color ? { color: color } : undefined,
        children: kids.length ? kids.map(function (k) { return conv(k, null); }) : undefined };
    }
    function build() {
      return {
        tooltip: { formatter: function (p) {
          var path = (p.treePathInfo || []).slice(1).map(function (x) { return x.name; }).join(' / ');
          return tip(path || p.name, [Dash.num(p.value) + (opts.unit ? ' ' + opts.unit : ''), p.data.raw && p.data.raw.title]);
        } },
        series: [{
          type: 'treemap', roam: false, nodeClick: 'zoomToNode', leafDepth: opts.depth || 2,
          top: 8, left: 2, right: 2, bottom: 28,
          breadcrumb: { show: true, bottom: 2, height: 20, itemStyle: { color: css('--surface-2'), borderColor: css('--line'), textStyle: { color: css('--text-2') } } },
          upperLabel: { show: true, height: 22, color: css('--on-strong'), fontWeight: 600, fontSize: 12 },
          label: { show: true, fontSize: 11, color: css('--on-strong'), overflow: 'truncate',
            formatter: function (p) { return p.name + '\n' + Dash.num(p.value); } },
          itemStyle: { borderColor: css('--surface'), borderWidth: 1, gapWidth: 1 },
          levels: [
            { itemStyle: { borderWidth: 0, gapWidth: 3 } },
            { itemStyle: { gapWidth: 1, borderColorSaturation: .5 } },
            { itemStyle: { gapWidth: 1 } }
          ],
          data: spec.nodes.map(function (n, i) { return conv(n, n.tone ? tone(n.tone) : Dash.color('layer', i)); })
        }]
      };
    }
    return wrap(host(build, opts, function (inst) {
      clickTo(inst, opts, function (p) { return p.data && p.data.raw; });
    }), opts);
  };

  /* ================= 时间线 ================= */

  C.timeline = function (spec, opts) {
    opts = Object.assign({ kind: 'timeline' }, opts);
    if (!spec || !spec.items || !spec.items.length) return emptyNode(opts, '这段时间里没有记录');
    var lanes = spec.lanes && spec.lanes.length ? spec.lanes : [{ id: '', label: '' }];
    var li = {};
    lanes.forEach(function (l, i) { li[l.id] = i; });
    var items = spec.items.filter(function (it) { return it.lane in li && !isNaN(Dash.parseTime(it.at)); });
    var rowH = opts.rowHeight || 40, labelW = opts.labelWidth || (lanes.length > 1 || lanes[0].label ? 120 : 16);
    opts.height = opts.height || lanes.length * rowH + 48;
    var pts = [], spans = [];
    items.forEach(function (it, i) { (it.end ? spans : pts).push(i); });
    var times = items.map(function (it) { return Dash.parseTime(it.at); }).concat(items.filter(function (it) { return it.end; }).map(function (it) { return Dash.parseTime(it.end); }));
    var from = spec.from ? Dash.parseTime(spec.from) : Math.min.apply(null, times);
    var to = spec.to ? Dash.parseTime(spec.to) : Math.max(Date.now(), Math.max.apply(null, times));
    var pad = Math.max(60000, (to - from) * .03);

    function build() {
      return {
        grid: { left: labelW, right: 18, top: 8, bottom: 26 },
        tooltip: { trigger: 'item', formatter: function (p) {
          var it = items[p.data.i];
          return tip(it.label, [Dash.at(it.at) + (it.end ? ' – ' + Dash.at(it.end) : ''), it.tone ? Dash.label(it.tone[0], it.tone[1]) : '', it.title]);
        } },
        xAxis: { type: 'time', min: from - pad, max: to + pad, axisLine: { lineStyle: { color: css('--line') } }, axisTick: { show: false },
          splitLine: { show: true, lineStyle: { color: css('--line-2') } },
          axisLabel: { color: css('--text-3'), fontSize: 11, hideOverlap: true, formatter: function (v) { return Dash.at(v); } } },
        yAxis: { type: 'category', inverse: true, data: lanes.map(function (l) { return l.label; }), axisTick: { show: false }, axisLine: { show: false },
          splitLine: { show: true, lineStyle: { color: css('--line-2') } }, axisLabel: { color: css('--text-2'), fontSize: 12, fontWeight: 600 } },
        dataZoom: opts.zoom === false ? [] : [{ type: 'inside', xAxisIndex: 0, filterMode: 'none' }],
        series: [
          { id: 'spans', type: 'custom', z: 2, data: spans.map(function (i) { var it = items[i]; return { value: [Dash.parseTime(it.at), li[it.lane], Dash.parseTime(it.end)], i: i }; }),
            encode: { x: [0, 2], y: 1 },
            renderItem: function (params, api) {
              var a = api.coord([api.value(0), api.value(1)]), b = api.coord([api.value(2), api.value(1)]);
              var it = items[spans[params.dataIndex]], hh = Math.min(14, api.size([0, 1])[1] * .4);
              return { type: 'rect', shape: { x: a[0], y: a[1] - hh / 2, width: Math.max(3, b[0] - a[0]), height: hh, r: 3 },
                style: { fill: tone(it.tone), opacity: .85 } };
            } },
          { id: 'points', type: 'scatter', z: 3, symbolSize: opts.symbolSize || 11,
            data: pts.map(function (i) {
              var it = items[i];
              return { value: [Dash.parseTime(it.at), li[it.lane]], i: i,
                itemStyle: { color: tone(it.tone), borderColor: it.ring ? css('--text') : css('--surface'), borderWidth: it.ring ? 2 : 1.5 } };
            }),
            markLine: { silent: true, symbol: 'none', label: { show: true, formatter: '现在', color: css('--text-3'), fontSize: 10 },
              lineStyle: { color: css('--accent'), type: 'dashed', width: 1 }, data: [{ xAxis: Date.now() }] } }
        ]
      };
    }
    return wrap(host(build, opts, function (inst) {
      clickTo(inst, opts, function (p) { return p.data && p.data.i != null ? items[p.data.i] : null; });
    }), opts);
  };

  /* ================= 条形图 ================= */

  C.bars = function (spec, opts) {
    opts = Object.assign({ kind: 'bars' }, opts);
    if (!spec || !spec.categories || !spec.categories.length || !spec.series || !spec.series.length) return emptyNode(opts);
    var horiz = !!spec.horizontal;
    opts.height = opts.height || (horiz ? spec.categories.length * 26 + 40 : 260);
    function build() {
      var cat = { type: 'category', data: spec.categories, axisTick: { show: false }, axisLine: { lineStyle: { color: css('--line') } },
        axisLabel: { color: css('--text-2'), fontSize: 11, interval: 0, rotate: !horiz && spec.categories.length > 12 ? 40 : 0 } };
      var val = { type: 'value', minInterval: 1, splitLine: { lineStyle: { color: css('--line-2') } }, axisLabel: { color: css('--text-3'), fontSize: 11 } };
      if (horiz) cat.inverse = true;
      return {
        grid: { left: horiz ? (opts.labelWidth || 140) : 40, right: 16, top: 12, bottom: horiz ? 24 : (spec.categories.length > 12 ? 60 : 30) },
        tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' }, formatter: function (ps) {
          return tip(ps[0].name, ps.filter(function (p) { return p.value; }).map(function (p) { return p.seriesName + '：' + Dash.num(p.value) + (opts.unit ? ' ' + opts.unit : ''); }));
        } },
        xAxis: horiz ? val : cat,
        yAxis: horiz ? cat : val,
        series: spec.series.map(function (s) {
          return { name: s.name, type: 'bar', stack: spec.stack ? 'all' : undefined, data: s.values, barMaxWidth: 26,
            itemStyle: { color: tone(s.tone), borderRadius: spec.stack ? 0 : 3 } };
        })
      };
    }
    return wrap(host(build, opts, function (inst) {
      clickTo(inst, opts, function (p) { return { category: p.name, series: p.seriesName, value: p.value }; });
    }), opts);
  };
})();
