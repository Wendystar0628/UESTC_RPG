/* 观察台运行时：数据按需加载（版本索引）、页面与子标签路由、面板注册与渲染、页内选中状态。
   通用格式化与小组件在 ui.js，色表在 palette.js，图组件在 charts*.js；接口见 dashboard/README.md。
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
  var picks = {};      // 页内选中状态：键 -> 值（Dash.select）
  var flushTimer = 0;
  var booted = false;
  var view = { page: null, tab: null };
  var index = { state: 'wait', versions: null };   // wait | ready | missing | stale

  M.data.forEach(function (s) { srcs[s.name] = s; });
  Dash.internal = { store: store, srcs: srcs, indexState: function () { return index.state; } };

  function h() { return Dash.h.apply(null, arguments); }
  function parseTime(iso) { return Dash.parseTime(iso); }
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
  Dash.get = have;

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

  /* 这份数据此刻要不要：页眉导航要用的、当前页面板 needs 的、当前页面板的 wants() 此刻点名的 */
  function wanted(name) {
    var s = srcs[name];
    if (!s) return false;
    if (s.poll || s.keep) return true;
    return Object.keys(panels).some(function (id) {
      var p = panels[id];
      return isActive(p) && (p.needs.indexOf(name) >= 0 || (p.wants && [].concat(p.wants()).indexOf(name) >= 0));
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

  /* "知道了"写回采集侧：本地服务 POST /api/attention/ack {id, revision?} → {ok: true}；成功后马上重读 attention 与索引。
     页面不是由本地服务打开的（file://）或服务没在跑时 Promise 失败，面板把原因写出来；不存浏览器。 */
  Dash.ack = function (id, revision) {
    if (DEMO) return Promise.reject(new Error('演示数据不写回'));
    if (!/^https?:$/.test(location.protocol)) return Promise.reject(new Error('页面不是由观察台本地服务打开的，写不回去'));
    var body = { id: id };
    if (revision != null) body.revision = revision;
    return fetch('/api/attention/ack', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
      .then(function (r) { return r.json().then(function (j) { if (!r.ok || !j || j.ok !== true) throw new Error((j && j.error) || '本地服务返回 ' + r.status); return j; }); })
      .then(function (j) { loadData(srcs.attention, null); loadData(srcs.index, null); return j; });
  };

  /* 数据状态，给面板说清"没有"的原因：wait 读取中 / missing 读过但没有这份文件 / error 读取失败（文件执行出错，或已有数据后重读失败）/ ok */
  Dash.dataState = function (name) {
    var m = meta[name];
    if (m && m.fail && (m.fail === 'run' || store[name])) return 'error';
    if (store[name]) return 'ok';
    return m && m.tried ? 'missing' : 'wait';
  };

  /* ================= 面板 ================= */

  function isActive(p) { return p.page === view.page && (!p.tab || p.tab === view.tab); }

  function dependents(name) {
    return Object.keys(panels).map(function (id) { return panels[id]; })
      .filter(function (p) { return p.needs.indexOf(name) >= 0 || p.uses.indexOf(name) >= 0; });
  }

  /* Dash.panel({ id, page, tab?, title?, note?, span?, order?, needs?, uses?, partial?, watch?, render(el, data) })
     needs 打开这页就拉、到齐才画（partial 为真时到一份就画）；uses 不主动拉、只在它变了时重绘；wants() 返回此刻要按需拉的数据名（取自 uses，例如选中了才拉），面板在当前页时每次同步都点名，切页回来照样按版本更新；
     span 占 12 栏里的几栏：12（默认）、7 或 5（style.css 只定义了这几种，要别的宽度先在那里加）；watch 是 Dash.select 的键，选中变了就重绘；note 是标题下一行小字 */
  Dash.panel = function (def) {
    if (!def || !def.id) return;
    var p = panels[def.id] = Object.assign({ needs: [], uses: [], watch: [], order: 100, span: 12 }, def);
    if (booted) { mount(p); sync(); }
  };

  function hostOf(p) {
    var page = document.getElementById('page-' + p.page);
    if (!page) return null;
    if (p.tab) return page.querySelector('.page-tab[data-tab="' + p.tab + '"] .page-body');
    return page.querySelector('.page-body');
  }

  function mount(p) {
    var host = hostOf(p);
    if (!host) { console.warn('[Dash] 面板 ' + p.id + ' 的页面 ' + p.page + (p.tab ? '/' + p.tab : '') + ' 不在 manifest 里'); return; }
    var box = h('section.panel.card.span-' + p.span, { dataset: { panel: p.id, order: p.order } });
    p.metaEl = h('span.card-meta');
    if (p.title) {
      box.appendChild(h('header.card-head', h('div.card-titles', h('h2.card-title', p.title), p.note ? h('p.card-note', p.note) : null), p.metaEl));
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
      if (!isActive(p)) return;
      delete dirty[id];
      renderPanel(p);
    });
    refreshMeta();
  }

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
    var failed = p.needs.concat(p.uses).filter(function (n) { return Dash.dataState(n) === 'error'; });
    function nameOf(n) { var s = srcs[n]; return s ? (s.label || n) + '（' + srcPath(s) + '）' : n; }
    var failText = failed.map(nameOf).join('、') + ' 读取失败，上一次读到的内容不再显示；会自动重试';
    var ready = p.needs.length === 0 || (p.partial ? got > 0 : missing.length === 0);
    var body = p.bodyEl;
    if (Dash.charts) Dash.charts.release(body);
    p.el.classList.remove('is-error', 'is-loading');
    if (!ready) {
      if (waiting) { p.el.classList.add('is-loading'); body.replaceChildren(h('div.skeleton')); return; }
      body.replaceChildren(failed.length ? Dash.empty('读取失败', failText, true)
        : Dash.empty('等数据', missing.map(nameOf).join('、') + ' 还没有数据，采集侧写出之后会自动出现', true));
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

  /* 卡片右上角的"更新于"、数据旧了变灰 */
  function refreshMeta() {
    Object.keys(panels).forEach(function (id) {
      var p = panels[id];
      if (!p.el || !isActive(p)) return;
      var oldest = NaN, stale = false;
      p.needs.forEach(function (n) {
        var s = srcs[n];
        if (!have(n) || !s) return;
        var t = parseTime(store[n].updated);
        if (!isNaN(t) && (isNaN(oldest) || t < oldest)) oldest = t;
        if (Dash.isStale(n)) stale = true;
      });
      p.el.classList.toggle('is-stale', stale);
      p.metaEl.textContent = stale ? '数据旧了 · ' + Dash.at(oldest) : (isNaN(oldest) ? '' : '更新 ' + Dash.at(oldest));
    });
    updateHeader();
  }

  /* ================= 页内选中状态 ================= */

  /* Dash.select('row', 'C-239')：面板 watch 里有 'row' 的都会重绘；值相同再选一次就取消 */
  Dash.select = function (key, value) {
    picks[key] = picks[key] === value ? null : value;
    Object.keys(panels).forEach(function (id) { if (panels[id].watch.indexOf(key) >= 0) schedule(panels[id]); });
    sync();   // 选中可能改变面板 wants() 点名的数据
  };
  Dash.selected = function (key) { return picks[key] == null ? null : picks[key]; };

  /* 重画面板（例如本地交互改了显示方式） */
  Dash.redraw = function (id) { schedule(panels[id]); };
  Dash.redrawAll = function () { Object.keys(panels).forEach(function (id) { schedule(panels[id]); }); };

  /* ================= 页眉、导航、角标 ================= */

  function headlineText() {
    var q = have('queue'), l = have('live'), parts = [];
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
    if (hl) { hl.textContent = headlineText(); hl.title = hl.textContent; }
    var dot = document.getElementById('fresh-dot'), txt = document.getElementById('fresh-text');
    if (!dot || !txt) return;
    var l = have('live'), stale = Dash.isStale('live'), bad = Dash.dataState('live') === 'error';
    dot.className = 'dot t-' + (bad ? 'risk' : !l ? 'muted' : stale ? 'warn' : 'ok');
    txt.textContent = DEMO ? '演示数据' : bad ? '实时数据读取失败' : !l ? '等数据' : (stale ? '实时数据旧了 · ' : '实时 ') + Dash.at(l.updated);
    var pop = document.getElementById('fresh-pop');
    if (pop && !pop.hidden) fillPop(pop);
  }

  var MODE_TEXT = { poll: '定时', version: '按版本', timer: '定时（无索引）' };
  function fillPop(pop) {
    var rows = M.data.map(function (s) {
      var d = have(s.name), m = meta[s.name] || {}, tone, text;
      if (Dash.dataState(s.name) === 'error') { tone = 'risk'; text = '读取失败'; }
      else if (!d) { tone = 'muted'; text = m.tried ? '没有数据' : (wanted(s.name) ? '读取中' : '这页用不到，没拉'); }
      else if (Dash.isStale(s.name)) { tone = 'warn'; text = '旧了 · ' + Dash.at(d.updated); }
      else { tone = 'ok'; text = Dash.at(d.updated); }
      return h('div.pop-row.t-' + tone, h('span.dot'), h('span.grow', s.label || s.name), h('span.pop-mode', MODE_TEXT[modeOf(s)]), h('span.ago', text));
    });
    pop.replaceChildren(h('div.pop-title', DEMO ? '正在看演示数据（?demo）' : '各份数据'), rows);
  }

  function updateCorner() {
    var c = document.getElementById('corner');
    if (!c) return;
    var text = index.state === 'missing' ? '数据索引未就绪：各份数据按定时拉取'
      : index.state === 'stale' ? '数据索引旧了（' + Dash.at(store.index && store.index.updated) + '）：各份数据按定时拉取' : '';
    c.hidden = !text;
    c.textContent = text;
  }

  /* 导航角标：现在页是没点"知道了"的待办数，健康页是要看的检查数 */
  function updateBadges() {
    var att = have('attention') && store.attention.items || [];
    var open = att.filter(function (x) { return !x.acked; });
    var urgent = open.some(function (x) { return x.urgency >= 3; });
    badge('now', open.length ? String(open.length) : '', urgent ? 'risk' : 'muted');
    var checks = have('health') && store.health.checks || [];
    var bad = checks.filter(function (c) { return c.level !== 'ok'; });
    badge('health', bad.length ? String(bad.length) : '', bad.some(function (c) { return c.level === 'bad'; }) ? 'risk' : 'warn');
  }

  function badge(pageId, text, tone) {
    var a = document.querySelector('#nav a[data-page="' + pageId + '"]');
    if (!a) return;
    var b = a.querySelector('.nav-badge');
    if (!text) { if (b) b.remove(); return; }
    if (!b) { b = h('span.nav-badge'); a.appendChild(b); }
    b.className = 'nav-badge t-' + (tone || 'muted');
    b.textContent = text;
  }

  function afterData() {
    refreshMeta();
    updateBadges();
    updateCorner();
  }

  /* ================= 路由 ================= */
  /* #页 、#页/子标签 、#页/块 、#页/子标签/块（块 id 就是面板 id） */

  Dash.go = function (path) { location.hash = '#' + path; };

  function route() {
    var parts = (location.hash || '').replace(/^#/, '').split('/').map(decodeURIComponent);
    var pg = pageOf(parts[0]) || M.pages[0];
    var tab = null, block = null;
    if (pg.tabs && pg.tabs.length) {
      var t = pg.tabs.filter(function (x) { return x.id === parts[1]; })[0];
      tab = t ? t.id : pg.tabs[0].id;
      block = t ? parts[2] : parts[1];
    } else {
      block = parts[1];
    }
    var changed = view.page !== pg.id || view.tab !== tab;
    view.page = pg.id; view.tab = tab;
    M.pages.forEach(function (p) {
      var sec = document.getElementById('page-' + p.id);
      if (sec) sec.classList.toggle('is-active', p.id === pg.id);
    });
    if (tab) {
      var sec = document.getElementById('page-' + pg.id);
      Array.prototype.forEach.call(sec.querySelectorAll('.page-tab'), function (el) { el.hidden = el.dataset.tab !== tab; });
      Array.prototype.forEach.call(sec.querySelectorAll('.tabs a'), function (a) {
        if (a.dataset.tab === tab) a.setAttribute('aria-current', 'page'); else a.removeAttribute('aria-current');
      });
    }
    Array.prototype.forEach.call(document.querySelectorAll('#nav a'), function (a) {
      if (a.dataset.page === pg.id) a.setAttribute('aria-current', 'page'); else a.removeAttribute('aria-current');
    });
    var tabTitle = tab ? pg.tabs.filter(function (x) { return x.id === tab; })[0].title + ' · ' : '';
    document.title = tabTitle + pg.title + ' · 作者观察台';
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
    var head = h('header.page-head',
      h('div.page-kicker', p.title),
      h('h1.page-ask', p.ask || p.title));
    var sec = h('section.page', { id: 'page-' + p.id }, head);
    if (p.tabs && p.tabs.length) {
      sec.appendChild(h('nav.tabs', { 'aria-label': p.title + '子标签' }, p.tabs.map(function (t) {
        return h('a', { href: '#' + p.id + '/' + t.id, dataset: { tab: t.id } }, t.title);
      })));
      p.tabs.forEach(function (t) {
        sec.appendChild(h('div.page-tab', { dataset: { tab: t.id }, hidden: true }, h('div.page-body')));
      });
    } else {
      sec.appendChild(h('div.page-body'));
    }
    return sec;
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
    var files = [];
    M.pages.forEach(function (p) {
      if (p.file) files.push(p);
      (p.tabs || []).forEach(function (t) { files.push(t); });
    });
    files.forEach(function (f) {
      if (f.css) loadAsset('css', 'app/' + f.file.replace(/\.js$/, '.css') + stamp);
      loadAsset('js', 'app/' + f.file + stamp, function () { console.warn('[Dash] 页面文件没找到：' + f.file); });
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
