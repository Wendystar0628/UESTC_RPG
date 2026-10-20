/* 文档页：顶层文档单列，其下域 → 主文档 → 子文档 → 节的可展开树 + 引用关系图（自绘 SVG，不引库）。数据 docs（10 min 刷新）。 */
(function () {
  const NS = 'http://www.w3.org/2000/svg';
  const LEVELS = [
    { key: 'main', label: '主要', k: 2, min: 3, crossMin: 4 }, // 每个文档只画最强的 2 条，跨域的要求更强
    { key: 'more', label: '更多', k: 4, min: 2, crossMin: 2 },
    { key: 'all', label: '全部', k: 1e9, min: 0, crossMin: 0 },
  ];
  const view = { level: 'main', pinned: null }; // 重绘后保持

  function h(tag, cls, kids) {
    const e = document.createElement(tag);
    if (cls) e.className = cls;
    (kids == null ? [] : [].concat(kids)).forEach((k) => {
      if (k == null || k === false) return;
      e.append(k instanceof Node ? k : document.createTextNode(String(k)));
    });
    return e;
  }
  function s(tag, attrs, kids) {
    const e = document.createElementNS(NS, tag);
    Object.keys(attrs || {}).forEach((k) => e.setAttribute(k, attrs[k]));
    (kids == null ? [] : [].concat(kids)).forEach((k) => e.append(k instanceof Node ? k : document.createTextNode(String(k))));
    return e;
  }
  const status = Dash.statusLine;
  function hrefOf(path) {
    const l = Dash.link(path);
    if (l instanceof Node) return l.getAttribute('href') || '#';
    const str = String(l);
    if (str.charAt(0) === '<') {
      const t = document.createElement('div');
      t.innerHTML = str;
      const a = t.querySelector('a');
      return a ? a.getAttribute('href') : '#';
    }
    return str;
  }
  function sizeLevel(chars) { return Math.max(1, Math.min(4, Math.ceil((chars || 0) / 3000))); }
  function charsText(n) { return n ? Dash.num(n) + ' 字' : ''; }

  /* ---------- 文档树：域 → 主文档 → 子文档 → 节 ---------- */
  function sectionNode(sec, docId, path, parentId) {
    const id = parentId + '/' + sec.id;
    return { id: id, kind: 'section', secId: sec.id, title: sec.title, chars: sec.chars, path: path,
      children: (sec.children || []).map((c) => sectionNode(c, docId, path, id)) };
  }
  function buildTree(domains) {
    return domains.map((dm) => {
      const docs = dm.docs || [];
      const here = {};
      docs.forEach((d) => { here[d.code] = d; });
      const subs = {};
      docs.forEach((d) => { if (d.parent && here[d.parent]) (subs[d.parent] = subs[d.parent] || []).push(d); });
      function docNode(d) {
        const id = 'c:' + d.code;
        const secs = Array.isArray(d.sections) ? d.sections : [];   // 采集还没更新时 sections 是个数，当作没有
        return { id: id, kind: 'doc', doc: d,
          children: (subs[d.code] || []).map(docNode).concat(secs.map((x) => sectionNode(x, id, d.path, id))) };
      }
      const roots = docs.filter((d) => !(d.parent && here[d.parent])).map(docNode);
      return { id: 'd:' + dm.key, kind: 'domain', name: dm.name, count: docs.length, children: roots };
    });
  }
  function topNode(d) {
    const id = 'c:' + d.code;
    const secs = Array.isArray(d.sections) ? d.sections : [];
    return { id: id, kind: 'doc', top: true, doc: d, children: secs.map((x) => sectionNode(x, id, d.path, id)) };
  }
  function treeLabel(n) {
    if (n.kind === 'domain') return h('span', 'dc-line', [h('span', 'dc-dname', n.name), h('span', 'dc-dcount', n.count + ' 份')]);
    if (n.kind === 'section') {
      return h('span', 'dc-line', [h('span', 'dc-code', n.secId), h('span', 'dc-stitle', Dash.a(n.path, n.title)), h('span', 'dc-chars', charsText(n.chars))]);
    }
    const d = n.doc;
    const bar = h('span', 'dc-size');
    for (let i = 1; i <= 4; i++) bar.append(h('i', i <= sizeLevel(d.chars) ? 'on' : ''));
    bar.title = charsText(d.chars);
    return h('span', 'dc-line', [n.top ? h('span', 'dc-dname', '顶层') : null, h('span', 'dc-code', d.code), h('span', 'dc-title', Dash.a(d.path, d.title)), bar, h('span', 'dc-sum', d.summary || '')]);
  }
  function docTree(top, domains) {
    return Dash.tree({ key: 'docs', nodes: (top ? [topNode(top)] : []).concat(buildTree(domains)), id: (n) => n.id, label: treeLabel, openDepth: 0 });
  }

  /* ---------- 引用关系图 ---------- */
  function graph(data) {
    const domains = (data.top ? [{ key: 'top', name: '顶层', docs: [data.top] }] : []).concat(data.domains || []);
    const nodes = [];
    const byCode = {};
    domains.forEach((dm, di) => (dm.docs || []).forEach((d) => {
      const n = { code: d.code, title: d.title, path: d.path, chars: d.chars || 0, dom: di, domName: dm.name, adj: [] };
      nodes.push(n);
      byCode[d.code] = n;
    }));
    const edges = [];
    (data.edges || []).forEach((e) => {
      const a = byCode[e.from], b = byCode[e.to];
      if (!a || !b || a === b) return;
      const ed = { a, b, n: e.n || 1, cross: a.dom !== b.dom, shown: false };
      edges.push(ed);
      a.adj.push(ed);
      b.adj.push(ed);
    });
    const root = h('div', 'dg');
    if (!nodes.length) return root;

    // 极坐标：按域连续排布，域间留缝
    const R = 205, GAP = 0.16;
    const total = Math.PI * 2 - GAP * domains.length;
    const per = total / nodes.length;
    let ang = -Math.PI / 2;
    let lastDom = -1;
    const arcs = {};
    nodes.forEach((n) => {
      if (n.dom !== lastDom) { if (lastDom !== -1) ang += GAP; lastDom = n.dom; arcs[n.dom] = { from: ang }; }
      n.ang = ang + per / 2;
      ang += per;
      arcs[n.dom].to = ang;
      n.x = Math.cos(n.ang) * R;
      n.y = Math.sin(n.ang) * R;
    });

    function curve(e) {
      let d = Math.abs(e.a.ang - e.b.ang);
      if (d > Math.PI) d = Math.PI * 2 - d;
      const k = 0.85 - 0.45 * (d / Math.PI); // 近的贴边，远的往中心收拢成束
      const cx = ((e.a.x + e.b.x) / 2) * k;
      const cy = ((e.a.y + e.b.y) / 2) * k;
      return 'M' + e.a.x.toFixed(1) + ' ' + e.a.y.toFixed(1) + ' Q' + cx.toFixed(1) + ' ' + cy.toFixed(1) + ' ' + e.b.x.toFixed(1) + ' ' + e.b.y.toFixed(1);
    }

    const svg = s('svg', { viewBox: '-318 -312 636 624', class: 'dg-svg', role: 'img', 'aria-label': '文档引用关系图' });
    const gArcs = s('g', { class: 'dg-arcs' });
    const gEdges = s('g', { class: 'dg-edges' });
    const gNodes = s('g', { class: 'dg-nodes' });
    svg.append(gArcs, gEdges, gNodes);

    // 域弧与域名
    domains.forEach((dm, i) => {
      const a = arcs[i];
      if (!a) return;
      const r = R + 8;
      const p0 = [Math.cos(a.from) * r, Math.sin(a.from) * r], p1 = [Math.cos(a.to) * r, Math.sin(a.to) * r];
      const large = a.to - a.from > Math.PI ? 1 : 0;
      gArcs.append(s('path', { class: 'dg-arc', style: '--dn:' + (0.3 + 0.1 * i), d: 'M' + p0[0].toFixed(1) + ' ' + p0[1].toFixed(1) + ' A' + r + ' ' + r + ' 0 ' + large + ' 1 ' + p1[0].toFixed(1) + ' ' + p1[1].toFixed(1) }));
      const mid = (a.from + a.to) / 2, lr = R - 38;
      gArcs.append(s('text', { class: 'dg-dom', x: (Math.cos(mid) * lr).toFixed(1), y: (Math.sin(mid) * lr).toFixed(1), 'text-anchor': 'middle', 'dominant-baseline': 'middle' }, dm.name));
    });

    edges.forEach((e) => {
      e.el = s('path', { class: 'dg-edge' + (e.cross ? ' is-cross' : ''), d: curve(e), style: 'stroke-width:' + (0.6 + Math.min(e.n, 8) * 0.4).toFixed(2) });
      gEdges.append(e.el);
    });

    nodes.forEach((n) => {
      const r = 3 + Math.min(4, n.chars / 3000);
      const deg = (n.ang * 180) / Math.PI;
      const flip = Math.cos(n.ang) < 0;
      const g = s('g', { class: 'dg-node', style: '--dn:' + (0.3 + 0.1 * n.dom), tabindex: '0' });
      g.append(
        s('title', {}, n.code + ' ' + n.title),
        s('circle', { cx: n.x.toFixed(1), cy: n.y.toFixed(1), r: r.toFixed(1) }),
        s('text', {
          x: ((R + 16) * Math.cos(n.ang)).toFixed(1), y: ((R + 16) * Math.sin(n.ang)).toFixed(1),
          transform: 'rotate(' + (flip ? deg + 180 : deg).toFixed(1) + ' ' + ((R + 16) * Math.cos(n.ang)).toFixed(1) + ' ' + ((R + 16) * Math.sin(n.ang)).toFixed(1) + ')',
          'text-anchor': flip ? 'end' : 'start', 'dominant-baseline': 'middle',
        }, n.code),
        // 透明的大点击区
        s('circle', { class: 'dg-hit', cx: n.x.toFixed(1), cy: n.y.toFixed(1), r: 9 })
      );
      n.el = g;
      g.addEventListener('mouseenter', () => focus(n));
      g.addEventListener('mouseleave', () => focus(view.pinned ? byCode[view.pinned] : null));
      g.addEventListener('focus', () => focus(n));
      g.addEventListener('click', (ev) => { ev.stopPropagation(); view.pinned = view.pinned === n.code ? null : n.code; focus(view.pinned ? n : null); });
      gNodes.append(g);
    });
    svg.addEventListener('click', () => { view.pinned = null; focus(null); });

    // 控件与信息条
    const seg = h('div', 'dg-seg');
    const info = h('div', 'dg-info');
    LEVELS.forEach((lv) => {
      const b = h('button', 'dg-segbtn', lv.label);
      b.type = 'button';
      b.dataset.key = lv.key;
      b.addEventListener('click', () => { view.level = lv.key; applyLevel(); });
      seg.append(b);
    });

    function applyLevel() {
      const lv = LEVELS.find((l) => l.key === view.level) || LEVELS[0];
      edges.forEach((e) => (e.shown = false));
      nodes.forEach((n) => {
        n.adj.slice().sort((x, y) => y.n - x.n).slice(0, lv.k).forEach((e) => { if (e.n >= (e.cross ? lv.crossMin : lv.min)) e.shown = true; });
      });
      edges.forEach((e) => e.el.classList.toggle('is-shown', e.shown));
      [...seg.children].forEach((b) => b.classList.toggle('is-on', b.dataset.key === view.level));
      const cnt = edges.filter((e) => e.shown).length;
      seg.title = '显示 ' + cnt + ' 条关系';
    }

    function focus(n) {
      svg.classList.toggle('is-focus', !!n);
      const nb = new Set();
      if (n) { nb.add(n); n.adj.forEach((e) => { nb.add(e.a); nb.add(e.b); }); }
      nodes.forEach((x) => {
        x.el.classList.toggle('is-self', x === n);
        x.el.classList.toggle('is-nb', !!n && x !== n && nb.has(x));
      });
      edges.forEach((e) => e.el.classList.toggle('is-hot', !!n && (e.a === n || e.b === n)));
      info.replaceChildren();
      if (!n) { info.append(h('span', 'dg-hint', '悬停或点选一个文档，看它和谁有关；默认只画最强的关系')); return; }
      const a = h('a', 'dg-open', n.code + ' ' + n.title);
      a.href = hrefOf(n.path);
      a.target = '_blank';
      a.rel = 'noopener';
      const list = h('span', 'dg-nbs');
      n.adj.slice().sort((x, y) => y.n - x.n).slice(0, 8).forEach((e) => {
        const o = e.a === n ? e.b : e.a;
        const c = h('button', 'dg-nb', o.code);
        c.type = 'button';
        c.title = o.title + '，引用 ' + Dash.num(e.n) + ' 次';
        c.addEventListener('click', (ev) => { ev.stopPropagation(); view.pinned = o.code; focus(o); });
        list.append(c);
      });
      info.append(a, h('span', 'dg-deg', '关联 ' + n.adj.length + ' 份'), list);
    }

    root.append(h('div', 'dg-bar', [h('span', 'dg-label', '显示关系'), seg]), svg, info);
    applyLevel();
    focus(view.pinned && byCode[view.pinned] ? byCode[view.pinned] : null);
    return root;
  }

  Dash.panel({
    id: 'docs',
    page: 'docs',
    title: '文档地图',
    order: 10,
    needs: ['docs'],
    render(el, data) {
      const d = data.docs;
      el.replaceChildren();
      el.classList.add('dc');
      if (!d) { el.append(h('p', 'muted', '还没有文档数据')); return; }
      const domains = d.domains || [];
      const all = domains.reduce((a, x) => a.concat(x.docs || []), d.top ? [d.top] : []);
      const over = ((Dash.get('health') || {}).docs || {}).over || 0; // 口径以 doc-size.py 为准
      const top = status(over ? 'auto' : 'muted');
      top.body.append((d.top ? '顶层 ' + d.top.code + ' 加 ' : '') + domains.length + ' 个域，共 ' + all.length + ' 份文档');
      if (over) top.body.append('，', h('span', 'dc-over', over + ' 份超过体量上限'));
      el.append(top, docTree(d.top, domains));
    },
  });

  Dash.panel({
    id: 'docs-graph',
    page: 'docs',
    title: '引用关系',
    order: 20,
    needs: ['docs'],
    render(el, data) {
      el.replaceChildren();
      el.classList.add('dc');
      if (data.docs) el.append(graph(data.docs));
    },
  });
})();
