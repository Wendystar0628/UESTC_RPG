/* 结构页：层 → 目录 → 子目录的可展开树；文件数与代码行都写确切值。数据 structure（10 min 刷新）。 */
(function () {
  const h = Dash.h;

  function filesText(n) { return n ? n + ' 个文件' : '没有文件'; }
  function rows(n) { return Dash.num(n) + ' 行'; }
  function locText(n) { return n ? rows(n) : '没有代码'; }

  /* 统一成树节点：层（汇总下面的目录）与目录（含递归的 children） */
  function dirNode(d, id) {
    return { id: id, path: d.path, files: d.files, loc: d.loc, testLoc: d.testLoc, note: d.note, children: (d.children || []).map((c, i) => dirNode(c, id + '/' + i)) };
  }
  function layerNode(l, i) {
    const dirs = (l.dirs || []).map((d, j) => dirNode(d, 'l' + i + '/' + j));
    const sum = (k) => dirs.reduce((a, d) => a + (d[k] || 0), 0);
    return { id: 'l' + i, layer: true, name: l.name, note: l.note, files: sum('files'), loc: sum('loc'), children: dirs };
  }

  function label(n, depth) {
    const parts = [];
    if (n.layer) {
      parts.push(h('span.st-name.st-layer', n.name));
    } else {
      const name = depth <= 1 ? n.path : n.path.split('/').pop();
      parts.push(h('span.st-name', Dash.a(n.path, name)));
    }
    parts.push(h('span.st-note', n.note || ''));
    const tip = n.testLoc ? '测试代码 ' + rows(n.testLoc) : '';
    parts.push(h('span.st-files', filesText(n.files)));
    parts.push(h('span.st-loc', { title: tip }, locText(n.loc)));
    return h('span.st-line', parts);
  }

  Dash.panel({
    id: 'structure',
    page: 'structure',
    title: '项目分层',
    order: 10,
    needs: ['structure'],
    render(el, data) {
      const s = data.structure;
      el.classList.add('st');
      if (!s) { el.append(h('p.muted', '还没有结构数据')); return; }
      const layers = s.layers || [];
      const dirs = layers.reduce((acc, l) => acc.concat(l.dirs || []), []);
      const biggest = dirs.slice().sort((a, b) => (b.loc || 0) - (a.loc || 0))[0];
      const top = Dash.statusLine('muted');
      top.body.append(layers.length + ' 层，' + dirs.length + ' 个主要目录' + (biggest ? '；代码最多的是 ' + biggest.path + '，' + locText(biggest.loc) : ''));
      el.append(top, Dash.tree({
        key: 'structure',
        nodes: layers.map(layerNode),
        id: (n) => n.id,
        label: label,
        openDepth: 1,
      }), h('p.st-foot', '依赖只朝下：上层可以用下层，下层不认识上层。'));
    },
  });
})();
