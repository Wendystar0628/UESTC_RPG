/* 性能页：游戏跑得顺不顺、成本花在哪。
   块（面板 id）：perf-head 口径与记录状态 · perf-latest 最新数据 · perf-frames 帧时与帧率 · perf-render GPU 与 CPU 渲染
                  · perf-gpu GPU 占用与显存 · perf-anomalies 本次异常 · perf-trend 跨提交趋势 · perf-sessions 会话列表。
   数据：perf（README §2 的 perf 段）。路由参数：src 来源（play 作者、ai），缺省取完成时刻更晚的那份；z 缩放到本次异常的第几条；
   tg、tm 趋势的比较组与指标。只画后端给的值：不重算分位、low 与有效性；null 写后端给的原因，不画 0；目标线只在后端给出时画。 */
(function () {
  'use strict';

  var h = Dash.h;
  var PAGE = 'perf';
  var HOT = { heavy: true, severe: true };                 // 重级以上有逐帧切片
  var NEAR_MS = 10000;                                      // 点异常时曲线缩到前后 10 秒
  var PHASES = ['physMs', 'procMs', 'lateMs', 'drawMs', 'tailMs'];
  var SHOWN = 12;                                           // 本次异常先列的条数，其余折叠

  var CAL = {
    latest: '默认取完成时刻更晚的来源；数是这次会话全部帧的摘要，好坏与有效性照分析器；主视口 GPU 与视口 CPU 渲染是近似 ±2 帧的成本，只看分布，不逐帧归因',
    frames: '每桶一条均值线，浅色带画到桶内最慢一帧；帧率是每桶有效帧数 ÷ 有效帧时总和；灰纹是非稳态，紫纹是上下文未知（分析器判不出稳不稳态），色带与针是异常；缩到重级以上的异常时改画逐帧五段',
    render: '每桶均值线与桶内最大值带；渲染计时是延迟回读，尖峰前后 ±2 帧只作关联，不叫精确对齐；目标线只在本次会话条件与预算一致时画',
    gpu: '占用是有内核在跑的时间占比、整卡、相对当前频率，限帧时本该低，不能单独当卡顿症状；显存分 NVML 整卡已用与引擎记账两条线',
    anomalies: '分析器判出的异常，每类至多 20 条、按超出量排；关联阶段是墙钟区间里超出最多的一段，不据段名断言原因；点一条，上面的曲线缩到它前后 10 秒',
    trend: '横轴是主线提交序号；线是同一提交各次运行稳态摘要的中位，细点是单次运行，空心是含未提交改动的运行，圈是最新一次；体验指标只画作者会话',
    sessions: '每次会话一行，新的在前；留到哪层照现存文件；卡底的保留统计现值与上限都由后端给'
  };

  var params = {};   // 当前路由参数，各块画时更新；换来源、缩放、选趋势时在它上面改
  function go(change, block) {
    var next = Object.assign({}, params, change);
    Object.keys(next).forEach(function (k) { if (next[k] == null) delete next[k]; });
    Dash.go(PAGE, next, block);
  }

  /* ================= 取数与格式 ================= */

  function byKey(list) { var m = {}; (list || []).forEach(function (x) { m[x.key] = x; }); return m; }
  function sessionOf(pd, id) { return id == null ? null : (pd.sessions || []).filter(function (s) { return s.id === id; })[0] || null; }

  /* 来源签：路由点名的来源有会话就用它，否则取完成时刻更晚的那份 */
  function pickSource(pd, src) {
    var lt = pd.latest || {}, play = sessionOf(pd, lt.play), ai = sessionOf(pd, lt.ai);
    if (src === 'play' && play) return 'play';
    if (src === 'ai' && ai) return 'ai';
    if (!play || !ai) return play ? 'play' : ai ? 'ai' : null;
    return Dash.parseTime(play.endedAt) >= Dash.parseTime(ai.endedAt) ? 'play' : 'ai';
  }

  function current(pd, ctx) {
    params = ctx.params;
    var src = pickSource(pd, ctx.params.src);
    var s = src ? sessionOf(pd, pd.latest[src]) : null;
    var z = s && ctx.params.z != null ? (s.anomalies || [])[Number(ctx.params.z)] || null : null;
    return { src: src, s: s, detail: s && pd.details ? pd.details[s.id] || null : null, z: z, zi: z ? Number(ctx.params.z) : null };
  }

  function fmt(v, unit) {
    if (v == null || !isFinite(v)) return '—';
    if (unit === '0–1') return Dash.pct(v, 1);
    if (unit === 'B') return bytes(v);
    return Dash.num(v) + (unit ? ' ' + unit : '');
  }
  function bytes(b) {
    if (b == null || !isFinite(b)) return '—';
    var units = ['B', 'KiB', 'MiB', 'GiB', 'TiB'], i = 0;
    while (b >= 1024 && i < units.length - 1) { b /= 1024; i++; }
    return Dash.num(b) + ' ' + units[i];
  }
  function seconds(sec) {
    if (sec == null || !isFinite(sec)) return '—';
    sec = Math.max(0, Math.round(sec));
    return sec < 60 ? sec + ' 秒' : Math.floor(sec / 60) + ' 分' + (sec % 60 ? ' ' + (sec % 60) + ' 秒' : '');
  }
  function clock(sec) { sec = Math.max(0, Math.round(sec)); var m = Math.floor(sec / 60), r = sec % 60; return m + ':' + (r < 10 ? '0' : '') + r; }

  function buildText(b) {
    if (!b || b.seq == null) return b && b.reason ? '构建身份未确认：' + b.reason : '构建身份未确认';
    return '主线 #' + b.seq + (b.subject ? ' · ' + b.subject : '');
  }

  /* 设置签：比较键里的单值与成对值配后端给的中文名；嵌套的设置不展开 */
  function settingsText(s) {
    var key = s.comparisonKey || {}, labels = s.comparisonLabels || {};
    return Object.keys(labels).map(function (k) {
      var v = key[k];
      if (v == null || (typeof v === 'object' && !Array.isArray(v))) return null;
      if (Array.isArray(v)) { if (v.some(function (x) { return x == null; })) return null; v = v.join('×'); }
      return labels[k] + ' ' + v;
    }).filter(Boolean).join(' · ');
  }

  var STATUS_TONE = { current: ['level', 'ok'], partial: ['level', 'warn'], invalid: ['level', 'bad'], unsupported: ['level', 'warn'] };
  function originMark(s) { return s.origin === 'play' ? Dash.mark('side', 'author', s.originLabel) : Dash.mark('perf', 'ai', s.originLabel); }
  function statusMark(s) {
    var t = STATUS_TONE[s.status] || ['side', 'muted'];
    var m = Dash.mark(t[0], t[1], s.statusLabel || '未知');
    if (s.statusReasons && s.statusReasons.length) m.dataset.tip = s.statusLabel + '\n' + s.statusReasons.join('\n');
    return m;
  }

  /* 逐桶曲线：按 columns 解包；横轴是会话内经过的时间 */
  function unpack(d) {
    var idx = {};
    d.columns.forEach(function (c, i) { idx[c] = i; });
    var B = d.buckets || [], t0 = B.length ? B[0][0] : 0;
    function col(name) { var i = idx[name]; return i == null ? null : B.map(function (r) { return r[i]; }); }
    function index(ms) {
      if (ms == null || !B.length) return null;
      for (var i = B.length - 1; i >= 0; i--) if (B[i][0] <= ms) return i;
      return 0;
    }
    return { col: col, index: index, t0: t0, n: B.length, bucketMs: d.bucketSeconds * 1000,
      xs: B.map(function (r) { return clock((r[0] - t0) / 1000); }),
      titles: B.map(function (r) { return '开始后 ' + seconds((r[0] - t0) / 1000) + (d.bucketSeconds > 1 ? '（每桶 ' + d.bucketSeconds + ' 秒）' : '') + ' · ' + Dash.num(r[2]) + ' 帧'; }) };
  }

  /* "在做什么"：成对的开始、结束打点拼成区间；单点打点不成区间，不画 */
  function activities(s) {
    var open = {}, out = [];
    ((s.intervals && s.intervals.segments) || []).forEach(function (e) {
      var p = e.payload || {}, id = p.id == null ? '' : String(p.id);
      if (p.phase === 'begin') open[id] = e;
      else if (p.phase === 'end' && open[id]) { out.push({ fromMs: open[id].monotonicMs, toMs: e.monotonicMs, label: open[id].label }); delete open[id]; }
    });
    return out;
  }
  function doingAt(s, a) {
    var hit = activities(s).filter(function (x) { return x.fromMs <= a.toMs && x.toMs >= a.fromMs; }).map(function (x) { return x.label; });
    return hit.length ? hit.filter(function (v, i) { return hit.indexOf(v) === i; }).join('、') : '—';
  }

  /* 曲线上的标注：非稳态灰纹、上下文未知紫纹、在做什么、异常色带（跨过一桶的）与异常针；top 是针落在的高度；
     unknown 是未知区间段数，写进图注 */
  function curveMarks(s, u, top, zi) {
    var areas = [], pins = [], unknown = 0;
    ((s.intervals && s.intervals.context) || []).forEach(function (c) {
      if (c.state === 'nonSteady') areas.push({ from: u.index(c.fromMs), to: u.index(c.toMs), tone: ['perf', 'idle'], alpha: 0.22 });
      else if (c.state === 'unknown') { unknown++; areas.push({ from: u.index(c.fromMs), to: u.index(c.toMs), label: Dash.label('perf', 'unknown'), tone: ['perf', 'unknown'], alpha: 0.24 }); }
    });
    activities(s).forEach(function (a) { areas.push({ from: u.index(a.fromMs), to: u.index(a.toMs), label: a.label, tone: ['perf', 'activity'], alpha: 0.07 }); });
    (s.anomalies || []).forEach(function (a, i) {
      if (a.level === 'info' && i !== zi) return;   // 提示级只在列表里，选中时才上图
      var from = u.index(a.fromMs), to = u.index(a.toMs);
      if (from == null) return;
      if (a.durationMs >= u.bucketMs) areas.push({ from: from, to: to, tone: ['perf', a.level], alpha: i === zi ? 0.38 : 0.18 });
      else pins.push({ index: from, value: top ? top[from] : null, label: a.typeLabel + ' · ' + a.levelLabel, tone: ['perf', a.level] });
    });
    return { areas: areas, pins: pins, unknown: unknown };
  }
  function unknownNote(mk) { return mk.unknown ? Dash.label('perf', 'unknown') + ' ' + mk.unknown + ' 段（紫纹），这几段不分稳态与非稳态' : null; }

  function zoomOf(u, z) {
    if (!z) return null;
    var from = u.index(z.fromMs - NEAR_MS), to = u.index(z.toMs + NEAR_MS);
    return from == null ? null : { from: from, to: Math.max(to, from + 1) };
  }

  /* 缩到重级以上的异常时：它前后 10 秒内的逐帧切片，五段堆叠；分段不可用的行留空 */
  function hotStack(pd, detail, z) {
    if (!z || !HOT[z.level] || !detail || !detail.hotframes || !detail.hotframes.length) return null;
    var idx = {};
    detail.hotframeColumns.forEach(function (c, i) { idx[c] = i; });
    var rows = detail.hotframes.filter(function (r) { var t = r[idx.monotonicMs]; return t >= z.fromMs - NEAR_MS && t <= z.toMs + NEAR_MS; });
    if (!rows.length) return null;
    var names = (pd.labels && pd.labels.phases) || {};
    var chart = Dash.charts.stack(rows.map(function (r) { return '帧 ' + r[idx.collectionFrame]; }), PHASES.map(function (k) {
      return { key: k, label: names[k] || Dash.label('perf', k), tone: ['perf', k], values: rows.map(function (r) { return r[idx[k]]; }) };
    }), { height: 260, unit: 'ms', label: '异常前后的逐帧五段', caliber: '五段是墙钟区间，只作关联阶段；分段不可用的帧留空' });
    return h('div.pf-hot', h('div.pf-sub', '逐帧五段 · ' + rows.length + ' 帧（重级以上的切片）'), chart);
  }

  function zoomBar(z) {
    if (!z) return document.createDocumentFragment();
    return h('div.pf-zoom', h('span', '已缩到 ', h('b', z.typeLabel + ' · ' + z.levelLabel), ' 前后 10 秒，可拖动平移'),
      h('button.btn', { type: 'button', on: { click: function () { go({ z: null }); } } }, '看整段'));
  }

  /* 曲线缺数：后端在 seriesReasons 里给原因 */
  function seriesWhy(detail, key) { return (detail && detail.seriesReasons && detail.seriesReasons[key]) || '缺测'; }
  /* 列不存在或全是 null 都算没有样本，返回 null，不画空图 */
  function sampled(a) { return a && a.some(function (v) { return v != null; }) ? a : null; }
  /* 没画的曲线：defs [[曲线key, 色键]] 与 series 同序，没画的那项为 null；写名称与后端原因 */
  function missingOf(defs, series, names, detail) {
    var miss = defs.filter(function (x, i) { return !series[i]; }).map(function (x) { return ((names[x[0]] || {}).label || Dash.label('perf', x[1])) + '（' + seriesWhy(detail, x[0]) + '）'; });
    return miss.length ? h('div.note-line', '没画：' + miss.join('；')) : null;
  }

  function noSession(el) { el.appendChild(Dash.empty('还没有已关闭的会话', '录一次性能或跑一次 AI 采集，关闭后这里出数', true)); }
  function noDetail(el) { el.appendChild(Dash.empty('这次会话没有逐秒曲线', '后端只给各来源最新会话的曲线；原始记录不在时也没有', true)); }

  function expReason(s, key) {
    return (s.all && s.all.reasons && s.all.reasons[key]) || s.scopeReason || '缺测';
  }

  /* ================= 页顶：口径句与记录状态 ================= */

  var recState = null;   // { el, rec }：正在记录那一行，每 30 秒按当前时刻重写经过时间
  function paintRec(el, rec) {
    var min = rec.since ? Dash.sinceMin(rec.since) : rec.minutes;
    el.replaceChildren(h('span.pf-rec-dot'), '正在记录（自 ' + (rec.since ? Dash.at(rec.since).slice(-5) : '时刻未确认') + '，已 ' + (min == null ? '—' : Math.round(min)) + ' 分钟）');
  }
  setInterval(function () { if (recState && recState.el.isConnected) paintRec(recState.el, recState.rec); }, 30000);

  Dash.panel({
    id: 'perf-head', page: PAGE, span: 12, order: 1, window: 'commits', needs: ['perf'],
    render: function (el, d) {
      var pd = d.perf;
      el.appendChild(h('div.pf-head', h('div.pf-caliber', '这页看趋势；调优以最新原始记录为准'),
        pd.caliber ? h('div.note-line', pd.caliber) : null));
      recState = null;
      if (pd.recording) {
        var rec = h('div.pf-rec');
        recState = { el: rec, rec: pd.recording };
        paintRec(rec, pd.recording);
        el.appendChild(rec);
      }
    }
  });

  /* ================= 1 最新数据 ================= */

  function card(s, key, meta, extra) {
    var m = meta[key] || { label: key, unit: '' }, v = s.all.values[key];
    var known = v != null && isFinite(v);
    return Dash.kpi({
      label: m.label, value: known ? (m.unit === '0–1' ? Dash.pct(v, 1) : v) : null, unit: m.unit === '0–1' ? '' : m.unit,
      note: known ? extra || null : s.all.reasons[key] || '缺测'
    });
  }

  function targetNote(s, metricLabel) {
    var t = (s.targets || []).filter(function (x) { return x.label === metricLabel && x.line != null; })[0];
    if (!t) return null;
    return '目标 ≤ ' + Dash.num(t.line) + ' ms' + (t.exceeded === true ? '，越线' : t.exceeded === false ? '，未越线' : '');
  }

  Dash.panel({
    id: 'perf-latest', page: PAGE, title: '最新数据', note: '最近一次跑得怎样', caliber: CAL.latest,
    span: 12, order: 10, window: 'commits', needs: ['perf'], uses: ['params'],
    render: function (el, d, ctx) {
      var pd = d.perf, cur = current(pd, ctx), s = cur.s, meta = byKey(pd.metrics);
      var lt = pd.latest || {};
      var tabs = [['play', '作者'], ['ai', 'AI']].filter(function (x) { return sessionOf(pd, lt[x[0]]); }).map(function (x) {
        var one = sessionOf(pd, lt[x[0]]);
        return { key: x[0], label: x[1] + ' · ' + Dash.at(one.endedAt), tone: x[0] === 'play' ? ['side', 'author'] : ['perf', 'ai'] };
      });
      if (!s) { noSession(el); return; }
      el.appendChild(h('div.pf-top', Dash.chips(tabs, cur.src, function (k) { go({ src: k, z: null }); }),
        !lt.play ? h('span.muted', '作者还没有会话') : !lt.ai ? h('span.muted', 'AI 还没有会话') : null));
      el.appendChild(h('div.pf-meta',
        h('span', '完成 ', h('b', Dash.at(s.endedAt))), h('span', '时长 ', h('b', seconds(s.recordedSeconds))),
        h('span.pf-build', buildText(s.build), s.build && s.build.dirty === true ? Dash.mark('level', 'warn', '含未提交改动') : null,
          s.build && s.build.behindMainline != null ? h('span.muted', s.build.behindMainline ? '落后主线 ' + s.build.behindMainline + ' 次' : '是主线最新') : null),
        Dash.mark('side', 'muted', s.scopeLabel), statusMark(s)));
      var set = settingsText(s);
      if (set) el.appendChild(h('div.pf-set', '设置 ', set));

      var gpuLabel = (meta.mainViewportGpuP95Ms || {}).label;
      var cap = s.vramCapacityMiB;
      var whole = s.all.values.wholeGpuMemoryPeakMiB;
      el.appendChild(h('div.pf-sub', '成本'));
      el.appendChild(h('div.pf-kpis',
        card(s, 'mainViewportGpuP95Ms', meta, targetNote(s, gpuLabel)),
        card(s, 'mainViewportCpuP95Ms', meta, targetNote(s, (meta.mainViewportCpuP95Ms || {}).label)),
        card(s, 'frameSetupCpuP95Ms', meta, targetNote(s, (meta.frameSetupCpuP95Ms || {}).label)),
        Dash.kpi({ label: (meta.wholeGpuMemoryPeakMiB || {}).label || '显存峰值', value: whole == null ? null : Dash.num(whole) + (cap != null ? ' / ' + Dash.num(cap) : ''),
          unit: 'MiB', note: whole == null ? s.all.reasons.wholeGpuMemoryPeakMiB || '缺测' : cap == null ? '容量缺测，不算占比' : '已用 / 容量，占 ' + Dash.pct(whole, cap) }),
        card(s, 'engineVideoMemoryPeakMiB', meta, '引擎自己记的账，和整卡已用不是一回事')));
      el.appendChild(h('div.pf-sub', '体验'));
      if (s.scope === 'cost') {
        el.appendChild(Dash.status(expReason(s, 'averageFps'), 'muted'));
      } else {
        var tail = s.all.values.low01TailN, valid = s.all.values.validFrameN;
        el.appendChild(h('div.pf-kpis', ['averageFps', 'low1Fps', 'low01Fps', 'frameP99Ms', 'gpuUtilFraction'].map(function (k) {
          return card(s, k, meta, k === 'low01Fps' && tail != null ? '最慢 ' + Dash.num(tail) + ' 帧的均值' + (valid != null ? '，共 ' + Dash.num(valid) + ' 帧' : '') : null);
        })));
      }
    }
  });

  /* ================= 2 帧时与帧率 ================= */

  /* 目标帧时与卡顿阈值线：只画后端给了值的；高出本段最慢一帧的不画，免得把曲线压扁，在注里写数 */
  var LINE_TONE = { targetFrameMs: 'target', heavyStutterMs: 'heavy', severeStutterMs: 'severe', freezeMs: 'severe' };   // 卡死阈值判的是严重级的冻结
  function frameLines(s, peak) {
    var lines = [], notes = [];
    (s.frameLines || []).forEach(function (l) {
      if (l.value == null) { notes.push(l.label + '：' + (l.reason || '缺测')); return; }
      if (l.value > peak * 1.2) { notes.push(l.label + ' ' + Dash.num(l.value) + ' ms，高过本段最慢一帧，没画'); return; }
      lines.push({ value: l.value, label: l.label + ' ' + Dash.num(l.value) + ' ms', tone: ['perf', LINE_TONE[l.key] || 'target'] });
    });
    return { lines: lines, notes: notes };
  }

  Dash.panel({
    id: 'perf-frames', page: PAGE, title: '帧时与帧率', note: '哪一刻卡了、卡多久', caliber: CAL.frames,
    span: 12, order: 20, window: 'commits', needs: ['perf'], uses: ['params'],
    render: function (el, d, ctx) {
      var pd = d.perf, cur = current(pd, ctx), s = cur.s, names = byKey(pd.series);
      if (!s) { noSession(el); return; }
      if (s.scope === 'cost') { el.appendChild(Dash.status((cur.detail && cur.detail.reason) || expReason(s, 'frameP99Ms'), 'muted')); return; }
      if (!cur.detail) { noDetail(el); return; }
      var u = unpack(cur.detail), mean = sampled(u.col('dtMsMean')), max = u.col('dtMsMax');
      if (!mean) { el.appendChild(Dash.empty('这次会话没有帧时样本', seriesWhy(cur.detail, 'dtMs'), true)); return; }
      el.appendChild(zoomBar(cur.z));
      var hot = hotStack(pd, cur.detail, cur.z);
      if (hot) { el.appendChild(hot); return; }
      var fps = sampled(u.col('fpsMean')), hasFps = !!fps;
      var mk = curveMarks(s, u, max || mean, cur.zi);
      var peak = (max || mean).reduce(function (a, v) { return v != null && v > a ? v : a; }, 0);
      var fl = frameLines(s, peak);
      var series = [{ key: 'dt', label: (names.dtMs || {}).label || '帧时', tone: ['perf', 'frame'], mean: mean, max: max, axis: 0 }];
      if (hasFps) series.push({ key: 'fps', label: (names.fps || {}).label || '帧率', tone: ['perf', 'fps'], mean: fps, axis: 1, dashed: true });
      el.appendChild(Dash.charts.band(u.xs, series, { height: 300, units: ['ms', hasFps ? 'FPS' : ''], titles: u.titles, lines: fl.lines,
        areas: mk.areas, pins: mk.pins, zoom: zoomOf(u, cur.z), label: '逐桶帧时与帧率', caliber: CAL.frames }));
      var gaps = ((s.intervals && s.intervals.gaps) || []).length;
      el.appendChild(h('div.note-line', ['每桶 ' + cur.detail.bucketSeconds + ' 秒，共 ' + u.n + ' 桶', gaps ? '记录缺口 ' + gaps + ' 处，缺口处断开' : null,
        unknownNote(mk), hasFps ? null : '帧率：' + seriesWhy(cur.detail, 'fps')].concat(fl.notes).filter(Boolean).join('；')));
    }
  });

  /* ================= 3 GPU 与 CPU 渲染 ================= */

  Dash.panel({
    id: 'perf-render', page: PAGE, title: 'GPU 与 CPU 渲染', note: '每帧渲染花了多少', caliber: CAL.render,
    span: 12, order: 30, window: 'commits', needs: ['perf'], uses: ['params'],
    render: function (el, d, ctx) {
      var pd = d.perf, cur = current(pd, ctx), s = cur.s, names = byKey(pd.series);
      if (!s) { noSession(el); return; }
      if (!cur.detail) { noDetail(el); return; }
      var u = unpack(cur.detail);
      var defs = [['mainViewportGpuMs', 'gpu'], ['measuredViewportGpuMs', 'measured'], ['mainViewportCpuMs', 'cpu'], ['frameSetupCpuMs', 'setup']];
      var series = defs.map(function (x) {
        var mean = sampled(u.col(x[0] + 'Mean'));
        return mean
          ? { key: x[0], label: (names[x[0]] || {}).label || Dash.label('perf', x[1]), tone: ['perf', x[1]], mean: mean, max: u.col(x[0] + 'Max'), axis: 0 } : null;
      });
      var missing = missingOf(defs, series, names, cur.detail);
      series = series.filter(Boolean);
      if (s.scope === 'cost') el.appendChild(zoomBar(cur.z));
      var hot = s.scope === 'cost' ? hotStack(pd, cur.detail, cur.z) : null;
      if (hot) el.appendChild(hot);
      else if (!series.length) el.appendChild(Dash.empty('这次会话的渲染计时全部缺测', '', true));
      else {
        var top = series[0].max || series[0].mean;
        var mk = curveMarks(s, u, top, cur.zi);
        var lines = (s.targets || []).filter(function (t) { return t.line != null; }).map(function (t) {
          return { value: t.line, label: '目标 ' + Dash.num(t.line) + ' ms · ' + t.label, tone: ['perf', 'target'] };
        });
        el.appendChild(Dash.charts.band(u.xs, series, { height: 280, units: ['ms', ''], titles: u.titles, lines: lines,
          areas: mk.areas, pins: mk.pins, zoom: zoomOf(u, cur.z), label: '逐桶渲染耗时', caliber: CAL.render }));
        if (unknownNote(mk)) el.appendChild(h('div.note-line', unknownNote(mk)));
      }
      if (missing) el.appendChild(missing);
    }
  });

  /* ================= 4 GPU 占用与显存 ================= */

  Dash.panel({
    id: 'perf-gpu', page: PAGE, title: 'GPU 占用与显存', note: '显卡忙不忙、显存够不够', caliber: CAL.gpu,
    span: 12, order: 40, window: 'commits', needs: ['perf'], uses: ['params'],
    render: function (el, d, ctx) {
      var pd = d.perf, cur = current(pd, ctx), s = cur.s, names = byKey(pd.series);
      if (!s) { noSession(el); return; }
      if (!cur.detail) { noDetail(el); return; }
      var u = unpack(cur.detail), zoom = zoomOf(u, cur.z);
      function lab(k, t) { return (names[k] || {}).label || Dash.label('perf', t); }
      function pct(a) { return a && a.map(function (v) { return v == null ? null : v * 100; }); }
      var utilMean = s.scope === 'cost' ? null : sampled(u.col('gpuUtilFractionMean'));
      var power = sampled(u.col('powerWMean'));
      var load = s.scope === 'cost' ? null : sampled(u.col('normalizedGpuLoadMean'));
      var hasLoad = !!load;
      var leftDefs = s.scope === 'cost' ? [['powerW', 'power']] : [['gpuUtilFraction', 'util'], ['powerW', 'power']];
      var leftSeries = leftDefs.map(function (x) {
        if (x[0] === 'powerW') return power ? { key: 'power', label: lab('powerW', 'power'), tone: ['perf', 'power'], mean: power, axis: 1, dashed: true } : null;
        return utilMean ? { key: 'util', label: lab('gpuUtilFraction', 'util'), tone: ['perf', 'util'], mean: pct(utilMean), max: pct(u.col('gpuUtilFractionMax')), axis: 0 } : null;
      });
      var leftMissing = missingOf(leftDefs, leftSeries, names, cur.detail);
      var left = leftSeries.filter(Boolean);
      if (hasLoad) left.splice(utilMean ? 1 : 0, 0, { key: 'load', label: lab('normalizedGpuLoad', 'load') + '（提示）', tone: ['perf', 'load'], mean: pct(load), axis: 0, dashed: true });
      var notes = ['gpuUtilSampleAgeMs', 'gpuUtilSamplingWindowMs'].map(function (k) {
        var c = u.col(k + 'Mean'), v = c ? c.filter(function (x) { return x != null; }) : [];
        return v.length ? lab(k, 'util') + ' 最近 ' + fmt(v[v.length - 1], 'ms') : lab(k, 'util') + '：' + seriesWhy(cur.detail, k);
      }).concat([hasLoad ? '归一化负载是占用 × 当前频率 ÷ 最高频率，只作提示，可能超过 100%' : lab('normalizedGpuLoad', 'load') + '：' + seriesWhy(cur.detail, 'normalizedGpuLoad')]);
      var a = h('div.col', h('div.pf-sub', '占用与功耗'),
        s.scope === 'cost' ? h('div.pf-why', lab('gpuUtilFraction', 'util') + '：' + ((cur.detail && cur.detail.reason) || expReason(s, 'gpuUtilFraction'))) : null,
        left.length ? Dash.charts.band(u.xs, left, { height: 240, units: ['%', 'W'], titles: u.titles, zoom: zoom, label: 'GPU 占用与功耗', caliber: CAL.gpu })
          : Dash.empty('这次会话没有占用与功耗样本', '', true),
        leftMissing, s.scope === 'cost' ? null : h('div.note-line', notes.join('；')));
      var cap = s.vramCapacityMiB;
      var vramDefs = [['wholeGpuMemoryMiB', 'vram'], ['engineVideoMemoryMiB', 'vramEngine']];
      var vramSeries = vramDefs.map(function (x) {
        var mean = sampled(u.col(x[0] + 'Mean'));
        return mean ? { key: x[0], label: lab(x[0], x[1]), tone: ['perf', x[1]], mean: mean, max: u.col(x[0] + 'Max'), axis: 0 } : null;
      });
      var vramMissing = missingOf(vramDefs, vramSeries, names, cur.detail), vram = vramSeries.filter(Boolean);
      var b = h('div.col', h('div.pf-sub', '显存'),
        vram.length ? Dash.charts.band(u.xs, vram, { height: 240, units: ['MiB', ''], titles: u.titles, zoom: zoom, label: '显存',
          lines: cap != null ? [{ value: cap, label: '容量 ' + Dash.num(cap) + ' MiB', tone: ['perf', 'capacity'] }] : [], caliber: CAL.gpu })
          : Dash.empty('这次会话没有显存样本', '', true),
        vramMissing, cap == null ? h('div.note-line', '显存容量缺测，不画容量线') : null);
      el.appendChild(h('div.cols', a, b));
    }
  });

  /* ================= 5 本次异常 ================= */

  function anomalyRow(pd, s, a, i, zi, t0, meta, phases) {
    var unit = (meta[a.metric] || {}).unit || 'ms';
    var vt = a.value == null ? '—' : fmt(a.value, unit) + (a.threshold == null ? '' : ' / ' + fmt(a.threshold, unit));
    var why = a.candidates || [];
    var block = s.scope === 'cost' ? 'perf-render' : 'perf-frames';
    return h('button.pf-an' + (i === zi ? '.is-on' : ''), {
      type: 'button', style: '--tone: var(' + Dash.colorVar('perf', a.level) + ')',
      dataset: { tip: [a.typeLabel].concat(why.length ? why : ['没有候选原因']).join('\n') },
      on: { click: function () { go({ z: i === zi ? null : i }, i === zi ? null : block); } }
    },
      h('span.pf-an-at', t0 == null ? '—' : '+' + clock((a.fromMs - t0) / 1000)),
      h('span.pf-an-lv', h('span.dot'), a.levelLabel),
      h('span.pf-an-type', a.typeLabel),
      h('span.pf-an-phase', a.phase ? phases[a.phase] || '—' : '—'),
      h('span.pf-an-dur', !a.durationMs ? '一瞬' : a.durationMs < 1000 ? fmt(a.durationMs, 'ms') : Dash.num(a.durationMs / 1000) + ' 秒'),
      h('span.pf-an-val', vt),
      h('span.pf-an-doing', doingAt(s, a)));
  }

  /* 明细为空：只有状态是当前、明细确实留下时才是"这次没有异常"；其余照后端的状态与原因写缺数，不给正常的颜色 */
  function noAnomalies(s) {
    var kept = s.retained ? s.retained.anomalies : null;
    if (kept === false) return Dash.status('异常明细没有留下，只剩条数', 'muted');
    if (s.status === 'current' && kept === true) return Dash.status('这次没有异常', 'ok');
    var why = (s.statusReasons || []).join('；');
    return Dash.status('这次没有列出异常，但会话状态是' + (s.statusLabel ? '「' + s.statusLabel + '」' : '未知') + '，不能当作没有异常' + (why ? '：' + why : ''), s.status === 'invalid' ? 'bad' : 'warn');
  }

  Dash.panel({
    id: 'perf-anomalies', page: PAGE, title: '本次异常', note: '这次哪里不对', caliber: CAL.anomalies,
    span: 12, order: 50, window: 'commits', needs: ['perf'], uses: ['params'],
    render: function (el, d, ctx) {
      var pd = d.perf, cur = current(pd, ctx), s = cur.s;
      if (!s) { noSession(el); return; }
      var list = s.anomalies || [];
      var top = h('div.pf-top', h('span', '本次 ', h('b', Dash.num(s.anomalyTotal)), ' 条' + (s.anomalyTotal > list.length ? '，这里列 ' + list.length + ' 条' : '')),
        h('span.pf-counts', Object.keys(s.anomalyCounts || {}).map(function (k) {
          var one = list.filter(function (a) { return a.type === k; })[0];
          return Dash.mark('side', 'muted', (one ? one.typeLabel : '其他') + ' ' + s.anomalyCounts[k]);
        })),
        h('button.btn', { type: 'button', on: { click: function () { Dash.go('health'); } } }, '看健康页'));
      el.appendChild(top);
      if (!list.length) { el.appendChild(noAnomalies(s)); return; }
      var ctxs = (s.intervals && s.intervals.context) || [];
      var t0 = cur.detail && cur.detail.buckets && cur.detail.buckets.length ? cur.detail.buckets[0][0] : ctxs.length ? ctxs[0].fromMs : null;
      var meta = Object.assign(byKey(pd.series), byKey(pd.metrics)), phases = (pd.labels && pd.labels.phases) || {};
      var head = h('div.pf-an.is-head', ['时刻', '级别', '类别', '关联阶段', '持续', '值 / 阈值', '在做什么'].map(function (t) { return h('span', t); }));
      var rows = list.map(function (a, i) { return anomalyRow(pd, s, a, i, cur.zi, t0, meta, phases); });
      el.appendChild(h('div.pf-an-list', head, rows.slice(0, SHOWN)));
      if (rows.length > SHOWN) el.appendChild(Dash.fold('另外 ' + (rows.length - SHOWN) + ' 条', h('div.pf-an-list', rows.slice(SHOWN)), cur.zi != null && cur.zi >= SHOWN));
      el.appendChild(h('div.note-line', '时刻是会话开始后多久；悬停看候选原因；异常不进失败登记，开发机器的状况在健康页'));
    }
  });

  /* ================= 6 跨提交趋势 ================= */

  function groupLabel(g, i) { return (g.origin === 'play' ? '作者' : 'AI') + ' · ' + (g.scope === 'full' ? '成本与体验' : '仅成本') + ' · 第 ' + (i + 1) + ' 组'; }
  function sameGroup(s, g) {
    return s.origin === g.origin && s.scope === g.scope && s.calc === g.calc && JSON.stringify(s.comparisonKey) === JSON.stringify(g.comparisonKey);
  }

  Dash.panel({
    id: 'perf-trend', page: PAGE, title: '跨提交趋势', note: '越改越快还是越改越慢', caliber: CAL.trend,
    span: 12, order: 60, window: 'commits', needs: ['perf'], uses: ['params'],
    render: function (el, d, ctx) {
      var pd = d.perf;
      params = ctx.params;
      var groups = (pd.trends || []).filter(function (g) { return g.points && g.points.length; });
      if (!groups.length) { el.appendChild(Dash.empty('还没有进主线的运行', '只有构建身份确认、不含未提交改动的运行进趋势', true)); return; }
      var gi = Math.min(Math.max(0, Number(ctx.params.tg) || 0), groups.length - 1), g = groups[gi];
      var keys = Object.keys(g.points[0].metrics);
      var metrics = (pd.metrics || []).filter(function (m) { return keys.indexOf(m.key) >= 0 && (m.scope === 'cost' || g.origin === 'play'); });
      var m = metrics.filter(function (x) { return x.key === ctx.params.tm; })[0] || metrics[0];
      var members = (pd.sessions || []).filter(function (s) { return sameGroup(s, g); });
      el.appendChild(h('div.pf-pick', h('span.pf-pick-name', '比较组'), Dash.chips(groups.map(function (x, i) {
        return { key: String(i), label: groupLabel(x, i), tone: x.origin === 'play' ? ['side', 'author'] : ['perf', 'ai'] };
      }), String(gi), function (k) { go({ tg: k, tm: null }); })));
      if (members[0] && settingsText(members[0])) el.appendChild(h('div.pf-set', '设置 ', settingsText(members[0])));
      el.appendChild(h('div.pf-pick', h('span.pf-pick-name', '指标'), Dash.chips(metrics.map(function (x) {
        return { key: x.key, label: x.label + (x.scope === 'experience' ? '（体验）' : '') };
      }), m.key, function (k) { go({ tm: k }); })));
      if (g.status === 'stale') el.appendChild(Dash.status('这一组是旧的计算版本，只作历史证据，不能当当前基线', 'warn'));
      if (g.status === 'unsupported') el.appendChild(Dash.status('这一组是旧的记录格式，只作历史证据，不能当当前基线', 'warn'));
      var pts = g.points, seqs = pts.map(function (p) { return p.seq; });
      var frac = m.unit === '0–1', show = function (v) { return v == null ? null : frac ? v * 100 : v; }, unit = frac ? '%' : m.unit;
      var latestIds = [pd.latest && pd.latest.play, pd.latest && pd.latest.ai];
      var dots = members.map(function (s) {
        var i = s.build ? seqs.indexOf(s.build.seq) : -1, v = s.steady && s.steady.values[m.key];
        return i < 0 || v == null ? null : { index: i, value: show(v), hollow: s.build.dirty !== false, ring: latestIds.indexOf(s.id) >= 0 };
      });
      var unplaced = dots.filter(function (x) { return !x; }).length;
      var tone = g.origin === 'play' ? ['side', 'author'] : ['perf', 'ai'];
      var target = members.length ? (members[0].targets || []).filter(function (t) { return t.label === m.label && t.line != null; })[0] : null;
      el.appendChild(Dash.charts.trend(seqs.map(function (q) { return '#' + q; }), [
        { key: 'median', label: g.label, tone: tone, values: pts.map(function (p) { return show(p.metrics[m.key]); }) },
        { key: 'runs', label: '单次运行', tone: tone, points: dots.filter(Boolean) }
      ], { height: 260, units: [unit, ''], gap: true, label: m.label + '的跨提交趋势', caliber: CAL.trend,
        titles: pts.map(function (p) { return '主线 #' + p.seq + (p.subject ? ' · ' + p.subject : ''); }),
        notes: pts.map(function (p) { return Dash.num(p.n) + ' 次运行'; }),
        lines: target ? [{ value: show(target.line), label: '目标 ' + fmt(target.line, m.unit), tone: ['perf', 'target'] }] : [] }));
      el.appendChild(h('div.note-line', g.label + '；横轴是主线提交序号' + (unplaced ? '；另有 ' + unplaced + ' 次运行没有主线序号或没有这项值，没画' : '')));
      el.appendChild(h('div.pf-sub', '每分钟异常数'));
      el.appendChild(Dash.charts.trend(seqs.map(function (q) { return '#' + q; }), [
        { key: 'rate', label: '每分钟异常组数（各次运行的中位）', tone: ['perf', 'heavy'], values: pts.map(function (p) { return p.anomaliesPerMinute; }) }
      ], { height: 150, units: ['次/分钟', ''], gap: true, label: '每次提交的每分钟异常数', caliber: CAL.trend,
        titles: pts.map(function (p) { return '主线 #' + p.seq + (p.subject ? ' · ' + p.subject : ''); }),
        notes: pts.map(function (p) { return Dash.num(p.anomalyRateRuns) + ' 次运行计入' + (p.anomalyRateReason ? '；' + p.anomalyRateReason : ''); }) }));
    }
  });

  /* ================= 7 会话列表 ================= */

  var LAYERS = [['raw', '原始记录'], ['hotframes', '逐帧切片'], ['anomalies', '异常明细'], ['summary', '摘要']];
  var KEEP = [['authorSessions', '作者会话'], ['authorBytes', '作者记录'], ['aiBatches', 'AI 批次'], ['historyPerGroup', '每组历史'], ['diagnosticBytes', '诊断附件']];

  function keepText(x) {
    if (!x) return '未知';
    var f = function (v) { return v == null ? '—' : x.unit === 'B' ? bytes(v) : Dash.num(v) + ' ' + x.unit; };
    return f(x.value) + ' / ' + (x.limit == null ? '上限未知' : f(x.limit));
  }

  Dash.panel({
    id: 'perf-sessions', page: PAGE, title: '会话列表', note: '录过哪些、留下了什么', caliber: CAL.sessions,
    span: 12, order: 70, window: 'commits', needs: ['perf'],
    render: function (el, d) {
      var pd = d.perf, list = pd.sessions || [];
      var latest = [pd.latest && pd.latest.play, pd.latest && pd.latest.ai];
      if (!list.length) noSession(el);
      else el.appendChild(h('div.pf-ss', list.map(function (s) {
        var r = s.retained || {};
        return h('div.pf-s',
          h('span.pf-s-who', originMark(s), latest.indexOf(s.id) >= 0 ? h('span.chip.pf-newest', '最新') : null),
          h('span.pf-s-at', Dash.at(s.endedAt), h('span.muted', ' · ' + seconds(s.recordedSeconds))),
          h('span.pf-s-build', buildText(s.build), s.build && s.build.dirty === true ? h('span.t-warn.pf-dirty', ' · 含未提交改动') : null),
          h('span.pf-s-tags', Dash.mark('side', 'muted', s.scopeLabel), statusMark(s), Dash.mark('side', 'muted', '异常 ' + Dash.num(s.anomalyTotal))),
          h('span.pf-s-keep', LAYERS.map(function (x) { return h('span.pf-layer' + (r[x[0]] ? '.is-on' : ''), x[1]); })));
      })));
      var dc = pd.displayCoverage;
      var keep = pd.retention || {};
      el.appendChild(h('div.pf-keep',
        h('div.pf-keep-row', KEEP.map(function (x) { return h('span', x[1] + ' ', h('b', keepText(keep[x[0]]))); })),
        h('div.note-line', [pd.coverage && pd.coverage.from ? '最早的会话 ' + Dash.day(pd.coverage.from) + ' ' + Dash.at(pd.coverage.from).slice(-5) : null,
          dc && dc.totalSessions != null && dc.sessions !== dc.totalSessions ? '为守体积只显示 ' + dc.sessions + ' / 共 ' + dc.totalSessions + ' 个会话，原始记录不受影响' : null,
          dc && dc.note].filter(Boolean).join('；'))));
    }
  });
})();
