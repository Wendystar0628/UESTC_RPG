// fx_glow.gdshader（WP-39）：加法、不受光的夜间光晕。规格：ART-STYLE §7.5、§7.6；ENG-PERF §1.4、§10.2、§10.3。
// 分支由材质 uniform fx_kind 选（材质库每种一份材质，不在运行时改 render_mode 或 #define）：
//   0 窗灯光晕：贴立面的面片，边长 1.5 倍窗宽、离立面 2 cm，光晕半径不超过窗宽的一半；250 m 外淡出
//   1 灯头光晕：朝镜头的面片；夜雾里半径 ×2
//   2 光锥、3 室外软边光柱、4 室内棱柱光柱、5 细尘、6 萤火虫：主线留空（塌成 0 面积）
// 实例数据（MultiMesh，每实例 Transform3D + INSTANCE_CUSTOM）：
//   窗：基 = (右 × 面片宽, 上 × 面片高, 外法线)，原点 = 窗心 + 法线 × 2 cm；
//       custom = (窗号, 楼基址, 槽位 + 32 × 楼窗网格总列数（与玻璃的 UV2.y 同一写法）, 标志：1 = 外围，跳过窗覆盖)
//   灯：基 = 单位阵，原点 = 灯头；custom = (lamp_table 列号, 光晕半径 m, 0, 0)
// 网格：1 × 1 的四边形，中心在原点、朝 +Z（QuadMesh 默认）。
// 亮度全在这里按 light_channels、window_override、lamp_table 算，CPU 不逐盏更新；没亮的实例在 vertex() 里塌成 0 面积。
// 颜色只取调色板第 0 行（色号），不写色值；不用 TIME；距离一律按 MAIN_CAM_INV_VIEW_MATRIX。
shader_type spatial;
render_mode blend_add, unshaded, depth_draw_never, cull_disabled, fog_disabled, shadows_disabled, skip_vertex_transform;

#include "res://style/shaders/globals.gdshaderinc"

uniform int fx_kind : hint_range(0, 6) = 0;

// ---- 灯光公共求值 起（WP-39；整段与 toon.gdshaderinc、precip.gdshader 里的副本逐字一致）----
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

// ---- 灯光公共求值 止 ----

// ---------------------------------------------------------------------------
// fx_glow 自己的参数（P0 看图定值）
// ---------------------------------------------------------------------------
const float GLOW_WINDOW_PEAK = 0.18;     // 窗灯光晕的峰值（显示空间，加法）
const float GLOW_WINDOW_FADE0 = 200.0;   // 窗灯光晕 200–250 m 淡出
const float GLOW_WINDOW_FADE1 = 250.0;
const float GLOW_WINDOW_RATIO = 0.25;    // 每边外扩 = 窗宽 × 0.25（与 C# 的 GlowSets.WindowHaloRatio 一致）
const float GLOW_LAMP_HALO = 0.42;       // 灯头光晕的峰值（柔的一圈）
const float GLOW_LAMP_CORE = 1.0;        // 灯头亮芯的峰值（初灯时在金色天光里也认得出灯亮了）
const float GLOW_LAMP_DUSK = 0.85;       // 初灯时（太阳 4° 以上）灯头光晕相对入夜的比例
const float GLOW_LAMP_MIN_ANGLE = 0.0022;  // 远处灯头光晕的最小张角（弧度），远处仍认得出一点灯
const float GLOW_LAMP_PULL = 0.35;       // 灯头光晕朝镜头拉近，免得被灯罩几何挡住
const float GLOW_FOG_RADIUS = 2.0;       // 夜雾里灯头光晕半径 ×2（SYS-WEATHER §2）
const int GLOW_PAL_WINDOW_WARM = 10;     // 色号与 fog.gdshaderinc 的 PAL_WINDOW_WARM、PAL_WINDOW_COOL 一致（WP-04 登记）
const int GLOW_PAL_WINDOW_COOL = 11;

varying vec3 v_color;
varying vec2 v_q;
varying vec2 v_half;
varying float v_margin;
varying float v_level;

// 光晕随天色：白天不画，初灯时淡，入夜满（真实太阳高度）
float glow_night(float h, float dusk_floor) {
	float day = 1.0 - smoothstep(6.0, 12.0, h);
	return day * mix(dusk_floor, 1.0, 1.0 - smoothstep(-6.0, 4.0, h));
}

void vertex() {
	vec3 cam = MAIN_CAM_INV_VIEW_MATRIX[3].xyz;
	float h_true = degrees(asin(clamp(sun_dir.y, -1.0, 1.0)));
	// 默认塌成 0 面积（没亮的实例、留空的分支）；varying 只在本体里赋值
	vec3 out_vertex = vec3(0.0);
	vec3 out_normal = vec3(0.0, 0.0, 1.0);
	float level = 0.0;
	vec3 color = vec3(0.0);
	vec2 q = vec2(0.0);
	vec2 half_size = vec2(1.0);
	float margin = 1.0;
	if (fx_kind == 0) {
		int wid = int(round(INSTANCE_CUSTOM.x));
		int base = int(round(INSTANCE_CUSTOM.y));
		float packed = round(INSTANCE_CUSTOM.z);
		int slot = int(mod(packed, 32.0));
		int cols = int(floor(packed / 32.0));
		bool outskirts = (int(round(INSTANCE_CUSTOM.w)) & 1) != 0;
		vec4 o = outskirts ? vec4(0.0) : lamp_window_override(wid);
		float on = lamp_window_on(wid, base, cols, slot, o);
		vec3 center = MODEL_MATRIX[3].xyz;
		vec3 n = normalize(MODEL_MATRIX[2].xyz);
		vec3 to_cam = cam - center;
		float dist = length(to_cam);
		float facing = smoothstep(0.0, 0.2, dot(n, to_cam / max(dist, 1e-3)));
		float fade = 1.0 - smoothstep(GLOW_WINDOW_FADE0, GLOW_WINDOW_FADE1, dist);
		level = on * facing * fade * glow_night(h_true, 0.3);
		if (level >= 0.002) {
			float sx = length(MODEL_MATRIX[0].xyz);
			float sy = length(MODEL_MATRIX[1].xyz);
			float w = sx / (1.0 + 2.0 * GLOW_WINDOW_RATIO);
			margin = w * GLOW_WINDOW_RATIO;
			half_size = vec2(0.5 * sx - margin, 0.5 * sy - margin);
			q = VERTEX.xy * vec2(sx, sy);
			color = texelFetch(palette, ivec2(lamp_window_cool(wid, o) ? GLOW_PAL_WINDOW_COOL : GLOW_PAL_WINDOW_WARM, 0), 0).rgb;
			vec3 wpos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
			out_vertex = (VIEW_MATRIX * vec4(wpos, 1.0)).xyz;
			out_normal = normalize((VIEW_MATRIX * vec4(n, 0.0)).xyz);
		} else {
			level = 0.0;
		}
	} else if (fx_kind == 1) {
		int idx = int(round(INSTANCE_CUSTOM.x));
		float radius = INSTANCE_CUSTOM.y;
		LampData l = lamp_fetch(idx);
		level = lamp_level(l, lamp_heff());
		if (l.slot == LAMP_SLOT_AVIATION) {
			level *= 0.15 + 0.85 * smoothstep(0.35, 0.65, 0.5 + 0.5 * sin(anim_time * 6.2831853 / 2.5));
		}
		vec3 head = MODEL_MATRIX[3].xyz;
		float dist = length(head - cam);
		float far = max(fog.w, 800.0);
		level *= 1.0 - smoothstep(far * 0.9, far * 1.6, dist);
		level *= glow_night(h_true, GLOW_LAMP_DUSK);
		if (level >= 0.002) {
			radius *= mix(1.0, GLOW_FOG_RADIUS, smoothstep(1.2, 2.5, weather_fog.x));
			float rs = max(radius, dist * GLOW_LAMP_MIN_ANGLE);
			level *= pow(radius / rs, 0.6);
			vec3 cv = (VIEW_MATRIX * vec4(head, 1.0)).xyz;
			cv += normalize(-cv) * min(GLOW_LAMP_PULL, rs * 0.5);
			q = VERTEX.xy * 2.0;
			out_vertex = cv + vec3(VERTEX.xy * 2.0 * rs, 0.0);
			color = texelFetch(palette, ivec2(clamp(l.col, 0, 255), 0), 0).rgb;
		} else {
			level = 0.0;
		}
	}
	// 2–6：光锥、光柱、细尘、萤火虫的分支主线留空（shaft 由 WP-39 照常写）
	VERTEX = out_vertex;
	NORMAL = out_normal;
	v_level = level;
	v_color = color;
	v_q = q;
	v_half = half_size;
	v_margin = margin;
}

void fragment() {
	float a = 0.0;
	if (fx_kind == 0) {
		vec2 q = abs(v_q) - v_half;
		float s = length(max(q, vec2(0.0))) + min(max(q.x, q.y), 0.0);
		float t = s / max(v_margin, 1e-3);
		a = pow(1.0 - smoothstep(-0.35, 1.0, t), 1.6) * GLOW_WINDOW_PEAK;
	} else if (fx_kind == 1) {
		float r = length(v_q);
		float halo = pow(1.0 - smoothstep(0.0, 1.0, r), 2.2);
		float core = 1.0 - smoothstep(0.06, 0.2, r);
		a = halo * GLOW_LAMP_HALO + core * GLOW_LAMP_CORE;
	}
	ALBEDO = v_color * (a * v_level);
}
