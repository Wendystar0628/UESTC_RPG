/* 观察台的统计图：ECharts（vendor/echarts.min.js）画常规图，桑基与密度条用 SVG/HTML 自绘（ECharts 的桑基画不了 0 次的虚线）。
   每个函数返回一个节点；通用 opts：height、label（无障碍名）、caliber（悬停框末行的口径句）、unit、onClick(p)，
   p = {index 类别下标, key, label, value, series 系列的 key}。颜色一律经 palette.js 的 tone: [组, 键]。
   节点进到可见页面、有了宽度才画，容器变宽变窄自动重排；面板重绘时 core 调 release 释放旧图；主题切换全部重画。接口见 README §3。 */
(function () {
  'use strict';

  var Dash = window.Dash;
  if (!Dash.internal) return;
  var h = Dash.h;
  var live = [];          // { el, inst, ro }
  var drawnKeys = {};     // 画过的图：之后重画不再放动画

  function css(v) { return Dash.cssVar(v); }
  function tone(t) { return t ? Dash.color(t[0], t[1]) : css('--accent'); }
  function toneVar(t) { return 'var(' + (t ? Dash.colorVar(t[0], t[1]) : '--accent') + ')'; }
  function esc(s) { return Dash.esc(s); }
  function unitOf(opts) { return opts.unit ? ' ' + opts.unit : ''; }
  /* 画布不认 color-mix：#rrggbb → rgba */
  function alpha(c, a) {
    var m = /^#([0-9a-f]{6})$/i.exec(c);
    if (!m) return c;
    var n = parseInt(m[1], 16);
    return 'rgba(' + (n >> 16) + ',' + ((n >> 8) & 255) + ',' + (n & 255) + ',' + a + ')';
  }

  /* 悬停框：首行粗体，其余各一行，末行是口径句 */
  function tip(title, lines, caliber) {
    return '<b>' + esc(title) + '</b>' + [].concat(lines || []).filter(Boolean).map(function (l) { return '<br>' + esc(l); }).join('')
      + (caliber ? '<br><span style="color:' + css('--text-3') + '">' + esc(caliber) + '</span>' : '');
  }

  function base() {
    var font = css('--font');
    return {
      animationDuration: 400,
      textStyle: { fontFamily: font, color: css('--text-2'), fontSize: 12 },
      tooltip: {
        confine: true, borderWidth: 1, borderColor: css('--line'), backgroundColor: css('--surface'),
        padding: [6, 10], textStyle: { color: css('--text'), fontSize: 12, fontFamily: font },
        extraCssText: 'box-shadow: var(--shadow-pop); border-radius: 8px;'
      }
    };
  }

  function axisStyle() {
    return {
      axisLine: { lineStyle: { color: css('--line') } }, axisTick: { show: false },
      axisLabel: { color: css('--text-3'), fontSize: 12 },
      splitLine: { lineStyle: { color: css('--line-2') } }
    };
  }

  /* 日期串 → "M-D" */
  function md(day) {
    var t = Dash.parseTime(day);
    if (isNaN(t)) return String(day);
    var d = new Date(t);
    return (d.getMonth() + 1) + '-' + d.getDate();
  }

  /* ECharts 宿主：等进到可见页面、有了宽度才初始化；toP 把 ECharts 的点击参数换成 p */
  function host(kind, build, opts, toP) {
    var el = h('div.chart.chart-' + kind, { style: 'height:' + Math.round(opts.height) + 'px', role: 'img', 'aria-label': opts.label || '' });
    var rec = { el: el, inst: null, ro: null };
    rec.ro = new ResizeObserver(function () {
      if (!el.clientWidth) return;
      if (rec.inst) { rec.inst.resize(); return; }
      rec.inst = window.echarts.init(el, null, { renderer: 'canvas' });
      var opt = Object.assign(base(), build());
      var panel = el.closest('[data-panel]');
      var key = panel ? panel.dataset.panel + ':' + kind + ':' + (opts.label || '') : '';
      opt.animation = !(key && drawnKeys[key]);
      if (key) drawnKeys[key] = true;
      rec.inst.setOption(opt, true);
      if (opts.onClick && toP) rec.inst.on('click', function (e) { var p = toP(e); if (p) opts.onClick(p); });
      if (opts.onClick) el.classList.add('is-click');
    });
    rec.ro.observe(el);
    live.push(rec);
    return el;
  }

  /* 自绘宿主：有了宽度就调 draw(宽)，变宽变窄重画 */
  function own(kind, draw, opts) {
    var el = h('div.chart.chart-' + kind, { role: 'img', 'aria-label': opts.label || '' });
    var rec = { el: el, inst: null, ro: null, w: 0 };
    rec.ro = new ResizeObserver(function () {
      var w = el.clientWidth;
      if (!w || w === rec.w) return;
      rec.w = w;
      draw(el, w);
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

  matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function () { Dash.redrawAll(); });

  function dayText(i, n) {
    var d = new Date(Date.now() - (n - 1 - i) * 864e5);
    return i === n - 1 ? '今天' : (d.getMonth() + 1) + ' 月 ' + d.getDate() + ' 日';
  }

  /* ---------- 小柱：近 N 天，最右是今天 ---------- */
  C.spark = function (values, opts) {
    opts = Object.assign({ height: 44 }, opts);
    var n = values.length;
    return host('spark', function () {
      return {
        grid: { left: 0, right: 0, top: 4, bottom: 0 },
        tooltip: { trigger: 'item', formatter: function (p) { return tip(dayText(p.dataIndex, n), Dash.num(p.value) + unitOf(opts), opts.caliber); } },
        xAxis: { type: 'category', show: false, data: values.map(function (v, i) { return i; }) },
        yAxis: { type: 'value', show: false, min: 0, max: function (v) { return Math.max(1, v.max); } },
        series: [{
          type: 'bar', barCategoryGap: '28%',
          data: values.map(function (v, i) {
            return { value: v, itemStyle: { color: i === n - 1 ? css('--accent') : css('--spark'), borderRadius: [3, 3, 0, 0] } };
          })
        }]
      };
    }, opts, function (e) { return { index: e.dataIndex, key: e.dataIndex, label: dayText(e.dataIndex, n), value: e.value, series: null }; });
  };

  /* ---------- 环：parts [{label, value, tone, key?}]；opts.center 环心的字 ---------- */
  C.ring = function (parts, opts) {
    opts = Object.assign({ height: 76 }, opts);
    var total = parts.reduce(function (s, p) { return s + (p.value || 0); }, 0);
    return host('ring', function () {
      return {
        tooltip: { trigger: 'item', formatter: function (p) {
          if (!total) return tip('没有', null, opts.caliber);
          return tip(p.name, Dash.num(p.value) + unitOf(opts) + '，占 ' + Math.round(p.value * 100 / total) + '%', opts.caliber);
        } },
        title: opts.center == null ? undefined : { text: String(opts.center), left: 'center', top: 'middle',
          textStyle: { fontSize: 14, fontWeight: 650, color: css('--text'), fontFamily: css('--font') } },
        series: [{
          type: 'pie', radius: ['64%', '92%'], center: ['50%', '50%'], avoidLabelOverlap: false,
          label: { show: false }, labelLine: { show: false }, emphasis: { scale: false },
          itemStyle: { borderColor: css('--surface'), borderWidth: 2 },
          data: total ? parts.map(function (p) { return { name: p.label, value: p.value, itemStyle: { color: tone(p.tone) } }; })
            : [{ name: '没有', value: 1, itemStyle: { color: css('--line') } }]
        }]
      };
    }, opts, function (e) {
      if (!total) return null;
      var x = parts[e.dataIndex];
      return { index: e.dataIndex, key: x.key == null ? x.label : x.key, label: x.label, value: x.value, series: null };
    });
  };

  /* ---------- 横向条形：items [{label, value, text?, note?, tone, key?}]；text 是条尾的字；opts.max 定满格 ---------- */
  C.bars = function (items, opts) {
    opts = Object.assign({ height: items.length * 38 + 8, labelWidth: 108 }, opts);
    var max = opts.max != null ? opts.max : items.reduce(function (m, x) { return Math.max(m, x.value || 0); }, 0) || 1;
    return host('bars', function () {
      return {
        grid: { left: opts.labelWidth, right: opts.textWidth || 132, top: 4, bottom: 4 },
        tooltip: { trigger: 'item', formatter: function (p) {
          var x = items[p.dataIndex];
          return tip(x.label, [(x.text == null ? Dash.num(x.value) + unitOf(opts) : x.text), x.note], opts.caliber);
        } },
        xAxis: { type: 'value', show: false, max: max },
        yAxis: { type: 'category', inverse: true, data: items.map(function (x) { return x.label; }),
          axisTick: { show: false }, axisLine: { show: false },
          axisLabel: { color: css('--text'), fontSize: 13, fontWeight: 600, width: opts.labelWidth - 10, overflow: 'break', lineHeight: 16 } },
        series: [{
          type: 'bar', barWidth: 18, showBackground: true,
          backgroundStyle: { color: css('--surface-2'), borderRadius: 5 },
          label: { show: true, position: 'right', distance: 10, color: css('--text-2'), fontSize: 13,
            formatter: function (p) { var x = items[p.dataIndex]; return x.text == null ? Dash.num(x.value) + unitOf(opts) : x.text; } },
          data: items.map(function (x) { return { value: Math.min(x.value, max), itemStyle: { color: tone(x.tone), borderRadius: 5 } }; })
        }]
      };
    }, opts, function (e) {
      var x = items[e.dataIndex];
      return { index: e.dataIndex, key: x.key == null ? x.label : x.key, label: x.label, value: x.value, series: null };
    });
  };

  /* ---------- 面积图：items [{label, value, note?, tone, key?}] ---------- */
  C.treemap = function (items, opts) {
    opts = Object.assign({ height: 220 }, opts);
    return host('treemap', function () {
      return {
        tooltip: { formatter: function (p) { return tip(p.name + ' · ' + Dash.num(p.value) + unitOf(opts), p.data.note, opts.caliber); } },
        series: [{
          type: 'treemap', roam: false, nodeClick: false, breadcrumb: { show: false },
          top: 0, left: 0, right: 0, bottom: 0, squareRatio: 1.3,
          label: { show: true, color: css('--on-strong'), fontSize: 13, fontWeight: 600, lineHeight: 18,
            formatter: function (p) { return p.name + '\n' + Dash.num(p.value) + unitOf(opts); } },
          itemStyle: { borderColor: css('--surface'), borderWidth: 3, gapWidth: 3, borderRadius: 6 },
          data: items.map(function (x, i) { return { name: x.label, value: x.value, note: x.note || '', i: i, itemStyle: { color: tone(x.tone) } }; })
        }]
      };
    }, opts, function (e) {
      var x = items[e.data.i];
      return { index: e.data.i, key: x.key == null ? x.label : x.key, label: x.label, value: x.value, series: null };
    });
  };

  /* ---------- 箱线：groups [{key, label, tone, cur: 分布, prev: 分布|null}]，分布 {n, q: [min, p25, p50, p80, max]|null}；
     值是分钟；样本少于 3 的组不画箱，只画点，并在组名下标"样本少" ---------- */
  function minLabel(v) { return v < 60 ? Math.round(v) + ' 分' : Math.round(v / 60) + ' 小时'; }
  C.box = function (groups, opts) {
    opts = Object.assign({ height: 240 }, opts);
    function few(d) { return d && d.q && d.n < 3; }
    function boxData(which) {
      return groups.map(function (g) { var d = g[which]; return d && d.q && d.n >= 3 ? d.q.slice() : '-'; });
    }
    function dots(which) {
      var out = [];
      groups.forEach(function (g, i) {
        var d = g[which];
        if (!few(d)) return;
        [d.q[0], d.q[2], d.q[4]].filter(function (v, j, a) { return a.indexOf(v) === j; }).forEach(function (v) { out.push([i, v]); });
      });
      return out;
    }
    function lines(d) {
      if (!d || !d.q) return ['没有样本'];
      return ['n = ' + Dash.num(d.n) + (d.n < 3 ? '（样本少）' : ''), '中位 ' + Dash.minutes(d.q[2]) + ' · P80 ' + Dash.minutes(d.q[3]), '最长 ' + Dash.minutes(d.q[4])];
    }
    var hasPrev = groups.some(function (g) { return g.prev; });
    return host('box', function () {
      var ax = axisStyle();
      var series = [{
        name: 'cur', type: 'boxplot', data: boxData('cur'), boxWidth: [10, 26],
        itemStyle: { borderWidth: 1.6 }
      }];
      series[0].data = series[0].data.map(function (q, i) {
        return q === '-' ? q : { value: q, itemStyle: { color: alpha(tone(groups[i].tone), 0.3), borderColor: tone(groups[i].tone) } };
      });
      series.push({ name: 'curDots', type: 'scatter', symbolSize: 9, data: dots('cur'),
        itemStyle: { color: function (p) { return tone(groups[p.value[0]].tone); } } });
      if (hasPrev) {
        series.splice(1, 0, {
          name: 'prev', type: 'boxplot', boxWidth: [10, 26],
          data: boxData('prev').map(function (q, i) {
            return q === '-' ? q : { value: q, itemStyle: { color: css('--surface-2'), borderColor: alpha(tone(groups[i].tone), 0.5), borderType: 'dashed' } };
          })
        });
        series.push({ name: 'prevDots', type: 'scatter', symbolSize: 8, data: dots('prev'),
          itemStyle: { color: css('--surface'), borderWidth: 1.5, borderColor: function (p) { return tone(groups[p.value[0]].tone); } } });
      }
      return {
        grid: { left: 52, right: 12, top: 12, bottom: 44 },
        tooltip: { trigger: 'item', formatter: function (p) {
          var g = groups[Array.isArray(p.value) && p.seriesType === 'scatter' ? p.value[0] : p.dataIndex];
          var prev = /^prev/.test(p.seriesName);
          return tip(g.label + (prev ? ' · 前 7 天' : ' · 本 7 天'), lines(prev ? g.prev : g.cur), opts.caliber);
        } },
        xAxis: Object.assign({}, ax, { type: 'category', data: groups.map(function (g) { return g.label + (few(g.cur) ? '\n样本少' : ''); }), splitLine: { show: false } }),
        yAxis: Object.assign({}, ax, { type: 'value', min: 0, axisLabel: Object.assign({}, ax.axisLabel, { formatter: minLabel }) }),
        series: series
      };
    }, opts, function (e) {
      var i = e.seriesType === 'scatter' ? e.value[0] : e.dataIndex, g = groups[i], prev = /^prev/.test(e.seriesName);
      var d = prev ? g.prev : g.cur;
      return { index: i, key: g.key, label: g.label, value: d && d.q ? d.q[2] : null, series: prev ? 'prev' : 'cur' };
    });
  };

  /* ---------- 堆叠条：cats [标签]；series [{key, label, tone, values}]；opts.horizontal 横向 ---------- */
  C.stack = function (cats, series, opts) {
    opts = Object.assign({ height: opts && opts.horizontal ? cats.length * 40 + 40 : 220 }, opts);
    return host('stack', function () {
      var ax = axisStyle();
      var catAx = Object.assign({}, ax, { type: 'category', data: cats, splitLine: { show: false } });
      var valAx = Object.assign({}, ax, { type: 'value', min: 0 });
      if (opts.horizontal) {
        catAx.inverse = true;
        catAx.axisLabel = Object.assign({}, ax.axisLabel, { color: css('--text'), fontSize: 13, width: opts.labelWidth || 120, overflow: 'break', lineHeight: 16 });
      }
      return {
        grid: { left: 8, right: 12, top: 30, bottom: 8, containLabel: true },
        legend: { top: 0, left: 0, icon: 'roundRect', itemWidth: 10, itemHeight: 10, textStyle: { color: css('--text-2'), fontSize: 12 } },
        tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' }, formatter: function (ps) {
          return tip(cats[ps[0].dataIndex], ps.map(function (p) { return p.seriesName + ' ' + Dash.num(p.value) + unitOf(opts); }), opts.caliber);
        } },
        xAxis: opts.horizontal ? valAx : catAx,
        yAxis: opts.horizontal ? catAx : valAx,
        series: series.map(function (s) {
          return { name: s.label, type: 'bar', stack: 'all', barMaxWidth: opts.horizontal ? 20 : 28, data: s.values,
            itemStyle: { color: tone(s.tone) }, emphasis: { focus: 'series' } };
        })
      };
    }, opts, function (e) {
      var s = series[e.seriesIndex];
      return { index: e.dataIndex, key: s.key, label: cats[e.dataIndex], value: e.value, series: s.key };
    });
  };

  /* ---------- 柱线组合：days [日期]；bars [{key, label, tone, values}]（多条就堆叠）；line {key?, label, tone, values}；opts.tips 每天一段补充 ---------- */
  C.combo = function (days, bars, line, opts) {
    opts = Object.assign({ height: 220 }, opts);
    return host('combo', function () {
      var ax = axisStyle();
      var series = bars.map(function (s) {
        return { name: s.label, type: 'bar', stack: 'bars', barMaxWidth: 16, data: s.values, itemStyle: { color: tone(s.tone) } };
      });
      if (line) series.push({ name: line.label, type: 'line', data: line.values, symbol: 'circle', symbolSize: 5,
        lineStyle: { width: 2, color: tone(line.tone) }, itemStyle: { color: tone(line.tone) }, z: 5 });
      return {
        grid: { left: 8, right: 12, top: 30, bottom: 8, containLabel: true },
        legend: { top: 0, left: 0, icon: 'roundRect', itemWidth: 10, itemHeight: 10, textStyle: { color: css('--text-2'), fontSize: 12 } },
        tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' }, formatter: function (ps) {
          var i = ps[0].dataIndex;
          return tip(Dash.day(days[i]), ps.filter(function (p) { return p.value; }).map(function (p) { return p.seriesName + ' ' + Dash.num(p.value) + unitOf(opts); })
            .concat(opts.tips && opts.tips[i] ? [opts.tips[i]] : []), opts.caliber);
        } },
        xAxis: Object.assign({}, ax, { type: 'category', data: days.map(md), splitLine: { show: false } }),
        yAxis: Object.assign({}, ax, { type: 'value', min: 0, minInterval: 1 }),
        series: series
      };
    }, opts, function (e) {
      var s = e.seriesIndex < bars.length ? bars[e.seriesIndex] : line;
      return { index: e.dataIndex, key: days[e.dataIndex], label: Dash.day(days[e.dataIndex]), value: e.value, series: s.key == null ? s.label : s.key };
    });
  };

  /* ---------- 热力：rows、cols 是标签数组；cells [{r, c, value, text?}]（r、c 是下标，text 是格内小字）；p.index 是 cells 下标 ---------- */
  C.heat = function (rows, cols, cells, opts) {
    opts = Object.assign({ height: rows.length * 58 + 34 }, opts);
    var max = cells.reduce(function (m, x) { return Math.max(m, x.value || 0); }, 0) || 1;
    return host('heat', function () {
      var ax = axisStyle();
      return {
        grid: { left: opts.labelWidth || 120, right: 8, top: 26, bottom: 4 },
        tooltip: { trigger: 'item', formatter: function (p) {
          var x = cells[p.data.i];
          return tip(rows[x.r] + ' · ' + cols[x.c], [Dash.num(x.value) + unitOf(opts), x.text], opts.caliber);
        } },
        xAxis: Object.assign({}, ax, { type: 'category', position: 'top', data: cols, axisLine: { show: false }, splitLine: { show: false },
          axisLabel: Object.assign({}, ax.axisLabel, { color: css('--text-2'), fontSize: 13 }) }),
        yAxis: Object.assign({}, ax, { type: 'category', inverse: true, data: rows, axisLine: { show: false }, splitLine: { show: false },
          axisLabel: Object.assign({}, ax.axisLabel, { color: css('--text'), fontSize: 13, fontWeight: 600 }) }),
        visualMap: { show: false, min: 0, max: max, inRange: { color: [css('--surface-2'), css('--accent')] } },
        series: [{
          type: 'heatmap', itemStyle: { borderColor: css('--surface'), borderWidth: 3, borderRadius: 6 },
          label: { show: true, fontSize: 13, lineHeight: 16, formatter: function (p) {
            var x = cells[p.data.i];
            return '{v|' + Dash.num(x.value) + '}' + (x.text ? '\n{t|' + x.text + '}' : '');
          }, rich: { v: { fontSize: 15, fontWeight: 700, color: css('--text'), align: 'center' }, t: { fontSize: 11, color: css('--text-2'), align: 'center' } } },
          data: cells.map(function (x, i) { return { value: [x.c, x.r, x.value], i: i }; })
        }]
      };
    }, opts, function (e) {
      var x = cells[e.data.i];
      return { index: e.data.i, key: x.r + ':' + x.c, label: rows[x.r] + ' · ' + cols[x.c], value: x.value, series: null };
    });
  };

  /* ---------- 折线：xs [标签或日期]；series [{key?, label, tone, values, axis: 0|1}]；opts.notes 每点一句；opts.units 两轴各自的单位 ---------- */
  C.trend = function (xs, series, opts) {
    opts = Object.assign({ height: 220 }, opts);
    var two = series.some(function (s) { return s.axis === 1; });
    var units = opts.units || [opts.unit || '', ''];
    return host('trend', function () {
      var ax = axisStyle();
      var yAx = [Object.assign({}, ax, { type: 'value', min: 0, name: units[0] || '', nameTextStyle: { color: css('--text-3') } })];
      if (two) yAx.push(Object.assign({}, ax, { type: 'value', min: 0, name: units[1] || '', splitLine: { show: false }, nameTextStyle: { color: css('--text-3') } }));
      return {
        grid: { left: 8, right: 8, top: 34, bottom: 8, containLabel: true },
        legend: { top: 0, left: 0, icon: 'roundRect', itemWidth: 10, itemHeight: 10, textStyle: { color: css('--text-2'), fontSize: 12 } },
        tooltip: { trigger: 'axis', formatter: function (ps) {
          var i = ps[0].dataIndex;
          return tip(isNaN(Dash.parseTime(xs[i])) ? String(xs[i]) : Dash.day(xs[i]),
            ps.map(function (p) { var s = series[p.seriesIndex]; return p.seriesName + ' ' + Dash.num(p.value) + (units[s.axis || 0] ? ' ' + units[s.axis || 0] : ''); })
              .concat(opts.notes && opts.notes[i] ? [opts.notes[i]] : []), opts.caliber);
        } },
        xAxis: Object.assign({}, ax, { type: 'category', data: xs.map(md), boundaryGap: false, splitLine: { show: false } }),
        yAxis: yAx,
        series: series.map(function (s) {
          return { name: s.label, type: 'line', yAxisIndex: s.axis || 0, data: s.values, symbol: 'circle', symbolSize: 5, connectNulls: false,
            lineStyle: { width: 2, color: tone(s.tone) }, itemStyle: { color: tone(s.tone) } };
        })
      };
    }, opts, function (e) {
      var s = series[e.seriesIndex];
      return { index: e.dataIndex, key: xs[e.dataIndex], label: String(xs[e.dataIndex]), value: e.value, series: s.key == null ? s.label : s.key };
    });
  };

  /* ---------- 桑基（SVG）：nodes [{id, label, tone, depth, note?}]，id 全图唯一；links [{from, to, value}]；
     值为 0 的连线画细灰虚线（结构还在）；p.key 是节点 id，p.series 是 'node' 或 'link' ---------- */
  var SVG = 'http://www.w3.org/2000/svg';
  function s(tag, attrs) {
    var el = document.createElementNS(SVG, tag);
    Object.keys(attrs || {}).forEach(function (k) { if (attrs[k] != null) el.setAttribute(k, attrs[k]); });
    return el;
  }
  C.sankey = function (nodes, links, opts) {
    opts = Object.assign({ height: 420 }, opts);
    var byId = {};
    nodes.forEach(function (n, i) { byId[n.id] = { n: n, i: i, inV: 0, outV: 0, ins: [], outs: [] }; });
    links.forEach(function (l) {
      var a = byId[l.from], b = byId[l.to];
      if (!a || !b) return;
      a.outV += l.value || 0; b.inV += l.value || 0;
      a.outs.push(l); b.ins.push(l);
    });
    var depths = nodes.reduce(function (m, n) { return Math.max(m, n.depth || 0); }, 0) + 1;
    function show(e, lines) { Dash.tip.show(lines.concat(opts.caliber ? [opts.caliber] : []), e.clientX, e.clientY); }
    return own('sankey', function (el, W) {
      var H = opts.height, nodeW = 12, gap = 8, minH = 6, labelPad = 6;
      var cols = [];
      nodes.forEach(function (n) { (cols[n.depth || 0] = cols[n.depth || 0] || []).push(byId[n.id]); });
      var scale = Infinity;
      cols.forEach(function (c) {
        var tot = c.reduce(function (t, r) { return t + Math.max(r.inV, r.outV); }, 0);
        var room = H - (c.length - 1) * gap - c.length * minH;
        if (tot > 0) scale = Math.min(scale, Math.max(0, room) / tot);
      });
      if (!isFinite(scale)) scale = 0;
      var left = 4, right = 4, span = W - left - right - nodeW;
      cols.forEach(function (c, d) {
        var x = left + (depths > 1 ? d * span / (depths - 1) : 0);
        var hs = c.map(function (r) { return minH + Math.max(r.inV, r.outV) * scale; });
        var tot = hs.reduce(function (a, b) { return a + b; }, 0) + (c.length - 1) * gap;
        var y = Math.max(0, (H - tot) / 2);
        c.forEach(function (r, i) { r.x = x; r.y = y; r.h = hs[i]; r.outY = y; r.inY = y; y += hs[i] + gap; });
      });
      var svg = s('svg', { width: W, height: H, viewBox: '0 0 ' + W + ' ' + H, class: 'sankey' });
      var gl = s('g', { class: 'sankey-links' }), gn = s('g', { class: 'sankey-nodes' });
      svg.appendChild(gl); svg.appendChild(gn);
      var linkEls = [];
      links.forEach(function (l, li) {
        var a = byId[l.from], b = byId[l.to];
        if (!a || !b) return;
        var w = (l.value || 0) * scale, y0, y1;
        if (l.value) { y0 = a.outY + w / 2; a.outY += w; y1 = b.inY + w / 2; b.inY += w; }
        else { y0 = a.y + a.h / 2; y1 = b.y + b.h / 2; }
        var x0 = a.x + nodeW, x1 = b.x, xm = (x0 + x1) / 2;
        var path = s('path', {
          d: 'M' + x0 + ',' + y0 + ' C' + xm + ',' + y0 + ' ' + xm + ',' + y1 + ' ' + x1 + ',' + y1,
          class: 'sankey-link' + (l.value ? '' : ' is-zero'),
          style: l.value ? 'stroke:' + toneVar(a.n.tone) + ';stroke-width:' + Math.max(1, w) + 'px' : null
        });
        path.addEventListener('mousemove', function (e) { show(e, [a.n.label + ' → ' + b.n.label, '近 24 小时 ' + Dash.num(l.value) + unitOf(opts)]); });
        path.addEventListener('mouseleave', Dash.tip.hide);
        if (opts.onClick) path.addEventListener('click', function () { opts.onClick({ index: li, key: l.from + '→' + l.to, label: a.n.label + ' → ' + b.n.label, value: l.value, series: 'link' }); });
        linkEls.push({ el: path, l: l });
        gl.appendChild(path);
      });
      nodes.forEach(function (n) {
        var r = byId[n.id], v = Math.max(r.inV, r.outV), last = (n.depth || 0) === depths - 1;
        var g = s('g', { class: 'sankey-node' + (opts.onClick ? ' is-click' : '') });
        g.appendChild(s('rect', { x: r.x, y: r.y, width: nodeW, height: Math.max(1, r.h), rx: 3, style: 'fill:' + toneVar(n.tone) }));
        var t = s('text', { x: last ? r.x - labelPad : r.x + nodeW + labelPad, y: r.y + r.h / 2, 'text-anchor': last ? 'end' : 'start', 'dominant-baseline': 'middle' });
        t.textContent = n.label + ' ' + Dash.num(v);
        g.appendChild(t);
        g.addEventListener('mousemove', function (e) { show(e, [n.label, '近 24 小时 ' + Dash.num(v) + unitOf(opts), n.note]); });
        g.addEventListener('mouseenter', function () {
          svg.classList.add('is-focus');
          linkEls.forEach(function (x) { x.el.classList.toggle('is-on', x.l.from === n.id || x.l.to === n.id); });
        });
        g.addEventListener('mouseleave', function () { svg.classList.remove('is-focus'); Dash.tip.hide(); });
        if (opts.onClick) g.addEventListener('click', function () { opts.onClick({ index: r.i, key: n.id, label: n.label, value: v, series: 'node' }); });
        gn.appendChild(g);
      });
      el.replaceChildren(svg);
    }, opts);
  };

  /* ---------- 密度条（HTML）：days [{day, value}]，一天一格，深浅按数；opts.coverage 记账起点，之前的格画虚线框；
     拖选一段 → opts.onBrush({from, to})（日期串），点一格 → opts.onClick(p)；opts.selected {from, to} 高亮已选 ---------- */
  C.strip = function (days, opts) {
    opts = Object.assign({}, opts);
    var max = days.reduce(function (m, d) { return Math.max(m, d.value || 0); }, 0) || 1;
    var cov = Dash.parseTime(opts.coverage);
    var sel = opts.selected || null, start = null, cells = [];
    var el = h('div.chart.chart-strip', { role: 'img', 'aria-label': opts.label || '' });
    function mark(a, b) {
      var lo = Math.min(a, b), hi = Math.max(a, b);
      cells.forEach(function (c, i) { c.classList.toggle('is-sel', a != null && i >= lo && i <= hi); });
    }
    days.forEach(function (d, i) {
      var t = Dash.parseTime(d.day), before = !isNaN(cov) && !isNaN(t) && t + 864e5 <= cov;
      var unknown = d.value == null;  // 这一天没有可靠记账：画成未确认，不当 0
      var c = h('span.strip-cell' + (before ? '.is-before' : '') + (unknown ? '.is-unknown' : d.value ? '' : '.is-zero'), {
        style: '--k:' + Math.round((d.value || 0) * 100 / max) + '%',
        dataset: { tip: Dash.day(d.day) + '\n' + (unknown ? '这天没有可靠记账' : Dash.num(d.value) + unitOf(opts)) + (before ? '\n记账开始前，不全' : '') + (opts.caliber ? '\n' + opts.caliber : '') }
      });
      c.addEventListener('mousedown', function (e) { e.preventDefault(); start = i; mark(i, i); });
      c.addEventListener('mouseenter', function () { if (start != null) mark(start, i); });
      c.addEventListener('mouseup', function () {
        if (start == null) return;
        var lo = Math.min(start, i), hi = Math.max(start, i);
        start = null;
        if (lo === hi) { if (opts.onClick) opts.onClick({ index: i, key: d.day, label: Dash.day(d.day), value: d.value, series: null }); }
        else if (opts.onBrush) opts.onBrush({ from: days[lo].day, to: days[hi].day });
      });
      cells.push(c);
      el.appendChild(c);
    });
    el.addEventListener('mouseleave', function () { if (start != null) { start = null; mark(null); } });
    if (sel) {
      var a = days.findIndex(function (d) { return d.day === sel.from; }), b = days.findIndex(function (d) { return d.day === sel.to; });
      if (a >= 0 && b >= 0) mark(a, b);
    }
    return el;
  };
})();
