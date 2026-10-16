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

// 灯光通道、窗阈值、灯表解码、单灯亮度与光斑的公共求值（WP-39；toon、降水、倒影共用同一份）
#include "res://style/shaders/lamp.gdshaderinc"

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
