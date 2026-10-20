/* 观察台图组件·统计类：热力矩阵 heatmap、矩形树图 treemap、条形图 bars。共用底座在 charts.js（Dash.internal.chartKit）。
   heatmap({ x:[名], y:[名], cells:[{x,y,value?,tone?,title?}], mode:'count'|'tone' }, opts)
       热力矩阵：count 按数值深浅（单色渐变），tone 每格按语义色（检查矩阵、历史条带）。x、y 是下标。
   treemap({ nodes:[{name,value,tone?,children?,title?}] }, opts)
       矩形树图：面积为 value，顶层按顺序取 layer 组的蓝（或 tone）；opts.unit 数值单位。点格子下钻。
   bars({ categories:[名], series:[{name,tone,values:[数]}], stack?, horizontal? }, opts)
       条形图（可堆叠）：近 24 小时各型号的个数、各文档注入次数一类。 */
(function () {
  'use strict';

  var Dash = window.Dash;
  var h = Dash.h;
  var C = Dash.charts;
  var K = Dash.internal.chartKit;
  var css = K.css;
  var tone = K.tone;
  var mix = K.mix;
  var tip = K.tip;
  var host = K.host;
  var wrap = K.wrap;
  var emptyNode = K.emptyNode;
  var clickTo = K.clickTo;

  /* ================= 热力矩阵 ================= */

  C.heatmap = function (spec, opts) {
    opts = Object.assign({ kind: 'heatmap' }, opts);
    if (!spec || !spec.x || !spec.x.length || !spec.y || !spec.y.length) return emptyNode(opts);
    var mode = spec.mode || 'count';
    var cellH = opts.cellHeight || 24, labelW = opts.labelWidth || 120;
    var bottom = opts.xLabels === false ? 8 : (spec.x.length > 10 ? 64 : 28);
    opts.height = opts.height || spec.y.length * cellH + bottom + 10;
    var cells = spec.cells || [];
    var max = cells.reduce(function (m, c) { return Math.max(m, c.value || 0); }, 0) || 1;

    function build(size) {
      var cw = (size.width - labelW - 10) / spec.x.length;
      var s0 = Dash.color('scale', 0), s1 = Dash.color('scale', 1);
      return {
        grid: { left: labelW, right: 10, top: 6, bottom: bottom },
        tooltip: { trigger: 'item', formatter: function (p) {
          var c = cells[p.dataIndex];
          return tip(spec.y[c.y] + ' × ' + spec.x[c.x], [mode === 'count' ? Dash.num(c.value) + (opts.unit ? ' ' + opts.unit : '') : (c.tone ? Dash.label(c.tone[0], c.tone[1]) : ''), c.title]);
        } },
        xAxis: { type: 'category', data: spec.x, splitArea: { show: false }, axisTick: { show: false }, axisLine: { show: false },
          axisLabel: { show: opts.xLabels !== false, interval: opts.xInterval == null ? 0 : opts.xInterval, rotate: spec.x.length > 10 ? 45 : 0, color: css('--text-2'), fontSize: 11 } },
        yAxis: { type: 'category', data: spec.y, inverse: true, axisTick: { show: false }, axisLine: { show: false },
          axisLabel: { color: css('--text-2'), fontSize: 11, width: labelW - 10, overflow: 'truncate' } },
        series: [{
          type: 'heatmap',
          data: cells.map(function (c) {
            var color = mode === 'tone' ? tone(c.tone) : (c.value ? mix(s1, s0, .15 + .85 * c.value / max) : css('--surface-2'));
            return { value: [c.x, c.y, c.value || 0], itemStyle: { color: color, borderColor: css('--surface'), borderWidth: mode === 'tone' && cw < 10 ? 1 : 2, borderRadius: 3 } };
          }),
          label: { show: mode === 'count' && cw >= 26, color: css('--text'), fontSize: 11,
            formatter: function (p) { return p.value[2] ? Dash.num(p.value[2]) : ''; } },
          emphasis: { itemStyle: { borderColor: css('--text'), borderWidth: 2 } }
        }]
      };
    }
    return wrap(host(build, Object.assign({ relayout: true }, opts), function (inst) {
      clickTo(inst, opts, function (p) { return cells[p.dataIndex]; });
    }), opts);
  };

  /* ================= 矩形树图 ================= */

  C.treemap = function (spec, opts) {
    opts = Object.assign({ kind: 'treemap' }, opts);
    if (!spec || !spec.nodes || !spec.nodes.length) return emptyNode(opts);
    opts.height = opts.height || 420;
    function conv(n, color) {
      var kids = (n.children || []).filter(function (k) { return k.value > 0; });
      return { name: n.name, value: n.value, raw: n, itemStyle: color ? { color: color } : undefined,
        children: kids.length ? kids.map(function (k) { return conv(k, null); }) : undefined };
    }
    function build() {
      return {
        tooltip: { formatter: function (p) {
          var path = (p.treePathInfo || []).slice(1).map(function (x) { return x.name; }).join(' / ');
          return tip(path || p.name, [Dash.num(p.value) + (opts.unit ? ' ' + opts.unit : ''), p.data.raw && p.data.raw.title]);
        } },
        series: [{
          type: 'treemap', roam: false, nodeClick: 'zoomToNode', leafDepth: opts.depth || 2,
          top: 8, left: 2, right: 2, bottom: 28,
          breadcrumb: { show: true, bottom: 2, height: 20, itemStyle: { color: css('--surface-2'), borderColor: css('--line'), textStyle: { color: css('--text-2') } } },
          upperLabel: { show: true, height: 22, color: css('--on-strong'), fontWeight: 600, fontSize: 12 },
          label: { show: true, fontSize: 11, color: css('--on-strong'), overflow: 'truncate',
            formatter: function (p) { return p.name + '\n' + Dash.num(p.value); } },
          itemStyle: { borderColor: css('--surface'), borderWidth: 1, gapWidth: 1 },
          levels: [
            { itemStyle: { borderWidth: 0, gapWidth: 3 } },
            { itemStyle: { gapWidth: 1, borderColorSaturation: .5 } },
            { itemStyle: { gapWidth: 1 } }
          ],
          data: spec.nodes.map(function (n, i) { return conv(n, n.tone ? tone(n.tone) : Dash.color('layer', i)); })
        }]
      };
    }
    return wrap(host(build, opts, function (inst) {
      clickTo(inst, opts, function (p) { return p.data && p.data.raw; });
    }), opts);
  };

  /* ================= 条形图 ================= */

  C.bars = function (spec, opts) {
    opts = Object.assign({ kind: 'bars' }, opts);
    if (!spec || !spec.categories || !spec.categories.length || !spec.series || !spec.series.length) return emptyNode(opts);
    var horiz = !!spec.horizontal;
    opts.height = opts.height || (horiz ? spec.categories.length * 26 + 40 : 260);
    function build() {
      var cat = { type: 'category', data: spec.categories, axisTick: { show: false }, axisLine: { lineStyle: { color: css('--line') } },
        axisLabel: { color: css('--text-2'), fontSize: 11, interval: 0, rotate: !horiz && spec.categories.length > 12 ? 40 : 0 } };
      var val = { type: 'value', minInterval: 1, splitLine: { lineStyle: { color: css('--line-2') } }, axisLabel: { color: css('--text-3'), fontSize: 11 } };
      if (horiz) cat.inverse = true;
      return {
        grid: { left: horiz ? (opts.labelWidth || 140) : 40, right: 16, top: 12, bottom: horiz ? 24 : (spec.categories.length > 12 ? 60 : 30) },
        tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' }, formatter: function (ps) {
          return tip(ps[0].name, ps.filter(function (p) { return p.value; }).map(function (p) { return p.seriesName + '：' + Dash.num(p.value) + (opts.unit ? ' ' + opts.unit : ''); }));
        } },
        xAxis: horiz ? val : cat,
        yAxis: horiz ? cat : val,
        series: spec.series.map(function (s) {
          return { name: s.name, type: 'bar', stack: spec.stack ? 'all' : undefined, data: s.values, barMaxWidth: 26,
            itemStyle: { color: tone(s.tone), borderRadius: spec.stack ? 0 : 3 } };
        })
      };
    }
    return wrap(host(build, opts, function (inst) {
      clickTo(inst, opts, function (p) { return { category: p.name, series: p.seriesName, value: p.value }; });
    }), opts);
  };
})();
