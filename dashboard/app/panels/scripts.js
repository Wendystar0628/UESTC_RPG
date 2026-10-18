/* 脚本页：所有脚本与作用，按组列，组内按子目录成可展开的树；点脚本看用法、开头说明全文、路径、行数、修改时刻。数据 catalog（live.py 每 60 s 写，全部由程序读出）。 */
(function () {
  var h = Dash.h;

  function scriptNode(s) { return { kind: 'script', s: s, id: s.path, children: [{ kind: 'detail', s: s, id: s.path + '#detail' }] }; }
  function dirNode(d) {
    return { kind: 'dir', d: d, id: 'dir:' + d.path, children: d.dirs.map(dirNode).concat(d.scripts.map(scriptNode)) };
  }
  function missingIn(d) {
    return d.scripts.filter(function (s) { return !s.purpose; }).length + d.dirs.reduce(function (n, x) { return n + missingIn(x); }, 0);
  }

  function scriptLabel(s) {
    var gist = Dash.gist(s.purpose);
    return h('span.sx-line',
      h('span.sx-name', s.name),
      s.purpose ? h('span.sx-purpose', gist.text) : h('span.sx-purpose.is-missing', '没写开头说明'),
      h('span.sx-tags',
        s.running ? Dash.chip('在跑', 'ok') : null,
        s.purpose ? null : Dash.chip('缺说明', 'auto'),
        Dash.chip(s.lang, 'muted'),
        Dash.chip(s.tracked ? '进 git' : '仅本机', s.tracked ? 'muted' : 'auto')));
  }

  function detailLabel(s) {
    return h('div.sx-detail',
      s.usage ? h('div.sx-block', h('div.sx-cap', '用法'), h('pre.sx-pre', s.usage.replace(/^(用法|usage)\s*[：:]\s*/i, ''))) : null,
      s.intro ? h('div.sx-block', h('div.sx-cap', '开头说明'), h('pre.sx-pre', s.intro))
        : h('div.sx-block.muted', '没写开头说明：按 ENG-CODE 接入要点在开头补一句话作用，另起一行写“用法：”。'),
      h('div.sx-meta',
        Dash.a(s.path, s.path), h('span', s.lines + ' 行'), h('span', '修改 ' + Dash.at(s.mtime)),
        s.running ? h('span', '在跑 ' + s.running.n + ' 个进程，最早 ' + Dash.at(s.running.since) + ' 起') : null));
  }

  function nodeLabel(n) {
    if (n.kind === 'script') return scriptLabel(n.s);
    if (n.kind === 'detail') return detailLabel(n.s);
    var miss = missingIn(n.d);
    return h('span.sx-line', h('span.sx-dir', n.d.name + '/'), h('span.sx-count', n.d.count + ' 个'), miss ? Dash.chip(miss + ' 个缺说明', 'auto') : null);
  }

  function group(g) {
    var nodes = g.dirs.map(dirNode).concat(g.scripts.map(scriptNode));
    var title = g.label + '（' + g.count + ' 个' + (g.missing ? '，' + g.missing + ' 个缺说明' : '') + '）';
    return Dash.card(title, Dash.tree({ key: 'scripts:' + g.label, nodes: nodes, id: function (n) { return n.id; }, label: nodeLabel, openDepth: 0 }), g.note);
  }

  Dash.panel({
    id: 'scripts',
    page: 'scripts',
    order: 10,
    card: false,
    needs: ['catalog'],
    render: function (el, d) {
      var c = d.catalog;
      var stale = Dash.isStale('catalog');
      var root = h('div.stack' + (stale ? '.is-stale' : ''));
      var top = Dash.statusLine(c.missing ? 'auto' : 'ok');
      top.body.append('共 ' + c.total + ' 个脚本，' + (c.missing ? c.missing + ' 个没写开头说明' : '都写了开头说明') + (stale ? '（数据旧了）' : ''));
      root.appendChild(top);
      c.groups.forEach(function (g) { root.appendChild(group(g)); });
      el.appendChild(root);
    }
  });
})();
