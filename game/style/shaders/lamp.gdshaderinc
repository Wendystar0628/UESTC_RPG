#ifndef CAMPUS_LAMP_INCLUDED
#define CAMPUS_LAMP_INCLUDED

#include "res://style/shaders/globals.gdshaderinc"

// ---------------------------------------------------------------------------
// 灯光通道、窗与灯表的公共求值（WP-39）。与 toon.gdshaderinc、precip.gdshader 里的同名函数逐字一致，
// C# 一侧（ScheduleMath、LampTable）用同一套常数；G2 用同一组输入比对。并入公共 include 之前各自保留一份。
// ---------------------------------------------------------------------------
const int LAMP_SLOT_CUE = 23;            // lamp.cue：过场初灯追镜头接管的银杏大道一线
const int LAMP_SLOT_AVIATION = 26;       // lamp.aviation：慢明灭
const int LAMP_SLOT_SYS_CUE = 30;        // sys.cue：x = 1 表示覆盖生效，y = 覆盖期间判开灯用的目标太阳高度
const int LAMP_SLOT_POOL_GAIN = 31;      // sys.poolGain：x = 光斑面积增益（冬夜 1.5，半径乘它的平方根），不是 0–1 的量
const float LAMP_ON_HALF_DEG = 0.08;     // 开灯过渡的半宽（度）
const float LAMP_RAMP_FROM = 0.6;        // 刚亮时的亮度（SYS-TIME §6）
const float LAMP_RAMP_FULL_DEG = -4.0;   // 太阳到 −4° 时亮度升到 1.0
const float LAMP_ALWAYS_DEG = 89.0;      // θ ≥ 89° 表示常亮
const float LAMP_POOL_TOP_M = 4.0;       // 光斑只照到 4 m 以下的立面
const float LAMP_POOL_SOFT = 0.14;       // 光斑边缘的柔边，占半径的比例（平涂、一阶，边缘不硬切）
const float LAMP_POOL_DUSK = 0.2;        // 黄金时段（h_eff 5° 以上）光斑的可见度：灯头先亮，地面只在影里隐约
const vec2 LAMP_POOL_FADE_DEG = vec2(-1.5, 5.0);  // h_eff 从 5° 降到 −1.5°（蓝调）时光斑淡到完整
const vec2 LAMP_POOL_DAY_DEG = vec2(6.5, 10.0);   // h_eff 高于 10° 时白天不画光斑

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

// lamp_table（N×2 RGBA32F）：行 0 = (x, y_base, z, r)；行 1 = (色号 + 256 × 能量%, 槽位 + 32 × 灯头分米 + 32768 × 事件序号, θ, κ)
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
	l.col = int(mod(r1.x, 256.0));
	l.energy = floor(r1.x / 256.0) / 100.0;
	l.slot = int(mod(r1.y, 32.0));
	l.head = mod(floor(r1.y / 32.0), 1024.0) * 0.1;
	l.seq = floor(r1.y / 32768.0);
	l.theta = r1.z;
	l.kappa = r1.w;
	return l;
}

// 单盏灯的亮度 0–1（不含光斑、光晕的形状）：按 h_eff 开灯、刚亮 0.6 → −4° 时 1.0、通道保留比例、级别
float lamp_level(LampData l, float h_eff) {
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
	return on * ramp * keep * l.energy;
}

// 光斑的可见度随天色：灯头和窗灯可以先亮，地面光斑要等环境暗下来才出来（阴雨天按 h_eff 一起提前）
float lamp_pool_visibility(float h_eff) {
	float day = 1.0 - smoothstep(LAMP_POOL_DAY_DEG.x, LAMP_POOL_DAY_DEG.y, h_eff);
	float dusk = 1.0 - smoothstep(LAMP_POOL_FADE_DEG.x, LAMP_POOL_FADE_DEG.y, h_eff);
	return day * mix(LAMP_POOL_DUSK, 1.0, dusk);
}

// 地面与 4 m 以下立面的光斑：返回 (强度 0–1，已乘天色可见度, 色号；无灯时为 −1)。aa 是光斑边缘的过渡宽度（m）：
// 片元里传 max(fwidth(length(p.xz)), 0.02)，vertex() 里传一个常数。
vec2 lamp_pool(vec3 p, float aa) {
	if (!in_map(p)) {
		return vec2(0.0, -1.0);
	}
	ivec2 gs = textureSize(lamp_grid, 0);
	ivec2 cell = clamp(ivec2(floor(map_uv(p) * vec2(gs))), ivec2(0), gs - ivec2(1));
	vec4 g = texelFetch(lamp_grid, cell, 0);
	float ids[4] = float[4](g.x, g.y, g.z, g.w);
	float gain = max(lamp_channel(LAMP_SLOT_POOL_GAIN).x, 0.0);
	float h_eff = lamp_heff();
	float best = 0.0;
	float col = -1.0;
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
		float top = max(min(l.head, LAMP_POOL_TOP_M), 1.0);
		float hp = p.y - l.base.y;
		float v = 1.0 - smoothstep(top - 0.5, top, hp);
		float r = l.radius * sqrt(gain);
		float d = length(p.xz - l.base.xz);
		float m = 1.0 - smoothstep(r * (1.0 - LAMP_POOL_SOFT) - aa, r + aa, d);
		float s = m * v * lv;
		if (s > best) {
			best = s;
			col = float(l.col);
		}
	}
	return vec2(best * lamp_pool_visibility(h_eff), col);
}


#endif
