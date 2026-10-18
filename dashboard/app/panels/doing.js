/* 在做页：正在处理的工作。顶部两个主会话现在在干什么；每个工作项一张卡（行、状态、谁、子代理、改动的目录、任务单、不一致）；再是临时工作树一张卡；末尾是没挂队列行的子代理。数据 live.work、live.worktrees（15 s 刷新）。 */
(function () {
  var h = Dash.h;

  function sideOf(who) {
    var s = String(who || '').toLowerCase();
    return s === 'codex' ? 'codex' : s === 'claude' ? 'claude' : 'muted';
  }

  /* 主会话现况：名称、型号、推理档、在跑/空闲、起始时刻、Codex 最新一句话 */
  function mainRow(m) {
    var side = m.name.indexOf('Codex') >= 0 ? 'codex' : 'claude';
    return h('div.dg-main.dg-main--' + side,
      h('div.dg-main-head',
        h('span.who-dot.who-dot--' + m.state),
        h('span.dg-main-name', m.name),
        Dash.chip(Dash.modelName(m.model) || '—', side),
        m.effort ? Dash.chip(m.effort, 'muted') : null,
        Dash.chip(m.state === 'running' ? '在跑' : '空闲', m.state === 'running' ? 'ok' : 'muted'),
        h('span.dg-main-when', Dash.at(m.since) + (m.state === 'running' ? ' 起，已 ' + Dash.elapsed(m.since) : ' 起'))),
      m.task ? h('div.dg-main-task', Dash.longText(m.task)) : null);
  }

  function agentChip(a) {
    var c = Dash.chip(Dash.agentLabel(a), a.side);
    c.title = a.task || '';
    return c;
  }

  function changesLine(ch) {
    if (!ch.files) return h('div.dg-line.muted', '工作区里这行的目录下没有未提交的改动');
    return h('div.dg-line', h('b', '改动 ' + ch.files + ' 个文件'), '：',
      ch.dirs.map(function (d, i) { return h('span.dg-dir', (i ? '、' : '') + d.path + '（' + d.files + '）'); }));
  }

  function itemCard(it) {
    var head = h('div.dg-head',
      h('span.dg-id', it.row),
      h('span.dg-what', Dash.longText(it.what)),
      Dash.chip(it.state, it.state === '进行中' ? 'ok' : 'muted'),
      Dash.chip(it.who || '—', sideOf(it.who)));
    var body = h('div.dg-card', head);
    body.appendChild(it.agents.length
      ? h('div.dg-agents', it.agents.map(agentChip))
      : h('div.dg-line.muted', '最近 30 分钟没有对上的子代理'));
    body.appendChild(changesLine(it.changes));
    var foot = h('div.dg-line',
      it.sheet ? h('span', '任务单：', Dash.a(it.sheet, it.sheet.split('/').pop().replace(/\.md$/, ''))) : h('span.muted', '没有对上任务单'),
      it.dirs ? h('span.muted', ' · 目录：' + it.dirs) : null);
    body.appendChild(foot);
    it.flags.forEach(function (f) { body.appendChild(h('div.dg-flag', f)); });
    return body;
  }

  /* 临时工作树：只用于统一编译、快检和视觉样板，开在指定目录下，用完 remove（WF-RULE §13） */
  function worktreeCard(live) {
    var w = live.worktrees;
    var n = w && w.items ? w.items.length : 0;
    var flagged = n ? w.items.filter(function (x) { return x.flags.length; }).length : 0;
    var meta = w && w.root ? '指定目录 ' + w.root : '';
    return Dash.card('临时工作树（' + n + ' 个' + (flagged ? '，' + flagged + ' 个要看' : '') + '）', Dash.worktrees(live), meta);
  }

  Dash.panel({
    id: 'doing',
    page: 'doing',
    order: 10,
    card: false,
    needs: ['live'],
    render: function (el, d) {
      var stale = Dash.isStale('live');
      var root = h('div.stack' + (stale ? '.is-stale' : ''));
      var gap = Dash.liveGap(d.live, 'work', ['在做的工作', '施工队列'], '');
      if (gap && (d.live === null || d.live.work === undefined || d.live.work === null || (d.live.errors || []).length)) {
        root.appendChild(Dash.card('在做', gap));
      } else {
        var w = d.live.work;
        root.appendChild(Dash.card('主会话现在在干什么', h('div.dg-mains', w.mains.length ? w.mains.map(mainRow) : Dash.empty('等数据', '', true)), stale ? '数据旧了' : ''));
        var items = w.items.slice().sort(function (a, b) { return (a.state === '进行中' ? 0 : 1) - (b.state === '进行中' ? 0 : 1); });
        root.appendChild(Dash.status(items.length ? '队列里 ' + items.length + ' 项在做（进行中 ' + items.filter(function (i) { return i.state === '进行中'; }).length +
          '，待编译 ' + items.filter(function (i) { return i.state === '待编译'; }).length + '）' : '队列里没有进行中或待编译的行', items.length ? 'ok' : 'muted'));
        root.appendChild(h('div.dg-cards', items.map(itemCard)));
      }
      if (d.live) root.appendChild(worktreeCard(d.live));
      if (d.live && d.live.work) {
        var loose = d.live.work.loose || [];
        root.appendChild(Dash.card('没挂队列行的子代理（' + loose.length + ' 个）',
          loose.length ? h('div.dg-agents', loose.map(agentChip)) : h('div.muted', '最近 30 分钟没有对不上队列行的子代理（调研、核查一类会出现在这里）')));
      }
      el.appendChild(root);
    }
  });
})();
