/* 观察台通用格式化与小组件：h() 建节点、确切个数与时刻、状态句、空状态、折叠、指挥与模型的中文名。
   依赖 core.js 先加载（取 Dash.internal 里的数据仓与数据源）；接口见 dashboard/README.md §3。 */
(function () {
  'use strict';

  var Dash = window.Dash;
  var store = Dash.internal.store;
  var srcs = Dash.internal.srcs;
  var DEMO = Dash.demo;

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
        if (k === 'cls') el.className += (el.className ? ' ' : '') + v;
        else if (k === 'text') el.textContent = v;
        else if (k === 'on') Object.keys(v).forEach(function (ev) { el.addEventListener(ev, v[ev]); });
        else if (k === 'dataset') Object.keys(v).forEach(function (d) { el.dataset[d] = v[d]; });
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

  /* 数一律写确切值（千位加逗号，小数留一位，如"19.9 万行"）；没有数据写 — */
  Dash.num = function (n) {
    if (n == null || isNaN(n)) return '—';
    return Number(n).toLocaleString('en-US', { maximumFractionDigits: 1 });
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

  /* 日期：M 月 D 日 */
  Dash.day = function (iso) {
    var t = parseTime(iso);
    if (isNaN(t)) return '—';
    var d = new Date(t);
    return (d.getMonth() + 1) + ' 月 ' + d.getDate() + ' 日';
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
    // 版本索引在管的数据：内容没变后端不重写，updated 是"上次变化"；索引按时刷新就说明这份是最新的
    var versions = store.index && store.index.versions;
    if (!s.poll && Dash.internal.indexState() === 'ready' && versions && name in versions) return false;
    return Dash.stale(store[name], s.stale);
  };

  /* 仓库相对路径 → 相对 dashboard/ 的链接 */
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

  /* 一句状态：tone 取 ok/warn/risk/muted 等 */
  Dash.status = function (text, tone) {
    return h('div.status.t-' + (tone || 'muted'), h('span.status-dot'), h('span', text));
  };

  Dash.empty = function (title, sub, compact) {
    return h('div.empty' + (compact ? '.is-compact' : ''),
      h('div.empty-title', title || '等数据'), sub ? h('div.empty-sub', sub) : null);
  };

  /* 折叠：summary 是文字或节点，content 是节点或数组，默认合上 */
  Dash.fold = function (summary, content, open) {
    var d = h('details.fold', h('summary', summary), h('div.fold-body', content));
    if (open) d.open = true;
    return d;
  };

  /* 指挥或负责方 → 色键：含 codex/gpt → codex；含 claude → claude；作者 → author */
  Dash.sideKey = function (who) {
    var s = String(who || '').toLowerCase();
    if (/codex|gpt/.test(s)) return 'codex';
    if (/claude|opus|sonnet|haiku/.test(s)) return 'claude';
    if (/author|作者/.test(s)) return 'author';
    return 'muted';
  };

  /* 型号的展示名：claude-opus-5-5 → Opus 5.5；别的照原样 */
  Dash.modelName = function (m) {
    var s = String(m || '');
    var c = /^claude-([a-z]+)-(\d+)-(\d+)/.exec(s);
    return c ? c[1].charAt(0).toUpperCase() + c[1].slice(1) + ' ' + c[2] + '.' + c[3] : s;
  };

  /* 推理档的中文名 */
  var EFFORT = { low: '低', medium: '中', high: '高', xhigh: '很高', max: '最高' };
  Dash.effortName = function (e) { return EFFORT[e] || e || '默认'; };

  /* 任务流水线比例条（首页、进展页共用）：counts 是 progress.counts，每段长度按个数；link(stage) 给每段的跳转地址 */
  var STAGES = ['queued', 'doing', 'finishing', 'paused'];
  Dash.stageKeys = STAGES;
  Dash.flowbar = function (counts, link) {
    return h('div.flowbar', { role: 'img', 'aria-label': STAGES.map(function (k) { return Dash.label('stage', k) + ' ' + (counts[k] || 0); }).join('，') },
      STAGES.map(function (k) {
        var n = counts[k] || 0;
        return h((link ? 'a' : 'div') + '.flowbar-seg' + (n ? '' : '.is-zero') + (k === 'paused' || k === 'queued' ? '.is-light' : ''),
          { href: link ? link(k) : null, style: '--n:' + Math.max(n, 1) + '; --tone: var(' + Dash.colorVar('stage', k) + ')' },
          h('span.flowbar-name', Dash.label('stage', k)), h('span.flowbar-n', Dash.num(n)));
      }));
  };

  /* 编号对象一行：编号 + 一句话标题 + 右侧签条（谁或状态） */
  Dash.item = function (id, title, side) {
    return h('div.item', h('span.id', id), h('span.item-title', title), side ? h('span.item-side', side) : null);
  };

  /* 分钟数写成"45 分钟""2 小时 5 分钟" */
  Dash.minutes = function (m) {
    m = Math.round(m || 0);
    return m < 60 ? m + ' 分钟' : Math.floor(m / 60) + ' 小时' + (m % 60 ? ' ' + (m % 60) + ' 分钟' : '');
  };
})();
