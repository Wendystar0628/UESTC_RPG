/* 观察台运行时：数据注册与定时重载、页面路由、面板渲染、通用格式化与小组件。
   接口见 dashboard/README.md §3；面板只经 Dash.* 读数据，不直接碰 DOM 之外的全局。 */
(function () {
  'use strict';

  var M = window.DashManifest || { pages: [], panels: [], data: [] };
  var DEMO = /[?&]demo(?:[=&]|$)/.test(location.search);
  var Dash = window.Dash = { demo: DEMO };

  var store = {};      // 数据名 -> 数据
  var meta = {};       // 数据名 -> { sig, tried, missing, loading }
  var panels = {};     // 面板 id -> 面板记录
  var srcs = {};       // 数据名 -> manifest 条目
  var dirty = {};      // 待重绘的面板 id
  var flushTimer = 0;
  var booted = false;
  var currentPage = null;

  M.data.forEach(function (s) { srcs[s.name] = s; });

  /* ================= 小工具 ================= */

  function esc(s) {
    return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
      return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
    });
  }

  function appendKids(el, kids) {
    kids.forEach(function (k) {
      if (k == null || k === false || k === true) return;
      if (Array.isArray(k)) appendKids(el, k);
      else if (k instanceof Node) el.appendChild(k);
      else el.appendChild(document.createTextNode(String(k)));
    });
  }

  /* h('div.row.big', { title:'..', on:{click:fn}, text:'..' }, 子节点...)  子节点可以是节点、文字（按文字处理）、数组 */
  function h(tag) {
    var kids = Array.prototype.slice.call(arguments, 1);
    var props = null;
    if (kids[0] && typeof kids[0] === 'object' && !(kids[0] instanceof Node) && !Array.isArray(kids[0])) props = kids.shift();
    var parts = String(tag || 'div').split('.');
    var el = document.createElement(parts[0] || 'div');
    if (parts.length > 1) el.className = parts.slice(1).join(' ');
    if (props) {
      Object.keys(props).forEach(function (k) {
        var v = props[k];
        if (v == null || v === false) return;
        if (k === 'class' || k === 'cls') el.className += (el.className ? ' ' : '') + v;
        else if (k === 'text') el.textContent = v;
        else if (k === 'on') Object.keys(v).forEach(function (ev) { el.addEventListener(ev, v[ev]); });
        else if (k === 'dataset') Object.keys(v).forEach(function (d) { el.dataset[d] = v[d]; });
        else el.setAttribute(k, v === true ? '' : v);
      });
    }
    appendKids(el, kids);
    return el;
  }

  /* 返回的节点既能 appendChild，也能放进模板字符串（toString 给出 outerHTML） */
  function printable(el) {
    el.toString = function () { return el.outerHTML; };
    return el;
  }

  function parseTime(iso) {
    if (typeof iso === 'number') return iso;
    var t = Date.parse(iso);
    return isNaN(t) ? NaN : t;
  }

  function cmpDesc(a, b) {
    var x = parseTime(a), y = parseTime(b);
    if (isNaN(x)) x = 0;
    if (isNaN(y)) y = 0;
    return y - x;
  }

  /* ================= 公开：格式化 ================= */

  Dash.esc = esc;
  Dash.h = h;

  Dash.mag = function (n) {
    if (n == null || isNaN(n)) return '—';
    n = Math.floor(n);
    if (n <= 0) return '无';
    if (n < 10) return String(n);
    if (n < 20) return '十几';
    if (n < 100) return '几十';
    if (n < 200) return '上百';
    if (n < 1000) return '几百';
    if (n < 2000) return '上千';
    if (n < 10000) return '几千';
    return '上万';
  };

  /* 数量级加单位："几十个"、"3 个" */
  Dash.magText = function (n, unit) {
    var m = Dash.mag(n);
    return m + (/^\d+$/.test(m) ? ' ' : '') + (unit || '');
  };

  Dash.ago = function (iso) {
    var t = parseTime(iso);
    if (isNaN(t)) return '—';
    var s = (Date.now() - t) / 1000;
    if (s < 120) return '刚刚';
    if (s < 3600) return '几分钟前';
    if (s < 86400) return '约 ' + Math.max(1, Math.round(s / 3600)) + ' 小时前';
    var today = new Date(); today.setHours(0, 0, 0, 0);
    var day = new Date(t); day.setHours(0, 0, 0, 0);
    var d = Math.round((today - day) / 86400000);
    if (d <= 1) return '昨天';
    if (d < 14) return '几天前';
    if (d < 60) return '几周前';
    return '很久以前';
  };

  Dash.stale = function (data, seconds) {
    if (!data) return true;
    if (!data.updated) return false;
    var t = parseTime(data.updated);
    if (isNaN(t)) return false;
    return (Date.now() - t) / 1000 > seconds;
  };

  /* 按 manifest 里这份数据的期限判断；没有数据、演示模式、期限为 null 都返回 false */
  Dash.isStale = function (name) {
    var s = srcs[name];
    if (DEMO || !store[name] || !s || s.stale == null) return false;
    return Dash.stale(store[name], s.stale);
  };

  Dash.link = function (path) {
    if (!path) return '#';
    path = String(path);
    if (/^[a-z][a-z0-9+.-]*:/i.test(path)) return path;
    path = path.replace(/\\/g, '/').replace(/^(\.\/)+/, '').replace(/^\/+/, '');
    return '../' + path.split('/').map(encodeURIComponent).join('/');
  };

  Dash.chip = function (text, tone) {
    return printable(h('span.chip.t-' + (tone || 'muted'), text));
  };

  /* ================= 公开：小组件 ================= */

  /* 指向仓库文件的链接（新标签页打开） */
  Dash.a = function (path, text, cls) {
    return printable(h('a', { href: Dash.link(path), target: '_blank', rel: 'noopener', cls: cls }, text == null ? path : text));
  };

  /* 每块第一行的一句状态：tone 取 ok/warn/risk/muted 等；opts.large 放大；sub 是小一号的补充 */
  Dash.status = function (text, tone, opts) {
    opts = opts || {};
    var box = h('div');
    box.appendChild(h('div.status.t-' + (tone || 'muted') + (opts.large ? '.status-lg' : ''),
      h('span.status-dot'), h('span', text)));
    if (opts.sub) box.appendChild(h('div.status-sub', opts.sub));
    return printable(box);
  };

  /* 面板第一行的状态条：返回 <p>，内容往 p.body 里追加 */
  Dash.statusLine = function (tone) {
    var p = h('p.status.t-' + (tone || 'muted'));
    var body = h('span.grow');
    p.appendChild(h('span.status-dot')); p.appendChild(body);
    p.body = body;
    return p;
  };

  /* 型号的展示名：claude-opus-5-5 → Opus 5.5；别的照原样 */
  Dash.modelName = function (m) {
    var s = String(m || '');
    var c = /^claude-([a-z]+)-(\d+)-(\d+)/.exec(s);
    return c ? c[1].charAt(0).toUpperCase() + c[1].slice(1) + ' ' + c[2] + '.' + c[3] : s;
  };

  Dash.empty = function (title, sub, compact) {
    return h('div.empty' + (compact ? '.is-compact' : ''),
      h('div.empty-title', title || '等数据'), sub ? h('div.empty-sub', sub) : null);
  };

  /* 折叠：summary 是文字，content 是节点或数组，默认合上 */
  Dash.fold = function (summary, content, open) {
    var d = h('details.fold', h('summary', summary), h('div.fold-body', content));
    if (open) d.open = true;
    return d;
  };

  /* 数量级徽标：<b>几十</b><i>个</i> */
  Dash.magBadge = function (n, unit) {
    return printable(h('span.mag', Dash.mag(n), unit ? h('span.unit', unit) : null));
  };

  /* 带标题的卡片，面板自己要分块时用 */
  Dash.card = function (title, body, meta) {
    var head = (title || meta) ? h('div.card-head', title ? h('span.card-title', title) : null, meta ? h('span.card-meta', meta) : null) : null;
    return h('section.card', head, h('div.card-body', body));
  };

  /* 列表行。opts: lead 前导（签）、title、sub、meta 右侧小字、act 操作节点数组、tone 给整行左侧色条、seen 淡化 */
  Dash.row = function (o) {
    var cls = 'row' + (o.tone ? '.is-flag.t-' + o.tone : '') + (o.seen ? '.is-seen' : '');
    return h('div.' + cls,
      o.lead != null ? h('div.row-lead', o.lead) : null,
      h('div.row-main', h('span.row-title', o.title), o.sub ? h('span.row-sub', o.sub) : null),
      o.meta ? h('div.row-meta', o.meta) : null,
      o.act && o.act.length ? h('div.row-act', o.act) : null);
  };

  /* 指标块：label 小字、value 一句、sub 补充；tone 决定点的颜色，risk 时整块淡染 */
  Dash.tile = function (label, value, sub, tone) {
    return h('div.tile.t-' + (tone || 'muted'),
      h('div.tile-label', label),
      h('div.tile-value', h('span.dot'), h('span', value)),
      sub ? h('div.tile-sub', sub) : null);
  };

  /* 按"谁"挑一个色：from 里含 codex/gpt/astra → 青绿；含 claude → 紫；其余蓝灰。side 优先 */
  Dash.sideTone = function (from, side) {
    var s = String(side || from || '').toLowerCase();
    if (/codex|gpt|astra/.test(s)) return 'codex';
    if (/claude|sonnet|opus|haiku|fable|主会话|子代理/.test(s)) return 'claude';
    if (/author|作者/.test(s)) return 'author';
    return 'muted';
  };

  Dash.expired = function (item) {
    if (!item || !item.expires) return false;
    var t = parseTime(item.expires);
    return !isNaN(t) && t < Date.now();
  };

  /* ================= 已看标记（只存浏览器本地） ================= */

  var seenSet = null;
  function seenLoad() {
    if (seenSet) return seenSet;
    seenSet = {};
    try { JSON.parse(localStorage.getItem('dash.seen') || '[]').forEach(function (k) { seenSet[k] = 1; }); } catch (e) { /* 无本地存储就只在本次有效 */ }
    return seenSet;
  }
  Dash.seen = function (key) { return !!seenLoad()[key]; };
  Dash.markSeen = function (key, on) {
    var set = seenLoad();
    if (on === false) delete set[key]; else set[key] = 1;
    try { localStorage.setItem('dash.seen', JSON.stringify(Object.keys(set).slice(-400))); } catch (e) { /* 同上 */ }
    Dash.touch(['audit', 'audit-auto', 'notices']);
  };

  /* 需要作者看的事：合并模型挂的审计项、自动发现的待审项、等级为 decision 的通知。
     返回 { open: [...], seen: [...], done: [...] }，每项 { key, src, kind, label, tone, need, title, why, link, at, from, seen } */
  var AUTO_KIND = { shots: '新截图', poster: '新海报', report: '报告页', research: '待审调研', decision: '需定来信' };
  Dash.attention = function () {
    var open = [], seen = [], done = [];
    var a = store['audit'], au = store['audit-auto'], n = store['notices'];
    ((a && a.items) || []).forEach(function (it) {
      var o = { key: 'audit:' + it.id, src: 'audit', kind: 'audit', label: '待审', tone: Dash.sideTone(it.from), need: false,
        title: it.title, why: it.why, link: it.link, at: it.at, from: it.from };
      o.seen = Dash.seen(o.key);
      (it.status === 'done' ? done : (o.seen ? seen : open)).push(o);
    });
    ((au && au.items) || []).forEach(function (it) {
      var need = it.kind === 'decision';
      var o = { key: 'auto:' + it.id, src: 'auto', kind: it.kind, label: AUTO_KIND[it.kind] || '待审', tone: need ? 'risk' : 'auto', need: need,
        title: it.title, why: '', link: it.path, at: it.since, from: '' };
      o.seen = Dash.seen(o.key);
      (o.seen ? seen : open).push(o);
    });
    ((n && n.items) || []).forEach(function (it) {
      if (it.level !== 'decision' || Dash.expired(it)) return;
      var o = { key: 'notice:' + it.id, src: 'notice', kind: 'decision', label: '需定', tone: 'risk', need: true,
        title: it.text, why: '', link: it.link, at: it.at, from: it.from };
      o.seen = Dash.seen(o.key);
      (o.seen ? seen : open).push(o);
    });
    // 自动发现的项若已有模型挂的审计项指向同一个文件，只留后者
    var linked = {};
    open.concat(seen, done).forEach(function (o) { if (o.src === 'audit' && o.link) linked[o.link] = true; });
    function keep(o) { return !(o.src === 'auto' && o.link && linked[o.link]); }
    open = open.filter(keep); seen = seen.filter(keep);
    function sort(x, y) { return (y.need - x.need) || cmpDesc(x.at, y.at); }
    open.sort(sort); seen.sort(sort); done.sort(function (x, y) { return cmpDesc(x.at, y.at); });
    return { open: open, seen: seen, done: done };
  };

  /* ================= 数据 ================= */

  function signature(data) {
    try {
      return JSON.stringify(data, function (k, v) { return (this === data && (k === 'updated' || k === 'source')) ? undefined : v; });
    } catch (e) { return String(Math.random()); }
  }

  Dash.put = function (name, data) {
    var m = meta[name] || (meta[name] = {});
    var sig = signature(data);
    var changed = m.sig !== sig;
    store[name] = data;
    m.sig = sig; m.missing = false; m.tried = true;
    if (changed) {
      dependents(name).forEach(schedule);
      afterData();
    } else {
      refreshMeta();
    }
  };

  Dash.get = function (name) { return store[name] || null; };

  Dash.touch = function (names) {
    var set = {};
    names.forEach(function (n) { dependents(n).forEach(function (p) { set[p.id] = p; }); });
    Object.keys(set).forEach(function (id) { schedule(set[id]); });
    updateBadges();
  };

  function srcPath(s) { return DEMO ? 'data/demo/' + s.name + '.js' : s.path; }

  function loadData(s) {
    var m = meta[s.name] || (meta[s.name] = {});
    if (m.loading && Date.now() - m.loading < 15000) return;
    m.loading = Date.now();
    var old = document.querySelectorAll('script[data-dash="' + s.name + '"]');
    Array.prototype.forEach.call(old, function (e) { e.remove(); });
    var el = document.createElement('script');
    el.dataset.dash = s.name;
    el.src = srcPath(s) + '?t=' + Date.now();
    function done(ok) {
      m.loading = 0;
      if (!m.tried) {
        m.tried = true;
        if (!store[s.name]) m.missing = true;
        dependents(s.name).forEach(schedule);   // 第一次读完仍没有：从"读取中"变成"等数据"
      }
      if (!ok && !store[s.name]) m.missing = true;
      el.remove();
      afterData();
    }
    el.onload = function () { done(true); };
    el.onerror = function () { done(false); };
    document.head.appendChild(el);
  }

  function startData() {
    M.data.forEach(function (s) {
      loadData(s);
      setInterval(function () { if (!document.hidden) loadData(s); }, Math.max(5, s.every || 30) * 1000);
    });
    document.addEventListener('visibilitychange', function () {
      if (!document.hidden) M.data.forEach(loadData);
    });
  }

  /* ================= 面板 ================= */

  function dependents(name) {
    return Object.keys(panels).map(function (id) { return panels[id]; })
      .filter(function (p) { return p.needs.indexOf(name) >= 0; });
  }

  Dash.panel = function (def) {
    if (!def || !def.id) return;
    var p = panels[def.id] = Object.assign({ needs: [], order: 100 }, def);
    p.needs = p.needs || [];
    if (booted) mount(p);
  };

  function mount(p) {
    var page = document.getElementById('page-' + p.page);
    if (!page) { console.warn('[Dash] 面板 ' + p.id + ' 的页面 ' + p.page + ' 不在 manifest 里'); return; }
    var host = page.querySelector('.page-body');
    var box = h('div.panel' + (p.card === false ? '' : '.card'), { dataset: { panel: p.id, order: p.order } });
    if (p.card !== false) {
      p.metaEl = h('span.card-meta');
      if (p.title) box.appendChild(h('div.card-head', h('span.card-title', p.title), p.metaEl));
      p.bodyEl = h('div.card-body');
      box.appendChild(p.bodyEl);
    } else {
      p.bodyEl = box;
    }
    p.el = box;
    var after = null;
    Array.prototype.forEach.call(host.children, function (c) {
      if (!after && Number(c.dataset.order) > p.order) after = c;
    });
    host.insertBefore(box, after);
    schedule(p);
  }

  function schedule(p) {
    if (!p || !p.el) return;
    dirty[p.id] = p;
    if (!flushTimer) flushTimer = setTimeout(flush, 30);
  }

  function flush() {
    flushTimer = 0;
    var list = Object.keys(dirty).map(function (id) { return dirty[id]; });
    dirty = {};
    list.forEach(renderPanel);
    refreshMeta();
  }

  function renderPanel(p) {
    var data = {}, have = 0, missing = [], waiting = false;
    p.needs.forEach(function (n) {
      data[n] = store[n] || null;
      if (store[n]) have++;
      else {
        missing.push(n);
        if (!(meta[n] && meta[n].tried)) waiting = true;
      }
    });
    var ready = p.needs.length === 0 || (p.partial ? have > 0 : missing.length === 0);
    var body = p.bodyEl;
    p.el.classList.remove('is-error', 'is-loading');
    if (!ready) {
      if (waiting) { p.el.classList.add('is-loading'); body.replaceChildren(); return; }
      var names = missing.map(function (n) { return (srcs[n] && srcs[n].label) || n; }).join('、');
      body.replaceChildren(Dash.empty('等数据', names + ' 还没有数据，采集脚本跑过之后会自动出现'));
      return;
    }
    var opens = Array.prototype.map.call(body.querySelectorAll('details'), function (d) { return d.open; });
    var inner = h('div.panel-inner');
    body.replaceChildren(inner);
    try {
      p.render(inner, data);
    } catch (e) {
      console.error('[Dash] 面板 ' + p.id + ' 出错', e);
      p.el.classList.add('is-error');
      body.replaceChildren(Dash.empty('这块出错了', String(e && e.message || e)));
      return;
    }
    var ds = inner.querySelectorAll('details');
    if (ds.length === opens.length) Array.prototype.forEach.call(ds, function (d, i) { if (opens[i]) d.open = true; });
  }

  /* 卡片右上角的"更新于"、数据旧了变灰 */
  function refreshMeta() {
    Object.keys(panels).forEach(function (id) {
      var p = panels[id];
      if (!p.el) return;
      var oldest = NaN, stale = false;
      p.needs.forEach(function (n) {
        var s = srcs[n];
        if (!store[n] || !s || s.stale == null) return;
        var t = parseTime(store[n].updated);
        if (!isNaN(t) && (isNaN(oldest) || t < oldest)) oldest = t;
        if (Dash.isStale(n)) stale = true;
      });
      if (p.card !== false) {
        p.el.classList.toggle('is-stale', stale);
        if (p.metaEl) {
          p.metaEl.textContent = stale ? '数据旧了 · 更新于' + Dash.ago(oldest) : (isNaN(oldest) ? '' : '更新 ' + Dash.ago(oldest));
        }
      }
    });
    updateHeader();
  }

  /* ================= 页眉与导航 ================= */

  function headlineText() {
    var q = store['queue'], l = store['live'], parts = [];
    if (q && q.counts && q.counts['进行中']) parts.push('施工 ' + Dash.mag(q.counts['进行中']) + ' 项在做');
    if (l) {
      var running = (l.sessions || []).filter(function (s) { return s.state === 'running'; }).length;
      parts.push(running ? Dash.mag(running) + ' 个会话在跑' : '没有会话在跑');
    }
    if (q && q.stage) parts.push('阶段：' + q.stage);
    return parts.length ? parts.join(' · ') : '等数据';
  }

  function updateHeader() {
    var hl = document.getElementById('headline');
    if (hl) hl.textContent = headlineText();
    var dot = document.getElementById('fresh-dot'), txt = document.getElementById('fresh-text');
    if (!dot || !txt) return;
    var newest = NaN, anyStale = false, any = false;
    M.data.forEach(function (s) {
      var d = store[s.name];
      if (!d) return;
      any = true;
      if (s.stale == null) return;
      var t = parseTime(d.updated);
      if (!isNaN(t) && (isNaN(newest) || t > newest)) newest = t;
      if (Dash.isStale(s.name)) anyStale = true;
    });
    var tone = !any ? 'muted' : (anyStale ? 'warn' : 'ok');
    dot.className = 'dot t-' + tone;
    txt.textContent = !any ? '等数据' : (anyStale ? '部分数据旧了' : (isNaN(newest) ? '数据已就绪' : '数据 ' + Dash.ago(newest)));
    if (DEMO) txt.textContent = '演示数据';
    var pop = document.getElementById('fresh-pop');
    if (pop && !pop.hidden) fillPop(pop);
  }

  function fillPop(pop) {
    var rows = M.data.map(function (s) {
      var d = store[s.name], tone, text;
      if (!d) { tone = 'muted'; text = meta[s.name] && meta[s.name].tried ? '没有数据' : '读取中'; }
      else if (s.stale == null) { tone = 'ok'; text = Dash.ago(d.updated) + '改动'; }
      else if (Dash.isStale(s.name)) { tone = 'warn'; text = '旧了 · ' + Dash.ago(d.updated); }
      else { tone = 'ok'; text = Dash.ago(d.updated); }
      return h('div.pop-row.t-' + tone, h('span.dot'), h('span.grow', s.label || s.name), h('span.ago', text));
    });
    pop.replaceChildren(h('div.pop-title', DEMO ? '正在看演示数据（?demo）' : '数据新鲜度'), rows);
  }

  function updateBadges() {
    var nav = document.getElementById('nav');
    if (!nav) return;
    var att = Dash.attention();
    var needN = att.open.filter(function (x) { return x.need; }).length;
    var notice = ((store['notices'] && store['notices'].items) || []).filter(function (it) {
      return it.level === 'attention' && !Dash.expired(it) && !Dash.seen('notice:' + it.id);
    }).length;
    Dash.badge('audit', att.open.length ? String(att.open.length) : '', needN ? 'risk' : 'muted');
    Dash.badge('notices', notice ? String(notice) : '', 'warn');
  }

  Dash.badge = function (pageId, text, tone) {
    var a = document.querySelector('#nav a[data-page="' + pageId + '"]');
    if (!a) return;
    var b = a.querySelector('.nav-badge');
    if (!text) { if (b) b.remove(); return; }
    if (!b) { b = h('span.nav-badge'); a.appendChild(b); }
    b.className = 'nav-badge t-' + (tone || 'muted');
    b.textContent = text;
  };

  function afterData() {
    refreshMeta();
    updateBadges();
  }

  /* ================= 路由 ================= */

  Dash.go = function (id) { location.hash = '#' + id; };
  Dash.page = function () { return currentPage; };

  function route() {
    var id = (location.hash || '').replace(/^#/, '');
    if (!M.pages.some(function (p) { return p.id === id; })) id = M.pages.length ? M.pages[0].id : '';
    currentPage = id;
    M.pages.forEach(function (p) {
      var sec = document.getElementById('page-' + p.id);
      if (sec) sec.classList.toggle('is-active', p.id === id);
    });
    Array.prototype.forEach.call(document.querySelectorAll('#nav a'), function (a) {
      if (a.dataset.page === id) a.setAttribute('aria-current', 'page'); else a.removeAttribute('aria-current');
    });
    var pg = M.pages.filter(function (p) { return p.id === id; })[0];
    document.title = (pg ? pg.title + ' · ' : '') + '作者观察台';
    window.scrollTo(0, 0);
  }

  /* ================= 启动 ================= */

  function loadAsset(kind, url, onerror) {
    var el = kind === 'css' ? h('link', { rel: 'stylesheet', href: url }) : h('script', { src: url });
    el.onerror = onerror || function () { console.warn('[Dash] 加载失败：' + url); };
    document.head.appendChild(el);
  }

  function boot() {
    var nav = document.getElementById('nav'), main = document.getElementById('main');
    M.pages.forEach(function (p) {
      nav.appendChild(h('a', { href: '#' + p.id, dataset: { page: p.id } }, h('span', p.title)));
      main.appendChild(h('section.page', { id: 'page-' + p.id },
        h('div.page-head', h('h1', p.title), p.desc ? h('p', p.desc) : null),
        h('div.page-body')));
    });

    var btn = document.getElementById('fresh'), pop = document.getElementById('fresh-pop');
    btn.addEventListener('click', function (e) {
      e.stopPropagation();
      pop.hidden = !pop.hidden;
      btn.setAttribute('aria-expanded', String(!pop.hidden));
      if (!pop.hidden) fillPop(pop);
    });
    document.addEventListener('click', function (e) {
      if (!pop.hidden && !pop.contains(e.target)) { pop.hidden = true; btn.setAttribute('aria-expanded', 'false'); }
    });

    window.addEventListener('hashchange', route);
    route();
    booted = true;

    var stamp = '?t=' + Date.now();
    M.panels.forEach(function (pm) {
      if (pm.css) loadAsset('css', 'app/' + pm.file.replace(/\.js$/, '.css') + stamp);
      loadAsset('js', 'app/' + pm.file + stamp, function () {
        console.warn('[Dash] 面板文件没找到：' + pm.file);
        if (panels[pm.id]) return;
        Dash.panel({
          id: pm.id, page: pm.page, title: '', order: 100, needs: [], card: true,
          render: function (el) { el.appendChild(Dash.empty('这块还没有写好', pm.file, true)); }
        });
      });
    });
    Object.keys(panels).forEach(function (id) { mount(panels[id]); });   // 理论上面板在 boot 之后才到，这里兜底

    startData();
    setInterval(refreshMeta, 20000);
    updateHeader();
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot);
  else boot();
})();
