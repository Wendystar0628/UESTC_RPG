/* 观察台运行时：数据注册与定时重载、页面路由、面板注册与渲染；通用格式化与小组件在 ui.js。
   接口见 dashboard/README.md §3；面板只经 Dash.* 读数据，不直接碰 DOM 之外的全局。 */
(function () {
  'use strict';

  var M = window.DashManifest || { pages: [], panels: [], data: [] };
  var DEMO = /[?&]demo(?:[=&]|$)/.test(location.search);
  var Dash = window.Dash = { demo: DEMO };

  var store = {};      // 数据名 -> 数据
  var meta = {};       // 数据名 -> { sig, tried, missing, loading }
  var panels = {};     // 面板 id -> 面板记录
  var srcs = {};       // 数据名 -> manifest 条目
  var dirty = {};      // 待重绘的面板 id
  var flushTimer = 0;
  var booted = false;
  var currentPage = null;

  M.data.forEach(function (s) { srcs[s.name] = s; });
  Dash.internal = { store: store, srcs: srcs };

  // 通用组件在 ui.js（晚于本文件加载），这里只在运行时取用
  function h() { return Dash.h.apply(null, arguments); }
  function parseTime(iso) { return Dash.parseTime(iso); }

  /* ================= 数据 ================= */

  function signature(data) {
    try {
      return JSON.stringify(data, function (k, v) { return (this === data && (k === 'updated' || k === 'source')) ? undefined : v; });
    } catch (e) { return String(Math.random()); }
  }

  Dash.put = function (name, data) {
    var m = meta[name] || (meta[name] = {});
    var sig = signature(data);
    var changed = m.sig !== sig;
    store[name] = data;
    m.sig = sig; m.missing = false; m.tried = true;
    if (changed) {
      dependents(name).forEach(schedule);
      afterData();
    } else {
      refreshMeta();
    }
  };

  Dash.get = function (name) { return store[name] || null; };

  Dash.touch = function (names) {
    var set = {};
    names.forEach(function (n) { dependents(n).forEach(function (p) { set[p.id] = p; }); });
    Object.keys(set).forEach(function (id) { schedule(set[id]); });
    updateBadges();
  };

  function srcPath(s) { return DEMO ? 'data/demo/' + s.name + '.js' : s.path; }

  function loadData(s) {
    var m = meta[s.name] || (meta[s.name] = {});
    if (m.loading && Date.now() - m.loading < 15000) return;
    m.loading = Date.now();
    var old = document.querySelectorAll('script[data-dash="' + s.name + '"]');
    Array.prototype.forEach.call(old, function (e) { e.remove(); });
    var el = document.createElement('script');
    el.dataset.dash = s.name;
    el.src = srcPath(s) + '?t=' + Date.now();
    function done(ok) {
      m.loading = 0;
      if (!m.tried) {
        m.tried = true;
        if (!store[s.name]) m.missing = true;
        dependents(s.name).forEach(schedule);   // 第一次读完仍没有：从"读取中"变成"等数据"
      }
      if (!ok && !store[s.name]) m.missing = true;
      el.remove();
      afterData();
    }
    el.onload = function () { done(true); };
    el.onerror = function () { done(false); };
    document.head.appendChild(el);
  }

  function startData() {
    M.data.forEach(function (s) {
      loadData(s);
      setInterval(function () { if (!document.hidden) loadData(s); }, Math.max(5, s.every || 30) * 1000);
    });
    document.addEventListener('visibilitychange', function () {
      if (!document.hidden) M.data.forEach(loadData);
    });
  }

  /* ================= 面板 ================= */

  function dependents(name) {
    return Object.keys(panels).map(function (id) { return panels[id]; })
      .filter(function (p) { return p.needs.indexOf(name) >= 0; });
  }

  Dash.panel = function (def) {
    if (!def || !def.id) return;
    var p = panels[def.id] = Object.assign({ needs: [], order: 100 }, def);
    p.needs = p.needs || [];
    if (booted) mount(p);
  };

  function mount(p) {
    var page = document.getElementById('page-' + p.page);
    if (!page) { console.warn('[Dash] 面板 ' + p.id + ' 的页面 ' + p.page + ' 不在 manifest 里'); return; }
    var host = page.querySelector('.page-body');
    var box = h('div.panel' + (p.card === false ? '' : '.card'), { dataset: { panel: p.id, order: p.order } });
    if (p.card !== false) {
      p.metaEl = h('span.card-meta');
      if (p.title) box.appendChild(h('div.card-head', h('span.card-title', p.title), p.metaEl));
      p.bodyEl = h('div.card-body');
      box.appendChild(p.bodyEl);
    } else {
      p.bodyEl = box;
    }
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

  function flush() {
    flushTimer = 0;
    var list = Object.keys(dirty).map(function (id) { return dirty[id]; });
    dirty = {};
    list.forEach(renderPanel);
    refreshMeta();
  }

  function renderPanel(p) {
    var data = {}, have = 0, missing = [], waiting = false;
    p.needs.forEach(function (n) {
      data[n] = store[n] || null;
      if (store[n]) have++;
      else {
        missing.push(n);
        if (!(meta[n] && meta[n].tried)) waiting = true;
      }
    });
    var ready = p.needs.length === 0 || (p.partial ? have > 0 : missing.length === 0);
    var body = p.bodyEl;
    p.el.classList.remove('is-error', 'is-loading');
    if (!ready) {
      if (waiting) { p.el.classList.add('is-loading'); body.replaceChildren(); return; }
      var names = missing.map(function (n) { return (srcs[n] && srcs[n].label) || n; }).join('、');
      body.replaceChildren(Dash.empty('等数据', names + ' 还没有数据，采集脚本跑过之后会自动出现'));
      return;
    }
    var opens = Array.prototype.map.call(body.querySelectorAll('details'), function (d) { return d.open; });
    var inner = h('div.panel-inner');
    body.replaceChildren(inner);
    try {
      p.render(inner, data);
    } catch (e) {
      console.error('[Dash] 面板 ' + p.id + ' 出错', e);
      p.el.classList.add('is-error');
      body.replaceChildren(Dash.empty('这块出错了', String(e && e.message || e)));
      return;
    }
    var ds = inner.querySelectorAll('details');
    if (ds.length === opens.length) Array.prototype.forEach.call(ds, function (d, i) { if (opens[i]) d.open = true; });
  }

  /* 卡片右上角的"更新于"、数据旧了变灰 */
  function refreshMeta() {
    Object.keys(panels).forEach(function (id) {
      var p = panels[id];
      if (!p.el) return;
      var oldest = NaN, stale = false;
      p.needs.forEach(function (n) {
        var s = srcs[n];
        if (!store[n] || !s || s.stale == null) return;
        var t = parseTime(store[n].updated);
        if (!isNaN(t) && (isNaN(oldest) || t < oldest)) oldest = t;
        if (Dash.isStale(n)) stale = true;
      });
      if (p.card !== false) {
        p.el.classList.toggle('is-stale', stale);
        if (p.metaEl) {
          p.metaEl.textContent = stale ? '数据旧了 · 更新于 ' + Dash.at(oldest) : (isNaN(oldest) ? '' : '更新 ' + Dash.at(oldest));
        }
      }
    });
    updateHeader();
  }

  /* ================= 页眉与导航 ================= */

  function headlineText() {
    var q = store['queue'], l = store['live'], parts = [];
    if (q && q.counts && q.counts['进行中']) parts.push('施工 ' + q.counts['进行中'] + ' 项在做');
    if (l) {
      var running = (l.sessions || []).filter(function (s) { return s.state === 'running'; }).length;
      parts.push(running ? running + ' 个会话在跑' : '没有会话在跑');
    }
    if (q && q.stage) parts.push('阶段：' + q.stage);
    return parts.length ? parts.join(' · ') : '等数据';
  }

  function updateHeader() {
    var hl = document.getElementById('headline');
    if (hl) hl.textContent = headlineText();
    var dot = document.getElementById('fresh-dot'), txt = document.getElementById('fresh-text');
    if (!dot || !txt) return;
    var newest = NaN, anyStale = false, any = false;
    M.data.forEach(function (s) {
      var d = store[s.name];
      if (!d) return;
      any = true;
      if (s.stale == null) return;
      var t = parseTime(d.updated);
      if (!isNaN(t) && (isNaN(newest) || t > newest)) newest = t;
      if (Dash.isStale(s.name)) anyStale = true;
    });
    var tone = !any ? 'muted' : (anyStale ? 'warn' : 'ok');
    dot.className = 'dot t-' + tone;
    txt.textContent = !any ? '等数据' : (anyStale ? '部分数据旧了' : (isNaN(newest) ? '数据已就绪' : '数据 ' + Dash.at(newest)));
    if (DEMO) txt.textContent = '演示数据';
    var pop = document.getElementById('fresh-pop');
    if (pop && !pop.hidden) fillPop(pop);
  }

  function fillPop(pop) {
    var rows = M.data.map(function (s) {
      var d = store[s.name], tone, text;
      if (!d) { tone = 'muted'; text = meta[s.name] && meta[s.name].tried ? '没有数据' : '读取中'; }
      else if (s.stale == null) { tone = 'ok'; text = Dash.at(d.updated) + ' 改动'; }
      else if (Dash.isStale(s.name)) { tone = 'warn'; text = '旧了 · ' + Dash.at(d.updated); }
      else { tone = 'ok'; text = Dash.at(d.updated); }
      return h('div.pop-row.t-' + tone, h('span.dot'), h('span.grow', s.label || s.name), h('span.ago', text));
    });
    pop.replaceChildren(h('div.pop-title', DEMO ? '正在看演示数据（?demo）' : '数据新鲜度'), rows);
  }

  function updateBadges() {
    var nav = document.getElementById('nav');
    if (!nav) return;
    var att = Dash.attention();
    var needN = att.open.filter(function (x) { return x.need; }).length;
    var notice = ((store['notices'] && store['notices'].items) || []).filter(function (it) {
      return it.level === 'attention' && !Dash.expired(it) && !Dash.seen('notice:' + it.id);
    }).length;
    Dash.badge('audit', att.open.length ? String(att.open.length) : '', needN ? 'risk' : 'muted');
    Dash.badge('notices', notice ? String(notice) : '', 'warn');
  }

  Dash.badge = function (pageId, text, tone) {
    var a = document.querySelector('#nav a[data-page="' + pageId + '"]');
    if (!a) return;
    var b = a.querySelector('.nav-badge');
    if (!text) { if (b) b.remove(); return; }
    if (!b) { b = h('span.nav-badge'); a.appendChild(b); }
    b.className = 'nav-badge t-' + (tone || 'muted');
    b.textContent = text;
  };

  function afterData() {
    refreshMeta();
    updateBadges();
  }

  /* ================= 路由 ================= */

  Dash.go = function (id) { location.hash = '#' + id; };
  Dash.page = function () { return currentPage; };

  function route() {
    var id = (location.hash || '').replace(/^#/, '');
    if (!M.pages.some(function (p) { return p.id === id; })) id = M.pages.length ? M.pages[0].id : '';
    currentPage = id;
    M.pages.forEach(function (p) {
      var sec = document.getElementById('page-' + p.id);
      if (sec) sec.classList.toggle('is-active', p.id === id);
    });
    Array.prototype.forEach.call(document.querySelectorAll('#nav a'), function (a) {
      if (a.dataset.page === id) a.setAttribute('aria-current', 'page'); else a.removeAttribute('aria-current');
    });
    var pg = M.pages.filter(function (p) { return p.id === id; })[0];
    document.title = (pg ? pg.title + ' · ' : '') + '作者观察台';
    window.scrollTo(0, 0);
  }

  /* ================= 启动 ================= */

  function loadAsset(kind, url, onerror) {
    var el = kind === 'css' ? h('link', { rel: 'stylesheet', href: url }) : h('script', { src: url });
    el.onerror = onerror || function () { console.warn('[Dash] 加载失败：' + url); };
    document.head.appendChild(el);
  }

  function boot() {
    var nav = document.getElementById('nav'), main = document.getElementById('main');
    M.pages.forEach(function (p) {
      nav.appendChild(h('a', { href: '#' + p.id, dataset: { page: p.id } }, h('span', p.title)));
      main.appendChild(h('section.page', { id: 'page-' + p.id },
        h('div.page-head', h('h1', p.title), p.desc ? h('p', p.desc) : null),
        h('div.page-body')));
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

    window.addEventListener('hashchange', route);
    route();
    booted = true;

    var stamp = '?t=' + Date.now();
    M.panels.forEach(function (pm) {
      if (pm.css) loadAsset('css', 'app/' + pm.file.replace(/\.js$/, '.css') + stamp);
      loadAsset('js', 'app/' + pm.file + stamp, function () {
        console.warn('[Dash] 面板文件没找到：' + pm.file);
        if (panels[pm.id]) return;
        Dash.panel({
          id: pm.id, page: pm.page, title: '', order: 100, needs: [], card: true,
          render: function (el) { el.appendChild(Dash.empty('这块还没有写好', pm.file, true)); }
        });
      });
    });
    Object.keys(panels).forEach(function (id) { mount(panels[id]); });   // 理论上面板在 boot 之后才到，这里兜底

    startData();
    setInterval(refreshMeta, 20000);
    updateHeader();
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot);
  else boot();
})();
