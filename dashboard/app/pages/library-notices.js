/* 资料页 · 通知：时间线与列表。
   块（面板 id）：lib-notices。数据 notices（post.py 发布）。 */
(function () {
  var h = Dash.h;
  var LEVEL = { info: ['信息', 'muted'], attention: ['留意', 'warn'], decision: ['需定', 'risk'] };
  var TONE = { info: ['tag', '知会'], attention: ['tag', '待审'], decision: ['tag', '需定'] };

  var LANES = [{ id: 'claude', label: 'Claude' }, { id: 'codex', label: 'Codex' }, { id: 'author', label: '作者' }, { id: 'auto', label: '自动' }];
  function laneOf(from) { var t = Dash.sideTone(from); return t === 'muted' ? 'auto' : t; }

  function item(it) {
    var lv = LEVEL[it.level] || LEVEL.info;
    return Dash.row({ lead: Dash.chip(lv[0], lv[1]), title: it.link ? Dash.a(it.link, it.text) : it.text,
      sub: [it.from, it.pinned ? '置顶' : ''].filter(Boolean).join(' · '), meta: Dash.at(it.at), dim: Dash.expired(it) });
  }

  Dash.panel({
    id: 'lib-notices', page: 'library', tab: 'notices', title: '通知', note: '两边和作者发了什么通知：按发布方分道，颜色是等级（红 需定、橙 留意、蓝 信息），列表里是全文', order: 10, needs: ['notices'],
    render: function (el, d) {
      var all = (d.notices.items || []).slice().sort(function (a, b) { return Dash.parseTime(b.at) - Dash.parseTime(a.at); });
      var live = all.filter(function (x) { return !Dash.expired(x); }), gone = all.filter(Dash.expired);
      el.appendChild(Dash.charts.timeline({
        lanes: LANES.filter(function (l) { return live.some(function (x) { return laneOf(x.from) === l.id; }); }),
        items: live.map(function (x) { return { lane: laneOf(x.from), at: x.at, tone: TONE[x.level] || TONE.info, label: x.text }; })
      }, { rowHeight: 34, empty: '没有没过期的通知' }));
      el.appendChild(h('div.list', live.map(item)));
      if (gone.length) el.appendChild(Dash.fold('已过期 ' + gone.length + ' 条', h('div.list', gone.map(item))));
    }
  });
})();
