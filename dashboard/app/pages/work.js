/* 施工页：队列里的活各在哪一段、流转快不快、卡在哪。
   块（面板 id）：work-board 任务看板、work-leadtime 流转时长、work-stages 阶段停留、work-wip 在制与完成、
                  work-blocked 阻塞、work-jump 插队、work-rework 返工。
   数据：progress（未结行与各自的状态史）、flow（历史统计，口径见各块 caliber）。侧筛选 side 作用于看板、流转时长、阶段停留、阻塞。 */
(function () {
  var h = Dash.h;
  var SIDE_NAME = { claude: 'Claude', codex: 'Codex' };
  var KIND_TONE = { Q: ['seq', 0], C: ['seq', 2] };
  var CAL = {
    lead: '从入队到推送删行；箱体是 25%–80%，横线是中位',
    stage: '近 7 天删除的行在各段停留的中位；四段相加不等于流转中位',
    wip: '在制是日终在做 + 收尾的行，完成是当天推送删掉的行',
    blocked: '等前置与暂停占未结行的比例',
    jump: '新增时就标了插队；暂停按前置里写的行号认',
    rework: '一格一次推送；红是回滚，橙是审计查实开了行'
  };
  var BOARD_ROWS = 8;   // 看板每列先露几行，其余折叠

  /* ---------- 小工具 ---------- */
  function inSide(who, side) { return side === 'all' || Dash.sideKey(who) === side; }
  function byId(rows) {
    var m = {};
    rows.forEach(function (r) { m[r.id] = r; });
    return m;
  }
  function sec(title, node) { return h('section.wk-sec', h('h3.wk-sec-title', title), node); }

  /* ---------- 抽屉：一行活的状态史、前置、被谁等着 ---------- */
  function history(list) {
    var segs = list || [];
    if (!segs.length) return Dash.empty('没有状态史', '', true);
    var total = segs.reduce(function (a, s) { return a + s.min; }, 0) || 1;
    return h('div',
      h('div.wk-band', { role: 'img', 'aria-label': '状态史色带' }, segs.map(function (s) {
        return h('span.wk-band-seg', {
          style: '--n:' + Math.max(s.min, total * 0.02) + '; --tone: var(' + Dash.colorVar('stage', s.stage) + ')',
          title: Dash.label('stage', s.stage) + ' ' + Dash.minutes(s.min)
        });
      })),
      h('ul.wk-hist', segs.map(function (s) {
        return h('li', { style: '--tone: var(' + Dash.colorVar('stage', s.stage) + ')' },
          h('span.dot'), h('span.wk-hist-name', Dash.label('stage', s.stage)),
          h('span.muted', Dash.at(s.from) + ' → ' + (s.to ? Dash.at(s.to) : '现在')),
          h('span.wk-hist-len', Dash.minutes(s.min)));
      })));
  }

  function refs(ids, rows) {
    if (!ids || !ids.length) return h('p.muted', '没有');
    var map = byId(rows);
    return h('div.wk-refs', ids.map(function (id) {
      var r = map[id];
      if (!r) return h('div.wk-ref.is-gone', h('span.id', id), h('span.muted', '不在未结行里'));
      return h('button.wk-ref', { type: 'button', on: { click: function () { openRow(rows, id); } } },
        h('span.id', id), h('span.wk-ref-title', r.title));
    }));
  }

  function openRow(rows, id) {
    var r = byId(rows)[id];
    if (!r) return;
    Dash.drawer({
      title: r.id + ' ' + r.title,
      sub: Dash.label('stage', r.stage) + ' · ' + (r.who || '未定') + ' · 本段' + Dash.since(r.since),
      body: h('div.wk-drawer',
        sec('状态史', history(r.history)),
        sec('前置', refs(r.deps, rows)),
        sec('被谁等着', refs(r.waitedBy, rows)))
    });
  }

  /* 只带数据的抽屉：一串编号 + 标题 + 右侧一句 */
  function listDrawer(title, sub, items) {
    Dash.drawer({
      title: title, sub: sub,
      body: items.length
        ? h('div.wk-drawer', items.map(function (it) { return Dash.item(it.id, it.title, it.tail); }))
        : Dash.empty('没有', '', true)
    });
  }

  /* ---------- work-board 任务看板 ---------- */
  function card(r, rows) {
    return h('button.wk-card' + (r.long ? '.is-long' : ''), { type: 'button', on: { click: function () { openRow(rows, r.id); } } },
      h('span.wk-card-head',
        h('span.id', r.id),
        r.long ? Dash.mark('level', 'warn', '久') : null,
        r.jump ? Dash.mark('mark', '插队', '插队') : null,
        h('span.wk-card-side', Dash.mark('side', Dash.sideKey(r.who), r.who || '未定'))),
      h('span.wk-card-title', r.title),
      h('span.wk-card-time', '本段' + Dash.since(r.since)));
  }

  Dash.panel({
    id: 'work-board', page: 'work', title: '任务看板', note: '每行活现在在哪一段、待了多久', span: 12, order: 10,
    caliber: '本段 = 进入当前这一段至今；列头的中位是该列各行本段时长的中位；"久"是本段已超过历史上同段的 P80',
    needs: ['progress'], uses: ['side', 'params'],
    render: function (el, d, ctx) {
      var all = d.progress.rows || [], dwells = d.progress.dwell || [];
      var rows = all.filter(function (r) { return inSide(r.who, ctx.filters.side); });
      if (!rows.length) { el.appendChild(Dash.empty(all.length ? '这一侧没有未结的行' : '队列空了')); return; }
      el.appendChild(h('div.wk-cols', Dash.stageKeys.map(function (k) {
        var list = rows.filter(function (r) { return r.stage === k; })
          .sort(function (a, b) { return Dash.parseTime(a.since) - Dash.parseTime(b.since); });
        var hit = ctx.params.stage === k;
        var cards = list.map(function (r) { return card(r, all); });
        var dw = dwells.filter(function (x) { return x.side === ctx.filters.side && x.stage === k; })[0];
        var body = !list.length ? h('div.wk-none', '没有')
          : list.length <= BOARD_ROWS ? h('div.wk-cards', cards)
          : h('div.wk-cards', cards.slice(0, BOARD_ROWS), Dash.fold('另 ' + (list.length - BOARD_ROWS) + ' 行', h('div.wk-cards', cards.slice(BOARD_ROWS)), hit));
        return h('section.wk-col' + (hit ? '.is-hit' : ''), { style: '--tone: var(' + Dash.colorVar('stage', k) + ')' },
          h('header.wk-col-head',
            h('span.wk-col-name', Dash.label('stage', k)),
            h('span.wk-col-n', Dash.num(list.length) + ' 行')),
          h('div.wk-col-med', dw && dw.n ? '本段中位 ' + Dash.minutes(dw.med) : ' '),
          body);
      })));
    }
  });

  /* ---------- work-leadtime 流转时长 ---------- */
  Dash.panel({
    id: 'work-leadtime', page: 'work', title: '流转时长', note: '活是不是越拆越大、哪边在变慢', span: 7, order: 20,
    caliber: CAL.lead, window: '14d',
    needs: ['flow'], uses: ['side'],
    render: function (el, d, ctx) {
      var side = ctx.filters.side, list = d.flow.leadTime || [];
      var specs = side === 'all'
        ? [{ s: 'claude', k: 'all' }, { s: 'codex', k: 'all' }, { s: 'all', k: 'Q' }, { s: 'all', k: 'C' }]
        : [{ s: side, k: 'all' }, { s: side, k: 'Q' }, { s: side, k: 'C' }];
      var empty = true, hasPrev = false;
      var groups = specs.map(function (sp) {
        var e = list.filter(function (x) { return x.side === sp.s && x.kind === sp.k; })[0] || {};
        var cur = e.cur || null, prev = e.prev || null;
        if ((cur && cur.n) || (prev && prev.n)) empty = false;
        if (prev) hasPrev = true;
        var name = sp.k === 'all' ? SIDE_NAME[sp.s] + (side === 'all' ? '' : ' · 全部')
          : (side === 'all' ? '' : SIDE_NAME[side] + ' · ') + sp.k + ' 类';
        return {
          key: sp.s + '-' + sp.k, label: name,
          tone: sp.k === 'all' ? ['side', sp.s] : KIND_TONE[sp.k], cur: cur, prev: prev
        };
      });
      if (empty) { el.appendChild(Dash.empty('近 7 天还没有推送删行', '有了就在这里画')); return; }
      el.appendChild(Dash.charts.box(groups, { height: 280, unit: '分钟', label: '流转时长', caliber: CAL.lead }));
      el.appendChild(h('div.note-line', hasPrev ? '实色是近 7 天，浅色虚线是前 7 天' : '前 7 天的记账还不够，先不画对比'));
    }
  });

  /* ---------- work-stages 阶段停留 ---------- */
  Dash.panel({
    id: 'work-stages', page: 'work', title: '阶段停留', note: '时间耗在哪一段', span: 5, order: 30,
    caliber: CAL.stage, window: '14d',
    needs: ['flow'], uses: ['side'],
    render: function (el, d, ctx) {
      var side = ctx.filters.side, stages = d.flow.stages || [];
      var cats = [], ents = [];
      (side === 'all' ? ['claude', 'codex'] : [side]).forEach(function (s) {
        ['cur', 'prev'].forEach(function (w) {
          var e = stages.filter(function (x) { return Dash.sideKey(x.side) === s && x.week === w; })[0] || null;
          if (w === 'prev' && e && e.parts === null) return;
          ents.push(e);
          cats.push(SIDE_NAME[s] + (w === 'cur' ? ' 近 7 天' : ' 前 7 天') + ' · ' + (e && e.n != null ? e.n + ' 行' : '行数未确认'));
        });
      });
      if (ents.every(function (e) { return !e || e.n == null; })) { el.appendChild(Dash.empty('近 7 天推送删行的数量未确认', '后台记账够了就在这里画')); return; }
      if (ents.every(function (e) { return !e || !e.n; })) { el.appendChild(Dash.empty('近 7 天还没有推送删行', '有了就在这里画')); return; }
      var series = Dash.stageKeys.map(function (k) {
        return {
          key: k, label: Dash.label('stage', k), tone: ['stage', k],
          values: ents.map(function (e) { return e && e.parts ? e.parts[k] : null; })
        };
      });
      el.appendChild(Dash.charts.stack(cats, series, {
        horizontal: true, height: cats.length * 46 + 64, unit: '分钟', label: '各段停留的中位', caliber: CAL.stage,
        onClick: function (p) {
          var e = ents[p.index], k = p.series;
          if (!e) return;
          var top = (e.top && e.top[k]) || [];
          listDrawer(cats[p.index] + ' · ' + Dash.label('stage', k), '这一段停留最久的 5 行',
            top.slice(0, 5).map(function (t) { return { id: t.id, title: t.title, tail: Dash.minutes(t.minutes) }; }));
        }
      }));
      el.appendChild(h('div.note-line', '点某一段看停留最久的 5 行'));
    }
  });

  /* ---------- work-wip 在制与完成 ---------- */
  var wip = { domain: false, days: 14 };   // 本块自己的开关；切换时重画本块

  Dash.panel({
    id: 'work-wip', page: 'work', title: '在制与完成', note: '活是不是开得太多', span: 7, order: 40,
    caliber: CAL.wip, window: '14d',
    needs: ['flow'],
    render: function (el, d) {
      var win = (d.flow.wip.windows || {})[String(wip.days)], days = win && win.days;
      if (!win || !days || !days.length) { el.appendChild(Dash.empty('还没有按天的记账')); return; }
      el.appendChild(h('div.wk-tools',
        Dash.chips([{ key: '14', label: '14 天' }, { key: '30', label: '30 天' }], String(wip.days), function (k) { wip.days = Number(k); Dash.redraw('work-wip'); }),
        Dash.chips([{ key: 'domain', label: '按领域' }], wip.domain ? ['domain'] : [], function (v) { wip.domain = v.length > 0; Dash.redraw('work-wip'); }, { multi: true })));
      var bars, tips = null;
      if (wip.domain && !win.byDomain) { el.appendChild(Dash.empty('领域归属还没有可靠来源，暂时不能按领域分')); return; }
      if (wip.domain) {
        bars = win.byDomain.map(function (g, i) {
          return {
            key: g.key, label: g.label, tone: g.unassigned ? ['side', 'muted'] : ['seq', i],
            values: days.map(function (x) { return (x.byDomain && x.byDomain[g.key]) || 0; })
          };
        });
      } else {
        bars = [{ key: 'done', label: '完成', tone: ['seq', 3], values: days.map(function (x) { return x.done; }) }];
        tips = win.byDomain && days.map(function (x) {
          var dm = win.byDomain.filter(function (g) { return x.byDomain && x.byDomain[g.key]; });
          return dm.length ? '领域：' + dm.map(function (g) { return g.label + ' ' + x.byDomain[g.key]; }).join('、') : '';
        });
      }
      el.appendChild(Dash.charts.combo(days.map(function (x) { return x.day; }), bars,
        { key: 'wip', label: '在制', tone: ['stage', 'doing'], values: days.map(function (x) { return x.wip; }) },
        { height: 260, unit: '行', label: '每日在制与完成', caliber: CAL.wip, tips: tips }));
      el.appendChild(h('div.note-line', '近 ' + days.length + ' 天完成 ' + Dash.num(win.done) + ' 行；最近一天在制 ' + Dash.num(days[days.length - 1].wip) + ' 行'));
    }
  });

  /* ---------- work-blocked 阻塞（flow.blocked 按侧给全，页面不重算） ---------- */
  Dash.panel({
    id: 'work-blocked', page: 'work', title: '阻塞', note: '卡在哪、等了多久', span: 5, order: 50,
    caliber: CAL.blocked, window: '24h',
    needs: ['flow', 'progress'], uses: ['side'],
    render: function (el, d, ctx) {
      var side = ctx.filters.side, b = (d.flow.blocked || {})[side] || {}, all = d.progress.rows || [];
      var open = b.open, waiting = b.waiting, avg = b.avgWait;
      if (!open) { el.appendChild(Dash.empty('队列空了')); return; }
      el.appendChild(h('div.wk-lead',
        side === 'all' ? '未结 ' : SIDE_NAME[side] + ' 侧未结 ', h('b', Dash.num(open)), ' 行里等着 ', h('b', Dash.num(waiting)),
        ' 行（', Dash.pct(b.rate, 1), '）' + (waiting ? '，平均等了 ' + Dash.minutes(avg) : '')));

      var longest = b.longest || [];
      if (longest.length) {
        el.appendChild(Dash.charts.bars(longest.slice(0, 5).map(function (x) {
          return {
            label: x.id + ' ' + x.title, value: x.minutes, text: Dash.minutes(x.minutes),
            note: x.status + ((x.on || []).length ? '，等 ' + x.on.join('、') : ''),
            tone: ['stage', x.stage]
          };
        }), {
          labelWidth: 170, textWidth: 110, label: '等得最久的行', caliber: CAL.blocked,
          onClick: function (p) { openRow(all, longest[p.index].id); }
        }));
      }

      function nodeOf(x, root) {
        var kids = (x.waitedBy || []).map(function (k) { return nodeOf(k, false); });
        return {
          key: x.id, label: x.id + ' ' + x.title, tone: ['stage', x.stage],
          sub: kids.length ? '被 ' + kids.length + ' 行等着' : null, kids: kids, open: root
        };
      }
      var chains = (b.chains || []).map(function (c) { return nodeOf(c, true); }).filter(function (n) { return n.kids.length; });
      if (chains.length) {
        el.appendChild(sec('等前置的链', Dash.tree(chains, { onClick: function (n) { openRow(all, n.key); } })));
      }
      if (!longest.length && !chains.length) el.appendChild(h('div.note-line', waiting ? '这些行都不在最长的 5 条里' : '没有行在等'));
    }
  });

  /* ---------- work-jump 插队 ---------- */
  Dash.panel({
    id: 'work-jump', page: 'work', title: '插队', note: '插队多不多、打断了谁', span: 6, order: 60,
    caliber: CAL.jump, window: '7d',
    needs: ['flow'],
    render: function (el, d) {
      var j = d.flow.jump || {}, count = function (v) { return v == null ? '未确认' : Dash.num(v); };
      el.appendChild(h('div.wk-ringrow',
        h('div.wk-ring', Dash.charts.ring([
          { label: '插队', value: j.jumped, tone: ['mark', '插队'] },
          { label: '其余新增', value: j.others, tone: ['side', 'muted'] }
        ], { height: 112, unit: '行', center: Dash.pct(j.rate, 1), label: '近 7 天新增里插队的占比', caliber: CAL.jump })),
        h('div.wk-ringtext',
          h('div.wk-lead', '近 7 天新增 ', h('b', count(j.added)), ' 行，插队 ', h('b', count(j.jumped)), ' 行'),
          h('div.muted', '让 ' + count(j.paused) + ' 行暂停'))));
      var rows = j.rows || [];
      if (!rows.length) { el.appendChild(h('div.note-line', j.jumped == null ? '近 7 天有没有插队未确认' : '近 7 天没有插队')); return; }
      el.appendChild(h('div.wk-list', rows.map(function (r) {
        return h('button.wk-jrow', {
          type: 'button',
          on: { click: function () {
            listDrawer(r.id + ' ' + r.title, '插队于 ' + Dash.at(r.at) + (r.paused && r.paused.length ? '，让 ' + r.paused.length + ' 行暂停' : '，没有行因此暂停'),
              (r.paused || []).map(function (x) { return { id: x.id, title: x.title, tail: '被暂停' }; }));
          } }
        },
          h('span.id', r.id), h('span.wk-jrow-title', r.title),
          h('span.wk-jrow-sub', r.paused && r.paused.length ? '让谁暂停：' + r.paused.map(function (x) { return x.id; }).join('、') : '没有行因此暂停'));
      })));
    }
  });

  /* ---------- work-rework 返工 ---------- */
  var REWORK = { none: ['level', 'ok'], revert: ['level', 'bad'], audit: ['level', 'warn'] };
  var REWORK_NAME = { revert: '回滚', audit: '审计查实' };

  Dash.panel({
    id: 'work-rework', page: 'work', title: '返工', note: '推上去的有多少要返工', span: 6, order: 70,
    caliber: CAL.rework, window: '30d',
    needs: ['flow'],
    render: function (el, d) {
      var rw = d.flow.rework || {}, sm = rw.summary || {}, pushes = rw.pushes || [];
      if (!sm.n) { el.appendChild(Dash.empty('还没有推送记账')); return; }
      el.appendChild(h('div.wk-lead', '近 30 天 ', h('b', Dash.num(sm.n)), ' 次推送里 ', h('b', Dash.num(sm.revert + sm.audit)),
        ' 次有返工（', Dash.pct(sm.rate, 1), '）'));
      el.appendChild(Dash.dots(pushes.map(function (p) {
        return {
          tone: REWORK[p.kind] || REWORK.none,
          tip: Dash.at(p.at) + ' · ' + ((p.rows || []).length ? p.rows.join('、') : '无行号') + (p.why ? ' · ' + p.why : '')
        };
      })));
      el.appendChild(h('div.wk-legend',
        Dash.mark('level', 'ok', '没返工'), Dash.mark('level', 'bad', '回滚 ' + sm.revert), Dash.mark('level', 'warn', '审计查实 ' + sm.audit)));
      var items = rw.items || [];
      if (items.length) {
        el.appendChild(sec('返工项', h('div', items.map(function (it) {
          return Dash.item(it.id, it.title, h('span.wk-right', Dash.mark('level', it.kind === 'revert' ? 'bad' : 'warn', REWORK_NAME[it.kind] || it.kind), h('span.muted', Dash.at(it.at))));
        }))));
      }
    }
  });
})();
