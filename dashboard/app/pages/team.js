/* 团队页：两边各在忙什么、用了哪些模型。
   块（面板 id）：team-leads 两位指挥 · team-usage 今天各模型做了多少 · team-rules 模型分工。
   数据：team（指挥、今天用量、分工），live（指挥在不在跑）。不列子代理明细，Codex 侧不显示 token 用量。 */
(function () {
  var h = Dash.h;

  var AVATAR = { claude: 'Cl', codex: 'Cx' };

  function leadCard(x, running) {
    var side = Dash.sideKey(x.who);
    var on = running == null ? null : !!running;
    return h('div.lead.t-' + side,
      h('div.lead-avatar', { 'aria-hidden': 'true' }, AVATAR[side] || String(x.who || '?').charAt(0).toUpperCase(),
        on == null ? null : h('span.lead-light' + (on ? '.is-on' : '.is-off'))),
      h('div.lead-main',
        h('div.lead-name', x.who, h('span.lead-state', on == null ? '' : on ? '在跑' : '空闲')),
        h('div.lead-model', Dash.modelName(x.model) + ' · 推理档 ' + Dash.effortName(x.effort)),
        h('div.lead-row', x.row ? [h('span.id', x.row), ' ', x.rowTitle || ''] : h('span.muted', '眼下没有在做的行'))),
      h('div.lead-agents', h('div.lead-agents-n', Dash.num(x.agents || 0)), h('div.lead-agents-label', '个子代理在跑')));
  }

  Dash.panel({
    id: 'team-leads', page: 'team', title: '两位指挥', note: '各用什么模型、正在做哪一行、手下几个子代理在跑', order: 10, needs: ['team', 'live'], partial: true,
    render: function (el, d) {
      if (!d.team) { el.appendChild(Dash.empty('还没有团队数据', '', true)); return; }
      var runs = {};
      ((d.live && d.live.leads) || []).forEach(function (l) { runs[Dash.sideKey(l.who)] = l.running; });
      el.appendChild(h('div.leads-grid', (d.team.leads || []).map(function (x) { return leadCard(x, runs[Dash.sideKey(x.who)]); })));
    }
  });

  Dash.panel({
    id: 'team-usage', page: 'team', title: '今天各模型做了多少', note: '条长是子代理累计干活的时长，后面写个数与时长', span: 7, order: 20, needs: ['team'],
    render: function (el, d) {
      var u = (d.team.usageToday || []).slice().sort(function (a, b) { return (b.minutes || 0) - (a.minutes || 0); });
      if (!u.length) { el.appendChild(Dash.empty('今天还没有派过子代理', '', true)); return; }
      var count = u.reduce(function (s, x) { return s + (x.count || 0); }, 0);
      var mins = u.reduce(function (s, x) { return s + (x.minutes || 0); }, 0);
      el.appendChild(h('div.usage-sum', '今天一共 ', h('b', Dash.num(count)), ' 个子代理，累计 ', h('b', Dash.minutes(mins))));
      el.appendChild(Dash.charts.bars(u.map(function (x) {
        return { label: Dash.modelName(x.model), value: x.minutes || 0, text: Dash.num(x.count) + ' 个 · ' + Dash.minutes(x.minutes), tone: ['model', Dash.modelKey(x.model)] };
      }), { label: '今天各模型的子代理时长', labelWidth: 112, textWidth: 150 }));
    }
  });

  Dash.panel({
    id: 'team-rules', page: 'team', title: '模型分工', note: '什么活交给哪个模型', span: 5, order: 30, needs: ['team'],
    render: function (el, d) {
      var r = d.team.rules || [];
      if (!r.length) { el.appendChild(Dash.empty('还没有分工表', '', true)); return; }
      el.appendChild(h('div.rules', r.map(function (x) {
        var key = Dash.modelKey(x.model);
        return h('div.rule',
          h('span.rule-model', { style: '--tone: var(' + Dash.colorVar('model', key) + ')' }, h('i.rule-swatch'), Dash.modelName(x.model)),
          h('span.rule-use', x.use));
      })));
    }
  });
})();
