/* 文档阅读器：Dash.reader(el, {code, sec?, find?, docs}) 调 /api/doc 取 Markdown 原文，用 marked（vendor/marked.min.js）渲染。
   左边小节目录（可折叠，当前节随滚动高亮），右边正文；正文里的"编码 §n"变成链接；sec 认小节的 anchor，也认小节号 n；
   find 先到 sec 那一节（没给 sec 就全文）再找第一处，高亮并滚过去；读不到时写"这份文档没读到：原因"。接口见 README §3。 */
(function () {
  'use strict';

  var Dash = window.Dash;
  if (!Dash.internal) return;
  var h = Dash.h;
  var cache = {};          // 编码@更新时刻 -> Promise<文档>
  var tocOpen = true;
  var md = new window.marked.Marked({
    gfm: true,
    renderer: { html: function (t) { return Dash.esc(t.text || t.raw || ''); } }   // 文档里的原始 HTML 当文字显示
  });

  function docsList(d) {
    var out = {};
    (d && d.domains || []).forEach(function (dm) { (dm.docs || []).forEach(function (x) { out[x.code] = x; }); });
    return out;
  }

  function fetchDoc(code, docs) {
    var info = docsList(docs)[code];
    var key = code + '@' + (info ? info.updated : '');
    if (!cache[key]) {
      cache[key] = fetch('/api/doc?code=' + encodeURIComponent(code))
        .catch(function () { throw new Error('观察台本地服务没在运行'); })
        .then(function (r) {
          return r.json().catch(function () { throw new Error('本地服务还不提供文档阅读'); }).then(function (j) {
            if (!j.ok) throw new Error(j.error || (r.status === 404 ? '没有这份文档' : '本地服务没有给出'));
            return j;
          });
        });
      cache[key].catch(function () { delete cache[key]; });
    }
    return cache[key];
  }

  function docHref(code, sec) {
    return '#docs?code=' + encodeURIComponent(code) + (sec ? '&sec=' + encodeURIComponent(sec) : '');
  }

  /* 标题配上服务端算好的 anchor：按出现顺序，同级、文字对得上的优先 */
  function anchorHeadings(body, toc) {
    var hs = Array.prototype.slice.call(body.querySelectorAll('h2, h3'));
    var from = 0;
    toc.forEach(function (t) {
      var tag = 'H' + t.level, pick = -1;
      for (var i = from; i < hs.length; i++) {
        if (hs[i].tagName === tag && hs[i].textContent.indexOf(t.title) >= 0) { pick = i; break; }
      }
      if (pick < 0) for (var j = from; j < hs.length; j++) if (hs[j].tagName === tag) { pick = j; break; }
      if (pick < 0) return;
      hs[pick].id = t.anchor;
      from = pick + 1;
    });
  }

  /* 正文里的"编码 §n"、单独的"§n"（归前面最近的编码，没有就是本文）变成链接；代码与已有链接里不动 */
  var REF = /([A-Z][A-Z0-9]*(?:-[A-Z0-9]+)+)(?:\s*§\s*(\d+(?:\.\d+)*))?|§\s*(\d+(?:\.\d+)*)/g;
  function linkRefs(body, self, docs) {
    var codes = docsList(docs);
    var walker = document.createTreeWalker(body, NodeFilter.SHOW_TEXT, {
      acceptNode: function (n) { return n.parentElement.closest('code, pre, a') ? NodeFilter.FILTER_REJECT : NodeFilter.FILTER_ACCEPT; }
    });
    var texts = [];
    while (walker.nextNode()) texts.push(walker.currentNode);
    texts.forEach(function (node) {
      var s = node.nodeValue, last = 0, m, frag = null, lastCode = null;
      REF.lastIndex = 0;
      while ((m = REF.exec(s))) {
        var code = m[1] || lastCode || self, sec = m[2] || m[3] || null;
        if (m[1] && !codes[m[1]]) continue;
        if (m[1]) lastCode = m[1];
        frag = frag || document.createDocumentFragment();
        frag.appendChild(document.createTextNode(s.slice(last, m.index)));
        frag.appendChild(h('a.doc-ref', { href: docHref(code, sec) }, m[0]));
        last = m.index + m[0].length;
      }
      if (!frag) return;
      frag.appendChild(document.createTextNode(s.slice(last)));
      node.parentNode.replaceChild(frag, node);
    });
  }

  /* 文档里的相对链接按文档所在位置解析，新标签页打开；页内 #锚点 只滚动，不改地址 */
  function fixLinks(body, path) {
    Array.prototype.forEach.call(body.querySelectorAll('a[href]:not(.doc-ref)'), function (a) {
      var href = a.getAttribute('href');
      if (href.charAt(0) === '#') {
        a.addEventListener('click', function (e) {
          e.preventDefault();
          var t = document.getElementById(decodeURIComponent(href.slice(1)));
          if (t) t.scrollIntoView({ block: 'start' });
        });
        return;
      }
      if (!/^[a-z]+:/i.test(href)) a.href = new URL(href, location.origin + '/' + path).pathname;
      a.target = '_blank';
      a.rel = 'noopener';
    });
  }

  /* 一节的范围：从它的标题到下一个同级或更高的标题 */
  function sectionNodes(body, head) {
    if (!head) return [body];
    var lv = Number(head.tagName.slice(1)), out = [head], n = head.nextElementSibling;
    while (n && !(/^H[1-6]$/.test(n.tagName) && Number(n.tagName.slice(1)) <= lv)) { out.push(n); n = n.nextElementSibling; }
    return out;
  }

  function highlight(roots, text) {
    for (var r = 0; r < roots.length; r++) {
      var walker = document.createTreeWalker(roots[r], NodeFilter.SHOW_TEXT);
      while (walker.nextNode()) {
        var node = walker.currentNode, i = node.nodeValue.indexOf(text);
        if (i < 0) continue;
        var hit = node.splitText(i);
        hit.splitText(text.length);
        var mk = h('mark.find-hit');
        hit.parentNode.replaceChild(mk, hit);
        mk.appendChild(hit);
        return mk;
      }
    }
    return null;
  }

  function render(el, doc, o) {
    var toc = (doc.toc || []).filter(function (t) { return t.level === 2 || t.level === 3; });
    var body = h('article.reader-body.md');
    body.innerHTML = md.parse(String(doc.md || '').replace(/^---\r?\n[\s\S]*?\r?\n---\r?\n/, ''));   // 文件头的元数据不显示
    anchorHeadings(body, toc);
    linkRefs(body, doc.code, o.docs);
    fixLinks(body, doc.path || '');

    var items = toc.map(function (t) {
      return h('a.toc-item.lv-' + t.level, { href: docHref(doc.code, t.anchor), dataset: { anchor: t.anchor } },
        h('span.toc-n', '§' + t.n), h('span.toc-t', t.title));
    });
    var tocEl = h('nav.reader-toc' + (tocOpen ? '' : '.is-closed'), { 'aria-label': '小节目录' },
      h('button.btn.toc-toggle', { type: 'button', on: { click: function () {
        tocOpen = !tocOpen;
        tocEl.classList.toggle('is-closed', !tocOpen);
        wrap.classList.toggle('is-toc-closed', !tocOpen);
      } } }, '目录'),
      h('div.toc-list', items.length ? items : h('div.muted', '这份没有小节')));
    var wrap = h('div.reader-wrap' + (tocOpen ? '' : '.is-toc-closed'), tocEl, body);
    var info = docsList(o.docs)[doc.code];
    el.replaceChildren(h('div.reader',
      h('header.reader-head',
        h('div.reader-title', h('span.chip.reader-code', doc.code), h('span', doc.title)),
        h('div.reader-meta', [doc.domain ? '所在域 ' + doc.domain : null, '更新 ' + Dash.at(doc.updated || (info && info.updated))].filter(Boolean).join(' · '))),
      wrap));

    /* 当前节随滚动高亮 */
    var heads = toc.map(function (t) { return document.getElementById(t.anchor); });
    function onScroll() {
      if (!body.isConnected) { window.removeEventListener('scroll', onScroll); return; }
      var cur = -1;
      heads.forEach(function (x, i) { if (x && x.getBoundingClientRect().top < innerHeight * 0.4) cur = i; });
      items.forEach(function (a, i) { a.classList.toggle('is-current', i === cur); });
    }
    window.addEventListener('scroll', onScroll, { passive: true });

    var target = null;
    if (o.sec) {
      var t = toc.filter(function (x) { return x.anchor === o.sec || x.n === String(o.sec); })[0];
      target = t ? document.getElementById(t.anchor) : null;
    }
    var hit = o.find ? highlight(sectionNodes(body, target), String(o.find)) : null;
    requestAnimationFrame(function () {
      if (hit) hit.scrollIntoView({ block: 'center' });
      else if (target) target.scrollIntoView({ block: 'start' });
      onScroll();
    });
  }

  Dash.reader = function (el, o) {
    o = o || {};
    if (!o.code) { el.replaceChildren(Dash.empty('从左边点一份文档', '', true)); return; }
    el.replaceChildren(h('div.skeleton.is-tall'));
    fetchDoc(o.code, o.docs).then(function (doc) {
      if (el.isConnected) render(el, doc, o);
    }, function (e) {
      if (el.isConnected) el.replaceChildren(Dash.empty('这份文档没读到：' + e.message, '', true));
    });
  };
})();
