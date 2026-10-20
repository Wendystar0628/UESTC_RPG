/* 进展页：还没做完的活各在哪一段。
   块（面板 id）：progress-board 任务流水线（比例条 + 四列卡片：排着 → 在做 → 收尾 → 暂停）。
   数据：progress；行所在的段与一句状态都由采集侧给，这里只画。已完成的只给个数，不列。 */
(function () {
  var h = Dash.h;

  var COL_NOTE = { queued: '可派或等前置', doing: '正在施工', finishing: '编译、检查、评审这些收尾', paused: '等别的事先完成' };

  function card(r) {
    var side = Dash.sideKey(r.who);
    return h('div.pcard.t-' + side,
      h('div.pcard-head', h('span.id', r.id), Dash.mark('side', side, r.who || '未定')),
      h('div.pcard-title', r.title),
      r.status ? h('div.pcard-status', r.status) : null);
  }

  Dash.panel({
    id: 'progress-board', page: 'progress', title: '任务流水线', note: '每行活现在在哪一段、谁在做；条的长短按行数', needs: ['progress'],
    render: function (el, d) {
      var g = d.progress, c = g.counts || {}, done = g.done || {}, rows = g.rows || [];
      el.appendChild(h('div.pb-top',
        h('div.pb-bar', Dash.flowbar(c)),
        h('div.pb-done', h('span.pb-done-n', Dash.num(done.today)), h('span', '今天完成'), h('span.pb-sep'), h('span.pb-done-n', Dash.num(done.week)), h('span', '本周完成'))));
      el.appendChild(h('div.pb-cols', Dash.stageKeys.map(function (k) {
        var list = rows.filter(function (r) { return r.stage === k; });
        return h('section.pb-col', { style: '--tone: var(' + Dash.colorVar('stage', k) + ')' },
          h('header.pb-col-head', h('span.pb-col-name', Dash.label('stage', k)), h('span.pb-col-n', Dash.num(c[k] != null ? c[k] : list.length) + ' 行')),
          h('div.pb-col-note', COL_NOTE[k]),
          list.length ? h('div.pb-cards', list.map(card)) : h('div.pb-none', '没有'));
      })));
    }
  });
})();
