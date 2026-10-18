/* 健康页：全部检查按分组列出（测试与门禁、代码与文档、工作区与推送、协作流程、后台与钩子、会话与子代理）。
   每项一行：状态点、标题、一句话；点开看细节、建议、链接。检查由 .collab/tools/monitor/check/ 产生（巡逻读同一份），数据 health（10 min 刷新）。 */
(function () {
  var h = Dash.h;
  var GROUPS = ['测试与门禁', '代码与文档', '工作区与推送', '协作流程', '后台与钩子', '会话与子代理'];
  var TONE = { ok: 'ok', warn: 'warn', bad: 'risk' };
  var RANK = { bad: 0, warn: 1, ok: 2 };

  function detailRow(d) {
    return h('div.hl-detail',
      h('span.hl-dtext', d.link ? Dash.a(d.link, d.text) : d.text),
      d.meta ? h('span.hl-dmeta', d.meta) : null);
  }

  function checkRow(c) {
    var body = h('div.hl-body');
    (c.details || []).forEach(function (d) { body.appendChild(detailRow(d)); });
    if (c.tip) body.appendChild(h('div.hl-tip', h('b', '建议：'), c.tip));
    if (c.link) body.appendChild(h('div.hl-detail', Dash.a(c.link, '打开相关文件')));
    var empty = !body.childNodes.length;
    return h('details.hl-item.t-' + TONE[c.level],
      h('summary.hl-sum', h('span.dot'), h('span.hl-title', c.title), h('span.hl-summary', c.summary), empty ? null : h('span.hl-caret')),
      empty ? null : body);
  }

  function groupCard(name, list) {
    var bad = list.filter(function (c) { return c.level !== 'ok'; }).length;
    var meta = (list.length - bad) + ' 项正常' + (bad ? '，' + bad + ' 项要看' : '');
    var sorted = list.slice().sort(function (a, b) { return RANK[a.level] - RANK[b.level]; });   // 稳定排序：要看的在前，组内先后不变
    var card = Dash.card(name, h('div.hl-list', sorted.map(checkRow)), meta);
    if (bad) card.classList.add('has-bad');
    return card;
  }

  Dash.panel({
    id: 'health',
    page: 'health',
    order: 10,
    card: false,
    needs: ['health'],
    render: function (el, d) {
      var checks = d.health.checks || [];
      var bad = checks.filter(function (c) { return c.level !== 'ok'; });
      var root = h('div.stack');
      root.appendChild(bad.length
        ? Dash.status(bad.length + ' 项要看，' + (checks.length - bad.length) + ' 项正常', bad.some(function (c) { return c.level === 'bad'; }) ? 'risk' : 'warn')
        : Dash.status('全部 ' + checks.length + ' 项正常', 'ok'));
      GROUPS.forEach(function (g) {
        var list = checks.filter(function (c) { return c.group === g; });
        if (list.length) root.appendChild(groupCard(g, list));
      });
      var other = checks.filter(function (c) { return GROUPS.indexOf(c.group) < 0; });
      if (other.length) root.appendChild(groupCard('其他', other));
      el.appendChild(root);
    }
  });
})();
