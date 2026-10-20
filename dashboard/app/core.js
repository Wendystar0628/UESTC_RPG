/* 观察台运行时：推送订阅（/api/events）与按需加载数据、路由与筛选、面板注册与渲染、页眉与导航角标、写回（"知道了"、留言）。
   通用格式化与小组件在 ui.js，色表在 palette.js，图在 charts.js，文档阅读器在 reader.js；接口见 dashboard/README.md §3。
   面板只经 Dash.panel 声明要哪几份数据，不自己订阅、不自己拉文件。 */
(function () {
  'use strict';

  var M = window.DashManifest;
  var Dash = window.Dash = {};

  /* 页面不经本地服务打开时，推送与写回都没有：整页只写一句 */
  if (!/^https?:$/.test(location.protocol)) {
    var only = function () {
      document.body.replaceChildren();
      var p = document.createElement('p');
      p.className = 'only-line';
      p.textContent = '请双击「打开观察台」，经本地服务打开';
      document.body.appendChild(p);
    };
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', only); else only();
    Dash.panel = function () {};
    return;
  }

  var store = {};      // 数据名 -> 数据
  var meta = {};       // 数据名 -> { ver 已交付的版本, loading, reqVer, got 本次请求执行了 Dash.put, errors 这版载荷里后端写的失败原因, fail 'fetch'|'run'|'errors'|null }
  var remote = {};     // 数据名 -> 推送来的最新版本
  var checked = {};    // 数据名 -> { at 采集最后一次确认它仍是现状的时刻, stale 旧了吗 true|false|null, ratio 已用掉旧期限的比例 }；全部由后端在推送里给
  var srcs = {};       // 数据名 -> manifest 条目
  var panels = {};     // 面板 id -> 面板记录
  var dirty = {};
  var flushTimer = 0;
  var booted = false;
  var push = { full: false, on: false, es: null, app: null };
  var view = { page: null, block: null, params: {}, filters: null };
  var current = null;  // 正在画的面板（Dash.freshness 据此知道这块要跟着 checked 重画）

  M.data.forEach(function (s) { srcs[s.name] = s; });

  function h() { return Dash.h.apply(null, arguments); }
  function pageOf(id) { return M.pages.filter(function (p) { return p.id === id; })[0] || null; }
  function labelOf(n) { return srcs[n] ? srcs[n].label : n; }

  /* ================= 数据 ================= */

  Dash.put = function (name, data) {
    if (!srcs[name]) return;
    var m = meta[name] || (meta[name] = {});
    var errs = data && Array.isArray(data.errors) ? data.errors : [];
    // 只暂存在本次请求里，下载确认完成时才一次交付；后端发布的 errors 载荷是采集失败，不是业务数据
    m.pending = { errors: errs.length ? errs : null, data: data };
  };

  /* 能画的数据：最近一次读取失败的不给（不显示旧值） */
  function have(name) {
    var m = meta[name];
    return m && m.fail ? null : (store[name] || null);
  }

  function loadData(s) {
    var m = meta[s.name] || (meta[s.name] = {});
    if (m.loading) return;
    m.loading = true;
    m.reqVer = remote[s.name];
    m.pending = null;   // 已交付的版本、失败状态与数据在 done 之前不动
    var el = document.createElement('script');
    el.src = s.path + '?v=' + encodeURIComponent(m.reqVer || Date.now());
    function done(ok) {
      m.loading = false;
      el.remove();
      var before = m.fail;
      var got = m.pending;
      m.pending = null;
      var delivered = ok && !!got;   // 文件执行出错或没调 Dash.put：不记新版本
      if (delivered) {   // 一次交付：版本、失败状态、errors 与数据同时换成这一版；errors 载荷也是真实版本，等后端发下一版
        m.ver = m.reqVer;
        m.errors = got.errors;
        m.fail = got.errors ? 'errors' : null;
        if (got.errors) delete store[s.name]; else store[s.name] = got.data;
      } else {
        m.fail = ok ? 'run' : 'fetch';
        console.warn('[Dash] 数据 ' + s.name + (ok ? ' 的文件执行出错或没有调用 Dash.put' : ' 下载失败'));
      }
      if (delivered && m.ver !== remote[s.name]) { loadData(s); return; }   // 加载期间又推来新版本
      if (!m.fail || m.fail === 'errors' || !before) dependents(s.name).forEach(schedule);
      afterData();
    }
    el.onload = function () { done(true); };
    el.onerror = function () { done(false); };
    document.head.appendChild(el);
  }

  /* 这份数据此刻要不要：页眉导航要用的（keep）、当前页面板 needs 的 */
  function wanted(name) {
    if (srcs[name].keep) return true;
    return Object.keys(panels).some(function (id) {
      var p = panels[id];
      return p.page === view.page && p.needs.indexOf(name) >= 0;
    });
  }

  /* 要的数据里，版本落后于推送的就拉；读取失败的等下一次推送（版本或心跳）再试 */
  function sync(retry) {
    if (!booted || !push.full) return;
    M.data.forEach(function (s) {
      var m = meta[s.name] || {};
      if (!(s.name in remote) || !wanted(s.name) || m.loading) return;
      if (m.fail === 'fetch' || m.fail === 'run' ? retry : m.ver !== remote[s.name]) loadData(s);
    });
  }

  /* 推送：versions（连上先来一次全量，之后只来变了的）与 checked（每 15 s，兼当心跳） */
  function onVersions(ev) {
    var msg = JSON.parse(ev.data);
    var vs = msg.versions || {};
    if ('app' in vs) {
      if (push.app == null) push.app = vs.app;
      else if (vs.app !== push.app) { location.reload(); return; }
    }
    if (msg.full) {
      push.full = true;
      M.data.forEach(function (s) {
        if (s.name in vs) return;
        delete remote[s.name]; delete store[s.name];   // 服务端没有这份：按"还没有数据"
        meta[s.name] = { tried: true };
        dependents(s.name).forEach(schedule);
      });
    }
    Object.keys(vs).forEach(function (n) {
      if (!srcs[n] || remote[n] === vs[n]) return;
      remote[n] = vs[n];
      if (!wanted(n)) delete store[n];   // 用不到的先丢掉，切过去时重拉，不先画旧的
    });
    Object.assign(checked, msg.checked || {});
    setPush(true);
    sync(msg.full);
    afterData();
  }

  function onChecked(ev) {
    var msg = JSON.parse(ev.data);
    Object.assign(checked, msg.checked || {});
    sync(true);
    Object.keys(panels).forEach(function (id) { if (panels[id].fresh) schedule(panels[id]); });
    afterData();
  }

  function connect() {
    var es = push.es = new EventSource('/api/events');
    es.addEventListener('versions', onVersions);
    es.addEventListener('checked', onChecked);
    es.onerror = function () {
      setPush(false);
      if (es.readyState === EventSource.CLOSED) setTimeout(connect, 3000);   // 服务返回错误时浏览器不会自己重连
    };
  }

  function setPush(on) {
    push.on = on;
    var c = document.getElementById('corner');
    c.hidden = on;
    c.textContent = on ? '' : '推送断开，正在重连';
  }

  /* wait 读取中 / missing 读过但没有这份 / error 没读到 / ok */
  Dash.dataState = function (name) {
    var m = meta[name] || {};
    if (m.fail) return 'error';
    if (store[name]) return 'ok';
    return push.full && !(name in remote) ? 'missing' : 'wait';
  };

  function failReason(name) {
    var m = meta[name] || {};
    if (m.fail === 'errors') return '来源没读到：' + m.errors.join('；');
    return m.fail === 'fetch' ? '下载没成功，下一次推送时再试' : '数据文件有误，没有交出数据';
  }

  Dash.checkedAt = function (name) { return (checked[name] || {}).at || null; };

  /* 旧不旧、已用掉旧期限的多少，都是后端在 checked 推送里算好的；null 是后端不判断 */
  function freshOf(name) {
    var c = checked[name] || {};
    return { stale: c.stale == null ? null : c.stale, ratio: c.ratio == null ? null : c.ratio };
  }
  function isStale(name) { return freshOf(name).stale === true; }

  Dash.internal = { srcs: srcs, freshOf: freshOf, beginFresh: function () { if (current) current.fresh = true; } };

  /* ================= 写回：本地服务 serve.py ================= */

  function post(url, body) {
    return fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
      .catch(function () { throw new Error('观察台本地服务没在运行，没有送出；双击"打开观察台"再试'); })
      .then(function (r) {
        return r.json().catch(function () { return null; }).then(function (j) {
          if (!j) throw new Error('本地服务不认这个操作，要先换成新版本地服务');
          if (!r.ok || j.ok !== true) {
            var e = new Error(j.error || '本地服务没有接受');
            e.status = r.status;
            throw e;
          }
          return j;
        });
      });
  }

  function reload(name) { var m = meta[name]; if (m) m.ver = null; loadData(srcs[name]); }

  /* "知道了"：带着作者看到的那一版（revision）确认；内容已经变了（409）就换成新内容，请作者重新过目 */
  Dash.ack = function (id, revision) {
    return post('/api/attention/ack', { id: id, revision: revision }).then(function (j) {
      reload('attention');
      return j;
    }, function (e) {
      if (e.status === 409) { reload('attention'); throw new Error('内容刚变过，已换成新内容，请重新过目'); }
      throw e;
    });
  };

  Dash.feedback = function (text) {
    return post('/api/feedback', { text: text }).then(function (j) { reload('feedback'); return j; });
  };

  /* ================= 面板 ================= */

  function dependents(name) {
    return Object.keys(panels).map(function (id) { return panels[id]; })
      .filter(function (p) { return p.needs.indexOf(name) >= 0; });
  }

  Dash.panel = function (def) {
    var p = panels[def.id] = Object.assign({ needs: [], uses: [], order: 100, span: 12 }, def);
    if (booted) { mount(p); sync(); }
  };

  function mount(p) {
    var host = document.querySelector('#page-' + p.page + ' .page-body');
    if (!host) { console.warn('[Dash] 面板 ' + p.id + ' 的页面不在清单里'); return; }
    var box = h('section.panel.card.span-' + p.span, { dataset: { panel: p.id, order: p.order } });
    p.metaEl = h('span.card-meta');
    if (p.title) {
      box.appendChild(h('header.card-head',
        h('div.card-titles', h('h2.card-title', p.title,
          p.caliber ? h('span.cal', { tabindex: '0', 'aria-label': '口径：' + p.caliber, dataset: { tip: p.caliber } }, 'ⓘ') : null),
          p.note ? h('p.card-note', p.note) : null),
        p.metaEl));
    }
    if (p.fixed) {
      var fixedEl = h('div.card-fixed');
      box.appendChild(fixedEl);
      p.fixed(fixedEl);
    }
    p.bodyEl = h('div.card-body');
    p.footEl = h('div.card-foot', { hidden: true });
    box.appendChild(p.bodyEl);
    box.appendChild(p.footEl);
    p.el = box;
    var after = null;
    Array.prototype.forEach.call(host.children, function (c) {
      if (!after && Number(c.dataset.order) > p.order) after = c;
    });
    host.insertBefore(box, after);
    schedule(p);
  }

  function schedule(p) {
    if (!p || !p.el) return;
    dirty[p.id] = p;
    if (!flushTimer) flushTimer = setTimeout(flush, 30);
  }

  /* 只画当前页的面板；别的页记着脏，切过去再画 */
  function flush() {
    flushTimer = 0;
    Object.keys(dirty).forEach(function (id) {
      var p = dirty[id];
      if (p.page !== view.page) return;
      delete dirty[id];
      renderPanel(p);
    });
    refreshMeta();
  }

  /* needs 里各份数据的 coverage.from 取最晚的：那份记账最短 */
  function coverageOf(p) {
    var latest = null;
    p.needs.forEach(function (n) {
      var c = have(n) && store[n].coverage, t = c ? Dash.parseTime(c.from) : NaN;
      if (!isNaN(t) && (latest == null || t > Dash.parseTime(latest))) latest = c.from;
    });
    return latest;
  }

  var WINDOW_MS = { '24h': 864e5, '7d': 7 * 864e5, '14d': 14 * 864e5, '30d': 30 * 864e5 };

  /* 面板数据的时间窗起点：window 缺省按筛选条的 range（地址里拖选了 from 就从那天起） */
  function windowStart(p) {
    var w = p.window || 'range';
    if (w !== 'range') return Date.now() - WINDOW_MS[w];
    var from = Dash.parseTime(view.params.from);
    return isNaN(from) ? Date.now() - WINDOW_MS[view.filters.range] : from;
  }

  /* 记账起点同一页只说一次：这一页上记账起点相同、且起点晚于各自时间窗的面板里，按 order 第一块的卡底写 */
  function coverageOwner(p, cov) {
    var t = Dash.parseTime(cov);
    return Object.keys(panels).map(function (id) { return panels[id]; })
      .filter(function (q) { return q.page === p.page && coverageOf(q) === cov && t > windowStart(q); })
      .sort(function (a, b) { return a.order - b.order || (a.id < b.id ? -1 : 1); })[0] || null;
  }

  function renderPanel(p) {
    var data = {}, missing = [], waiting = false, failed = [];
    p.needs.forEach(function (n) {
      data[n] = have(n);
      if (data[n]) return;
      var st = Dash.dataState(n);
      if (st === 'error') failed.push(n);
      else if (st === 'wait') waiting = true;
      else missing.push(n);
    });
    var got = p.needs.length - failed.length - missing.length - (waiting ? 1 : 0);
    var body = p.bodyEl;
    Dash.charts.release(body);
    p.el.classList.remove('is-error', 'is-loading');
    p.footEl.hidden = true;
    var ready = p.partial ? (got > 0 || !p.needs.length) && !waiting : !failed.length && !missing.length && !waiting;
    if (!ready) {
      if (failed.length) {
        body.replaceChildren(Dash.empty('没读到', failed.map(function (n) { return labelOf(n) + '：' + failReason(n); }).join('；'), true));
      } else if (waiting) {
        p.el.classList.add('is-loading');
        body.replaceChildren(h('div.skeleton'));
      } else {
        body.replaceChildren(Dash.empty('还没有数据', '缺' + missing.map(labelOf).join('、') + '，后台采集写出之后会自动出现', true));
      }
      return;
    }
    var ctx = {
      filters: view.filters, params: view.params,
      stale: p.needs.some(isStale), coverage: coverageOf(p)
    };
    var opens = Array.prototype.map.call(body.querySelectorAll('details'), function (d) { return d.open; });
    var inner = h('div.panel-inner');
    body.replaceChildren(inner);
    current = p;
    p.fresh = false;
    p.drawn = { params: ctx.params, filters: ctx.filters };
    try {
      p.render(inner, data, ctx);
    } catch (e) {
      console.error('[Dash] 面板 ' + p.id + ' 出错', e);
      p.el.classList.add('is-error');
      Dash.charts.release(body);
      body.replaceChildren(Dash.empty('这块出错了', '别的面板不受影响；细节在浏览器控制台', true));
      return;
    } finally { current = null; }
    var ds = inner.querySelectorAll('details');
    if (ds.length === opens.length) Array.prototype.forEach.call(ds, function (d, i) { if (opens[i]) d.open = true; });
    if (ctx.coverage && coverageOwner(p, ctx.coverage) === p) {
      p.footEl.hidden = false;
      p.footEl.textContent = '记账从 ' + Dash.day(ctx.coverage) + ' ' + Dash.at(ctx.coverage).slice(-5) + ' 起，之前的不全';
    }
  }

  Dash.redraw = function (id) { schedule(panels[id]); };
  Dash.redrawAll = function () { Object.keys(panels).forEach(function (id) { schedule(panels[id]); }); };

  /* 卡头的"更新 HH:MM"（needs 里最旧的一份）；旧了写"· 旧了"，橙字，正文降透明 */
  function refreshMeta() {
    Object.keys(panels).forEach(function (id) {
      var p = panels[id];
      if (!p.el || p.page !== view.page) return;
      var oldest = NaN, stale = false;
      p.needs.forEach(function (n) {
        if (!have(n)) return;
        var t = Dash.parseTime(store[n].updated);
        if (!isNaN(t) && (isNaN(oldest) || t < oldest)) oldest = t;
        if (isStale(n)) stale = true;
      });
      p.el.classList.toggle('is-stale', stale);
      p.metaEl.textContent = isNaN(oldest) ? '' : '更新 ' + Dash.at(oldest) + (stale ? ' · 旧了' : '');
    });
    updateHeader();
  }

  /* ================= 页眉、导航、角标 ================= */

  function updateHeader() {
    var box = document.getElementById('leads');
    var l = have('live');
    box.replaceChildren.apply(box, (l && l.leads || []).map(function (x) {
      var state = x.running === true ? '在跑' : x.running === false ? '空闲' : '未确认';
      return h('span.lead-pill.t-' + Dash.sideKey(x.who), { title: x.who + ' ' + state + (x.since ? '，自 ' + Dash.at(x.since) : '') },
        h('span.dot' + (x.running === true ? '.is-on' : x.running === false ? '.is-off' : '.is-unknown')), x.who + ' ' + state);
    }));
    var old = M.data.filter(function (s) { return isStale(s.name); }).length;
    var unknown = M.data.filter(function (s) { return freshOf(s.name).stale == null; }).length;   // 后端不判断的不算新
    var btn = document.getElementById('fresh');
    btn.className = 'fresh' + (push.full ? ' t-' + (old ? 'warn' : unknown ? 'muted' : 'ok') : '');
    document.getElementById('fresh-text').textContent = !push.full ? '等数据'
      : old ? old + ' 份旧了' : unknown ? unknown + ' 份未确认' : '数据都新';
  }

  function updateBadges() {
    var att = have('attention');
    var open = att ? att.open : 0;
    var a = document.querySelector('#nav a[data-page="now"]');
    var b = a.querySelector('.nav-badge');
    if (!open) { if (b) b.remove(); return; }
    if (!b) { b = h('span.nav-badge'); a.appendChild(b); }
    b.textContent = String(open);
    b.title = open + ' 条等你过目';
  }

  function afterData() {
    if (!booted) return;
    refreshMeta();
    updateBadges();
  }

  /* ================= 路由与筛选 ================= */
  /* 地址：#页/块?k=v；side、range、type 是筛选，其余是路由参数 */

  var FILTERS = {
    side: { def: 'all', options: [['all', '全部'], ['claude', 'Claude'], ['codex', 'Codex']] },
    range: { def: '7d', options: [['24h', '近 24 小时'], ['7d', '近 7 天'], ['30d', '近 30 天']] },
    type: { multi: true }
  };
  function typeKeys() { return Object.keys(Dash.palette.feed); }
  function isDefault(k, v) { return k === 'type' ? !v.length : v === FILTERS[k].def; }
  function filterParams(f) {
    var out = {};
    Object.keys(FILTERS).forEach(function (k) { if (!isDefault(k, f[k])) out[k] = f[k]; });
    return out;
  }

  function parseHash() {
    var raw = (location.hash || '').replace(/^#/, ''), q = raw.indexOf('?');
    var path = (q < 0 ? raw : raw.slice(0, q)).split('/').map(decodeURIComponent);
    var params = {}, filters = { side: FILTERS.side.def, range: FILTERS.range.def, type: [] };   // type 空数组即全选
    new URLSearchParams(q < 0 ? '' : raw.slice(q + 1)).forEach(function (v, k) {
      if (k === 'type') filters.type = v ? v.split(',') : [];
      else if (k in FILTERS) filters[k] = v;
      else params[k] = v;
    });
    return { page: (pageOf(path[0]) || M.pages[0]).id, block: path[1] || null, params: params, filters: filters };
  }

  function hashOf(page, params, block) {
    var q = new URLSearchParams();
    Object.keys(params || {}).forEach(function (k) {
      var v = params[k];
      if (v == null || v === '') return;
      q.set(k, Array.isArray(v) ? v.join(',') : String(v));
    });
    var qs = q.toString();
    return '#' + encodeURIComponent(page) + (block ? '/' + encodeURIComponent(block) : '') + (qs ? '?' + qs : '');
  }

  /* 同页换参数时保留地址里已有的筛选（params 里写了筛选键的以它为准）；换页不带筛选 */
  Dash.go = function (page, params, block) {
    var all = page === view.page ? Object.assign(filterParams(view.filters), params || {}) : params;
    location.hash = hashOf(page, all, block);
  };
  Dash.openDoc = function (code, o) { o = o || {}; Dash.go('docs', { code: code, sec: o.sec, find: o.find }); };

  /* 改一个筛选：保留这页的其他筛选与路由参数；默认值不写进地址；改时间范围时去掉拖选出的 from、to */
  Dash.setFilter = function (key, value) {
    var f = Object.assign({}, view.filters);
    f[key] = key === 'type' && value.length === typeKeys().length ? [] : value;
    var params = Object.assign({}, view.params);
    if (key === 'range') { delete params.from; delete params.to; }
    location.hash = hashOf(view.page, Object.assign(params, filterParams(f)), null);
  };

  function filterBar(pg) {
    var bar = document.querySelector('#page-' + pg.id + ' .page-filters');
    bar.replaceChildren.apply(bar, (pg.filters || []).map(function (k) {
      var f = FILTERS[k];
      var opts = k === 'type' ? typeKeys().map(function (t) { return { key: t, label: Dash.label('feed', t), tone: ['feed', t] }; })
        : f.options.map(function (o) { return { key: o[0], label: o[1] }; });
      var v = k === 'type' && !view.filters.type.length ? typeKeys() : view.filters[k];
      return Dash.chips(opts, v, function (nv) { Dash.setFilter(k, nv); }, { multi: !!f.multi });
    }));
  }

  function same(a, b) { return JSON.stringify(a) === JSON.stringify(b); }

  function route() {
    var r = parseHash(), prev = view;
    view = r;
    var pageChanged = prev.page !== r.page;
    M.pages.forEach(function (p) {
      document.getElementById('page-' + p.id).classList.toggle('is-active', p.id === r.page);
    });
    Array.prototype.forEach.call(document.querySelectorAll('#nav a'), function (a) {
      if (a.dataset.page === r.page) a.setAttribute('aria-current', 'page'); else a.removeAttribute('aria-current');
    });
    var pg = pageOf(r.page);
    document.title = pg.title + ' · 作者观察台';
    filterBar(pg);
    /* 与每块上次实际画出时的参数比（不与上一个路由比）：离开又回来的页，筛选和参数也对得上地址 */
    Object.keys(panels).forEach(function (id) {
      var p = panels[id];
      if (p.page !== r.page || !p.drawn) return;
      var hit = p.uses.some(function (u) {
        return u === 'params' ? !same(p.drawn.params, r.params) : !same(p.drawn.filters[u], r.filters[u]);
      });
      if (hit) schedule(p);
    });
    if (pageChanged) {
      window.scrollTo(0, 0);
      Dash.drawer.close();
      if (!flushTimer) flushTimer = setTimeout(flush, 0);
    }
    sync();
    if (r.block && (pageChanged || r.block !== prev.block)) {
      var jump = function () {
        var box = document.querySelector('#page-' + r.page + ' [data-panel="' + r.block + '"]');
        if (box) box.scrollIntoView({ block: 'start' });
      };
      setTimeout(jump, 60);
      setTimeout(jump, 500);   // 刚打开页面时面板还没画完
    }
  }

  /* ================= 启动 ================= */

  function loadAsset(kind, url) {
    var el = kind === 'css' ? h('link', { rel: 'stylesheet', href: url }) : h('script', { src: url });
    el.onerror = function () { console.warn('[Dash] 加载失败：' + url); };
    document.head.appendChild(el);
  }

  function boot() {
    var nav = document.getElementById('nav'), main = document.getElementById('main');
    M.pages.forEach(function (p) {
      nav.appendChild(h('a', { href: '#' + p.id, dataset: { page: p.id }, title: p.ask }, h('span.nav-title', p.title)));
      main.appendChild(h('section.page', { id: 'page-' + p.id },
        h('header.page-head', h('div.page-kicker', p.title), h('h1.page-ask', p.ask), h('div.page-filters')),
        h('div.page-body')));
    });
    document.getElementById('fresh').addEventListener('click', function () { Dash.go('health', null, 'health-fresh'); });

    booted = true;
    var stamp = '?t=' + Date.now();
    M.pages.forEach(function (p) {
      loadAsset('css', 'app/' + p.file.replace(/\.js$/, '.css') + stamp);
      loadAsset('js', 'app/' + p.file + stamp);
    });
    Object.keys(panels).forEach(function (id) { mount(panels[id]); });

    window.addEventListener('hashchange', route);
    route();
    updateHeader();
    connect();
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot);
  else boot();
})();
