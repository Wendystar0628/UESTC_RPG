/* 体系页：这套开发机器由什么组成、是否正常。
   块（面板 id）：system-health 健康 · system-hooks 钩子 · system-scripts 脚本 · system-hookfeed 最近注入了什么 · system-docs 文档。
   数据：system（健康、钩子、脚本分组、文档按域）、hookfeed（四条泳道最近有注入的事件）。不画关系图，不展示注入正文。 */
(function () {
  var h = Dash.h;
  var LEVEL_TONE = { ok: 'ok', warn: 'warn', bad: 'risk' };

  Dash.panel({
    id: 'system-health', page: 'system', title: '健康', note: '巡逻查下来正不正常、要看的有哪几件', order: 10, needs: ['system'],
    render: function (el, d) {
      var g = d.system.health || {}, n = g.counts || null, probs = g.problems || [];
      var ring = n ? Dash.charts.ring([
        { label: '正常', value: n.ok || 0, tone: ['level', 'ok'] },
        { label: '要看', value: n.warn || 0, tone: ['level', 'warn'] },
        { label: '有问题', value: n.bad || 0, tone: ['level', 'bad'] }
      ], { label: '健康检查', unit: '项', height: 96, center: Dash.num(n.ok || 0) + '/' + Dash.num((n.ok || 0) + (n.warn || 0) + (n.bad || 0)) }) : null;
      el.appendChild(h('div.health',
        ring ? h('div.health-ring', ring) : null,
        h('div.health-main',
          Dash.status(g.summary || '还没有结论', LEVEL_TONE[g.level] || 'muted'),
          n ? h('div.health-counts',
            h('span.t-ok', h('span.dot'), '正常 ' + Dash.num(n.ok || 0) + ' 项'),
            h('span.t-warn', h('span.dot'), '要看 ' + Dash.num(n.warn || 0) + ' 项'),
            h('span.t-risk', h('span.dot'), '有问题 ' + Dash.num(n.bad || 0) + ' 项')) : null,
          probs.length ? h('ul.health-probs', probs.map(function (p) { return h('li', p); })) : h('div.muted', '没有要看的问题'))));
    }
  });

  Dash.panel({
    id: 'system-hooks', page: 'system', title: '钩子', note: '在对话里自动送规则、提醒和来信的机制，各管什么、是否正常', span: 5, order: 20, needs: ['system'],
    render: function (el, d) {
      var hooks = d.system.hooks || [];
      if (!hooks.length) { el.appendChild(Dash.empty('没有登记的钩子', '', true)); return; }
      ['claude', 'codex'].forEach(function (side) {
        var list = hooks.filter(function (x) { return Dash.sideKey(x.side) === side; });
        if (!list.length) return;
        el.appendChild(h('div.hook-group.t-' + side,
          h('div.hook-group-name', Dash.label('side', side) + ' 侧 · ' + list.length + ' 个'),
          list.map(function (x) {
            return h('div.hook' + (x.ok ? '' : '.is-bad'),
              h('span.hook-light'),
              h('div.hook-main', h('div.hook-purpose', x.purpose || x.name), h('div.hook-name', x.name)),
              h('span.hook-state', x.ok ? '正常' : '要看'));
          })));
      });
    }
  });

  Dash.panel({
    id: 'system-scripts', page: 'system', title: '脚本', note: '按用途分组，每组几个、管什么；色带长短按个数', span: 7, order: 30, needs: ['system'],
    render: function (el, d) {
      var groups = d.system.scripts || [];
      if (!groups.length) { el.appendChild(Dash.empty('没有脚本分组', '', true)); return; }
      var total = groups.reduce(function (s, g) { return s + (g.count || 0); }, 0);
      el.appendChild(h('div.sc-total', '一共 ', h('b', Dash.num(total)), ' 个脚本'));
      el.appendChild(h('div.sc-strip', groups.map(function (g, i) {
        return h('i.sc-seg', { title: g.group + ' ' + g.count + ' 个', style: 'flex:' + Math.max(g.count, 1) + ' 1 0; background: var(' + Dash.colorVar('seq', i) + ')' });
      })));
      el.appendChild(h('div.sc-groups', groups.map(function (g, i) {
        return h('div.sc-group', { style: '--tone: var(' + Dash.colorVar('seq', i) + ')' },
          h('div.sc-head', h('i.sc-swatch'), h('span.sc-name', g.group), h('span.sc-n', Dash.num(g.count) + ' 个')),
          h('div.sc-purpose', g.purpose));
      })));
    }
  });

  Dash.panel({
    id: 'system-docs', page: 'system', title: '文档', note: '规则与设计文档按域分，色块面积按份数', order: 40, needs: ['system'],
    render: function (el, d) {
      var docs = d.system.docs || [];
      if (!docs.length) { el.appendChild(Dash.empty('没有文档清单', '', true)); return; }
      var total = docs.reduce(function (s, x) { return s + (x.count || 0); }, 0);
      el.appendChild(h('div.sc-total', '一共 ', h('b', Dash.num(total)), ' 份，分 ', h('b', Dash.num(docs.length)), ' 个域'));
      el.appendChild(Dash.charts.treemap(docs.map(function (x, i) {
        return { label: x.domain, value: x.count, note: x.summary, tone: ['seq', i] };
      }), { label: '文档按域的份数', unit: '份', height: 230 }));
      el.appendChild(h('div.dom-list', docs.map(function (x, i) {
        return h('div.dom', { style: '--tone: var(' + Dash.colorVar('seq', i) + ')' },
          h('span.dom-name', h('i.sc-swatch'), x.domain, h('span.dom-n', Dash.num(x.count) + ' 份')), h('span.dom-sum', x.summary));
      })));
    }
  });

  /* 最近注入了什么：四条泳道，每道最近 10 次有注入的事件 */
  var LANES = [
    { role: '主会话', side: 'claude', main: true },
    { role: 'Claude 子代理', side: 'claude', main: false },
    { role: 'Codex 主线', side: 'codex', main: true },
    { role: 'Codex 子代理', side: 'codex', main: false }
  ];
  var EVENT = { UserPromptSubmit: '作者发消息', SessionStart: '会话开始', SubagentStart: '子代理开工', PostToolUse: '工具用完', PreToolUse: '工具用前', Stop: '一轮结束' };

  Dash.panel({
    id: 'system-hookfeed', page: 'system', title: '最近注入了什么', note: '钩子最近往四方的对话里送了哪几块规则或提醒，每道最近 10 次；不展示正文', order: 35, needs: ['hookfeed'],
    render: function (el, d) {
      var byRole = {};
      (d.hookfeed.lanes || []).forEach(function (l) { byRole[l.role] = l.items || []; });
      el.appendChild(h('div.feed', LANES.map(function (lane) {
        var items = (byRole[lane.role] || []).slice().sort(function (a, b) { return Dash.parseTime(b.at) - Dash.parseTime(a.at); }).slice(0, 10);
        return h('section.feed-lane.t-' + lane.side + (lane.main ? '.is-main' : '.is-sub'),
          h('header.feed-head', h('span.feed-role', lane.role), h('span.feed-n', items.length ? '最近 ' + items.length + ' 次' : '')),
          items.length ? h('div.feed-items', items.map(function (x) {
            return h('div.feed-item',
              h('div.feed-meta', h('span.feed-at', Dash.at(x.at)), h('span.feed-ev', EVENT[x.event] || x.event), h('span.feed-chars', Dash.num(x.chars) + ' 字')),
              h('div.feed-blocks', (x.blocks || []).map(function (b) { return h('span.feed-block', b); })));
          })) : h('div.feed-none', '最近没有注入'));
      })));
    }
  });
})();
