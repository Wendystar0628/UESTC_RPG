/* 模型页：谁在用什么模型干什么。
   块（面板 id）：models-table 会话与子代理 · models-usage 近 24 小时 · models-rules 分工对照 · models-tree 派工树。
   数据只有 models（采集侧 C-239）；Codex 侧不读也不显示 token 用量（契约 models.agents[].tokens 只有 Claude 侧有）。 */
(function () {
  var h = Dash.h;
  var RECENT = 30 * 60 * 1000;
  var filt = { side: 'all', model: 'all' };   // 会话与子代理表的筛选，只在本页内有效
  var usageMode = 'count';                     // 近 24 小时的图：count 个数 / minutes 时长

  function seconds(a) {
    var s = Dash.parseTime(a.startedAt), e = a.endedAt ? Dash.parseTime(a.endedAt) : Date.now();
    return isNaN(s) || isNaN(e) ? NaN : Math.max(0, (e - s) / 1000);
  }
  function dur(sec) {
    if (isNaN(sec)) return '—';
    var m = Math.round(sec / 60);
    if (m < 1) return '不到 1 分钟';
    return m < 60 ? m + ' 分钟' : Math.floor(m / 60) + ' 小时 ' + (m % 60) + ' 分钟';
  }
  function sideName(s) { return s === 'codex' ? 'Codex' : 'Claude'; }
  function modelMark(m) { return Dash.mark('model', Dash.modelKey(m), Dash.modelName(m) || '—'); }
  function rowLink(id) {
    return id ? h('a.mono', { href: '#work/work-history', on: { click: function () { if (Dash.selected('row') !== id) Dash.select('row', id); } } }, id) : h('span.muted', '无行号');
  }
  function tokenCell(x) { return x.side === 'claude' ? (x.tokens == null ? '—' : Dash.num(x.tokens)) : h('span.muted', '不读'); }

  /* ---------- 会话与子代理：在跑 / 最近 30 分钟交回 / 更早，可按侧与型号筛 ---------- */
  function sessionCard(s) {
    return h('div.ms-card.is-' + s.side,
      h('div.ms-head', Dash.mark('side', s.side, s.name || sideName(s.side)), modelMark(s.model), s.effort ? Dash.chip(s.effort, 'muted') : null),
      h('div.ms-line', s.state === 'running' ? '在跑 · ' + Dash.at(s.since) + ' 起' : '空闲 · 上次 ' + Dash.at(s.since)),
      h('div.ms-line', s.side === 'claude' ? 'token ' + (s.tokens == null ? '—' : Dash.num(s.tokens)) : 'Codex 侧不读 token 用量'));
  }

  var COLS = [
    { label: '子代理', cell: function (x) { return h('span.ma-name', h('i.ma-dot.is-' + (x.state === 'running' ? 'run' : 'done')), Dash.mark('side', x.side), h('span', x.name || '—')); } },
    { label: '型号', cell: function (x) { return modelMark(x.model); } },
    { label: '推理', cell: function (x) { return x.effort || h('span.ma-warn', '没传'); } },
    { label: '在做', cell: function (x) { return rowLink(x.row); } },
    { label: '开始', num: true, cell: function (x) { return Dash.at(x.startedAt); } },
    { label: '时长', num: true, cell: function (x) { return dur(seconds(x)) + (x.state === 'running' ? '（在跑）' : ''); } },
    { label: '交回', num: true, cell: function (x) { return x.endedAt ? Dash.at(x.endedAt) : '—'; } },
    { label: 'token', num: true, cell: tokenCell }
  ];

  Dash.panel({
    id: 'models-table', page: 'models', title: '会话与子代理', note: '两个指挥用的型号，和它们派出的子代理：在跑的、最近 30 分钟交回的、更早的分开放', order: 10, needs: ['models'],
    render: function (el, d) {
      var m = d.models, all = m.agents || [];
      el.appendChild(h('div.ms-cards', (m.sessions || []).map(sessionCard)));
      var keys = [];
      all.forEach(function (a) { var k = Dash.modelKey(a.model); if (keys.indexOf(k) < 0) keys.push(k); });
      var order = Object.keys(Dash.palette.model);
      keys.sort(function (a, b) { return order.indexOf(a) - order.indexOf(b); });
      if (filt.model !== 'all' && keys.indexOf(filt.model) < 0) filt.model = 'all';
      el.appendChild(h('div.ma-tools',
        Dash.segment([{ id: 'all', label: '两侧' }, { id: 'claude', label: 'Claude' }, { id: 'codex', label: 'Codex' }], filt.side,
          function (v) { filt.side = v; Dash.redraw('models-table'); }),
        Dash.segment([{ id: 'all', label: '全部型号' }].concat(keys.map(function (k) { return { id: k, label: Dash.label('model', k) }; })), filt.model,
          function (v) { filt.model = v; Dash.redraw('models-table'); })));
      var list = all.filter(function (a) { return (filt.side === 'all' || a.side === filt.side) && (filt.model === 'all' || Dash.modelKey(a.model) === filt.model); })
        .sort(function (a, b) { return Dash.parseTime(b.startedAt) - Dash.parseTime(a.startedAt); });
      var now = Date.now();
      var run = list.filter(function (a) { return a.state === 'running'; });
      var recent = list.filter(function (a) { return a.state !== 'running' && a.endedAt && now - Dash.parseTime(a.endedAt) <= RECENT; });
      var older = list.filter(function (a) { return run.indexOf(a) < 0 && recent.indexOf(a) < 0; });
      el.appendChild(Dash.status(Dash.num(run.length) + ' 个在跑，最近 30 分钟交回 ' + Dash.num(recent.length) + ' 个，更早 ' + Dash.num(older.length) + ' 个' +
        (list.length < all.length ? '（筛出 ' + Dash.num(list.length) + ' / ' + Dash.num(all.length) + '）' : ''), run.length ? 'ok' : 'muted'));
      [[run, '在跑', true], [recent, '最近 30 分钟交回', true], [older, '更早', false]].forEach(function (g) {
        if (!g[0].length) return;
        el.appendChild(Dash.fold(g[1] + ' ' + Dash.num(g[0].length) + ' 个', Dash.table(COLS, g[0]), g[2]));
      });
      if (!list.length) el.appendChild(Dash.empty('没有符合筛选的子代理', '', true));
    }
  });

  /* ---------- 近 24 小时：各行用了哪些型号、多少个、多久 ---------- */
  Dash.panel({
    id: 'models-usage', page: 'models', title: '近 24 小时', note: '每一行派了哪些型号的子代理、各几个、跑了多久（与 token 无关）', span: 7, order: 20, needs: ['models'],
    render: function (el, d) {
      if (!('usage24' in d.models)) { el.appendChild(Dash.empty('等这一项数据', '来自 models.usage24，等后端 C-239 写出', true)); return; }
      var use = d.models.usage24 || [];
      var rows = {}, keys = [], tot = {};
      use.forEach(function (u) {
        var r = u.row || '无行号', k = Dash.modelKey(u.model);
        var v = usageMode === 'count' ? u.count : u.seconds / 60;
        rows[r] = (rows[r] || 0) + v;
        if (keys.indexOf(k) < 0) keys.push(k);
        tot[k] = tot[k] || { n: 0, s: 0 };
        tot[k].n += u.count; tot[k].s += u.seconds;
      });
      var cats = Object.keys(rows).sort(function (a, b) { return rows[b] - rows[a] || (a < b ? -1 : 1); });
      var order = Object.keys(Dash.palette.model);
      keys.sort(function (a, b) { return order.indexOf(a) - order.indexOf(b); });
      el.appendChild(h('div.mu-head',
        h('div.mu-tot', keys.map(function (k) {
          return h('span.mu-tot-item', Dash.mark('model', k), h('b', Dash.num(tot[k].n) + ' 个'), h('span.muted', dur(tot[k].s)));
        })),
        Dash.segment([{ id: 'count', label: '个数' }, { id: 'minutes', label: '时长' }], usageMode, function (v) { usageMode = v; Dash.redraw('models-usage'); })));
      el.appendChild(Dash.charts.bars({
        categories: cats, stack: true, horizontal: true,
        series: keys.map(function (k) {
          return { name: Dash.label('model', k), tone: ['model', k], values: cats.map(function (r) {
            return Math.round(use.filter(function (u) { return (u.row || '无行号') === r && Dash.modelKey(u.model) === k; })
              .reduce(function (n, u) { return n + (usageMode === 'count' ? u.count : u.seconds / 60); }, 0));
          }) };
        })
      }, { unit: usageMode === 'count' ? '个' : '分钟', labelWidth: 80, empty: '近 24 小时没有子代理' }));
    }
  });

  /* ---------- 分工对照：WF-MODEL 的分工、实际用量与偏离 ---------- */
  Dash.panel({
    id: 'models-rules', page: 'models', title: '分工对照', note: 'WF-MODEL 规定每个型号做什么，近 24 小时实际派了几个，哪里偏离了', span: 5, order: 30, needs: ['models'],
    render: function (el, d) {
      var m = d.models, byId = {}, used = {};
      (m.agents || []).forEach(function (a) { byId[a.id] = a; });
      (m.usage24 || []).forEach(function (u) { var k = Dash.modelKey(u.model); used[k] = (used[k] || 0) + u.count; });
      var dev = (m.deviations || []).slice(), rules = m.rules || [];
      el.appendChild(Dash.status(dev.length ? Dash.num(dev.length) + ' 处偏离分工' : '实际使用与分工一致', dev.length ? 'warn' : 'ok'));
      function devRow(x) {
        var a = byId[x.agent];
        return h('div.mr-dev', h('span.mr-dev-who', a ? [Dash.mark('side', a.side), ' ', a.name, a.row ? ' · ' + a.row : ''] : h('span.mono', x.agent)), h('span.mr-dev-note', x.note));
      }
      var box = h('div.mr-list');
      rules.forEach(function (r) {
        var k = Dash.modelKey(r.model);
        var mine = dev.filter(function (x) { return String(x.rule || '').indexOf(r.model) >= 0 || String(x.rule || '').indexOf(Dash.label('model', k)) >= 0; });
        mine.forEach(function (x) { dev.splice(dev.indexOf(x), 1); });
        box.appendChild(h('div.mr-rule' + (mine.length ? '.is-off' : ''),
          h('div.mr-line', Dash.mark('model', k), h('span.mr-use', r.use), h('span.mr-n', used[k] ? '近 24 小时 ' + Dash.num(used[k]) + ' 个' : '近 24 小时没用')),
          mine.map(devRow)));
      });
      el.appendChild(box);
      if (dev.length) {
        el.appendChild(h('h3.sub-title', '其他规则的偏离'));
        el.appendChild(h('div.mr-list', dev.map(function (x) { return h('div.mr-rule.is-off', h('div.mr-line', h('span.mr-use', x.rule)), devRow(x)); })));
      }
      if (!rules.length) el.appendChild(Dash.empty('没有读到分工规则', '来自 models.rules（采集侧现读 WF-MODEL）', true));
    }
  });

  /* ---------- 派工树：谁派了谁 ---------- */
  var DONE_SHOWN = 3;

  Dash.panel({
    id: 'models-tree', page: 'models', title: '派工树', note: '谁派了谁：会话 → 子代理 → 子代理再派的子代理；实心点在跑，空心已交回', order: 40, needs: ['models'],
    render: function (el, d) {
      var m = d.models, nodes = {}, kids = {}, hasParent = {};
      (m.sessions || []).forEach(function (s) { nodes[s.id] = Object.assign({ main: true }, s); });
      (m.agents || []).forEach(function (a) { nodes[a.id] = a; });
      (m.spawnTree || []).forEach(function (e) {
        if (!nodes[e.child]) return;
        (kids[e.parent] = kids[e.parent] || []).push(e.child);
        hasParent[e.child] = 1;
      });
      function line(n) {
        return h('div.mt-line' + (n.main ? '.is-main' : ''),
          h('i.ma-dot.is-' + (n.state === 'running' ? 'run' : 'done')),
          n.main ? Dash.mark('side', n.side, n.name || sideName(n.side)) : h('span.mt-name', n.name || '—'),
          modelMark(n.model), n.effort ? h('span.mt-effort', n.effort) : null,
          n.main ? null : rowLink(n.row),
          h('span.mt-when', n.main ? (n.state === 'running' ? '在跑 · ' + Dash.at(n.since) + ' 起' : '空闲') : Dash.at(n.startedAt) + ' 起 · ' + dur(seconds(n)) + (n.state === 'running' ? '' : ' · 已交回')));
      }
      function branch(id, depth, seen) {
        var n = nodes[id] || { id: id, name: id.slice(0, 8), side: 'claude' };
        var list = (kids[id] || []).filter(function (c) { return !seen[c]; }).map(function (c) { return nodes[c]; })
          .sort(function (a, b) { return ((b.state === 'running') - (a.state === 'running')) || (Dash.parseTime(b.startedAt) - Dash.parseTime(a.startedAt)); });
        seen[id] = 1;
        var run = list.filter(function (c) { return c.state === 'running'; }), done = list.filter(function (c) { return c.state !== 'running'; });
        var sub = run.concat(done.slice(0, DONE_SHOWN)).map(function (c) { return branch(c.id, depth + 1, seen); });
        var more = done.length > DONE_SHOWN ? Dash.fold('另有 ' + Dash.num(done.length - DONE_SHOWN) + ' 个已交回', done.slice(DONE_SHOWN).map(function (c) { return branch(c.id, depth + 1, seen); })) : null;
        return h('div.mt-node.is-' + (n.side || 'claude'), line(n),
          list.length ? h('div.mt-kids', sub, more) : (n.main ? h('div.mt-none', '没有派过子代理') : null));
      }
      var roots = Object.keys(nodes).filter(function (id) { return nodes[id].main; })
        .concat(Object.keys(kids).filter(function (id) { return !hasParent[id] && !(nodes[id] && nodes[id].main); }));
      var seen = {};
      el.appendChild(h('div.mt-trees', roots.map(function (id) { return branch(id, 0, seen); })));
      var orphan = (m.agents || []).filter(function (a) { return !seen[a.id]; });
      if (orphan.length) el.appendChild(Dash.fold('没挂进派工树的子代理 ' + Dash.num(orphan.length) + ' 个', Dash.table(COLS, orphan)));
    }
  });
})();
