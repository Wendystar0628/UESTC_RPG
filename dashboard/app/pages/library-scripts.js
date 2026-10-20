/* 资料页 · 脚本目录：按根分组的树，每个一句作用；点"详情"跳到体系页的脚本明细（用法、谁调用、读写什么、最近运行）。
   块（面板 id）：lib-scripts。数据 catalog（全部由程序从脚本自己读出）；选中的脚本放在 Dash.select('script')，与体系页共用。 */
(function () {
  var h = Dash.h;

  function scriptNode(s) { return { kind: 'script', s: s, id: s.path }; }
  function dirNode(d) { return { kind: 'dir', d: d, id: 'dir:' + d.path, children: d.dirs.map(dirNode).concat(d.scripts.map(scriptNode)) }; }

  function detail(path) {
    return h('button.btn.btn-quiet.sx-more', { type: 'button', on: { click: function (e) {
      e.stopPropagation();
      if (Dash.selected('script') !== path) Dash.select('script', path);
      Dash.go('system/system-scripts');
    } } }, '详情');
  }

  function nodeLabel(n) {
    if (n.kind === 'dir') return h('span.sx-line', h('span.sx-dir', n.d.name + '/'), h('span.sx-count', n.d.count + ' 个'));
    var s = n.s;
    return h('span.sx-line',
      h('span.sx-name', Dash.a(s.path, s.name)),
      s.purpose ? h('span.sx-purpose', Dash.gist(s.purpose).text) : h('span.sx-purpose.is-missing', '没写开头说明'),
      h('span.sx-tags', detail(s.path), s.running ? Dash.chip('在跑', 'ok') : null, s.purpose ? null : Dash.chip('缺说明', 'auto'), Dash.chip(s.lang, 'muted'),
        Dash.chip(s.tracked ? '进 git' : '仅本机', s.tracked ? 'muted' : 'auto')));
  }

  Dash.panel({
    id: 'lib-scripts', page: 'library', tab: 'scripts', title: '脚本目录', note: '仓库里有哪些脚本、各管什么：按根目录分组，每个一句作用；点"详情"看用法、谁调用它、它读写什么、最近运行', order: 10, needs: ['catalog'],
    render: function (el, d) {
      var c = d.catalog;
      el.appendChild(Dash.status('共 ' + Dash.num(c.total) + ' 个脚本，' + (c.missing ? c.missing + ' 个没写开头说明' : '都写了开头说明'), c.missing ? 'warn' : 'ok'));
      (c.groups || []).forEach(function (g) {
        el.appendChild(h('section.sx-group', h('h3.sub-title', g.label + '（' + g.count + ' 个）', g.note ? h('span.muted', '  ' + g.note) : null),
          Dash.tree({ key: 'scripts:' + g.label, nodes: g.dirs.map(dirNode).concat(g.scripts.map(scriptNode)), id: function (n) { return n.id; }, label: nodeLabel, openDepth: 0 })));
      });
    }
  });
})();
