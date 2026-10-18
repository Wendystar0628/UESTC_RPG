/* 施工页：队列按状态分栏，行只显示号、一句话、谁。数据 queue（10 min 刷新）。 */
(function () {
  const opened = new Set();
  const COLS = ['进行中', '待编译', '可派', '等前置'];
  const HINT = { '进行中': '正在做', '待编译': '写完，等统一编译', '可派': '可以派出去', '等前置': '要先等别的做完' };

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
  function whoSide(w) {
    const s = String(w || '').toLowerCase();
    if (/astra|codex|gpt|sol|luna/.test(s)) return 'codex';
    if (/claude|opus|sonnet|主会话/.test(s)) return 'claude';
    if (/作者|author/.test(s)) return 'author';
    return 'muted';
  }
  function whoLabel(w) {
    const s = String(w || '');
    return /^claude$/i.test(s) ? 'Claude' : /^astra$/i.test(s) ? 'Astra' : s;
  }

  function card(r, col) {
    const c = h('div', 'qu-card');
    const foot = h('span', 'qu-foot');
    if (col === '等前置' && r.pre) foot.append(h('span', 'qu-pre', '等 ' + r.pre));
    if (r.who) foot.append(chip(whoLabel(r.who), whoSide(r.who)));
    c.append(h('span', 'qu-id', r.id), foot, h('span', 'qu-title', r.title));
    return c;
  }

  function board(q) {
    const rows = q.rows || [];
    const counts = q.counts || {};
    const wrap = h('div', 'qu-board');
    COLS.forEach((col) => {
      const items = rows.filter((r) => r.status === col);
      const n = counts[col] != null ? counts[col] : items.length;
      const colEl = h('section', 'qu-col qu-col--' + COLS.indexOf(col) + (n ? '' : ' is-empty'));
      colEl.append(h('header', 'qu-colhead', [h('span', 'qu-colname', col), h('span', 'qu-count', n), h('span', 'qu-hint', HINT[col])]));
      const body = h('div', 'qu-cards');
      items.forEach((r) => body.append(card(r, col)));
      if (!items.length) body.append(h('div', 'qu-none', '空'));
      colEl.append(body);
      wrap.append(colEl);
    });
    return wrap;
  }

  function paused(q) {
    const rows = (q.rows || []).filter((r) => r.status === '暂停');
    const n = (q.counts || {})['暂停'] != null ? q.counts['暂停'] : rows.length;
    if (!n) return null;
    const key = 'paused';
    const wrap = h('div', 'qu-paused' + (opened.has(key) ? ' is-open' : ''));
    const btn = h('button', 'qu-fold', [h('span', 'qu-caret'), '暂停 ' + n + ' 项']);
    btn.type = 'button';
    btn.disabled = !rows.length;
    btn.addEventListener('click', () => {
      wrap.classList.toggle('is-open');
      wrap.classList.contains('is-open') ? opened.add(key) : opened.delete(key);
    });
    wrap.append(btn);
    if (rows.length) {
      const list = h('div', 'qu-cards qu-cards--paused');
      rows.forEach((r) => list.append(card(r, '暂停')));
      wrap.append(list);
    }
    return wrap;
  }

  Dash.panel({
    id: 'queue',
    page: 'work',
    title: '施工队列',
    order: 10,
    needs: ['queue'],
    render(el, data) {
      const q = data.queue;
      el.replaceChildren();
      el.classList.add('qu');
      if (!q) { el.append(h('p', 'muted', '还没有队列数据')); return; }
      const top = status('ok');
      top.body.append(q.stage || '', q.priority ? '，' + q.priority : '');
      el.append(top, board(q));
      const p = paused(q);
      if (p) el.append(p);
    },
  });
})();
