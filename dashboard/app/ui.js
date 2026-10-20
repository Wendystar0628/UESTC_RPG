/* 观察台通用格式化与小组件：确切个数、时刻、链接、h() 建节点、卡片、行、表格、树。
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

  /* ================= 公开：格式化 ================= */

  Dash.esc = esc;
  Dash.h = h;
  Dash.parseTime = parseTime;

  /* 测得到的个数一律写确切值（千位加逗号）；没有数据写 — */
  Dash.num = function (n) {
    if (n == null || isNaN(n)) return '—';
    return Math.floor(n).toLocaleString('en-US');
  };

  /* 确切时刻：今天写 HH:MM，不是今天写 M-D HH:MM；解析不了写 — */
  function pad2(n) { return (n < 10 ? '0' : '') + n; }
  Dash.at = function (iso) {
    var t = parseTime(iso);
    if (isNaN(t)) return '—';
    var d = new Date(t), now = new Date();
    var hm = pad2(d.getHours()) + ':' + pad2(d.getMinutes());
    var same = d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth() && d.getDate() === now.getDate();
    return same ? hm : (d.getMonth() + 1) + '-' + d.getDate() + ' ' + hm;
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

  function link(path) {
    if (!path) return '#';
    path = String(path);
    if (/^[a-z][a-z0-9+.-]*:/i.test(path)) return path;
    path = path.replace(/\\/g, '/').replace(/^(\.\/)+/, '').replace(/^\/+/, '');
    return '../' + path.split('/').map(encodeURIComponent).join('/');
  }

  Dash.chip = function (text, tone) {
    return printable(h('span.chip.t-' + (tone || 'muted'), text));
  };

  /* ================= 公开：小组件 ================= */

  /* 指向仓库文件的链接（新标签页打开） */
  Dash.a = function (path, text, cls) {
    return printable(h('a', { href: link(path), target: '_blank', rel: 'noopener', cls: cls }, text == null ? path : text));
  };

  /* 每块第一行的一句状态：tone 取 ok/warn/risk/muted 等 */
  Dash.status = function (text, tone) {
    return printable(h('div', h('div.status.t-' + (tone || 'muted'), h('span.status-dot'), h('span', text))));
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

  /* 列表行。opts: lead 前导（签）、title、meta 右侧小字、act 操作节点数组 */
  Dash.row = function (o) {
    return h('div.row',
      o.lead != null ? h('div.row-lead', o.lead) : null,
      h('div.row-main', h('span.row-title', o.title)),
      o.meta ? h('div.row-meta', o.meta) : null,
      o.act && o.act.length ? h('div.row-act', o.act) : null);
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

  /* ================= 树（可展开、收起） =================
     Dash.tree({ key, nodes, label(n, depth), kids(n)?, id(n)?, openDepth? })  返回节点
       key        这棵树展开状态的名字（按页面分开）；只记在本页内存里，重绘时保持，刷新页面后回到默认
       nodes      顶层节点数组；kids(n) 给子节点数组（默认取 n.children），没有或为空就是叶子
       label      返回这一行的内容（节点）；里面的链接（Dash.a）回车时会被打开
       id         节点的稳定标识（默认按位置）；数据刷新后据此保持展开状态
       openDepth  默认展开的层数（0 只露顶层，1 展开顶层，默认 1）
     键盘：上下移动，右键展开（再按进入第一个子节点），左键收起（再按回到父节点），回车打开本行链接（没有则展开或收起），空格展开或收起。 */
  var treeMem = {};   // 树名 -> { id: 1 展开 / 0 收起 }

  Dash.tree = function (o) {
    var kidsOf = function (n) { var k = o.kids ? o.kids(n) : n.children; return Array.isArray(k) ? k : []; };
    var openDepth = o.openDepth == null ? 1 : o.openDepth;
    var over = treeMem[o.key] || (treeMem[o.key] = {});              // id -> 1 展开 / 0 收起，只记作者动过的
    var branches = [];                       // 全部有子节点的 id，给"全部展开"用
    var body = h('div.tree-body', { role: 'tree' });

    function idOf(n, path) { return o.id ? String(o.id(n)) : path; }
    function collect(list, path) {
      list.forEach(function (n, i) {
        var p = path + '/' + i, k = kidsOf(n);
        if (k.length) { branches.push(idOf(n, p)); collect(k, p); }
      });
    }
    collect(o.nodes || [], '');

    function isOpen(id, depth) { return id in over ? !!over[id] : depth < openDepth; }

    function build(list, depth, path) {
      var group = h('div.tree-group', { role: 'group' });
      list.forEach(function (n, i) {
        var p = path + '/' + i, id = idOf(n, p), k = kidsOf(n);
        var open = k.length > 0 && isOpen(id, depth);
        var row = h('div.tree-row', { role: 'treeitem', tabindex: '-1', 'aria-level': String(depth + 1), style: '--depth:' + depth },
          h('span.tree-caret' + (k.length ? '' : '.is-leaf')), o.label(n, depth));
        var item = h('div.tree-item' + (open ? '.is-open' : ''), row);
        item.treeId = id;
        item.treeDepth = depth;
        if (k.length) {
          row.setAttribute('aria-expanded', String(open));
          item.treeKids = k;
          item.treePath = p;
          if (open) item.appendChild(build(k, depth + 1, p));
        }
        group.appendChild(item);
      });
      Array.prototype.forEach.call(group.querySelectorAll('a'), function (a) { a.tabIndex = -1; });
      return group;
    }

    function setOpen(item, on) {
      if (!item.treeKids || item.classList.contains('is-open') === on) return;
      over[item.treeId] = on ? 1 : 0;
      if (on && !item.querySelector(':scope > .tree-group')) item.appendChild(build(item.treeKids, item.treeDepth + 1, item.treePath));
      item.classList.toggle('is-open', on);
      item.firstChild.setAttribute('aria-expanded', String(on));
    }

    function all(on) {
      branches.forEach(function (id) { over[id] = on ? 1 : 0; });
      show();
    }

    function show() {
      body.replaceChildren(build(o.nodes || [], 0, ''));
      var first = body.querySelector('.tree-row');
      if (first) first.tabIndex = 0;
    }

    function visibleRows() {
      return Array.prototype.filter.call(body.querySelectorAll('.tree-row'), function (r) { return r.offsetParent !== null; });
    }
    function focusRow(r) {
      if (!r) return;
      var cur = body.querySelector('.tree-row[tabindex="0"]');
      if (cur && cur !== r) cur.tabIndex = -1;
      r.tabIndex = 0;
      r.focus();
    }

    body.addEventListener('click', function (e) {
      var row = e.target.closest && e.target.closest('.tree-row');
      if (!row) return;
      if (e.target.closest('a')) return;
      focusRow(row);
      var item = row.parentNode;
      if (item.treeKids) setOpen(item, !item.classList.contains('is-open'));
    });

    body.addEventListener('keydown', function (e) {
      var row = e.target.closest && e.target.closest('.tree-row');
      if (!row || e.target !== row) return;
      var item = row.parentNode, openNow = item.classList.contains('is-open'), rows, i;
      switch (e.key) {
        case 'ArrowDown': case 'ArrowUp':
          rows = visibleRows(); i = rows.indexOf(row);
          focusRow(rows[i + (e.key === 'ArrowDown' ? 1 : -1)]);
          break;
        case 'ArrowRight':
          if (item.treeKids && !openNow) setOpen(item, true);
          else if (item.treeKids) focusRow(item.querySelector(':scope > .tree-group > .tree-item > .tree-row'));
          break;
        case 'ArrowLeft':
          if (item.treeKids && openNow) setOpen(item, false);
          else if (item.parentNode.parentNode.classList.contains('tree-item')) focusRow(item.parentNode.parentNode.firstChild);
          break;
        case 'Home': focusRow(visibleRows()[0]); break;
        case 'End': rows = visibleRows(); focusRow(rows[rows.length - 1]); break;
        case 'Enter': {
          var a = row.querySelector('a[href]');
          if (a) a.click(); else if (item.treeKids) setOpen(item, !openNow);
          break;
        }
        case ' ':
          if (item.treeKids) setOpen(item, !openNow);
          break;
        default: return;
      }
      e.preventDefault();
    });

    show();
    var tools = branches.length ? h('div.tree-tools',
      h('button.btn', { type: 'button', on: { click: function () { all(true); } } }, '全部展开'),
      h('button.btn', { type: 'button', on: { click: function () { all(false); } } }, '全部收起')) : null;
    return h('div.tree', tools, body);
  };
  /* 表格：cols [{ key, label, num?, width? }]，rows 是对象数组；格子取 row[key]，可以是文字或节点；
     cols[i].cell(row) 给定时用它算格子；opts.onRow(row) 点一行；opts.selected(row) 为真的行高亮；opts.empty 空表时的话 */
  Dash.table = function (cols, rows, opts) {
    opts = opts || {};
    if (!rows || !rows.length) return Dash.empty(opts.empty || '没有记录', '', true);
    var head = h('tr', cols.map(function (c) { return h('th' + (c.num ? '.is-num' : ''), { style: c.width ? 'width:' + c.width : null }, c.label); }));
    var body = rows.map(function (r) {
      var tr = h('tr' + (opts.onRow ? '.is-click' : '') + (opts.selected && opts.selected(r) ? '.is-selected' : ''),
        cols.map(function (c) {
          var v = c.cell ? c.cell(r) : r[c.key];
          return h('td' + (c.num ? '.is-num' : ''), v == null || v === '' ? '—' : v);
        }));
      if (opts.onRow) tr.addEventListener('click', function (e) { if (!e.target.closest('a,button')) opts.onRow(r); });
      return tr;
    });
    return h('div.tbl-wrap', h('table.tbl', h('thead', head), h('tbody', body)));
  };

  /* 一排状态灯：items [{ label, tone: [组, 键], sub?, href? }]，点了去 href */
  Dash.lights = function (items) {
    return h('div.lights', items.map(function (it) {
      var v = Dash.colorVar(it.tone[0], it.tone[1]);
      var inner = [h('span.light-dot', { style: '--tone: var(' + v + ')' }), h('span.light-text', h('span.light-label', it.label), it.sub ? h('span.light-sub', it.sub) : null)];
      return it.href ? h('a.light', { href: it.href }, inner) : h('span.light', inner);
    }));
  };

  /* 页内小分段开关：options [{ id, label }]，value 当前值，onPick(id) */
  Dash.segment = function (options, value, onPick) {
    return h('div.segment', { role: 'tablist' }, options.map(function (o) {
      return h('button.segment-item', { type: 'button', 'aria-pressed': String(o.id === value), on: { click: function () { onPick(o.id); } } }, o.label);
    }));
  };
})();
