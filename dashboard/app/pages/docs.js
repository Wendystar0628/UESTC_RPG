/* 文档页：左边找文档（树、本地过滤、正文搜索），右边读（Dash.reader）。
   块（面板 id）：docs-tree 文档目录 · docs-reader 阅读。
   数据：docs（域 → 文档）；正文搜索走 /api/doc/search，阅读走 Dash.reader（自己调 /api/doc）。 */
(function () {
  var h = Dash.h;

  /* 重画之间共用的状态：搜索词、树和份数挂在 fixed 里建的节点上、正文搜索的序号 */
  var state = { q: '', tree: null, totalEl: null, hits: null, seq: 0, info: {} };

  function matches(doc, q) {
    var hay = [doc.code, doc.title].concat(doc.keywords || []).join(' ').toLowerCase();
    return hay.indexOf(q) >= 0;
  }

  /* 整棵树：域 → 顶层文档 → 子文档（parent 找不到的当顶层）；当前文档高亮，它所在的域和父文档展开。过滤交给树自己（root.filter） */
  function buildNodes(domains, cur) {
    state.info = {};
    return domains.map(function (dm) {
      var docs = dm.docs || [], byCode = {}, kids = {}, roots = [], holdsCur = false;
      docs.forEach(function (x) { byCode[x.code] = x; });
      docs.forEach(function (x) {
        if (x.parent && byCode[x.parent]) (kids[x.parent] = kids[x.parent] || []).push(x);
        else roots.push(x);
      });
      state.info['d:' + dm.key] = { domain: dm };
      function node(x, parent) {
        state.info['c:' + x.code] = { doc: x, domain: dm, parent: parent };
        var n = { key: 'c:' + x.code, label: x.code, sub: x.title, current: x.code === cur };
        var subs = (kids[x.code] || []).map(function (s) { return node(s, x); });
        if (subs.length) { n.kids = subs; n.open = subs.some(function (s) { return s.current || s.open; }); }
        if (n.current || n.open) holdsCur = true;
        return n;
      }
      var nodes = roots.map(function (r) { return node(r, null); });
      return { key: 'd:' + dm.key, label: dm.name, count: docs.length, kids: nodes, open: holdsCur };
    });
  }

  /* 过滤谓词：域名命中显示整个域，顶层文档命中显示它的所有子文档，只有子文档命中则只显示命中的那几个 */
  function matchNode(n, q) {
    var it = state.info[n.key];
    if (!it) return false;
    if (!it.doc) return String(it.domain.name).toLowerCase().indexOf(q) >= 0;
    return matches(it.doc, q) || (it.parent != null && matches(it.parent, q)) || String(it.domain.name).toLowerCase().indexOf(q) >= 0;
  }

  /* ---- 正文搜索：回车或点"搜正文"，列命中的小节 ---- */

  function snippetNode(text, q) {
    var s = String(text || ''), i = s.toLowerCase().indexOf(q.toLowerCase());
    if (!q || i < 0) return s;
    return [s.slice(0, i), h('span.docs-hit-q', s.slice(i, i + q.length)), s.slice(i + q.length)];
  }

  function showHits(body) {
    state.hits.replaceChildren(body || []);
    state.hits.hidden = !body;
  }

  function closeHits() { state.seq++; showHits(null); }

  function searchBody() {
    var q = state.q.trim();
    if (!q) { closeHits(); return; }
    var seq = ++state.seq;
    showHits(h('div.docs-hits-head.muted', '搜索中…'));
    fetch('/api/doc/search?q=' + encodeURIComponent(q), { cache: 'no-store' })
      .then(function (r) {
        return r.json().catch(function () { return { ok: false, error: 'HTTP ' + r.status }; });
      })
      .then(function (j) {
        if (seq !== state.seq) return;
        if (!j || !j.ok) throw new Error(j && j.error || '没有返回结果');
        var items = j.items || [];
        var head = h('div.docs-hits-head',
          h('span', items.length ? '正文命中 ' + Dash.num(items.length) + ' 个小节' : '正文里没有「' + q + '」'),
          h('button.btn.docs-hits-close', { type: 'button', on: { click: closeHits } }, '收起'));
        showHits(h('div.docs-hits-box', head, items.map(function (it) {
          var sec = it.sec || {};
          return h('button.docs-hit', {
            type: 'button',
            on: { click: function () { Dash.openDoc(it.code, { sec: sec.anchor }); } }
          },
            h('span.docs-hit-ref', h('span.id', it.code), sec.n ? ' §' + sec.n + ' ' : ' ', h('span.docs-hit-sec', sec.title || it.title || '')),
            h('span.docs-hit-snip', snippetNode(it.snippet, q)));
        })));
      })
      .catch(function (e) {
        if (seq !== state.seq) return;
        showHits(h('div.docs-hits-box', Dash.status('没搜到：' + (e && e.message || e), 'risk')));
      });
  }

  Dash.panel({
    id: 'docs-tree', page: 'docs', title: '文档目录', note: '按域排的规则与设计文档；输入即过滤，回车搜正文', span: 4, order: 10, needs: ['docs'],
    caliber: '文档来自 docs/ 各文件的头部与 SECTIONS.md；过滤只看编码、标题与关键词，「搜正文」按小节正文匹配、最多列 30 个小节',
    uses: ['params'],
    fixed: function (el) {
      state.totalEl = h('div.docs-total.muted');
      var input = h('input.docs-search', {
        type: 'search', placeholder: '过滤编码、标题、关键词；回车搜正文', 'aria-label': '搜索文档',
        autocomplete: 'off', spellcheck: 'false'
      });
      input.addEventListener('input', function () { state.q = input.value; if (state.tree) state.tree.filter(state.q); });
      input.addEventListener('keydown', function (e) { if (e.key === 'Enter') { state.q = input.value; searchBody(); } });
      state.hits = h('div.docs-hits', { hidden: true });
      el.appendChild(h('div.docs-top', state.totalEl,
        h('div.docs-bar', input, h('button.btn.btn-primary', { type: 'button', on: { click: function () { state.q = input.value; searchBody(); } } }, '搜正文')),
        state.hits));
    },
    render: function (el, d, ctx) {
      state.totalEl.textContent = '共 ' + Dash.num(d.docs.count) + ' 份';
      state.tree = Dash.tree(buildNodes(d.docs.domains || [], ctx.params.code), {
        filter: matchNode,
        onClick: function (n) { if (n.key.indexOf('c:') === 0) Dash.openDoc(n.key.slice(2)); }
      });
      el.appendChild(h('div.docs-tree', state.tree));
      if (state.q.trim()) state.tree.filter(state.q);
    }
  });

  Dash.panel({
    id: 'docs-reader', page: 'docs', title: '阅读', note: '左边的小节目录随滚动高亮；正文里的「编码 §n」可以直接点', span: 8, order: 20, needs: ['docs'],
    uses: ['params'],
    render: function (el, d, ctx) {
      Dash.reader(el, { code: ctx.params.code, sec: ctx.params.sec, find: ctx.params.find, docs: d.docs });
    }
  });
})();
