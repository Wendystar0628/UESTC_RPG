/* 观察台共用图组件的底座（Apache ECharts，vendor/echarts.min.js）：主题色、尺寸自适应、空数据与加载中、释放，各类图共用。
   图本身按职责分在三个文件：charts-flow.js（swimlane 泳道矩阵、dag 依赖图、timeline 时间线）、charts-layered.js（layered 分层关系图）、
   charts-stats.js（heatmap 热力矩阵、treemap 矩形树图、bars 条形图）；它们都挂在这里建的 Dash.charts 上，经 Dash.internal.chartKit 取下面的共用函数。
   每个组件返回一个节点，直接 append；节点进到可见的页面里才初始化，容器变宽变窄自动重排；面板重绘时 core.js 调 release 释放旧图。
   颜色一律经 palette.js：图里的项可带 tone: [组, 键]（如 ['queue', '进行中']、['tag', '需定']），不写颜色字面量。
   通用 opts：height 高度（像素，省略按数据算）、onClick(项) 点了哪一项、legend: [[组, 键们?], ...] 图上方的图例、empty 空数据时的话、key 同一张图的稳定名（只第一次画时有动画）。
   Dash.charts.loading(height) 加载中占位；Dash.charts.release(root) 释放 root 里的图。 */
(function () {
  'use strict';

  var Dash = window.Dash;
  var h = Dash.h;
  var live = [];          // { el, inst, ro }
  var drawnKeys = {};     // 画过的图：之后重画不再放动画

  function css(v) { return Dash.cssVar(v); }
  function tone(t) { return t ? Dash.color(t[0], t[1]) : css('--muted'); }
  function esc(s) { return Dash.esc(s); }
  function dark() { return matchMedia('(prefers-color-scheme: dark)').matches; }

  /* 十六进制色与底色按比例混合，给浅底、描边用 */
  function rgb(hex) {
    var s = String(hex).replace('#', '');
    if (s.length === 3) s = s.split('').map(function (c) { return c + c; }).join('');
    var n = parseInt(s, 16);
    return [(n >> 16) & 255, (n >> 8) & 255, n & 255];
  }
  function mix(fg, bg, t) {
    var a = rgb(fg), b = rgb(bg);
    return '#' + [0, 1, 2].map(function (i) { return ('0' + Math.round(a[i] * t + b[i] * (1 - t)).toString(16)).slice(-2); }).join('');
  }
  function tint(c, t) { return mix(c, css('--surface'), t); }

  function base() {
    var font = css('--font');
    return {
      animation: true,
      animationDuration: 400,
      textStyle: { fontFamily: font, color: css('--text-2'), fontSize: 12 },
      tooltip: {
        confine: true, borderWidth: 1, borderColor: css('--line'), backgroundColor: css('--surface'),
        padding: [8, 10], textStyle: { color: css('--text'), fontSize: 12, fontFamily: font },
        extraCssText: 'box-shadow: 0 8px 24px -10px rgba(20,38,64,.35); border-radius: 8px; max-width: 360px; white-space: normal;'
      }
    };
  }

  function tip(title, lines) {
    return '<div class="ct-title">' + esc(title) + '</div>' + (lines || []).filter(Boolean).map(function (l) { return '<div class="ct-line">' + esc(l) + '</div>'; }).join('');
  }

  /* ---------- 宿主：等进到可见页面、有了宽度才初始化 ---------- */

  function host(build, opts, bind) {
    opts = opts || {};
    var el = h('div.chart', { style: 'height:' + Math.round(opts.height || 300) + 'px', role: 'img', 'aria-label': opts.label || '' });
    var rec = { el: el, inst: null, ro: null };
    function draw(first) {
      var size = { width: el.clientWidth, height: el.clientHeight };
      var opt = Object.assign(base(), build(size));
      var key = opts.key || (el.closest('[data-panel]') ? el.closest('[data-panel]').dataset.panel + ':' + (opts.kind || '') : '');
      if (!first || (key && drawnKeys[key])) opt.animation = false;
      if (key) drawnKeys[key] = true;
      rec.inst.setOption(opt, true);
    }
    rec.ro = new ResizeObserver(function () {
      if (!el.clientWidth) return;
      if (!rec.inst) {
        if (!window.echarts) { el.replaceChildren(Dash.empty('图表库没有加载', 'vendor/echarts.min.js 读不到', true)); return; }
        rec.inst = window.echarts.init(el, null, { renderer: 'canvas' });
        draw(true);
        if (bind) bind(rec.inst, el, draw);
      } else {
        rec.inst.resize();
        if (opts.relayout) draw(false);
      }
    });
    rec.ro.observe(el);
    live.push(rec);
    return el;
  }

  function wrap(chart, opts) {
    var legends = (opts && opts.legend || []).map(function (l) { return Dash.legend(l[0], l[1]); });
    if (!legends.length && !(opts && opts.top)) return chart;
    return h('div.chart-wrap', opts.top || null, legends.length ? h('div.chart-legends', legends) : null, chart);
  }

  function emptyNode(opts, defaultText) {
    return Dash.empty(opts && opts.empty || defaultText || '没有数据', '', true);
  }

  var C = Dash.charts = {};

  C.release = function (root) {
    live = live.filter(function (r) {
      if (!root || root.contains(r.el)) {
        r.ro.disconnect();
        if (r.inst) r.inst.dispose();
        return false;
      }
      return true;
    });
  };

  C.loading = function (height) { return h('div.skeleton', { style: 'height:' + (height || 160) + 'px' }); };

  /* 主题切换：所有面板重画（图随之按新色重建） */
  matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function () { Dash.redrawAll(); });

  function clickTo(inst, opts, pick) {
    if (!opts.onClick) return;
    inst.on('click', function (p) { var item = pick(p); if (item) opts.onClick(item, p); });
  }

  Dash.internal.chartKit = { css: css, tone: tone, dark: dark, rgb: rgb, mix: mix, tint: tint, tip: tip, host: host, wrap: wrap, emptyNode: emptyNode, clickTo: clickTo };
})();
