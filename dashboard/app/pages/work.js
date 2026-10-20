/* 施工页：每件活走到哪一步、谁挡着谁。
   块（面板 id）：work-flow 流水线泳道 · work-history 行的历史 · work-deps 依赖图 · work-board 看板 · work-trees 临时工作树。
   选中的行放在 Dash.select('row')，泳道、依赖图、看板、现在页的卡片都能选；行的历史 watch 它，选中后才拉 rowhistory。 */
(function () {
  var h = Dash.h;

  function sideKey(who) { var s = String(who || '').toLowerCase(); return s.indexOf('codex') >= 0 ? 'codex' : s.indexOf('claude') >= 0 ? 'claude' : /作者|author/.test(s) ? 'author' : ''; }
  function pick(id) { if (Dash.selected('row') !== id) Dash.select('row', id); }
  function preList(pre) { return (Array.isArray(pre) ? pre : String(pre || '').split(/[、,，\s]+/)).filter(function (x) { return /^[A-Z]-\d/.test(x); }); }
  function rowLink(id) { return h('a.mono', { href: '#work/work-history', on: { click: function () { pick(id); } } }, id); }

  /* ---------- 流水线泳道：每个在途行一道，同一次推送收口的行归一批 ---------- */
  Dash.panel({
    id: 'work-flow', page: 'work', title: '流水线', note: '每个在途行走到哪个阶段：一行一道，格子写状态与时刻；同一次推送收口的行归为一批。点一行看它的历史', order: 10,
    needs: ['pipeline'], watch: ['row'],
    render: function (el, d) {
      var p = d.pipeline, byId = {}, rows = p.rows || [];
      rows.forEach(function (r) { byId[r.id] = r; });
      var lanes = [], seen = {};
      function laneOf(r) { seen[r.id] = 1; return { id: r.id, label: r.id, sub: Dash.gist(r.title).text, side: sideKey(r.who) }; }
      var inBatch = {};
      (p.batches || []).forEach(function (b) { b.rows.forEach(function (id) { inBatch[id] = 1; }); });
      var loose = rows.filter(function (r) { return !inBatch[r.id]; });
      if (loose.length) { lanes.push({ id: 'head:loose', head: true, label: '还没推送 · ' + Dash.num(loose.length) + ' 行' }); loose.forEach(function (r) { lanes.push(laneOf(r)); }); }
      (p.batches || []).slice().sort(function (a, b) { return Dash.parseTime(b.pushedAt) - Dash.parseTime(a.pushedAt); }).forEach(function (b) {
        var ids = b.rows.filter(function (id) { return byId[id] && !seen[id]; });
        if (!ids.length) return;
        lanes.push({ id: 'head:' + b.id, head: true, label: '一批推送 ' + Dash.at(b.pushedAt) + ' · ' + Dash.num(ids.length) + ' 行' });
        ids.forEach(function (id) { lanes.push(laneOf(byId[id])); });
      });
      /* 各阶段此刻有几行在做、几行失败（第一行状态） */
      var doing = {}, fails = 0;
      rows.forEach(function (r) {
        Dash.stages.forEach(function (s) {
          var x = r.stages[s.id];
          if (x && x.status === 'doing') doing[s.id] = (doing[s.id] || 0) + 1;
          if (x && x.status === 'fail') fails++;
        });
      });
      var parts = Dash.stages.filter(function (s) { return doing[s.id]; }).map(function (s) { return s.label + ' ' + Dash.num(doing[s.id]); });
      el.appendChild(Dash.status('在途 ' + Dash.num(rows.length) + ' 行' + (parts.length ? '：' + parts.join(' · ') : '') + (fails ? '；' + Dash.num(fails) + ' 格失败' : ''), fails ? 'risk' : rows.length ? 'ok' : 'muted'));
      var cells = [];
      lanes.forEach(function (l) {
        if (l.head) return;
        Dash.stages.forEach(function (s) { var x = byId[l.id].stages[s.id] || { status: 'todo' }; cells.push({ lane: l.id, col: s.id, status: x.status, at: x.at, note: x.note }); });
      });
      el.appendChild(Dash.charts.swimlane({ columns: Dash.stages, lanes: lanes, cells: cells },
        { legend: [['status'], ['side', ['claude', 'codex']]], selected: Dash.selected('row'), labelWidth: 280, empty: '没有在途的行',
          onClick: function (it) { pick(it.lane.id); } }));
    }
  });

  /* ---------- 行的历史：选中一行显示时间线（状态变化、阶段、提交） ---------- */
  Dash.panel({
    id: 'work-history', page: 'work', title: '行的历史', note: '选中的那一行什么时候加进来、状态怎么变、各阶段几点走完、提交了什么', order: 15,
    needs: ['pipeline', 'queue'], partial: true, uses: ['rowhistory'], watch: ['row'],
    render: function (el, d) {
      var id = Dash.selected('row');
      if (!id) { el.appendChild(Dash.empty('还没选行', '在上面的流水线、下面的依赖图或看板里点一行', true)); return; }
      var prow = ((d.pipeline && d.pipeline.rows) || []).filter(function (r) { return r.id === id; })[0];
      var qrow = ((d.queue && d.queue.rows) || []).filter(function (r) { return r.id === id; })[0];
      var info = prow || qrow || { id: id };
      var state = qrow ? qrow.status : prow && prow.state;
      var pre = preList(prow ? prow.pre : qrow && qrow.pre);
      el.appendChild(h('div.wh-head',
        h('div.wh-line', h('b.wh-id', id), sideKey(info.who) ? Dash.mark('side', sideKey(info.who)) : null, state ? Dash.mark('queue', state) : null,
          pre.length ? h('span.wh-pre', '前置 ', pre.map(function (p, i) { return [i ? '、' : '', rowLink(p)]; })) : null,
          h('button.btn.wh-close', { type: 'button', on: { click: function () { Dash.select('row', id); } } }, '取消选中')),
        info.title ? h('div.wh-title', Dash.longText(info.title)) : h('div.wh-title.muted', '队列与流水里都没有这一行（可能已删行）')));

      var hist = Dash.get('rowhistory');
      if (!hist) {
        if (Dash.dataState('rowhistory') === 'missing') {
          el.appendChild(Dash.empty('行的历史还没有数据', '状态变化来自 rowhistory，等后端 C-239 写出 data/auto/rowhistory.js；下面先画阶段与提交', true));
        } else {
          el.appendChild(Dash.charts.loading(120));
          Dash.need(['rowhistory']);   // 到了以后经 uses 重绘这一块
          return;
        }
      }
      var states = (hist && hist[id]) || [];
      var items = states.map(function (s, i) {
        var next = states[i + 1];
        return { lane: 'state', at: s.at, end: next ? next.at : new Date().toISOString(), tone: ['queue', s.state], label: s.state };
      });
      if (prow) {
        Dash.stages.forEach(function (s) {
          var x = prow.stages[s.id];
          if (x && x.at && x.status !== 'na') items.push({ lane: 'stage', at: x.at, tone: ['status', x.status], label: s.label + ' · ' + Dash.label('status', x.status), title: x.note });
        });
        (prow.commits || []).forEach(function (c) { items.push({ lane: 'commit', at: c.at, tone: ['side', sideKey(prow.who) || 'auto'], label: c.hash, title: c.subject }); });
      }
      el.appendChild(Dash.charts.timeline({ lanes: [{ id: 'state', label: '队列状态' }, { id: 'stage', label: '阶段' }, { id: 'commit', label: '提交' }], items: items },
        { empty: '还没有 ' + id + ' 的状态记录、阶段时刻或提交', legend: [['queue', ['可派', '等前置', '进行中', '待编译', '已推', '暂停']]] }));
      if (prow && prow.commits && prow.commits.length) {
        el.appendChild(h('div.wh-commits', prow.commits.map(function (c) {
          return h('div.wh-commit', h('span.mono', c.hash), h('span.muted', Dash.at(c.at)), h('span', c.subject));
        })));
      }
    }
  });

  /* ---------- 依赖图：全部队列行按前置连成有向图 ---------- */

  /* 关键路径：没完成的行之间最长的一条前置链（只为了画出来，不改任何状态） */
  function criticalPath(open, edges) {
    var next = {}, memo = {};
    edges.forEach(function (e) { if (open[e.from] && open[e.to]) (next[e.from] = next[e.from] || []).push(e.to); });
    function longest(id, stack) {
      if (memo[id]) return memo[id];
      if (stack[id]) return [id];
      stack[id] = 1;
      var best = [];
      (next[id] || []).forEach(function (n) { var p = longest(n, stack); if (p.length > best.length) best = p; });
      delete stack[id];
      return (memo[id] = [id].concat(best));
    }
    var best = [];
    Object.keys(open).sort().forEach(function (id) { var p = longest(id, {}); if (p.length > best.length) best = p; });
    return best.length > 1 ? best : [];
  }

  Dash.panel({
    id: 'work-deps', page: 'work', title: '依赖图', note: '谁挡着谁：箭头从前置指向后续，颜色是队列状态，形状是负责方；主色粗线是关键路径，红虚线圈是前置已完成却还没解锁的行', order: 20,
    needs: ['queue'], watch: ['row'],
    render: function (el, d) {
      var rows = d.queue.rows || [], ids = {};
      rows.forEach(function (r) { ids[r.id] = r; });
      function finished(id) { return !ids[id] || ids[id].status === '已推'; }
      var edges = [];
      rows.forEach(function (r) { preList(r.pre).forEach(function (p) { if (ids[p]) edges.push({ from: p, to: r.id }); }); });
      var open = {};
      rows.forEach(function (r) { if (!finished(r.id)) open[r.id] = 1; });
      var path = criticalPath(open, edges), onPath = {};
      path.forEach(function (id, i) { if (i) onPath[path[i - 1] + '>' + id] = 1; });
      var sel = Dash.selected('row');
      var stuck = rows.filter(function (r) { var pre = preList(r.pre); return r.status === '等前置' && pre.length && pre.every(finished); });
      var stuckIds = {};
      stuck.forEach(function (r) { stuckIds[r.id] = 1; });
      var line = Dash.num(rows.length) + ' 行、' + Dash.num(edges.length) + ' 条前置';
      if (path.length) line += '；关键路径 ' + path.join(' → ');
      if (stuck.length) line += '；' + Dash.num(stuck.length) + ' 行前置已完成却还标着等前置';
      el.appendChild(Dash.status(line, stuck.length ? 'warn' : 'ok'));
      var present = Object.keys(Dash.palette.queue).filter(function (k) { return rows.some(function (r) { return r.status === k; }); });
      el.appendChild(Dash.charts.dag({
        critical: path.length > 0,
        nodes: rows.map(function (r) {
          return { id: r.id, label: Dash.gist(r.title).text, tone: ['queue', r.status], side: sideKey(r.who), title: r.title,
            mark: !!stuckIds[r.id], markNote: stuckIds[r.id] ? '前置都已完成，还标着等前置' : '' };
        }),
        edges: edges.map(function (e) {
          return { from: e.from, to: e.to, critical: !!onPath[e.from + '>' + e.to], strong: !!sel && (e.from === sel || e.to === sel) };
        })
      }, { legend: [['queue', present]], selected: sel, markText: '前置已完成却没解锁', onClick: function (n) { pick(n.id); Dash.go('work/work-history'); } }));
    }
  });

  /* ---------- 看板：可派、等前置、暂停 ---------- */
  var COLS = [['可派', '可以派出去'], ['等前置', '要先等别的做完'], ['暂停', '先放着']];

  function qCard(r, col, ids) {
    var pre = preList(r.pre);
    return h('button.qu-card' + (Dash.selected('row') === r.id ? '.is-selected' : ''), { type: 'button', on: { click: function () { pick(r.id); Dash.go('work/work-history'); } } },
      h('span.qu-top', h('span.qu-id', r.id), sideKey(r.who) ? Dash.mark('side', sideKey(r.who)) : null),
      h('span.qu-title', r.title),
      col === '等前置' && pre.length ? h('span.qu-pre', '等 ', pre.map(function (p) {
        var q = ids[p];
        return q ? Dash.mark('queue', q.status, p) : Dash.mark('status', 'done', p + ' 已完成');
      })) : null,
      r.fix ? h('span.qu-fix', r.fix) : null);   // 巡逻算出的维护建议（改可派、改等前置、补前置……），与健康页"施工队列"同一份判据
  }

  Dash.panel({
    id: 'work-board', page: 'work', title: '看板', note: '排着还没开工的行：哪些能马上派、哪些在等前置、哪些先放着', order: 30, needs: ['queue'], watch: ['row'],
    render: function (el, d) {
      var q = d.queue, ids = {};
      (q.rows || []).forEach(function (r) { ids[r.id] = r; });
      el.appendChild(h('div.qu-board', COLS.map(function (c) {
        var rows = (q.rows || []).filter(function (r) { return r.status === c[0]; });
        var body = h('div.qu-cards', rows.length ? rows.map(function (r) { return qCard(r, c[0], ids); }) : h('div.qu-none', '空'));
        var col = h('section.qu-col', { style: '--tone: var(' + Dash.colorVar('queue', c[0]) + ')' },
          h('header.qu-colhead', h('span.qu-colname', c[0]), h('span.qu-count', Dash.num(rows.length)), h('span.qu-hint', c[1])));
        col.appendChild(c[0] === '暂停' && rows.length > 4 ? Dash.fold('展开 ' + Dash.num(rows.length) + ' 行', body) : body);
        return col;
      })));
    }
  });

  /* ---------- 临时工作树 ---------- */
  Dash.panel({
    id: 'work-trees', page: 'work', title: '临时工作树', note: 'Claude 侧施工用的临时工作树：在用还是闲置、离 main 多远、有没有没提交的改动', order: 50, needs: ['live'],
    render: function (el, d) {
      var w = d.live.worktrees;
      if (w && w.root) el.appendChild(h('p.muted.small.wt-root', '指定目录 ' + w.root));
      el.appendChild(Dash.worktrees(d.live));
    }
  });
})();
