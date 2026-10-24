/* 团队页：此刻谁在干活，两边沟通、审计、派单顺不顺。
   块（面板 id）：team-leads 两位指挥 · team-agents 实时子代理 · team-letters 来信往来 · team-audit 收口审计
                 · team-dispatch 派单合规 · team-idle 有活却空闲。
   数据：team（指挥、子代理；行的阶段、前置等自带）、collab（来信、审计、派单、空闲）；页面只画，不重算。 */
(function () {
  'use strict';

  var h = Dash.h;

  var AVATAR = { claude: 'Cl', codex: 'Cx' };
  var SIDES = [{ key: 'claude', name: 'Claude' }, { key: 'codex', name: 'Codex' }];
  var MARKS = ['需定', '待审', '知会', '插队'];
  var NEED_REPLY = { '需定': true, '待审': true };
  var STATE_TEXT = { running: '在跑', stalled: '可能卡住', done: '已交回', unknown: '未确认' };

  var ruleFilter = null;      // 派单规则条选中的 R 键；改它之后 Dash.redraw 重画本块

  /* ---------- 小件 ---------- */

  function dur(m) { return m == null ? '未确认' : m < 1 ? '不到 1 分钟' : Dash.minutes(m); }

  /* 会随时间变长的文字：外壳只在数据推送时重画，这里每 30 秒原地刷新一次 */
  var TIME_FMT = {
    run: function (iso) { return '这一轮' + Dash.since(iso); },
    idle: function (iso) { return Dash.since(iso); },
    ran: function (iso) { return '运行' + Dash.since(iso); }
  };
  function timeText(kind, iso, cls) {
    return h('span' + (cls ? '.' + cls : ''), { dataset: { tmKind: kind, tmSince: iso || '' } }, iso ? TIME_FMT[kind](iso) : '未确认');
  }
  setInterval(function () {
    Array.prototype.forEach.call(document.querySelectorAll('[data-tm-since]'), function (n) {
      if (n.dataset.tmSince && TIME_FMT[n.dataset.tmKind]) n.textContent = TIME_FMT[n.dataset.tmKind](n.dataset.tmSince);
    });
  }, 30000);

  function lines(items, max, label) {
    if (items.length <= max) return items;
    return items.slice(0, max).concat([Dash.fold('还有 ' + (items.length - max) + ' ' + label, items.slice(max))]);
  }

  function modelMark(m) { return Dash.mark('model', Dash.modelKey(m), Dash.modelName(m)); }
  function effortChip(e) { return h('span.chip.t-muted', '推理档 ' + (e == null ? '未确认' : Dash.effortName(e))); }
  function sideMark(side) { return Dash.mark('side', Dash.sideKey(side), sideLabel(side)); }

  function sideLabel(side) {
    var k = Dash.sideKey(side);
    return k === 'claude' ? 'Claude' : k === 'codex' ? 'Codex' : String(side || '未定');
  }

  function kv(pairs) {
    return h('dl.tm-kv', pairs.filter(Boolean).map(function (p) { return [h('dt', p[0]), h('dd', p[1])]; }));
  }

  /* ---------- 行与抽屉 ---------- */

  /* 行对象（leads[].rows、agents[] 带的负责行、audit.missing）自带阶段、谁、前置、被谁等着；stage 为 null 表示队列里已经没有这一行 */
  function rowPairs(r) {
    if (r.stage == null) return [['此刻在哪段', '队列里已经没有这一行（已推送，或不是队列行）']];
    var pairs = [['此刻在哪段', [Dash.mark('stage', r.stage), ' 本段' + Dash.since(r.since)]], ['谁签', r.who || '未定']];
    if (r.jump) pairs.push(['类别', Dash.mark('mark', '插队')]);
    if ((r.deps || []).length) pairs.push(['前置', r.deps.join('、')]);
    if ((r.waitedBy || []).length) pairs.push(['被谁等着', r.waitedBy.join('、')]);
    return pairs;
  }

  function goWork() { Dash.drawer.close(); Dash.go('work', null, 'work-board'); }

  function rowDrawer(r, extra) {
    Dash.drawer({
      title: r.id + ' ' + (r.title || ''),
      body: h('div.tm-drawer', kv([].concat(extra || [], rowPairs(r))),
        h('button.btn.tm-go', { type: 'button', on: { click: goWork } }, '到施工页看看板'))
    });
  }

  function rowButton(r, extra) {
    return h('button.tm-row', { type: 'button', on: { click: function () { rowDrawer(r, extra); } } },
      h('span.id', r.id), h('span.tm-row-title', r.title || ''));
  }

  function emptyLine(text) { return h('div.tm-none', text); }

  /* ---------- team-leads ---------- */

  function leadCard(l) {
    var side = Dash.sideKey(l.who);
    var on = l.running == null ? null : !!l.running;
    var rows = l.rows || [];
    var rowEls = rows.map(function (r) { return rowButton(r, [['所属指挥', l.who]]); });
    return h('div.tm-lead.t-' + side,
      h('div.tm-lead-head',
        h('div.tm-avatar', { 'aria-hidden': 'true' }, AVATAR[side] || String(l.who || '?').charAt(0).toUpperCase()),
        h('div.tm-lead-id',
          h('div.tm-lead-name', l.who),
          h('div.tm-tags', modelMark(l.model), effortChip(l.effort)))),
      h('div.tm-lead-state',
        h('span.tm-lamp' + (on ? '.is-on' : on === false ? '.is-off' : '.is-unknown')),
        h('b', on == null ? '未确认' : on ? '在跑' : '空闲'),
        on == null ? null : timeText(on ? 'run' : 'idle', l.since, 'muted')),
      h('div.tm-lead-rows',
        rows.length ? lines(rowEls, 3, '行') : emptyLine('眼下没有在做的行')),
      h('div.tm-lead-agents', h('b', Dash.num(l.agents)), ' 个子代理在跑'));
  }

  Dash.panel({
    id: 'team-leads', page: 'team', title: '两位指挥', note: '各用什么模型、在做哪些行、手下几个子代理在跑',
    caliber: '运行状态来自会话记录；在做的行来自施工队列；手下在跑的个数是这位指挥名下状态为在跑或可能卡住的子代理',
    span: 12, order: 10, needs: ['team'],
    render: function (el, d) {
      if (!d.team) { el.appendChild(Dash.empty('还没有团队数据', '', true)); return; }
      el.appendChild(h('div.tm-leads', (d.team.leads || []).map(leadCard)));
    }
  });

  /* ---------- team-agents ---------- */

  function agentItem(a, d) {
    var stalled = a.state === 'stalled';
    var timing = a.state === 'done' ? h('span.muted', a.runMin == null ? '用时未确认' : '用时 ' + dur(a.runMin))
      : timeText('ran', a.started);
    return h('button.tm-agent' + (stalled ? '.is-stalled' : ''), { type: 'button', on: { click: function () { agentDrawer(a, d); } } },
      h('span.tm-adot', { style: '--tone: var(' + Dash.colorVar('agent', a.state) + ')', title: STATE_TEXT[a.state] || a.state }),
      h('div.tm-agent-main',
        h('div.tm-agent-top', h('span.tm-agent-name', a.name), modelMark(a.model), effortChip(a.effort)),
        h('div.tm-agent-row', a.row ? [h('span.id', a.row), ' ' + (a.rowTitle || '')] : h('span.muted', '没有对应的行'))),
      h('div.tm-agent-time', timing,
        stalled ? h('span.tm-quiet', a.quietMin == null ? '可能卡住' : Dash.num(Math.round(a.quietMin)) + ' 分钟没动静') : null,
        a.state === 'unknown' ? h('span.muted', '状态未确认') : null));
  }

  function agentDrawer(a, d) {
    var lead = ((d.team && d.team.leads) || []).filter(function (l) { return Dash.sideKey(l.who) === Dash.sideKey(a.side); })[0];
    var pairs = [
      ['状态', STATE_TEXT[a.state] || a.state],
      ['模型', [modelMark(a.model), ' ', effortChip(a.effort)]],
      ['开始', a.started ? Dash.at(a.started) : '未确认'],
      ['交回', a.state === 'done' ? (a.ended ? Dash.at(a.ended) : '未确认') : '还没有'],
      ['所属指挥', lead ? lead.who + ' · ' + Dash.modelName(lead.model) : sideLabel(a.side)],
      ['负责行', a.row ? a.row + ' ' + (a.rowTitle || '') : '没有对应的行']
    ];
    if (a.row) {
      rowPairs(a).forEach(function (p) { pairs.push([p[0] === '此刻在哪段' ? '负责行此刻在哪段' : p[0], p[1]]); });
    }
    Dash.drawer({
      title: a.name, sub: sideLabel(a.side) + ' · ' + Dash.modelName(a.model),
      body: h('div.tm-drawer', kv(pairs),
        a.row ? h('button.btn.tm-go', { type: 'button', on: { click: goWork } }, '到施工页看看板') : null)
    });
  }

  /* agents 已按卡住优先、开始时刻、交回时刻排好；个数读 sides。这里只把在跑与已交回分到两处画 */
  function agentColumn(s, sd, agents, d) {
    var live = agents.filter(function (a) { return a.state !== 'done'; });
    var done = agents.filter(function (a) { return a.state === 'done'; });
    var run = (sd.running || 0) + (sd.stalled || 0);
    var head = run || sd.unknown
      ? '在跑 ' + run + (sd.stalled ? ' · 其中可能卡住 ' + sd.stalled : '') + (sd.unknown ? ' · 未确认 ' + sd.unknown : '')
      : '没有在跑的';
    return h('div.col.tm-col.t-' + s.key,
      h('div.tm-col-head', h('span.tm-col-name', s.name), h('span.muted', head)),
      live.length ? live.map(function (a) { return agentItem(a, d); }) : emptyLine('此刻没有在跑的子代理'),
      done.length ? Dash.fold('近一小时交回 ' + Dash.num(sd.doneN) + ' 个', done.map(function (a) { return agentItem(a, d); })) : null);
  }

  Dash.panel({
    id: 'team-agents', page: 'team', title: '实时子代理', note: '此刻谁在做哪一行；可能卡住的排最前，近一小时交回的折在下面',
    caliber: '状态按子代理的会话记录判断，一段时间没有新动静标为可能卡住；在跑 = 在跑 + 可能卡住，未确认单列；交回的只留近一小时',
    span: 12, order: 20, needs: ['team'],
    render: function (el, d) {
      if (!d.team) { el.appendChild(Dash.empty('还没有团队数据', '', true)); return; }
      var agents = d.team.agents || [];
      var sides = d.team.sides || [];
      el.appendChild(h('div.cols', SIDES.map(function (s) {
        var sd = sides.filter(function (x) { return Dash.sideKey(x.side) === s.key; })[0] || {};
        return agentColumn(s, sd, agents.filter(function (a) { return Dash.sideKey(a.side) === s.key; }), d);
      })));
    }
  });

  /* ---------- team-letters ---------- */

  var DIRS = [{ from: 'claude', to: 'codex', text: 'Claude → Codex' }, { from: 'codex', to: 'claude', text: 'Codex → Claude' }];

  function openLetter(mark, x) {
    return h('div.tm-letter',
      Dash.mark('mark', mark),
      h('span.tm-letter-first', x.first || '（没有第一行）'),
      h('span.tm-letter-wait', '已等 ' + dur(x.minutes) + ' · ' + Dash.at(x.at)));
  }

  function sameCell(x, dir, mark) { return Dash.sideKey(x.from) === dir.from && Dash.sideKey(x.to) === dir.to && x.mark === mark; }

  function letterDrawer(dir, mark, cell, open) {
    var mine = open.filter(function (x) { return sameCell(x, dir, mark); });
    var body = [kv([
      ['近 7 天', cell && cell.n != null ? Dash.num(cell.n) + ' 封' : '未确认'],
      NEED_REPLY[mark] ? ['响应时间中位数', cell && cell.p50 != null ? dur(cell.p50) : cell && cell.n != null ? '没有可配对的回信' : '未确认'] : null
    ])];
    if (!NEED_REPLY[mark]) body.push(emptyLine('这一类不要求回信，不统计响应时间'));
    else if (!cell || cell.openN == null) body.push(emptyLine('这一格没回的信数未确认'));
    else if (!mine.length) body.push(emptyLine('这一格没有没回的信'));
    else body.push(h('div.tm-sub', '没回的 ' + mine.length + ' 封，最久的在前'), mine.map(function (x) { return openLetter(mark, x); }));
    Dash.drawer({ title: dir.text + ' · ' + mark, body: h('div.tm-drawer', body) });
  }

  Dash.panel({
    id: 'team-letters', page: 'team', title: '来信往来', note: '近 7 天两个方向、四类标记的来信封数；需定与待审写回信的响应时间',
    caliber: '回信按同一行号配对，没写行号的不算响应时间', window: '7d',
    span: 7, order: 30, needs: ['collab'],
    render: function (el, d) {
      var L = d.collab.letters;
      if (!L) { el.appendChild(Dash.empty('还没有来信数据', '', true)); return; }
      var cells = [];
      var src = L.cells || [];
      DIRS.forEach(function (dir, r) {
        MARKS.forEach(function (mark, c) {
          var x = src.filter(function (y) { return Dash.sideKey(y.from) === dir.from && Dash.sideKey(y.to) === dir.to && y.mark === mark; })[0];
          var parts = [];
          if (!x || x.n == null) parts.push('未确认');
          if (NEED_REPLY[mark] && x && x.p50 != null) parts.push('中位 ' + dur(x.p50));
          if (x && x.openN) parts.push('未回 ' + x.openN);
          cells.push({
            r: r, c: c, value: x ? x.n : null,
            text: parts.length ? parts.join(' · ') : undefined,
            _dir: dir, _mark: mark, _cell: x
          });
        });
      });
      el.appendChild(Dash.charts.heat(DIRS.map(function (x) { return x.text; }), MARKS, cells, {
        height: 150, label: '近 7 天来信封数，按方向和标记',
        onClick: function (p) {
          var c = cells[p.index];
          if (c) letterDrawer(c._dir, c._mark, c._cell, L.open || []);
        }
      }));
      if (L.unpaired) el.appendChild(h('div.note-line', '另有 ' + Dash.num(L.unpaired) + ' 封没写行号，无法配对，不算响应时间'));
      var open = L.open || [];
      el.appendChild(h('div.tm-sub', L.openN == null ? '没回的信数未确认'
        : L.openN ? '没回的 ' + Dash.num(L.openN) + ' 封，最久没回的在前；点上面带“未回”的格子看全部' : '需定与待审都有回信'));
      if (open.length) {
        el.appendChild(h('div.tm-list', lines(open.map(function (x) {
          return h('div.tm-letter', sideMark(x.from), h('span.muted', '→'), sideMark(x.to), openLetter(x.mark, x));
        }), 3, '封')));
      }
    }
  });

  /* ---------- team-audit ---------- */

  Dash.panel({
    id: 'team-audit', page: 'team', title: '收口审计', note: '近 7 天推上去的行，有没有被对方审过',
    caliber: '按推送所含的行号认，一行审过一次就算', window: '7d',
    span: 5, order: 40, needs: ['collab'],
    render: function (el, d) {
      var A = d.collab.audit;
      if (!A) { el.appendChild(Dash.empty('还没有审计数据', '', true)); return; }
      if (A.rows == null) { el.appendChild(Dash.empty('近 7 天推了几行未确认', '', true)); return; }
      if (!A.rows) { el.appendChild(Dash.empty('近 7 天没有推送的行', '', true)); return; }
      var missing = A.missing || [];
      el.appendChild(h('div.tm-audit',
        h('div.tm-ring', Dash.charts.ring([
          { label: '已审', value: A.audited, tone: ['level', 'ok'] },
          { label: '未审', value: A.unaudited, tone: ['level', 'warn'] }
        ], { height: 124, unit: '行', center: Dash.pct(A.rate, 1), label: '收口审计覆盖率' })),
        h('div.tm-stats',
          h('div.tm-stat', h('b', Dash.num(A.audited) + ' / ' + Dash.num(A.rows)), '行已审'),
          h('div.tm-stat', h('b', Dash.num(A.found)), '条查实'),
          h('div.tm-stat', h('b', Dash.num(A.newRows)), '条转成了队列行'))));
      el.appendChild(h('div.tm-sub', A.unaudited == null ? '没审的行数未确认' : A.unaudited ? '没审的 ' + Dash.num(A.unaudited) + ' 行' : '推上去的行都审过了'));
      if (missing.length) {
        el.appendChild(h('div.tm-list', lines(missing.map(function (r) {
          return rowButton(r, [['推送时刻', Dash.at(r.at)], ['审计', '近 7 天没有对方侧的审计记录']]);
        }), 6, '行')));
      }
    }
  });

  /* ---------- team-dispatch ---------- */

  function violation(b) {
    return h('div.tm-viol',
      h('span.tm-viol-name', b.name), sideMark(b.side), modelMark(b.model), effortChip(b.effort),
      (b.rules || []).map(function (k) { return h('span.chip.t-risk', k); }));
  }

  function notice(x) {
    return h('div.tm-viol', h('span.tm-viol-name', x.name), sideMark(x.side), modelMark(x.model), effortChip(x.effort),
      h('span.muted', x.text || ''));
  }

  Dash.panel({
    id: 'team-dispatch', page: 'team', title: '派单合规', note: '近 7 天开工的子代理，按 WF-MODEL 里能机读的规则逐条对照',
    caliber: '只判能机读的规则；Opus 是否在做观感未定的活由主会话看', window: '7d',
    span: 7, order: 50, needs: ['collab'],
    render: function (el, d) {
      var D = d.collab.dispatch;
      if (!D) { el.appendChild(Dash.empty('还没有派单数据', '', true)); return; }
      if (D.n == null) { el.appendChild(Dash.empty('近 7 天派了几个子代理未确认', '', true)); return; }
      if (!D.n) { el.appendChild(Dash.empty('近 7 天没有派过子代理', '', true)); return; }
      var rules = D.rules || [];
      var bad = D.bad || [];
      el.appendChild(h('div.tm-sum', '近 7 天派 ', h('b', Dash.num(D.n)), ' 个，合规 ', h('b', Dash.num(D.ok)), ' 个',
        h('span.muted', '（' + Dash.pct(D.rate, 1) + '）')));
      if (ruleFilter && !rules.some(function (r) { return r.key === ruleFilter; })) ruleFilter = null;
      if (rules.length) {
        el.appendChild(Dash.charts.bars(rules.map(function (r) {
          return { key: r.key, label: r.key + ' ' + r.label, value: r.bad, tone: r.bad ? ['level', 'bad'] : r.bad == null || D.ok == null ? ['side', 'muted'] : ['level', 'ok'] };
        }), {
          label: '各条派单规则的违规个数', unit: '个违规', labelWidth: 200, textWidth: 90,
          onClick: function (p) { ruleFilter = ruleFilter === p.key ? null : p.key; Dash.redraw('team-dispatch'); }
        }));
      }
      var rows = ruleFilter ? bad.filter(function (b) { return (b.rules || []).indexOf(ruleFilter) >= 0; }) : bad;
      if (ruleFilter) {
        el.appendChild(h('div.tm-filter', h('span', '只看 ' + ruleFilter),
          h('button.btn', { type: 'button', on: { click: function () { ruleFilter = null; Dash.redraw('team-dispatch'); } } }, '清除')));
      }
      el.appendChild(h('div.tm-viols', rows.length ? rows.map(violation)
        : D.bad == null ? h('div.tm-ok', Dash.status('违规名单未确认', 'muted'))
        : D.ok == null ? h('div.tm-ok', Dash.status('判得出的里' + (ruleFilter ? '没有违反 ' + ruleFilter + ' 的' : '没有违规') + '，但有派单字段没记、没判，不能当作都合规', 'warn'))
        : h('div.tm-ok', Dash.status(ruleFilter ? '没有违反 ' + ruleFilter + ' 的' : '没有违规', 'ok'))));
      var hints = D.hints || [];
      var opus = D.opus || [];
      if (hints.length) el.appendChild(Dash.fold('提示 ' + hints.length + ' 条：高档，确认是不是诊断类', hints.map(notice)));
      if (opus.length) {
        el.appendChild(Dash.fold('Opus 单 ' + opus.length + ' 个', opus.map(function (x) {
          return h('div.tm-viol', h('span.tm-viol-name', x.name), x.row ? h('span.id', x.row) : h('span.muted', '没有对应的行'), h('span.muted', Dash.at(x.at)));
        })));
      }
    }
  });

  /* ---------- team-idle ---------- */

  Dash.panel({
    id: 'team-idle', page: 'team', title: '有活却空闲', note: '近 24 小时指挥空着、队列里却有能接的就绪行，累计多少分钟',
    caliber: '指挥空闲且有能接的就绪行的时长', window: '24h',
    span: 5, order: 60, needs: ['collab'],
    render: function (el, d) {
      var idle = d.collab.idle;
      if (!idle || !idle.length) { el.appendChild(Dash.empty('还没有这项数据', '', true)); return; }
      el.appendChild(Dash.charts.bars(idle.map(function (x) {
        var g = x.longest;
        return {
          key: x.who, label: x.who, value: x.minutes, text: x.minutes == null ? '未确认' : Dash.minutes(x.minutes), tone: ['side', Dash.sideKey(x.who)],
          note: g ? '最长一段 ' + Dash.at(g.from) + ' – ' + Dash.at(g.to) + '，当时就绪 ' + ((g.rows || []).length ? g.rows.join('、') : '行号未确认') : null
        };
      }), { label: '近 24 小时有活却空闲的分钟数', labelWidth: 70, textWidth: 100 }));
      if (idle.some(function (x) { return x.minutes == null; })) el.appendChild(h('div.note-line', '有的指挥这段时长未确认'));
      else if (!idle.some(function (x) { return x.minutes; })) el.appendChild(h('div.note-line', '近 24 小时没有出现有活却空着的时段'));
    }
  });
})();
