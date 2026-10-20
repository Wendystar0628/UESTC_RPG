/* 观察台清单：页面、页面文件、数据源的唯一登记处。加页面、子标签、数据源只改这里。
   pages  导航顺序即页面顺序。ask 是页顶那句"这页回答什么"；file 是这页的面板文件（路径相对 app/）；
          css 为真时加载同名 .css；tabs 是子标签（资料页），每个子标签一个面板文件，面板注册时写 tab。
   data   数据源。path 相对 dashboard/（?demo 时改读 data/demo/<name>.js）。
          poll   true：不看版本索引，每 every 秒拉一次（live 与 index 本身）。
          keep   true：页眉与导航要用，不管在哪一页都保持最新。
          其余数据只在当前页的面板用到时才拉，之后版本索引里它的版本变了才重拉；
          版本索引还没有、旧了或没列这份数据时，按 every 秒定时拉。
          stale  超过多少秒没更新算"数据旧了"（null 不判断）。 */
window.DashManifest = {
  pages: [
    { id: 'now',     title: '现在', ask: '要不要我动手、此刻在发生什么', file: 'pages/now.js', css: true },
    { id: 'work',    title: '施工', ask: '每件活走到哪一步、谁挡着谁', file: 'pages/work.js', css: true },
    { id: 'collab',  title: '协作', ask: '两边在说什么、审了什么', file: 'pages/collab.js', css: true },
    { id: 'models',  title: '模型', ask: '谁在用什么模型干什么', file: 'pages/models.js', css: true },
    { id: 'system',  title: '体系', ask: '钩子、脚本、文档、检查怎么相互作用', file: 'pages/system.js', css: true },
    { id: 'health',  title: '健康', ask: '系统哪里不对', file: 'pages/health.js', css: true },
    { id: 'library', title: '资料', ask: '文档、结构、决策、通知和脚本都在哪', tabs: [
      { id: 'docs',      title: '文档',     file: 'pages/library-docs.js', css: true },
      { id: 'structure', title: '结构',     file: 'pages/library-structure.js', css: true },
      { id: 'decisions', title: '决策',     file: 'pages/library-decisions.js' },
      { id: 'notices',   title: '通知',     file: 'pages/library-notices.js' },
      { id: 'scripts',   title: '脚本目录', file: 'pages/library-scripts.js', css: true }
    ] }
  ],
  data: [
    { name: 'live',       label: '运行状态',   path: 'data/auto/live.js',       poll: true, every: 15, stale: 90 },
    { name: 'index',      label: '数据索引',   path: 'data/auto/index.js',      poll: true, every: 15, stale: 300 },
    { name: 'attention',  label: '需要你',     path: 'data/auto/attention.js',  keep: true, every: 60, stale: 900 },
    { name: 'health',     label: '健康检查',   path: 'data/auto/health.js',     keep: true, every: 60, stale: 900 },
    { name: 'queue',      label: '施工队列',   path: 'data/auto/queue.js',      keep: true, every: 60, stale: 900 },
    { name: 'pipeline',   label: '流水',       path: 'data/auto/pipeline.js',   every: 60, stale: 900 },
    { name: 'rowhistory', label: '行的历史',   path: 'data/auto/rowhistory.js', every: 300, stale: null },
    { name: 'letters',    label: '来信',       path: 'data/auto/letters.js',    every: 60, stale: 900 },
    { name: 'audits',     label: '收口审计',   path: 'data/auto/audits.js',     every: 60, stale: 900 },
    { name: 'models',     label: '模型',       path: 'data/auto/models.js',     every: 60, stale: 900 },
    { name: 'history',    label: '短期历史',   path: 'data/auto/history.js',    every: 120, stale: 900 },
    { name: 'relations',  label: '体系关系',   path: 'data/auto/relations.js',  every: 600, stale: null },
    { name: 'docs',       label: '文档树',     path: 'data/auto/docs.js',       every: 600, stale: null },
    { name: 'docrefs',    label: '文档引用',   path: 'data/auto/docrefs.js',    every: 600, stale: null },
    { name: 'structure',  label: '项目结构',   path: 'data/auto/structure.js',  every: 600, stale: null },
    { name: 'decisions',  label: '决策',       path: 'data/auto/decisions.js',  every: 600, stale: null },
    { name: 'catalog',    label: '脚本目录',   path: 'data/auto/catalog.js',    every: 300, stale: null },
    { name: 'catalogdetail', label: '脚本详情', path: 'data/auto/catalogdetail.js', every: 300, stale: null },
    { name: 'notices',    label: '通知',       path: 'data/curated/notices.js', every: 60, stale: null }
  ]
};
