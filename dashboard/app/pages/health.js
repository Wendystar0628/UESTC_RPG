/* 健康页：这套开发机器健不健康。
   块（面板 id）：health-summary 总体 · health-groups 各组检查 · health-trend 最近 48 轮 · health-gates 门禁
                  · health-tests 测试规模与耗时 · health-hooks 钩子 · health-fresh 数据新鲜度。
   数据：health（健康页契约 v3 §8.1）；health-fresh 不用数据，读 Dash.freshness()，外壳随推送心跳重画它。
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
      title: (g.checkout || '检出') + ' · ' + (g.pkg || '—'),
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
          h('span.gt-top', h('b.gt-checkout', g.checkout || '—'), h('span.gt-pkg', g.pkg || '—'), h('span.gt-at', Dash.at(g.at))),
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
          { label: '快检耗时', tone: ['stage', 'finishing'], axis: 1, values: days.map(function (x) { return x.quickSec; }) }
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
