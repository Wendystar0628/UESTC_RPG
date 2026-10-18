/* 观察台清单：页面、面板、数据源的唯一登记处。加页面、面板、数据源只改这里。
   pages   页面（顺序即导航顺序）
   panels  面板文件（路径相对 app/）；page 是挂到哪一页；css 为 true 时加载同名 .css
   data    数据源：路径相对 dashboard/；every 刷新间隔（秒）；stale 超过多少秒没更新算"数据旧了"（null 表示不判断） */
window.DashManifest = {
  pages: [
    { id: 'overview',  title: '总览', desc: '现在要不要你动手' },
    { id: 'models',    title: '模型', desc: '谁在跑什么' },
    { id: 'work',      title: '施工', desc: '队列与进度' },
    { id: 'structure', title: '结构', desc: '项目分层与主要目录' },
    { id: 'docs',      title: '文档', desc: '按域分组的文档地图' },
    { id: 'audit',     title: '审计', desc: '等你审的事' },
    { id: 'notices',   title: '通知', desc: '给你的消息' },
    { id: 'decisions', title: '决策', desc: '最近定下来的事' }
  ],
  panels: [
    { id: 'overview',  page: 'overview',  file: 'panels/overview.js',  css: true },
    { id: 'models',    page: 'models',    file: 'panels/models.js',    css: true },
    { id: 'queue',     page: 'work',      file: 'panels/queue.js',     css: true },
    { id: 'structure', page: 'structure', file: 'panels/structure.js', css: true },
    { id: 'docs',      page: 'docs',      file: 'panels/docs.js',      css: true },
    { id: 'audit',     page: 'audit',     file: 'panels/audit.js' },
    { id: 'notices',   page: 'notices',   file: 'panels/notices.js',   css: true },
    { id: 'decisions', page: 'decisions', file: 'panels/decisions.js' }
  ],
  data: [
    { name: 'live',       label: '运行状态', path: 'data/auto/live.js',         every: 15,  stale: 90 },
    { name: 'queue',      label: '施工队列', path: 'data/auto/queue.js',        every: 600, stale: 2400 },
    { name: 'structure',  label: '项目结构', path: 'data/auto/structure.js',    every: 600, stale: 2400 },
    { name: 'docs',       label: '文档地图', path: 'data/auto/docs.js',         every: 600, stale: 2400 },
    { name: 'decisions',  label: '决策',     path: 'data/auto/decisions.js',    every: 600, stale: 2400 },
    { name: 'health',     label: '健康状态', path: 'data/auto/health.js',       every: 600, stale: 2400 },
    { name: 'audit-auto', label: '自动待审', path: 'data/auto/audit-auto.js',   every: 600, stale: 2400 },
    { name: 'notices',    label: '通知',     path: 'data/curated/notices.js',   every: 30,  stale: null },
    { name: 'audit',      label: '审计项',   path: 'data/curated/audit.js',     every: 30,  stale: null }
  ]
};
