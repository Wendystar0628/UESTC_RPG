/* 观察台通用格式化与小组件：数量级、时间、链接、h() 建节点、卡片/行/指标块、已看标记、待看汇总。
   依赖 core.js 先加载（取 Dash.internal 里的数据仓与数据源）；接口见 dashboard/README.md §3。 */
(function () {
  'use strict';

  var Dash = window.Dash;
  var store = Dash.internal.store;
  var srcs = Dash.internal.srcs;
  var DEMO = Dash.demo;

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
  Dash.parseTime = parseTime;

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
})();
