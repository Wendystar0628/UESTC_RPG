/* 模型页：会话 → 子代理的树，后台脚本与构建锁。数据 live（15 s 刷新）。 */
(function () {
  const opened = new Set(); // 折叠状态在重绘后保持

  function h(tag, cls, kids) {
    const e = document.createElement(tag);
    if (cls) e.className = cls;
    (kids == null ? [] : [].concat(kids)).forEach((k) => {
      if (k == null || k === false) return;
      e.append(k instanceof Node ? k : document.createTextNode(String(k)));
    });
    return e;
  }
  const status = Dash.statusLine;
  function chip(text, tone) {
    const c = Dash.chip(text, tone);
    if (c instanceof Node) return c;
    const t = document.createElement('span');
    t.innerHTML = c;
    return t.firstElementChild || t;
  }
  function dot(kind) { return h('span', 'mdl-dot mdl-dot--' + kind); }

  function agentSide(a, root) {
    const m = String(a.model || '');
    if (/gpt|codex|luna|sol\b|^o\d/i.test(m)) return 'codex';
    if (/claude|opus|sonnet|haiku|fable/i.test(m)) return 'claude';
    return root ? root.side : 'claude';
  }

  function row(opts) {
    // opts: name, side, model, effort, state(running|idle|done), task, when, nested
    const r = h('div', 'mdl-row mdl-row--' + opts.side + (opts.nested ? ' is-nested' : '') + ' is-' + opts.state);
    r.append(
      dot(opts.state),
      h('span', 'mdl-name', opts.name),
      h('span', 'mdl-sigs', [chip(Dash.modelName(opts.model), opts.side), chip(opts.effort, 'muted')]),
      h('span', 'mdl-task', opts.task),
      h('span', 'mdl-when', opts.when ? Dash.at(opts.when) : '')
    );
    return r;
  }

  function tree(live) {
    const box = h('div', 'mdl-tree');
    const sessions = live.sessions || [];
    const agents = live.agents || [];
    const ids = new Set(sessions.map((s) => s.id));
    const groups = sessions.map((s) => ({ root: s, kids: agents.filter((a) => a.parent === s.id) }));
    const orphans = agents.filter((a) => !ids.has(a.parent));
    if (orphans.length) groups.push({ root: { id: '_o', name: '其他', side: 'claude', model: '', effort: '', state: 'idle', task: '', since: '' }, kids: orphans, virtual: true });

    groups.forEach((g) => {
      const side = g.root.side;
      const grp = h('div', 'mdl-group mdl-group--' + side);
      if (!g.virtual) {
        grp.append(row({ name: g.root.name, side, model: g.root.model, effort: g.root.effort, state: g.root.state, task: g.root.task, when: g.root.since }));
      } else {
        grp.append(h('div', 'mdl-orphan', '没有挂到会话的子代理'));
      }
      const live_ = g.kids.filter((a) => a.state !== 'done');
      const done = g.kids.filter((a) => a.state === 'done');
      live_.forEach((a) => grp.append(row({ name: a.name, side: agentSide(a, g.root), model: a.model, effort: a.effort, state: a.state, task: a.task, when: a.started, nested: true })));
      if (done.length) {
        const key = 'done:' + g.root.id;
        const wrap = h('div', 'mdl-done' + (opened.has(key) ? ' is-open' : ''));
        const btn = h('button', 'mdl-fold', [h('span', 'mdl-caret'), '另有 ' + done.length + ' 个已交回']);
        btn.type = 'button';
        btn.addEventListener('click', () => {
          wrap.classList.toggle('is-open');
          wrap.classList.contains('is-open') ? opened.add(key) : opened.delete(key);
        });
        const list = h('div', 'mdl-done-list');
        done.forEach((a) => list.append(row({ name: a.name, side: agentSide(a, g.root), model: a.model, effort: a.effort, state: 'done', task: a.task, when: a.started, nested: true })));
        wrap.append(btn, list);
        grp.append(wrap);
      }
      box.append(grp);
    });
    return box;
  }

  const KIND = { watch: '监视', cron: '定时', gate: '门禁', engine: '引擎', build: '构建', other: '其他' };

  function scripts(live) {
    const box = h('div', 'mdl-scripts');
    const lock = live.build || {};
    const held = lock.lock === 'held';
    const eng = lock.engines ? '，引擎 ' + lock.engines + ' 个' : '';
    const lr = h('div', 'mdl-script is-' + (held ? 'held' : 'idle'));
    lr.append(dot(held ? 'held' : 'idle'), h('span', 'mdl-name', '构建锁'), h('span', 'mdl-kind', '构建'),
      h('span', 'mdl-task', (held ? '被 ' + (lock.holder || '某会话') + ' 占用' : '空闲') + eng), h('span', 'mdl-when', ''));
    box.append(lr);
    (live.scripts || []).forEach((s) => {
      const tone = s.state === 'running' ? 'running' : 'idle';
      const r = h('div', 'mdl-script is-' + tone);
      r.append(
        dot(tone),
        h('span', 'mdl-name', s.name),
        h('span', 'mdl-kind', KIND[s.kind] || ''),
        h('span', 'mdl-task', s.note || ''),
        h('span', 'mdl-when', s.since ? Dash.at(s.since) : '')
      );
      box.append(r);
    });
    return box;
  }

  function statusLine(live) {
    const ss = (live.sessions || []).filter((s) => s.state === 'running').length;
    const ag = (live.agents || []).filter((a) => a.state === 'running').length;
    const lock = live.build || {};
    const held = lock.lock === 'held';
    const parts = [
      ss ? ss + ' 个会话在跑' : '没有会话在跑',
      ag ? ag + ' 个子代理在跑' : '没有子代理在跑',
    ];
    const line = status(held ? 'auto' : ss ? 'ok' : 'muted');
    line.body.append(parts.join('，') + '，', h('span', held ? 'mdl-lock is-held' : 'mdl-lock', held ? '构建锁被 ' + (lock.holder || '某会话') + ' 占用' : '构建锁空闲'));
    return line;
  }

  Dash.panel({
    id: 'models',
    page: 'models',
    title: '会话与子代理',
    order: 10,
    needs: ['live'],
    render(el, data) {
      const live = data.live;
      el.replaceChildren();
      el.classList.add('mdl');
      if (!live) { el.append(h('p', 'muted', '还没有实时数据')); return; }
      el.append(statusLine(live), tree(live));
    },
  });

  Dash.panel({
    id: 'models-scripts',
    page: 'models',
    title: '后台脚本与构建锁',
    order: 20,
    needs: ['live'],
    render(el, data) {
      el.replaceChildren();
      el.classList.add('mdl');
      if (data.live) el.append(scripts(data.live));
    },
  });
})();
