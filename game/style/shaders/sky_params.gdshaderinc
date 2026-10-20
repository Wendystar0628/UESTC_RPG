// ---------------------------------------------------------------------------
// 每帧由 SkyNode 写入（只在变化时写）
// ---------------------------------------------------------------------------
uniform float cloud_cover = 0.0;   // 最终云量 0–1（IColorScriptFrame.CloudCover，已含天气改写）
uniform float cloud_thr = 1.0;     // 当前云量对应的积云密度阈值（按 cloud_noise 的 R 通道直方图求出）

// ---------------------------------------------------------------------------
// 可调参数（集中在这里，评审定值；C# 一侧的在 SkyTuning.cs）
// ---------------------------------------------------------------------------
// 日盘：直径放大到真实的 1.5 倍（ART-SKYLIGHT）
const float SUN_RADIUS_DEG = 0.40;
const float SUN_LOW_FROM = 3.0;      // 太阳低于这个高度：橙红硬边圆盘
const float SUN_LOW_TO = 20.0;       // 高于这个高度：白金色、柔光晕开
const float SUN_LOW_ORANGE = 0.30;   // 低悬时向 sky.sun.1 的偏移
const float SUN_LOW_GAIN = 1.45;
const float SUN_HIGH_GAIN = 1.7;     // 高太阳略超 1，交给后期柔光晕开
const float SUN_HALO_LOW = 0.18;
const float SUN_HALO_HIGH = 0.55;
// 月盘：直径约为真实的 2 倍（ART-SKYLIGHT），只带一圈很淡的光晕
const float MOON_RADIUS_DEG = 0.52;
const float MOON_GAIN = 1.05;
const float MOON_HALO = 0.07;
const float MOON_MARIA = 0.86;       // 两三块淡灰斑的明度系数
const float MOON_LIMB = 0.93;        // 外圈一阶的明度系数（只为看出球面）
// 星空：立方体面网格，每格至多一颗（ART-SKYLIGHT）；上半球约 280 颗
const float STAR_GRID = 16.0;
const float STAR_P = 0.36;
const float STAR_GAIN = 1.0;
// 积云层（SYS-WEATHER）：云底 1.5 km 平面，团块高度场 = (密度 − 阈值) × 高度系数
const float CU_BASE = 1500.0;
// 云体 = { 密度(xz) − 阈值 − 剖面(高度) > 0 }，云底 y = CU_BASE 处平切：
// 剖面在云底略收（下沿往里收一点，侧面在 CU_BULGE_Y 处鼓出），往上按 ((y − 鼓出高)/顶高)² 收成穹顶，
// 所以团块越密越高，团块之间互相压出大小层次；侧面是真的三维法线，不是贴纸
const float CU_BULGE_Y = 120.0;      // 侧面最鼓的高度（m）
const float CU_BULGE_B = 0.035;      // 云底处剖面收进的量（密度单位）
const float CU_H_TOP = 1100.0;       // 密度高出阈值 1 时的穹顶高（m）；高 = 鼓出高 + 顶高 · √(密度 − 阈值)
const float CU_FLAT_FROM = 9000.0;   // 远处的云压扁成"几缕"，近地平线不挤成一排小馒头
const float CU_FLAT_TO = 32000.0;
const float CU_FLAT_FAR = 0.35;
const float CU_FAR_THIN = 0.10;      // 远处再收一点云量：地平线上留几缕，不排成一整排
const float CU_TILE = 24000.0;       // cloud_noise 一次平铺覆盖的米数
const float CU_LOBE_REPEAT = 1.5;    // 中团块（B 通道 ×1.5，格约 0.67 km）
const float CU_LOBE_LOD = 0.585;     // log2(1.5)
const float CU_LOBE_AMP = 0.22;
const float CU_DETAIL_REPEAT = 4.0;  // 小团块（B 通道 ×4，格约 0.25 km）
const float CU_DETAIL_AMP = 0.10;
const float CU_BUMP_MID = 0.45;      // 团块通道的"零点"：高于它鼓起，低于它凹下
const float CU_MAX_DIST = 50000.0;
const float CU_FADE_FROM = 20000.0;
const float CU_AP_DIST = 30000.0;    // 大气透视：越远越接近当时的霾色
const float CU_AP_MAX = 0.75;
const int CU_MARCH_MAX = 96;         // 步进上限
const float CU_SAFE_M = 260.0;       // 密度差 1 对应的安全步长（m）
const float CU_STEP_MIN = 12.0;      // 最小步长（m），随距离按 CU_STEP_GROW 放大
const float CU_STEP_GROW = 0.004;
const float CU_STEP_MAX = 600.0;
const int CU_REFINE = 6;
const float CU_LIGHT_LIFT = 0.45;    // 低太阳时把光向上抬一点，团块顶仍受光
const float CU_CAV_D = 110.0;        // 最暗处：沿法线往外探的距离（m）
const float CU_CAV_EPS = 0.004;
const float CU_T_LIT = 0.12;
const float CU_BAND_W = 0.025;       // 阶与阶之间的过渡半宽的下限（硬边，只够抗锯齿）
// 阶的交界按像素抗锯齿（40b）：半分辨率里阶与阶的交界若是一像素一判的硬切（挡光、凹褶、云底与侧面），
// 双线性放大后就是一级一级的台阶。所以每个交界都按"一个半分辨率像素里判据变多少"放成连续的过渡，
// 再由双线性放大成全分辨率约 2 px 的干净柔边
const float CU_TONE_AA = 1.0;        // 交界的过渡宽度（半分辨率像素）
const float CU_TONE_GRAZE = 0.25;    // 估一像素在云面上走多远时 |n·视线| 的下限（越贴着视线的面走得越远）
const float CU_TERM_PX = 2.0;        // 估受光交界（n·l）每像素变化量时，沿受光方向探出去的像素数
const float CU_SHADOW_D1 = 160.0;    // 团块之间的自遮挡：朝光方向取两点
const float CU_SHADOW_D2 = 480.0;
const float CU_EDGE_PX = 1.0;        // 半分辨率里覆盖率从 0.5 走到 1（或 0）跨的像素数
const float CU_EDGE_SLACK = 0.002;   // 判"离云够近"时多放的一点密度余量
const int CU_EDGE_SEARCH = 5;        // 没进云的光线：找最近点的三分搜索次数
const float CU_EDGE_SHARP = 0.75;    // 全分辨率收边的过渡宽度（×覆盖率梯度，约 1 px）
const float CU_RIM_PX = 1.3;         // 金边与下沿高光的宽度（全分辨率像素）
const float CU_RIM_FAR = 0.45;
// 金边与下沿高光"朝不朝光、是不是下沿"的判据（40b）：在四分之一分辨率通道里按云面的三维法线投到屏幕上求，
// 双线性取样后是一片连续的场；不用覆盖率的屏幕梯度，那个梯度在半分辨率格子上按格子方向量化，亮边会断成一串点
const float CU_RIM_Q_PX = 2.0;       // 四分之一分辨率里，离云多少像素以内还记下最近云面的朝向       // 背向光源一侧的云，金边的强度
const float CU_BEHIND_M1 = 160.0;    // 判"后面还是云"的两个探点（m）
const float CU_BEHIND_M2 = 450.0;
const float CU_GRAZE_FROM = 0.06;    // |n·视线| 小于它的面不单独受光（轮廓细条交给金边）
const float CU_GRAZE_TO = 0.22;
const float CU_HL_SUN_FROM = 12.0;   // 太阳低于这个高度才有（夜里按月亮算）
const float CU_HL_SUN_TO = 25.0;
// 火烧云：太阳贴着地平线（约 −4° 到 4°）时，薄的云底边被从下方点燃（SYS-WEATHER"低太阳时云腹被点燃"）
const float CU_UNDER_REACH = 320.0;  // 云底朝太阳一侧被点燃的宽度（m）
const float CU_UNDER_MIX = 0.75;
// 层云盖（阴、雨，ART-SKYLIGHT）
const float DECK_Y = 1150.0;
const float DECK_TILE = 48000.0;
const float DECK_MAX_DIST = 70000.0;
const float DECK_AP_DIST = 22000.0;
const float DECK_LIT_MIX = 0.30;
const float DECK_SHADE_MIX = 0.22;
const float DECK_GAP_W = 0.010;      // 亮缝宽度（低频场的等值线带宽）
const float DECK_GAP_RIM = 1.9;      // 缝边银边的外径倍数
const float DECK_RAIN_DIM = 0.07;
const float DECK_CELL_REPEAT = 6.0;  // 层积云胞（B 通道 ×6，格约 0.33 km）
const float DECK_CELL_EDGE = 0.22;
const float DECK_CELL_MIX = 0.16;

