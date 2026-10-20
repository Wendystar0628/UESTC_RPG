/* 动态页：近 30 天发生过什么。
   块（面板 id）：feed-strip 30 天密度条（只做导航：拖选一段时间，点一格选那一天；类型在页头筛选条里选）；
                  feed-timeline 按天分组的竖时间线，点开一条看详情和链接；没有发生时刻的事实列在最后的“时刻未确认”下。
   数据：feed。时间范围取地址里的 from、to（日期，拖选写入），没有就取筛选条的 range；
   侧与类型筛选对两块都生效，密度条不受时间范围影响（它是用来选时间的）。 */
(function () {
  var h = Dash.h;

  var DAY = 86400000;
  var RANGE_MS = { '24h': DAY, '7d': 7 * DAY, '30d': 30 * DAY };
  var DATE_RE = /^\d{4}-\d{2}-\d{2}$/;

  function pad2(n) { return (n < 10 ? '0' : '') + n; }
  function dateKey(ms) {
    var d = new Date(ms);
    return d.getFullYear() + '-' + pad2(d.getMonth() + 1) + '-' + pad2(d.getDate());
  }
  function dayStart(key) {
    var p = key.split('-');
    return new Date(+p[0], +p[1] - 1, +p[2]).getTime();
  }
  function short(key) { return +key.slice(5, 7) + '-' + +key.slice(8, 10); }

  /* 类型空数组即全选 */
  function byFilters(items, ctx) {
    var side = ctx.filters.side, types = ctx.filters.type;
    return items.filter(function (it) {
      return (!types.length || types.indexOf(it.type) >= 0) && (side === 'all' || it.side === side);
    });
  }

  /* 时间窗：地址有 from、to 就按日期取（含两端那天，custom）；否则按筛选条的 range。from、to 是日期串，给密度条标已选 */
  function windowOf(ctx) {
    var p = ctx.params;
    if (DATE_RE.test(p.from || '') && DATE_RE.test(p.to || '')) {
      var a = p.from <= p.to ? p.from : p.to, b = p.from <= p.to ? p.to : p.from;
      return { start: dayStart(a), end: dayStart(b) + DAY, from: a, to: b, custom: true };
    }
    var now = Date.now(), start = now - RANGE_MS[ctx.filters.range];
    return { start: start, end: Infinity, from: dateKey(start), to: dateKey(now), custom: false };
  }

  /* 不带 from、to 就是回到页头的时间范围；筛选由 Dash.go 自己保留 */
  function setSpan(ctx, from, to) {
    Dash.go('feed', Object.assign({}, ctx.params, { from: from || null, to: to || null }));
  }

  /* ---------- feed-strip ---------- */

  function strip(el, d, ctx) {
    var win = windowOf(ctx), side = ctx.filters.side, types = ctx.filters.type;
    var days = d.feed.days.map(function (day) {  // 所选格子里有一格覆盖不足（n 为 null），这一天就是未确认
      var picked = day.cells.filter(function (c) { return (!types.length || types.indexOf(c.type) >= 0) && (side === 'all' || c.side === side); });
      var unknown = picked.some(function (c) { return c.n == null; });
      return { day: day.day, value: unknown ? null : picked.reduce(function (n, c) { return n + c.n; }, 0) };
    });

    el.appendChild(Dash.charts.strip(days, {
      unit: '条', label: '近 30 天，每天一格，深浅表示事件多少；按住拖选一段时间',
      coverage: ctx.coverage, selected: { from: win.from, to: win.to },
      onBrush: function (r) { setSpan(ctx, r.from, r.to); },
      onClick: function (p) { setSpan(ctx, p.key, p.key); }
    }));
    el.appendChild(win.custom
      ? h('div.note-line', '已选 ' + short(win.from) + (win.from === win.to ? '' : ' 至 ' + short(win.to)) + ' ',
        h('button.btn.fd-clear', { type: 'button', on: { click: function () { setSpan(ctx); } } }, '清除时间选择'))
      : h('div.note-line', '在密度条上按住拖选一段时间（点一格选那一天）；不选就按页头的时间范围，框出来的是当前范围。'));
  }

  /* ---------- feed-timeline ---------- */

  function titleOf(it) {
    var t = it.title || '';
    if ((it.type === 'decision' || it.type === 'done') && it.id && t.indexOf(it.id) !== 0) return it.id + ' ' + t;
    return t;
  }

  function bodyOf(it) {
    var kids = [];
    if (it.detail) kids.push(h('p.fd-detail', it.detail));
    else if (it.type === 'done') kids.push(h('p.fd-detail', '推送于 ' + Dash.at(it.at)));
    var l = it.link;
    if (l && l.doc) {
      kids.push(h('button.btn.fd-link', { type: 'button', on: { click: function () { Dash.openDoc(l.doc, { find: l.find }); } } }, '在阅读器中打开'));
    } else if (l && l.path) {
      kids.push(Dash.open(l.path, '打开产物', 'btn.fd-link'));
    }
    return kids.length ? h('div.fd-body', kids) : null;
  }

  function timeline(el, d, ctx) {
    var win = windowOf(ctx);
    /* 顺序是后端给的（新的在前，没有时刻的排在最后）；没有时刻的放不进任何时间窗，一律列在末尾的"时刻未确认"下 */
    var rows = byFilters(d.feed.items || [], ctx).filter(function (it) {
      var t = Dash.parseTime(it.at);
      return isNaN(t) || (t >= win.start && t < win.end);
    });

    if (!rows.length) { el.appendChild(Dash.empty('这段时间没有这类动态', '', true)); return; }
    el.appendChild(Dash.timeline(rows.map(function (it) {
      return { at: it.at, tone: ['feed', it.type], tag: Dash.label('feed', it.type), side: it.side, title: titleOf(it), body: bodyOf(it) };
    })));
  }

  var USES = ['side', 'range', 'type', 'params'];

  Dash.panel({
    id: 'feed-strip', page: 'feed', title: '想看哪段时间', note: '一天一格，越深事件越多；类型在页头选，时间在这里拖选',
    window: '30d', span: 12, order: 1, needs: ['feed'], uses: USES, render: strip
  });

  Dash.panel({
    id: 'feed-timeline', page: 'feed', title: '发生了什么', note: '按天分组，新的在上；点开一条看详情和链接',
    caliber: '近 30 天的决策登记、通知、里程碑、完成推送、回滚和收口审计，按发生时刻排列',
    span: 12, order: 2, needs: ['feed'], uses: USES, render: timeline
  });
})();
