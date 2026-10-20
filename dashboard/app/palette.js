/* 观察台色表：颜色按语义只定一套，值写在 style.css 的 CSS 变量里（浅色与深色各一份），这里只登记"哪个语义用哪个变量、叫什么"。
   签条、色块、所有图（charts*.js）都从这里取色，页面不自己挑颜色。
   Dash.colorVar(组, 键) → CSS 变量名      Dash.color(组, 键) → 当前主题下的色值（给 ECharts）
   Dash.label(组, 键)    → 中文名          Dash.mark(组, 键, 字?) → 语义签条（节点）
   Dash.modelKey(型号)   → 型号的色表键：claude-opus-5-5 → opus，gpt-6.1-sol 原样 */
(function () {
  'use strict';

  var Dash = window.Dash;
  var h = Dash.h;

  var P = Dash.palette = {
    /* 任务流水线四段（契约 progress.rows[].stage） */
    stage: {
      queued:    ['--st-queued', '排着'],
      doing:     ['--st-doing', '在做'],
      finishing: ['--st-finishing', '收尾'],
      paused:    ['--st-paused', '暂停']
    },
    /* 负责方 */
    side: {
      claude: ['--claude', 'Claude'],
      codex:  ['--codex', 'Codex'],
      author: ['--author', '作者'],
      muted:  ['--muted', '未定']
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
    /* 健康等级 */
    level: {
      ok:   ['--ok', '正常'],
      warn: ['--warn', '要看'],
      bad:  ['--risk', '有问题']
    },
    /* 等你过目的种类（契约 attention.items[].kind） */
    kind: {
      audit:  ['--author', '观感'],
      shots:  ['--accent', '截图'],
      poster: ['--auto', '海报'],
      review: ['--muted', '评审']
    },
    /* 分组色块：按顺序取的一组蓝（脚本分组、文档域），键是下标 */
    seq: ['--seq-1', '--seq-2', '--seq-3', '--seq-4', '--seq-5', '--seq-6']
  };

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
    if (Array.isArray(g)) return [g[Math.abs(Number(key) || 0) % g.length], String(key)];
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
    var c = /^claude-([a-z]+)/.exec(s) || /^(opus|sonnet|haiku)/.exec(s);
    if (c && P.model[c[1]]) return c[1];
    if (P.model[s]) return s;
    var g = /(sol|luna|astra)$/.exec(s);
    if (g) return Object.keys(P.model).filter(function (k) { return k.slice(-g[1].length) === g[1]; })[0];
    return 'other';
  };

  /* 语义签条：浅底深字，颜色取自色表；text 省略时用色表里的中文名 */
  Dash.mark = function (group, key, text) {
    var v = Dash.colorVar(group, key);
    return h('span.chip.mark', { style: '--tone: var(' + v + '); --tone-bg: color-mix(in srgb, var(' + v + ') 14%, transparent)' },
      text == null ? Dash.label(group, key) : text);
  };
})();
