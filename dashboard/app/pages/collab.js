/* 协作页：两边在说什么、审了什么。
   块（面板 id）：collab-letters 来信 · collab-audits 收口审计 · collab-files 任务单、契约申请、调研与交接材料。
   选中的来信放在 Dash.select('letter')；审计里的行号点了选中那一行并去施工页。 */
(function () {
  var h = Dash.h;

  function tagKey(t) { return t in Dash.palette.tag ? t : '无'; }
  function byTimeDesc(a, b) { return Dash.parseTime(b.at) - Dash.parseTime(a.at); }
  function rowLinks(list) {
    if (!list || !list.length) return '—';
    return h('span', list.map(function (r, i) {
      return [i ? '、' : '', h('a.mono', { href: '#work/work-history', on: { click: function () { if (Dash.selected('row') !== r) Dash.select('row', r); } } }, r)];
    }));
  }

  /* ---------- 来信：两条道两个方向，点是一封信 ---------- */
  function letterLine(x, pick) {
    return h('button.lt-row' + (x.id === pick ? '.is-selected' : '') + (x.delivered ? '' : '.is-pending'),
      { type: 'button', on: { click: function () { Dash.select('letter', x.id); } } },
      Dash.mark('tag', tagKey(x.tag)), h('span.lt-dir', x.from === 'codex' ? 'Codex → Claude' : 'Claude → Codex'),
      h('span.lt-title', x.title), h('span.lt-at', Dash.at(x.at)), x.delivered ? null : h('span.lt-flag', '未送达'));
  }

  Dash.panel({
    id: 'collab-letters', page: 'collab', title: '来信', note: '两个指挥 7 天里给对方写了什么：上道 Claude 写给 Codex，下道 Codex 写给 Claude；点按标签着色，带黑圈的还没送达。点一封看全文', order: 10,
    needs: ['letters'], watch: ['letter'],
    render: function (el, d) {
      var items = (d.letters.items || []).slice().sort(byTimeDesc);
      var pending = items.filter(function (x) { return !x.delivered; });
      var count = {};
      items.forEach(function (x) { var k = tagKey(x.tag); count[k] = (count[k] || 0) + 1; });
      var toCodex = items.filter(function (x) { return x.from === 'claude'; }).length;
      el.appendChild(Dash.status(Dash.num(items.length) + ' 封：Claude 写给 Codex ' + Dash.num(toCodex) + ' 封，Codex 写给 Claude ' + Dash.num(items.length - toCodex) + ' 封' +
        (pending.length ? '；' + Dash.num(pending.length) + ' 封还没送达' : '；都已送达'), pending.length ? 'warn' : 'ok'));
      el.appendChild(h('div.lt-counts', Object.keys(Dash.palette.tag).filter(function (k) { return count[k]; }).map(function (k) {
        return h('span.lt-count', Dash.mark('tag', k), h('b', Dash.num(count[k])));
      })));
      var pick = Dash.selected('letter');
      el.appendChild(Dash.charts.timeline({
        lanes: [{ id: 'claude', label: 'Claude → Codex' }, { id: 'codex', label: 'Codex → Claude' }],
        items: items.map(function (x) {
          return { lane: x.from, at: x.at, tone: ['tag', tagKey(x.tag)], label: x.title, title: (x.delivered ? '已送达' : '还没送达') + ' · 点开看全文', ring: !x.delivered || x.id === pick, id: x.id };
        })
      }, { rowHeight: 46, symbolSize: 13, onClick: function (it) { Dash.select('letter', it.id); } }));

      if (pending.length) {
        el.appendChild(h('div.lt-pending', h('h3.sub-title', '还没送达 ' + Dash.num(pending.length) + ' 封'), h('div.lt-list', pending.map(function (x) { return letterLine(x, pick); }))));
      }

      var letter = items.filter(function (x) { return x.id === pick; })[0];
      var shown = letter || items[0];
      if (shown) {
        el.appendChild(h('article.letter',
          h('header.letter-head', Dash.mark('tag', tagKey(shown.tag)), Dash.mark('side', shown.from, shown.from === 'codex' ? 'Codex → Claude' : 'Claude → Codex'),
            h('b.letter-title', shown.title),
            h('span.muted.small', (letter ? '' : '最近一封 · ') + Dash.at(shown.at) + (shown.delivered ? ' · 已送达' : ' · 还没送达'))),
          h('pre.letter-body', shown.body || '（没有正文）')));
      }
      el.appendChild(Dash.fold('按时间列出全部 ' + Dash.num(items.length) + ' 封', h('div.lt-list', items.map(function (x) { return letterLine(x, pick); }))));
    }
  });

  /* ---------- 收口审计 ---------- */
  var VERDICT = { ok: ['level', 'ok', '通过'], rework: ['level', 'warn', '返工'], revert: ['level', 'bad', '撤回'] };
  function verdictTone(x) { var v = VERDICT[x.verdict]; return v ? [v[0], v[1]] : ['status', 'doing']; }
  function verdictText(x) { var v = VERDICT[x.verdict]; return v ? v[2] : x.doneAt ? '没给结论' : '在审'; }

  Dash.panel({
    id: 'collab-audits', page: 'collab', title: '收口审计', note: '每批推送由另一方派只读子代理审：谁派的、用哪个模型、交回没有、查出几处补丁、转成了哪些行', order: 20, needs: ['audits'],
    render: function (el, d) {
      var items = (d.audits.items || []).slice().sort(function (a, b) { return Dash.parseTime(b.startedAt) - Dash.parseTime(a.startedAt); });
      var tally = { ok: 0, rework: 0, revert: 0, open: 0 }, found = 0;
      items.forEach(function (x) { if (x.verdict in tally) tally[x.verdict]++; else if (!x.doneAt) tally.open++; found += x.findings || 0; });
      el.appendChild(Dash.status(Dash.num(items.length) + ' 次审计：在审 ' + Dash.num(tally.open) + '、通过 ' + Dash.num(tally.ok) + '、返工 ' + Dash.num(tally.rework) + '、撤回 ' + Dash.num(tally.revert) +
        '；共查出 ' + Dash.num(found) + ' 处补丁', tally.revert || tally.rework ? 'warn' : 'ok'));
      el.appendChild(Dash.charts.timeline({
        lanes: [{ id: 'claude', label: 'Claude 派' }, { id: 'codex', label: 'Codex 派' }],
        items: items.map(function (x) {
          return { lane: x.by, at: x.startedAt, end: x.doneAt || new Date().toISOString(), tone: verdictTone(x),
            label: (x.rows || []).join('、') + ' · ' + verdictText(x), title: Dash.modelName(x.model) + (x.findings ? ' · 查出 ' + Dash.num(x.findings) + ' 处' : '') };
        })
      }, { rowHeight: 38, legend: [['level', ['ok', 'warn', 'bad']]], empty: '还没有收口审计记录' }));
      el.appendChild(Dash.table([
        { label: '审的行', cell: function (x) { return rowLinks(x.rows); } },
        { label: '谁派', cell: function (x) { return Dash.mark('side', x.by); } },
        { label: '型号', cell: function (x) { return Dash.mark('model', Dash.modelKey(x.model), Dash.modelName(x.model)); } },
        { label: '开始', num: true, cell: function (x) { return Dash.at(x.startedAt); } },
        { label: '交回', num: true, cell: function (x) { return x.doneAt ? Dash.at(x.doneAt) : Dash.mark('status', 'doing', '在审，已 ' + Dash.elapsed(x.startedAt)); } },
        { label: '结论', cell: function (x) { var t = verdictTone(x); return Dash.mark(t[0], t[1], verdictText(x)); } },
        { label: '查出补丁', num: true, cell: function (x) { return Dash.num(x.findings); } },
        { label: '转成的行', cell: function (x) { return rowLinks(x.newRows); } },
        { label: '提交', cell: function (x) { return (x.commits || []).length ? h('span.mono.small', x.commits.join(' ')) : '—'; } }
      ], items, { empty: '还没有收口审计记录' }));
    }
  });

  /* ---------- 任务单、契约申请、调研与交接材料 ---------- */
  var ART_KIND = { task: '任务单', request: '契约申请', research: '调研', handoff: '交接材料' };
  var artKind = 'all';

  Dash.panel({
    id: 'collab-files', page: 'collab', title: '任务单、契约申请、调研与交接材料', note: '两边交接用的文件：是什么、走到哪、对应哪些行', order: 30, needs: ['letters'],
    render: function (el, d) {
      if (!('artifacts' in d.letters)) { el.appendChild(Dash.empty('等这一项数据', '来自 letters.artifacts，等后端 C-239 在 data/auto/letters.js 里写出', true)); return; }
      var arts = d.letters.artifacts || [];
      var kinds = Object.keys(ART_KIND).filter(function (k) { return arts.some(function (x) { return x.kind === k; }); });
      arts.forEach(function (x) { if (!(x.kind in ART_KIND) && kinds.indexOf(x.kind) < 0) kinds.push(x.kind); });
      if (artKind !== 'all' && kinds.indexOf(artKind) < 0) artKind = 'all';
      el.appendChild(h('div.panel-tools', Dash.segment([{ id: 'all', label: '全部 ' + Dash.num(arts.length) }].concat(kinds.map(function (k) {
        return { id: k, label: (ART_KIND[k] || k) + ' ' + Dash.num(arts.filter(function (x) { return x.kind === k; }).length) };
      })), artKind, function (v) { artKind = v; Dash.redraw('collab-files'); })));
      var list = arts.filter(function (x) { return artKind === 'all' || x.kind === artKind; });
      el.appendChild(Dash.table([
        { label: '种类', cell: function (x) { return Dash.chip(ART_KIND[x.kind] || x.kind, 'muted'); } },
        { label: '标题', cell: function (x) { return h('div', x.path ? Dash.a(x.path, x.title) : x.title, x.text ? h('div.small', Dash.longText(x.text)) : null); } },
        { label: '状态', cell: function (x) { return x.status ? h('span.art-status', x.status) : '—'; } },
        { label: '对应行', cell: function (x) { return rowLinks(x.rows); } }
      ], list, { empty: '没有这一类材料' }));
    }
  });
})();
