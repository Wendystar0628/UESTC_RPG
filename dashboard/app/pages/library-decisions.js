/* 资料页 · 决策：按 D 编号的一条带（状态着色）与最近的决策列表。
   块（面板 id）：lib-decisions。数据 decisions。 */
(function () {
  var h = Dash.h;
  var SHOW = 12;

  /* 决策状态 → 色表（生效记完成、待定记进行中、其余记不适用） */
  function toneOf(s) {
    s = String(s || '');
    if (/生效|有效/.test(s)) return ['status', 'done'];
    if (/待|草|提议/.test(s)) return ['status', 'doing'];
    return ['status', 'na'];
  }
  function num(id) { return parseInt(String(id).replace(/^\D+/, ''), 10) || 0; }

  function row(it) {
    var t = toneOf(it.status);
    return Dash.row({ lead: h('span.mono.muted', it.id), title: it.text, meta: it.scope ? h('span.tag', it.scope) : null, act: [Dash.mark(t[0], t[1], it.status || '—')] });
  }

  Dash.panel({
    id: 'lib-decisions', page: 'library', tab: 'decisions', title: '决策', note: '最近定了什么、还生效吗：按 D 编号排，一格一条，颜色是状态；点格子看全文', order: 10, needs: ['decisions'], watch: ['decision'],
    render: function (el, d) {
      var x = d.decisions, recent = (x.recent || []).slice().sort(function (a, b) { return num(a.id) - num(b.id); });
      var by = {};
      recent.forEach(function (r) { var k = r.status || '—'; by[k] = (by[k] || 0) + 1; });
      el.appendChild(Dash.status('累计 ' + Dash.num(x.total) + ' 条决策；这里是编号最大的 ' + recent.length + ' 条：' + Object.keys(by).map(function (k) { return k + ' ' + by[k]; }).join('、'), 'muted',
        { sub: x.total > recent.length ? '更早的决策要等采集侧给出全量列表（含登记时刻）才能排上时间线' : '' }));
      el.appendChild(Dash.charts.heatmap({ x: recent.map(function (r) { return r.id; }), y: ['决策'], mode: 'tone',
        cells: recent.map(function (r, i) { return { x: i, y: 0, tone: toneOf(r.status), title: r.text, id: r.id }; }) },
        { cellHeight: 34, labelWidth: 50, xInterval: Math.max(0, Math.floor(recent.length / 16) - 1), legend: [['status', ['done', 'doing', 'na']]],
          onClick: function (c) { Dash.select('decision', c.id); } }));
      var pick = recent.filter(function (r) { return r.id === Dash.selected('decision'); })[0];
      if (pick) el.appendChild(h('div.list.lib-pick', row(pick)));
      var latest = recent.slice().reverse();
      el.appendChild(h('div.list', latest.slice(0, SHOW).map(row)));
      if (latest.length > SHOW) el.appendChild(Dash.fold('更早的 ' + (latest.length - SHOW) + ' 条', h('div.list', latest.slice(SHOW).map(row))));
    }
  });
})();
