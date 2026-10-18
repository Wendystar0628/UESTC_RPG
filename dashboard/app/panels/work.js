/* 施工页：自上而下四块。① 主会话现况；② 正在做（队列里进行中、待编译的行，一行一张详细卡，末尾是没挂队列行的子代理）；
   ③ 排着的（可派、等前置、暂停三栏的紧凑卡片，暂停默认收起）；④ 临时工作树。
   数据 live.work、live.worktrees（15 s 刷新）、queue（10 min 刷新）。总览里的链接用 #work/work-doing 直达第二块（块 id 就是面板 id）。 */
(function () {
  var h = Dash.h;
  var pausedOpen = false;   // 暂停栏展开与否，重绘后保持

  function sideOf(who) {
    var s = String(who || '').toLowerCase();
    return s === 'codex' ? 'codex' : s === 'claude' ? 'claude' : /作者|author/.test(s) ? 'author' : 'muted';
  }
  function workGap(live) {
    var gap = Dash.liveGap(live, 'work', ['在做的工作', '施工队列'], '');
    return gap && (live === null || live.work === undefined || live.work === null || (live.errors || []).length) ? gap : null;
  }

  /* ---------- ① 主会话现况：名称、型号、推理档、在跑/空闲、起始时刻、Codex 最新一句话 ---------- */
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

  Dash.panel({
    id: 'work-mains', page: 'work', order: 10, card: false, needs: ['live'],
    render: function (el, d) {
      var gap = workGap(d.live);
      var stale = Dash.isStale('live');
      el.appendChild(Dash.card('主会话现在在干什么', gap || h('div.dg-mains', d.live.work.mains.length ? d.live.work.mains.map(mainRow) : Dash.empty('等数据', '', true)),
        stale ? '数据旧了' : ''));
    }
  });

  /* ---------- ② 正在做：每个工作项一张卡（行、状态、谁、子代理、改动的目录、任务单、不一致） ---------- */
  function agentChip(a) {
    var c = Dash.chip(Dash.agentLabel(a), a.side);
    c.title = a.task || '';
    return c;
  }
  function changesLine(ch) {
    if (!ch.files) return h('div.dg-line.muted', '工作区里这行的目录下没有未提交的改动');
    return h('div.dg-line', h('b', '改动 ' + ch.files + ' 个文件'), '：',
      ch.dirs.map(function (x, i) { return h('span.dg-dir', (i ? '、' : '') + x.path + '（' + x.files + '）'); }));
  }
  function itemCard(it) {
    var body = h('div.dg-card', h('div.dg-head',
      h('span.dg-id', it.row),
      h('span.dg-what', Dash.longText(it.what)),
      Dash.chip(it.state, it.state === '进行中' ? 'ok' : 'muted'),
      Dash.chip(it.who || '—', sideOf(it.who))));
    body.appendChild(it.agents.length
      ? h('div.dg-agents', it.agents.map(agentChip))
      : h('div.dg-line.muted', '最近 30 分钟没有对上的子代理'));
    body.appendChild(changesLine(it.changes));
    body.appendChild(h('div.dg-line',
      it.sheet ? h('span', '任务单：', Dash.a(it.sheet, it.sheet.split('/').pop().replace(/\.md$/, ''))) : h('span.muted', '没有对上任务单'),
      it.dirs ? h('span.muted', ' · 目录：' + it.dirs) : null));
    it.flags.forEach(function (f) { body.appendChild(h('div.dg-flag', f)); });
    return body;
  }

  Dash.panel({
    id: 'work-doing', page: 'work', order: 20, card: false, needs: ['live'],
    render: function (el, d) {
      var root = h('div.stack' + (Dash.isStale('live') ? '.is-stale' : ''));
      var gap = workGap(d.live);
      if (gap) {
        root.appendChild(Dash.card('正在做', gap));
      } else {
        var items = d.live.work.items.slice().sort(function (a, b) { return (a.state === '进行中' ? 0 : 1) - (b.state === '进行中' ? 0 : 1); });
        var run = items.filter(function (i) { return i.state === '进行中'; }).length;
        root.appendChild(Dash.status(items.length ? '正在做 ' + items.length + ' 项（进行中 ' + run + '，待编译 ' + (items.length - run) + '）' : '队列里没有进行中或待编译的行',
          items.length ? 'ok' : 'muted'));
        root.appendChild(h('div.dg-cards', items.map(itemCard)));
      }
      if (d.live && d.live.work) {
        var loose = d.live.work.loose || [];
        root.appendChild(Dash.card('没挂队列行的子代理（' + loose.length + ' 个）',
          loose.length ? h('div.dg-agents', loose.map(agentChip)) : h('div.muted', '最近 30 分钟没有对不上队列行的子代理（调研、核查一类会出现在这里）')));
      }
      el.appendChild(root);
    }
  });

  /* ---------- ③ 排着的：可派、等前置、暂停，紧凑卡片 ---------- */
  var COLS = [['可派', '可以派出去'], ['等前置', '要先等别的做完'], ['暂停', '先放着']];

  function whoLabel(w) {
    var s = String(w || '');
    return /^claude$/i.test(s) ? 'Claude' : /^codex$/i.test(s) ? 'Codex' : s;
  }
  function qCard(r, col) {
    return h('div.qu-card',
      h('span.qu-id', r.id),
      h('span.qu-foot',
        col === '等前置' && r.pre ? h('span.qu-pre', '等 ' + r.pre) : null,
        r.who ? Dash.chip(whoLabel(r.who), sideOf(r.who)) : null),
      h('span.qu-title', r.title));
  }
  function qColumn(q, col, hint) {
    var rows = (q.rows || []).filter(function (r) { return r.status === col; });
    var n = q.counts && q.counts[col] != null ? q.counts[col] : rows.length;
    var folded = col === '暂停';
    var head = h(folded ? 'button.qu-colhead.qu-fold' : 'header.qu-colhead',
      folded ? h('span.qu-caret') : null, h('span.qu-colname', col), h('span.qu-count', n), h('span.qu-hint', hint));
    var body = h('div.qu-cards', rows.length ? rows.map(function (r) { return qCard(r, col); }) : h('div.qu-none', '空'));
    var sec = h('section.qu-col' + (n ? '' : '.is-empty') + (folded && pausedOpen ? '.is-open' : ''), head, body);
    if (folded) {
      head.type = 'button';
      head.disabled = !rows.length;
      head.addEventListener('click', function () { pausedOpen = !pausedOpen; sec.classList.toggle('is-open', pausedOpen); });
    }
    return sec;
  }

  Dash.panel({
    id: 'work-queued', page: 'work', title: '排着的', order: 30, needs: ['queue'],
    render: function (el, d) {
      var q = d.queue;
      var top = Dash.statusLine('ok');
      top.body.append(q.stage || '', q.priority ? '，' + q.priority : '');
      el.append(top, h('div.qu-board', COLS.map(function (c) { return qColumn(q, c[0], c[1]); })));
    }
  });

  /* ---------- ④ 临时工作树：只用于统一编译、快检和视觉样板，开在指定目录下，用完 remove（WF-RULE §13） ---------- */
  Dash.panel({
    id: 'work-trees', page: 'work', order: 40, card: false, needs: ['live'],
    render: function (el, d) {
      var w = d.live.worktrees;
      var n = w && w.items ? w.items.length : 0;
      var flagged = n ? w.items.filter(function (x) { return x.flags.length; }).length : 0;
      el.appendChild(h('div' + (Dash.isStale('live') ? '.stack.is-stale' : ''),
        Dash.card('临时工作树（' + n + ' 个' + (flagged ? '，' + flagged + ' 个要看' : '') + '）', Dash.worktrees(d.live), w && w.root ? '指定目录 ' + w.root : '')));
    }
  });
})();
