/* 观察台运行时：数据按需加载（版本索引）、页面路由、面板注册与渲染、页眉与导航角标、写回（"知道了"、留言）。
   通用格式化与小组件在 ui.js，色表在 palette.js，图在 charts*.js；接口见 dashboard/README.md §3。
   面板只经 Dash.* 读数据，不直接拉文件。 */
(function () {
  'use strict';

  var M = window.DashManifest || { pages: [], data: [] };
  var DEMO = /[?&]demo(?:[=&]|$)/.test(location.search);
  var Dash = window.Dash = { demo: DEMO };

  var store = {};      // 数据名 -> 数据
  var meta = {};       // 数据名 -> { sig, tried, loading, at, ver, reqVer, got, fail }；got 本次请求的文件执行了 Dash.put，fail 最近一次读取失败的原因（fetch 下载失败 / run 执行出错 / null）
  var panels = {};     // 面板 id -> 面板记录
  var srcs = {};       // 数据名 -> manifest 条目
  var dirty = {};      // 待重绘的面板 id
  var flushTimer = 0;
  var booted = false;
  var view = { page: null };
  var index = { state: 'wait', versions: null };   // wait | ready | missing | stale

  M.data.forEach(function (s) { srcs[s.name] = s; });
  Dash.internal = { store: store, srcs: srcs, indexState: function () { return index.state; } };

  function h() { return Dash.h.apply(null, arguments); }
  function pageOf(id) { return M.pages.filter(function (p) { return p.id === id; })[0] || null; }

  /* ================= 数据 ================= */

  function signature(data) {
    try {
      return JSON.stringify(data, function (k, v) { return (this === data && (k === 'updated' || k === 'source')) ? undefined : v; });
    } catch (e) { return String(Math.random()); }
  }

  var appStamp = null;   // 界面代码的时刻（live 带来）：变了就整页刷新，开着的页面不跑旧代码
  Dash.put = function (name, data) {
    if (name === 'live' && data && data.appStamp) {
      if (appStamp === null) appStamp = data.appStamp;
      else if (data.appStamp !== appStamp) { location.reload(); return; }
    }
    var m = meta[name] || (meta[name] = {});
    var sig = signature(data);
    var changed = m.sig !== sig;
    store[name] = data;
    m.sig = sig; m.tried = true;
    if (name === 'index') readIndex(data);
    if (changed) dependents(name).forEach(schedule);
    m.got = true;   // 走到这里才算交付（上面任何一步抛错，loadData 都不会记新版本）
    afterData();
  };

  /* 当前数据；最近一次读取失败的数据不给（旧缓存不冒充新数据），原因看 dataState */
  function have(name) {
    var m = meta[name];
    return m && m.fail ? null : (store[name] || null);
  }

  /* 索引就绪：有 versions、且没旧；否则其余数据按 every 定时拉 */
  function readIndex(data) {
    var ok = data && data.versions && typeof data.versions === 'object';
    index.versions = ok ? data.versions : null;
    index.state = !ok ? 'missing' : (!DEMO && Dash.stale(data, srcs.index.stale) ? 'stale' : 'ready');
    sync();
  }

  function srcPath(s) { return DEMO ? 'data/demo/' + s.name + '.js' : s.path; }

  function loadData(s, ver) {
    var m = meta[s.name] || (meta[s.name] = {});
    if (m.loading && Date.now() - m.loading < 15000) return;
    m.loading = Date.now();
    m.at = Date.now();
    m.reqVer = ver == null ? null : ver;
    m.got = false;
    Array.prototype.forEach.call(document.querySelectorAll('script[data-dash="' + s.name + '"]'), function (e) { e.remove(); });
    var el = document.createElement('script');
    el.dataset.dash = s.name;
    el.src = srcPath(s) + '?t=' + Date.now();
    /* 只有这次请求的文件真的执行了 Dash.put 才算交付：版本在此时才记，失败不推进版本 */
    function done(ok) {
      m.loading = 0;
      var delivered = ok && m.got, before = m.fail || null;
      if (delivered) { m.ver = m.reqVer; m.fail = null; }
      else {
        m.fail = ok ? 'run' : 'fetch';
        console.warn('[Dash] 数据 ' + s.name + (ok ? ' 的文件执行出错或没有调用 Dash.put：' : ' 下载失败：') + srcPath(s));
      }
      if (!m.tried) m.tried = true;
      if (!delivered || before) dependents(s.name).forEach(schedule);   // 第一次读完仍没有、读取失败、从失败恢复：重画面板
      el.remove();
      if (s.name === 'index' && !delivered) { index.state = 'missing'; index.versions = null; sync(); }
      afterData();
    }
    el.onload = function () { done(true); };
    el.onerror = function () { done(false); };
    document.head.appendChild(el);
  }

  /* 这份数据此刻要不要：页眉导航要用的、当前页面板 needs 的 */
  function wanted(name) {
    var s = srcs[name];
    if (!s) return false;
    if (s.poll || s.keep) return true;
    return Object.keys(panels).some(function (id) {
      var p = panels[id];
      return p.page === view.page && p.needs.indexOf(name) >= 0;
    });
  }

  /* 数据的拉取方式：poll（定时）、version（按版本）、timer（索引没就绪或没列这份，按 every 定时） */
  function modeOf(s) {
    if (s.poll) return 'poll';
    return index.state === 'ready' && index.versions && s.name in index.versions ? 'version' : 'timer';
  }

  function sync() {
    if (!booted || index.state === 'wait') return;   // 先等索引第一次读完，免得同一份数据开头拉两遍
    M.data.forEach(function (s) {
      if (s.poll || !wanted(s.name)) return;
      var m = meta[s.name] || {};
      if (modeOf(s) === 'version') {
        var v = index.versions[s.name];
        if (m.ver !== v && !m.loading && !(m.fail && Date.now() - m.at < 15000)) loadData(s, v);   // 读取失败后隔 15 s 再试
      } else if (!m.at || Date.now() - m.at >= Math.max(5, s.every || 60) * 1000) {
        loadData(s, null);
      }
    });
    updateCorner();
  }

  function tick() {
    if (document.hidden) return;
    M.data.forEach(function (s) {
      if (!s.poll) return;
      var m = meta[s.name] || {};
      if (!m.at || Date.now() - m.at >= Math.max(5, s.every || 15) * 1000 - 500) loadData(s, null);
    });
    if (index.state === 'ready' && store.index && Dash.stale(store.index, srcs.index.stale) && !DEMO) index.state = 'stale';
    sync();
  }

  /* 数据状态，给面板说清"没有"的原因：wait 读取中 / missing 读过但没有这份文件 / error 读取失败（文件执行出错，或已有数据后重读失败）/ ok */
  Dash.dataState = function (name) {
    var m = meta[name];
    if (m && m.fail && (m.fail === 'run' || store[name])) return 'error';
    if (store[name]) return 'ok';
    return m && m.tried ? 'missing' : 'wait';
  };

  /* ================= 写回：本地服务 serve.py ================= */

  /* 把一次 POST 的失败说成人话：没由本地服务打开、服务没在跑、服务不认这个操作、服务给的原因 */
  function post(url, body) {
    if (!/^https?:$/.test(location.protocol)) return Promise.reject(new Error('页面是直接打开的文件，没经过观察台本地服务；双击"打开观察台"再试'));
    return fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
      .catch(function () { throw new Error('观察台本地服务没在运行，没有送出；双击"打开观察台"再试'); })
      .then(function (r) {
        return r.json().catch(function () { return null; }).then(function (j) {
          if (!j) throw new Error('本地服务不认这个操作（返回 ' + r.status + '），要先换成新版本地服务');
          if (!r.ok || !j || j.ok !== true) throw new Error((j && j.error) || '本地服务返回 ' + r.status);
          return j;
        });
      });
  }

  /* "知道了"：POST /api/attention/ack {id} → {ok: true}；成功后马上重读 attention 与索引。失败时 Promise 带原因，面板写出来；不存浏览器 */
  Dash.ack = function (id) {
    if (DEMO) return Promise.reject(new Error('演示数据，不写回'));
    return post('/api/attention/ack', { id: id })
      .then(function (j) { loadData(srcs.attention, null); loadData(srcs.index, null); return j; });
  };

  /* 给主会话留言：POST /api/feedback {text} → {ok: true, at}；成功后重读 feedback 与索引 */
  Dash.feedback = function (text) {
    if (DEMO) return Promise.reject(new Error('演示数据，留言没有发出'));
    return post('/api/feedback', { text: text })
      .then(function (j) { loadData(srcs.feedback, null); loadData(srcs.index, null); return j; });
  };

  /* ================= 面板 ================= */

  function dependents(name) {
    return Object.keys(panels).map(function (id) { return panels[id]; })
      .filter(function (p) { return p.needs.indexOf(name) >= 0; });
  }

  /* Dash.panel({ id, page, title?, note?, span?, order?, needs?, partial?, fixed?(el), render(el, data) })
     needs 打开这页就拉、到齐才画（partial 为真时到一份就画）；任一份变了重绘。
     fixed(el) 只在挂上时画一次、之后不随数据重画的部分（输入框），在数据内容上面。
     span 占 12 栏里的几栏：12（默认）、8、7、6、5、4（style.css 定义的这几种）；note 是标题下一行"这块回答什么" */
  Dash.panel = function (def) {
    if (!def || !def.id) return;
    var p = panels[def.id] = Object.assign({ needs: [], order: 100, span: 12 }, def);
    if (booted) { mount(p); sync(); }
  };

  function mount(p) {
    var page = document.getElementById('page-' + p.page);
    var host = page && page.querySelector('.page-body');
    if (!host) { console.warn('[Dash] 面板 ' + p.id + ' 的页面 ' + p.page + ' 不在 manifest 里'); return; }
    var box = h('section.panel.card.span-' + p.span, { dataset: { panel: p.id, order: p.order } });
    p.metaEl = h('span.card-meta');
    if (p.title) {
      box.appendChild(h('header.card-head', h('div.card-titles', h('h2.card-title', p.title), p.note ? h('p.card-note', p.note) : null), p.metaEl));
    }
    if (p.fixed) {
      var fixedEl = h('div.card-fixed');
      box.appendChild(fixedEl);
      p.fixed(fixedEl);
    }
    p.bodyEl = h('div.card-body');
    box.appendChild(p.bodyEl);
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

  function labelOf(n) { return srcs[n] ? srcs[n].label : n; }

  function renderPanel(p) {
    var data = {}, got = 0, missing = [], waiting = false;
    p.needs.forEach(function (n) {
      data[n] = have(n);
      if (data[n]) got++;
      else {
        missing.push(n);
        if (!(meta[n] && meta[n].tried)) waiting = true;
      }
    });
    var failed = p.needs.filter(function (n) { return Dash.dataState(n) === 'error'; });
    var failText = failed.map(labelOf).join('、') + '这次没读到，先不显示旧内容，会自动重试';
    var ready = p.needs.length === 0 || (p.partial ? got > 0 : missing.length === 0);
    var body = p.bodyEl;
    if (Dash.charts) Dash.charts.release(body);
    p.el.classList.remove('is-error', 'is-loading');
    if (!ready) {
      if (waiting) { p.el.classList.add('is-loading'); body.replaceChildren(h('div.skeleton')); return; }
      body.replaceChildren(failed.length ? Dash.empty('没读到', failText, true)
        : Dash.empty('还没有数据', missing.map(labelOf).join('、') + '还没有数据，后台采集写出之后会自动出现', true));
      return;
    }
    var opens = Array.prototype.map.call(body.querySelectorAll('details'), function (d) { return d.open; });
    var inner = h('div.panel-inner');
    body.replaceChildren(inner);
    if (failed.length) inner.appendChild(Dash.status(failText, 'risk'));
    try {
      p.render(inner, data);
    } catch (e) {
      console.error('[Dash] 面板 ' + p.id + ' 出错', e);
      p.el.classList.add('is-error');
      body.replaceChildren(Dash.empty('这块出错了', String(e && e.message || e), true));
      return;
    }
    var ds = inner.querySelectorAll('details');
    if (ds.length === opens.length) Array.prototype.forEach.call(ds, function (d, i) { if (opens[i]) d.open = true; });
  }

  /* 重画一个面板（本地交互改了显示，如"知道了"正在写回）；全部重画给主题切换用（图要按新色重建） */
  Dash.redraw = function (id) { schedule(panels[id]); };
  Dash.redrawAll = function () { Object.keys(panels).forEach(function (id) { schedule(panels[id]); }); };

  /* 卡片右上角的"更新于"、数据旧了变灰 */
  function refreshMeta() {
    Object.keys(panels).forEach(function (id) {
      var p = panels[id];
      if (!p.el || p.page !== view.page) return;
      var oldest = NaN, stale = false;
      p.needs.forEach(function (n) {
        if (!have(n)) return;
        var t = Dash.parseTime(store[n].updated);
        if (!isNaN(t) && (isNaN(oldest) || t < oldest)) oldest = t;
        if (Dash.isStale(n)) stale = true;
      });
      p.el.classList.toggle('is-stale', stale);
      p.metaEl.textContent = stale ? '数据旧了 · ' + Dash.at(oldest) : (isNaN(oldest) ? '' : '更新 ' + Dash.at(oldest));
    });
    updateHeader();
  }

  /* ================= 页眉、导航、角标 ================= */

  function headlineText() {
    var g = have('progress'), parts = [];
    if (g && g.counts) {
      parts.push('在做 ' + Dash.num(g.counts.doing) + ' 行');
      parts.push('收尾 ' + Dash.num(g.counts.finishing) + ' 行');
    }
    if (g && g.done) parts.push('今天完成 ' + Dash.num(g.done.today) + ' 行');
    return parts.length ? parts.join(' · ') : '等数据';
  }

  /* 两位指挥在不在跑：live.leads [{ who, running }] */
  function updateLeads() {
    var box = document.getElementById('leads');
    if (!box) return;
    var l = have('live');
    box.replaceChildren.apply(box, (l && l.leads || []).map(function (x) {
      var side = Dash.sideKey(x.who);
      return h('span.lead-pill.t-' + side, { title: x.who + (x.running ? ' 在跑' : ' 空闲') },
        h('span.dot.' + (x.running ? 'is-on' : 'is-off')), x.who + (x.running ? ' 在跑' : ' 空闲'));
    }));
  }

  function updateHeader() {
    var hl = document.getElementById('headline');
    if (hl) { hl.textContent = headlineText(); hl.title = hl.textContent; }
    updateLeads();
    var dot = document.getElementById('fresh-dot'), txt = document.getElementById('fresh-text');
    if (!dot || !txt) return;
    var l = have('live'), stale = Dash.isStale('live'), bad = Dash.dataState('live') === 'error';
    dot.className = 'dot t-' + (bad ? 'risk' : !l ? 'muted' : stale ? 'warn' : 'ok');
    txt.textContent = DEMO ? '演示数据' : bad ? '实时数据没读到' : !l ? '等数据' : (stale ? '实时数据旧了 · ' : '实时 ') + Dash.at(l.updated);
    var pop = document.getElementById('fresh-pop');
    if (pop && !pop.hidden) fillPop(pop);
  }

  function fillPop(pop) {
    var rows = M.data.map(function (s) {
      var d = have(s.name), m = meta[s.name] || {}, tone, text;
      if (Dash.dataState(s.name) === 'error') { tone = 'risk'; text = '没读到'; }
      else if (!d) { tone = 'muted'; text = m.tried ? '还没有' : (wanted(s.name) ? '读取中' : '这页用不到'); }
      else if (Dash.isStale(s.name)) { tone = 'warn'; text = '旧了 · ' + Dash.at(d.updated); }
      else { tone = 'ok'; text = Dash.at(d.updated); }
      return h('div.pop-row.t-' + tone, h('span.dot'), h('span.grow', s.label), h('span.ago', text));
    });
    pop.replaceChildren(h('div.pop-title', DEMO ? '正在看演示数据' : '各份数据的更新时刻'), rows);
  }

  function updateCorner() {
    var c = document.getElementById('corner');
    if (!c) return;
    var text = index.state === 'missing' ? '后台的数据索引还没就绪，页面先按固定间隔刷新'
      : index.state === 'stale' ? '后台的数据索引停在 ' + Dash.at(store.index && store.index.updated) + '，页面先按固定间隔刷新' : '';
    c.hidden = !text;
    c.textContent = text;
  }

  /* 导航角标：首页是还没点"知道了"的过目条数 */
  function updateBadges() {
    var att = have('attention') && store.attention.items || [];
    var open = att.filter(function (x) { return !x.acked; }).length;
    var a = document.querySelector('#nav a[data-page="home"]');
    if (!a) return;
    var b = a.querySelector('.nav-badge');
    if (!open) { if (b) b.remove(); return; }
    if (!b) { b = h('span.nav-badge'); a.appendChild(b); }
    b.textContent = String(open);
    b.title = open + ' 条等你过目';
  }

  function afterData() {
    refreshMeta();
    updateBadges();
    updateCorner();
  }

  /* ================= 路由 ================= */
  /* #页 、#页/块（块 id 就是面板 id） */

  function route() {
    var parts = (location.hash || '').replace(/^#/, '').split('/').map(decodeURIComponent);
    var pg = pageOf(parts[0]) || M.pages[0];
    var block = parts[1];
    var changed = view.page !== pg.id;
    view.page = pg.id;
    M.pages.forEach(function (p) {
      var sec = document.getElementById('page-' + p.id);
      if (sec) sec.classList.toggle('is-active', p.id === pg.id);
    });
    Array.prototype.forEach.call(document.querySelectorAll('#nav a'), function (a) {
      if (a.dataset.page === pg.id) a.setAttribute('aria-current', 'page'); else a.removeAttribute('aria-current');
    });
    document.title = pg.title + ' · 作者观察台';
    if (changed) window.scrollTo(0, 0);
    if (!flushTimer) flushTimer = setTimeout(flush, 0);
    sync();
    if (block) {
      var jump = function () {
        var box = document.querySelector('#page-' + pg.id + ' [data-panel="' + block + '"]');
        if (box) box.scrollIntoView({ block: 'start' });
      };
      setTimeout(jump, 60);
      setTimeout(jump, 500);   // 刚打开页面时面板还没画完
    }
  }

  /* ================= 启动 ================= */

  function loadAsset(kind, url, onerror) {
    var el = kind === 'css' ? h('link', { rel: 'stylesheet', href: url }) : h('script', { src: url });
    el.onerror = onerror || function () { console.warn('[Dash] 加载失败：' + url); };
    document.head.appendChild(el);
  }

  function pageSection(p) {
    return h('section.page', { id: 'page-' + p.id },
      h('header.page-head', h('div.page-kicker', p.title), h('h1.page-ask', p.ask || p.title)),
      h('div.page-body'));
  }

  function boot() {
    var nav = document.getElementById('nav'), main = document.getElementById('main');
    M.pages.forEach(function (p) {
      nav.appendChild(h('a', { href: '#' + p.id, dataset: { page: p.id }, title: p.ask || '' }, h('span.nav-title', p.title)));
      main.appendChild(pageSection(p));
    });

    var btn = document.getElementById('fresh'), pop = document.getElementById('fresh-pop');
    btn.addEventListener('click', function (e) {
      e.stopPropagation();
      pop.hidden = !pop.hidden;
      btn.setAttribute('aria-expanded', String(!pop.hidden));
      if (!pop.hidden) fillPop(pop);
    });
    document.addEventListener('click', function (e) {
      if (!pop.hidden && !pop.contains(e.target)) { pop.hidden = true; btn.setAttribute('aria-expanded', 'false'); }
    });

    booted = true;
    var stamp = '?t=' + Date.now();
    M.pages.forEach(function (p) {
      loadAsset('css', 'app/' + p.file.replace(/\.js$/, '.css') + stamp);
      loadAsset('js', 'app/' + p.file + stamp, function () { console.warn('[Dash] 页面文件没找到：' + p.file); });
    });
    Object.keys(panels).forEach(function (id) { mount(panels[id]); });

    window.addEventListener('hashchange', route);
    route();
    tick();
    setInterval(tick, 5000);
    setInterval(refreshMeta, 20000);
    document.addEventListener('visibilitychange', function () { if (!document.hidden) tick(); });
    updateHeader();
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot);
  else boot();
})();
