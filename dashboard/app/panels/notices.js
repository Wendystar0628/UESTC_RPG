/* 通知：时间线，置顶在前，过期自动收起。 */
(function () {
  var h = Dash.h;
  var SHOW = 12;
  var LEVEL = { info: ['信息', 'muted'], attention: ['留意', 'warn'], decision: ['需定', 'risk'] };

  function item(it) {
    var lv = LEVEL[it.level] || LEVEL.info;
    var key = 'notice:' + it.id;
    var seen = Dash.seen(key);
    var act = [];
    if (it.link) act.push(Dash.a(it.link, '打开'));
    if (it.level !== 'info' && !Dash.expired(it)) {
      act.push(h('button.btn.btn-quiet', { type: 'button', on: { click: function () { Dash.markSeen(key, !seen); } } }, seen ? '放回' : '知道了'));
    }
    return h('div.tl-item.t-' + (seen ? 'muted' : lv[1]),
      h('div.tl-main',
        h('div.tl-top',
          Dash.chip(lv[0], lv[1]),
          it.from ? Dash.chip(it.from, Dash.sideTone(it.from)) : null,
          it.pinned ? h('span.tag', '置顶') : null,
          h('span.tl-time', Dash.ago(it.at))),
        h('div.tl-text' + (seen ? '.is-seen' : ''), it.text)),
      act.length ? h('div.row-act', act) : null);
  }

  function timeline(list) { return h('div.timeline', list.map(item)); }

  Dash.panel({
    id: 'notices',
    page: 'notices',
    order: 10,
    card: false,
    needs: ['notices'],
    render: function (el, d) {
      var all = d.notices.items || [];
      var live = all.filter(function (x) { return !Dash.expired(x); });
      var gone = all.filter(Dash.expired);
      live.sort(function (a, b) { return (b.pinned ? 1 : 0) - (a.pinned ? 1 : 0) || (a.at < b.at ? 1 : -1); });
      gone.sort(function (a, b) { return a.at < b.at ? 1 : -1; });
      var care = live.filter(function (x) { return x.level !== 'info' && !Dash.seen('notice:' + x.id); }).length;

      var status;
      if (!live.length) status = Dash.status('没有新通知', 'ok');
      else if (care) status = Dash.status(Dash.mag(care) + ' 条需要你留意', 'warn', { sub: '共 ' + Dash.mag(live.length) + ' 条通知' });
      else status = Dash.status('共 ' + Dash.mag(live.length) + ' 条通知，都不急', 'muted');

      var stack = h('div.stack', status);
      if (live.length) {
        var body = h('div', timeline(live.slice(0, SHOW)));
        if (live.length > SHOW) body.appendChild(Dash.fold('更早的 ' + (live.length - SHOW) + ' 条', timeline(live.slice(SHOW))));
        stack.appendChild(Dash.card('', body));
      }
      if (gone.length) stack.appendChild(Dash.fold('已过期 ' + gone.length + ' 条', timeline(gone)));
      el.appendChild(stack);
    }
  });
})();
