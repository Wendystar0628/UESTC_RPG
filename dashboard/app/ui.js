/* 观察台通用格式化与小组件：h() 建节点、确切个数与时刻、状态句、空状态、折叠、指标卡、时间线、树、点阵、灯、分段签、抽屉、悬停框。
   依赖 core.js 先加载；色表 palette.js 在后面加载，组件在调用时才取色。接口见 dashboard/README.md §3。 */
(function () {
  'use strict';

  var Dash = window.Dash;
  if (!Dash.internal) return;   // 不经本地服务打开：core 已整页只写一句

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

  /* h('div.row.big', { title:'..', on:{click:fn}, text:'..', dataset:{..} }, 子节点...)  子节点可以是节点、文字、数组 */
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
        if (k === 'cls') el.className += (el.className ? ' ' : '') + v;
        else if (k === 'text') el.textContent = v;
        else if (k === 'on') Object.keys(v).forEach(function (ev) { el.addEventListener(ev, v[ev]); });
        else if (k === 'dataset') Object.keys(v).forEach(function (d) { if (v[d] != null) el.dataset[d] = v[d]; });
        else el.setAttribute(k, v === true ? '' : v);
      });
    }
    appendKids(el, kids);
    return el;
  }

  function parseTime(iso) {
    if (typeof iso === 'number') return iso;
    var t = Date.parse(iso);
    return isNaN(t) ? NaN : t;
  }

  Dash.esc = esc;
  Dash.h = h;
  Dash.parseTime = parseTime;

  /* ---------- 数与时刻 ---------- */

  /* 确切值：千位加逗号，小数留一位；没有数据写 — */
  Dash.num = function (n) {
    if (n == null || isNaN(n)) return '—';
    return Number(n).toLocaleString('en-US', { maximumFractionDigits: 1 });
  };

  function pad2(n) { return (n < 10 ? '0' : '') + n; }
  /* 今天写 HH:MM，不是今天写 M-D HH:MM；解析不了写 — */
  Dash.at = function (iso) {
    var t = parseTime(iso);
    if (isNaN(t)) return '—';
    var d = new Date(t), now = new Date();
    var hm = pad2(d.getHours()) + ':' + pad2(d.getMinutes());
    var same = d.toDateString() === now.toDateString();
    return same ? hm : (d.getMonth() + 1) + '-' + d.getDate() + ' ' + hm;
  };

  /* M 月 D 日 */
  Dash.day = function (iso) {
    var t = parseTime(iso);
    if (isNaN(t)) return '—';
    var d = new Date(t);
    return (d.getMonth() + 1) + ' 月 ' + d.getDate() + ' 日';
  };

  /* 分钟数："45 分钟""2 小时 5 分钟""3 天 4 小时"；缺事实写 — */
  Dash.minutes = function (m) {
    if (m == null || !isFinite(m)) return '—';
    m = Math.max(0, Math.round(m));
    if (m < 60) return m + ' 分钟';
    if (m < 1440) return Math.floor(m / 60) + ' 小时' + (m % 60 ? ' ' + (m % 60) + ' 分钟' : '');
    var hr = Math.floor((m % 1440) / 60);
    return Math.floor(m / 1440) + ' 天' + (hr ? ' ' + hr + ' 小时' : '');
  };

  /* 距此刻多少分钟（不小于 0）；解析不了是 null。施工页、团队页要分钟数做排序或刷新文字时用它 */
  Dash.sinceMin = function (iso) { var t = parseTime(iso); return isNaN(t) ? null : Math.max(0, (Date.now() - t) / 60000); };

  /* "12 分钟前""刚刚" */
  Dash.ago = function (iso) {
    var m = Dash.sinceMin(iso);
    if (m == null) return '—';
    return m < 1 ? '刚刚' : Dash.minutes(m) + '前';
  };

  /* "已 2 小时 5 分钟" */
  Dash.since = function (iso) {
    var m = Dash.sinceMin(iso);
    return m == null ? '—' : '已 ' + Dash.minutes(m);
  };

  /* 占比："37%"；分母是 0 或 null 写 — */
  Dash.pct = function (a, b) {
    if (a == null || !b || !isFinite(a) || !isFinite(b)) return '—';
    return Math.round(a * 100 / b) + '%';
  };

  /* ---------- 小件 ---------- */

  /* 仓库相对路径 → 本地服务上的链接 */
  function href(path) {
    path = String(path);
    if (/^[a-z][a-z0-9+.-]*:/i.test(path)) return path;
    path = path.replace(/\\/g, '/').replace(/^(\.\/)+/, '').replace(/^\/+/, '');
    return '../' + path.split('/').map(encodeURIComponent).join('/');
  }

  /* 打开仓库里的产物（新标签页）；页面上只写动作，不露路径 */
  Dash.open = function (path, text, cls) {
    return h('a' + (cls ? '.' + cls : '.btn'), { href: href(path), target: '_blank', rel: 'noopener' }, text || '打开');
  };

  Dash.status = function (text, tone) {
    return h('div.status.t-' + (tone || 'muted'), h('span.status-dot'), h('span', text));
  };

  Dash.empty = function (title, sub, compact) {
    return h('div.empty' + (compact ? '.is-compact' : ''),
      h('div.empty-title', title || '等数据'), sub ? h('div.empty-sub', sub) : null);
  };

  Dash.fold = function (summary, content, open) {
    var d = h('details.fold', h('summary', summary), h('div.fold-body', content));
    if (open) d.open = true;
    return d;
  };

  Dash.sideKey = function (who) {
    var s = String(who || '').toLowerCase();
    if (/codex|gpt/.test(s)) return 'codex';
    if (/claude|opus|sonnet|haiku/.test(s)) return 'claude';
    if (/author|作者/.test(s)) return 'author';
    return 'muted';
  };

  Dash.modelName = function (m) {
    var s = String(m || '');
    var c = /^claude-([a-z]+)-(\d+)-(\d+)/.exec(s);
    return c ? c[1].charAt(0).toUpperCase() + c[1].slice(1) + ' ' + c[2] + '.' + c[3] : s;
  };

  var EFFORT = { low: '低', medium: '中', high: '高', xhigh: '很高', max: '最高' };
  Dash.effortName = function (e) { return EFFORT[e] || e || '默认'; };

  /* 施工阶段顺序与名称均来自服务元数据；counts 是 progress.counts */
  var STAGES = window.DashQueueStates.map(function (x) { return x.key; });
  Dash.stageKeys = STAGES;
  Dash.flowbar = function (counts, link) {
    if (!counts || !STAGES.every(function (k) { return typeof counts[k] === 'number' && isFinite(counts[k]); })) return Dash.empty('流水线各段的行数未确认', '', true);
    return h('div.flowbar', { role: 'img', 'aria-label': STAGES.map(function (k) { return Dash.label('stage', k) + ' ' + counts[k]; }).join('，') },
      STAGES.map(function (k) {
        var n = counts[k];
        return h((link ? 'a' : 'div') + '.flowbar-seg' + (n ? '' : '.is-zero') + (k === 'paused' || k === 'waiting' || k === 'ready' ? '.is-light' : ''),
          { href: link ? link(k) : null, style: '--n:' + Math.max(n, 1) + '; --tone: var(' + Dash.colorVar('stage', k) + ')' },
          h('span.flowbar-name', Dash.label('stage', k)), h('span.flowbar-n', Dash.num(n)));
      }));
  };

  Dash.item = function (id, title, side) {
    return h('div.item', h('span.id', id), h('span.item-title', title), side ? h('span.item-side', side) : null);
  };

  /* 灯：ok 正常、warn 要看、bad 有问题；null 是空心灰"未确认" */
  var LEVEL_TONE = { ok: 'ok', warn: 'warn', bad: 'risk' };
  Dash.lamp = function (level) {
    var t = LEVEL_TONE[level];
    return h('span.lamp' + (t ? '.t-' + t : '.is-unknown'), { title: t ? Dash.label('level', level) : '未确认', role: 'img', 'aria-label': t ? Dash.label('level', level) : '未确认' });
  };

  /* 指标卡：数、单位、一句 note、趋势箭头；vs 是箭头比的是什么（悬停显示），不传就不写；点了去 href */
  var TREND = { better: ['ok', '↑ 变好'], worse: ['warn', '↓ 变坏'], same: ['muted', '→ 持平'] };
  Dash.kpi = function (k) {
    var tr = TREND[k.trend];
    var v = k.value == null ? '—' : typeof k.value === 'number' ? Dash.num(k.value) : k.value;
    return h(k.href ? 'a.kpi' : 'div.kpi', { href: k.href || null, title: k.value == null ? '未确认' : null },
      h('div.kpi-label', k.label),
      h('div.kpi-value', v, k.unit && k.value != null ? h('span.kpi-unit', k.unit) : null),
      h('div.kpi-foot', k.note ? h('span.kpi-note', k.note) : null, tr ? h('span.kpi-trend.t-' + tr[0], { title: k.vs || null }, tr[1]) : null));
  };

  /* 时间线：items [{at, tone: [组, 键], tag, side?, title, sub?, body?}]；按天分组（compact 不分组、更紧）；有 body 的点开展开 */
  Dash.timeline = function (items, opts) {
    opts = opts || {};
    var list = h('div.tl' + (opts.compact ? '.is-compact' : ''));
    var lastDay = null, today = new Date().toDateString();
    items.forEach(function (x, i) {
      var t = parseTime(x.at), dkey = isNaN(t) ? '' : new Date(t).toDateString();
      if (!opts.compact && dkey !== lastDay) {
        lastDay = dkey;
        list.appendChild(h('div.tl-day', isNaN(t) ? '时刻未确认' : Dash.day(x.at) + (dkey === today ? ' · 今天' : '')));
      }
      var tone = x.tone || ['side', 'muted'];
      var head = h('div.tl-row', { style: '--tone: var(' + Dash.colorVar(tone[0], tone[1]) + ')' },
        h('span.tl-dot'),
        h('span.tl-at', opts.compact ? Dash.at(x.at) : (isNaN(t) ? '—' : Dash.at(x.at).slice(-5))),
        x.tag ? Dash.mark(tone[0], tone[1], x.tag) : null,
        x.side ? Dash.mark('side', x.side) : null,
        h('span.tl-main', h('span.tl-title', x.title), x.sub ? h('span.tl-sub', x.sub) : null));
      var row;
      if (x.body != null) {
        row = h('details.tl-item', h('summary', head), h('div.tl-body', x.body));
      } else {
        row = h('div.tl-item', head);
      }
      if (opts.onClick) {
        row.classList.add('is-click');
        head.addEventListener('click', function () { opts.onClick(x, i); });
      }
      list.appendChild(row);
    });
    return list;
  };

  /* 树：nodes [{key, label, sub?, tone?, count?, kids?, open?, current?}]；onClick(node) 点任一行；
     opts.filter(node, 文字) → bool 是命中谓词（缺省比 label、sub、key）；返回的节点有 .filter(文字)，树自己过滤并展开命中路径 */
  Dash.tree = function (nodes, opts) {
    opts = opts || {};
    var match = opts.filter || function (n, text) {
      return [n.label, n.sub, n.key].some(function (s) { return s != null && String(s).toLowerCase().indexOf(text) >= 0; });
    };
    var recs = [];
    function build(n, depth) {
      var row = h('div.tree-row' + (n.current ? '.is-current' : ''), { style: '--depth:' + depth },
        n.tone ? h('span.dot', { style: '--tone: var(' + Dash.colorVar(n.tone[0], n.tone[1]) + ')' }) : null,
        h('span.tree-label', n.label),
        n.sub ? h('span.tree-sub', n.sub) : null,
        n.count != null ? h('span.tree-count', Dash.num(n.count)) : null);
      if (opts.onClick) row.addEventListener('click', function () { opts.onClick(n); });
      var el, kids = [];
      if (n.kids && n.kids.length) {
        var box = h('div.tree-kids');
        el = h('details.tree-node', h('summary', row), box);
        el.open = !!n.open;
        kids = n.kids.map(function (k) { var r = build(k, depth + 1); box.appendChild(r.el); return r; });
      } else {
        el = h('div.tree-node.is-leaf', row);
      }
      var rec = { node: n, el: el, kids: kids };
      recs.push(rec);
      return rec;
    }
    var tops = nodes.map(function (n) { return build(n, 0); });
    var root = h('div.tree', tops.map(function (r) { return r.el; }));
    function apply(rec, text) {
      var hitKid = rec.kids.map(function (k) { return apply(k, text); }).some(Boolean);
      var hit = !text || match(rec.node, text) || hitKid;
      rec.el.hidden = !hit;
      if (rec.kids.length) rec.el.open = text ? hitKid : !!rec.node.open;
      return hit;
    }
    root.filter = function (text) {
      text = String(text || '').trim().toLowerCase();
      var any = tops.map(function (r) { return apply(r, text); }).some(Boolean);
      root.classList.toggle('is-none', !any);
    };
    return root;
  };

  /* 点阵：cells [{tone, tip}]，一格一项；onHover(cell, 下标) */
  Dash.dots = function (cells, opts) {
    opts = opts || {};
    return h('div.dots', cells.map(function (c, i) {
      var t = c.tone || ['side', 'muted'];
      return h('span.dots-cell', {
        style: '--tone: var(' + Dash.colorVar(t[0], t[1]) + ')', dataset: { tip: c.tip },
        on: opts.onHover ? { mouseenter: function () { opts.onHover(c, i); } } : null
      });
    }));
  };

  /* 分段签：options [{key, label, tone?}]（也收字符串）；单选 onChange(键)，多选 onChange(新的键数组) */
  Dash.chips = function (options, value, onChange, opts) {
    var multi = opts && opts.multi;
    var cur = multi ? (value || []).slice() : value;
    return h('div.seg' + (multi ? '.is-multi' : ''), { role: 'group' }, options.map(function (o) {
      if (typeof o !== 'object') o = { key: o, label: String(o) };
      var on = multi ? cur.indexOf(o.key) >= 0 : cur === o.key;
      return h('button.seg-btn' + (on ? '.is-on' : ''), {
        type: 'button', 'aria-pressed': String(on),
        style: o.tone ? '--tone: var(' + Dash.colorVar(o.tone[0], o.tone[1]) + ')' : null,
        on: { click: function () {
          if (!multi) { if (!on) onChange(o.key); return; }
          onChange(on ? cur.filter(function (k) { return k !== o.key; }) : cur.concat([o.key]));
        } }
      }, o.tone ? h('span.dot') : null, o.label);
    }));
  };

  /* 每份数据的新鲜度：stale、ratio 是后端随 checked 推送给的（ratio = 已用掉旧期限的比例），null 是后端不判断。调用它的面板随推送心跳重画 */
  Dash.freshness = function () {
    Dash.internal.beginFresh();
    return window.DashManifest.data.map(function (s) {
      var f = Dash.internal.freshOf(s.name);
      return { name: s.name, label: s.label, checked: Dash.checkedAt(s.name), stale: f.stale, ratio: f.ratio };
    });
  };

  /* ---------- 抽屉：从右侧滑出，Esc 或点外面关掉，路由不变 ---------- */
  var drawerEl = null;
  function closeDrawer() {
    if (!drawerEl) return;
    if (Dash.charts) Dash.charts.release(drawerEl);
    drawerEl.remove();
    drawerEl = null;
    document.removeEventListener('keydown', onKey);
  }
  function onKey(e) { if (e.key === 'Escape') closeDrawer(); }
  Dash.drawer = function (o) {
    closeDrawer();
    var panel = h('aside.drawer', { role: 'dialog', 'aria-label': o.title },
      h('header.drawer-head',
        h('div.grow', h('h2.drawer-title', o.title), o.sub ? h('p.drawer-sub', o.sub) : null),
        h('button.btn.drawer-x', { type: 'button', 'aria-label': '关掉', on: { click: closeDrawer } }, '关掉')),
      h('div.drawer-body', o.body));
    drawerEl = h('div.drawer-wrap', h('div.drawer-back', { on: { click: closeDrawer } }), panel);
    document.body.appendChild(drawerEl);
    document.addEventListener('keydown', onKey);
    requestAnimationFrame(function () { if (drawerEl) drawerEl.classList.add('is-open'); });
    return panel;
  };
  Dash.drawer.close = closeDrawer;

  /* ---------- 悬停框：带 data-tip 的元素（ⓘ、点阵格）统一用它 ---------- */
  var tipEl = null;
  Dash.tip = {
    show: function (lines, x, y) {
      if (!tipEl) { tipEl = h('div.tipbox', { role: 'tooltip' }); document.body.appendChild(tipEl); }
      tipEl.replaceChildren.apply(tipEl, [].concat(lines).filter(Boolean).map(function (l, i) { return h(i ? 'div' : 'b', l); }));
      tipEl.hidden = false;
      var w = tipEl.offsetWidth, ht = tipEl.offsetHeight;
      tipEl.style.left = Math.max(8, Math.min(x + 12, innerWidth - w - 8)) + 'px';
      tipEl.style.top = (y + 16 + ht > innerHeight ? y - ht - 10 : y + 16) + 'px';
    },
    hide: function () { if (tipEl) tipEl.hidden = true; }
  };
  document.addEventListener('mouseover', function (e) {
    var t = e.target.closest && e.target.closest('[data-tip]');
    if (t) Dash.tip.show(String(t.dataset.tip).split('\n'), e.clientX, e.clientY);
  });
  document.addEventListener('mouseout', function (e) {
    var t = e.target.closest && e.target.closest('[data-tip]');
    if (t && !t.contains(e.relatedTarget)) Dash.tip.hide();
  });
  document.addEventListener('focusin', function (e) {
    var t = e.target.closest && e.target.closest('[data-tip]');
    if (t) { var r = t.getBoundingClientRect(); Dash.tip.show(String(t.dataset.tip).split('\n'), r.left, r.bottom - 8); }
  });
  document.addEventListener('focusout', function () { Dash.tip.hide(); });
})();
