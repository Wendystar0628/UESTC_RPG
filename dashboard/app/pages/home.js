/* 首页：要不要我动手、项目推进得怎样。一屏内放得下下面几块。
   块（面板 id）：home-attention 等你过目 · home-feedback 给主会话留言 · home-progress 进展一览 · home-milestones 最近里程碑 · home-stats 关键统计。
   数据：attention、feedback、progress、notices、stats；判断都在采集侧，这里只画。"知道了"经 Dash.ack、留言经 Dash.feedback 写回本地服务。 */
(function () {
  var h = Dash.h;

  /* ---------- 等你过目 ---------- */
  var SHOW_ATT = 3;
  var acking = {};    // id -> true：正在写回
  var ackErr = {};    // id -> 失败原因

  function ack(id, revision) {
    acking[id] = true; delete ackErr[id];
    Dash.redraw('home-attention');
    Dash.ack(id, revision).catch(function (e) { ackErr[id] = e.message; })
      .then(function () { delete acking[id]; Dash.redraw('home-attention'); });
  }

  function attRow(x) {
    return h('div.att',
      h('div.att-kind', Dash.mark('kind', x.kind)),
      h('div.att-main',
        h('div.att-title', x.title, h('span.att-at', Dash.at(x.at))),
        x.detail ? h('div.att-detail', x.detail) : null,
        ackErr[x.id] ? h('div.att-err', '没记下：' + ackErr[x.id]) : null),
      h('div.att-act',
        x.link ? Dash.open(x.link, '打开') : null,
        h('button.btn.btn-primary', { type: 'button', disabled: acking[x.id] || null, on: { click: function () { ack(x.id, x.revision); } } },
          acking[x.id] ? '记下中' : '知道了')));
  }

  Dash.panel({
    id: 'home-attention', page: 'home', title: '等你过目', note: '都已按主会话的判断在做，不挡推进；看过点"知道了"', span: 7, order: 10, needs: ['attention'],
    render: function (el, d) {
      var open = (d.attention.items || []).filter(function (x) { return !x.acked; })
        .sort(function (a, b) { return Dash.parseTime(b.at) - Dash.parseTime(a.at); });
      if (!open.length) { el.appendChild(Dash.status('都看过了，没有等你过目的', 'ok')); return; }
      el.appendChild(h('div.att-list', open.slice(0, SHOW_ATT).map(attRow)));
      if (open.length > SHOW_ATT) {
        el.appendChild(Dash.fold(h('span.fold-sum', '另外 ' + (open.length - SHOW_ATT) + ' 条'), h('div.att-list', open.slice(SHOW_ATT).map(attRow))));
      }
    }
  });

  /* ---------- 给主会话留言 ---------- */
  var sendState = { sending: false, ok: null, err: null };

  function feedbackForm(el) {
    var ta = h('textarea.fb-input', { rows: '2', placeholder: '想对主会话说的话，比如"夜景再暗一点"' });
    var btn = h('button.btn.btn-primary.fb-send', { type: 'button', disabled: true }, '发送');
    var result = h('div.fb-result', { role: 'status' });
    function refresh() {
      btn.disabled = sendState.sending || !ta.value.trim();
      btn.textContent = sendState.sending ? '发送中' : '发送';
      result.className = 'fb-result' + (sendState.err ? ' t-risk' : sendState.ok ? ' t-ok' : '');
      result.textContent = sendState.err ? '没有送出：' + sendState.err : sendState.ok ? '已送到主会话 ' + sendState.ok : '';
    }
    function send() {
      var text = ta.value.trim();
      if (!text || sendState.sending) return;
      sendState = { sending: true, ok: null, err: null };
      refresh();
      Dash.feedback(text).then(function (j) {
        sendState = { sending: false, ok: Dash.at(j.at || Date.now()), err: null };
        ta.value = '';
      }, function (e) {
        sendState = { sending: false, ok: null, err: e.message };
      }).then(refresh);
    }
    ta.addEventListener('input', function () { if (sendState.ok) sendState.ok = null; refresh(); });
    ta.addEventListener('keydown', function (e) { if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) { e.preventDefault(); send(); } });
    btn.addEventListener('click', send);
    el.appendChild(h('div.fb-form', ta, h('div.fb-bar', result, h('span.fb-hint', 'Ctrl + Enter 也能发'), btn)));
    refresh();
  }

  Dash.panel({
    id: 'home-feedback', page: 'home', title: '给主会话留言', note: '只送主会话，它下一步就会读到', span: 5, order: 20, needs: ['feedback'],
    fixed: feedbackForm,
    render: function (el, d) {
      var items = (d.feedback.items || []).slice().sort(function (a, b) { return Dash.parseTime(b.at) - Dash.parseTime(a.at); }).slice(0, 3);
      if (!items.length) { el.appendChild(h('div.fb-none', '还没有留过言')); return; }
      el.appendChild(h('div.fb-list', items.map(function (x) {
        return h('div.fb-item',
          h('span.fb-at', Dash.at(x.at)),
          h('span.fb-text', x.text),
          h('span.fb-state' + (x.delivered ? '.t-ok' : '.t-muted'), x.delivered ? '主会话已读' : '等主会话读'));
      })));
    }
  });

  /* ---------- 进展一览 ---------- */
  Dash.panel({
    id: 'home-progress', page: 'home', title: '进展一览', note: '没做完的活在哪一段、在做的前 5 行；点色条看全部', span: 7, order: 30, needs: ['progress'],
    render: function (el, d) {
      var g = d.progress, c = g.counts || {}, done = g.done || {};
      el.appendChild(h('div.pg-top',
        h('div.pg-bar', Dash.flowbar(c, function () { return '#progress'; })),
        h('div.pg-done', h('span', '今天完成'), h('b', Dash.num(done.today)), h('span', '本周'), h('b', Dash.num(done.week)))));
      var doing = (g.rows || []).filter(function (r) { return r.stage === 'doing'; });
      if (!doing.length) { el.appendChild(Dash.empty('现在没有在做的行', '', true)); return; }
      el.appendChild(h('div.pg-doing', doing.slice(0, 5).map(function (r) {
        return Dash.item(r.id, r.title, Dash.mark('side', Dash.sideKey(r.who), r.who || '未定'));
      })));
    }
  });

  /* ---------- 最近里程碑 ---------- */
  Dash.panel({
    id: 'home-milestones', page: 'home', title: '最近里程碑', note: '两边最近发给你的 3 条通知，全部在资料页', span: 5, order: 40, needs: ['notices'],
    render: function (el, d) {
      var items = (d.notices.items || []).slice().sort(function (a, b) { return Dash.parseTime(b.at) - Dash.parseTime(a.at); }).slice(0, 3);
      if (!items.length) { el.appendChild(Dash.empty('还没有通知', '', true)); return; }
      el.appendChild(h('ol.ms-list', items.map(function (x) {
        return h('li.ms.t-' + Dash.sideKey(x.from),
          h('div.ms-head', h('span.ms-at', Dash.at(x.at)), h('span.ms-from', x.from)),
          h('div.ms-text', x.text));
      })));
    }
  });

  /* ---------- 关键统计 ---------- */
  function statTile(c) {
    var vis = null;
    if (c.trend && c.trend.length && c.trend.every(function (v) { return typeof v === 'number' && isFinite(v); })) vis = Dash.charts.spark(c.trend, { height: 46, unit: c.unit, label: c.label + '近 ' + c.trend.length + ' 天' });
    else if (c.parts && c.parts.length && c.parts.every(function (p) { return typeof p.value === 'number' && isFinite(p.value); })) {
      vis = Dash.charts.ring(c.parts.map(function (p, i) { return { label: p.label, value: p.value, tone: p.tone || ['seq', i] }; }), { label: c.label, unit: c.unit, height: 60 });
    }
    return h('div.stat' + (c.level ? '.t-' + ({ ok: 'ok', warn: 'warn', bad: 'risk' }[c.level]) : ''),
      h('div.stat-text',
        h('div.stat-label', c.level ? h('span.dot') : null, c.label),
        h('div.stat-value', c.value == null ? '—' : typeof c.value === 'number' ? Dash.num(c.value) : c.value, c.unit ? h('span.stat-unit', c.unit) : null),
        c.note ? h('div.stat-note', c.note) : null),
      vis ? h('div.stat-vis' + (c.trend ? '.is-spark' : '.is-ring'), vis) : null);
  }

  Dash.panel({
    id: 'home-stats', page: 'home', title: '关键统计', note: '今天干了多少、项目有多大、是否健康；小柱是近 7 天，最右是今天', order: 50, needs: ['stats'],
    render: function (el, d) {
      el.appendChild(h('div.stats', (d.stats.cards || []).map(statTile)));
    }
  });
})();
