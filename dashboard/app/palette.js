/* 观察台色表：颜色按语义只定一套，值写在 style.css 的 CSS 变量里（浅色与深色各一份），这里只登记"哪个语义用哪个变量、叫什么"。
   签条、图例、所有图（charts.js）都从这里取色，页面不自己挑颜色。
   Dash.color(组, 键)     → 当前主题下的色值（给 ECharts）
   Dash.label(组, 键)     → 中文名
   Dash.mark(组, 键, 字?) → 语义签条（节点）
   Dash.legend(组, 键们?) → 一行图例（节点）
   Dash.modelKey(型号)    → 型号的色表键：claude-opus-5-5 → opus，gpt-6.1-sol 原样 */
(function () {
  'use strict';

  var Dash = window.Dash;
  var h = Dash.h;

  var P = Dash.palette = {
    /* 流水阶段与队列行的状态（契约 pipeline.stages.*.status） */
    status: {
      done:  ['--st-done', '完成'],
      doing: ['--st-doing', '进行中'],
      fail:  ['--st-fail', '失败'],
      todo:  ['--st-todo', '未到'],
      na:    ['--st-na', '不适用']
    },
    /* 施工队列的状态列，映射到上面的状态色（不另起颜色） */
    queue: {
      '进行中': ['--st-doing', '进行中'],
      '待编译': ['--st-wait', '待编译'],
      '可派':   ['--st-ready', '可派'],
      '等前置': ['--st-todo', '等前置'],
      '暂停':   ['--st-na', '暂停'],
      '已推':   ['--st-done', '已推']
    },
    /* 负责方与其他角色 */
    side: {
      claude: ['--claude', 'Claude'],
      codex:  ['--codex', 'Codex'],
      author: ['--author', '作者'],
      auto:   ['--auto', '自动']
    },
    /* 来信标签 */
    tag: {
      '需定': ['--tag-decide', '需定'],
      '待审': ['--tag-review', '待审'],
      '知会': ['--tag-inform', '知会'],
      '插队': ['--tag-cut', '插队'],
      '无':   ['--tag-none', '无标签']
    },
    /* 型号：Claude 一族紫、Codex 一族青，按家族深浅区分 */
    model: {
      opus:          ['--m-opus', 'Opus'],
      sonnet:        ['--m-sonnet', 'Sonnet'],
      haiku:         ['--m-haiku', 'Haiku'],
      'gpt-6.1-sol': ['--m-sol', 'gpt-6.1-sol'],
      'gpt-6-luna':  ['--m-luna', 'gpt-6-luna'],
      'gpt-6-astra': ['--m-astra', 'gpt-6-astra'],
      other:         ['--muted', '其他']
    },
    /* 健康检查等级 */
    level: {
      ok:   ['--ok', '正常'],
      warn: ['--warn', '要看'],
      bad:  ['--risk', '要你动手']
    },
    /* 体系关系图的节点种类：自动机制暖色、文档与代码蓝、数据灰 */
    node: {
      event:  ['--k-event', '事件与时机'],
      hook:   ['--k-hook', '钩子'],
      doc:    ['--k-doc', '文档'],
      script: ['--k-script', '脚本'],
      data:   ['--k-data', '数据与产物'],
      check:  ['--k-check', '巡逻检查'],
      gate:   ['--k-gate', '门禁'],
      path:   ['--k-path', '代码路径']
    },
    /* 体系关系图的边种类 */
    edge: {
      inject:    ['--k-hook', '注入'],
      route:     ['--k-path', '路由'],
      govern:    ['--k-doc', '管辖'],
      readwrite: ['--k-script', '读写'],
      generate:  ['--k-data', '生成'],
      call:      ['--k-event', '调用'],
      check:     ['--k-check', '检查'],
      gate:      ['--k-gate', '门禁']
    },
    /* 代码分层（结构页矩形树图）：从契约到工具由深到浅的一组蓝；按层的顺序取，不按名字 */
    layer: ['--layer-1', '--layer-2', '--layer-3', '--layer-4', '--layer-5', '--layer-6'],
    /* 计数类的单色渐变两端（热力矩阵） */
    scale: ['--scale-0', '--scale-1']
  };

  /* 流水阶段：契约 pipeline.rows[].stages 的键与中文名，按先后排 */
  Dash.stages = [
    { id: 'dispatch', label: '派工' }, { id: 'build', label: '施工' }, { id: 'pending', label: '待编译' }, { id: 'gate', label: '快检' },
    { id: 'pushed', label: '已推' }, { id: 'audit', label: '收口审计' }, { id: 'shots', label: '截图评审' }, { id: 'prebake', label: '预烘' }
  ];

  var cache = {};
  var cacheTheme = null;
  function themeKey() { return matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'; }

  /* CSS 变量 → 色值；主题切换后重新取 */
  Dash.cssVar = function (name) {
    var t = themeKey();
    if (t !== cacheTheme) { cache = {}; cacheTheme = t; }
    if (!(name in cache)) cache[name] = getComputedStyle(document.documentElement).getPropertyValue(name).trim() || '#888';
    return cache[name];
  };

  function entry(group, key) {
    var g = P[group];
    if (!g) throw new Error('色表里没有这一组：' + group);
    if (Array.isArray(g) && typeof g[0] === 'string') return [g[Math.abs(Number(key) || 0) % g.length], String(key)];
    return g[key] || null;
  }

  Dash.colorVar = function (group, key) {
    var e = entry(group, key);
    return e ? e[0] : '--muted';
  };
  Dash.color = function (group, key) { return Dash.cssVar(Dash.colorVar(group, key)); };
  Dash.label = function (group, key) {
    var e = entry(group, key);
    return e ? e[1] : String(key == null ? '—' : key);
  };

  Dash.modelKey = function (m) {
    var s = String(m || '').toLowerCase();
    var c = /^claude-([a-z]+)/.exec(s);
    if (c && P.model[c[1]]) return c[1];
    if (P.model[s]) return s;
    var g = /(sol|luna|astra)$/.exec(s);
    if (g) return Object.keys(P.model).filter(function (k) { return k.slice(-g[1].length) === g[1]; })[0];
    return 'other';
  };

  /* 语义签条：浅底深字，颜色取自色表；text 省略时用色表里的中文名 */
  Dash.mark = function (group, key, text) {
    var v = Dash.colorVar(group, key);
    var el = h('span.chip.mark', { style: '--tone: var(' + v + '); --tone-bg: color-mix(in srgb, var(' + v + ') 15%, transparent)' },
      text == null ? Dash.label(group, key) : text);
    el.toString = function () { return el.outerHTML; };
    return el;
  };

  /* 一行图例：色块 + 中文名；keys 省略时列出整组 */
  Dash.legend = function (group, keys) {
    var g = P[group];
    keys = keys || Object.keys(g);
    return h('div.legend', keys.map(function (k) {
      return h('span.legend-item', h('i.legend-swatch', { style: 'background: var(' + Dash.colorVar(group, k) + ')' }), Dash.label(group, k));
    }));
  };
})();
