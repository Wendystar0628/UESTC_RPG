/* 体系页：钩子、脚本、文档、检查怎么相互作用。全部关系由采集侧从项目现读（relations），这里只画。
   块（面板 id）：system-graph 关系图与选中节点的上下游 · system-hooks 钩子明细 · system-scripts 脚本明细。
   relations 只在打开这页时拉，catalogdetail 选中脚本时才拉；选中的脚本放在 Dash.select('script')（资料页的脚本目录也用它跳过来）。 */
(function () {
  var h = Dash.h;
  /* 分层：事件与时机 → 钩子 → 代码路径 → 文档 → 门禁；脚本 → 数据与产物；巡逻检查在最右，指回它读的脚本与数据 */
  var COLUMNS = [
    { kind: 'event', label: '事件与时机' }, { kind: 'hook', label: '钩子' }, { kind: 'path', label: '代码路径' }, { kind: 'doc', label: '文档' },
    { kind: 'gate', label: '门禁' }, { kind: 'script', label: '脚本' }, { kind: 'data', label: '数据与产物' }, { kind: 'check', label: '巡逻检查' }
  ];
  var KINDS = Object.keys(Dash.palette.node);
  /* 关系图的交互状态（选中、折叠、关掉的边种）：数据刷新重画后保留 */
  var gstate = {};
  var graph = null;    // 当前关系图节点（带 pick）

  /* 侧栏的小标题：同一种边，按方向与节点种类说成一句问话 */
  function inTitle(kind, n) {
    switch (kind) {
      case 'inject': return n.kind === 'hook' ? '挂在哪些事件上' : '哪些钩子注入它';
      case 'route': return '改到哪些路径时送来它';
      case 'govern': return '哪些文档管辖它';
      case 'readwrite': return '哪些脚本读写它';
      case 'generate': return '由哪些脚本生成';
      case 'call': return n.kind === 'check' ? '在什么时机跑' : '被哪些脚本调用';
      case 'check': return '哪些巡逻检查读它';
      case 'gate': return '由哪些文档规定';
    }
    return Dash.label('edge', kind);
  }
  function outTitle(kind, n) {
    switch (kind) {
      case 'inject': return n.kind === 'event' ? '触发哪些钩子' : '注入哪些文档';
      case 'route': return '路由到哪些文档';
      case 'govern': return '它管辖的代码路径';
      case 'readwrite': return '它读写的文档与数据';
      case 'generate': return '它生成的文件';
      case 'call': return n.kind === 'event' ? '这时跑哪些检查' : '它调用的脚本';
      case 'check': return '它检查的脚本与数据';
      case 'gate': return '它规定的门禁';
    }
    return Dash.label('edge', kind);
  }

  function index(r) {
    var byId = {};
    (r.nodes || []).forEach(function (n) { byId[n.id] = n; });
    var edges = (r.edges || []).filter(function (e) { return byId[e.from] && byId[e.to]; });
    return { byId: byId, edges: edges };
  }

  function whenOf(e) { return e.meta && e.meta.when || ''; }
  function scriptPath(id) { return String(id).replace(/^script:/, ''); }

  /* 钩子在某个时机由哪些事件触发：事件 → 钩子 的边带同一个 when */
  function eventsAt(ix, hookId, when) {
    return ix.edges.filter(function (e) { return e.to === hookId && e.kind === 'inject' && ix.byId[e.from].kind === 'event' && whenOf(e) === when; })
      .map(function (e) { return ix.byId[e.from].label; });
  }

  function jump(id) { if (graph && graph.pick) graph.pick(id); }

  function nodeButton(o, extra) {
    return h('button.sys-node', { type: 'button', title: o.label + (o.group ? ' · ' + o.group : ''), on: { click: function () { jump(o.id); } } },
      h('i.sys-dot', { style: '--tone: var(' + Dash.colorVar('node', o.kind) + ')' }), h('span.sys-node-name', o.label), extra || null);
  }

  /* 侧栏：选中节点的直接上下游，按边种说成一句话；钩子注入的文档写出时机与事件 */
  function side(box, r) {
    var ix = index(r), n = gstate.pick && ix.byId[gstate.pick];
    if (!n) {
      box.replaceChildren(Dash.empty('点图里的一个节点', '这里按种类列出它的上游与下游：谁在什么时机注入它、哪些路径路由到它、哪些脚本读写它……点列表里的名字接着顺藤摸瓜', true));
      return;
    }
    var head = h('div.sys-side-head', Dash.mark('node', n.kind), h('b.sys-side-name', n.label), n.group ? h('span.muted', n.group) : null);
    var acts = [];
    if (n.kind === 'script') acts.push(h('button.btn', { type: 'button', on: { click: function () { pickScript(scriptPath(n.id)); Dash.go('system/system-scripts'); } } }, '看脚本明细'));
    if (n.kind === 'hook') acts.push(h('button.btn', { type: 'button', on: { click: function () { Dash.go('system/system-hooks'); } } }, '看钩子明细'));
    if (n.meta && n.meta.path) acts.push(Dash.a(n.meta.path, '打开文件', 'btn'));
    var parts = [head, n.title ? h('div.muted.small', n.title) : null, acts.length ? h('div.cluster', acts) : null];
    [['in', 'to', 'from', inTitle], ['out', 'from', 'to', outTitle]].forEach(function (dir) {
      var mine = ix.edges.filter(function (e) { return e[dir[1]] === n.id; });
      var label = dir[0] === 'in' ? '上游' : '下游';
      parts.push(h('h3.sub-title.sys-dir', label + ' ' + Dash.num(mine.length)));
      if (!mine.length) { parts.push(h('div.muted.small', dir[0] === 'in' ? '没有指向它的边' : '它不指向别的节点')); return; }
      Object.keys(Dash.palette.edge).forEach(function (k) {
        var es = mine.filter(function (e) { return e.kind === k; });
        if (!es.length) return;
        parts.push(h('div.sys-rel', h('div.sys-rel-title', h('i.sys-bar', { style: '--tone: var(' + Dash.colorVar('edge', k) + ')' }), dir[3](k, n), h('span.muted', ' ' + Dash.num(es.length))),
          h('div.sys-rel-list', es.map(function (e) {
            var o = ix.byId[e[dir[2]]], when = whenOf(e), extra = null;
            if (when) {
              var evs = o.kind === 'hook' && dir[0] === 'in' ? eventsAt(ix, o.id, when) : (n.kind === 'hook' && dir[0] === 'out' ? eventsAt(ix, n.id, when) : []);
              extra = h('span.sys-when', when + (evs.length ? '（' + evs.join('、') + '）' : ''));
            }
            return nodeButton(o, extra);
          }))));
      });
    });
    box.replaceChildren.apply(box, parts.filter(Boolean));
  }

  Dash.panel({
    id: 'system-graph', page: 'system', title: '关系图', order: 10, needs: ['relations'],
    note: '谁触发谁、谁注入谁、谁读写谁。点节点高亮它的上下游、右侧列出明细；点段名（▸）展开折起来的一组；上方按边的种类开关',
    render: function (el, d) {
      var r = d.relations, ix = index(r);
      var count = {};
      (r.nodes || []).forEach(function (n) { count[n.kind] = (count[n.kind] || 0) + 1; });
      var line = Dash.statusLine('muted');
      line.body.textContent = Dash.num((r.nodes || []).length) + ' 个节点、' + Dash.num(ix.edges.length) + ' 条关系：' +
        KINDS.filter(function (k) { return count[k]; }).map(function (k) { return Dash.label('node', k) + ' ' + Dash.num(count[k]); }).join('、');
      el.appendChild(line);
      if (gstate.pick && !ix.byId[gstate.pick]) gstate.pick = null;
      var box = h('aside.sys-side');
      graph = Dash.charts.layered({ columns: COLUMNS, nodes: r.nodes || [], edges: ix.edges.map(function (e) {
        return Object.assign({ title: whenOf(e) ? '时机：' + whenOf(e) : '' }, e);
      }) }, { state: gstate, fold: ['doc', 'script', 'check'], foldOver: 16, through: ['event', 'hook', 'script'], key: 'system-graph',
        onClick: function () { side(box, r); } });
      el.appendChild(h('div.sys-graph', h('div.sys-main', graph), box));
      side(box, r);
    }
  });

  /* ---------- 钩子明细 ---------- */

  /* live.hooks 的一项对应关系图里的哪个钩子节点：同名，Codex 侧的标签带 ·codex */
  function hookNode(ix, x) {
    var hooks = Object.keys(ix.byId).map(function (id) { return ix.byId[id]; }).filter(function (n) { return n.kind === 'hook'; });
    function sideOf(n) { return n.meta && n.meta.side; }
    return hooks.filter(function (n) { return sideOf(n) ? sideOf(n) === x.side && (n.label === x.name || n.label === x.name + '·' + x.side) : n.label === x.name + '·' + x.side; })[0]
      || hooks.filter(function (n) { return !sideOf(n) && n.label === x.name; })[0] || null;
  }

  /* 时机表：钩子 → 文档 的注入边按 when 归并，配上同一时机触发它的事件 */
  function timingTable(ix, node) {
    var rows = {}, order = [];
    ix.edges.forEach(function (e) {
      if (e.kind !== 'inject' || (e.from !== node.id && e.to !== node.id)) return;
      var w = whenOf(e) || '—';
      if (!rows[w]) { rows[w] = { when: w, events: [], docs: [], meta: e.meta || {} }; order.push(w); }
      if (e.to === node.id) rows[w].events.push(ix.byId[e.from]);
      else rows[w].docs.push(ix.byId[e.to]);
    });
    return Dash.table([
      { label: '时机', cell: function (x) { return h('b', x.when); } },
      { label: '事件', cell: function (x) { return x.events.length ? h('span.mono', x.events.map(function (n) { return n.label; }).join('、')) : ''; } },
      { label: '注入的文档', cell: function (x) { return x.docs.length ? h('div.sys-chips', x.docs.map(function (n) { return nodeButton(n); })) : h('span.muted', '不注入文档'); } },
      { label: '条件与间隔', cell: function (x) { return [x.meta.cond, x.meta.every].filter(Boolean).join(' · '); } }
    ], order.map(function (w) { return rows[w]; }), { empty: '关系数据里没有这个钩子的时机' });
  }

  function stat(label, n, bad) {
    return h('div.sys-stat' + (bad && n ? '.is-bad' : ''), h('span.sys-stat-n', Dash.num(n)), h('span.sys-stat-l', label));
  }

  function hookCard(x, ix) {
    var b = x.h24 || {}, cut = (x.recent || []).filter(function (r) { return r.cut && r.cut.length; }).length;
    var node = ix ? hookNode(ix, x) : null;
    var stats = x.tracked ? h('div.sys-stats', stat('近 24 小时触发', b.n), stat('注入', b.inj), stat('报错', b.err, true), stat('挤掉', b.dropped, true), stat('取不到', b.miss, true),
      stat('最近 ' + (x.recent || []).length + ' 条里降级', cut, true)) : h('div.muted.small', '这个钩子不记账：' + (x.note || '没有触发记录'));
    var recent = (x.recent || []).filter(function (r) { return r.inj.length || r.err || r.dropped.length || r.miss.length || r.cut.length; });
    return h('section.sys-hook',
      h('div.sys-hook-head', Dash.mark('side', x.side), h('b.mono', x.name), x.desc ? h('span.muted', x.desc) : null,
        h('span.grow'), h('span.muted.small', x.last ? '最后触发 ' + Dash.at(x.last) : '没有触发记录')),
      x.flag ? h('div.sys-flag', x.flag) : null,
      h('div.muted.small', '挂的事件：' + (x.events || []).join('、')),
      stats,
      !ix ? Dash.empty('时机表等体系关系数据', '采集侧（C-239）由钩子 --describe 导出时机清单后出现', true) : node ? timingTable(ix, node) : Dash.empty('关系图里没有这个钩子', '', true),
      recent.length ? Dash.fold('最近有注入或异常的 ' + recent.length + ' 条', h('div.sys-recs', recent.map(function (r) {
        return h('div.sys-rec' + (r.err ? '.is-bad' : ''), h('span.mono.muted', Dash.at(r.at)), h('span', r.role + ' · ' + r.event + (r.tool ? ' ' + r.tool : '')),
          h('span.grow', r.err ? '报错：' + r.err : r.inj.join('、') + '（' + Dash.num(r.chars) + ' 字）'),
          r.dropped.length || r.miss.length || r.cut.length ? h('span.sys-warn', [r.dropped.length ? '挤掉 ' + r.dropped.join('、') : '', r.miss.length ? '取不到 ' + r.miss.join('、') : '', r.cut.length ? '降级 ' + r.cut.join('、') : ''].filter(Boolean).join('；')) : null);
      }))) : null);
  }

  function injCharts(hist) {
    var since = Date.now() - 24 * 3600 * 1000;
    var rows = ((hist && hist.hookInj) || []).filter(function (x) { return Dash.parseTime(x.at) >= since; });
    var per = {}, hours = {}, hourKeys = [];
    rows.forEach(function (x) {
      var side = x.side === 'codex' ? 'codex' : 'claude', inj = 0;
      Object.keys(x.inj || {}).forEach(function (k) {
        per[k] = per[k] || { claude: 0, codex: 0 };
        per[k][side] += x.inj[k];
        inj += x.inj[k];
      });
      var t = new Date(Dash.parseTime(x.at)); t.setMinutes(0, 0, 0);
      var hk = Dash.at(t.getTime());
      if (!hours[hk]) { hours[hk] = { fired: 0, inj: 0, t: t.getTime() }; hourKeys.push(hk); }
      hours[hk].fired += x.fired || 0;
      hours[hk].inj += inj;
    });
    hourKeys.sort(function (a, b) { return hours[a].t - hours[b].t; });
    var docs = Object.keys(per).sort(function (a, b) { return (per[b].claude + per[b].codex) - (per[a].claude + per[a].codex); });
    var empty = hist ? '近 24 小时没有注入记录' : '等短期历史（history.hookInj，C-239）';
    return h('div.sys-hook-charts',
      h('div', h('h3.sub-title', '近 24 小时各文档被注入的次数'), h('p.muted.small', '按 Claude、Codex 两侧堆叠，最多的在上'),
        Dash.charts.bars({ categories: docs, horizontal: true, stack: true, series: [
          { name: 'Claude', tone: ['side', 'claude'], values: docs.map(function (k) { return per[k].claude; }) },
          { name: 'Codex', tone: ['side', 'codex'], values: docs.map(function (k) { return per[k].codex; }) }] },
          { unit: '次', labelWidth: 150, empty: empty, legend: [['side', ['claude', 'codex']]], key: 'system-inj-docs' })),
      h('div', h('h3.sub-title', '每小时触发与注入'), h('p.muted.small', '触发是钩子被调用的次数，注入是真正送出文档的次数'),
        Dash.charts.bars({ categories: hourKeys, series: [
          { name: '触发', tone: ['node', 'event'], values: hourKeys.map(function (k) { return hours[k].fired; }) },
          { name: '注入', tone: ['node', 'hook'], values: hourKeys.map(function (k) { return hours[k].inj; }) }] },
          { unit: '次', height: 220, empty: empty, key: 'system-inj-hours' })));
  }

  Dash.panel({
    id: 'system-hooks', page: 'system', title: '钩子明细', order: 20, partial: true, needs: ['live', 'relations', 'history'],
    note: '每个钩子挂在哪些事件、什么时机注入哪些文档，近 24 小时注入了多少、有没有挤掉和降级',
    render: function (el, d) {
      var ix = d.relations ? index(d.relations) : null;
      var list = d.live && d.live.hooks;
      var gap = d.live ? Dash.liveGap(d.live, 'hooks', ['钩子'], '没有登记的钩子') : Dash.empty('等实时数据', '钩子的触发与注入记账在 live 里', true);
      var bad = (list || []).filter(function (x) { return x.flag; }).length;
      if (list) {
        var line = Dash.statusLine(bad ? 'warn' : 'ok');
        line.body.textContent = Dash.num(list.length) + ' 个钩子' + (bad ? '，' + bad + ' 个要看' : '，都在正常注入');
        el.appendChild(line);
      }
      el.appendChild(h('div.sys-hooks', h('div.sys-hook-list', gap || list.map(function (x) { return hookCard(x, ix); })), injCharts(d.history)));
    }
  });

  /* ---------- 脚本明细 ---------- */

  function flatScripts(catalog) {
    var out = [];
    (catalog.groups || []).forEach(function (g) {
      (function walk(dir) {
        (dir.scripts || []).forEach(function (s) { out.push(Object.assign({ group: g.label }, s)); });
        (dir.dirs || []).forEach(walk);
      })(g);
    });
    return out;
  }

  /* 选中一个脚本（已选中的不取消） */
  function pickScript(path) { if (Dash.selected('script') !== path) Dash.select('script', path); }

  /* history.runs 里的 script 是文件名：按名字对到目录里的脚本 */
  function runsOf(hist, s) { return ((hist && hist.runs) || []).filter(function (r) { return r.script === s.name || r.script === s.path; }); }

  function runTone(r) { return r.event === 'start' ? ['status', 'doing'] : ['status', r.exitCode === 0 ? 'done' : 'fail']; }

  function runsTimeline(hist, list) {
    var runs = ((hist && hist.runs) || []).filter(function (r) { return !isNaN(Dash.parseTime(r.at)); });
    var names = [];
    runs.forEach(function (r) { if (names.indexOf(r.script) < 0) names.push(r.script); });
    names.sort();
    /* 结束记录画成一段（开始到结束）；还没结束的开始记录画成一个点 */
    function open(r) { return !runs.some(function (x) { return x.script === r.script && x.event !== 'start' && Dash.parseTime(x.at) >= Dash.parseTime(r.at); }); }
    var items = runs.filter(function (r) { return r.event !== 'start' || open(r); }).map(function (r) {
      var t = Dash.parseTime(r.at), dur = r.durationSeconds || 0;
      var label = r.script + (r.event === 'start' ? ' 开始' : ' · 退出码 ' + r.exitCode + ' · ' + Dash.num(dur) + ' 秒');
      return r.event === 'start' ? { lane: r.script, at: r.at, tone: runTone(r), label: label, script: r.script }
        : { lane: r.script, at: new Date(t - dur * 1000).toISOString(), end: r.at, tone: runTone(r), label: label, script: r.script };
    });
    return Dash.charts.timeline({ lanes: names.map(function (n) { return { id: n, label: n }; }), items: items },
      { rowHeight: 26, labelWidth: 120, symbolSize: 9, legend: [['status', ['done', 'fail', 'doing']]], key: 'system-runs',
        empty: hist ? '近 48 小时没有运行记录' : '等短期历史（history.runs，C-239 给常用脚本记开始与结束）',
        onClick: function (it) { var s = list.filter(function (x) { return x.name === it.script; })[0]; if (s) pickScript(s.path); } });
  }

  function relList(title, nodes) {
    if (!nodes.length) return null;
    return h('div.sys-rel', h('div.sys-rel-title', title, h('span.muted', ' ' + Dash.num(nodes.length))), h('div.sys-rel-list', nodes.map(function (o) {
      return o.id ? nodeButton(o) : h('span.sys-node.is-plain', h('span.sys-node-name', o.label));
    })));
  }

  function scriptDetail(s, d, ix) {
    var det = Dash.get('catalogdetail');
    var item = det && (det.items || []).filter(function (x) { return x.path === s.path; })[0];
    var id = 'script:' + s.path, node = ix && ix.byId[id];
    var es = ix ? ix.edges : [];
    function others(dir, kinds) {
      return es.filter(function (e) { return (dir === 'in' ? e.to : e.from) === id && kinds.indexOf(e.kind) >= 0; }).map(function (e) { return ix.byId[dir === 'in' ? e.from : e.to]; });
    }
    var callers = others('in', ['call']);
    ((item && item.callers) || []).forEach(function (c) {
      if (!callers.some(function (o) { return o.label === c || scriptPath(o.id) === c; })) callers.push({ label: c });
    });
    var runs = runsOf(d.history, s).filter(function (r) { return r.event !== 'start'; }).sort(function (a, b) { return Dash.parseTime(b.at) - Dash.parseTime(a.at); });
    var last = runs[0] || (item && item.lastRun);
    var parts = [
      h('div.sys-side-head', Dash.mark('node', 'script'), h('b.sys-side-name.mono', s.name), h('span.muted', s.group)),
      h('div.cluster', Dash.a(s.path, s.path, 'mono small'), node ? h('button.btn', { type: 'button', on: { click: function () { jump(id); Dash.go('system/system-graph'); } } }, '在关系图里看') : null),
      h('p.sys-purpose', s.purpose || '没写开头说明'),
      h('div.muted.small', last ? '最近一次运行 ' + Dash.at(last.at) + (last.exitCode != null ? ' · 退出码 ' + last.exitCode : '') + (last.durationSeconds != null ? ' · ' + Dash.num(last.durationSeconds) + ' 秒' : '')
        : (d.history ? '近 48 小时没有运行记录' : '运行记录等短期历史（history.runs）')),
      !det ? Dash.charts.loading(60) : !item ? Dash.empty('脚本详情里没有这个脚本', '', true) : (item.usage ? h('pre.pre.sys-usage', item.usage) : h('div.muted.small', '开头说明里没写用法')),
      ix ? null : Dash.empty('读写与调用关系等体系关系数据', '', true),
      relList('被谁调用', callers), relList('它调用', others('out', ['call'])), relList('它读写', others('out', ['readwrite'])),
      relList('它生成', others('out', ['generate'])), relList('哪些巡逻检查读它', others('in', ['check']))
    ];
    return parts.filter(Boolean);
  }

  var groupPick = 'all';
  Dash.panel({
    id: 'system-scripts', page: 'system', title: '脚本明细', order: 30, partial: true, needs: ['catalog', 'history', 'relations'], uses: ['catalogdetail'], watch: ['script'],
    note: '常用脚本近 48 小时的运行，下面是全部脚本；点一行看用法、谁调用它、它读写什么',
    render: function (el, d) {
      if (!d.catalog) { el.appendChild(Dash.empty('等脚本目录', 'catalog 还没有数据', true)); return; }
      var ix = d.relations ? index(d.relations) : null;
      var list = flatScripts(d.catalog);
      var pick = Dash.selected('script'), cur = list.filter(function (s) { return s.path === pick; })[0];
      if (cur) Dash.need(['catalogdetail']);
      var callIn = {};
      if (ix) ix.edges.forEach(function (e) { if (e.kind === 'call' && ix.byId[e.from].kind === 'script') callIn[e.to] = (callIn[e.to] || 0) + 1; });
      var lastRun = {};
      ((d.history && d.history.runs) || []).forEach(function (r) { if (r.event !== 'start' && (!lastRun[r.script] || Dash.parseTime(r.at) > Dash.parseTime(lastRun[r.script].at))) lastRun[r.script] = r; });
      el.appendChild(h('h3.sub-title', '近 48 小时的运行'));
      el.appendChild(runsTimeline(d.history, list));
      var groups = [];
      list.forEach(function (s) { if (groups.indexOf(s.group) < 0) groups.push(s.group); });
      if (groupPick !== 'all' && groups.indexOf(groupPick) < 0) groupPick = 'all';
      var shown = list.filter(function (s) { return groupPick === 'all' || s.group === groupPick; });
      var table = Dash.table([
        { label: '脚本', cell: function (s) { return h('span.mono', s.name); } },
        { label: '作用', cell: function (s) { return s.purpose ? Dash.gist(s.purpose).text : h('span.muted', '没写开头说明'); } },
        { label: '被调用', num: true, cell: function (s) { return ix ? Dash.num(callIn['script:' + s.path] || 0) : ''; } },
        { label: '最近运行', num: true, cell: function (s) { var r = lastRun[s.name] || lastRun[s.path]; return r ? h('span', { cls: r.exitCode === 0 ? '' : 'sys-fail' }, Dash.at(r.at) + (r.exitCode === 0 ? '' : ' · 退出码 ' + r.exitCode)) : ''; } }
      ], shown, { onRow: function (s) { pickScript(s.path); }, selected: function (s) { return s.path === pick; } });
      el.appendChild(h('div.sys-scripts',
        h('div.sys-scripts-list',
          Dash.segment([{ id: 'all', label: '全部 ' + list.length }].concat(groups.map(function (g) { return { id: g, label: g + ' ' + list.filter(function (s) { return s.group === g; }).length }; })),
            groupPick, function (id) { groupPick = id; Dash.redraw('system-scripts'); }),
          table),
        h('aside.sys-side.sys-script-side', cur ? scriptDetail(cur, d, ix) : Dash.empty('点左边一行', '这里显示它的作用、用法、谁调用它、它读写和生成什么、最近一次运行', true))));
    }
  });
})();
