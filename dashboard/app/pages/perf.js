/* 性能页：每个场景最近一次跑得怎样、比上一次快还是慢、有没有越过预算线。
   块（面板 id）：perf-head 口径与记录状态 · perf-scenes 各场景主要指标 · perf-more 各场景其余指标 · perf-sessions 最近会话。
   数据：perf（README §2 的 perf 段）。只画后端给的值：差值与越线照分析器，null 写"—"，不画 0。 */
(function () {
  'use strict';

  var h = Dash.h;
  var PAGE = 'perf';
  var MAIN = ['mainViewportGpuMs', 'mainViewportCpuMs', 'frameSetupCpuMs', 'workingSetMiB', 'engineVideoMemoryMiB'];

  var CAL = {
    scenes: '每个场景取分析器最近一次的稳态 p95（内存取全程 p95），差值和同场景上一次比；预算线来自 budgets.json 的开发机目标，越线标红',
    more: '五段是墙钟区间的 p95；存档与关闭耗时是那一次的单次值；没有的写"—"',
    sessions: '最近完成的会话，新的在前；没读成的会话单列，原因照分析器原文'
  };

  /* 小数按量级给位数：小于 10 留两位，否则沿用 Dash.num */
  function fmt(v) {
    if (v == null || !isFinite(v)) return '—';
    return Math.abs(v) < 10 ? (Math.round(v * 100) / 100).toString() : Dash.num(v);
  }
  function signed(v) { return v == null || !isFinite(v) ? '—' : (v > 0 ? '+' : v < 0 ? '−' : '±') + fmt(Math.abs(v)); }

  function buildText(b) {
    if (!b || b.seq == null) return '构建未确认';
    return '#' + b.seq + (b.dirty === true ? ' 含未提交改动' : '');
  }

  /* ================= 页顶：口径句与记录状态 ================= */

  var recState = null;   // { el, since }：正在记录那一行，每 30 秒按当前时刻重写经过时间
  function paintRec(el, since) {
    var min = since ? Dash.sinceMin(since) : null;
    el.replaceChildren(h('span.pf-rec-dot'), '正在记录（自 ' + (since ? Dash.at(since).slice(-5) : '时刻未确认') + '，已 ' + (min == null ? '—' : Math.round(min)) + ' 分钟）');
  }
  setInterval(function () { if (recState && recState.el.isConnected) paintRec(recState.el, recState.since); }, 30000);

  Dash.panel({
    id: 'perf-head', page: PAGE, span: 12, order: 1, window: 'commits', needs: ['perf'],
    render: function (el, d) {
      var pd = d.perf;
      el.appendChild(h('div.pf-head', h('div.pf-caliber', '每个场景看最近一次采集，和上一次比、和预算线比')));
      recState = null;
      if (pd.recording) {
        var rec = h('div.pf-rec');
        recState = { el: rec, since: pd.recording.since };
        paintRec(rec, pd.recording.since);
        el.appendChild(rec);
      }
    }
  });

  /* ================= 场景表：一行一个场景，一格一个指标 ================= */

  function cell(sc, key) {
    var v = sc.values[key], dl = (sc.delta || {})[key], b = (sc.budgets || {})[key];
    var diff = sc.previousId == null ? '首次' : dl && dl.delta != null
      ? signed(dl.delta) + (dl.changeFraction != null ? '（' + signed(dl.changeFraction * 100) + '%）' : '') : '—';
    var line = b && b.budget != null
      ? h('span.pf-line' + (b.over === true ? '.t-risk' : ''), b.over === true ? '越线 ' + signed(b.deltaToBudget) : '线 ' + fmt(b.budget)) : null;
    return h('span.pf-c' + (b && b.over === true ? '.is-over' : ''),
      h('b', fmt(v)), h('span.pf-diff', diff), line);
  }

  function sceneTable(pd, keys) {
    var meta = {};
    (pd.metrics || []).forEach(function (m) { meta[m.key] = m; });
    keys = keys.filter(function (k) { return meta[k]; });
    var cols = '--cols: ' + keys.length;
    var head = h('div.pf-row.is-head', { style: cols }, h('span', '场景'),
      keys.map(function (k) { return h('span', meta[k].label + (meta[k].unit ? '（' + meta[k].unit + '）' : '')); }));
    var rows = pd.scenes.map(function (sc) {
      return h('div.pf-row', { style: cols },
        h('span.pf-scene', h('b', sc.scene), h('span.muted', Dash.at(sc.endedAt) + ' · ' + buildText(sc.build))),
        keys.map(function (k) { return cell(sc, k); }));
    });
    return h('div.pf-table', head, rows);
  }

  function noScenes(el) { el.appendChild(Dash.empty('还没有场景指标', '跑一次 scripts/perf/run.ps1，分析器出数后这里显示', true)); }

  Dash.panel({
    id: 'perf-scenes', page: PAGE, title: '各场景', note: '最近一次跑得怎样，比上次快还是慢', caliber: CAL.scenes,
    span: 12, order: 10, window: 'commits', needs: ['perf'],
    render: function (el, d) {
      var pd = d.perf;
      if (!(pd.scenes || []).length) { noScenes(el); return; }
      el.appendChild(sceneTable(pd, MAIN));
      var over = pd.scenes.filter(function (sc) { return Object.keys(sc.budgets || {}).some(function (k) { return sc.budgets[k].over === true; }); }).length;
      el.appendChild(h('div.note-line', '共 ' + pd.scenes.length + ' 个场景，' + (over ? over + ' 个有指标越线' : '都没越线') + '；每格下方是和上一次的差，再下方是预算线'));
    }
  });

  Dash.panel({
    id: 'perf-more', page: PAGE, title: '其余指标', note: '成本花在哪一段、存档关闭多久', caliber: CAL.more,
    span: 12, order: 20, window: 'commits', needs: ['perf'],
    render: function (el, d) {
      var pd = d.perf;
      if (!(pd.scenes || []).length) { noScenes(el); return; }
      var rest = (pd.metrics || []).map(function (m) { return m.key; }).filter(function (k) { return MAIN.indexOf(k) < 0; });
      if (!rest.length) { el.appendChild(Dash.empty('分析器没给其余指标', '', true)); return; }
      el.appendChild(sceneTable(pd, rest));
    }
  });

  /* ================= 最近会话与读源错误 ================= */

  function dirName(p) { var parts = String(p || '').split(/[\\/]/).filter(Boolean); return parts.length ? parts[parts.length - 1] : '来源未确认'; }
  function sessionError(e) {
    return h('div.note-line', { title: e.source || '' },
      h('b', dirName(e.source)), e.id ? h('span.muted', '（会话 ' + e.id + '）') : null, '：' + (e.reason || '没给原因'));
  }

  Dash.panel({
    id: 'perf-sessions', page: PAGE, title: '最近会话', note: '最近录了哪些、哪些没读成', caliber: CAL.sessions,
    span: 12, order: 30, window: 'commits', needs: ['perf'],
    render: function (el, d) {
      var pd = d.perf, list = pd.sessions || [];
      if (!list.length) el.appendChild(Dash.empty('还没有已分析的会话', '', true));
      else el.appendChild(h('div.pf-ss', list.map(function (s) {
        return h('div.pf-s',
          h('span.pf-s-at', Dash.at(s.endedAt)),
          h('span.pf-s-scene', s.scene || '场景未知'),
          h('span.pf-s-tags', s.origin === 'play' ? Dash.mark('side', 'author', '作者') : Dash.mark('side', 'muted', 'AI'),
            Dash.mark('side', 'muted', s.scope === 'full' ? '成本与体验' : '仅成本')),
          h('span.pf-s-build', buildText(s.build)),
          h('span.pf-s-id.muted', s.id));
      })));
      var errs = pd.sessionErrors || [];
      if (errs.length) {
        var items = errs.map(sessionError);
        el.appendChild(h('div.pf-errs', Dash.status(errs.length + ' 个会话没读成，不算进场景指标', 'warn'),
          items.length > 3 ? items.slice(0, 3).concat([Dash.fold('还有 ' + (items.length - 3) + ' 个', items.slice(3))]) : items));
      }
      var keep = (pd.retention || {}).authorSessions;
      el.appendChild(h('div.note-line', ['共 ' + Dash.num(pd.totalSessions) + ' 个会话，这里列最近 ' + list.length + ' 个',
        keep ? '作者会话 ' + (keep.value == null ? '—' : keep.value) + ' / ' + (keep.limit == null ? '上限未知' : keep.limit) + ' ' + keep.unit : null].filter(Boolean).join('；')));
    }
  });
})();
