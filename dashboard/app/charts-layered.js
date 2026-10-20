/* 观察台图组件·分层关系图 layered。共用底座在 charts.js（Dash.internal.chartKit）。
   layered({ columns:[{kind,label}], nodes:[{id,label,kind,group?,title?}], edges:[{from,to,kind,title?}] }, opts)
       分层关系图：按 columns 从左到右分栏，栏内按 group 分段，栏宽按本栏最长的标签分配（截断的标签悬停看全文）；节点色取 node 组、边色取 edge 组；
       图上方可按边种开关；点节点高亮上下游，再点或点空白取消。opts.fold 是可按段折叠的节点种类（节点多于 opts.foldOver 默认折起，点段名展开），
       opts.through 是高亮时继续往下走的节点种类（默认全部），opts.state 是调用方保存的交互状态（选中、折叠、关掉的边种，重画后保留）；
       返回的节点带 pick(id)，从图外选中一个节点。 */
(function () {
  'use strict';

  var Dash = window.Dash;
  var h = Dash.h;
  var C = Dash.charts;
  var K = Dash.internal.chartKit;
  var css = K.css;
  var tint = K.tint;
  var tip = K.tip;
  var host = K.host;
  var wrap = K.wrap;
  var emptyNode = K.emptyNode;

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
})();
