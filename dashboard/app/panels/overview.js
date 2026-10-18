/* 总览：现在要不要作者动手。宽屏四行：状态句与阶段；需要你看 / 在做 / 谁在干活；后台脚本 / 健康摘要；钩子；最新通知。 */
(function () {
  var h = Dash.h;
  var MAX_NEED = 5, MAX_WORK = 8, MAX_HEALTH = 5;

  function staleCard(card, name, show) {
    if (show && Dash.isStale(name)) card.classList.add('is-stale');
    return card;
  }

  /* ---------- 需要你看 ---------- */
  function needRow(it) {
    var act = [];
    if (it.link) act.push(Dash.a(it.link, '打开'));
    act.push(h('button.btn.btn-quiet', { type: 'button', on: { click: function () { Dash.markSeen(it.key); } } }, '知道了'));
    return Dash.row({
      lead: Dash.chip(it.label, it.tone),
      title: it.title,
      sub: it.why || '',
      meta: Dash.atAgo(it.at),
      act: act,
      tone: it.need ? 'risk' : ''
    });
  }

  function needCard(att) {
    var body = h('div');
    var open = att.open;
    if (!open.length) {
      body.appendChild(Dash.empty('没有等你的事', '审计项、自动发现的待审项和【需定】来信都已清空', true));
    } else {
      body.appendChild(h('div.list', open.slice(0, MAX_NEED).map(needRow)));
      if (open.length > MAX_NEED) {
        body.appendChild(Dash.fold('还有 ' + (open.length - MAX_NEED) + ' 条', h('div.list', open.slice(MAX_NEED).map(needRow))));
      }
      body.appendChild(h('div.ov-more', h('a', { href: '#audit' }, '去审计页看全部')));
    }
    return Dash.card('需要你看', body);
  }

  /* ---------- 在做 ---------- */
  function sideOf(who) {
    var s = String(who || '').toLowerCase();
    return s === 'codex' ? 'codex' : s === 'claude' ? 'claude' : 'muted';
  }

  function doingCard(live) {
    var items = ((live && live.work && live.work.items) || []).slice();
    var rank = function (it) { return (it.state === '进行中' ? 0 : 1) + (it.agents.some(function (a) { return a.state === 'running'; }) ? -0.5 : 0); };
    items.sort(function (a, b) { return rank(a) - rank(b); });
    var body = h('div');
    var gap = Dash.liveGap(live, 'work', ['在做的工作', '施工队列'], '队列里没有进行中或待编译的行');
    if (gap) {
      body.appendChild(gap);
    } else {
      body.appendChild(h('div.ov-work', items.slice(0, MAX_WORK).map(function (it) {
        var run = it.agents.filter(function (a) { return a.state === 'running'; }).length;
        return h('div.ov-work-row',
          h('span.ov-work-id', it.row),
          Dash.longText(it.what, 'ov-work-what'),
          it.flags.length ? Dash.chip('不一致', 'auto') : null,
          run ? Dash.chip(run + ' 个在跑', sideOf(it.who)) : null,
          Dash.chip(it.state, it.state === '进行中' ? 'ok' : 'muted'),
          Dash.chip(it.who || '—', sideOf(it.who)));
      })));
      var more = items.length > MAX_WORK ? '另有 ' + (items.length - MAX_WORK) + ' 项，' : '';
      body.appendChild(h('div.ov-more', more, h('a', { href: '#doing' }, '去在做页')));
    }
    return staleCard(Dash.card('在做（' + items.length + ' 项）', body), 'live', !!live);
  }

  /* ---------- 谁在干活 ---------- */
  function whoCard(live) {
    var card = Dash.card('谁在干活', live ? Dash.who(live) : Dash.empty('等数据', '实时采集还没有写出运行状态', true));
    return staleCard(card, 'live', !!live);
  }

  /* ---------- 后台脚本 ---------- */
  function scriptsCard(live) {
    var card = Dash.card('后台脚本', live ? h('div', Dash.scripts(live), h('div.ov-wt', Dash.worktreeFold(live)), h('div.ov-more', h('a', { href: '#models' }, '看模型页'), '，', h('a', { href: '#doing' }, '看在做页的工作树'))) : Dash.empty('等数据', '', true));
    return staleCard(card, 'live', !!live);
  }

  /* ---------- 健康摘要：只给不正常的项 ---------- */
  function healthCard(hl) {
    var body = h('div');
    var checks = (hl && hl.checks) || [];
    if (!hl) {
      body.appendChild(Dash.empty('等数据', '项目采集还没有写出健康检查', true));
    } else {
      var bad = checks.filter(function (c) { return c.level !== 'ok'; });
      bad.sort(function (a, b) { return (b.level === 'bad') - (a.level === 'bad'); });
      if (!bad.length) {
        body.appendChild(Dash.status('全部 ' + checks.length + ' 项正常', 'ok'));
      } else {
        body.appendChild(h('div.list', bad.slice(0, MAX_HEALTH).map(function (c) {
          return Dash.row({ lead: h('span.dot.t-' + (c.level === 'bad' ? 'risk' : 'warn')), title: c.title, sub: c.summary, tone: c.level === 'bad' ? 'risk' : '' });
        })));
        if (bad.length > MAX_HEALTH) body.appendChild(h('div.ov-more', '还有 ' + (bad.length - MAX_HEALTH) + ' 项要看'));
      }
      body.appendChild(h('div.ov-more', checks.length - bad.length ? '另有 ' + (checks.length - bad.length) + ' 项正常，' : '', h('a', { href: '#health' }, '去健康页')));
    }
    return staleCard(Dash.card('健康', body), 'health', !!hl);
  }

  /* ---------- 钩子 ---------- */
  function hooksCard(live) {
    var list = (live && live.hooks) || [];
    var flagged = list.filter(function (x) { return x.flag; }).length;
    var card = Dash.card('钩子与生效记录', Dash.hooks(live), flagged ? flagged + ' 个要看' : '');
    return staleCard(card, 'live', !!live);
  }

  /* ---------- 最新通知 ---------- */
  var LEVEL = { info: ['信息', 'muted'], attention: ['留意', 'warn'], decision: ['需定', 'risk'] };

  function noticesCard(nt) {
    var items = ((nt && nt.items) || []).filter(function (x) { return !Dash.expired(x); });
    items.sort(function (a, b) { return (b.pinned ? 1 : 0) - (a.pinned ? 1 : 0) || (a.at < b.at ? 1 : -1); });
    var body = h('div');
    if (!items.length) {
      body.appendChild(Dash.empty('没有新通知', '', true));
    } else {
      body.appendChild(h('div.list', items.slice(0, 3).map(function (it) {
        var lv = LEVEL[it.level] || LEVEL.info;
        return Dash.row({
          lead: Dash.chip(lv[0], lv[1]),
          title: it.link ? Dash.a(it.link, it.text) : it.text,
          meta: Dash.atAgo(it.at)
        });
      })));
      body.appendChild(h('div.ov-more', h('a', { href: '#notices' }, '全部通知')));
    }
    return Dash.card('最新通知', body);
  }

  /* ---------- 第一行：状态句、阶段与优先级、数据新旧 ---------- */
  function headStatus(att, loaded) {
    if (!loaded) return Dash.status('等数据', 'muted', { large: true, sub: '采集脚本还没有写出数据，稍后会自动出现' });
    var needN = att.open.filter(function (x) { return x.need; }).length;
    if (needN) return Dash.status('有 ' + needN + ' 件事等你拍板', 'risk', { large: true, sub: att.open.length > needN ? '另外还有 ' + (att.open.length - needN) + ' 条待看' : '' });
    if (att.open.length) return Dash.status('有 ' + att.open.length + ' 条待看，不急', 'muted', { large: true });
    return Dash.status('现在没有要你动手的事', 'ok', { large: true });
  }

  function headMeta(q, live) {
    var stage = q && (q.stage || q.priority) ? [q.stage ? '阶段：' + q.stage : '', q.priority ? '优先级：' + q.priority : ''].filter(Boolean) : [];
    var fresh = live && live.updated ? '实时数据 ' + Dash.at(live.updated) : '实时数据：等数据';
    return h('div.ov-headmeta',
      stage.length ? h('div.ov-stage', stage.join(' · ')) : null,
      h('div.ov-fresh' + (live && Dash.isStale('live') ? '.is-stale' : ''), fresh + (live && Dash.isStale('live') ? '（旧了）' : '')));
  }

  Dash.panel({
    id: 'overview',
    page: 'overview',
    order: 10,
    card: false,
    partial: true,
    needs: ['live', 'queue', 'health', 'audit', 'audit-auto', 'notices'],
    render: function (el, d) {
      var att = Dash.attention();
      var loaded = !!(d.audit || d['audit-auto'] || d.notices);
      el.appendChild(h('div.stack',
        h('div.ov-head', headStatus(att, loaded), headMeta(d.queue, d.live)),
        h('div.ov-row3', needCard(att), doingCard(d.live), whoCard(d.live)),
        h('div.ov-row2', scriptsCard(d.live), healthCard(d.health)),
        hooksCard(d.live),
        noticesCard(d.notices)));
    }
  });
})();
