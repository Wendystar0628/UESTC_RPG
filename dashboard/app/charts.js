/* 观察台的统计图（Apache ECharts，vendor/echarts.min.js）：只画高层统计，不画原始数据与牵连关系。
   Dash.charts.spark(values, opts)   近 N 天的小柱（最右是今天，主色），卡片里的趋势；opts.unit 单位
   Dash.charts.ring(parts, opts)     环形占比：parts [{ label, value, tone: [组, 键] }]；opts.center 环心的字
   Dash.charts.bars(items, opts)     横向条形：items [{ label, value, text?, tone: [组, 键] }]；text 是条尾的字（省略写数值）
   Dash.charts.treemap(items, opts)  面积图：items [{ label, value, note?, tone: [组, 键] }]；note 是悬停时的一句话；opts.unit 单位
   通用 opts：height（像素）、label（无障碍名）。节点进到可见页面、有了宽度才初始化，容器变宽变窄自动重排；
   面板重绘时 core.js 调 Dash.charts.release 释放旧图；主题切换全部重画。颜色一律经 palette.js。 */
(function () {
  'use strict';

  var Dash = window.Dash;
  var h = Dash.h;
  var live = [];          // { el, inst, ro }
  var drawnKeys = {};     // 画过的图：之后重画不再放动画

  function css(v) { return Dash.cssVar(v); }
  function tone(t) { return t ? Dash.color(t[0], t[1]) : css('--accent'); }
  function esc(s) { return Dash.esc(s); }

  function base() {
    var font = css('--font');
    return {
      animationDuration: 400,
      textStyle: { fontFamily: font, color: css('--text-2'), fontSize: 12 },
      tooltip: {
        confine: true, borderWidth: 1, borderColor: css('--line'), backgroundColor: css('--surface'),
        padding: [6, 10], textStyle: { color: css('--text'), fontSize: 12, fontFamily: font },
        extraCssText: 'box-shadow: 0 8px 24px -10px rgba(20,38,64,.35); border-radius: 8px;'
      }
    };
  }

  function tip(title, line) {
    return '<b>' + esc(title) + '</b>' + (line ? '<br>' + esc(line) : '');
  }

  /* 宿主：等进到可见页面、有了宽度才初始化 */
  function host(kind, build, opts) {
    var el = h('div.chart.chart-' + kind, { style: 'height:' + Math.round(opts.height) + 'px', role: 'img', 'aria-label': opts.label || '' });
    var rec = { el: el, inst: null, ro: null };
    function draw() {
      var opt = Object.assign(base(), build());
      var panel = el.closest('[data-panel]');
      var key = panel ? panel.dataset.panel + ':' + kind + ':' + (opts.label || '') : '';
      opt.animation = !(key && drawnKeys[key]);
      if (key) drawnKeys[key] = true;
      rec.inst.setOption(opt, true);
    }
    rec.ro = new ResizeObserver(function () {
      if (!el.clientWidth) return;
      if (!rec.inst) {
        if (!window.echarts) { el.replaceChildren(Dash.empty('图没有加载', '图表库读不到', true)); return; }
        rec.inst = window.echarts.init(el, null, { renderer: 'canvas' });
        draw();
      } else {
        rec.inst.resize();
      }
    });
    rec.ro.observe(el);
    live.push(rec);
    return el;
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

  /* 主题切换：所有面板重画（图随之按新色重建） */
  matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function () { Dash.redrawAll(); });

  /* 第 i 天（0 是最早）的日期文字，最右一天是今天 */
  function dayText(i, n) {
    var d = new Date(Date.now() - (n - 1 - i) * 86400000);
    return i === n - 1 ? '今天' : (d.getMonth() + 1) + ' 月 ' + d.getDate() + ' 日';
  }

  C.spark = function (values, opts) {
    opts = Object.assign({ height: 44 }, opts);
    var n = values.length;
    return host('spark', function () {
      return {
        grid: { left: 0, right: 0, top: 4, bottom: 0 },
        tooltip: { trigger: 'item', formatter: function (p) { return tip(dayText(p.dataIndex, n), Dash.num(p.value) + (opts.unit ? ' ' + opts.unit : '')); } },
        xAxis: { type: 'category', show: false, data: values.map(function (v, i) { return i; }) },
        yAxis: { type: 'value', show: false, min: 0, max: function (v) { return Math.max(1, v.max); } },
        series: [{
          type: 'bar', barCategoryGap: '28%',
          data: values.map(function (v, i) {
            return { value: v, itemStyle: { color: i === n - 1 ? css('--accent') : css('--spark'), borderRadius: [3, 3, 0, 0] } };
          })
        }]
      };
    }, opts);
  };

  C.ring = function (parts, opts) {
    opts = Object.assign({ height: 76 }, opts);
    var total = parts.reduce(function (s, p) { return s + (p.value || 0); }, 0);
    return host('ring', function () {
      return {
        tooltip: { trigger: 'item', formatter: function (p) {
          return tip(p.name, Dash.num(p.value) + (opts.unit ? ' ' + opts.unit : '') + (total ? '，占 ' + Math.round(p.value * 100 / total) + '%' : ''));
        } },
        title: opts.center == null ? undefined : { text: String(opts.center), left: 'center', top: 'middle',
          textStyle: { fontSize: 13, fontWeight: 650, color: css('--text'), fontFamily: css('--font') } },
        series: [{
          type: 'pie', radius: ['64%', '92%'], center: ['50%', '50%'], avoidLabelOverlap: false,
          label: { show: false }, labelLine: { show: false }, emphasis: { scale: false },
          itemStyle: { borderColor: css('--surface'), borderWidth: 2 },
          data: total ? parts.map(function (p) { return { name: p.label, value: p.value, itemStyle: { color: tone(p.tone) } }; })
            : [{ name: '没有', value: 1, itemStyle: { color: css('--line') } }]
        }]
      };
    }, opts);
  };

  C.bars = function (items, opts) {
    opts = Object.assign({ height: items.length * 38 + 8 }, opts);
    var max = items.reduce(function (m, x) { return Math.max(m, x.value || 0); }, 0) || 1;
    return host('bars', function () {
      return {
        grid: { left: opts.labelWidth || 108, right: opts.textWidth || 132, top: 4, bottom: 4 },
        tooltip: { show: false },
        xAxis: { type: 'value', show: false, max: max },
        yAxis: { type: 'category', inverse: true, data: items.map(function (x) { return x.label; }),
          axisTick: { show: false }, axisLine: { show: false }, axisLabel: { color: css('--text'), fontSize: 13, fontWeight: 600 } },
        series: [{
          type: 'bar', barWidth: 18, showBackground: true,
          backgroundStyle: { color: css('--surface-2'), borderRadius: 5 },
          label: { show: true, position: 'right', distance: 10, color: css('--text-2'), fontSize: 13,
            formatter: function (p) { var x = items[p.dataIndex]; return x.text == null ? Dash.num(x.value) : x.text; } },
          data: items.map(function (x) { return { value: x.value, itemStyle: { color: tone(x.tone), borderRadius: 5 } }; })
        }]
      };
    }, opts);
  };

  C.treemap = function (items, opts) {
    opts = Object.assign({ height: 220 }, opts);
    return host('treemap', function () {
      return {
        tooltip: { formatter: function (p) { return tip(p.name + ' · ' + Dash.num(p.value) + (opts.unit ? ' ' + opts.unit : ''), p.data.note); } },
        series: [{
          type: 'treemap', roam: false, nodeClick: false, breadcrumb: { show: false },
          top: 0, left: 0, right: 0, bottom: 0, squareRatio: 1.3,
          label: { show: true, color: css('--on-strong'), fontSize: 13, fontWeight: 600, lineHeight: 18,
            formatter: function (p) { return p.name + '\n' + Dash.num(p.value) + (opts.unit ? ' ' + opts.unit : ''); } },
          itemStyle: { borderColor: css('--surface'), borderWidth: 3, gapWidth: 3, borderRadius: 6 },
          data: items.map(function (x) { return { name: x.label, value: x.value, note: x.note || '', itemStyle: { color: tone(x.tone) } }; })
        }]
      };
    }, opts);
  };
})();
