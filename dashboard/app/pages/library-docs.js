/* 资料页 · 文档：文档树 + 域×域引用热力矩阵（点格展开到文档）。
   块（面板 id）：lib-docs-tree 文档树 · lib-docs-refs 引用矩阵。docrefs 只在打开这个子标签时拉；选中的格子放在 Dash.select('docpair')。 */
(function () {
  var h = Dash.h;

  function charsText(n) { return n ? Dash.num(n) + ' 字' : ''; }

  /* ---------- 文档树：顶层 → 域 → 主文档 → 子文档 → 节 ---------- */
  function sectionNode(sec, path, parentId) {
    var id = parentId + '/' + sec.id;
    return { id: id, kind: 'section', secId: sec.id, title: sec.title, chars: sec.chars, path: path,
      children: (sec.children || []).map(function (c) { return sectionNode(c, path, id); }) };
  }
  function docNode(d, subs, top) {
    var id = 'c:' + d.code;
    var secs = Array.isArray(d.sections) ? d.sections : [];
    return { id: id, kind: 'doc', top: top, doc: d,
      children: (subs[d.code] || []).map(function (x) { return docNode(x, subs, false); }).concat(secs.map(function (x) { return sectionNode(x, d.path, id); })) };
  }
  function buildTree(top, domains) {
    var roots = top ? [docNode(top, {}, true)] : [];
    return roots.concat(domains.map(function (dm) {
      var docs = dm.docs || [], here = {}, subs = {};
      docs.forEach(function (d) { here[d.code] = d; });
      docs.forEach(function (d) { if (d.parent && here[d.parent]) (subs[d.parent] = subs[d.parent] || []).push(d); });
      return { id: 'd:' + dm.key, kind: 'domain', name: dm.name, count: docs.length,
        children: docs.filter(function (d) { return !(d.parent && here[d.parent]); }).map(function (d) { return docNode(d, subs, false); }) };
    }));
  }
  function treeLabel(n) {
    if (n.kind === 'domain') return h('span.dc-line', h('span.dc-dname', n.name), h('span.dc-dcount', n.count + ' 份'));
    if (n.kind === 'section') return h('span.dc-line', h('span.dc-code', n.secId), h('span.dc-stitle', Dash.a(n.path, n.title)), h('span.dc-chars', charsText(n.chars)));
    var d = n.doc;
    return h('span.dc-line', n.top ? h('span.dc-dname', '顶层') : null, h('span.dc-code', d.code), h('span.dc-title', Dash.a(d.path, d.title)),
      h('span.dc-chars', charsText(d.chars)), h('span.dc-sum', d.summary || ''));
  }

  Dash.panel({
    id: 'lib-docs-tree', page: 'library', tab: 'docs', title: '文档树', note: '文档怎么分层：与 AI 读的结构一致，顶层 → 域 → 主文档 → 子文档 → 节；点文档或节打开文件', order: 10, needs: ['docs'],
    render: function (el, d) {
      var x = d.docs, domains = x.domains || [];
      var total = domains.reduce(function (n, dm) { return n + (dm.docs || []).length; }, x.top ? 1 : 0);
      el.appendChild(Dash.status((x.top ? '顶层 ' + x.top.code + ' 加 ' : '') + domains.length + ' 个域，共 ' + Dash.num(total) + ' 份文档', 'muted'));
      el.appendChild(Dash.tree({ key: 'docs', nodes: buildTree(x.top, domains), id: function (n) { return n.id; }, label: treeLabel, openDepth: 0 }));
    }
  });

  /* ---------- 域×域引用矩阵 ---------- */
  var inner = false;   // 矩阵里算不算域内引用（对角线）：默认不算，免得域内的大数把跨域压成一片浅色
  Dash.panel({
    id: 'lib-docs-refs', page: 'library', tab: 'docs', title: '引用矩阵', order: 20, partial: true, needs: ['docs', 'docrefs'], watch: ['docpair'],
    note: '哪个域的文档在引用哪个域：行是引用方、列是被引用方，格子是正文引用次数；点格子列出这两个域之间具体的文档。右边是被引用最多的文档',
    render: function (el, d) {
      if (!d.docs) { el.appendChild(Dash.empty('等文档树', 'docs 还没有数据', true)); return; }
      if (!d.docrefs) { el.appendChild(Dash.empty('等文档引用边', 'docrefs 由采集侧（C-239）从 docs 拆出单独写出，打开这页时才拉', true)); return; }
      var x = d.docs, doms = (x.top ? [{ name: '顶层', docs: [x.top] }] : []).concat(x.domains || []);
      var domOf = {}, title = {};
      doms.forEach(function (dm, i) { (dm.docs || []).forEach(function (doc) { domOf[doc.code] = i; title[doc.code] = doc.title; }); });
      var m = {}, inDeg = {}, edges = (d.docrefs.edges || []).filter(function (e) { return e.from in domOf && e.to in domOf && e.from !== e.to; });
      edges.forEach(function (e) {
        var k = domOf[e.from] + ',' + domOf[e.to];
        m[k] = (m[k] || 0) + (e.n || 1);
        inDeg[e.to] = (inDeg[e.to] || 0) + (e.n || 1);
      });
      var total = edges.reduce(function (a, e) { return a + (e.n || 1); }, 0);
      var cross = edges.filter(function (e) { return domOf[e.from] !== domOf[e.to]; }).reduce(function (a, e) { return a + (e.n || 1); }, 0);
      el.appendChild(Dash.status(Dash.num(edges.length) + ' 对文档之间引用 ' + Dash.num(total) + ' 次，其中跨域 ' + Dash.num(cross) + ' 次', 'muted'));
      var cells = [];
      doms.forEach(function (_, yi) { doms.forEach(function (__, xi) {
        if (xi !== yi || inner) cells.push({ x: xi, y: yi, value: m[yi + ',' + xi] || 0 });
      }); });
      var names = doms.map(function (dm) { return dm.name; });
      var top = Object.keys(inDeg).sort(function (a, b) { return inDeg[b] - inDeg[a]; }).slice(0, 15);
      el.appendChild(Dash.segment([{ id: 'cross', label: '只看跨域' }, { id: 'all', label: '含域内引用' }], inner ? 'all' : 'cross',
        function (id) { inner = id === 'all'; Dash.redraw('lib-docs-refs'); }));
      el.appendChild(h('div.dc-refs',
        h('div', Dash.charts.heatmap({ x: names, y: names, cells: cells, mode: 'count' },
          { cellHeight: 32, labelWidth: 64, unit: '次', key: 'lib-docs-heat', onClick: function (c) { Dash.select('docpair', c.y + ',' + c.x); } })),
        h('div', h('h3.sub-title', '被引用最多的 ' + top.length + ' 份'), Dash.charts.bars({ categories: top, horizontal: true,
          series: [{ name: '被引用', tone: ['node', 'doc'], values: top.map(function (k) { return inDeg[k]; }) }] }, { unit: '次', labelWidth: 150, key: 'lib-docs-top' }))));
      var pick = Dash.selected('docpair');
      if (!pick) return;
      var ab = pick.split(',').map(Number);
      if (!names[ab[0]] || !names[ab[1]]) return;
      var list = edges.filter(function (e) { return domOf[e.from] === ab[0] && domOf[e.to] === ab[1]; }).sort(function (p, q) { return (q.n || 1) - (p.n || 1); });
      el.appendChild(h('h3.sub-title', names[ab[0]] + ' → ' + names[ab[1]] + '：' + Dash.num(list.length) + ' 对文档'));
      el.appendChild(Dash.table([
        { label: '引用方', cell: function (e) { return h('span', h('span.mono', e.from), ' ', h('span.muted', title[e.from] || '')); } },
        { label: '被引用', cell: function (e) { return h('span', h('span.mono', e.to), ' ', h('span.muted', title[e.to] || '')); } },
        { label: '次数', num: true, cell: function (e) { return Dash.num(e.n || 1); } }
      ], list, { empty: '这两个域之间没有引用' }));
    }
  });
})();
