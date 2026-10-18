/* 模型页：会话 → 子代理（与总览"谁在干活"是同一个渲染函数 Dash.who），后台脚本与构建锁（Dash.scripts）。数据 live（15 s 刷新）。 */
(function () {
  var h = Dash.h;

  function statusLine(live) {
    var ss = (live.sessions || []).filter(function (s) { return s.state === 'running'; }).length;
    var ag = (live.agents || []).filter(function (a) { return a.state === 'running'; }).length;
    var b = live.build || {};
    var held = b.lock === 'held';
    var line = Dash.statusLine(held ? 'auto' : ss ? 'ok' : 'muted');
    line.body.append((ss ? ss + ' 个会话在跑' : '没有会话在跑') + '，' + (ag ? ag + ' 个子代理在跑' : '没有子代理在跑') + '，' +
      (held ? '构建锁被 ' + (b.holder || '某个进程') + ' 占用' : '构建锁空闲'));
    return line;
  }

  Dash.panel({
    id: 'models',
    page: 'models',
    title: '会话与子代理',
    order: 10,
    needs: ['live'],
    render: function (el, data) {
      if (!data.live) { el.appendChild(h('p.muted', '还没有实时数据')); return; }
      el.appendChild(h('div.stack', statusLine(data.live), Dash.who(data.live)));
    }
  });

  Dash.panel({
    id: 'models-scripts',
    page: 'models',
    title: '后台脚本与构建锁',
    order: 20,
    needs: ['live'],
    render: function (el, data) {
      if (data.live) el.appendChild(Dash.scripts(data.live));
    }
  });
})();
