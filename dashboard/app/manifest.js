/* 观察台清单：页面、页面文件、数据源的唯一登记处。加页面、数据源只改这里。
   pages  导航顺序即页面顺序。ask 是页顶那句"这页回答什么"；file 是这页的面板文件（路径相对 app/），同名 .css 一并加载。
   data   数据源（数据契约见 dashboard/README.md §2）。path 相对 dashboard/（?demo 时改读 data/demo/<name>.js）。
          poll   true：不看版本索引，每 every 秒拉一次（live 与 index 本身）。
          keep   true：页眉与导航要用，不管在哪一页都保持最新。
          其余数据只在当前页的面板用到时才拉，之后版本索引里它的版本变了才重拉；
          版本索引还没有、旧了或没列这份数据时，按 every 秒定时拉。
          stale  超过多少秒没更新算"数据旧了"（null 不判断）。 */
window.DashManifest = {
  pages: [
    { id: 'home',     title: '首页', ask: '要不要我动手、项目推进得怎样', file: 'pages/home.js' },
    { id: 'progress', title: '进展', ask: '还没做完的活各在哪一段', file: 'pages/progress.js' },
    { id: 'team',     title: '团队', ask: '两边各在忙什么、用了哪些模型', file: 'pages/team.js' },
    { id: 'system',   title: '体系', ask: '这套开发机器由什么组成、是否正常', file: 'pages/system.js' },
    { id: 'library',  title: '资料', ask: '查文档、决策和通知', file: 'pages/library.js' }
  ],
  data: [
    { name: 'live',      label: '运行状态', path: 'data/auto/live.js',      poll: true, every: 15, stale: 90 },
    { name: 'index',     label: '数据索引', path: 'data/auto/index.js',     poll: true, every: 15, stale: 300 },
    { name: 'attention', label: '等你过目', path: 'data/auto/attention.js', keep: true, every: 60, stale: 900 },
    { name: 'progress',  label: '进展',     path: 'data/auto/progress.js',  keep: true, every: 60, stale: 900 },
    { name: 'feedback',  label: '留言',     path: 'data/auto/feedback.js',  every: 30, stale: null },
    { name: 'stats',     label: '关键统计', path: 'data/auto/stats.js',     every: 60, stale: 900 },
    { name: 'team',      label: '团队',     path: 'data/auto/team.js',      every: 60, stale: 900 },
    { name: 'system',    label: '体系',     path: 'data/auto/system.js',    every: 120, stale: 900 },
    { name: 'hookfeed',  label: '最近注入', path: 'data/auto/hookfeed.js',  every: 30, stale: 900 },
    { name: 'records',  label: '决策',     path: 'data/auto/records.js',   every: 600, stale: null },
    { name: 'docs',      label: '文档目录', path: 'data/auto/docs.js',      every: 600, stale: null },
    { name: 'notices',   label: '通知',     path: 'data/curated/notices.js', every: 60, stale: null }
  ]
};
