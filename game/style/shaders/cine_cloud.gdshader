// cine_cloud.gdshader：开始页与过场的云卡、镜头云幕（SYS-TITLE，ENG-PERF 白名单，WP-49b）
//
// 用法（节点由 WP-49a 建，内核挂载）：
// - 云卡：每层一个 MultiMesh（QuadMesh 1×1，按实例变换缩放到云卡的宽高），mode = 0。
//   INSTANCE_CUSTOM：x 形状种子（0–1）；y 平底高度（0–1，映射到卡高的 5%–16%）；z 塔状程度（0 扁平的积云带，
//   1 偏心隆起的高积云）；w 细节密度（0 远层只有大团块，1 近层加满顶上的小团块）。卡面竖直，法线朝镜头一侧。
// - 镜头云幕：一张 QuadMesh，作为相机的子节点放在 (0, 0, −0.5)，缩放到盖住视锥（例如 4 × 1.2），mode = 1。
//   instance uniform wipe 从 0 推到 1：几层远近不同的云从画面四边叠进来（近层先到），1 为全遮；反向即擦出。
// - 两种用法的节点都关投影（CastShadow = Off），不进倒影世界；只在 Title、Entering、Leaving 可见。
// - 云卡层的 wipe 大于 0 时，各团块从外缘往里收（交还前淡出用）。
//
// 画法：剪影是"大团块打底 + 偏心的塔 + 塔肩上的小团块"，团块之间平滑相接、边缘按一拍二轻微起伏，底部平切。
// 受光按各团块的球面法线（按"打底 → 塔 → 小团块"的次序，后画的压在前面）：朝光的一面是受光阶，其余是云腹阶；
// 下半部背光最深处是最暗阶。金边只在朝太阳一侧的外轮廓上，1080p 下不超过 1.5 px；下沿一道细高光。
// 光的方向取太阳（夜里取月亮）在卡面上的投影并往上抬（顶缘受光，ART-SKYLIGHT）。颜色只读调色板第 0 行，
// 走共用雾函数写 FOG；材质 ID 取"不描"的一档（云卡不描线）；动的部分只读一拍二时钟 t12()，不用 TIME。

shader_type spatial;
render_mode cull_disabled, shadows_disabled, ambient_light_disabled, specular_disabled, depth_draw_opaque;

#include "res://style/shaders/globals.gdshaderinc"
#include "res://style/shaders/fog.gdshaderinc"

float t12() {
    return floor(anim_time * step_rate) / step_rate;
}
float toon_id_rough(int id) {
    return float(id) / 127.0;
}

// 材质 ID：WP-05 保留的通用"不描"ID（fx.noline）。云卡与镜头云幕都不出描线，剪影也不出（ART-STYLE）
const int CINE_CLOUD_MAT_ID = 126;

// ---- 可调参数（材质 uniform；初值按 T0 预览定，运行时不改 render_mode）
uniform int mode : hint_range(0, 1) = 0;                     // 0 云卡，1 镜头云幕
uniform float light_lift : hint_range(0.0, 2.0) = 0.50;       // 光往上抬：顶缘受光
uniform float light_depth : hint_range(0.0, 1.0) = 0.28;      // 光在卡面法线方向上的分量上限（越小越像侧光）
uniform float lit_threshold : hint_range(-1.0, 1.0) = 0.10;   // 受光阶阈值（N·L）
uniform float deep_threshold : hint_range(-1.0, 1.0) = -0.42; // 最暗阶阈值（只在下半部）
uniform float join_soft : hint_range(0.0, 0.5) = 0.08;        // 团块相接处的圆滑程度（占半径）
uniform float layer_soft : hint_range(0.0, 0.3) = 0.10;       // 前面的团块压在后面团块上时，交界的过渡宽度（占半径）
uniform float paint_warp : hint_range(0.0, 0.6) = 0.28;       // 受光交界的手绘起伏
uniform float edge_warp : hint_range(0.0, 0.03) = 0.008;      // 剪影边缘的细碎起伏（占卡宽）
uniform float rim_px : hint_range(0.0, 1.5) = 1.2;            // 金边宽度（1080p 像素，只在朝太阳一侧）
uniform float rim_facing : hint_range(0.0, 1.0) = 0.35;       // 外轮廓朝向太阳多少才描金边
uniform float bottom_px : hint_range(0.0, 1.5) = 1.2;         // 下沿细高光宽度（1080p 像素）
uniform float drift : hint_range(0.0, 0.2) = 0.04;            // 边缘起伏的漂移速度
uniform float night_belly : hint_range(0.0, 1.0) = 0.55;      // 夜里云腹向最暗色靠拢的量
uniform float veil_light : hint_range(0.0, 1.0) = 0.65;       // 镜头云幕的受光量：越大越亮（奶白为主）

instance uniform float wipe = 0.0;

// 每实例常量用 flat，免得插值误差把哈希打散成噪点
varying flat vec4 v_custom;
varying flat vec3 v_right;
varying flat vec3 v_up;
varying flat vec3 v_out;
varying flat float v_aspect;
varying vec3 v_world;

const int N_BODY = 4;
const int N_TOWER = 3;
const int N_MAJOR = 7;   // N_BODY + N_TOWER
const int N_BUMP = 10;
const int PUFFS = 17;    // N_MAJOR + N_BUMP

float hash11(float n) {
	return fract(sin(n * 12.9898 + 4.1414) * 4375.5453);
}

float hash21(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 4375.5453);
}

float vnoise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash21(i), hash21(i + vec2(1.0, 0.0)), u.x),
			mix(hash21(i + vec2(0.0, 1.0)), hash21(i + vec2(1.0, 1.0)), u.x), u.y);
}

// x 方向以 period 为周期的值噪声（镜头云幕绕一圈不留接缝）
float pnoise(vec2 p, float period) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);
	float x0 = mod(i.x, period);
	float x1 = mod(i.x + 1.0, period);
	return mix(mix(hash21(vec2(x0, i.y)), hash21(vec2(x1, i.y)), u.x),
			mix(hash21(vec2(x0, i.y + 1.0)), hash21(vec2(x1, i.y + 1.0)), u.x), u.y);
}

float smin(float a, float b, float k) {
	float h = clamp(0.5 + 0.5 * (b - a) / max(k, 1e-5), 0.0, 1.0);
	return mix(b, a, h) - k * h * (1.0 - h);
}

// 太阳与月亮的交接：太阳高于 1° 时按太阳，低于 −9° 时按月亮；−4° 前后两者的受光都接近 0，换向不跳
void key_light(out vec3 l, out float presence, out float night) {
	float sy = sun_dir.y;
	float day_p = smoothstep(-0.0698, 0.0175, sy);        // sin(−4°) … sin(1°)
	float night_p = smoothstep(-0.0698, -0.1564, sy);     // sin(−4°) … sin(−9°)
	night = 1.0 - smoothstep(-0.1392, -0.0349, sy);       // 云腹变暗：−8° … −2°
	l = sy > -0.0698 ? sun_dir : moon_dir;
	presence = max(day_p, night_p * clamp(moon_dir.y * 4.0, 0.0, 1.0));
}

void vertex() {
	v_custom = INSTANCE_CUSTOM;
	v_right = normalize(MODEL_MATRIX[0].xyz);
	v_up = normalize(MODEL_MATRIX[1].xyz);
	v_out = normalize(MODEL_MATRIX[2].xyz);
	v_aspect = length(MODEL_MATRIX[1].xyz) / max(length(MODEL_MATRIX[0].xyz), 1e-3);
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

// 大团块（打底与塔）：圆心 xy、半径 z。单位是卡宽：x ∈ [−0.5, 0.5]，y ∈ [0, aspect]，y = base 处平切。
vec3 major_puff(int k, float seed, float base, float room, float tower) {
	float fk = float(k);
	float h1 = hash11(seed + fk * 1.37);
	float h2 = hash11(seed + fk * 2.71 + 0.5);
	float h3 = hash11(seed + fk * 3.97 + 0.25);
	vec2 c;
	float r;
	if (k < N_BODY) {
		float u = (fk + 0.5) / float(N_BODY);
		c.x = mix(-0.31, 0.31, u) + (h1 - 0.5) * 0.08;
		float endf = 1.0 - smoothstep(0.12, 0.42, abs(c.x));
		r = room * mix(0.30, 0.46, h2) * mix(0.60, 1.0, endf);
		r = min(r, 0.20 * mix(0.72, 1.0, endf));
		c.y = base + r * mix(0.02, 0.32, h3);
	} else {
		float j = fk - float(N_BODY);
		float xt = (hash11(seed + 9.1) - 0.5) * 0.32;        // 塔偏离中线
		c.x = xt + (j - 1.0) * mix(0.07, 0.12, h1);
		r = room * mix(0.18, 0.28, h2) * mix(0.55, 1.0, tower);
		r = min(r, 0.15);
		float lift = j == 1.0 ? 0.12 * tower : 0.0;          // 中间那颗最高
		c.y = base + room * (mix(0.28, 0.50, h3) * mix(0.55, 1.0, tower) + lift);
		c.y = min(c.y, base + room - r * 1.02);
	}
	return vec3(c, r);
}

// 小团块：贴在大团块上半圈的肩上
vec3 bump_puff(int b, float seed, float base, float room, float tower) {
	float fb = float(b);
	int parent = N_BODY + (b % N_TOWER);                       // 先贴塔肩，再贴打底的团块
	if (b >= 6) {
		parent = (b * 3 + 1) % N_BODY;
	}
	vec3 pp = major_puff(parent, seed, base, room, tower);
	float h1 = hash11(seed + fb * 4.73 + 7.0);
	float h2 = hash11(seed + fb * 6.11 + 3.0);
	float h3 = hash11(seed + fb * 8.37 + 1.0);
	float th = mix(0.45, 2.70, h1);                          // 上半圈（弧度）
	vec2 c = pp.xy + vec2(cos(th), sin(th)) * pp.z * mix(0.84, 1.0, h2);   // 多数落在轮廓上，把外形拱出来
	float r = pp.z * mix(0.18, 0.40, h3 * h3 + 0.15 * h1);
	c.y = min(c.y, base + room - r * 1.02);
	return vec3(c, r);
}

vec3 puff_k(int k, float seed, float base, float room, float tower) {
	return k < N_MAJOR ? major_puff(k, seed, base, room, tower) : bump_puff(k - N_MAJOR, seed, base, room, tower);
}

void fragment() {
	vec3 l_world;
	float presence;
	float night;
	key_light(l_world, presence, night);

	vec3 c_lit = pal_cur(PAL_SKY_CLOUD_LIT);
	vec3 c_rim = pal_cur(PAL_SKY_CLOUD_RIM);
	vec3 c_belly = pal_cur(PAL_SKY_CLOUD_BELLY);
	vec3 c_deep = pal_cur(PAL_SKY_CLOUD_DEEP);
	c_belly = mix(c_belly, c_deep, night * night_belly);
	c_rim = mix(c_rim, c_lit, night);                       // 夜里是一道银边
	float px1080 = VIEWPORT_SIZE.y / 1080.0;                 // 1080p 像素换成当前像素
	float t_lit = mix(1.2, lit_threshold, presence);         // 无影蓝调里没有受光阶

	vec3 col;
	float alpha;
	float tw = t12();

	if (mode == 0) {
		float aspect = v_aspect;
		vec2 q = vec2(UV.x - 0.5, (1.0 - UV.y) * aspect);
		float px = max(fwidth(q.x), 1e-6);                   // 一个像素合多少卡宽
		float seed = v_custom.x * 61.0 + 3.0;
		float base = aspect * mix(0.05, 0.16, clamp(v_custom.y, 0.0, 1.0));
		float room = aspect - base;
		float tower = clamp(v_custom.z, 0.0, 1.0);
		float detail = clamp(v_custom.w, 0.0, 1.0);

		// 剪影边缘的细碎起伏，按一拍二时钟慢慢漂
		vec2 sd = vec2(seed * 3.1, seed * 1.7);
		vec2 warp = vec2(vnoise(q * 34.0 + sd + tw * drift * 10.0), vnoise(q * 34.0 + sd.yx - tw * drift * 7.0)) - 0.5;
		vec2 qw = q + warp * edge_warp;
		// 受光交界的低频手绘起伏
		vec2 pw = vec2(vnoise(q * 4.5 + sd), vnoise(q * 4.5 + sd.yx + 5.0)) - 0.5;

		// 按"打底 → 塔 → 小团块"的次序画：后画的压在前面，各自用球面法线，交界处在 layer_soft 宽度内过渡
		float d = 1e5;
		float near_d = 1e5;
		vec3 n = vec3(0.0, 0.0, 1.0);
		float on_bump = 0.0;                                   // 最前面一层是不是小团块
		int n_bump = int(round(detail * float(N_BUMP)));
		for (int k = 0; k < PUFFS; k++) {
			if (k >= N_MAJOR + n_bump) {
				break;
			}
			vec3 pf = puff_k(k, seed, base, room, tower);
			float r = pf.z - wipe * 0.5;
			if (k >= N_MAJOR) {
				r *= smoothstep(1.5 * px, 4.0 * px, r);        // 小于几个像素的小团块不画，免得闪
			}
			if (r <= 1e-4) {
				continue;
			}
			vec2 dq = qw - pf.xy;
			float dl = length(dq);
			d = smin(d, dl - r, join_soft * r);
			float rs = r * (1.0 + 0.5 * join_soft);            // 着色半径略放大，盖住相接处的圆滑填充
			vec2 dn = (dq + pw * paint_warp * rs) / rs;        // 手绘起伏只扰动法线
			if (dl - r < near_d) {
				near_d = dl - r;
				if (dl > rs) {
					n = normalize(vec3(dn * 0.6, 0.6));         // 相接处的填充：偏向朝观者，避免楔形暗块
				}
			}
			if (dl < rs) {
				// 小团块的法线压扁一些：顶上的小团块大多受光，只在压住大团块的那一侧留一道窄的暗弧；远层更平
				vec2 dnk = k >= N_MAJOR ? dn * mix(0.45, 0.8, detail) : dn;
				vec3 nk = normalize(vec3(dnk, max(sqrt(max(1.0 - dot(dnk, dnk), 0.0)), 0.15)));
				float b = smoothstep(0.0, max(layer_soft, 1e-3), (rs - dl) / rs);
				if (k >= N_MAJOR) {
					b *= 0.8;                                      // 小团块压在大团块上，交界再软一点
				}
				n = normalize(mix(n, nk, b));
				on_bump = mix(on_bump, k >= N_MAJOR ? 1.0 : 0.0, b);
			}
		}
		// 平底，带一点点起伏
		float floor_y = base + (vnoise(vec2(q.x * 9.0 + seed, 2.0)) - 0.5) * 0.004;
		d = max(d, floor_y - q.y);
		alpha = 1.0 - smoothstep(-px, px, d);

		float bb = smoothstep(floor_y + 0.06 * room, floor_y, q.y);
		n = normalize(mix(n, vec3(0.0, -1.0, 0.3), bb * 0.85));    // 云底平展、背光

		vec3 face = FRONT_FACING ? v_out : -v_out;
		vec3 lc = vec3(dot(l_world, v_right), dot(l_world, v_up), dot(l_world, face));
		vec3 l = normalize(vec3(lc.x, lc.y + light_lift, clamp(lc.z, -0.45, light_depth)));
		float s = dot(n, l);

		float t_eff = t_lit - 0.28 * on_bump;                   // 顶上的小团块暗弧更窄
		col = s > t_eff ? c_lit : c_belly;
		if (s <= t_eff && s < deep_threshold && q.y < floor_y + 0.45 * room && presence > 0.2) {
			col = c_deep;
		}
		// 金边：只在朝太阳一侧的外轮廓，宽度不超过 1.5 px（1080p）
		vec2 l2 = normalize(l.xy + vec2(1e-4));
		float facing = dot(normalize(n.xy + vec2(1e-5)), l2);
		float rim_w = rim_px * px1080 * px;
		if (d > -rim_w && facing > rim_facing && s > t_lit && q.y > floor_y + 2.0 * px && presence > 0.05) {
			col = c_rim;
		}
		// 下沿细高光
		float bw = bottom_px * px1080 * max(fwidth(q.y), 1e-6);
		if (q.y < floor_y + bw && d < -px) {
			col = c_lit;
		}
		FOG = fog_eval(v_world, INV_VIEW_MATRIX[3].xyz);
	} else {
		// 镜头云幕：三层远近不同的云在画面里向外流（近层快、远层慢），从四边叠进来；
		// 被阳光照透，奶白为主，淡紫只在远层和背光的深处
		vec2 vs = VIEWPORT_SIZE;
		vec2 q = vec2(SCREEN_UV.x - 0.5, 0.5 - SCREEN_UV.y) * vec2(vs.x / vs.y, 1.0);
		float rad = length(q);
		float ang = atan(q.y, q.x);
		float r_max = 0.5 * length(vec2(vs.x / vs.y, 1.0));
		float rn = rad / r_max;
		vec3 lv = (VIEW_MATRIX * vec4(l_world, 0.0)).xyz;     // 光在画面里的方向
		vec2 l2 = normalize(lv.xy + vec2(0.0, light_lift));
		vec3 warm = mix(c_lit, c_rim, 0.12);
		float lum_lit = dot(c_lit, vec3(0.2126, 0.7152, 0.0722));
		vec3 lav = c_belly + (lum_lit - dot(c_belly, vec3(0.2126, 0.7152, 0.0722))) * 0.6;   // 提亮的淡紫，保住色相
		alpha = 0.0;
		col = c_lit;
		for (int i = 0; i < 3; i++) {
			float fi = float(i) * 0.5;                           // 0 远，1 近
			float cells = mix(16.0, 6.0, fi);                    // 绕一圈的团块数
			float kr = cells / TAU;
			float speed = mix(0.7, 2.4, fi);
			float lead = mix(0.0, 0.18, fi);                     // 近层先到
			float sat = mix(1.2, 0.08, fi);                      // 全遮时远层铺满、近层只留团块
			float full = i == 0 ? smoothstep(0.85, 1.0, wipe) * 0.7 : 0.0;
			float edge = clamp((rn - (1.0 - wipe * (1.0 + lead))) * 2.6, -1.5, sat) + full;
			vec2 lp = vec2(ang / TAU * cells, log(max(rad, 1e-3)) * kr - speed * tw + fi * 7.3);
			float f = pnoise(lp, cells) * 0.62 + pnoise(lp * 2.0 + vec2(0.0, 3.1), cells * 2.0) * 0.38;
			float calm = smoothstep(0.02, 0.20, rad);
			f = mix(0.62, f, calm);
			float cov = f - 0.5 + edge;
			if (cov > 0.0) {
				vec2 qs = q + l2 * mix(0.012, 0.03, fi);
				float rs = length(qs);
				vec2 lps = vec2(atan(qs.y, qs.x) / TAU * cells, log(max(rs, 1e-3)) * kr - speed * tw + fi * 7.3);
				float fs = pnoise(lps, cells) * 0.62 + pnoise(lps * 2.0 + vec2(0.0, 3.1), cells * 2.0) * 0.38;
				fs = mix(0.62, fs, calm);
				bool lit = fs < f + (veil_light - 0.5) * 0.12;   // 朝光的一面
				// 近层：暖白 / 奶白；中层：奶白 / 淡紫；远层：淡紫两档
				vec3 c_hi = fi > 0.75 ? warm : (fi > 0.25 ? c_lit : mix(lav, c_lit, 0.35));
				vec3 c_lo = fi > 0.75 ? c_lit : (fi > 0.25 ? mix(lav, c_lit, 0.45) : lav);
				col = (lit && presence > 0.05) ? c_hi : c_lo;
				alpha = 1.0;
			}
		}
		FOG = vec4(0.0);
	}

	ALPHA = alpha;
	ALPHA_SCISSOR_THRESHOLD = 0.5;
	ALBEDO = vec3(0.0);
	METALLIC = 0.0;
	SPECULAR = 0.0;
	ROUGHNESS = toon_id_rough(CINE_CLOUD_MAT_ID);
	EMISSION = col;
}

void light() {
	// 不受场景灯光：受光在 fragment() 里按日月方位算好，写进 EMISSION
}
