/* 学习清单页：作者该先懂的关键点，每条一句话加指向权威文档的链接。
   块（面板 id）：learn-game 游戏效果 · learn-decisions 重大决策 · learn-base 共用基础知识。
   内容是下面的静态数组，不读数据；链接 [编码, 小节号] 在文档页的阅读器里打开，只有编号的不加链接。 */
(function () {
  var h = Dash.h;

  /* refs：['文字', 编码, 小节号?]；没有编码就只显示文字 */
  var GAME = [
    { name: '渲染、碰撞、导航是三份东西', why: '有外观不等于有碰撞，要走得到得另外做。', refs: [['SYS-WALK-BOUNDS §7', 'SYS-WALK-BOUNDS', '7']] },
    { name: '看得见的精度瑕疵只有两类', why: '接缝漏光（T 形接点）与共面闪烁；浮点误差本身看不见。', refs: [['WF-RULE §15', 'WF-RULE', '15'], ['D-290']] },
    { name: '视觉精度底线与分级', why: '越常被近看越密，旁观能飞到的地方也要达标。', refs: [['INV-11', 'PRD-INVARIANTS', '1'], ['ART-FIDELITY', 'ART-FIDELITY', '0']] },
    { name: '三渲二画风底线', why: '分阶明暗、浅透影子、随底色描线、平涂。', refs: [['INV-5', 'PRD-INVARIANTS', '1'], ['ART-STYLE', 'ART-STYLE', '0']] },
    { name: '扫掠与拐角', why: '尖角刺出看得见要修，内角穿插藏在里面不用管。', refs: [['D-290']] },
    { name: '帧数由什么决定', why: '三角形数、绘制调用、材质决定帧数，几何算得多精确不影响。', refs: [['ENG-BUDGET', 'ENG-BUDGET', '0']] },
    { name: '加载等待', why: '热启动、冷启动、预烘缓存各自要等多久，有目标时长。', refs: [['ENG-ARCH §4', 'ENG-ARCH', '4'], ['D-256']] },
    { name: '按格流式加载与就绪', why: '没就绪时冻结物理，到门槛前软停。', refs: [['SYS-WALK-BOUNDS §7', 'SYS-WALK-BOUNDS', '7'], ['ENG-PERF-STREAM §1', 'ENG-PERF-STREAM', '1']] },
    { name: '出错时玩家看到什么', why: '启动失败页，重试走引擎原生重启。', refs: [['SYS-TITLE §1', 'SYS-TITLE', '1'], ['D-288']] }
  ];

  var DECISIONS = [
    { name: '不要求全局高精度', why: '建模细节不简化，外面看着没问题就不管内部形态。', refs: [['D-290']] },
    { name: '不为看不出的误差换精确算法', why: '那是过度工程化。', refs: [['D-289']] },
    { name: '补丁看设计依据', why: '判断一个补丁看有没有设计依据，不看用词。', refs: [['D-270'], ['D-271']] },
    { name: '工程不自我繁殖', why: '审计只报有实际后果的，门禁加规则要有逃逸失败。', refs: [['D-291']] },
    { name: '内容开发的节奏归作者', why: '工程排期由指挥定，内容节奏你说了算。', refs: [['D-292']] },
    { name: '启动路径只读不建', why: '启动只读现成结果，该建的在构建期建好。', refs: [['D-256']] },
    { name: '审美以作者为准', why: '地图只改项目世界文件，不取外部地图。', refs: [['D-252'], ['INV-9', 'PRD-INVARIANTS', '1']] },
    { name: '游戏失败按类别登记', why: '在健康页看哪一类失败多。', refs: [['D-285']] },
    { name: '数值按数量级判断', why: '同一量级（不到 10 倍）又不挡体验就算合理。', refs: [['D-225']] }
  ];

  var BASE = [
    { name: '构建期与运行时', why: '能在烘焙时算好的不放到运行时，启动只读现成结果。', refs: [['ENG-ARCH §4', 'ENG-ARCH', '4']] },
    { name: '缓存判鲜', why: '按构建输入决定要不要重烘，改了生成代码会整片重烘。', refs: [['ENG-ARCH §4', 'ENG-ARCH', '4']] },
    { name: '网格基础', why: '顶点、三角形、法线、绕序；闭合与流形是什么、为什么这里不管。', refs: [['D-290']] },
    { name: '浮点精度的量级', why: '千米级坐标上单精度约 0.1–0.2 毫米，远小于一个像素。', refs: [['WF-RULE §15', 'WF-RULE', '15']] },
    { name: '失败即报错', why: '吞掉异常会变成无声失败。', refs: [['STD-CODE §4', 'STD-CODE', '4']] },
    { name: '测试三档', why: '快检、核心专项、集中测试，各管什么。', refs: [['ENG-GATES §1', 'ENG-GATES', '1']] },
    { name: '依赖只朝下', why: '上层可以用下层，反过来不行；生成层是纯 .NET。', refs: [['ENG-ARCH §2', 'ENG-ARCH', '2']] },
    { name: '确定性', why: '同机同输入同输出，缓存与存档都靠它。', refs: [['ENG-ARCH §2', 'ENG-ARCH', '2']] }
  ];

  /* 决策号在哪一卷登记；点了在那卷里定位到这一条 */
  var DECISION_VOLUME = { 'D-256': 'REG-DECISIONS-SYSTEMS', 'D-288': 'REG-DECISIONS-SYSTEMS', 'D-289': 'REG-DECISIONS-SYSTEMS' };

  function refNode(r) {
    var open = r[1] ? function () { Dash.openDoc(r[1], { sec: r[2] }); }
      : /^D-\d+$/.test(r[0]) ? function () { Dash.openDoc(DECISION_VOLUME[r[0]] || 'REG-DECISIONS-PROCESS', { find: r[0] }); }
      : null;
    if (!open) return h('span.id.muted', r[0]);
    return h('button.btn.learn-ref', { type: 'button', on: { click: open } }, r[0]);
  }

  function list(items) {
    return h('div.learn-list', items.map(function (it) {
      return h('div.item.learn-item',
        h('div.item-title', h('b', it.name), h('span.learn-why', ' ' + it.why)),
        h('div.learn-refs', it.refs.map(refNode)));
    }));
  }

  function reg(id, title, note, order, items) {
    Dash.panel({ id: id, page: 'learn', title: title, note: note, span: 12, order: order, needs: [],
      render: function (el) { el.appendChild(list(items)); } });
  }

  reg('learn-game', '游戏效果', '玩家看得见、玩得到的', 10, GAME);
  reg('learn-decisions', '重大决策', '定下来就不再回头争的取舍', 20, DECISIONS);
  reg('learn-base', '共用基础知识', '读文档和审工程时都会用到的底子', 30, BASE);
})();
