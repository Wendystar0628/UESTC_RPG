/* 观察台的时间轴图：trend（折线与散点，日期或提交序号作横轴）与 band（均值线 + 最大值带，逐桶时间作横轴）。
   两者共用标注：opts.lines 阈值线、opts.areas 区间色带、opts.pins 异常针；band 另有 opts.zoom 缩到一段。颜色一律经 palette.js 的 tone。
   依赖 charts.js 的宿主、悬停框与坐标轴样式（Dash.charts.internal），在它之后加载。接口见 README §3.6。 */
(function () {
  'use strict';

  var Dash = window.Dash;
  if (!Dash.internal) return;
  var C = Dash.charts, I = C.internal;
  var css = I.css, tone = I.tone, alpha = I.alpha, tip = I.tip;

  function unitAt(units, s) { var u = units[(s && s.axis) || 0]; return u ? ' ' + u : ''; }

  /* 标注挂到对应轴上第一条系列：lines [{value, label, tone, axis?}] 横向阈值线；
     areas [{from, to, label?, tone, alpha?}] 横轴下标区间的色带；pins [{index, value, label, tone}] 异常针。
     返回 {输入系列下标: {markLine?, markArea?, markPoint?}} */
  function marks(opts, list) {
    var out = {};
    function slot(axis) {
      var i = list.findIndex(function (s) { return (s.axis || 0) === axis; });
      i = i < 0 ? 0 : i;
      return out[i] || (out[i] = {});
    }
    (opts.lines || []).forEach(function (l) {
      var m = slot(l.axis || 0);
      m.markLine = m.markLine || { symbol: 'none', silent: true, animation: false, data: [] };
      m.markLine.data.push({
        yAxis: l.value, name: l.label,
        lineStyle: { color: tone(l.tone), type: 'dashed', width: 1.4 },
        label: { formatter: l.label, position: 'insideEndTop', color: tone(l.tone), fontSize: 11 }
      });
    });
    var areas = (opts.areas || []).filter(function (a) { return a.from != null && a.to != null && a.to >= a.from; });
    if (areas.length) {
      slot(0).markArea = { silent: true, animation: false, data: areas.map(function (a) {
        return [{ xAxis: a.from, name: a.label || '', itemStyle: { color: alpha(tone(a.tone), a.alpha == null ? 0.14 : a.alpha) },
          label: { show: !!a.label, position: 'insideTopLeft', color: css('--text-3'), fontSize: 11 } }, { xAxis: Math.max(a.to, a.from + 1) }];
      }) };
    }
    var pins = (opts.pins || []).filter(function (p) { return p.index != null && p.value != null; });
    if (pins.length) {
      slot(0).markPoint = { symbol: 'pin', symbolSize: 20, animation: false, label: { show: false },
        data: pins.map(function (p) { return { coord: [p.index, p.value], name: p.label, itemStyle: { color: tone(p.tone) } }; }) };
    }
    return out;
  }

  function yAxes(units, two) {
    var ax = I.axisStyle();
    var y = [Object.assign({}, ax, { type: 'value', min: 0, name: units[0] || '', nameTextStyle: { color: css('--text-3') } })];
    if (two) y.push(Object.assign({}, ax, { type: 'value', min: 0, name: units[1] || '', splitLine: { show: false }, nameTextStyle: { color: css('--text-3') } }));
    return y;
  }

  /* ---------- 折线：xs [标签或日期]；series [{key?, label, tone, values, axis: 0|1, dashed?}]，
     或散点 {key?, label, tone, axis?, points: [{index, value, hollow?, ring?}]}（同一横轴位置可有多点，hollow 空心，ring 加粗圈）；
     opts.units 两轴单位；opts.notes 每点一句；opts.titles 每个横轴位置的悬停标题；标注见 marks ---------- */
  C.trend = function (xs, series, opts) {
    opts = Object.assign({ height: 220 }, opts);
    var two = series.some(function (s) { return s.axis === 1; });
    var units = opts.units || [opts.unit || '', ''];
    function title(i) {
      if (opts.titles && opts.titles[i] != null) return opts.titles[i];
      return isNaN(Dash.parseTime(xs[i])) ? String(xs[i]) : Dash.day(xs[i]);
    }
    return I.host('trend', function () {
      var ax = I.axisStyle();
      var mk = marks(opts, series);
      return {
        grid: { left: 8, right: 8, top: 22, bottom: 8, containLabel: true },
        legend: I.hiddenLegend(),
        tooltip: { trigger: 'axis', formatter: function (ps) {
          var i = ps[0].dataIndex;
          if (ps[0].seriesType === 'scatter') i = ps[0].value[0];
          return tip(title(i), ps.map(function (p) {
            var s = series[p.seriesIndex], v = s.points ? p.value[1] : p.value;
            return p.seriesName + ' ' + Dash.num(v) + unitAt(units, s);
          }).concat(opts.notes && opts.notes[i] ? [opts.notes[i]] : []), opts.caliber);
        } },
        xAxis: Object.assign({}, ax, { type: 'category', data: xs.map(function (x) { return /^\d{4}-\d{2}-\d{2}/.test(x) ? I.md(x) : String(x); }), boundaryGap: !!opts.gap, splitLine: { show: false } }),
        yAxis: yAxes(units, two),
        series: series.map(function (s, i) {
          var c = tone(s.tone);
          if (s.points) {
            return Object.assign({ name: s.label, type: 'scatter', yAxisIndex: s.axis || 0, z: 4,
              data: s.points.map(function (p) {
                return { value: [p.index, p.value], symbolSize: p.ring ? 12 : 7,
                  itemStyle: { color: p.hollow ? css('--surface') : c, borderColor: c, borderWidth: p.ring ? 2.5 : p.hollow ? 1.5 : 0 } };
              }) }, mk[i]);
          }
          return Object.assign({ name: s.label, type: 'line', yAxisIndex: s.axis || 0, data: s.values, symbol: 'circle', symbolSize: 5, connectNulls: false,
            lineStyle: { width: 2, color: c, type: s.dashed ? 'dashed' : 'solid' }, itemStyle: { color: c } }, mk[i]);
        })
      };
    }, opts, function (e) {
      if (e.componentType !== 'series') return null;
      var s = series[e.seriesIndex], i = s.points ? e.value[0] : e.dataIndex;
      return { index: i, key: xs[i], label: String(xs[i]), value: s.points ? e.value[1] : e.value, series: s.key == null ? s.label : s.key };
    }, series);
  };

  /* ---------- 均值线 + 最大值带：xs [横轴标签]；series [{key?, label, tone, mean, max?, axis: 0|1, dashed?}]，
     有 max 的在均值线上方画到最大值的浅色带，没有就只画线；值为 null 处断开，不补线。
     opts.units 两轴单位；opts.titles 每个横轴位置的悬停标题；标注见 marks；opts.zoom {from, to} 横轴下标，缩到这一段，可拖动平移 ---------- */
  C.band = function (xs, series, opts) {
    opts = Object.assign({ height: 240 }, opts);
    var two = series.some(function (s) { return s.axis === 1; });
    var units = opts.units || [opts.unit || '', ''];
    var map = [];   // ECharts 系列下标 → 输入系列下标
    return I.host('band', function () {
      var ax = I.axisStyle();
      var mk = marks(opts, series);
      var out = [];
      map = [];
      series.forEach(function (s, i) {
        var c = tone(s.tone);
        out.push(Object.assign({ name: s.label, type: 'line', yAxisIndex: s.axis || 0, data: s.mean, stack: s.max ? 'band' + i : null,
          symbol: 'none', connectNulls: false, z: s.dashed ? 1 : 3, lineStyle: { width: s.dashed ? 1.2 : 1.8, color: c, type: s.dashed ? 'dashed' : 'solid' }, itemStyle: { color: c } }, mk[i]));
        map.push(i);
        if (!s.max) return;
        // 带宽只是几何量：最大值减均值，叠在均值线上
        out.push({ name: s.label, type: 'line', yAxisIndex: s.axis || 0, stack: 'band' + i, symbol: 'none', connectNulls: false, z: 2,
          data: s.max.map(function (v, j) { var m = s.mean[j]; return v == null || m == null ? null : Math.max(0, v - m); }),
          lineStyle: { width: 0 }, areaStyle: { color: alpha(c, 0.2) }, itemStyle: { color: c } });
        map.push(i);
      });
      var opt = {
        grid: { left: 8, right: 8, top: 28, bottom: 8, containLabel: true },
        legend: I.hiddenLegend(),
        tooltip: { trigger: 'axis', formatter: function (ps) {
          var i = ps[0].dataIndex, seen = {};
          var lines = ps.map(function (p) {
            var k = map[p.seriesIndex];
            if (seen[k]) return null;
            seen[k] = true;
            var s = series[k], m = s.mean[i];
            if (m == null) return s.label + ' 缺测';
            return s.label + ' ' + (s.max ? '均值 ' + Dash.num(m) + ' · 最大 ' + Dash.num(s.max[i]) : Dash.num(m)) + unitAt(units, s);
          });
          return tip(opts.titles && opts.titles[i] != null ? opts.titles[i] : String(xs[i]), lines, opts.caliber);
        } },
        xAxis: Object.assign({}, ax, { type: 'category', data: xs, boundaryGap: false, splitLine: { show: false } }),
        yAxis: yAxes(units, two),
        series: out
      };
      if (opts.zoom) opt.dataZoom = [{ type: 'inside', startValue: opts.zoom.from, endValue: opts.zoom.to, zoomOnMouseWheel: false, moveOnMouseWheel: false, moveOnMouseMove: true }];
      return opt;
    }, opts, function (e) {
      if (e.componentType !== 'series') return null;
      var s = series[map[e.seriesIndex]];
      return { index: e.dataIndex, key: xs[e.dataIndex], label: String(xs[e.dataIndex]), value: s.mean[e.dataIndex], series: s.key == null ? s.label : s.key };
    }, series);
  };
})();
