/* 资料页：查文档、决策和通知。
   块（面板 id）：library-docs 文档目录 · library-decisions 最近决策 · library-notices 通知。
   数据：docs（域 → 文档）、records（最近决策）、notices（通知）。 */
(function () {
  var h = Dash.h;

  Dash.panel({
    id: 'library-docs', page: 'library', title: '文档目录', note: '规则与设计文档按域排；点开一个域看它有哪些文档、各讲什么', order: 10, needs: ['docs'],
    render: function (el, d) {
      var domains = d.docs.domains || [];
      if (!domains.length) { el.appendChild(Dash.empty('没有文档清单', '', true)); return; }
      el.appendChild(h('div.lib-domains', domains.map(function (dm, i) {
        var docs = dm.docs || [];
        return Dash.fold(
          h('span.lib-dom', { style: '--tone: var(' + Dash.colorVar('seq', i) + ')' }, h('i.lib-swatch'), h('span.lib-dom-name', dm.name), h('span.lib-dom-n', Dash.num(docs.length) + ' 份')),
          h('div.lib-docs', docs.map(function (x) {
            return h('div.lib-doc', h('span.lib-code', x.code), h('span.lib-title', x.title), h('span.lib-sum', x.summary || ''));
          })));
      })));
    }
  });

  /* 一条竖时间线：每条一个点、日期与时刻、一句话 */
  function timeline(items, lead) {
    var lastDay = null;
    return h('ol.tl', items.map(function (x) {
      var day = Dash.day(x.at), showDay = day !== lastDay;
      lastDay = day;
      return h('li.tl-item' + (x.tone ? '.t-' + x.tone : ''),
        showDay ? h('div.tl-day', day) : null,
        h('div.tl-row', h('span.tl-at', Dash.at(x.at).replace(/^\d+-\d+ /, '')), lead(x)));
    }));
  }

  Dash.panel({
    id: 'library-decisions', page: 'library', title: '最近决策', note: '最近 15 条定下来的做法，每条一句结论', span: 7, order: 20, needs: ['records'],
    render: function (el, d) {
      var list = (d.records.decisions || []).slice().sort(function (a, b) { return Dash.parseTime(b.at) - Dash.parseTime(a.at); }).slice(0, 15);
      if (!list.length) { el.appendChild(Dash.empty('还没有决策', '', true)); return; }
      el.appendChild(timeline(list.map(function (x) { return Object.assign({ tone: 'muted' }, x); }), function (x) {
        return h('span.tl-text', h('span.id', x.id), ' ', x.text);
      }));
    }
  });

  Dash.panel({
    id: 'library-notices', page: 'library', title: '通知', note: '两边发给你的进展通知，新的在上', span: 5, order: 30, needs: ['notices'],
    render: function (el, d) {
      var list = (d.notices.items || []).slice().sort(function (a, b) { return Dash.parseTime(b.at) - Dash.parseTime(a.at); });
      if (!list.length) { el.appendChild(Dash.empty('还没有通知', '', true)); return; }
      el.appendChild(timeline(list.map(function (x) { return Object.assign({ tone: Dash.sideKey(x.from) }, x); }), function (x) {
        return h('span.tl-text', h('span.tl-from', x.from), ' ', x.text, x.link ? [' ', Dash.open(x.link, '打开', 'tl-open')] : null);
      }));
    }
  });
})();
