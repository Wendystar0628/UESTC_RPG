/* 审计：需要作者审的事（模型挂的 + 自动发现的），可点开对应文件。"知道了"只存在浏览器本地。 */
(function () {
  var h = Dash.h;

  function row(it, mode) {
    var act = [];
    if (it.link) act.push(Dash.a(it.link, '打开'));
    if (mode === 'open') {
      act.push(h('button.btn.btn-quiet', { type: 'button', on: { click: function () { Dash.markSeen(it.key); } } }, '知道了'));
    } else if (mode === 'seen') {
      act.push(h('button.btn.btn-quiet', { type: 'button', on: { click: function () { Dash.markSeen(it.key, false); } } }, '放回待看'));
    }
    var sub = [it.why, it.from && it.src === 'audit' ? '来自 ' + it.from : ''].filter(Boolean).join(' · ');
    return Dash.row({
      lead: Dash.chip(it.label, it.tone),
      title: it.title,
      sub: sub,
      meta: Dash.ago(it.at),
      act: act,
      tone: mode === 'open' && it.need ? 'risk' : '',
      seen: mode !== 'open'
    });
  }

  function rows(list, mode) { return h('div.list', list.map(function (it) { return row(it, mode); })); }

  Dash.panel({
    id: 'audit',
    page: 'audit',
    order: 10,
    card: false,
    partial: true,
    needs: ['audit', 'audit-auto', 'notices'],
    render: function (el) {
      var att = Dash.attention();
      var needN = att.open.filter(function (x) { return x.need; }).length;
      var auditOpen = att.open.filter(function (x) { return x.src === 'audit'; });
      var autoOpen = att.open.filter(function (x) { return x.src !== 'audit'; });
      var status;
      if (needN) status = Dash.status('有 ' + Dash.mag(needN) + ' 件要你拍板', 'risk', { sub: att.open.length > needN ? '另有 ' + Dash.mag(att.open.length - needN) + ' 条待看' : '' });
      else if (att.open.length) status = Dash.status(Dash.mag(att.open.length) + ' 条待看，不急', 'muted');
      else status = Dash.status('没有等你审的事', 'ok');

      var stack = h('div.stack', status);

      stack.appendChild(Dash.card('模型挂的审计项',
        auditOpen.length ? rows(auditOpen, 'open') : Dash.empty('这里没有待审的事', '', true)));
      stack.appendChild(Dash.card('自动发现与【需定】',
        autoOpen.length ? rows(autoOpen, 'open') : Dash.empty('新截图、海报、报告页、待审调研和【需定】来信都已看过', '', true)));

      if (att.seen.length) {
        stack.appendChild(Dash.fold('已知道 ' + att.seen.length + ' 条（只在这个浏览器里记着）', rows(att.seen, 'seen')));
      }
      if (att.done.length) {
        stack.appendChild(Dash.fold('已结项 ' + att.done.length + ' 条', rows(att.done, 'done')));
      }
      el.appendChild(stack);
    }
  });
})();
