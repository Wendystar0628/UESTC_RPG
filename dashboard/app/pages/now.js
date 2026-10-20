/* 现在（首页）：要不要我过目、推进到哪、机器正不正常、谁在干活、工作流与协作的六个数、最近发生了什么。
   块：now-attention 等你过目 · now-feedback 给主会话留言 · now-progress 推进到哪 · now-pulse 机器与人 · now-kpis 六个数 · now-feed 最近动态。
   数据：attention、feedback、now、progress、health、team、feed；"知道了"经 Dash.ack 带内容版本写回，留言经 Dash.feedback。 */
(function () {
  var h = Dash.h;

  /* ---------- 等你过目 ---------- */
  var SHOW_ATT = 3;
  var acking = {};    // id -> true：正在写回
  var ackErr = {};    // id -> 没记下的原因

  function ack(x) {
    acking[x.id] = true; delete ackErr[x.id];
    Dash.redraw('now-attention');
    Dash.ack(x.id, x.revision).catch(function (e) { ackErr[x.id] = e.message; })
      .then(function () { delete acking[x.id]; Dash.redraw('now-attention'); });
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
        h('button.btn.btn-primary', { type: 'button', disabled: acking[x.id] || null, on: { click: function () { ack(x); } } },
          acking[x.id] ? '记下中' : '知道了')));
  }

  Dash.panel({
    id: 'now-attention', page: 'now', title: '等你过目', note: '都已按主会话的判断在做，不挡推进；看过点"知道了"，内容变了会再请你看',
    span: 7, order: 10, needs: ['attention'],
    render: function (el, d) {
      var open = d.attention.items;   // 后端只放没过目的，按时刻降序；条数是 d.attention.open
      if (!d.attention.open) { el.appendChild(Dash.status('都看过了，没有等你过目的', 'ok')); return; }
      el.appendChild(h('div.att-list', open.slice(0, SHOW_ATT).map(attRow)));
      if (d.attention.open > SHOW_ATT) {
        el.appendChild(Dash.fold(h('span.fold-sum', '另外 ' + (d.attention.open - SHOW_ATT) + ' 条'), h('div.att-list', open.slice(SHOW_ATT).map(attRow))));
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
        sendState = { sending: false, ok: Dash.at(j.at), err: null };
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
    id: 'now-feedback', page: 'now', title: '给主会话留言', note: '只送主会话，它下一步就会读到', span: 5, order: 20, needs: ['feedback'],
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

  /* ---------- 推进到哪 ---------- */
  var STEPS = ['P0', 'P1', 'P2', 'P3'];
  Dash.panel({
    id: 'now-progress', page: 'now', title: '推进到哪', note: '阶段、此刻最先做的事、没做完的活在哪一段；点色条看那一段',
    span: 8, order: 30, needs: ['now', 'progress'],
    render: function (el, d) {
      var ph = d.now.phase || {};
      var at = STEPS.indexOf(ph.step);
      el.appendChild(h('div.np-phase',
        ph.step == null ? null : h('ol.np-steps', { 'aria-label': '阶段 ' + ph.step }, STEPS.map(function (s, i) {
          return h('li.np-step' + (i < at ? '.is-past' : i === at ? '.is-cur' : ''), s);
        })),
        h('div.np-text',
          h('div.np-line', h('span.np-k', '当前阶段'), h('span', ph.text || '未确认')),
          h('div.np-line', h('span.np-k', '当前优先级'), h('span', ph.priority || '未确认')))));
      el.appendChild(Dash.flowbar(d.progress.counts, function (k) { return '#work/work-board?stage=' + k; }));
    }
  });

  /* ---------- 机器与人 ---------- */
  Dash.panel({
    id: 'now-pulse', page: 'now', title: '机器与人', note: '巡逻结论、谁在干活；点进健康页、团队页',
    span: 4, order: 40, needs: ['health', 'team'],
    render: function (el, d) {
      var c = d.health.counts || {};
      el.appendChild(h('a.np-row', { href: '#health' },
        Dash.lamp(d.health.level),
        h('span.np-main', h('b', '健康'), h('span', '要看 ' + Dash.num(c.warn) + ' · 有问题 ' + Dash.num(c.bad))),
        h('span.np-go', '›')));
      var leads = d.team.leads || [];
      var by = {};
      leads.forEach(function (l) { by[Dash.sideKey(l.who)] = l; });
      el.appendChild(h('a.np-row', { href: '#team' },
        h('span.np-lamps', leads.map(function (l) {
          var on = l.running === true, off = l.running === false;
          return h('span.dot.t-' + Dash.sideKey(l.who) + (on ? '.is-on' : off ? '.is-off' : '.is-unknown'),
            { title: l.who + (on ? ' 在跑' : off ? ' 空闲' : ' 未确认') });
        })),
        h('span.np-main', h('b', '子代理在跑'),
          h('span', 'Claude ' + Dash.num(by.claude ? by.claude.agents : null) + ' · Codex ' + Dash.num(by.codex ? by.codex.agents : null))),
        h('span.np-go', '›')));
    }
  });

  /* ---------- 六个数 ---------- */
  Dash.panel({
    id: 'now-kpis', page: 'now', title: '工作流与协作', note: '每张卡点进它所在的页看分布与明细；箭头是和前 7 天比',
    caliber: '各卡的口径写在它所在页那块的 ⓘ 里',
    order: 50, needs: ['now'],
    render: function (el, d) {
      el.appendChild(h('div.np-kpis', (d.now.kpis || []).map(function (k) { return Dash.kpi(Object.assign({ vs: '比前 7 天' }, k)); })));
    }
  });

  /* ---------- 最近动态 ---------- */
  Dash.panel({
    id: 'now-feed', page: 'now', title: '最近动态', note: '最近 5 条决策、通知、完成与回滚；全部在动态页',
    order: 60, needs: ['feed'],
    render: function (el, d) {
      var items = (d.feed.items || []).slice().sort(function (a, b) { return Dash.parseTime(b.at) - Dash.parseTime(a.at); }).slice(0, 5);
      if (!items.length) { el.appendChild(Dash.empty('还没有动态', '', true)); return; }
      el.appendChild(Dash.timeline(items.map(function (x) {
        return { at: x.at, tone: ['feed', x.type], tag: Dash.label('feed', x.type), side: x.side, title: x.title };
      }), { compact: true, onClick: function () { Dash.go('feed'); } }));
    }
  });
})();
