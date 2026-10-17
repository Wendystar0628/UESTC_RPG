// precip.gdshader：降水池（WP-45b）。雨丝、雪花、溅落共用；檐滴（precip_k.z = 1）留到檐滴点表交付后接。
// 规格：SYS-WEATHER（雨的可读性）、§7（降水池）；ENG-PERF；SYS-MOTION。
//
// 实例数据（C# 侧 PrecipModel 写一次，之后不改）：变换是单位阵；INSTANCE_CUSTOM.xyz = 单位立方体里的起点 p0，
// .w = 随机数。实例按哈希排好序，前缀就是均匀抽稀（VisibleInstanceCount）。每 16 个实例里最后一个是溅落槽位。
// 每帧只改 instance uniform（ENG-PERF 登记）：
//   precip_d：xyz 循环盒位移（C# 按 T12 累加、对 8h 取模），w 半边长 h（雨 18 m、雪 14 m）
//   precip_v：xyz 雨丝方向（朝下的单位向量），w 丝长（m，一拍二的一步）
//   precip_k：x 降水类型（0 雨 → 1 雪），y 降水量，z 角色（0 降水池），w 保留
// 颜色只从背景（天空色带）与调色板推导，不写色值；不用 TIME，用 anim_time。
// 雨丝、溅落、雪花的画法照参考场景"太阳雨中的小渔港"的降水（ART-STYLE）：细、略亮于背景、逆光偏暖白，
// 多数淡、少数亮；预乘输出里 alpha 打七折，雨丝在亮背景上微微发亮；溅落是水面环 + 中心水柱 + 抛物线水珠；
// 雪花是带浅边的圆点。透明度在 SYS-WEATHER 第 1 条的形式上按 Opus 评审抬高（主线评审 2026-10-16）。
// 积水判定与 toon 共用 precip_wet.gdshaderinc（溅落不落在积水里）。
shader_type spatial;
render_mode blend_premul_alpha, unshaded, depth_draw_never, cull_disabled;

#include "res://style/shaders/globals.gdshaderinc"
#include "res://style/shaders/fog.gdshaderinc"
#include "res://style/shaders/lamp.gdshaderinc"
#include "res://style/shaders/precip_wet.gdshaderinc"

instance uniform vec4 precip_d = vec4(0.0, 0.0, 0.0, 18.0);
instance uniform vec4 precip_v = vec4(0.0, -1.0, 0.0, 0.58);
instance uniform vec4 precip_k = vec4(0.0);

// ---- 可调参数（Opus 评审定值；与 C# PrecipTuning 无重复项） ----
const float PC_LINE_W = 0.012;         // 雨丝的世界宽度（m），换算到屏幕后夹在下面的像素范围里
const vec2 PC_LINE_PX = vec2(1.0, 1.8);   // 雨丝线宽（内部分辨率像素）：最细 1 px，贴着镜头也不超过 1.8 px
const float PC_QUAD_PAD = 1.0;         // 四边形每侧多留 1 px 做抗锯齿
const vec2 PC_LEN = vec2(1.1, 1.8);    // 丝长 = 一拍的步长 × 1.1–1.8（每根随机）
const float PC_ALPHA_BASE = 0.15;      // α = (0.15 + 0.24 逆光 + 0.3 灯锥 + 0.1 暗背景) × 亮度个性，上限 0.6
const float PC_ALPHA_BACK = 0.24;
const float PC_ALPHA_LAMP = 0.30;
const float PC_ALPHA_DARK = 0.10;
const float PC_ALPHA_MAX = 0.6;
const vec2 PC_HERO = vec2(0.6, 1.7);   // 每根雨丝的亮度个性：多数淡、少数亮（按 r^2.5 分布）
const float PC_FAR_FLOOR = 0.4;        // 远处不足 1 px 的雨丝，透明度按覆盖率降到这个下限
const float PC_RAIN_DL = 0.14;         // 雨丝比背景亮 0.14
const float PC_WARM = 0.7;             // 逆光时朝 sun.light 的暖白插值的上限
const float PC_ADD = 0.7;              // 预乘输出的 alpha 系数：小于 1 时略带加法，雨丝在亮背景上也读得出
const float PC_SUN_DL = 0.06;          // 太阳雨：受光雨丝插到 sun.light（L +0.06）
const float PC_SUN_ALPHA = 0.24;       // 太阳雨：α 再加 0.24 × 受光量，上限 0.6
const float PC_NEAR = 1.0;             // 1–3.2 m 渐入（太近的雨丝在屏幕上过长）
const float PC_NEAR_FULL = 3.2;
const float PC_FLAKE_R0 = 0.022;       // 雪花半径（m），按实例随机放大到 2 倍
const float PC_FLAKE_DL = 0.24;        // 雪花比背景亮 0.24（不用白色）
const float PC_FLAKE_CHROMA = 0.3;     // 雪花只留背景三成的彩度：偏中性的淡色，不像花瓣
const float PC_FLAKE_RIM = 0.35;       // 雪花外圈朝背景色退 35%：带浅边的圆点
const float PC_FLAKE_ALPHA = 0.85;
const float PC_FLAKE_LAMP_DL = 0.18;   // 进路灯光斑再提亮
const float PC_FLAKE_SWAY = 0.12;      // 雪花横摆幅度（m）
const float PC_FLAKE_PX_MAX = 16.0;    // 雪花在屏幕上的最大半径（px）
const float PC_SPLASH_SPAN = 30.0;     // 溅落槽位落在相机周围 ±30 m（§7）
const float PC_SPLASH_SHOW = 20.0;     // 20 m 以内显形
const float PC_SPLASH_LIFE = 3.0;      // 一个溅点持续三拍（一拍二）
const vec2 PC_SPLASH_SIZE = vec2(0.07, 0.12);   // 溅点的尺寸（m）
const vec2 PC_SPLASH_PX = vec2(3.5, 9.0);       // 屏幕上不足 3.5 px 不画，到 9 px 完全显形
const float PC_SPLASH_DL = 0.12;       // 溅点比背景亮
const float PC_SPLASH_ALPHA = 0.42;
const float PC_OCC_NONE = -100000.0;   // precip_occ 范围外视为没有顶面

varying flat int v_role;     // 0 雨丝，1 雪花，2 溅落，-1 不画
varying vec2 v_q;            // 雨丝：x 离中线的像素距离、y 沿线 0（尾）→ 1（头）；雪花：[-1, 1]；溅落：x [-1, 1]、y [0, 1]
varying flat vec3 v_col;     // 线性色（未预乘）
varying flat vec3 v_col2;    // 雪花的外圈色
varying flat float v_alpha;
varying flat float v_frame;  // 溅落：本拍在寿命里的进度 0–1；雨丝：半线宽（px）
varying flat float v_seed;

uint pc_hash(uint x) {
	x ^= x >> 16u;
	x *= 2146121005u;
	x ^= x >> 15u;
	x *= 2221713035u;
	x ^= x >> 16u;
	return x;
}

float pc_rand(uint x) {
	return float(pc_hash(x) >> 8u) / 16777216.0;
}

float pc_luma(vec3 c) {
	return dot(c, vec3(0.2126, 0.7152, 0.0722));
}

// 明度近似取 OKLab L ≈ cbrt(Y)：把颜色整体缩放到 L + dl，色相不变
vec3 pc_shift_l(vec3 c, float dl) {
	float y = max(pc_luma(c), 1e-5);
	float l0 = pow(y, 1.0 / 3.0);
	float l1 = clamp(l0 + dl, 0.02, 0.98);
	return c * pow(l1 / l0, 3.0);
}

// 俯视遮蔽顶面高度（SYS-WEATHER）；范围外没有顶面
float pc_occ_top(vec3 p) {
	if (!in_map(p)) {
		return PC_OCC_NONE;
	}
	return texture(precip_occ, map_uv(p)).r;
}

// 介质：镜头在楼里、地下、水下时不画降水（主线版的遮挡还没有烘焙）
float pc_air() {
	float from_air = medium.x < 0.5 ? 1.0 : 0.0;
	float to_air = medium.y < 0.5 ? 1.0 : 0.0;
	return mix(from_air, to_air, clamp(medium.z, 0.0, 1.0));
}

// 雨丝与雪花的灯锥：灯头到地面光斑半径的圆锥（灯表与亮度用 WP-39 的 lamp.gdshaderinc）。
// 返回 x = 锥里的亮度 0–1，yzw = 该灯的灯照色；没有灯表（尺寸不对）时为 0
vec4 pc_lamp(vec3 p) {
	if (!in_map(p)) {
		return vec4(0.0);
	}
	ivec2 gs = textureSize(lamp_grid, 0);
	ivec2 ts = textureSize(lamp_table, 0);
	if (gs.x < 2 || ts.y != 2) {
		return vec4(0.0);
	}
	ivec2 cell = clamp(ivec2(floor(map_uv(p) * vec2(gs))), ivec2(0), gs - ivec2(1));
	vec4 g = texelFetch(lamp_grid, cell, 0);
	float ids[4] = float[4](g.x, g.y, g.z, g.w);
	float gain = max(lamp_channel(LAMP_SLOT_POOL_GAIN).x, 0.0);
	float h_eff = lamp_heff();
	vec4 best = vec4(0.0);
	for (int k = 0; k < 4; k++) {
		int id = int(ids[k] + 0.5) - 1;
		if (id < 0 || id >= ts.x) {
			break;
		}
		LampData l = lamp_fetch(id);
		float lv = lamp_level(l, h_eff);
		if (lv <= 0.0) {
			continue;
		}
		float head = max(l.head, 0.5);
		float rel = (l.base.y + head - p.y) / head;
		if (rel <= 0.0 || rel > 1.2) {
			continue;
		}
		float rad = l.radius * sqrt(gain) * clamp(rel, 0.0, 1.0);
		float d = length(p.xz - l.base.xz);
		float v = lv * lamp_pool_visibility(h_eff) * (1.0 - smoothstep(0.75 * rad, rad + 0.01, d));
		if (v > best.x) {
			best = vec4(v, pal_cur(l.col));
		}
	}
	return best;
}

// 背景：沿视线的天空色带（雨丝多半落在天空或远处的雾色前）
vec3 pc_background(vec3 dir) {
	return fog_sky(dir);
}

void vertex() {
	vec3 cam = MAIN_CAM_INV_VIEW_MATRIX[3].xyz;
	float h = max(precip_d.w, 1.0);
	float kind = precip_k.x;
	float amount = clamp(precip_k.y, 0.0, 1.0);
	uint id = uint(INSTANCE_ID);
	vec3 p0 = INSTANCE_CUSTOM.xyz * (2.0 * h);
	float r = INSTANCE_CUSTOM.w;
	bool is_snow = kind >= 0.5;
	bool splash = !is_snow && (INSTANCE_ID & 15) == 15;
	float air = pc_air() * smoothstep(0.0, 0.08, amount);
	float stepped = floor(anim_time * step_rate) / max(step_rate, 1.0);

	v_role = -1;
	v_q = vec2(0.0);
	v_col = vec3(0.0);
	v_alpha = 0.0;
	v_frame = 0.0;
	vec4 clip = vec4(2.0, 2.0, 2.0, 1.0);

	if (splash) {
		// 溅落：每个槽位每三拍换一处，落在相机周围 ±30 m（环面取模，位置在世界里不随镜头平移），20 m 以内显形
		float phase = floor(r * PC_SPLASH_LIFE);
		float s = floor(stepped * step_rate + 0.5) + phase;
		float epoch = floor(s / PC_SPLASH_LIFE);
		float age = (s - epoch * PC_SPLASH_LIFE + 0.5) / PC_SPLASH_LIFE;
		uint e = uint(int(mod(epoch, 4096.0)));
		vec2 off = vec2(pc_rand(id * 7919u + e * 104729u), pc_rand(id * 15485863u + e * 32452843u + 1u)) * (2.0 * PC_SPLASH_SPAN);
		vec2 xz = cam.xz + mod(off - cam.xz + PC_SPLASH_SPAN, 2.0 * PC_SPLASH_SPAN) - PC_SPLASH_SPAN;
		float y = pc_occ_top(vec3(xz.x, 0.0, xz.y));
		y = y <= PC_OCC_NONE * 0.5 ? 0.0 : y;
		vec3 base = vec3(xz.x, y, xz.y);
		float dist = length(base - cam);
		float size = mix(PC_SPLASH_SIZE.x, PC_SPLASH_SIZE.y, pc_rand(id * 31u + e * 7u + 3u));
		float px = size * abs(PROJECTION_MATRIX[1][1]) * VIEWPORT_SIZE.y * 0.5 / max(dist, 0.1);
		float show = (1.0 - smoothstep(PC_SPLASH_SHOW * 0.7, PC_SPLASH_SHOW, dist)) * smoothstep(PC_SPLASH_PX.x, PC_SPLASH_PX.y, px)
				* step(PC_SPLASH_PX.x, px) * step(0.8, dist);
		// 积水里不画溅点，改由积水的雨点环表现（与水面同一规则，SYS-WEATHER）
		float pl = clamp(weather_b.y, 0.0, 1.0);
		show *= (pl > 0.0 && precip_puddle_potential(xz) > precip_puddle_threshold(pl)) ? 0.0 : 1.0;
		// 竖直立着、水平朝向镜头的小片
		vec3 toc = cam - base;
		vec2 fh = length(toc.xz) > 1e-3 ? normalize(toc.xz) : vec2(0.0, 1.0);
		vec3 side = vec3(fh.y, 0.0, -fh.x);
		vec2 q = vec2(UV.x * 2.0 - 1.0, 1.0 - UV.y);
		vec3 wp = base + vec3(fh.x, 0.0, fh.y) * 0.02 + side * (q.x * size) + vec3(0.0, q.y * size * 0.9 - 0.01, 0.0);
		clip = PROJECTION_MATRIX * (VIEW_MATRIX * vec4(wp, 1.0));
		vec3 bg = pc_background(normalize(vec3(-toc.x, 0.0, -toc.z) + vec3(0.0, 0.05, 0.0)));
		v_col = pc_shift_l(bg, PC_SPLASH_DL);
		v_alpha = PC_SPLASH_ALPHA * show * air * smoothstep(0.1, 0.4, amount);
		v_q = q;
		v_frame = age;
		v_seed = float(e) * 0.13 + r * 7.0;
		v_role = v_alpha > 0.002 ? 2 : -1;
	} else {
		// 循环盒（ENG-PERF）：p = c + mod(p0 + d − c + h, 2h) − h；雪花另乘每片的速度系数（0.75、1、1.25，d 按 8h 取模所以不跳）
		float speed = is_snow ? 0.75 + 0.25 * floor(fract(r * 7.0) * 3.0) : 1.0;
		vec3 p = cam + mod(p0 + precip_d.xyz * speed - cam + h, 2.0 * h) - h;
		vec3 dir = normalize(precip_v.xyz);
		float len = precip_v.w;
		if (is_snow) {
			// 横摆：480 s 内整数个周期，按一拍二取样
			float ph = 6.2831853 * (stepped * (40.0 + floor(r * 40.0)) / 480.0 + r);
			p.xz += vec2(sin(ph), cos(ph * 0.7)) * PC_FLAKE_SWAY;
		}
		float top = pc_occ_top(p);
		vec3 tail = p - dir * len;
		float dist = length(p - cam);
		float fade = (1.0 - smoothstep(0.75 * h, 0.95 * h, dist)) * air;
		vec3 vd = dist > 1e-3 ? (p - cam) / dist : vec3(0.0, 0.0, -1.0);
		vec3 bg = pc_background(vd);
		float lbg = pow(max(pc_luma(bg), 1e-5), 1.0 / 3.0);
		vec4 lamp = pc_lamp(p);
		lamp.x *= lamp_env_dark();
		vec3 kd = normalize(key_dir);
		float fwd = clamp(dot(vd, kd), 0.0, 1.0);
		float overcast = clamp(weather_a.y, 0.0, 1.0);

		if (!is_snow) {
			float r2 = pc_rand(id * 3u + 1u);
			float r3 = pc_rand(id * 5u + 2u);
			len *= mix(PC_LEN.x, PC_LEN.y, r2);
			tail = p - dir * len;
			if (p.y < top) {
				fade = 0.0;
			} else if (tail.y < top) {
				tail = p - dir * (p.y - top) / max(-dir.y, 0.05);
			}
			fade *= smoothstep(PC_NEAR, PC_NEAR_FULL, dist);
			// 透明度：底子 + 逆光 + 灯锥 + 暗背景，再乘每根的亮度个性
			float back = smoothstep(0.55, 0.97, fwd) * key_strength * mix(1.0, 0.75, overcast);
			float dark = 1.0 - smoothstep(0.35, 0.75, lbg);
			float hero = mix(PC_HERO.x, PC_HERO.y, pow(r3, 2.5));
			float a = (PC_ALPHA_BASE + PC_ALPHA_BACK * back + PC_ALPHA_LAMP * lamp.x + PC_ALPHA_DARK * dark) * hero;
			// 颜色：略亮于背景；逆光时朝 sun.light 的暖白；灯下取灯照色
			vec3 col = pc_shift_l(bg, PC_RAIN_DL);
			col = mix(col, pc_shift_l(pal_cur(PAL_SUN_LIGHT), 0.02), PC_WARM * back);
			col = mix(col, pc_shift_l(lamp.yzw, 0.04), 0.6 * lamp.x);
			// 太阳雨（§2、§3 第 3 条）：降水量 > 0.05、阴度 < 0.5、主光 > 0.3 时，逆光雨丝插到 sun.light
			float sunny = smoothstep(0.55, 0.45, overcast) * smoothstep(0.25, 0.35, key_strength) * step(0.05, amount);
			float q = sunny * fwd * fwd * fwd;
			col = mix(col, pc_shift_l(pal_cur(PAL_SUN_LIGHT), PC_SUN_DL), q);
			a += PC_SUN_ALPHA * q * sunny;
			vec4 f = fog_eval(p, cam);
			v_col = mix(col, f.rgb, f.a);

			// 屏幕空间的细线：线宽按 1.2 cm 的世界宽度换算、夹在 1–1.8 px；不足 1 px 的按覆盖率降透明度
			vec4 vh = VIEW_MATRIX * vec4(p, 1.0);
			vec4 ca = PROJECTION_MATRIX * (VIEW_MATRIX * vec4(tail, 1.0));
			vec4 cb = PROJECTION_MATRIX * vh;
			float pxw = PC_LINE_W * abs(PROJECTION_MATRIX[1][1]) * VIEWPORT_SIZE.y * 0.5 / max(-vh.z, 0.05);
			float wpx = clamp(pxw, PC_LINE_PX.x, PC_LINE_PX.y);
			a *= mix(PC_FAR_FLOOR, 1.0, pow(clamp(pxw / wpx, 0.0, 1.0), 0.75));
			v_alpha = min(a, PC_ALPHA_MAX) * fade;
			if (ca.w > 0.05 && cb.w > 0.05 && v_alpha > 0.002) {
				vec2 sa = ca.xy / ca.w * VIEWPORT_SIZE * 0.5;
				vec2 sb = cb.xy / cb.w * VIEWPORT_SIZE * 0.5;
				vec2 sd = sb - sa;
				float ls = length(sd);
				sd = ls > 1e-3 ? sd / ls : vec2(0.0, 1.0);
				vec2 n = vec2(-sd.y, sd.x);
				float half_q = wpx * 0.5 + PC_QUAD_PAD;
				clip = mix(ca, cb, UV.y);
				vec2 offs = n * (UV.x * 2.0 - 1.0) * half_q + sd * (UV.y * 2.0 - 1.0);
				clip.xy += offs * 2.0 / VIEWPORT_SIZE * clip.w;
				v_q = vec2((UV.x * 2.0 - 1.0) * half_q, UV.y);
				v_frame = wpx * 0.5;
				v_role = 0;
			}
		} else {
			// 雪花：朝向镜头的小圆片，比背景亮 0.2，进路灯光锥再提亮；顶面以下不画
			fade *= (p.y < top ? 0.0 : 1.0) * smoothstep(0.6, 2.0, dist);
			float rad = PC_FLAKE_R0 * (1.0 + fract(r * 31.0));
			vec3 col = pc_shift_l(bg, PC_FLAKE_DL + PC_FLAKE_LAMP_DL * lamp.x);
			col = mix(vec3(pc_luma(col)), col, PC_FLAKE_CHROMA);
			col = mix(col, pc_shift_l(lamp.yzw, 0.1), 0.35 * lamp.x);
			vec4 f = fog_eval(p, cam);
			v_col = mix(col, f.rgb, f.a);
			v_col2 = mix(v_col, bg, PC_FLAKE_RIM);
			vec4 vc = VIEW_MATRIX * vec4(p, 1.0);
			// 屏幕上不足 1.5 px 时放大到 1.5 px、透明度按面积降，免得闪烁
			float px = rad * abs(PROJECTION_MATRIX[1][1]) * VIEWPORT_SIZE.y * 0.5 / max(-vc.z, 0.05);   // RD 渲染器的投影矩阵 y 取反
			float grow = max(1.0, 1.5 / max(px, 1e-3));
			// 贴着镜头的雪花在屏幕上太大：半径超过 8 px 起淡出，16 px 全隐
			v_alpha = PC_FLAKE_ALPHA * fade / (grow * grow) * (1.0 - smoothstep(PC_FLAKE_PX_MAX * 0.5, PC_FLAKE_PX_MAX, px));
			vec2 q = UV * 2.0 - 1.0;
			vc.xy += q * rad * grow;
			if (-vc.z > 0.05 && v_alpha > 0.002) {
				clip = PROJECTION_MATRIX * vc;
				v_q = q;
				v_role = 1;
			}
		}
	}
	POSITION = clip;
}

float pc_seg(vec2 p, vec2 a, vec2 b) {
	vec2 pa = p - a;
	vec2 ba = b - a;
	float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
	return length(pa - ba * h);
}

// 溅落：水面上压扁的环 + 早拍的中心水柱 + 沿抛物线飞出的两三颗水珠（参考场景的 atCrown）。px：一个屏幕像素在 uv 里的大小
float pc_crown(vec2 uv, float age, float seed, float px) {
	float e = 1.0 - (1.0 - age) * (1.0 - age);
	float w = max(0.045, px * 0.75);
	float rr = mix(0.12, 0.9, e);
	float ring = abs(length(vec2(uv.x, (uv.y - 0.04) * 4.5)) - rr);
	float a = (1.0 - smoothstep(w * 0.5, w * 0.5 + px, ring)) * (1.0 - age) * 0.55;
	float jt = 1.0 - smoothstep(0.0, 0.45, age);
	float jd = pc_seg(uv, vec2(0.0, 0.05 + 0.3 * e), vec2(0.0, 0.08 + 0.55 * e));
	a = max(a, (1.0 - smoothstep(w, w + px, jd)) * jt);
	for (int i = 0; i < 3; i++) {
		float fi = float(i);
		float side = i == 1 ? 0.0 : (i == 0 ? -1.0 : 1.0);
		float hv = pc_rand(uint(int(seed * 977.0)) * 3u + uint(i) * 7919u);
		if (side == 0.0 && hv < 0.5) {
			continue;
		}
		vec2 v0 = vec2(side * (0.55 + 0.35 * hv) + (side == 0.0 ? (hv - 0.75) : 0.0), 1.15 + 0.5 * hv);
		vec2 pos = vec2(0.0, 0.05) + v0 * e * 0.75 - vec2(0.0, 1.0) * e * e * 0.85;
		float rd = max(0.06, px * 0.9) * (1.0 - 0.4 * age);
		a = max(a, (1.0 - smoothstep(rd, rd + px, length(uv - pos))) * 0.75);
	}
	return a * (1.0 - smoothstep(0.6, 1.0, age));
}

void fragment() {
	float a = 0.0;
	vec3 col = v_col;
	float px = max(fwidth(v_q.x), fwidth(v_q.y));
	if (v_role == 0) {
		// 雨丝：按像素算覆盖，尾部 70% 渐入，头部略亮
		float cov = clamp(v_frame + 0.5 - abs(v_q.x), 0.0, 1.0);
		float along = smoothstep(0.0, 0.7, v_q.y) * (1.0 - smoothstep(0.93, 1.0, v_q.y));
		float head = smoothstep(0.62, 0.9, v_q.y) * (1.0 - smoothstep(0.96, 1.0, v_q.y));
		a = v_alpha * cov * along;
		col *= 1.0 + 0.25 * head;
	} else if (v_role == 1) {
		// 雪花：带浅边的圆点
		float r = length(v_q);
		float w = max(fwidth(r), 1e-3);
		a = v_alpha * (1.0 - smoothstep(1.0 - w, 1.0, r));
		col = mix(v_col, v_col2, smoothstep(0.55, 0.85, r));
	} else if (v_role == 2) {
		a = v_alpha * pc_crown(v_q, v_frame, v_seed, px);
	}
	if (a < 0.002) {
		discard;
	}
	ALBEDO = col * a;
	ALPHA = v_role == 0 ? a * PC_ADD : a;
	FOG = vec4(0.0);
}
