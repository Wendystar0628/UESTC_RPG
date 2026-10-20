/* 健康页：这套开发机器健不健康。
   块（面板 id）：health-summary 总体 · health-fail 游戏失败指标 · health-fail-types 失败类别 · health-fail-trend 失败走势 · health-fail-recent 最近登记
                  · health-groups 各组检查 · health-trend 最近 48 轮 · health-gates 门禁
                  · health-tests 测试规模与耗时 · health-hooks 钩子 · health-fresh 数据新鲜度。
   数据：health（健康页契约 v3 §8.1）、failures（游戏失败，路由参数 fmajor、fsub 选大类与子类）；health-fresh 不用数据，读 Dash.freshness()，外壳随推送心跳重画它。
   共用的过时、读取中、没数据等状态由外壳处理，这里只在数据到齐后画。 */
(function () {
  'use strict';

  var h = Dash.h;
  var TONE = { ok: 't-ok', warn: 't-warn', bad: 't-risk' };
  var FRESH_SCALE = 1.5;                    // 新鲜度条的满格 = 旧期限的 1.5 倍，超出的截在满格

  var CAL = {
    summary: '总灯取各项里最差的一级；组块数的是该组要看与有问题的项；巡逻超过应有间隔没更新，结论待刷新',
    groups: '巡逻每轮查下来的结果，按六组；正常项合并成一行，要看与有问题的项展开，建议由巡逻给出',
    trend: '一根柱是一轮巡逻，高度是该轮要看（橙）与有问题（红）的项数，最近 48 轮',
    gates: '每个检出最近一次完成的门禁报告；没跑的门禁画灰，不算没过',
    tests: '取每天最后一轮巡逻的测试个数与快检耗时，近 14 天',
    hooks: '自测数的是钩子自检合预期的个数；报错取近 24 小时钩子记账里的报错',
    fail: '窗口按提交数取（最近若干次提交），不是按天；频率是窗口内每一段提交里的登记次数，不足一段不给频率；登记数不等于实际失败数，没登记的失败这里看不到',
    failTypes: '按根因类别数窗口内的登记；点大类看它的子类，点子类筛选下面的最近登记；未收录类别是登记的类别不在类型表里',
    failTrend: '窗口按提交序号分段，一段一根柱，按大类堆叠；记账中途开始的段只含起点之后的登记',
    failRecent: '窗口内最近的登记，新的在前；没选类别时是全部，选了大类或子类只列对应的',
    fresh: '距上次确认的时间 ÷ 这份数据允许的旧期限；条长满格是 1.5 倍，超过 1 就算旧了，变橙'
  };

  function toneCls(level) { return TONE[level] || 't-muted'; }
  function levelName(level) { return level === 'ok' ? '正常' : level === 'warn' ? '要看' : level === 'bad' ? '有问题' : '未确认'; }

  /* "M-D" 的日期短标；格式不认识就原样 */
  function shortDay(day) {
    var m = /^\d{4}-(\d{2})-(\d{2})/.exec(String(day || ''));
    return m ? Number(m[1]) + '-' + Number(m[2]) : String(day == null ? '—' : day);
  }

  function pctText(ratio) { return ratio == null ? '—' : Math.round(ratio * 100) + '%'; }

  /* ================= 1 总体 ================= */

  var groupEls = {};     // 组 key → 折叠节点，health-groups 每次重画时更新；总体块的组块点了去找它
  var whenState = null;  // { el, at, late }：巡逻时刻那一行，每 30 秒按当前时间重写"多久前"

  function paintWhen(el, at, late) {
    if (isNaN(Dash.parseTime(at))) { el.replaceChildren('巡逻时刻未确认'); return; }
    el.replaceChildren(h('span', '巡逻 ' + Dash.at(at) + ' · ' + Dash.ago(at)), late ? h('span.hl-late', '结论待刷新') : '');
  }

  setInterval(function () {
    if (whenState && whenState.el.isConnected) paintWhen(whenState.el, whenState.at, whenState.late);
  }, 30000);

  function jumpToGroup(key) {
    var d = groupEls[key];
    if (!d || !d.isConnected) return;
    d.open = true;
    d.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  Dash.panel({
    id: 'health-summary', page: 'health', title: '总体', note: '总体怎样', caliber: CAL.summary,
    span: 12, order: 10, needs: ['health'],
    render: function (el, d) {
      var hd = d.health;
      var when = h('div.hl-when');
      whenState = { el: when, at: hd.at, late: hd.late === true };
      paintWhen(when, hd.at, whenState.late);
      var n = hd.counts;
      var counts = n && ['ok', 'warn', 'bad'].every(function (k) { return typeof n[k] === 'number'; })
        ? h('div.hl-counts', '正常 ' + Dash.num(n.ok) + ' 项 · 要看 ' + Dash.num(n.warn) + ' 项 · 有问题 ' + Dash.num(n.bad) + ' 项') : null;

      el.appendChild(h('div.hl-head',
        h('span.hl-big.' + toneCls(hd.level), { role: 'img', 'aria-label': levelName(hd.level) }),
        h('div.hl-main', h('div.hl-sentence', hd.summary || '还没有结论'), when, counts)));

      var groups = hd.groups || [];
      if (!groups.length) return;
      el.appendChild(h('div.hl-blocks', groups.map(function (g) {
        var t = g.counts;
        return h('button.hl-block.' + toneCls(g.level), { type: 'button', title: '看这一组的详情', on: { click: function () { jumpToGroup(g.key); } } },
          h('span.hl-block-top', Dash.lamp(g.level), h('span.hl-block-name', g.label)),
          h('span.hl-block-n', '要看 ', h('b', Dash.num(t.warn)), '　有问题 ', h('b', Dash.num(t.bad)),
            t.unknown ? h('span', '　未确认 ', h('b', Dash.num(t.unknown))) : null));
      })));
    }
  });

  /* ================= 游戏失败（四块，在总体之后） ================= */

  var FAIL_BLOCK = 'health-fail-types';

  function failBlank(fd) {
    if (!fd.coverage || fd.coverage.from == null) return Dash.empty('还没开始记账', '游戏本体的失败开始登记后，这里才有次数、频率和走势', true);
    if (!fd.window || fd.window.n == null) return Dash.empty('窗口内次数未确认', '', true);
    return null;
  }

  function failZero(fd) {
    if (fd.window.n !== 0) return null;
    return h('div', Dash.status('窗口内没有登记', 'ok'), h('div.note-line', '登记数不等于实际失败数：没登记的失败，这里看不到'));
  }

  /* 路由里的 fmajor / fsub 对不上数据（例如类别已改名）时当没选 */
  function failSel(fd, params) {
    var majors = fd.majors || [];
    var idx = -1;
    majors.forEach(function (m, i) { if (m.key === params.fmajor) idx = i; });
    var major = idx >= 0 ? majors[idx] : null;
    var sub = major ? (major.subs || []).filter(function (s) { return s.key === params.fsub; })[0] || null : null;
    return { major: major, sub: sub, idx: idx };
  }

  function failRateText(rate, seg) { return rate == null ? '—' : Dash.num(rate) + '/' + Dash.num(seg); }

  /* 记录 id 列表 → 时间线条目；记录里的子类不在类别表里时按原名显示 */
  function failItems(fd, ids) {
    var subs = {};
    (fd.majors || []).forEach(function (m, i) {
      (m.subs || []).forEach(function (s) { subs[s.key] = { label: s.label, idx: i }; });
    });
    return (ids || []).map(function (id) {
      var r = fd.records && fd.records[id];
      if (!r) return null;
      var s = subs[r.sub];
      return {
        at: r.at, tone: s ? ['seq', s.idx] : ['side', 'muted'], tag: s ? s.label : (r.sub || '未收录类别'),
        side: r.who || null, title: r.note || '（没写说明）', sub: r.row ? '行号 ' + r.row : null
      };
    }).filter(Boolean);
  }

  /* ---- 指标卡 ---- */
  Dash.panel({
    id: 'health-fail', page: 'health', title: '游戏失败', note: '游戏本体最近失败得多不多', caliber: CAL.fail,
    span: 12, order: 11, needs: ['failures'],
    render: function (el, d) {
      var fd = d.failures, w = fd.window;
      var blank = failBlank(fd);
      if (blank) { el.appendChild(blank); return; }
      var seg = Dash.num(w.segment);
      var freq = Dash.kpi({
        label: '每 ' + seg + ' 次提交', value: w.rate, unit: '次', trend: w.trend,
        note: w.rate == null ? '不足 ' + seg + ' 次提交，不算频率' : '窗口内的平均频率'
      });
      var tr = freq.querySelector('.kpi-trend');
      if (tr) tr.title = '最新一段与此前各满段的均值相比';
      el.appendChild(h('div.hf-kpis',
        Dash.kpi({ label: '窗口内登记', value: w.n, unit: '次', note: '最近 ' + Dash.num(w.size) + ' 次提交' }),
        freq,
        Dash.kpi({ label: '最近一次登记', value: w.last ? Dash.ago(w.last.at) : null, note: w.last ? Dash.at(w.last.at) : '窗口内没有登记' }),
        Dash.kpi({ label: '新增类别', value: w.fresh, unit: '个', note: '记账起点之后新增、窗口内的子类' })));
      var zero = failZero(fd);
      if (zero) el.appendChild(zero);
      var from = fd.coverage.from;
      el.appendChild(h('div.note-line',
        '窗口是最近 ' + Dash.num(w.size) + ' 次提交' + (w.commits != null ? '（现有 ' + Dash.num(w.commits) + ' 次）' : '') +
        '，每 ' + seg + ' 次一段；记账从 ' + Dash.day(from) + ' ' + Dash.at(from).slice(-5) + ' 起'));
    }
  });

  /* ---- 大类 → 子类 ---- */
  function failBarRow(o) {
    return h('button.fb-row' + (o.on ? '.is-on' : ''), {
      type: 'button', 'aria-pressed': String(!!o.on), dataset: { tip: o.tip },
      style: '--tone: var(' + Dash.colorVar('seq', o.tone) + ')', on: { click: o.click }
    },
      h('span.fb-name', o.label, o.fresh ? Dash.mark('level', 'warn', '新增 ' + shortDay(o.fresh)) : null),
      h('span.fb-track', h('span.fb-fill', { style: 'width:' + o.pct + '%' })),
      h('span.fb-tail', o.tail));
  }

  function failPct(n, max) { return n && max ? n / max * 100 : 0; }

  Dash.panel({
    id: FAIL_BLOCK, page: 'health', title: '失败类别', note: '哪一类失败得最多', caliber: CAL.failTypes,
    span: 5, order: 12, needs: ['failures'], uses: ['params'],
    render: function (el, d, ctx) {
      var fd = d.failures, w = fd.window;
      var blank = failBlank(fd);
      if (blank) { el.appendChild(blank); return; }
      var zero = failZero(fd);
      if (zero) { el.appendChild(zero); return; }
      var seg = w.segment;
      var sel = failSel(fd, ctx.params);
      var anyNull = false;
      function tail(x) {
        if (x.rate == null) anyNull = true;
        return Dash.num(x.n) + ' 次 · ' + failRateText(x.rate, seg);
      }
      if (!sel.major) {
        var majors = fd.majors || [];
        var maxM = majors.reduce(function (m, x) { return Math.max(m, x.n || 0); }, 0);
        el.appendChild(h('div.fb-list', majors.map(function (m, i) {
          return failBarRow({
            label: m.label, tone: i, pct: failPct(m.n, maxM), tail: tail(m), tip: m.label + '\n点开看它的子类',
            click: function () { Dash.go('health', { fmajor: m.key }, FAIL_BLOCK); }
          });
        })));
      } else {
        var m = sel.major, subs = m.subs || [];
        var maxS = subs.reduce(function (x, s) { return Math.max(x, s.n || 0); }, 0);
        el.appendChild(h('div.fb-head',
          h('button.btn', { type: 'button', on: { click: function () { Dash.go('health', {}, FAIL_BLOCK); } } }, '‹ 全部大类'),
          h('span.fb-cur', m.label, h('span.fb-cur-n', tail(m)))));
        el.appendChild(h('div.fb-list', subs.map(function (s) {
          var on = !!sel.sub && sel.sub.key === s.key;
          return failBarRow({
            label: s.label, fresh: s.fresh === true ? s.added : null, tone: sel.idx, pct: failPct(s.n, maxS), tail: tail(s), on: on,
            tip: s.label + '\n' + s.note,
            click: function () { Dash.go('health', on ? { fmajor: m.key } : { fmajor: m.key, fsub: s.key }, FAIL_BLOCK); }
          });
        })));
      }
      if (w.unclassified > 0) el.appendChild(Dash.status('未收录类别 ' + Dash.num(w.unclassified) + ' 次：类别不在类型表里，不计入任何大类', 'warn'));
      el.appendChild(h('div.note-line', '条尾：窗口内次数 · 每 ' + Dash.num(seg) + ' 次提交的次数' + (anyNull ? '；—：不足 ' + Dash.num(seg) + ' 次提交，不算频率' : '')));
    }
  });

  /* ---- 提交段走势 ---- */
  function openFailSegment(fd, s, label) {
    var items = failItems(fd, s.recent);
    Dash.drawer({
      title: '提交 ' + label.replace('\n', ' · '),
      sub: '登记 ' + Dash.num(s.n) + ' 次' + (s.n > items.length ? '，这里列最近 ' + items.length + ' 条' : ''),
      body: items.length ? Dash.timeline(items) : Dash.empty('这一段没有登记', '', true)
    });
  }

  Dash.panel({
    id: 'health-fail-trend', page: 'health', title: '失败走势', note: '失败是越来越多还是越来越少', caliber: CAL.failTrend,
    span: 7, order: 13, needs: ['failures'],
    render: function (el, d) {
      var fd = d.failures;
      var blank = failBlank(fd);
      if (blank) { el.appendChild(blank); return; }
      var zero = failZero(fd);
      if (zero) { el.appendChild(zero); return; }
      var segs = fd.segments || [];
      if (!segs.length) { el.appendChild(Dash.empty('还没有分段', '', true)); return; }
      var cats = segs.map(function (s) { return (s.from + 1) + '–' + s.to + (s.partial ? '\n记账中途开始' : ''); });
      var series = (fd.majors || []).map(function (m, i) {
        return {
          key: m.key, label: m.label, tone: ['seq', i],
          values: segs.map(function (s) { var v = s.byMajor ? s.byMajor[m.key] : null; return v == null ? null : v; })
        };
      });
      el.appendChild(Dash.charts.stack(cats, series, {
        height: 280, label: '每段提交里各大类的登记次数', unit: '次', caliber: CAL.failTrend,
        onClick: function (p) { if (segs[p.index]) openFailSegment(fd, segs[p.index], cats[p.index]); }
      }));
      var partial = segs.some(function (s) { return s.partial; });
      var skipped = segs.some(function (s) { return s.n == null; });
      el.appendChild(h('div.note-line',
        '横轴是提交序号区间，左旧右新，每段 ' + Dash.num(fd.window.segment) + ' 次提交；点一段看那段的登记' +
        (partial ? '；标"记账中途开始"的段只含起点之后的登记' : '') + (skipped ? '；整段在记账起点之前的不画' : '') +
        (fd.window.unclassified > 0 ? '；未收录类别的登记不在图里' : '')));
    }
  });

  /* ---- 最近登记 ---- */
  Dash.panel({
    id: 'health-fail-recent', page: 'health', title: '最近登记', note: '最近都是些什么失败', caliber: CAL.failRecent,
    span: 12, order: 14, needs: ['failures'], uses: ['params'],
    render: function (el, d, ctx) {
      var fd = d.failures;
      var blank = failBlank(fd);
      if (blank) { el.appendChild(blank); return; }
      var zero = failZero(fd);
      if (zero) { el.appendChild(zero); return; }
      var sel = failSel(fd, ctx.params);
      var ids = sel.sub ? sel.sub.recent : sel.major ? sel.major.recent : fd.recent;
      var total = sel.sub ? sel.sub.n : sel.major ? sel.major.n : fd.window.n;
      if (sel.major) {
        el.appendChild(h('div.fb-head',
          h('span.fb-cur', '只看 ', h('b', sel.major.label + (sel.sub ? ' › ' + sel.sub.label : ''))),
          h('button.btn', { type: 'button', on: { click: function () { Dash.go('health', {}, 'health-fail-recent'); } } }, '清除筛选')));
      }
      var items = failItems(fd, ids);
      if (!items.length) { el.appendChild(Dash.empty('这一类窗口内没有登记', '', true)); return; }
      el.appendChild(h('div.hf-recent', Dash.timeline(items)));
      if (total > items.length) el.appendChild(h('div.hl-foot', '窗口内共 ' + Dash.num(total) + ' 次，这里列最近 ' + items.length + ' 条'));
    }
  });

  /* ================= 2 各组检查 ================= */

  function itemRow(i) {
    return h('div.hi.' + toneCls(i.level),
      h('span.hi-lamp', Dash.lamp(i.level)),
      h('div.hi-main',
        h('div.hi-title', i.title),
        i.summary ? h('div.hi-sum', i.summary) : null,
        i.advice ? h('div.hi-advice', h('b', '建议'), i.advice) : null));
  }

  Dash.panel({
    id: 'health-groups', page: 'health', title: '各组检查', note: '哪项不对、怎么办', caliber: CAL.groups,
    span: 8, order: 20, needs: ['health'],
    render: function (el, d) {
      var groups = d.health.groups || [];
      groupEls = {};
      if (!groups.length) { el.appendChild(Dash.empty('巡逻还没有分组结果', '', true)); return; }
      el.appendChild(h('div.hg-list', groups.map(function (g) {
        var items = g.items || [];
        var t = g.counts;
        var rest = items.filter(function (i) { return i.level !== 'ok'; });
        var body = rest.map(itemRow);
        if (t.ok) body.push(h('div.hi-ok', h('b', Dash.num(t.ok) + ' 项正常：'), (g.okTitles || []).join('、')));
        if (!items.length) body.push(Dash.empty('这一组没有检查项', '', true));
        var count = h('span.hg-count', Dash.num(items.length) + ' 项',
          t.bad ? h('span.t-risk', ' · 有问题 ' + t.bad) : null,
          t.warn ? h('span.t-warn', ' · 要看 ' + t.warn) : null,
          t.unknown ? h('span.t-muted', ' · 未确认 ' + t.unknown) : null);
        var det = Dash.fold(h('span.hg-sum', Dash.lamp(g.level), h('span.hg-name', g.label), count), body, rest.length > 0);
        det.dataset.group = g.key;
        groupEls[g.key] = det;
        return det;
      })));
    }
  });

  /* ================= 3 最近 48 轮 ================= */

  function openRound(round) {
    var flagged = round.flagged || [];
    var body = !flagged.length
      ? Dash.empty('这一轮没有要看的项', '', true)
      : h('div.dr-groups', flagged.map(function (g) {
        return h('section.dr-group',
          h('div.dr-group-name', g.label || g.group),
          (g.items || []).map(function (f) {
            return h('div.hi.' + toneCls(f.level), h('span.hi-lamp', Dash.lamp(f.level)), h('div.hi-main', h('div.hi-title', f.title)));
          }));
      }));
    Dash.drawer({
      title: '巡逻 ' + Dash.at(round.at),
      sub: '要看 ' + Dash.num(round.warn) + ' 项 · 有问题 ' + Dash.num(round.bad) + ' 项',
      body: body
    });
  }

  Dash.panel({
    id: 'health-trend', page: 'health', title: '最近 48 轮', note: '最近在变好还是变坏', caliber: CAL.trend,
    span: 4, order: 21, needs: ['health'],
    render: function (el, d) {
      var rounds = d.health.rounds || [];
      if (!rounds.length) { el.appendChild(Dash.empty('还没有巡逻记录', '', true)); return; }
      el.appendChild(Dash.charts.stack(
        rounds.map(function (r) { return Dash.at(r.at); }),
        [
          { key: 'warn', label: '要看', tone: ['level', 'warn'], values: rounds.map(function (r) { return r.warn; }) },
          { key: 'bad', label: '有问题', tone: ['level', 'bad'], values: rounds.map(function (r) { return r.bad; }) }
        ],
        { height: 220, label: '最近 ' + rounds.length + ' 轮巡逻要看与有问题的项数', unit: '项', caliber: CAL.trend,
          onClick: function (p) { if (rounds[p.index]) openRound(rounds[p.index]); } }));
      var sm = d.health.roundsSummary, last = sm.last;
      el.appendChild(h('div.hl-foot', '这 ' + Dash.num(sm.n) + ' 轮里 ' + Dash.num(sm.flagged) + ' 轮有要看或有问题的项；最新一轮 ' + Dash.at(last.at) + '：要看 ' + Dash.num(last.warn) + '，有问题 ' + Dash.num(last.bad)));
    }
  });

  /* ================= 4 门禁 ================= */

  function scopeText(g) { return g.scope == null ? '—' : Dash.label('gateScope', g.scope); }

  function openGate(g) {
    var names = (g.failedNames || []).slice(0, 10);
    var body;
    if (g.failed == null) body = Dash.empty('失败数未确认', '', true);
    else if (!g.failed) body = Dash.empty('没有失败的测试', '', true);
    else {
      body = h('div.dr-names',
        names.map(function (n) { return h('div.dr-name', n); }),
        g.failed > names.length ? h('div.hl-foot', '共失败 ' + Dash.num(g.failed) + ' 项，这里只列前 ' + names.length + ' 个的名字') : null);
    }
    Dash.drawer({
      title: (g.checkout || '检出') + ' · ' + scopeText(g),
      sub: '门禁 ' + Dash.at(g.at) + ' · 执行 ' + Dash.num(g.run) + ' 项 · 失败 ' + Dash.num(g.failed) + ' 项',
      body: body
    });
  }

  Dash.panel({
    id: 'health-gates', page: 'health', title: '门禁', note: '门禁最近过没过', caliber: CAL.gates,
    span: 6, order: 30, needs: ['health'],
    render: function (el, d) {
      var gates = d.health.gates || [];
      if (!gates.length) { el.appendChild(Dash.empty('还没有门禁报告', '', true)); return; }
      el.appendChild(h('div.gt-list', gates.map(function (g) {
        return h('button.gt-row', { type: 'button', title: '看失败的测试', on: { click: function () { openGate(g); } } },
          h('span.gt-top', h('b.gt-checkout', g.checkout || '—'), h('span.gt-scope', scopeText(g)), h('span.gt-at', Dash.at(g.at))),
          h('span.gt-bottom',
            h('span.gt-caps', (g.gates || []).map(function (x) {
              return Dash.mark('gate', x.ok === true ? 'pass' : x.ok === false ? 'fail' : 'skip', x.name);
            })),
            h('span.gt-run' + (g.failed > 0 ? '.t-risk' : ''), '执行 ' + Dash.num(g.run) + ' 项，失败 ' + Dash.num(g.failed) + ' 项')));
      })));
    }
  });

  /* ================= 5 测试规模与耗时 ================= */

  Dash.panel({
    id: 'health-tests', page: 'health', title: '测试规模与耗时', note: '测试是不是越测越多、越跑越慢', caliber: CAL.tests, window: '14d',
    span: 6, order: 31, needs: ['health'],
    render: function (el, d) {
      var tests = d.health.tests;
      var days = (tests && tests.days) || [];
      if (!days.length) { el.appendChild(Dash.empty('还没有测试记录', '', true)); return; }
      el.appendChild(Dash.charts.trend(
        days.map(function (x) { return shortDay(x.day); }),
        [
          { label: '测试个数', tone: ['seq', 1], axis: 0, values: days.map(function (x) { return x.count; }) },
          { label: '快检耗时', tone: ['stage', 'integration'], axis: 1, values: days.map(function (x) { return x.quickSec; }) }
        ],
        { height: 220, label: '近 ' + days.length + ' 天测试个数与快检耗时', caliber: CAL.tests, units: ['个', '秒'],
          // 悬停时每个点多写一句行数与占比
          notes: days.map(function (x) { return Dash.num(x.lines) + ' 行，占生产代码 ' + pctText(x.ratio); }) }));
      var last = days[days.length - 1];
      el.appendChild(h('div.hl-foot', shortDay(last.day) + '：' + Dash.num(last.count) + ' 个测试，' + Dash.num(last.lines) + ' 行，占生产代码 ' + pctText(last.ratio) + '；快检 ' + Dash.num(last.quickSec) + ' 秒'));
    }
  });

  /* ================= 6 钩子 ================= */

  Dash.panel({
    id: 'health-hooks', page: 'health', title: '钩子', note: '钩子正常吗', caliber: CAL.hooks,
    span: 6, order: 40, needs: ['health'],
    render: function (el, d) {
      var hk = d.health.hooks;
      if (!hk) { el.appendChild(Dash.empty('钩子情况未确认', '', true)); return; }
      var st = hk.selftest;
      if (st) {
        var failed = st.failed || [];
        el.appendChild(h('div.hk-self.' + toneCls(st.level),
          Dash.lamp(st.level), h('span', '自测 通过 ', h('b', Dash.num(st.ok)), ' / 共 ', h('b', Dash.num(st.total)))));
        if (failed.length) el.appendChild(h('ul.hk-failed', failed.map(function (f) { return h('li', f); })));
      } else {
        el.appendChild(h('div.hk-self.t-muted', Dash.lamp(null), h('span', '自测结果未确认')));
      }
      el.appendChild(h('div.hk-sub', '近 24 小时的报错'));
      var errs = (hk.errors || []).slice().sort(function (a, b) { return (b.n || 0) - (a.n || 0); });
      if (!errs.length) { el.appendChild(Dash.status('近 24 小时钩子没有报错', 'ok')); return; }
      el.appendChild(Dash.charts.bars(errs.map(function (e) {
        return { key: e.hook, label: e.hook, value: e.n, note: '最近一次 ' + Dash.at(e.last), tone: ['level', 'warn'] };
      }), { label: '近 24 小时各钩子的报错次数', unit: '次', caliber: CAL.hooks, labelWidth: 132, textWidth: 70 }));
    }
  });

  /* ================= 7 数据新鲜度 ================= */

  Dash.panel({
    id: 'health-fresh', page: 'health', title: '数据新鲜度', note: '每份数据新不新', caliber: CAL.fresh,
    span: 6, order: 41, needs: [],
    render: function (el) {
      var list = Dash.freshness();
      var old = list.filter(function (f) { return f.stale === true; }).length;
      var unknown = list.filter(function (f) { return f.stale == null; }).length;
      el.appendChild(Dash.charts.bars(list.map(function (f) {
        var known = f.ratio != null && isFinite(f.ratio);
        return {
          key: f.name, label: f.label, value: known ? f.ratio : 0,
          tone: f.stale === true ? ['level', 'warn'] : f.stale === false ? ['seq', 2] : ['side', 'muted'],
          text: f.checked ? Dash.at(f.checked) + ' 确认' : '未确认',
          note: known ? '距上次确认已用掉旧期限的 ' + pctText(f.ratio) : '没有确认时刻，不判断旧不旧'
        };
      }), { height: list.length * 30 + 8, max: FRESH_SCALE, labelWidth: 96, textWidth: 118, label: '每份数据距上次确认已用掉多少旧期限', caliber: CAL.fresh }));
      el.appendChild(h('div.hl-foot', old || unknown
        ? [old ? old + ' 份旧了' : '', unknown ? unknown + ' 份未确认' : ''].filter(Boolean).join('，') : '全部在期限内'));
    }
  });
})();
