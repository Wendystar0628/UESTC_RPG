/* 观察台卡底的记账起点：时间窗起点、哪块面板写记账起点（同一页每个起点只说一次）、卡底文字与刷新。
   依赖 core.js 经 Dash.internal 给出的面板登记、当前视图与数据读取；在 core.js 之后加载；接口见 dashboard/README.md §3。 */
(function () {
  'use strict';

  var Dash = window.Dash;
  if (!Dash.internal) return;   // 页面不经本地服务打开时 core.js 不建内部状态
  var panels = Dash.internal.panels, currentView = Dash.internal.view, have = Dash.internal.have, coverageOf = Dash.internal.coverageOf;

  var WINDOW_MS = { '24h': 864e5, '7d': 7 * 864e5, '14d': 14 * 864e5, '30d': 30 * 864e5 };

  /* 面板数据的时间窗起点：window 缺省按筛选条的 range（地址里拖选了 from 就从那天起） */
  function windowStart(p) {
    var w = p.window || 'range';
    if (w !== 'range') return Date.now() - WINDOW_MS[w];
    var view = currentView(), from = Dash.parseTime(view.params.from);
    return isNaN(from) ? Date.now() - WINDOW_MS[view.filters.range] : from;
  }

  /* 面板的数据此刻齐不齐（render 会不会被调用）：与 renderPanel 的判断同一处 */
  function isReady(p) {
    var failed = 0, missing = 0, waiting = false;
    p.needs.forEach(function (n) {
      if (have(n)) return;
      var st = Dash.dataState(n);
      if (st === 'error') failed++; else if (st === 'wait') waiting = true; else missing++;
    });
    var got = p.needs.length - failed - missing - (waiting ? 1 : 0);
    return p.partial ? (got > 0 || !p.needs.length) && !waiting : !failed && !missing && !waiting;
  }

  /* 记账起点同一页只说一次，由页面统一算：这一页上已画出、数据此刻仍齐的按天窗口面板里，记账起点晚于各自时间窗的，
     同一个起点交给 order 最小的那块写卡底。只看静态登记（needs、window、order）与各份数据此刻的覆盖，稳定后的结果与加载先后无关。
     window 为 'commits' 的面板按提交次数算窗口，起点与"窗口未满"由它自己写 */
  function footPlan(page) {
    var owners = {};
    Object.keys(panels).map(function (id) { return panels[id]; })
      .filter(function (q) { return q.page === page && q.window !== 'commits' && q.state === 'ok' && isReady(q); })
      .sort(function (a, b) { return a.order - b.order || (a.id < b.id ? -1 : 1); })
      .forEach(function (q) {
        var cov = coverageOf(q), t = Dash.parseTime(cov);
        if (cov && t > windowStart(q) && !(t in owners)) owners[t] = { panel: q, cov: cov };
      });
    var plan = {};
    Object.keys(owners).forEach(function (t) { plan[owners[t].panel.id] = owners[t].cov; });
    return plan;
  }

  Dash.coverageText = function (from) {
    return '记账从 ' + Dash.day(from) + ' ' + Dash.at(from).slice(-5) + ' 起，之前的不全';
  };

  /* 只改当前页各块的卡底，不重绘正文；任一数据、筛选或页面变化后都整页重算一遍 */
  Dash.refreshFeet = function () {
    var view = currentView();
    if (!view.page || !view.filters) return;
    var plan = footPlan(view.page);
    Object.keys(panels).forEach(function (id) {
      var p = panels[id];
      if (!p.el || p.page !== view.page) return;
      var cov = plan[id];
      p.footEl.hidden = !cov;
      p.footEl.textContent = p.footEl.hidden ? '' : Dash.coverageText(cov);
    });
  };
})();
