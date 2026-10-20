/* 现在页：要不要我动手、此刻在发生什么。
   块（面板 id）：now-pulse 系统脉搏 · now-attention 需要你 · now-recent 最近 · now-lanes 两条指挥泳道 · now-flow 本批流水。
   数据：live（实时）、attention、pipeline、history、notices；判断都在采集侧，这里只画。"知道了"经 Dash.ack 写回采集侧本地服务。 */
(function () {
  var h = Dash.h;

  function sideKey(who) { return String(who || '').toLowerCase().indexOf('codex') >= 0 ? 'codex' : 'claude'; }
  function rowLink(id, cls) {
    return h('a' + (cls ? '.' + cls : '.mono'), { href: '#work/work-flow', on: { click: function () { if (Dash.selected('row') !== id) Dash.select('row', id); } } }, id);
  }

  /* 一行所处的阶段：有进行中或失败的取第一个，否则取最后一个完成的 */
  function stageNow(row) {
    var st = row.stages || {}, cur = null, last = null;
    Dash.stages.forEach(function (s) {
      var x = st[s.id];
      if (!x) return;
      if (!cur && (x.status === 'doing' || x.status === 'fail')) cur = { stage: s, status: x.status, at: x.at };
      if (x.status === 'done') last = { stage: s, status: 'done', at: x.at };
    });
    return cur || last;
  }

  function stageText(now) {
    if (now.status === 'fail') return now.stage.label + '失败';
    if (now.status === 'done') return now.stage.label + '完成';
    return now.stage.id === 'pending' ? '等编译' : now.stage.label + '中';
  }

  /* 八格阶段条：每格一个阶段，按状态着色 */
  function stageStrip(row) {
    return h('div.stage-strip', Dash.stages.map(function (s) {
      var x = (row.stages || {})[s.id] || { status: 'todo' };
      return h('i.stage-seg.is-' + x.status, { title: s.label + '：' + Dash.label('status', x.status) + (x.at ? ' ' + Dash.at(x.at) : '') + (x.note ? '（' + x.note + '）' : ''),
        style: '--tone: var(' + Dash.colorVar('status', x.status) + ')' });
    }));
  }

  /* ---------- 系统脉搏：一行五个灯 ---------- */
  function scriptLight(live, name, label) {
    var s = (live.scripts || []).filter(function (x) { return x.name === name; })[0];
    if (!s) return { label: label, tone: ['level', 'bad'], sub: '没在进程里找到', href: '#health/health-procs' };
    return { label: label, tone: ['level', s.state === 'running' ? 'ok' : 'bad'], sub: s.note || (s.state === 'running' ? '在跑' : '停了'), href: '#health/health-procs' };
  }

  Dash.panel({
    id: 'now-pulse', page: 'now', title: '系统脉搏', note: '自动机制有没有在转：巡逻、来信监视、采集、钩子、构建锁；点灯去看细节', order: 5, needs: ['live'],
    render: function (el, d) {
      var l = d.live, b = l.build || {};
      var hooks = l.hooks || [];
      var flagged = hooks.filter(function (x) { return x.flag; }).length;
      el.appendChild(Dash.lights([
        scriptLight(l, '巡逻', '巡逻'),
        scriptLight(l, 'inbox_wait.sh', '来信监视'),
        { label: '采集', tone: ['level', Dash.isStale('live') ? 'bad' : 'ok'], sub: '实时数据 ' + Dash.at(l.updated), href: '#health/health-procs' },
        { label: '钩子', tone: ['level', flagged ? 'warn' : 'ok'], sub: flagged ? Dash.num(flagged) + ' 个要看' : Dash.num(hooks.length) + ' 个都正常', href: '#system/system-hooks' },
        { label: '构建锁', tone: ['level', b.lock === 'held' ? 'warn' : 'ok'], sub: b.lock === 'held' ? '被 ' + (b.holder || '某个进程') + ' 占用' : '空闲', href: '#health/health-procs' }
      ]));
    }
  });

  /* ---------- 需要你：按紧急、时刻排；"知道了"写回采集侧 ---------- */
  var KIND = { decision: ['tag', '需定', '需定'], review: ['tag', '待审', '待审'], shots: ['side', 'auto', '截图'], research: ['side', 'auto', '调研'], audit: ['tag', '待审', '审计'] };
  var URGENCY = { 3: ['risk', '要你拍板'], 2: ['warn', '今天看'], 1: ['muted', '有空看'] };
  var SHOW = 6;

  function ackButton(it) {
    var b = h('button.btn', { type: 'button' }, '知道了');
    b.addEventListener('click', function () {
      b.disabled = true;
      b.textContent = '写回中';
      Dash.ack(it.id, it.revision).catch(function (e) {
        b.disabled = false; b.textContent = '知道了'; b.classList.add('is-failed');
        var why = b.parentNode && b.parentNode.parentNode && b.parentNode.parentNode.querySelector('.att-why');
        if (why) why.textContent = '没写回去：' + String(e.message || e);
      });
    });
    return b;
  }

  function attRow(it) {
    var k = KIND[it.kind] || ['side', 'auto', it.kind];
    var u = URGENCY[it.urgency] || URGENCY[1];
    return h('div.att-row.is-u' + (it.urgency || 1) + (it.acked ? '.is-acked' : ''),
      h('div.att-lead', Dash.mark(k[0], k[1], k[2]), it.acked ? null : h('span.att-urg.t-' + u[0], u[1])),
      h('div.att-main',
        h('div.att-title', it.title),
        it.detail ? h('div.att-detail', it.detail) : null,
        h('div.att-why')),
      h('div.att-meta', it.from && it.from !== 'auto' ? Dash.mark('side', it.from) : Dash.mark('side', 'auto'), h('span.att-at', Dash.at(it.at))),
      h('div.att-act', it.link ? Dash.a(it.link, '打开', 'btn') : null, it.acked ? null : ackButton(it)));
  }

  Dash.panel({
    id: 'now-attention', page: 'now', title: '需要你', note: '要你拍板或过目的事：需定来信、作者级审计、待看截图、待审调研', span: 7, order: 10, needs: ['attention'],
    render: function (el, d) {
      var items = d.attention.items || [];
      var open = items.filter(function (x) { return !x.acked; })
        .sort(function (a, b) { return (b.urgency - a.urgency) || (Dash.parseTime(b.at) - Dash.parseTime(a.at)); });
      var done = items.filter(function (x) { return x.acked; });
      var urgent = open.filter(function (x) { return x.urgency >= 3; }).length;
      el.appendChild(Dash.status(!open.length ? '现在没有要你动手的事'
        : urgent ? Dash.num(urgent) + ' 件等你拍板，共 ' + Dash.num(open.length) + ' 条待办' : '没有急事，' + Dash.num(open.length) + ' 条待过目',
        urgent ? 'risk' : open.length ? 'warn' : 'ok'));
      if (open.length) el.appendChild(h('div.att-list', open.slice(0, SHOW).map(attRow)));
      if (open.length > SHOW) el.appendChild(Dash.fold('另有 ' + Dash.num(open.length - SHOW) + ' 条不急的', h('div.att-list', open.slice(SHOW).map(attRow))));
      if (done.length) el.appendChild(Dash.fold('已知道 ' + Dash.num(done.length) + ' 条', h('div.att-list', done.map(attRow))));
      if (Dash.demo) el.appendChild(h('p.muted.small.att-note', '演示数据：点"知道了"不写回'));
    }
  });

  /* ---------- 最近：5 次推送、3 条通知 ---------- */
  var LEVEL = { info: ['muted', '信息'], attention: ['warn', '留意'], decision: ['risk', '需定'] };

  Dash.panel({
    id: 'now-recent', page: 'now', title: '最近', note: '最近推上 main 的 5 次推送与 3 条通知', span: 5, order: 20, partial: true, needs: ['history', 'notices'],
    render: function (el, d) {
      var pushes = ((d.history && d.history.pushes) || []).slice().sort(function (a, b) { return Dash.parseTime(b.at) - Dash.parseTime(a.at); }).slice(0, 5);
      var notes = ((d.notices && d.notices.items) || []).filter(function (x) { return !Dash.expired(x); })
        .sort(function (a, b) { return Dash.parseTime(b.at) - Dash.parseTime(a.at); }).slice(0, 3);
      el.appendChild(h('h3.sub-title', '推送'));
      if (!d.history) el.appendChild(Dash.empty('等短期历史', '推送记录来自 history.pushes，等后端 C-239 写出 data/auto/history.js', true));
      else if (!pushes.length) el.appendChild(Dash.empty('48 小时内没有推送', '', true));
      else el.appendChild(h('div.push-list', pushes.map(function (p) {
        var rows = p.rows || [];
        return h('div.push-row', h('span.push-at', Dash.at(p.at)), h('span.push-hash.mono', p.hash),
          h('span.push-rows', rows.length ? rows.map(function (r, i) { return [i ? '、' : '', rowLink(r)]; }) : h('span.muted', '没对上队列行')));
      })));
      el.appendChild(h('h3.sub-title', '通知'));
      if (!d.notices) el.appendChild(Dash.empty('等通知数据', 'data/curated/notices.js 还没读到', true));
      else if (!notes.length) el.appendChild(Dash.empty('没有未过期的通知', '', true));
      else el.appendChild(h('div.note-list', notes.map(function (it) {
        var lv = LEVEL[it.level] || LEVEL.info;
        return h('div.note-row', h('span.note-dot.t-' + lv[0], { title: lv[1] }),
          h('span.note-text', it.link ? Dash.a(it.link, it.text) : it.text), h('span.note-at', Dash.at(it.at)));
      })));
      el.appendChild(h('div.more', h('a', { href: '#library/notices' }, '全部通知')));
    }
  });

  /* ---------- 两条指挥泳道：道上是在做的行，卡片下挂它的子代理（型号着色） ---------- */
  function agentChip(a) {
    var c = Dash.mark('model', Dash.modelKey(a.model), (a.name || '') + ' · ' + Dash.modelName(a.model));
    c.classList.add('agent-chip');
    c.insertBefore(h('i.agent-dot.is-' + (a.state === 'running' ? 'run' : 'done')), c.firstChild);
    c.title = Dash.agentLabel(a);
    return c;
  }

  function agentGroup(list) {
    var run = list.filter(function (a) { return a.state === 'running'; });
    var done = list.filter(function (a) { return a.state !== 'running'; });
    if (!list.length) return h('div.lane-none', '没有子代理');
    var doneChip = done.length ? h('span.chip.agent-done', { title: done.map(Dash.agentLabel).join('\n') }, '已交回 ' + Dash.num(done.length)) : null;
    return h('div.lane-agents', run.map(agentChip), doneChip);
  }

  function laneCard(it, prow) {
    var now = prow ? stageNow(prow) : null;
    return h('div.lane-card.is-' + sideKey(it.who),
      h('div.lane-card-head', rowLink(it.row, 'lane-id'), Dash.mark('queue', it.state),
        now ? h('span.lane-stage' + (now.status === 'fail' ? '.is-fail' : ''), stageText(now)) : null),
      prow ? stageStrip(prow) : null,
      h('div.lane-what', { title: it.what }, it.what),
      agentGroup(it.agents || []));
  }

  Dash.panel({
    id: 'now-lanes', page: 'now', title: '两条指挥泳道', note: '此刻两个指挥各在做哪些行、走到哪一步、挂着哪些子代理（实心点在跑）', order: 30, partial: true, needs: ['live', 'pipeline'],
    render: function (el, d) {
      if (!d.live) { el.appendChild(Dash.empty('等实时数据', '实时采集还没有写出运行状态', true)); return; }
      var gap = Dash.liveGap(d.live, 'work', ['在做的工作', '施工队列'], '队列里没有进行中或待编译的行');
      if (gap) { el.appendChild(gap); return; }
      var w = d.live.work, prow = {};
      ((d.pipeline && d.pipeline.rows) || []).forEach(function (r) { prow[r.id] = r; });
      ['claude', 'codex'].forEach(function (side) {
        var main = (w.mains || []).filter(function (m) { return sideKey(m.name) === side; })[0];
        var items = w.items.filter(function (it) { return sideKey(it.who) === side; });
        var loose = (w.loose || []).filter(function (a) { return a.side === side; });
        var running = items.reduce(function (n, it) { return n + (it.agents || []).filter(function (a) { return a.state === 'running'; }).length; }, 0)
          + loose.filter(function (a) { return a.state === 'running'; }).length;
        el.appendChild(h('div.lane.lane--' + side,
          h('div.lane-head',
            h('div.lane-who', Dash.mark('side', side, side === 'claude' ? 'Claude 主会话' : 'Codex 主线'),
              main ? h('span.lane-model', Dash.modelName(main.model) + (main.effort ? ' · ' + main.effort : '')) : null),
            h('div.lane-state', main ? (main.state === 'running' ? '在跑 · ' + Dash.at(main.since) + ' 起' : '空闲') : '没找到会话'),
            h('div.lane-count', h('b', Dash.num(items.length)), ' 行 · ', h('b', Dash.num(running)), ' 个子代理在跑')),
          h('div.lane-body',
            items.length ? h('div.lane-cards', items.map(function (it) { return laneCard(it, prow[it.row]); })) : h('div.lane-none', '这一道没有在做的行'),
            loose.length ? h('div.lane-loose', h('span.lane-loose-label', '没挂到行的子代理'), agentGroup(loose)) : null)));
      });
      if (!d.pipeline) el.appendChild(h('p.muted.small', '卡片上的阶段条等流水数据（pipeline，后端 C-239 写出 data/auto/pipeline.js 后出现）'));
    }
  });

  /* ---------- 本批流水缩略：没收口的行 × 阶段 ---------- */
  Dash.panel({
    id: 'now-flow', page: 'now', title: '本批流水', note: '还没收口的行各走到哪个阶段；点一行去施工页看它的历史', order: 40, needs: ['pipeline'],
    render: function (el, d) {
      var rows = (d.pipeline.rows || []).filter(function (r) {
        return Dash.stages.some(function (s) { var x = r.stages[s.id]; return !x || (x.status !== 'done' && x.status !== 'na'); });
      });
      el.appendChild(Dash.charts.swimlane({
        columns: Dash.stages,
        lanes: rows.map(function (r) { return { id: r.id, label: r.id, sub: Dash.gist(r.title).text, side: sideKey(r.who) }; }),
        cells: [].concat.apply([], rows.map(function (r) {
          return Dash.stages.map(function (s) { var x = r.stages[s.id] || { status: 'todo' }; return { lane: r.id, col: s.id, status: x.status, at: x.at, note: x.note }; });
        }))
      }, { compact: true, labelWidth: 230, legend: [['status']], empty: '没有没收口的行',
        onClick: function (it) { if (Dash.selected('row') !== it.lane.id) Dash.select('row', it.lane.id); Dash.go('work/work-flow'); } }));
    }
  });
})();
