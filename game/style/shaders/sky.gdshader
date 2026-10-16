// sky.gdshader：全日天空（WP-40）
// 规格：ART-SKYLIGHT §4、§5.3；SYS-TIME §3.2；SYS-WEATHER §2、§5、§7；ENG-PERF §1.4、§1.5。
// 主线版：五段渐变（太阳侧 / 背阳侧，夜间换月侧）、霾带、日盘与柔光、月盘（球面明暗与月晕）、星空、
// 半分辨率云层（四分之一分辨率通道只记轮廓朝向，给金边与下沿高光用；积云：1.5 km 高的云底平面 + 高度场团块，硬边三阶；层云：满覆盖、两阶、留亮缝）。
// 颜色只读调色板纹理第 0 行（ENG-PERF §1.5 的行布局，色号与 game/style/palette/core.json 一致），着色器里不写颜色。
// 不用内置 TIME；时间只读 anim_time、step_rate；日月方向读全局参数里的真实方向（sun_dir、moon_dir），不读 LIGHT0。
// 当前帧的云量与阈值由 SkyNode 写进本材质的两个 uniform（云量来自 IColorScriptFrame，不从脚本读回全局参数）。
// 后置细节（ENG-WP §2.1）：银河、城市天光方位、九项光效、远处雨幕与云内闪、彩虹、积雨云卡片。

shader_type sky;
render_mode use_half_res_pass, use_quarter_res_pass;

#include "res://style/shaders/globals.gdshaderinc"
#include "res://style/shaders/fog.gdshaderinc"

// ---------------------------------------------------------------------------
// 每帧由 SkyNode 写入（只在变化时写）
// ---------------------------------------------------------------------------
uniform float cloud_cover = 0.0;   // 最终云量 0–1（IColorScriptFrame.CloudCover，已含天气改写）
uniform float cloud_thr = 1.0;     // 当前云量对应的积云密度阈值（按 cloud_noise 的 R 通道直方图求出）

// ---------------------------------------------------------------------------
// 可调参数（集中在这里，评审定值；C# 一侧的在 SkyTuning.cs）
// ---------------------------------------------------------------------------
// 日盘：直径放大到真实的 1.5 倍（ART-SKYLIGHT §2.3）
const float SUN_RADIUS_DEG = 0.40;
const float SUN_LOW_FROM = 3.0;      // 太阳低于这个高度：橙红硬边圆盘
const float SUN_LOW_TO = 20.0;       // 高于这个高度：白金色、柔光晕开
const float SUN_LOW_ORANGE = 0.30;   // 低悬时向 sky.sun.1 的偏移
const float SUN_LOW_GAIN = 1.45;
const float SUN_HIGH_GAIN = 1.7;     // 高太阳略超 1，交给后期柔光晕开
const float SUN_HALO_LOW = 0.18;
const float SUN_HALO_HIGH = 0.55;
// 月盘：直径约为真实的 2 倍（ART-SKYLIGHT §5.3），只带一圈很淡的光晕
const float MOON_RADIUS_DEG = 0.52;
const float MOON_GAIN = 1.05;
const float MOON_HALO = 0.07;
const float MOON_MARIA = 0.86;       // 两三块淡灰斑的明度系数
const float MOON_LIMB = 0.93;        // 外圈一阶的明度系数（只为看出球面）
// 星空：立方体面网格，每格至多一颗（ART-SKYLIGHT §4.11）；上半球约 280 颗
const float STAR_GRID = 16.0;
const float STAR_P = 0.36;
const float STAR_GAIN = 1.0;
// 积云层（SYS-WEATHER §7）：云底 1.5 km 平面，团块高度场 = (密度 − 阈值) × 高度系数
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
// 火烧云：太阳贴着地平线（约 −4° 到 4°）时，薄的云底边被从下方点燃（SYS-WEATHER §2"低太阳时云腹被点燃"）
const float CU_UNDER_REACH = 320.0;  // 云底朝太阳一侧被点燃的宽度（m）
const float CU_UNDER_MIX = 0.75;
// 层云盖（阴、雨，ART-SKYLIGHT §4.12）
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

// ---------------------------------------------------------------------------
// 五段渐变、霾带、定侧（太阳侧 / 背阳侧，入夜换月侧）、OKLab 换算与调色板取色都在 fog.gdshaderinc 里，
// 与共用雾函数、玻璃映天同一份（ART-SKYLIGHT §4.1–§4.3、§6.5），地平线处天色等于雾色。
// 五段的仰角 [0°, 4°, 12°, 28°, 90°] 是那里的 SKY_STOP_DEG。
// ---------------------------------------------------------------------------
float aastep(float edge, float x) {
	float w = max(fwidth(x), 1e-5);
	return smoothstep(edge - w, edge + w, x);
}

// ---------------------------------------------------------------------------
// 日盘、月盘、星空（全分辨率）
// ---------------------------------------------------------------------------
vec3 add_sun(vec3 c, vec3 dir, float px, float w_sun) {
	vec3 s = sky_sun_vec();
	float h = sky_alt_deg(s);
	if (h < -2.0 * SUN_RADIUS_DEG) {
		return c;
	}
	float chord = length(dir - s);
	float r = radians(SUN_RADIUS_DEG);
	float low = 1.0 - smoothstep(SUN_LOW_FROM, SUN_LOW_TO, h);
	vec3 light = pal_cur(PAL_SUN_LIGHT);
	vec3 disc = mix(light * SUN_HIGH_GAIN, mix(light, pal_cur(PAL_SKY_SUN0 + 1), SUN_LOW_ORANGE) * SUN_LOW_GAIN, low);
	float above = smoothstep(-px, px, dir.y);
	float fogk = sky_fog_weather_k();
	// 雾天：一枚隔雾可见的淡白圆，不带光晕（ART-SKYLIGHT §2.3）
	disc = mix(disc, mix(pal_cur(PAL_WEATHER_FOG), light, 0.35) * 1.05, fogk);
	// 低于霾带时日盘被霾染色、压暗
	float e = degrees(asin(clamp(dir.y, -1.0, 1.0)));
	float hz = exp2(-max(e, 0.0) / (0.5 * sky_haze_deg()));
	disc = mix(disc, mix(disc, sky_haze_color(w_sun), 0.55), hz);
	float halo_amp = mix(SUN_HALO_HIGH, SUN_HALO_LOW, low) * (1.0 - fogk);
	float halo_w = radians(mix(4.5, 1.6, low));
	c += light * halo_amp * exp(-chord / halo_w) * above;
	float k = (1.0 - smoothstep(r - px, r + px, chord)) * above;
	return mix(c, disc, k);
}

vec3 moon_color() {
	vec3 m = pal_cur(PAL_SKY_MOON);
	if (dot(m, vec3(1.0)) < 0.03) {
		float low = 1.0 - smoothstep(4.0, 30.0, sky_alt_deg(sky_moon_vec()));
		m = mix(pal_cur(PAL_WINDOW_COOL), pal_cur(PAL_WINDOW_WARM), mix(0.18, 0.45, low));
	}
	return m * MOON_GAIN;
}

vec3 add_moon(vec3 c, vec3 dir, float px) {
	vec3 m = sky_moon_vec();
	if (m.y < -0.02) {
		return c;
	}
	float r = radians(MOON_RADIUS_DEG);
	vec3 off = dir - m;
	float chord = length(off);
	vec3 col = moon_color();
	float above = smoothstep(-px, px, dir.y);
	// 月晕：一圈很淡的光晕，不出镜头光斑（ART-STYLE §7.3）
	c += col * MOON_HALO * (exp(-chord / radians(2.2)) + 0.4 * exp(-chord / radians(9.0))) * above;
	if (chord > r + 2.0 * px) {
		return c;
	}
	vec3 t = normalize(cross(m, vec3(0.0, 1.0, 0.0)) + vec3(1e-5, 0.0, 0.0));
	vec3 b = cross(t, m);
	vec2 q = vec2(dot(off, t), dot(off, b)) / r;
	float z = sqrt(max(1.0 - dot(q, q), 0.0));
	vec3 n = t * q.x + b * q.y - m * z;
	vec3 lit_dir = length(moon_lit.xyz) > 0.5 ? normalize(moon_lit.xyz) : -m;
	float lit = smoothstep(-0.03, 0.03, dot(n, lit_dir));
	// 月面只画两三块淡灰斑，不画写实月面
	float maria = max(max(
		1.0 - smoothstep(0.26, 0.30, length(q - vec2(-0.28, 0.22))),
		1.0 - smoothstep(0.16, 0.20, length(q - vec2(0.22, 0.34)))),
		1.0 - smoothstep(0.19, 0.23, length(q - vec2(0.06, -0.26))));
	float limb = 1.0 - smoothstep(0.86, 0.90, length(q));
	vec3 disc = col * mix(1.0, MOON_MARIA, maria) * mix(MOON_LIMB, 1.0, limb);
	// 背光的一侧不画成黑：只比周围的天亮一点点
	disc = mix(mix(c, disc, 0.12), disc, lit);
	float k = (1.0 - smoothstep(r - px, r + px, chord)) * above;
	return mix(c, disc, k);
}

uvec3 pcg3(uvec3 v) {
	v = v * 1664525u + 1013904223u;
	v.x += v.y * v.z;
	v.y += v.z * v.x;
	v.z += v.x * v.y;
	v ^= v >> 16u;
	v.x += v.y * v.z;
	v.y += v.z * v.x;
	v.z += v.x * v.y;
	return v;
}

vec3 face_dir(int face, vec2 uv) {
	if (face == 0) { return normalize(vec3(1.0, uv.y, uv.x)); }
	if (face == 1) { return normalize(vec3(-1.0, uv.y, uv.x)); }
	if (face == 2) { return normalize(vec3(uv.x, 1.0, uv.y)); }
	if (face == 3) { return normalize(vec3(uv.x, -1.0, uv.y)); }
	if (face == 4) { return normalize(vec3(uv.x, uv.y, 1.0)); }
	return normalize(vec3(uv.x, uv.y, -1.0));
}

float star_field(vec3 dir, float px, out vec3 tint) {
	tint = vec3(1.0);
	vec3 a = abs(dir);
	int face = 0;
	vec2 uv = vec2(0.0);
	if (a.x >= a.y && a.x >= a.z) {
		face = dir.x > 0.0 ? 0 : 1;
		uv = dir.zy / a.x;
	} else if (a.y >= a.z) {
		face = dir.y > 0.0 ? 2 : 3;
		uv = dir.xz / a.y;
	} else {
		face = dir.z > 0.0 ? 4 : 5;
		uv = dir.xy / a.z;
	}
	vec2 g = (uv * 0.5 + 0.5) * STAR_GRID;
	vec2 cell = floor(g);
	uvec3 h = pcg3(uvec3(uint(cell.x), uint(cell.y), uint(face) + 977u));
	vec3 rnd = vec3(h & uvec3(65535u)) / 65535.0;
	if (rnd.x > STAR_P) {
		return 0.0;
	}
	vec2 j = vec2(fract(rnd.y * 7.31), fract(rnd.z * 3.17)) * 0.6 + 0.2;
	vec2 suv = (cell + j) / STAR_GRID * 2.0 - 1.0;
	vec3 sd = face_dir(face, suv);
	float bright = pow(fract(rnd.y * 13.7 + rnd.z * 5.3), 3.0);
	float size = mix(0.75, 1.9, bright);
	float chord = length(dir - sd);
	float k = 1.0 - smoothstep(px * size * 0.5, px * size * 1.25, chord);
	// 闪烁按一拍二步进；频率取 480 s 的整数倍，anim_time 回卷时不跳
	float steps = max(step_rate, 1.0);
	float ts = floor(anim_time * steps) / steps;
	float f = TAU * floor(mix(70.0, 190.0, rnd.z)) / 480.0;
	float tw = 0.78 + 0.22 * sin(ts * f + rnd.x * 40.0);
	tint = mix(pal_cur(PAL_WINDOW_COOL), pal_cur(PAL_WINDOW_WARM), fract(rnd.z * 11.1) * 0.35);
	return k * mix(0.35, 1.0, bright) * tw;
}

// ---------------------------------------------------------------------------
// 云（半分辨率通道）
// ---------------------------------------------------------------------------
// 云的密度：云体（R）+ 两个尺度的圆团块（B）。团块是一组圆顶取并集，叠在云体表面上就是花椰菜顶和塔肩，
// 大小层次来自两个尺度；云体本身越大越高
float cu_field(vec2 xz, float lod) {
	vec2 uv = (xz + cloud.xy) / CU_TILE;
	float d = textureLod(cloud_noise, uv, lod).r;
	float lobe = textureLod(cloud_noise, uv * CU_LOBE_REPEAT + vec2(0.13, 0.29), lod + CU_LOBE_LOD).b;
	float det = textureLod(cloud_noise, uv * CU_DETAIL_REPEAT + vec2(0.37, 0.61), lod + 2.0).b;
	return d + (lobe - CU_BUMP_MID) * CU_LOBE_AMP + (det - CU_BUMP_MID) * CU_DETAIL_AMP;
}

float cu_flat(float t) {
	return mix(1.0, CU_FLAT_FAR, smoothstep(CU_FLAT_FROM, CU_FLAT_TO, t));
}

// 剖面 b(y') 与它的导数（y' 是云底以上的高度）
float cu_prof(float yb, float flat_k) {
	float ybul = CU_BULGE_Y * flat_k;
	if (yb < ybul) {
		float u = 1.0 - yb / ybul;
		return CU_BULGE_B * u * u;
	}
	float v = (yb - ybul) / (CU_H_TOP * flat_k);
	return v * v;
}

float cu_dprof(float yb, float flat_k) {
	float ybul = CU_BULGE_Y * flat_k;
	if (yb < ybul) {
		return -2.0 * CU_BULGE_B * (1.0 - yb / ybul) / ybul;
	}
	float htop = CU_H_TOP * flat_k;
	return 2.0 * (yb - ybul) / (htop * htop);
}

// > 0 在云里
float cu_inside(vec3 p, float lod, float flat_k) {
	float yb = p.y - CU_BASE;
	if (yb < 0.0) {
		return -1.0;
	}
	return cu_field(p.xz, lod) - cloud_thr - cu_prof(yb, flat_k) - CU_FAR_THIN * (1.0 - flat_k) / (1.0 - CU_FLAT_FAR);
}

// 三阶上色：受光 / 云腹 / 最暗处。背着光的面是云腹；被前面的团块挡住光、又背着光的地方压到最暗（前后团块分出层次）。
// w：一个半分辨率像素里云体函数大约变多少（密度单位），各个"在不在云里"的判据都按它放成约一像素宽的连续过渡
float cu_soft_in(float f, float w) {
	return smoothstep(-w, w, f);
}

// 云底平涂：云腹一阶；太阳贴着地平线时，云底的薄边被从下方点燃（火烧云）
vec3 cu_tone_base(vec3 l, vec3 p, float lod, float flat_k, float w, out float lit_k) {
	vec3 belly = pal_cur(PAL_SKY_CLOUD_BELLY);
	vec3 lit = pal_cur(PAL_SKY_CLOUD_LIT);
	float h = sky_alt_deg(l);
	float under = smoothstep(-4.0, -1.0, h) * (1.0 - smoothstep(1.0, 4.5, h));
	lit_k = 0.0;
	if (under <= 0.0) {
		return belly;
	}
	// 只点燃朝太阳的那一圈云底：往太阳方向走 CU_UNDER_REACH 就出了云底的地方
	vec2 lh = normalize(l.xz + vec2(1e-5));
	vec3 q = vec3(p.x + lh.x * CU_UNDER_REACH, CU_BASE + 1.0, p.z + lh.y * CU_UNDER_REACH);
	float edge = 1.0 - cu_soft_in(cu_inside(q, lod, flat_k), w);
	lit_k = under * edge;
	return mix(belly, mix(lit, pal_cur(PAL_SKY_CLOUD_RIM), 0.4), lit_k * CU_UNDER_MIX);
}

// 侧面三阶。dt：沿受光方向走一个像素，n·l 大约变多少（受光交界的过渡宽度）
vec3 cu_tone_side(vec3 n, vec3 l, vec3 p, float lod, float flat_k, float graze, float w, float dt, out float lit_k) {
	vec3 deep = pal_cur(PAL_SKY_CLOUD_DEEP);
	vec3 belly = pal_cur(PAL_SKY_CLOUD_BELLY);
	vec3 lit = pal_cur(PAL_SKY_CLOUD_LIT);
	vec3 lw = normalize(l + vec3(0.0, CU_LIGHT_LIFT, 0.0));
	float t = dot(n, lw);
	// 受光：朝光、且朝光方向上没有别的团块挡着（被挡住的退成云腹，前后团块分出层次）
	float occ = max(cu_soft_in(cu_inside(p + lw * CU_SHADOW_D1, lod, flat_k), w),
		cu_soft_in(cu_inside(p + lw * CU_SHADOW_D2, lod, flat_k), w));
	// 几乎贴着视线的面（轮廓上的细条）不单独受光，交给全分辨率的金边，免得半分辨率里断成碎点
	float bw = max(CU_BAND_W, dt * CU_TONE_AA);
	lit_k = smoothstep(CU_T_LIT - bw, CU_T_LIT + bw, t) * (1.0 - occ) * graze;
	// 最暗处：背光一侧团块与团块相夹的褶（沿法线往外一点仍在云里，就是凹处），勾出花椰菜的大小层次
	float cav = cu_soft_in(cu_inside(p + n * CU_CAV_D, lod, flat_k) + CU_CAV_EPS, w);
	float deep_k = cav * (1.0 - lit_k);
	vec3 c = mix(belly, deep, deep_k);
	return mix(c, lit, lit_k);
}

// base_k：1 是云底平面，0 是侧面；云底的外沿按像素过渡到侧面，云底与侧面的交界不起台阶
vec3 cu_tone(vec3 n, vec3 l, vec3 p, float lod, float flat_k, float base_k, float graze, float w_side, float w_base, float dt,
		out float lit_k) {
	vec3 c = vec3(0.0);
	lit_k = 0.0;
	if (base_k < 1.0) {
		c = cu_tone_side(n, l, p, lod, flat_k, graze, w_side, dt, lit_k);
	}
	if (base_k > 0.0) {
		float lk_b;
		vec3 cb = cu_tone_base(l, p, lod, flat_k, w_base, lk_b);
		c = mix(c, cb, base_k);
		lit_k = mix(lit_k, lk_b, base_k);
	}
	return c;
}

// 云体函数的梯度（水平取密度的梯度，用低一级细节；竖直取剖面的导数）
vec3 cu_grad(vec3 p, float lod, float flat_k, float e) {
	float dx = (cu_field(p.xz + vec2(e, 0.0), lod) - cu_field(p.xz - vec2(e, 0.0), lod)) / (2.0 * e);
	float dz = (cu_field(p.xz + vec2(0.0, e), lod) - cu_field(p.xz - vec2(0.0, e), lod)) / (2.0 * e);
	return vec3(dx, -cu_dprof(p.y - CU_BASE, flat_k), dz);
}

// 返回预乘的颜色与"边缘覆盖率"：轮廓处 0.5，往里 1、往外 0，跨约两个半分辨率像素连续变化。
// 全分辨率那一步双线性取样后在 0.5 处按约 1 px 收成硬边，金边与下沿高光也在那一步按覆盖率的梯度画
// geom_only：四分之一分辨率通道用，只求轮廓的朝向（金边、下沿高光的判据），sx、sy 是本通道一个像素的视线增量
vec4 cumulus(vec3 dir, vec3 cam, float pix, vec3 haze, bool geom_only, vec3 sx, vec3 sy) {
	if (cloud_cover <= 0.001 || cloud_thr >= 0.999 || dir.y < 0.012) {
		return vec4(0.0);
	}
	float cy = min(cam.y, CU_BASE - 50.0);
	float top = CU_BASE + CU_BULGE_Y + CU_H_TOP * sqrt(max(1.02 - cloud_thr, 0.0));
	float t0 = (CU_BASE - cy) / dir.y;
	if (t0 > CU_MAX_DIST) {
		return vec4(0.0);
	}
	float t1 = min((top - cy) / dir.y, CU_MAX_DIST);
	vec3 o = vec3(cam.x, cy, cam.z);
	float texel_m = CU_TILE / 512.0;
	// 自适应步进：离云越远步子越大（按密度场的变化率估出的安全距离），贴近表面时步子缩小，
	// 所以轮廓不会按固定高度切成一片一片；进云以后再二分细化。没进云的光线记下离云最近的一点，给轮廓外侧的覆盖率用
	int hit = -1;
	float ta = t0;
	float tb = t0;
	float t = t0;
	float f_best = -1e9;
	float t_best = t0;
	for (int i = 0; i < CU_MARCH_MAX; i++) {
		vec3 p = o + dir * t;
		p.y = max(p.y, CU_BASE);
		float lod = log2(max(t * pix / texel_m, 1.0));
		float f = cu_inside(p, lod, cu_flat(t));
		if (f > 0.0) {
			hit = i;
			tb = t;
			break;
		}
		if (f > f_best) {
			f_best = f;
			t_best = t;
		}
		ta = t;
		t += clamp(-f * CU_SAFE_M, CU_STEP_MIN + t * CU_STEP_GROW, CU_STEP_MAX);
		if (t > t1) {
			break;
		}
	}
	bool is_base = hit == 0;
	float th = tb;
	if (hit > 0) {
		for (int k = 0; k < CU_REFINE; k++) {
			float tm = 0.5 * (ta + tb);
			vec3 pm = o + dir * tm;
			float lodm = log2(max(tm * pix / texel_m, 1.0));
			if (cu_inside(pm, lodm, cu_flat(tm)) > 0.0) {
				tb = tm;
			} else {
				ta = tm;
			}
		}
		th = tb;
	} else if (hit < 0) {
		// 没进云：离云还远的直接返回；近的在最近点两侧三分搜索，找准最近的一点
		float pix_b = t_best * pix;
		float reach_px = geom_only ? CU_RIM_Q_PX : CU_EDGE_PX;
		if (f_best < -(reach_px + 1.0) * pix_b / CU_SAFE_M - CU_EDGE_SLACK) {
			return vec4(0.0);
		}
		float span = CU_STEP_MIN + t_best * CU_STEP_GROW;
		float lo = max(t_best - span, t0);
		float hi = t_best + span;
		for (int k = 0; k < CU_EDGE_SEARCH; k++) {
			float m1 = mix(lo, hi, 0.382);
			float m2 = mix(lo, hi, 0.618);
			vec3 p1 = o + dir * m1;
			vec3 p2 = o + dir * m2;
			p1.y = max(p1.y, CU_BASE);
			p2.y = max(p2.y, CU_BASE);
			float l1 = log2(max(m1 * pix / texel_m, 1.0));
			float l2 = log2(max(m2 * pix / texel_m, 1.0));
			if (cu_inside(p1, l1, cu_flat(m1)) > cu_inside(p2, l2, cu_flat(m2))) {
				hi = m2;
			} else {
				lo = m1;
			}
		}
		th = 0.5 * (lo + hi);
	}
	vec3 p = o + dir * th;
	p.y = max(p.y, CU_BASE);
	float lod = log2(max(th * pix / texel_m, 1.0));
	float flat_k = cu_flat(th);
	float pix_m = max(th * pix, 1.0);
	vec3 grad = cu_grad(p, lod + 1.0, flat_k, max(texel_m * 2.0, pix_m * 1.5));
	float glen = max(length(grad), 1e-7);
	// 边缘覆盖率：进云的光线看它往里穿了多深（短短一截就穿出去的是擦着轮廓的），没进云的看离云多近，都折成像素
	float cover;
	if (hit >= 0) {
		float pen = max(cu_inside(p + dir * pix_m * 0.75, lod, flat_k), cu_inside(p + dir * pix_m * 2.0, lod, flat_k));
		cover = clamp(0.5 + 0.5 * max(pen, 0.0) / (glen * pix_m * CU_EDGE_PX), 0.5, 1.0);
		// 前面的团块擦边穿出、后面还是云：这是云里面的轮廓，不是云和天的边，按满覆盖算
		float behind = max(max(cu_inside(p + dir * pix_m * 5.0, lod, flat_k), cu_inside(p + dir * CU_BEHIND_M1, lod, flat_k)),
			cu_inside(p + dir * CU_BEHIND_M2, lod, flat_k));
		cover = behind > 0.0 ? 1.0 : cover;
	} else {
		float f = cu_inside(p, lod, flat_k);
		cover = clamp(0.5 + 0.5 * f / (glen * pix_m * (geom_only ? CU_RIM_Q_PX : CU_EDGE_PX)), 0.0, 0.5);
		if (cover <= 0.001) {
			return vec4(0.0);
		}
	}
	// 侧面的法线：云底的点也取它（云底外沿往侧面过渡时用）
	vec3 n = -grad / glen;
	if (geom_only) {
		// 轮廓朝外的方向 = 法线投到屏幕上；光的屏幕方向 = dot(视线, 光) 增大的方向；上 = 视线 y 增大的方向
		vec2 n_scr = vec2(dot(n, sx), dot(n, sy));
		vec3 lq = sky_night_k() < 0.5 ? sky_sun_vec() : sky_moon_vec();
		vec2 l_scr = vec2(dot(lq, sx), dot(lq, sy));
		vec2 up_scr = vec2(sx.y, sy.y);
		float nl = length(n_scr);
		float facing_q = nl > 1e-6 && length(l_scr) > 1e-9 ? smoothstep(0.05, 0.55, dot(n_scr / nl, normalize(l_scr))) : 0.0;
		float bottom_q = nl > 1e-6 && length(up_scr) > 1e-9 ? smoothstep(0.35, 0.8, -dot(n_scr / nl, normalize(up_scr))) : 0.0;
		float wq = hit >= 0 ? 1.0 : clamp(0.5 + 0.5 * cu_inside(p, lod, flat_k) / (glen * pix_m * CU_RIM_Q_PX), 0.05, 1.0);
		return vec4(vec3(facing_q, bottom_q, 0.0) * wq, wq);
	}
	// 一个半分辨率像素在云面上走多远（m）；云体函数在这段距离里变多少，就是各判据的过渡宽度
	float step_side = pix_m / max(abs(dot(n, dir)), CU_TONE_GRAZE);
	float w_side = glen * step_side * CU_TONE_AA;
	float w_base = length(grad.xz) * pix_m / max(dir.y, 0.03) * CU_TONE_AA;
	// 云底：离云底外沿（云体函数在云底平面上的零线）不到一个像素的，按像素过渡到侧面
	float base_k = is_base ? smoothstep(0.0, max(w_base, 1e-6), cu_inside(p, lod, flat_k)) : 0.0;
	vec3 s = sky_sun_vec();
	vec3 m = sky_moon_vec();
	float nk = sky_night_k();
	vec3 lmain = nk < 0.5 ? s : m;
	float graze = smoothstep(CU_GRAZE_FROM, CU_GRAZE_TO, abs(dot(n, dir)));
	// 受光交界的过渡宽度：沿受光方向在云面上探出 CU_TERM_PX 个像素，看 n·l 变多少
	float dt = 0.0;
	if (base_k < 1.0) {
		vec3 lw = normalize(lmain + vec3(0.0, CU_LIGHT_LIFT, 0.0));
		vec3 tg = lw - n * dot(n, lw);
		float tl = length(tg);
		if (tl > 1e-3) {
			vec3 p2 = p + tg / tl * step_side * CU_TERM_PX;
			vec3 g2 = cu_grad(p2, lod + 1.0, flat_k, max(texel_m * 2.0, pix_m * 1.5));
			vec3 n2 = -g2 / max(length(g2), 1e-7);
			dt = abs(dot(n2, lw) - dot(n, lw)) / CU_TERM_PX;
		}
	}
	vec3 col;
	float lit_k;
	if (nk <= 0.01 || nk >= 0.99) {
		col = cu_tone(n, lmain, p, lod, flat_k, base_k, graze, w_side, w_base, dt, lit_k);
	} else {
		float lk_s;
		float lk_m;
		vec3 cs = cu_tone(n, s, p, lod, flat_k, base_k, graze, w_side, w_base, dt, lk_s);
		vec3 cm = cu_tone(n, m, p, lod, flat_k, base_k, graze, w_side, w_base, dt, lk_m);
		col = mix(cs, cm, nk);
		lit_k = mix(lk_s, lk_m, nk);
	}
	// 大气透视：远处的云向霾色靠，接到地平线的霾带里；远处与雾天的淡出也并进颜色，覆盖率只管轮廓
	float ap = (1.0 - exp(-th / CU_AP_DIST)) * CU_AP_MAX;
	col = mix(col, haze, ap);
	col = mix(col, haze, smoothstep(CU_FADE_FROM, CU_MAX_DIST, th));
	col = mix(col, haze, sky_fog_weather_k() * 0.85);
	return vec4(col * cover, cover);
}

vec4 overcast_deck(vec3 dir, vec3 cam, float pix, vec3 haze, float amt) {
	if (amt <= 0.001 || dir.y < 0.008) {
		return vec4(0.0);
	}
	float cy = min(cam.y, DECK_Y - 50.0);
	float t = (DECK_Y - cy) / dir.y;
	if (t > DECK_MAX_DIST) {
		return vec4(0.0);
	}
	vec2 xz = cam.xz + dir.xz * t;
	vec2 uv = (xz + cloud.xy * 0.5) / DECK_TILE;
	float lod = log2(max(t * pix / (DECK_TILE / 512.0), 1.0));
	vec4 nz = textureLod(cloud_noise, uv, lod);
	float a = nz.a;
	float g = nz.g;   // G：胞状起伏
	// 盖满的程度：低频场 + 胞状起伏，阴度到 1 时全盖上
	float field = a + (g - 0.5) * 0.3;
	float cov = aastep(1.0 - amt * 1.3, field);
	// 亮缝：低频场 0.5 等值线上的一道窄缝，缝边镶银边（至少留一道）
	float gap_amt = smoothstep(0.7, 1.0, amt);
	float gd = abs(a - 0.5);
	float gw = DECK_GAP_W * (0.55 + g);
	float fw = max(fwidth(gd), 1e-5);
	float gap = (1.0 - smoothstep(gw - fw, gw + fw, gd)) * gap_amt;
	float rim = (1.0 - smoothstep(gw * DECK_GAP_RIM - fw, gw * DECK_GAP_RIM + fw, gd)) * gap_amt - gap;
	// 底色：珍珠粉紫；冬季与雪天用薰衣草灰；夜里向夜雨的暖紫靠（云层放大城市天光）
	float winter = clamp(1.0 - abs(season.x - 3.0), 0.0, 1.0);
	float snow_k = max(winter, weather_a.w * step(0.01, weather_a.z));
	vec3 base = mix(pal_cur(PAL_WEATHER_OVERCAST), pal_cur(PAL_WEATHER_SNOW_OVERCAST), snow_k);
	base = mix(base, pal_cur(PAL_WEATHER_NIGHT_RAIN), sky_night_k() * 0.65);
	base *= 1.0 - DECK_RAIN_DIM * clamp(weather_a.z, 0.0, 1.0);
	// 两阶：朝天光（日、月）方位的一侧亮一阶，交界随胞状起伏弯曲
	vec3 l = mix(sky_sun_vec(), sky_moon_vec(), sky_night_k());
	vec2 dh = dir.xz;
	float toward = length(dh) > 1e-3 && length(l.xz) > 1e-3 ? dot(normalize(dh), normalize(l.xz)) : 0.0;
	float s = toward * 0.8 + (g - 0.5) * 1.4;
	float light_side = aastep(0.1, s);
	vec3 c = mix(mix(base, pal_cur(PAL_SKY_CLOUD_BELLY), DECK_SHADE_MIX), mix(base, pal_cur(PAL_SKY_CLOUD_LIT), DECK_LIT_MIX), light_side);
	// 层积云的胞：团块之间的缝压暗半阶，盖子不是一整块平板
	float cell = textureLod(cloud_noise, uv * DECK_CELL_REPEAT + vec2(0.71, 0.17), lod + log2(DECK_CELL_REPEAT)).b;
	float seam = 1.0 - aastep(DECK_CELL_EDGE, cell);
	c = mix(c, mix(c, pal_cur(PAL_SKY_CLOUD_BELLY), DECK_CELL_MIX), seam * (1.0 - smoothstep(0.0, DECK_AP_DIST, t)));
	c = mix(c, mix(pal_cur(PAL_SKY_CLOUD_LIT), pal_cur(PAL_SKY_CLOUD_RIM), 0.35), clamp(rim, 0.0, 1.0));
	float ap = 1.0 - exp(-t / DECK_AP_DIST);
	c = mix(c, haze, ap * 0.85);
	// 覆盖率只管轮廓（缝里透出天），远处的淡出并进颜色，与积云同一套全分辨率收边
	c = mix(c, haze, smoothstep(DECK_MAX_DIST * 0.7, DECK_MAX_DIST, t));
	float alpha = cov * (1.0 - gap);
	return vec4(c * alpha, alpha);
}

float deck_amount() {
	// 阴度（weather_a.y）或层状云型（cloud.z）× 云量，取大者
	return clamp(max(weather_a.y, cloud.z * cloud_cover), 0.0, 1.0);
}

// ---------------------------------------------------------------------------
vec4 half_res_clouds(vec3 dir, float pix) {
	if (dir.y <= 0.0) {
		return vec4(0.0);
	}
	float w_sun = sky_side_weight(dir);
	vec3 haze = sky_haze_color(w_sun);
	vec4 cu = cumulus(dir, POSITION, pix, haze, false, vec3(0.0), vec3(0.0));
	vec4 dk = overcast_deck(dir, POSITION, pix, haze, deck_amount());
	return vec4(dk.rgb + (1.0 - dk.a) * cu.rgb, dk.a + (1.0 - dk.a) * cu.a);
}

vec3 full_res_sky(vec3 dir, float pix) {
	float w_sun = sky_side_weight(dir);
	vec3 c = sky_gradient(dir, w_sun);
	// 星空：可见度是全局 stars（SkyNode 按色彩脚本、阴度与雾写入）；近地平线、近月处淡出，被云挡住的地方不画
	if (stars > 0.001 && dir.y > 0.0) {
		vec3 tint;
		float st = star_field(dir, pix, tint);
		float vis = stars * smoothstep(2.0, 10.0, degrees(asin(dir.y)));
		vec3 m = sky_moon_vec();
		if (m.y > -0.05) {
			vis *= smoothstep(radians(4.0), radians(16.0), length(dir - m));
		}
		c += tint * st * vis * STAR_GAIN;
	}
	c = add_moon(c, dir, pix);
	return add_sun(c, dir, pix, w_sun);
}

// 全分辨率合成：半分辨率的覆盖率双线性取样后在 0.5 处按约 1 px 收成硬边；
// 金边（夜里是月光的银边）与云底下沿的细高光按覆盖率的梯度画，宽度以全分辨率像素计，不随半分辨率起台阶
vec3 composite_clouds(vec3 sky, vec3 dir, vec4 cl, vec4 rq) {
	float a_s = cl.a;
	if (a_s <= 0.001) {
		return sky;
	}
	vec3 col = cl.rgb / a_s;
	// 细导数：逐像素取差分。粗导数按 2×2 块取，轮廓距离一块一块跳，细亮边断成一串点（40b）
	vec2 g = vec2(dFdxFine(a_s), dFdyFine(a_s));
	float gl = length(g);
	float w = clamp(gl * CU_EDGE_SHARP, 0.004, 0.5);
	float a = smoothstep(0.5 - w, 0.5 + w, a_s);
	if (gl > 1e-4) {
		vec2 outward = -g / gl;
		// 轮廓以内的像素距离；离轮廓 CU_RIM_PX 像素以内是描边带
		float inside_px = (a_s - 0.5) / gl;
		float band = 1.0 - smoothstep(CU_RIM_PX - 0.5, CU_RIM_PX + 0.5, inside_px);
		vec3 l = sky_night_k() < 0.5 ? sky_sun_vec() : sky_moon_vec();
		float sd = dot(dir, l);
		vec2 toward = vec2(dFdx(sd), dFdy(sd));
		float facing = length(toward) > 1e-7 ? smoothstep(0.05, 0.55, dot(outward, normalize(toward))) : 0.0;
		// 四分之一分辨率通道记下的朝向（连续）；只有层云缝边这类那里没记的轮廓，才退回覆盖率梯度
		float q_k = smoothstep(0.02, 0.2, rq.a);
		vec2 fb = rq.a > 1e-4 ? rq.rg / rq.a : vec2(0.0);
		facing = mix(facing, fb.x, q_k);
		float near_light = mix(CU_RIM_FAR, 1.0, smoothstep(-0.3, 0.8, sd));
		float deck_k = 1.0 - 0.75 * deck_amount();
		col = mix(col, pal_cur(PAL_SKY_CLOUD_RIM), band * facing * near_light * deck_k);
		// 云底下沿的白色细高光：低太阳从下方照亮（ART-SKYLIGHT §4.5）
		vec2 up = vec2(dFdx(dir.y), dFdy(dir.y));
		float bottom = length(up) > 1e-7 ? smoothstep(0.35, 0.8, -dot(outward, normalize(up))) : 0.0;
		bottom = mix(bottom, fb.y, q_k);
		float low = 1.0 - smoothstep(CU_HL_SUN_FROM, CU_HL_SUN_TO, sky_alt_deg(l));
		col = mix(col, pal_cur(PAL_SKY_CLOUD_LIT) * 1.06, band * bottom * low * mix(0.55, 1.0, near_light) * deck_k);
	}
	return mix(sky, col, a);
}

void sky() {
	vec3 dir = normalize(EYEDIR);
	float pix = max(length(fwidth(EYEDIR)), 1e-5);
	vec3 sx = dFdx(EYEDIR);
	vec3 sy = dFdy(EYEDIR);
	if (AT_CUBEMAP_PASS) {
		// 卡通材质不读天空的环境光和反射（ENG-BUDGET §10）：辐照度图只给常量，云和天气每帧变也不让它变贵
		COLOR = vec3(0.5);
	} else if (AT_QUARTER_RES_PASS) {
		vec4 q = vec4(0.0);
		if (dir.y > 0.0 && deck_amount() < 0.999) {
			q = cumulus(dir, POSITION, pix, vec3(0.0), true, sx, sy);
		}
		COLOR = q.rgb;
		ALPHA = q.a;
	} else if (AT_HALF_RES_PASS) {
		vec4 o = half_res_clouds(dir, pix);
		COLOR = o.rgb;
		ALPHA = o.a;
	} else {
		COLOR = composite_clouds(full_res_sky(dir, pix), dir, HALF_RES_COLOR, QUARTER_RES_COLOR);
	}
}
