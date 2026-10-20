/* 资料页 · 结构：代码目录矩形树图（面积为代码行、颜色为层）、每层的测试占比，下面是可展开的目录树。
   块（面板 id）：lib-structure-map 矩形树图 · lib-structure-tree 目录树。数据 structure。 */
(function () {
  var h = Dash.h;

  function rows(n) { return Dash.num(n) + ' 行'; }
  function dirNode(d, id) {
    return { id: id, path: d.path, files: d.files, loc: d.loc, testLoc: d.testLoc, note: d.note, children: (d.children || []).map(function (c, i) { return dirNode(c, id + '/' + i); }) };
  }
  function layerNode(l, i) {
    var dirs = (l.dirs || []).map(function (d, j) { return dirNode(d, 'l' + i + '/' + j); });
    var sum = function (k) { return dirs.reduce(function (a, d) { return a + (d[k] || 0); }, 0); };
    return { id: 'l' + i, layer: true, name: l.name, note: l.note, files: sum('files'), loc: sum('loc'), testLoc: sum('testLoc'), children: dirs };
  }
  function mapNode(n) {
    return { name: n.layer ? n.name : n.path.split('/').pop() || n.path, value: n.loc || 0,
      title: (n.note || '') + (n.testLoc ? '（测试代码 ' + rows(n.testLoc) + '）' : ''), children: (n.children || []).map(mapNode) };
  }

  function pct(a, b) { return b ? Math.round(a * 100 / b) + '%' : '—'; }

  Dash.panel({
    id: 'lib-structure-map', page: 'library', tab: 'structure', title: '代码分布', order: 10, needs: ['structure'],
    note: '代码都在哪：面积是生产代码行数，一层一色、层名写在色块顶上（越深越靠下层）；点格子下钻，底部路径条返回',
    render: function (el, d) {
      var layers = (d.structure.layers || []).map(layerNode);
      var loc = layers.reduce(function (a, l) { return a + l.loc; }, 0), test = layers.reduce(function (a, l) { return a + (l.testLoc || 0); }, 0);
      el.appendChild(Dash.status(layers.length + ' 层，生产代码 ' + rows(loc) + '，测试代码 ' + rows(test) + '（是生产代码的 ' + pct(test, loc) + '）', 'muted'));
      el.appendChild(h('div.legend', layers.map(function (l, i) {
        return h('span.legend-item', h('i.legend-swatch', { style: 'background: var(' + Dash.colorVar('layer', i) + ')' }), h('b', String(i + 1)), ' ', l.name);
      })));
      el.appendChild(Dash.charts.treemap({ nodes: layers.map(function (l, i) { var m = mapNode(l); m.name = (i + 1) + ' ' + l.name; return m; }) }, { unit: '行', height: 480 }));
    }
  });

  Dash.panel({
    id: 'lib-structure-tests', page: 'library', tab: 'structure', title: '测试占比', order: 15, needs: ['structure'],
    note: '每层的生产代码与测试代码各多少行；括号里是测试是生产代码的百分之几',
    render: function (el, d) {
      var layers = (d.structure.layers || []).map(layerNode);
      var cats = layers.map(function (l) { return l.name + '（' + pct(l.testLoc || 0, l.loc) + '）'; });
      el.appendChild(Dash.charts.bars({ categories: cats, horizontal: true, stack: true, series: [
        { name: '生产代码', tone: ['layer', 0], values: layers.map(function (l) { return l.loc; }) },
        { name: '测试代码', tone: ['layer', 5], values: layers.map(function (l) { return l.testLoc || 0; }) }
      ] }, { unit: '行', labelWidth: 170 }));
    }
  });

  function label(n, depth) {
    var name = n.layer ? h('span.st-name.st-layer', n.name) : h('span.st-name', Dash.a(n.path, depth <= 1 ? n.path : n.path.split('/').pop()));
    return h('span.st-line', name, h('span.st-note', n.note || ''), h('span.st-files', n.files ? Dash.num(n.files) + ' 个文件' : '没有文件'),
      h('span.st-loc', { title: n.testLoc ? '测试代码 ' + rows(n.testLoc) : '' }, n.loc ? rows(n.loc) : '没有代码'));
  }

  Dash.panel({
    id: 'lib-structure-tree', page: 'library', tab: 'structure', title: '目录树', note: '每个目录管什么、多少文件与行数；依赖只朝下：上层可以用下层，下层不认识上层', order: 20, needs: ['structure'],
    render: function (el, d) {
      el.appendChild(Dash.tree({ key: 'structure', nodes: (d.structure.layers || []).map(layerNode), id: function (n) { return n.id; }, label: label, openDepth: 1 }));
    }
  });
})();
