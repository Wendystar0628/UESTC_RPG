/* 结构页：分层图，每层一条带，主要目录作块，规模只报数量级。数据 structure（10 min 刷新）。 */
(function () {
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
  function hrefOf(path) {
    const l = Dash.link(path);
    if (l instanceof Node) return l.getAttribute('href') || '#';
    const s = String(l);
    if (s.charAt(0) === '<') {
      const t = document.createElement('div');
      t.innerHTML = s;
      const a = t.querySelector('a');
      return a ? a.getAttribute('href') : '#';
    }
    return s;
  }
  function tier(loc) { // 1–4 格
    if (!loc) return 0;
    return Math.max(1, Math.min(4, Math.floor(Math.log10(loc)) - 1));
  }

  function block(d) {
    const a = h('a', 'st-block');
    a.href = hrefOf(d.path);
    a.target = '_blank';
    a.rel = 'noopener';
    const loc = d.loc || 0;
    a.style.flexGrow = String(Math.max(1, tier(loc)));
    const base = d.path.split('/').pop();
    const head = h('span', 'st-bhead', [h('span', 'st-bname', base), h('span', 'st-bpath', d.path.replace(/\/[^/]*$/, '/'))]);
    const meter = h('span', 'st-meter');
    for (let i = 1; i <= 4; i++) meter.append(h('i', i <= tier(loc) ? 'on' : ''));
    const size = d.files ? Dash.mag(d.files) + ' 个文件' : '没有代码文件';
    a.append(head, h('span', 'st-note', d.note || ''), h('span', 'st-size', [meter, size]));
    return a;
  }

  Dash.panel({
    id: 'structure',
    page: 'structure',
    title: '项目分层',
    order: 10,
    needs: ['structure'],
    render(el, data) {
      const s = data.structure;
      el.replaceChildren();
      el.classList.add('st');
      if (!s) { el.append(h('p', 'muted', '还没有结构数据')); return; }
      const layers = s.layers || [];
      const all = layers.reduce((acc, l) => acc.concat(l.dirs || []), []);
      const biggest = all.slice().sort((a, b) => (b.loc || 0) - (a.loc || 0))[0];
      const top = status('muted'); top.body.append(layers.length + ' 层，' + Dash.mag(all.length) + ' 个主要目录' + (biggest ? '；最大的是 ' + biggest.path + '，' + Dash.mag(biggest.files) + ' 个文件' : ''));
      el.append(top);
      const stack = h('div', 'st-stack');
      layers.forEach((l, i) => {
        const band = h('section', 'st-layer');
        band.append(h('header', 'st-lhead', [h('span', 'st-lname', l.name), h('span', 'st-lnote', l.note || '')]));
        const row = h('div', 'st-blocks');
        (l.dirs || []).forEach((d) => row.append(block(d)));
        band.append(row);
        stack.append(band);
        if (i < layers.length - 1) stack.append(h('div', 'st-down'));
      });
      el.append(stack, h('p', 'st-foot', '依赖只朝下：上层可以用下层，下层不认识上层。'));
    },
  });
})();
