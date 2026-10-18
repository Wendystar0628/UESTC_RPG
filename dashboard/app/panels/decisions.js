/* 决策：最近定下来的事。 */
(function () {
  var h = Dash.h;
  var SHOW = 10;

  function statusTone(s) {
    s = String(s || '');
    if (/生效|有效|active|effective/i.test(s)) return 'ok';
    if (/待|草|提议|pending|proposed/i.test(s)) return 'warn';
    return 'muted';
  }

  function row(it) {
    return Dash.row({
      lead: h('span.mono.muted', it.id),
      title: it.text,
      meta: it.scope ? h('span.tag', it.scope) : null,
      act: [Dash.chip(it.status || '—', statusTone(it.status))]
    });
  }

  Dash.panel({
    id: 'decisions',
    page: 'decisions',
    order: 10,
    card: false,
    needs: ['decisions'],
    render: function (el, d) {
      var data = d.decisions;
      var recent = data.recent || [];
      var pending = recent.filter(function (x) { return statusTone(x.status) === 'warn'; }).length;
      var status = pending
        ? Dash.status(pending + ' 条决策还没生效', 'warn', { sub: '累计 ' + data.total + ' 条决策' })
        : Dash.status('累计 ' + data.total + ' 条决策，最近的都已生效', 'ok');

      var body = h('div');
      if (!recent.length) {
        body.appendChild(Dash.empty('还没有决策记录', '', true));
      } else {
        body.appendChild(h('div.list', recent.slice(0, SHOW).map(row)));
        if (recent.length > SHOW) body.appendChild(Dash.fold('更早的 ' + (recent.length - SHOW) + ' 条', h('div.list', recent.slice(SHOW).map(row))));
      }
      if (Dash.isStale('decisions')) el.classList.add('is-stale');
      el.appendChild(h('div.stack', status, Dash.card('最近的决策', body)));
    }
  });
})();
