/* 观察台清单：页面与数据的展示信息。接口见 dashboard/README.md。
   pages  导航顺序即页面顺序。ask 是页顶那句"这页回答什么"；file 是这页的面板文件（相对 app/），同名 .css 一并加载；
          filters 是页头筛选条要放的筛选（side 两侧、range 时间范围、type 动态类型），写进地址。
   data   有哪些数据只由后端的数据名单定（serve 的 /api/data-names.js 给 window.DashDataNames），这里只登记每份的展示信息：
          label 是"还没有数据"等提示里的中文名；keep 为真：页眉与导航要用，不管在哪一页都保持最新，其余只在当前页有面板 needs 它时才拉。
          多久算旧由后端判（推送的 checked 带 stale 与 ratio），这里不写阈值。名单与展示信息对不上就直接报错。 */
window.DashManifest = {
  pages: [
    { id: 'now',    title: '现在', ask: '要不要我过目、推进到哪、机器正不正常', file: 'pages/now.js' },
    { id: 'work',   title: '施工', ask: '活在哪一段、流得顺不顺', file: 'pages/work.js', filters: ['side'] },
    { id: 'team',   title: '团队', ask: '谁在做哪行、两边配合得怎样', file: 'pages/team.js' },
    { id: 'health', title: '健康', ask: '这套开发机器正不正常，游戏本体最近在哪类失败', file: 'pages/health.js' },
    { id: 'perf',   title: '性能', ask: '游戏跑得顺不顺、成本花在哪、越改越快还是越慢', file: 'pages/perf.js' },
    { id: 'system', title: '体系', ask: '钩子与脚本怎么搭起来、在送什么', file: 'pages/system.js', filters: ['side'] },
    { id: 'feed',   title: '动态', ask: '最近发生了什么', file: 'pages/feed.js', filters: ['range', 'side', 'type'] },
    { id: 'docs',   title: '文档', ask: '查文档、读文档', file: 'pages/docs.js' }
  ],
  data: (function (names, meta) {
    if (!Array.isArray(names)) throw new Error('没读到后端的数据名单（/api/data-names.js）');
    var extra = Object.keys(meta).filter(function (n) { return names.indexOf(n) < 0; });
    var missing = names.filter(function (n) { return !meta[n]; });
    if (extra.length || missing.length) throw new Error('数据名单与页面展示信息对不上：名单没有 ' + extra.join('、') + '；没有展示信息 ' + missing.join('、'));
    return names.map(function (n) { return { name: n, label: meta[n].label, keep: !!meta[n].keep, path: 'data/auto/' + n + '.js' }; });
  })(window.DashDataNames, {
    live:      { label: '运行状态', keep: true },
    attention: { label: '等你过目', keep: true },
    feedback:  { label: '留言' },
    now:       { label: '推进摘要' },
    progress:  { label: '施工队列' },
    flow:      { label: '流转分析' },
    team:      { label: '团队' },
    collab:    { label: '协作分析' },
    health:    { label: '健康' },
    failures:  { label: '游戏失败' },
    perf:      { label: '性能' },
    system:    { label: '体系' },
    hookfeed:  { label: '最近注入' },
    feed:      { label: '动态' },
    docs:      { label: '文档目录' }
  })
};
