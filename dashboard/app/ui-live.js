/* 实时状态的共用渲染：子代理签条文字 Dash.agentLabel、后台脚本与构建锁 Dash.scripts、临时工作树 Dash.worktrees、长文字 Dash.longText、空状态原因 Dash.liveGap。
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
  function sinceText(a) {
    if (a.state === 'unknown') return '记录不足，状态未知';
    if (a.stalled) return '未收尾，最后记录 ' + Dash.at(a.last) + '（可能卡住）';
    if (a.state === 'done') return Dash.at(a.last) + ' 交回';
    return Dash.at(a.started || a.since) + ' 起，已 ' + Dash.elapsed(a.started || a.since);
  }

  /* 子代理的签条文字：名字 · 型号 · 推理档 · 在跑/已交回 · 开始多久 */
  Dash.agentLabel = function (a) {
    return [a.side === 'codex' ? 'Codex' : 'Claude', a.name, Dash.modelName(a.model), a.effort, a.stalled ? '未收尾、可能卡住' : STATE[a.state] || a.state, sinceText(a)]
      .filter(Boolean).join(' · ');
  };

  function dot(kind) { return h('span.sc-dot.is-' + kind); }

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
})();
