/* 健康页：系统哪里不对。检查由 .collab/tools/monitor/check/ 产生，巡逻读同一份；这里只画。
   块（面板 id）：health-matrix 检查矩阵与 24 小时条带 · health-detail 选中检查的细节与建议 · health-checks 全部检查 · health-procs 后台进程与构建锁。
   选中的检查放在 Dash.select('check')，条带上选中的一格放在 Dash.select('healthAt')（值为"格的下标,组的下标"）。 */
(function () {
  var h = Dash.h;
  var GROUPS = ['测试与门禁', '代码与文档', '工作区与推送', '协作流程', '后台与钩子', '会话与子代理'];
  var RANK = { bad: 0, warn: 1, ok: 2 };
  var DAY = 24 * 3600 * 1000;

  function groupsOf(checks) {
    var gs = GROUPS.filter(function (g) { return checks.some(function (c) { return c.group === g; }); });
    if (checks.some(function (c) { return GROUPS.indexOf(c.group) < 0; })) gs.push('其他');
    return gs;
  }
  function groupKey(c) { return GROUPS.indexOf(c.group) >= 0 ? c.group : '其他'; }
  /* 格子里写的短名：去掉括号里的补充 */
  function shortName(c) { return String(c.title || c.id).replace(/（[^）]*）|\([^)]*\)/g, '').trim(); }
  function byLevel(a, b) { return RANK[a.level] - RANK[b.level]; }
  function recent(hist) { return ((hist && hist.health) || []).filter(function (x) { return Date.now() - Dash.parseTime(x.at) < DAY; }); }
  function tone(level) { return 'var(' + Dash.colorVar('level', level) + ')'; }

  /* 当前要看的检查：选中的，没选就取最要紧的一项 */
  function current(checks) {
    var id = Dash.selected('check');
    var c = checks.filter(function (x) { return x.id === id; })[0];
    return c ? { c: c, auto: false } : { c: checks.slice().sort(byLevel)[0], auto: true };
  }

  function pick(id) {
    if (Dash.selected('check') !== id) Dash.select('check', id);
    Dash.go('health/health-detail');
  }

  function tile(c, on) {
    var big = c.level !== 'ok';
    return h('button.hl-tile' + (big ? '.is-big' : '') + (on ? '.is-on' : ''), { type: 'button', title: c.title + '：' + c.summary, style: '--tone: ' + tone(c.level),
      on: { click: function () { pick(c.id); } } },
      h('span.hl-tile-name', shortName(c)), big ? h('span.hl-tile-sum', Dash.gist(c.summary).text) : null);
  }

  Dash.panel({
    id: 'health-matrix', page: 'health', title: '检查矩阵', order: 10, partial: true, needs: ['health', 'history'], watch: ['check', 'healthAt'],
    note: '6 组检查此刻的状态：红、橙的排在每组最前、放大写出结论；下面是近 24 小时每次采集的条带（每组取最差）。点格子看细节与建议',
    render: function (el, d) {
      var checks = (d.health && d.health.checks) || [];
      if (!d.health) { el.appendChild(Dash.empty('等健康检查', 'health 还没有数据', true)); return; }
      var bad = checks.filter(function (c) { return c.level === 'bad'; }).length, warn = checks.filter(function (c) { return c.level === 'warn'; }).length;
      el.appendChild(bad || warn
        ? Dash.status((bad ? bad + ' 项要你动手' : '') + (bad && warn ? '，' : '') + (warn ? warn + ' 项要看' : '') + '；其余 ' + (checks.length - bad - warn) + ' 项正常', bad ? 'risk' : 'warn')
        : Dash.status('全部 ' + checks.length + ' 项正常', 'ok'));
      var cur = current(checks), onId = cur.auto ? null : cur.c.id;
      el.appendChild(h('div.hl-matrix', groupsOf(checks).map(function (g) {
        var list = checks.filter(function (c) { return groupKey(c) === g; }).sort(byLevel);
        var nb = list.filter(function (c) { return c.level !== 'ok'; }).length;
        return h('div.hl-mrow',
          h('div.hl-mhead', h('span.hl-mname', g), h('span.hl-mcount' + (nb ? '.is-flag' : ''), nb ? nb + ' 项要看 / ' + list.length : list.length + ' 项正常')),
          h('div.hl-tiles', list.map(function (c) { return tile(c, c.id === onId); })));
      })));
      el.appendChild(Dash.legend('level'));
      el.appendChild(strip(d, checks));
    }
  });

  /* 24 小时条带：一行一组、一格一次采集，取这一组那一刻最差的等级；点格子在下面列出那一刻不正常的检查 */
  function strip(d, checks) {
    var box = h('div.hl-strip', h('h3.sub-title', '近 24 小时'));
    var hist = recent(d.history);
    if (!hist.length) { box.appendChild(Dash.empty(d.history ? '近 24 小时没有采集记录' : '等短期历史', d.history ? '' : 'history.health 由采集侧（C-239）每轮记一格，保留 48 小时', true)); return box; }
    var gs = groupsOf(checks), groupOf = {}, titleOf = {};
    checks.forEach(function (c) { groupOf[c.id] = groupKey(c); titleOf[c.id] = c.title; });
    var cells = [];
    hist.forEach(function (snap, x) {
      gs.forEach(function (g, y) {
        var worst = 'ok', n = 0;
        Object.keys(snap.checks || {}).forEach(function (id) {
          var lv = snap.checks[id];
          if (groupOf[id] !== g || !(lv in RANK)) return;
          if (lv !== 'ok') n++;
          if (RANK[lv] < RANK[worst]) worst = lv;
        });
        cells.push({ x: x, y: y, tone: ['level', worst], title: Dash.at(snap.at) + (n ? ' · ' + n + ' 项不正常' : ' · 全部正常') });
      });
    });
    box.appendChild(Dash.charts.heatmap({ x: hist.map(function (s) { return Dash.at(s.at); }), y: gs, cells: cells, mode: 'tone' },
      { cellHeight: 20, labelWidth: 110, xInterval: Math.max(0, Math.ceil(hist.length / 12) - 1), key: 'health-strip',
        onClick: function (c) { Dash.select('healthAt', c.x + ',' + c.y); } }));
    var at = Dash.selected('healthAt');
    if (at) {
      var xy = at.split(',').map(Number), snap = hist[xy[0]], g = gs[xy[1]];
      if (snap && g) {
        var odd = Object.keys(snap.checks || {}).filter(function (id) { return groupOf[id] === g && snap.checks[id] !== 'ok'; })
          .sort(function (a, b) { return RANK[snap.checks[a]] - RANK[snap.checks[b]]; });
        box.appendChild(h('div.hl-at', h('b', Dash.at(snap.at) + ' · ' + g), odd.length ? odd.map(function (id) {
          return h('button.hl-chip', { type: 'button', style: '--tone: ' + tone(snap.checks[id]), on: { click: function () { pick(id); } } }, titleOf[id] || id);
        }) : h('span.muted', '这一刻全部正常')));
      }
    }
    return box;
  }

  function detailRow(x) {
    return h('div.hl-detail', h('span.hl-dtext', x.link ? Dash.a(x.link, x.text) : x.text), x.meta ? h('span.hl-dmeta', x.meta) : null);
  }

  /* 细节面板里的一条明细：附注长（如构建日志）就折起来放在下面，不挤在右边 */
  function longRow(x) {
    var text = x.link ? Dash.a(x.link, x.text) : x.text;
    if (!x.meta || String(x.meta).length <= 160) return detailRow(x);
    return h('div.hl-dlong', h('div.hl-dtext', text), Dash.fold('附注 ' + Dash.num(String(x.meta).length) + ' 字', h('pre.pre.hl-log', x.meta)));
  }

  /* 一项检查近 24 小时的等级：一格一次采集 */
  function ownStrip(hist, id) {
    var snaps = recent(hist).filter(function (s) { return s.checks && s.checks[id]; });
    if (!snaps.length) return h('div.muted.small', hist ? '近 24 小时的采集里没有这一项' : '24 小时记录等短期历史（history.health）');
    var n = snaps.filter(function (s) { return s.checks[id] !== 'ok'; }).length;
    return h('div', h('div.muted.small', '近 24 小时 ' + snaps.length + ' 次采集，' + (n ? n + ' 次不正常' : '每次都正常')),
      h('div.hl-own', snaps.map(function (s) { return h('i', { style: '--tone: ' + tone(s.checks[id]), title: Dash.at(s.at) + ' · ' + Dash.label('level', s.checks[id]) }); })));
  }

  Dash.panel({
    id: 'health-detail', page: 'health', title: '细节与建议', order: 15, partial: true, needs: ['health', 'history'], watch: ['check'],
    note: '选中一项检查的结论、明细、建议和它自己近 24 小时的状态；没选时显示最要紧的一项',
    render: function (el, d) {
      var checks = (d.health && d.health.checks) || [];
      var cur = current(checks), c = cur.c;
      if (!c) { el.appendChild(Dash.empty('没有检查', '', true)); return; }
      el.appendChild(h('div.hl-dhead', Dash.mark('level', c.level), h('b.hl-dtitle', c.title), h('span.muted', c.group), cur.auto ? h('span.muted.small', '（没选时显示最要紧的一项）') : null));
      el.appendChild(h('p.hl-dsum' + (c.level !== 'ok' ? '.is-' + c.level : ''), c.summary));
      if (c.tip) el.appendChild(h('div.hl-tip', h('b', '建议：'), c.tip));
      if ((c.details || []).length) el.appendChild(h('div.hl-dlist', c.details.map(longRow)));
      if (c.link) el.appendChild(h('div', Dash.a(c.link, '打开相关文件')));
      el.appendChild(ownStrip(d.history, c.id));
    }
  });

  function checkRow(c, open) {
    var body = h('div.hl-body');
    (c.details || []).forEach(function (x) { body.appendChild(detailRow(x)); });
    if (c.tip) body.appendChild(h('div.hl-tip', h('b', '建议：'), c.tip));
    if (c.link) body.appendChild(h('div.hl-detail', Dash.a(c.link, '打开相关文件')));
    var empty = !body.childNodes.length;
    var item = h('details.hl-item' + (open ? '.is-picked' : ''),
      h('summary.hl-sum', h('span.dot', { style: '--tone: ' + tone(c.level) }), h('span.hl-title', c.title),
        h('span.hl-summary' + (c.level !== 'ok' ? '.is-' + c.level : ''), c.summary), empty ? null : h('span.hl-caret')),
      empty ? null : body);
    if (open) item.open = true;
    return item;
  }

  Dash.panel({
    id: 'health-checks', page: 'health', title: '全部检查', note: '每组要看的在前；点开看明细、建议与链接', order: 20, needs: ['health'], watch: ['check'],
    render: function (el, d) {
      var checks = d.health.checks || [], pickId = Dash.selected('check');
      el.appendChild(h('div.hl-groups', groupsOf(checks).map(function (g) {
        var list = checks.filter(function (c) { return groupKey(c) === g; }).sort(byLevel);
        var n = list.filter(function (c) { return c.level !== 'ok'; }).length;
        return h('section.hl-group', h('h3.sub-title', g, h('span.muted', '  ' + (list.length - n) + ' 项正常' + (n ? '，' + n + ' 项要看' : ''))),
          h('div.hl-list', list.map(function (c) { return checkRow(c, c.id === pickId); })));
      })));
    }
  });

  Dash.panel({
    id: 'health-procs', page: 'health', title: '后台进程与构建锁', order: 30, needs: ['live'],
    note: '该常驻的脚本在不在跑、构建锁被谁占着、有没有几天前留下的 Godot 进程',
    render: function (el, d) {
      var l = d.live, b = l.build || {};
      var items = [{ label: '构建锁', tone: ['level', b.lock === 'held' ? 'warn' : 'ok'], sub: b.lock === 'held' ? '被 ' + (b.holder || '某个进程') + ' 占用' : '空闲' }];
      (l.scripts || []).forEach(function (s) {
        items.push({ label: s.name, tone: ['level', s.state === 'running' ? 'ok' : 'bad'], sub: s.state === 'running' ? (s.kind === 'cron' ? '上次 ' + Dash.at(s.since) : Dash.at(s.since) + ' 起') : '没在跑' });
      });
      el.appendChild(Dash.lights(items));
      el.appendChild(Dash.scripts(l));
    }
  });
})();
