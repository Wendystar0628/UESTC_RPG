#ifndef CAMPUS_LAMP_INCLUDED
#define CAMPUS_LAMP_INCLUDED

#include "res://style/shaders/globals.gdshaderinc"
// 调色板取色只走 fog.gdshaderinc 的 pal_cur / pal_fetch（色号 0–511 → 纹素的同一条 bank 规则，contract-28）
#include "res://style/shaders/fog.gdshaderinc"

// ---------------------------------------------------------------------------
// 灯光通道、窗与灯表的公共求值（WP-39）。toon（WP-04）、fx_glow（WP-39）、precip（WP-45b）都 include 本文件，
// C# 一侧（ScheduleMath、LampTable）用同一套常数；G2 用同一组输入比对。只含函数与常量，不定义 uniform、varying。
// ---------------------------------------------------------------------------
const int LAMP_SLOT_LAMP_FIRST = 12;     // 灯具类槽位 12–27（LightChannelCatalog）：L 是保留比例，开灯另按每盏灯的太阳高度阈值
const int LAMP_SLOT_LAMP_LAST = 27;
const int LAMP_SLOT_CUE = 23;            // lamp.cue：过场初灯追镜头接管的银杏大道一线
const int LAMP_SLOT_AVIATION = 26;       // lamp.aviation：慢明灭
const int LAMP_SLOT_SYS_CUE = 30;        // sys.cue：x = 1 表示覆盖生效，y = 覆盖期间判开灯用的目标太阳高度
const int LAMP_SLOT_POOL_GAIN = 31;      // sys.poolGain：x = 光斑面积增益（冬夜 1.5，半径乘它的平方根），不是 0–1 的量
const float LAMP_ON_HALF_DEG = 0.08;     // 开灯过渡的半宽（度）
const float LAMP_RAMP_FROM = 0.6;        // 刚亮时的亮度（SYS-TIME §6）
const float LAMP_RAMP_FULL_DEG = -4.0;   // 太阳到 −4° 时亮度升到 1.0
const float LAMP_ALWAYS_DEG = 89.0;      // θ ≥ 89° 表示常亮
const float LAMP_POOL_TOP_M = 4.0;       // 光斑只照到 4 m 以下的立面
const float LAMP_FIXTURE_REACH = 1.0;    // 灯具部件（灯罩、灯牌）离本灯灯头标记的最远距离（m），再远不算这盏灯的部件
const vec2 LAMP_POOL_DAY_DEG = vec2(6.5, 10.0);   // h_eff 高于 10° 时白天不画光斑（toon 用 .y 提前跳过）

// ---------------------------------------------------------------------------
// 路灯光斑的观感参数（集中在这里，看图定值）。光斑分两阶：亮芯 + 收边的外圈，台阶边界只留抗锯齿宽度。
// 光是加在表面上的，不替换底色：亮芯加该材质的灯照色（调色板行 1，即固有色 × 灯色）× 强度；
// 外圈只沿灯色抬亮底色（加光方向 = 底色 × 灯色），不往灰白里混：草在外圈仍是绿、更亮更暖，铺装是偏暖的浅色。
// 强度随环境变暗升起：环境暗度读调色板第 0 行的 shadow.1（中性参考面当前的影色），黄昏弱、入夜满。
// ---------------------------------------------------------------------------
const float LAMP_POOL_GAIN = 0.55;       // 入夜满强度时亮芯加上的灯照量（× 灯照色，线性）
const float LAMP_POOL_CORE = 0.58;       // 亮芯半径占灯具标记光斑半径（Range）的比例
const float LAMP_POOL_OUTER = 0.8;       // 外圈外沿占光斑半径的比例（亮芯之外只留一道收边）
const float LAMP_POOL_RIM = 0.28;        // 外圈加光的亮度相对亮芯
const float LAMP_POOL_CORE_SAT = 0.85;   // 亮芯里灯色（相对 lamp.warm）的彩度保留；琥珀灯靠它不发橙
const float LAMP_POOL_CHROMA_CAP = 0.62; // 加光的彩度上限（偏离亮度的最大分量 / 亮度）：中性材质约 0.45 不受影响，草、银杏不再刷成黄漆
const float LAMP_POOL_SOFT = 0.02;       // 两道台阶边缘的柔边，占半径的比例（再加像素抗锯齿宽度）
const float LAMP_POOL_DUSK = 0.15;       // 黄昏（环境最亮时）的强度比例
const vec2 LAMP_POOL_ENV_Y = vec2(0.15, 0.40);    // shadow.1 影色的线性亮度从 0.40（黄昏）降到 0.15（入夜）时，强度从 DUSK 升到 1
// 竖直面（墙、台阶立面）上的光斑是扇贝形：以灯头为顶点向下张开的光罩与墙面相交，灯下最亮、往下变宽变暗、上沿是弧；
// 到灯脚高度时宽度正好等于地面光斑，亮芯、外圈两阶照旧。
const float LAMP_WALL_SPREAD = 0.75;     // 光罩半径 ∝ 深度^它：1 是直边锥（贴墙灯成 V 形），越小上沿越圆、扇贝越高
const float LAMP_WALL_FOOT = 0.5;        // 竖直面在灯脚高度处的亮度（灯头下为 1，线性变暗）
const vec2 LAMP_WALL_NY = vec2(0.5, 0.7);    // 世界法线 n.y 在这一段里地面一支与竖直面一支按 smoothstep 混合：低于 .x 全按竖直面，高于 .y 全按地面
const vec2 LAMP_DOWN_NY = vec2(-0.3, -0.1);  // 朝下的面不受光斑：n.y 低于 .x 不画，到 .y 渐入（檐口与雨棚底面、球底、扶手下沿）
const int LAMP_PAL_ENV = 12;             // shadow.1，与 fog.gdshaderinc 的 PAL_SHADOW_1 一致
const int LAMP_PAL_WARM = 51;            // lamp.warm，与 fog.gdshaderinc 的 PAL_LAMP_WARM 一致：行 1 的灯照色按它推

uint lamp_hash_u(uint x) {
	x ^= x >> 16u;
	x *= 2146121005u;
	x ^= x >> 15u;
	x *= 2221713035u;
	x ^= x >> 16u;
	return x;
}

float lamp_hash01(uint x) {
	return float(lamp_hash_u(x) >> 8u) / 16777216.0;
}

// light_channels：32×1 RGF，x = L，y = Δ
vec2 lamp_channel(int slot) {
	return texelFetch(light_channels, ivec2(clamp(slot, 0, 31), 0), 0).rg;
}

// 有效太阳高度 h_eff = h − 6° × 阴度 − 4° × 降水（SYS-WEATHER §7），与 C# 的 ScheduleMath.EffectiveSun 一致
float lamp_heff() {
	float h = degrees(asin(clamp(sun_dir.y, -1.0, 1.0)));
	return h - 6.0 * clamp(weather_a.y, 0.0, 1.0) - 4.0 * clamp(weather_a.z, 0.0, 1.0);
}

// 窗的亮灯阈值（与 toon_window_threshold、C# 的 ScheduleMath.WindowThreshold 一致）：
// 同一层相邻 3 个开间（大致一间房）阈值相近，所以灯按房间成团亮灭；fract(均匀 + 独立量) 仍是均匀分布，L 仍约等于亮窗比例。
// 整栋楼再加 ±0.25 的偏置，有的楼整体亮、有的暗。cols = 楼窗网格的总列数
float lamp_window_threshold(int id, int base, int cols) {
	int local = max(id - base, 0);
	int c = max(cols, 1);
	int row = local / c;
	int col = local - row * c;
	uint seg = uint(base) * 131u + uint(row) * 17u + uint(col / 3);
	float t = fract(lamp_hash01(uint(id) * 2u + 1u) * 0.3 + lamp_hash01(seg * 2u + 11u));
	float bld = lamp_hash01(uint(base) * 2u + 7u);
	return clamp(t + 0.5 * (bld - 0.5), 0.0, 1.0);
}

// window_override：R 0 不覆盖、128 强制灭、255 强制亮；G 0 按哈希、128 暖、255 冷；B 窗帘；只覆盖 0–65535 号
vec4 lamp_window_override(int id) {
	if (id < 0 || id >= 65536) {
		return vec4(0.0);
	}
	return texelFetch(window_override, ivec2(id % 256, id / 256), 0);
}

float lamp_window_on(int id, int base, int cols, int slot, vec4 o) {
	vec2 c = lamp_channel(slot);
	float th = lamp_window_threshold(id, base, cols);
	float on = smoothstep(th, th + max(c.y, 1e-3), c.x);
	if (o.r >= 0.75) {
		on = 1.0;
	} else if (o.r >= 0.25) {
		on = 0.0;
	}
	return on;
}

bool lamp_window_cool(int id, vec4 o) {
	if (o.g >= 0.75) {
		return true;
	}
	if (o.g >= 0.25) {
		return false;
	}
	return lamp_hash01(uint(id) * 2u + 2u) >= 0.6667;
}

// lamp_table（N×2 RGBA32F）：行 0 = (x, y_base, z, r)；行 1 = (色号低字节 + 256 × 能量% + 32768 × bank, 槽位 + 32 × 灯头分米 + 32768 × 事件序号, θ, κ)
// 行 1 的 x 是 PaletteEncoding.PackLampColor 的精确整数：bit 15 是色号的 bank（色号 256–511），能量% 只占 bit 8–14；bank 0 与旧值逐位相同。
struct LampData {
	vec3 base;      // 灯脚处的地面点
	float radius;   // 光斑半径（m），未乘冬夜倍数
	int col;        // 调色板色号
	float energy;   // 级别 0–1
	int slot;       // 通道槽位
	float head;     // 灯头离地高度（m）
	float seq;      // 偶发事件序号
	float theta;    // 开灯阈值（太阳高度，度）
	float kappa;    // 保留阈值 0–1
};

LampData lamp_fetch(int i) {
	vec4 r0 = texelFetch(lamp_table, ivec2(i, 0), 0);
	vec4 r1 = texelFetch(lamp_table, ivec2(i, 1), 0);
	LampData l;
	l.base = r0.xyz;
	l.radius = r0.w;
	float bank = floor(r1.x / 32768.0);
	float packed = r1.x - 32768.0 * bank;   // 色号低字节 + 256 × 能量%（PaletteEncoding.UnpackLampColor）
	l.col = int(mod(packed, 256.0)) + 256 * int(bank);
	l.energy = floor(packed / 256.0) / 100.0;
	l.slot = int(mod(r1.y, 32.0));
	l.head = mod(floor(r1.y / 32.0), 1024.0) * 0.1;
	l.seq = floor(r1.y / 32768.0);
	l.theta = r1.z;
	l.kappa = r1.w;
	return l;
}

// 单盏灯的亮灭 0–1（不乘级别，不含光斑、光晕的形状）：按 h_eff 开灯、刚亮 0.6 → −4° 时 1.0、通道保留比例
float lamp_on(LampData l, float h_eff) {
	float h = h_eff;
	if (l.slot == LAMP_SLOT_CUE) {
		vec2 cue = lamp_channel(LAMP_SLOT_SYS_CUE);
		if (cue.x > 0.5) {
			h = cue.y;
		}
	}
	float on = 1.0;
	float ramp = 1.0;
	if (l.theta < LAMP_ALWAYS_DEG) {
		on = 1.0 - smoothstep(l.theta - LAMP_ON_HALF_DEG, l.theta + LAMP_ON_HALF_DEG, h);
		ramp = mix(LAMP_RAMP_FROM, 1.0, clamp((l.theta - h) / max(l.theta - LAMP_RAMP_FULL_DEG, 0.5), 0.0, 1.0));
	}
	vec2 c = lamp_channel(l.slot);
	float keep = smoothstep(l.kappa, l.kappa + max(c.y, 1e-3), c.x);
	return on * ramp * keep;
}

// 单盏灯的亮度 0–1：亮灭 × 级别
float lamp_level(LampData l, float h_eff) {
	return lamp_on(l, h_eff) * l.energy;
}

// 灯具部件（灯罩、灯牌）的自发光亮度 0–1，slot 是材质登记的灯光通道。窗灯类通道直接取 L；
// 灯具类通道取灯表里灯头离 p 最近、且在 LAMP_FIXTURE_REACH 以内那盏灯的亮灭，与它的光斑、光晕同一个开灯阈值、
// 保留阈值和过场覆盖：白天灯灭，灯罩也不亮。附近没有登记的灯时退回通道的 L。
float lamp_fixture_level(vec3 p, int slot) {
	float lv = clamp(lamp_channel(slot).x, 0.0, 1.0);
	if (slot < LAMP_SLOT_LAMP_FIRST || slot > LAMP_SLOT_LAMP_LAST || !in_map(p)) {
		return lv;
	}
	ivec2 gs = textureSize(lamp_grid, 0);
	ivec2 cell = clamp(ivec2(floor(map_uv(p) * vec2(gs))), ivec2(0), gs - ivec2(1));
	vec4 g = texelFetch(lamp_grid, cell, 0);
	float ids[4] = float[4](g.x, g.y, g.z, g.w);
	float h_eff = lamp_heff();
	float best = LAMP_FIXTURE_REACH;
	for (int k = 0; k < 4; k++) {
		int id = int(ids[k] + 0.5) - 1;
		if (id < 0) {
			break;
		}
		LampData l = lamp_fetch(id);
		float d = distance(p, l.base + vec3(0.0, l.head, 0.0));
		if (d < best) {
			best = d;
			lv = lamp_on(l, h_eff);
		}
	}
	return lv;
}

float lamp_luma(vec3 c) {
	return dot(c, vec3(0.2126, 0.7152, 0.0722));
}

// 保亮度降彩度：sat = 1 原样，0 为同亮度的灰
vec3 lamp_desat(vec3 c, float sat) {
	return mix(vec3(lamp_luma(c)), c, sat);
}

// 保亮度把彩度压到上限以内：彩度低的颜色原样通过，只压高彩度的
vec3 lamp_chroma_cap(vec3 c, float cap) {
	float y = max(lamp_luma(c), 1e-4);
	vec3 d = c - vec3(y);
	float ch = max(max(abs(d.r), abs(d.g)), abs(d.b)) / y;
	return vec3(y) + d * min(1.0, cap / max(ch, 1e-4));
}

// 环境暗度 0–1：黄昏最亮时 0，入夜 1。读调色板 shadow.1 影色的线性亮度（阴雨天调色板更暗，跟着提前）。
// 光斑、降水的灯锥与雪花提亮等"随天色变强的灯光效果"共用这一个系数，不各自计时
float lamp_env_dark() {
	float env = lamp_luma(texelFetch(palette, ivec2(LAMP_PAL_ENV, 0), 0).rgb);
	return 1.0 - smoothstep(LAMP_POOL_ENV_Y.x, LAMP_POOL_ENV_Y.y, env);
}

// 光斑的强度随环境：白天（h_eff 10° 以上）不画；之后按环境暗度从黄昏的 DUSK 升到入夜的 1。
// 降水的灯锥（WP-45b 的 precip）也乘这个值
float lamp_pool_visibility(float h_eff) {
	float day = 1.0 - smoothstep(LAMP_POOL_DAY_DEG.x, LAMP_POOL_DAY_DEG.y, h_eff);
	return day * mix(LAMP_POOL_DUSK, 1.0, lamp_env_dark());
}

// 一处的光斑：core = 亮芯权重，rim = 外圈权重（两者都已乘灯的亮度与环境强度，互斥，台阶处按抗锯齿宽度过渡），
// col = 主导那盏灯的色号（无灯时为 −1）。多盏灯重叠时取最亮的一盏，不叠加。
struct LampPool {
	float core;
	float rim;
	int col;
};

// 一盏灯在一支上的 (亮芯, 外圈, 比较用的亮度)：d = 到灯脚的水平距离，r、rc = 外圈、亮芯半径，e = 边缘过渡宽度，w = 灯的亮度 × 这一支的亮度
vec3 lamp_pool_branch(float d, float r, float rc, float e, float w) {
	float outer = 1.0 - smoothstep(r * (1.0 - LAMP_POOL_SOFT) - e, r + e, d);
	float inner = 1.0 - smoothstep(rc * (1.0 - LAMP_POOL_SOFT) - e, rc + e, d);
	return vec3(w * inner, w * (outer - inner), w * mix(LAMP_POOL_RIM * outer, 1.0, inner));
}

// wall_t = 竖直面一支的权重（0 = 地面，1 = 竖直面，之间两支按权重混合，每盏灯分别混合后再取最亮的一盏）。
// 竖直面一支：光斑半径随灯头以下的深度从 0 张到灯脚高度的地面半径（按 LAMP_WALL_SPREAD，灯脚以下不再变宽），亮度从 1 线性降到 LAMP_WALL_FOOT；
// 只受 4 m 封顶。aa_wall 是竖直面上沿弧线的过渡宽度（含竖向像素尺寸）。地面一支照到灯头高度（不超过 4 m）。
LampPool lamp_pool_eval_surface(vec3 p, float aa, float wall_t, float aa_wall) {
	LampPool q = LampPool(0.0, 0.0, -1);
	if (!in_map(p)) {
		return q;
	}
	ivec2 gs = textureSize(lamp_grid, 0);
	ivec2 cell = clamp(ivec2(floor(map_uv(p) * vec2(gs))), ivec2(0), gs - ivec2(1));
	vec4 g = texelFetch(lamp_grid, cell, 0);
	float ids[4] = float[4](g.x, g.y, g.z, g.w);
	float gain = max(lamp_channel(LAMP_SLOT_POOL_GAIN).x, 0.0);
	float h_eff = lamp_heff();
	float best = 0.0;
	for (int k = 0; k < 4; k++) {
		int id = int(ids[k] + 0.5) - 1;
		if (id < 0) {
			break;
		}
		LampData l = lamp_fetch(id);
		float lv = lamp_level(l, h_eff);
		if (lv <= 0.0) {
			continue;
		}
		float r = l.radius * sqrt(gain) * LAMP_POOL_OUTER;
		float rc = l.radius * sqrt(gain) * LAMP_POOL_CORE;
		float d = length(p.xz - l.base.xz);
		vec3 c = vec3(0.0);
		if (wall_t < 1.0) {
			float top = max(min(l.head, LAMP_POOL_TOP_M), 1.0);
			float v = 1.0 - smoothstep(top - 0.5, top, p.y - l.base.y);
			c = lamp_pool_branch(d, r, rc, aa, lv * v);
		}
		if (wall_t > 0.0) {
			float h = max(l.head, 0.5);
			float k = clamp((l.base.y + h - p.y) / h, 0.0, 1.0);   // 0 = 灯头高度，1 = 灯脚高度及以下
			float g = pow(k, LAMP_WALL_SPREAD);
			float v = mix(1.0, LAMP_WALL_FOOT, k) * (1.0 - smoothstep(LAMP_POOL_TOP_M - 0.5, LAMP_POOL_TOP_M, p.y - l.base.y));
			c = mix(c, lamp_pool_branch(d, r * g, rc * g, aa_wall, lv * v), wall_t);
		}
		if (c.z > best) {
			best = c.z;
			q = LampPool(c.x, c.y, l.col);
		}
	}
	float vis = lamp_pool_visibility(h_eff);
	q.core *= vis;
	q.rim *= vis;
	return q;
}

// 不分朝向的光斑（降水的灯锥、雪花在 vertex() 里也用）：一律按地面一支求
LampPool lamp_pool_eval(vec3 p, float aa) {
	return lamp_pool_eval_surface(p, aa, 0.0, aa);
}

// 地面与 4 m 以下立面的光斑：返回 (强度 0–1, 色号；无灯时为 −1)。强度是两阶的：亮芯 1、外圈 LAMP_POOL_RIM，
// 已乘灯的亮度与环境强度。aa 是光斑边缘的过渡宽度（m）：片元里传 max(fwidth(length(p.xz)), 0.02)，vertex() 里传一个常数。
vec2 lamp_pool(vec3 p, float aa) {
	LampPool q = lamp_pool_eval(p, aa);
	return vec2(q.core + LAMP_POOL_RIM * q.rim, float(q.col));
}

// 外圈的加光：方向 = 底色 × 灯色（只沿灯色抬亮底色，不混灰白），亮度 = y
vec3 lamp_rim_lift(vec3 base, vec3 lc, float y) {
	vec3 c = lamp_chroma_cap(max(base * lc, vec3(0.0)), LAMP_POOL_CHROMA_CAP);
	return c * (y / max(lamp_luma(c), 1e-4));
}

// 光斑着色（卡通材质在湿润、积水之后调用）：把灯光加在当前的受光色和影色上，不替换底色。
// lamp_lit = 该材质的灯照色（调色板行 1，线性，已乘湿润变暗），即固有色在 lamp.warm 下的颜色；
// 别的灯色按相对 lamp.warm 的比值换算。亮芯加灯照色；外圈按亮芯亮度的 LAMP_POOL_RIM 沿灯色抬亮底色，只做收边。
// n_w = 世界法线（toon 传 c.n_w，含插值与解析法线）：n.y 在 LAMP_WALL_NY 一段里地面一支与竖直面一支平滑混合，
// 曲面上明暗随法线连续变化，平地（n.y = 1）、平墙（n.y = 0）只走各自一支；朝下的面按 LAMP_DOWN_NY 不受光斑。
// 竖直面上沿弧线的过渡宽度另取 fwidth(p.y)，只在片元里调用（toon 进来前的分支只看材质标志与全局太阳高度）。
void lamp_pool_shade(vec3 p, vec3 n_w, float aa, vec3 lamp_lit, inout vec3 lit, inout vec3 shade) {
	float aa_wall = max(aa, fwidth(p.y));   // 导数在提前返回之前取
	float ny = n_w.y / max(length(n_w), 1e-6);
	float facing = smoothstep(LAMP_DOWN_NY.x, LAMP_DOWN_NY.y, ny);
	if (facing <= 0.0) {
		return;
	}
	float wall_t = 1.0 - smoothstep(LAMP_WALL_NY.x, LAMP_WALL_NY.y, ny);
	LampPool q = lamp_pool_eval_surface(p, aa, wall_t, aa_wall);
	q.core *= facing;
	q.rim *= facing;
	if (q.col < 0 || q.core + q.rim <= 0.0) {
		return;
	}
	vec3 warm = texelFetch(palette, ivec2(LAMP_PAL_WARM, 0), 0).rgb;
	vec3 lc = pal_cur(q.col);
	vec3 tint = lamp_desat(lc / max(warm, vec3(0.02)), LAMP_POOL_CORE_SAT);
	vec3 core = lamp_chroma_cap(lamp_lit * tint, LAMP_POOL_CHROMA_CAP);
	vec3 core_add = LAMP_POOL_GAIN * q.core * core;
	float y_rim = LAMP_POOL_GAIN * LAMP_POOL_RIM * q.rim * lamp_luma(core);
	vec3 rim_lit = lamp_rim_lift(lit, lc, y_rim);
	vec3 rim_shade = lamp_rim_lift(shade, lc, y_rim);
	lit += core_add + rim_lit;
	shade += core_add + rim_shade;
}


#endif
