/* 体系页：钩子与脚本的结构。
   块（面板 id）：system-hookmap 钩子送什么 · system-collide 撞车提醒 · system-lanes 最近注入 · system-scripts 脚本树。
   数据：system（桑基图的节点与连线按侧由后端算好，另有撞车情景、脚本组、文档域下的文档）、hookfeed（四条泳道最近的注入）。
   连线只画到结构层（时机 → 钩子 → 注入类别 → 文档域），不画每个文件之间的边，不展示注入正文。 */
(function () {
  var h = Dash.h;

  function sideKey(x) { return Dash.sideKey(x); }
  function times(n) { return n == null ? '未确认' : Dash.num(n) + ' 次'; }
  function injects(n) { return n == null ? '未确认' : Dash.num(n) + ' 次注入'; }

  /* ---- 钩子结构图 ---- */

  /* 图由后端按侧算好（system.graph[side]：节点带层、24 小时次数、登记数、相邻节点；连线带次数）；
     这里只补上展示用的颜色和一句说明，交给桑基 */
  function byKey(list, k) {
    var m = {};
    list.forEach(function (x) { m[x[k]] = x; });
    return m;
  }

  function sankeyNodes(sys, graph) {
    var hooks = byKey(sys.hooks, 'id'), kinds = byKey(sys.kinds, 'key'), seq = {};
    return graph.nodes.map(function (n) {
      var tone, note = null;
      if (n.layer === 1) {
        var k = hooks[n.key];
        tone = ['side', sideKey(k.side)]; note = k.purpose;
      } else if (n.layer === 2) {
        var c = kinds[n.key];
        tone = ['inject', c.group]; note = c.group + (c.last ? '，最近一次 ' + Dash.at(c.last) : '');
      } else {
        seq[n.layer] = (seq[n.layer] || 0) + 1;
        tone = ['seq', seq[n.layer] - 1];
        if (n.layer === 0) note = '登记了 ' + n.regs + ' 个钩子';
      }
      return { id: n.id, label: n.label, tone: tone, depth: n.layer, value: n.n24, note: note };
    });
  }

  /* 抽屉里的"项名：内容"一行 */
  function row(k, v) { return h('div.sy-row', h('span.sy-k', k), h('div.sy-v', v)); }

  /* 节点自带的相邻节点（in 上一层、out 下一层）→ "名字 n 次"列表（后端已按次数排好） */
  function neighbors(list, nameOf) {
    return h('div.sy-list', list.map(function (x) {
      return h('div.sy-li' + (x.n24 === 0 ? '.is-zero' : ''), h('span', nameOf(x.id)), h('span.sy-n', times(x.n24)));
    }));
  }

  function hookLamp(ok) {
    var level = ok === true ? 'ok' : ok === false ? 'warn' : null;
    return h('span.sy-lamp', Dash.lamp(level), ok === true ? '正常' : ok === false ? '要看' : '未确认');
  }

  function docList(domain) {
    return h('div.sy-docs',
      h('button.btn', { type: 'button', on: { click: function () { Dash.drawer.close(); Dash.openDoc(domain.index); } } }, '读域总纲'),
      (domain.docs || []).map(function (x) {
      return h('button.sy-doc' + (x.parent ? '.is-sub' : ''), { type: 'button', on: { click: function () { Dash.drawer.close(); Dash.openDoc(x.code); } } },
        h('span.id', x.code), h('span.sy-doc-title', x.title));
    }));
  }

  function openNode(sys, graph, id) {
    var nodes = byKey(graph.nodes, 'id'), n = nodes[id];
    var nameOf = function (nid) { return nodes[nid].label; };
    var title = n.label, sub, body;
    if (n.layer === 0) {
      var hooks = byKey(sys.hooks, 'id');
      sub = '时机';
      body = [
        row('近 24 小时', injects(n.n24)),
        row('登记的钩子', h('div.sy-list', n.out.map(function (o) {
          var k = hooks[nodes[o.id].key];
          return h('div.sy-li', h('span', Dash.mark('side', sideKey(k.side)), ' ' + k.name), h('span.sy-n', hookLamp(k.ok)));
        })))
      ];
    } else if (n.layer === 1) {
      var x = byKey(sys.hooks, 'id')[n.key], tName = byKey(sys.timings, 'key');
      title = x.name; sub = Dash.label('side', sideKey(x.side)) + ' 侧的钩子';
      body = [
        row('状态', hookLamp(x.ok)),
        row('登记的事件', (x.timings || []).map(function (t) { return h('span.chip.t-muted', tName[t].label); })),
        row('作用', x.purpose || '—'),
        row('近 24 小时', injects(n.n24)),
        row('送出的类别', neighbors(n.out, nameOf))
      ];
    } else if (n.layer === 2) {
      var kd = byKey(sys.kinds, 'key')[n.key];
      sub = '注入类别 · ' + kd.group;
      body = [
        row('近 24 小时', times(n.n24)),
        row('最近一次', kd.last ? Dash.at(kd.last) + '（' + Dash.ago(kd.last) + '）' : '24 小时内没有'),
        row('由这些钩子送', neighbors(n.in, nameOf)),
        row('依据的文档域', neighbors(n.out, nameOf))
      ];
    } else {
      sub = '文档域';
      body = [row('近 24 小时', injects(n.n24)), row('下面的文档', docList(byKey(sys.domains, 'key')[n.key]))];
    }
    Dash.drawer({ title: title, sub: sub, body: h('div.sy-drawer', body) });
  }

  Dash.panel({
    id: 'system-hookmap', page: 'system', title: '钩子什么时候送什么',
    note: '从左到右：时机 → 钩子 → 注入类别 → 文档域；连线越粗，近 24 小时送得越多',
    caliber: '连线宽是近 24 小时钩子记账里这条路径上的注入次数；细灰虚线是 24 小时内没送过或次数未确认、结构仍在的路径', window: '24h',
    span: 12, order: 10, needs: ['system'], uses: ['side'],
    render: function (el, d, ctx) {
      var sys = d.system, graph = sys.graph[ctx.filters.side];
      if (!graph.links.length) { el.appendChild(Dash.empty(ctx.filters.side === 'all' ? '钩子记账里还没有连线' : '这一侧没有钩子的连线', '', true)); return; }
      var depthCount = [0, 0, 0, 0];
      graph.nodes.forEach(function (n) { depthCount[n.layer]++; });
      el.appendChild(h('div.sy-legend',
        Dash.mark('side', 'claude', 'Claude 侧钩子'), Dash.mark('side', 'codex', 'Codex 侧钩子'),
        h('span.muted', '细灰虚线：24 小时内没送过或次数未确认 · 点节点看详情')));
      el.appendChild(Dash.charts.sankey(sankeyNodes(sys, graph), graph.links.map(function (l) {
        return { from: l.from, to: l.to, value: l.n24 };
      }), {
        height: Math.max(380, Math.max.apply(null, depthCount) * 34 + 40), unit: '次', label: '钩子的时机、钩子、注入类别与文档域的连线',
        caliber: '近 24 小时注入次数；虚线是没送过或次数未确认、结构仍在的路径',
        onClick: function (p) { if (p.series === 'node') openNode(sys, graph, p.key); }
      }));
    }
  });

  /* ---- 撞车提醒 ---- */

  Dash.panel({
    id: 'system-collide', page: 'system', title: '哪类动作最常碰到撞车提醒', note: '动手前"先看对方"的提醒，按情景数近 24 小时的次数',
    caliber: '同一会话同一情景 30 分钟只记一次：数的是多少会话碰到这类动作，不是真撞车的次数', window: '24h',
    span: 4, order: 20, needs: ['system'],
    render: function (el, d) {
      var items = d.system.collide.items;
      if (!items.length) { el.appendChild(Dash.empty('钩子记账里没有撞车提醒的情景', '', true)); return; }
      if (d.system.collide.total24 == null) { el.appendChild(Dash.empty('近 24 小时撞车提醒的次数未确认', '', true)); return; }
      if (!d.system.collide.total24) { el.appendChild(Dash.empty('近 24 小时没有触发过撞车提醒', '', true)); return; }
      el.appendChild(Dash.charts.bars(items.map(function (x) {
        return { key: x.key, label: x.label, value: x.n24, text: x.n24 == null ? '未确认' : undefined, tone: x.n24 == null ? ['side', 'muted'] : ['inject', '提醒'] };
      }), { label: '撞车提醒按情景的近 24 小时次数', unit: '次', labelWidth: 84, textWidth: 56 }));
    }
  });

  /* ---- 最近注入了什么：四条泳道，每道最近 8 次 ---- */

  var LANES = [
    { role: '主会话', side: 'claude', main: true },
    { role: 'Claude 子代理', side: 'claude', main: false },
    { role: 'Codex 主线', side: 'codex', main: true },
    { role: 'Codex 子代理', side: 'codex', main: false }
  ];

  Dash.panel({
    id: 'system-lanes', page: 'system', title: '最近注入了什么', note: '钩子最近往四方的对话里送了哪几块规则或提醒',
    caliber: '钩子记账里每个对话最近 8 次有注入的事件；只写送了哪几块，不展示正文',
    span: 8, order: 30, needs: ['hookfeed'],
    render: function (el, d) {
      var byRole = {};
      (d.hookfeed.lanes || []).forEach(function (l) { byRole[l.role] = l.items || []; });
      el.appendChild(h('div.sy-lanes', LANES.map(function (lane) {
        var items = (byRole[lane.role] || []).slice().sort(function (a, b) { return Dash.parseTime(b.at) - Dash.parseTime(a.at); }).slice(0, 8);
        return h('section.sy-lane.t-' + lane.side + (lane.main ? '.is-main' : '.is-sub'),
          h('header.sy-lane-head', h('span.sy-lane-role', lane.role), h('span.sy-lane-n', items.length ? '最近 ' + items.length + ' 次' : '')),
          items.length ? h('div.sy-lane-items', items.map(function (x) {
            return h('div.sy-lane-item',
              h('div.sy-lane-meta', h('span.sy-lane-at', Dash.at(x.at)), h('span.sy-lane-ev', x.event), h('span.sy-lane-chars', Dash.num(x.chars) + ' 字')),
              h('div.sy-lane-blocks', (x.blocks || []).map(function (b) { return h('span.sy-lane-block', b); })));
          })) : h('div.sy-lane-none', '最近没有注入'));
      })));
    }
  });

  /* ---- 脚本树 ---- */

  var scripts = { input: null, count: null, tree: null };   // 搜索框与份数放在 fixed 里，重画不清掉它们

  function scriptNode(g, s) {
    var kids = [];
    if (s.usage) kids.push({ key: g.key + '/' + s.name + '#usage', label: '用法', sub: s.usage });
    if ((s.callers || []).length) kids.push({ key: g.key + '/' + s.name + '#callers', label: '谁调用', sub: s.callers.join('；') });
    if (s.outputs) kids.push({ key: g.key + '/' + s.name + '#outputs', label: '产出', sub: s.outputs });
    return { key: g.key + '/' + s.name, label: s.name, sub: s.purpose, kids: kids };
  }

  /* 搜索只比脚本的名字和作用；组和展开的用法行只随命中的脚本出现 */
  function matchScript(n, text) {
    if (n.key.indexOf('/') < 0 || n.key.indexOf('#') >= 0) return false;
    return [n.label, n.sub].some(function (s) { return s != null && String(s).toLowerCase().indexOf(text) >= 0; });
  }

  Dash.panel({
    id: 'system-scripts', page: 'system', title: '有哪些脚本、谁在用', note: '按用途分组；点开脚本看用法、谁调用、产出',
    caliber: '脚本清单来自脚本目录的扫描；用法、调用方与产出来自各脚本开头的说明，没写的不显示',
    span: 12, order: 40, needs: ['system'],
    fixed: function (el) {
      scripts.input = h('input.sy-search', { type: 'search', placeholder: '搜脚本名或作用', 'aria-label': '搜脚本名或作用' });
      scripts.count = h('span.muted');
      scripts.input.addEventListener('input', function () { if (scripts.tree) scripts.tree.filter(scripts.input.value); });
      el.appendChild(h('div.sy-bar', scripts.input, scripts.count));
    },
    render: function (el, d) {
      var groups = d.system.scripts.groups;
      scripts.tree = null;
      scripts.count.textContent = '';
      if (!groups.length) { el.appendChild(Dash.empty('没有脚本分组', '', true)); return; }
      scripts.count.textContent = groups.length + ' 组 · 共 ' + Dash.num(d.system.scripts.total) + ' 个脚本';
      scripts.tree = Dash.tree(groups.map(function (g) {
        var items = g.items || [];
        return { key: g.key, label: g.label, sub: g.note, count: items.length, kids: items.map(function (s) { return scriptNode(g, s); }) };
      }), { filter: matchScript });
      el.appendChild(scripts.tree);
      if (scripts.input.value) scripts.tree.filter(scripts.input.value);
    }
  });
})();
