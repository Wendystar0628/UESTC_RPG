/* 观察台图组件·流程类：泳道矩阵 swimlane、依赖图 dag、时间线 timeline。共用底座在 charts.js（Dash.internal.chartKit）。
   swimlane({ columns:[{id,label}], lanes:[{id,label,sub?,side?,head?}], cells:[{lane,col,status,at?,note?}] }, opts)
       泳道矩阵：一行一条道（head 为真是分组标题行，如"一批推送"），一列一个阶段；格子按状态着色、写时刻。opts.selected 高亮一道，opts.labelWidth 左侧标签宽。
   dag({ nodes:[{id,label,tone,side?,title?,mark?,markNote?}], edges:[{from,to,strong?,critical?}], critical? }, opts)
       有向依赖图：有边的行按前置分层从左到右排，节点里写行号，按 tone 着色、按负责方分形状（Claude 胶囊、Codex 方块、其他六边形）；
       mark 加红色虚线外圈（opts.markText 是它的图例名），opts.selected 加主色外圈；critical 边用主色加粗（spec.critical 为真时出图例），strong 边用正文色加粗。
       没有前后置关系的行不进图，在图下排成一行行签条，同样可点。
   timeline({ lanes:[{id,label}], items:[{lane,at,end?,tone,label,title?}], from?, to? }, opts)
       时间线：横轴时刻，一道一行；有 end 画成一段，否则画成点；竖线标出现在。 */
(function () {
  'use strict';

  var Dash = window.Dash;
  var h = Dash.h;
  var C = Dash.charts;
  var K = Dash.internal.chartKit;
  var css = K.css;
  var tone = K.tone;
  var rgb = K.rgb;
  var mix = K.mix;
  var tint = K.tint;
  var dark = K.dark;
  var tip = K.tip;
  var host = K.host;
  var wrap = K.wrap;
  var emptyNode = K.emptyNode;
  var clickTo = K.clickTo;

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
})();
