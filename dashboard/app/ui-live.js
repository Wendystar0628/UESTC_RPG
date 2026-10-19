/* 实时状态的共用渲染：总览的"谁在干活"与模型页是同一个函数 Dash.who；后台脚本列表 Dash.scripts 总览与模型页共用。
   依赖 core.js、ui.js 先加载；数据形状见 dashboard/README.md §2.1 的 live。 */
(function () {
  'use strict';

  var Dash = window.Dash;
  var h = Dash.h;

  /* 从 iso 时刻到现在的整分钟数，写成"12 分钟""1 小时 5 分钟"（确切值，不用"几分钟"） */
  Dash.elapsed = function (iso) {
    var t = Dash.parseTime(iso);
    if (isNaN(t)) return '—';
    var m = Math.max(0, Math.floor((Date.now() - t) / 60000));
    if (m < 1) return '不到 1 分钟';
    return m < 60 ? m + ' 分钟' : Math.floor(m / 60) + ' 小时 ' + (m % 60) + ' 分钟';
  };

  var STATE = { running: '在跑', idle: '空闲', done: '已交回', unknown: '状态未知' };

  /* 一条文字：不长（约 120 字以内）直接全文；真长的先给一句话摘要（第一句，没有句号就取前 120 字），点开看全文。不靠省略号硬截。 */
  var TEXT_SHORT = 120;
  /* 返回 { text: 摘要（不长就是全文）, long: 是否真长 } */
  Dash.gist = function (text) {
    text = String(text == null ? '' : text);
    if (text.length <= TEXT_SHORT) return { text: text, long: false };
    var m = /^[^。；！？.!?\n]{8,}?[。；！？]/.exec(text);
    if (m && m[0].length <= TEXT_SHORT) return { text: m[0], long: true };
    var cut = text.slice(0, TEXT_SHORT), at = Math.max(cut.lastIndexOf('，'), cut.lastIndexOf('；'), cut.lastIndexOf('、'), cut.lastIndexOf(', '));
    return { text: at >= 24 ? cut.slice(0, at) : cut, long: true };   // 第一句太长：落在最后一个逗号前，不在词中间断
  };
  Dash.longText = function (text, cls) {
    var g = Dash.gist(text);
    if (!g.long) return h('span' + (cls ? '.' + cls : ''), g.text);
    return h('details.longtext' + (cls ? '.' + cls : ''), h('summary', g.text + '（展开全文）'), h('div.longtext-body', String(text)));
  };

  /* 实时数据某一项没有内容时，说清真实原因：没有这份数据 / 数据里没有这个字段（旧版采集脚本）/ 读源失败（用采集记下的原文）/ 有字段但确实为空。
     key 是 live 里的字段，labels 是 errors 里出错信息开头的块名；有内容返回 null（调用方正常渲染），否则返回要显示的节点。 */
  Dash.liveGap = function (live, key, labels, emptyText) {
    if (!live) return Dash.empty('等数据', '实时采集还没有写出运行状态', true);
    var errs = (live.errors || []).filter(function (e) { return labels.some(function (l) { return e.indexOf(l + '：') === 0; }); });
    if (errs.length) return Dash.empty('这一项读不到', errs.join('；'), true);
    if (live[key] === undefined) return Dash.empty('采集数据里还没有这一项', '采集脚本可能是旧版本，数据时刻 ' + Dash.at(live.updated), true);
    if (live[key] === null) return Dash.empty('这一项读不到', '采集没有给出原因，数据时刻 ' + Dash.at(live.updated), true);
    var v = live[key];
    if (Array.isArray(v) ? !v.length : (v && v.items && !v.items.length && !(v.mains && v.mains.length))) return Dash.empty(emptyText, '', true);
    return null;
  };

  /* 在跑的写"HH:MM 起，已 N 分钟"，已交回的写"HH:MM 交回" */
  Dash.sinceText = function (a) {
    if (a.state === 'unknown') return '记录不足，状态未知';
    if (a.stalled) return '未收尾，最后记录 ' + Dash.at(a.last) + '（可能卡住）';
    if (a.state === 'done') return Dash.at(a.last) + ' 交回';
    return Dash.at(a.started || a.since) + ' 起，已 ' + Dash.elapsed(a.started || a.since);
  };

  /* 子代理的签条文字：名字 · 型号 · 推理档 · 在跑/已交回 · 开始多久 */
  Dash.agentLabel = function (a) {
    return [a.side === 'codex' ? 'Codex' : 'Claude', a.name, Dash.modelName(a.model), a.effort, a.stalled ? '未收尾、可能卡住' : STATE[a.state] || a.state, Dash.sinceText(a)]
      .filter(Boolean).join(' · ');
  };

  function dot(kind) { return h('span.who-dot.who-dot--' + kind); }

  function agentRow(a) {
    var title = a.row || a.name;
    var task = a.task && a.task.indexOf(a.row) === 0 ? a.task.slice(a.row.length).replace(/^\s+/, '') : a.task;
    return h('div.who-row.who-agent.is-' + a.state,
      dot(a.state),
      h('span.who-name', title),
      h('span.who-task', { title: a.task || '', on: { click: function (e) { e.currentTarget.classList.toggle('is-open'); } } }, task || (a.row ? a.name : '')),
      h('span.who-sigs', Dash.chip(Dash.modelName(a.model), a.side), a.effort ? Dash.chip(a.effort, 'muted') : null),
      h('span.who-when', Dash.sinceText(a)));
  }

  function head(s) {
    return h('div.who-row.who-main.is-' + s.state,
      dot(s.state),
      h('span.who-name', s.name),
      h('span.who-sigs', Dash.chip(Dash.modelName(s.model) || '—', s.side), s.effort ? Dash.chip(s.effort, 'muted') : null,
        Dash.chip(STATE[s.state] || s.state, s.state === 'running' ? 'ok' : 'muted')),
      h('span.who-when', Dash.at(s.since) + (s.state === 'running' ? ' 起' : '')),
      s.task ? h('div.who-main-task', Dash.longText(s.task)) : null);
  }

  function group(root, kids) {
    var run = kids.filter(function (a) { return a.state !== 'done'; });
    var done = kids.filter(function (a) { return a.state === 'done'; });
    var box = h('div.who-group.who-group--' + root.side, root.virtual ? h('div.who-orphan', root.name) : head(root));
    run.forEach(function (a) { box.appendChild(agentRow(a)); });
    if (!run.length && !root.virtual) box.appendChild(h('div.who-none', '没有在跑的子代理'));
    if (done.length) box.appendChild(Dash.fold('另有 ' + done.length + ' 个已交回', done.map(agentRow)));
    return box;
  }

  /* 会话 → 它名下的子代理：每个会话一块，在跑的逐个列出（行号或名字、型号、推理档、开始多久），已交回的折叠 */
  Dash.who = function (live) {
    var sessions = live.sessions || [], agents = live.agents || [];
    var owners = {};
    sessions.forEach(function (s) { owners[s.id] = s.id; });
    var changed = true;
    while (changed) {
      changed = false;
      agents.forEach(function (a) { if (!owners[a.id] && owners[a.parent]) { owners[a.id] = owners[a.parent]; changed = true; } });
    }
    var box = h('div.who');
    sessions.forEach(function (s) {
      box.appendChild(group(s, agents.filter(function (a) { return owners[a.id] === s.id; })));
    });
    var orphans = agents.filter(function (a) { return !owners[a.id]; });
    if (orphans.length) box.appendChild(group({ name: '没有挂到会话的子代理', side: 'claude', virtual: true }, orphans));
    if (!sessions.length && !orphans.length) {
      box.appendChild(Dash.liveGap(live, 'sessions', ['Codex', 'Claude'], '没有会话') || Dash.empty('没有会话', '', true));
    }
    return box;
  };

  /* ---------- 后台脚本 ---------- */

  function runLine(s) {
    var running = s.state === 'running';
    var kind = s.kind === 'cron' ? 'cron' : s.kind;
    var tone = running ? 'running' : 'stopped';
    var row = h('div.sc-row.is-' + tone,
      dot(tone),
      h('span.sc-name', s.name),
      s.where ? h('span.sc-where', s.where) : null,
      h('span.sc-cmd', s.cmd && s.cmd !== s.name ? s.cmd : (running ? '' : s.note)),
      h('span.sc-when', running && s.since && kind !== 'cron' ? Dash.at(s.since) + ' 起，' + s.note : (kind === 'cron' ? s.note : '')));
    if (s.stage || s.testing) {
      row.appendChild(h('span.sc-extra', [s.stage ? '现在在 ' + s.stage : '', s.testing || ''].filter(Boolean).join(' · ')));
    }
    return row;
  }

  /* 不是命令行任务的 Godot 进程：个数加各自的开始时刻（几天前留下的一眼能看出来），读不到命令行的写明 */
  function editorsText(list) {
    if (!Array.isArray(list) || !list.length) return '';
    var unread = list.filter(function (e) { return !e.cmd; }).length;
    return '另有 Godot 进程 ' + list.length + ' 个（' + list.map(function (e) { return Dash.at(e.since) + ' 起'; }).join('、') + '）' +
      (unread ? '，' + (unread === list.length ? '' : unread + ' 个') + '读不到命令行' : '');
  }

  /* 构建锁 + 逐个进程列出的后台脚本（同一脚本的外壳与子进程已在采集时合并）+ 巡逻 */
  Dash.scripts = function (live) {
    var b = live.build || {};
    var held = b.lock === 'held';
    var box = h('div.sc');
    box.appendChild(h('div.sc-row.sc-lock.is-' + (held ? 'held' : 'idle'),
      dot(held ? 'held' : 'idle'),
      h('span.sc-name', '构建锁'),
      h('span.sc-cmd', held ? '被 ' + (b.holder || '某个进程') + ' 占用' : '空闲'),
      h('span.sc-when', editorsText(b.editors))));
    var list = live.scripts || [];
    list.filter(function (s) { return s.kind !== 'cron'; }).forEach(function (s) { box.appendChild(runLine(s)); });
    list.filter(function (s) { return s.kind === 'cron'; }).forEach(function (s) {
      box.appendChild(runLine(Object.assign({}, s, { cmd: '', note: s.since ? '上次运行 ' + Dash.at(s.since) + (s.state === 'running' ? '' : '，' + s.note) : s.note })));
    });
    return box;
  };

  /* ---------- 临时工作树 ---------- */

  function treeRow(w) {
    var tone = { using: 'ok', resident: 'ok', idle: 'muted', invalid: 'risk' }[w.state] || 'muted';
    var label = { using: '在用', resident: '常驻' + (w.persistent ? '·' + w.persistent : ''), idle: '闲置', invalid: '失效' }[w.state] || w.state;
    var run;
    if (w.state === 'invalid') {
      run = '目录没了';
    } else if (w.running.length) {
      run = '在跑 ' + w.running.map(function (g) { return g.kind + (g.n > 1 ? ' ×' + g.n : '') + '（' + Dash.at(g.since) + ' 起）'; }).join('、');
    } else if (w.state === 'resident') {
      run = '常驻，不清理；' + Dash.elapsed(w.touched) + '前动过';
    } else {
      run = '闲置 ' + Dash.elapsed(w.touched);
    }
    var vs = w.ahead == null ? '' : (w.ahead || w.behind)
      ? '离 main：' + [w.ahead ? '领先 ' + w.ahead : '', w.behind ? '落后 ' + w.behind : ''].filter(Boolean).join('、') + ' 个提交'
      : '与 main 同一提交';
    var parts = [w.short + '（' + (w.detached ? '分离头' : '分支 ' + w.branch) + '）', vs, w.state === 'invalid' ? '' : '未提交 ' + w.dirty + ' 个文件', run]
      .filter(Boolean);
    var gate = w.state === 'invalid' ? null
      : w.gate ? h('div.wt-gate' + (w.gateOk ? '' : '.is-bad'), '最近一次门禁：' + w.gate)
      : h('div.wt-gate.is-none', '没有门禁报告');
    return h('div.wt-row.is-' + w.state,
      h('div.wt-head', h('span.wt-name', w.name), Dash.chip(label, tone), w.inRoot ? null : Dash.chip('不在指定目录', 'auto'),
        w.locked ? Dash.chip('已锁定', 'muted') : null,
        h('span.wt-when', w.touched ? '最后动过 ' + Dash.at(w.touched) : '')),
      h('div.wt-path', w.path),
      h('div.wt-line', parts.join(' · ')),
      gate,
      w.flags.map(function (f) { return h('div.wt-flag', f); }));
  }

  /* 每个登记着的临时工作树一行：名字、路径、离 main 的提交数、未提交文件数、在跑什么或闲置多久、最近一次门禁、状态签条（在用 / 闲置 / 不在指定目录 / 失效） */
  Dash.worktrees = function (live) {
    var gap = Dash.liveGap(live, 'worktrees', ['临时工作树'], '确实没有登记着的临时工作树');
    return gap || h('div.wt', live.worktrees.items.map(treeRow));
  };

  /* 总览用：一行"临时工作树 N 个"，点开看清单；没有内容时说清原因 */
  Dash.worktreeFold = function (live) {
    var w = live && live.worktrees;
    if (!w || !w.items || !w.items.length) return Dash.worktrees(live);
    var flagged = w.items.filter(function (x) { return x.flags.length; }).length;
    return Dash.fold('临时工作树 ' + w.items.length + ' 个' + (flagged ? '，' + flagged + ' 个要看' : ''), Dash.worktrees(live));
  };

  /* ---------- 钩子与生效记录 ---------- */

  function hookRow(x) {
    var sideName = x.side === 'claude' ? 'Claude' : 'Codex';
    var line1 = h('div.hk-main',
      Dash.chip(sideName, x.side),
      h('span.hk-name', x.name),
      x.desc ? h('span.hk-desc', x.desc) : null,
      x.tracked
        ? h('span.hk-last', x.last ? '最后触发 ' + Dash.at(x.last) : '没有触发记录')
        : h('span.hk-last', '无记账'));
    var line2;
    if (x.tracked) {
      var a = x.h1, b = x.h24;
      line2 = h('div.hk-sub',
        '近 1 小时触发 ' + a.n + ' 次、注入 ' + a.inj + ' 次；近 24 小时触发 ' + b.n + ' 次、注入 ' + b.inj + ' 次；报错 ' + b.err + '、被挤掉 ' + b.dropped + '、取不到 ' + b.miss,
        x.lastInj ? h('div', '最近一次注入 ' + Dash.at(x.lastInj.at) + '：' + (x.lastInj.inj.join('、') || '—') + '（' + x.lastInj.chars + ' 字）') : h('div', '还没有成功注入的记录'),
        x.note ? h('div', x.note) : null);
    } else {
      line2 = h('div.hk-sub', '挂的事件：' + x.events.join('、') + '；' + (x.note || ''));
    }
    var body = h('div.hk-body', h('div.hk-sub', '挂的事件：' + x.events.join('、')), h('div.hk-sub.mono', x.script));
    if (x.tracked) {
      if (!x.recent.length) body.appendChild(h('div.hk-sub', '日志里还没有这个钩子的记录'));
      x.recent.forEach(function (r) {
        body.appendChild(h('div.hk-rec' + (r.err ? '.is-err' : ''),
          h('span.hk-t', Dash.at(r.at) + ' ' + r.at.slice(17, 19) + '″'),
          h('span.hk-ev', r.event + (r.tool ? ' ' + r.tool : '')),
          h('span.hk-inj', r.err ? '报错：' + r.err : (r.inj.length ? r.inj.join('、') + '（' + r.chars + ' 字）' : '没有注入')),
          r.dropped || r.miss ? h('span.hk-warn', (r.dropped ? '挤掉 ' + r.dropped : '') + (r.miss ? ' 取不到 ' + r.miss : '')) : null));
      });
    }
    var item = h('details.hk-item' + (x.flag ? '.is-flag' : ''), h('summary', line1, x.flag ? h('div.hk-flag', x.flag) : null, line2), body);
    return item;
  }

  /* 每个登记的钩子一行：最后触发、近 1 小时与 24 小时触发/注入次数、最近一次注入了什么、报错/挤掉/取不到；点开看最近 20 条。
     琥珀色：登记了但一次都没触发、触发了但没有一次成功注入、只有报错 */
  Dash.hooks = function (live) {
    var gap = Dash.liveGap(live, 'hooks', ['钩子'], '没有登记的钩子');
    return gap || h('div.hk', live.hooks.map(hookRow));
  };
})();
