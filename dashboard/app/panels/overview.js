/* 总览：现在要不要作者动手。 */
(function () {
  var h = Dash.h;
  var MAX_NEED = 5;

  /* ---------- 需要你看 ---------- */
  function needRow(it) {
    var act = [];
    if (it.link) act.push(Dash.a(it.link, '打开'));
    act.push(h('button.btn.btn-quiet', { type: 'button', on: { click: function () { Dash.markSeen(it.key); } } }, '知道了'));
    return Dash.row({
      lead: Dash.chip(it.label, it.tone),
      title: it.title,
      sub: it.why || '',
      meta: Dash.ago(it.at),
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

  /* ---------- 健康签条 ---------- */
  function healthTiles(hl) {
    if (!hl) {
      return ['测试', '门禁', '推送', '清理', '文档'].map(function (n) { return Dash.tile(n, '等数据', '', 'muted'); });
    }
    var t = hl.tests || {}, g = hl.gate || {}, git = hl.git || {}, c = hl.cleanup || {}, d = hl.docs || {};
    var tiles = [];

    var tTone = t.failed > 0 || t.slow > 0 ? 'warn' : 'ok';
    var tVal = t.failed > 0 ? Dash.magText(t.failed, '个失败') : (t.slow > 0 ? '有慢测试' : '通过');
    var tSub = Dash.magText(t.count, '个') + (t.slow > 0 ? ' · 慢 ' + Dash.mag(t.slow) : '') + (t.lastRun ? ' · ' + Dash.ago(t.lastRun) : '');
    tiles.push(Dash.tile('测试', tVal, tSub, tTone));

    var res = String(g.result == null ? '' : g.result);
    var gTone = /fail|失败|红|错/i.test(res) ? 'warn' : (/pass|ok|通过|绿|success/i.test(res) ? 'ok' : 'muted');
    tiles.push(Dash.tile('门禁', gTone === 'ok' ? '通过' : (gTone === 'warn' ? '没过' : (res || '没记录')),
      [gTone === 'warn' && g.failedGates && g.failedGates.length ? g.failedGates.join(' ') : g.scope, g.at ? Dash.ago(g.at) : ''].filter(Boolean).join(' · '), gTone));

    var un = git.unpushed || 0;
    tiles.push(Dash.tile('推送', un ? Dash.magText(un, '个没推') : '已推送',
      git.recent && git.recent[0] ? Dash.ago(git.recent[0].at) + '最近提交' : '', un > 0 ? 'warn' : 'ok'));

    var st = c.stale || 0;
    tiles.push(Dash.tile('清理', st ? Dash.magText(st, '项待清') : '干净', '', st ? 'warn' : 'ok'));

    var ov = d.over || 0;
    tiles.push(Dash.tile('文档', ov ? Dash.magText(ov, '份超限') : '体量正常', '', ov ? 'warn' : 'ok'));
    return tiles;
  }

  function healthCard(hl) {
    var stale = hl && Dash.isStale('health');
    var card = Dash.card('健康', h('div.tiles', healthTiles(hl)), hl ? (stale ? '数据旧了' : '') : '');
    if (stale) card.classList.add('is-stale');
    return card;
  }

  /* ---------- 模型与脚本缩略 ---------- */
  function modelsCard(live) {
    var body = h('div');
    if (!live) {
      body.appendChild(Dash.empty('等数据', '实时采集还没有写出运行状态', true));
    } else {
      var ss = (live.sessions || []).filter(function (s) { return s.state === 'running'; });
      var ag = (live.agents || []).filter(function (a) { return a.state === 'running'; });
      var sc = (live.scripts || []).filter(function (x) { return x.state === 'running'; });
      var lock = live.build && live.build.lock;
      body.appendChild(h('div.ov-counts',
        count(ss.length, '个会话', 'claude'),
        count(ag.length, '个子代理', 'claude'),
        count(sc.length, '个脚本', 'auto')));
      if (ss.length) {
        body.appendChild(h('div.cluster.ov-sessions', ss.slice(0, 6).map(function (s) {
          return Dash.chip(s.name + (s.model ? ' · ' + Dash.modelName(s.model) : ''), Dash.sideTone(s.name, s.side));
        })));
      }
      body.appendChild(h('div.cluster.ov-lock',
        Dash.chip(lock === 'held' ? '构建锁 占用中' : '构建锁 空闲', lock === 'held' ? 'auto' : 'muted'),
        h('a', { href: '#models' }, '看模型页')));
    }
    var stale = live && Dash.isStale('live');
    var card = Dash.card('模型与脚本', body, stale ? '数据旧了' : '');
    if (stale) card.classList.add('is-stale');
    return card;
  }

  function count(n, unit, tone) {
    return h('div.ov-count.t-' + tone, h('span.ov-count-n', Dash.mag(n)), h('span.ov-count-u', unit));
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
          meta: Dash.ago(it.at)
        });
      })));
      body.appendChild(h('div.ov-more', h('a', { href: '#notices' }, '全部通知')));
    }
    return Dash.card('最新通知', body);
  }

  /* ---------- 第一行状态 ---------- */
  function headStatus(att, loaded) {
    if (!loaded) return Dash.status('等数据', 'muted', { large: true, sub: '采集脚本还没有写出数据，稍后会自动出现' });
    var needN = att.open.filter(function (x) { return x.need; }).length;
    if (needN) return Dash.status('有 ' + Dash.mag(needN) + ' 件事等你拍板', 'risk', { large: true, sub: att.open.length > needN ? '另外还有 ' + Dash.mag(att.open.length - needN) + ' 条待看' : '' });
    if (att.open.length) return Dash.status('有 ' + Dash.mag(att.open.length) + ' 条待看，不急', 'muted', { large: true });
    return Dash.status('现在没有要你动手的事', 'ok', { large: true });
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
      var left = h('div.stack', needCard(att), noticesCard(d.notices));
      var right = h('div.stack', healthCard(d.health), modelsCard(d.live));
      el.appendChild(h('div.stack',
        h('div.ov-head', headStatus(att, loaded)),
        h('div.grid.grid-2', left, right)));
    }
  });
})();
