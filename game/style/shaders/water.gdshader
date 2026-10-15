// water.gdshader：全校默认不透明水面（WP-24；WLD-WATER §1、§7.1，ENG-PERF §1.4、§10.4）
//
// 留在不透明通道：不读深度、不读屏幕纹理；接收主光阴影，两阶合成与 toon 相同
// （ALBEDO = 1，影写 EMISSION，"受光 − 影"在 light() 的方向光分支里按阈值化的阴影加进 DIFFUSE_LIGHT）；
// cull_disabled，从水下看到背面画成浅色的"水下天花板"；自身不投影（顶点标志 8，阴影通道里塌成一点）。
//
// 顶点数据（Campus.Gen.Water.WaterModule）：UV = 流速 (vx, vz) m/s，湖为 0；UV2 = (到岸距离 m, 倒影湖号，-1 = 不做平面倒影)。
// 颜色全部来自调色板：水色四档与泡沫是 WP-24 登记的表面色对（palette/ext/WP-24.json，色号 214–223），
// 天空倒影、主光色、雾色是随帧色（WP-37）。着色器里不写色值。
//
// 画法的机制移植自渲染气质参照"太阳雨中的小渔港"的水面（ART-STYLE §1）：
//   水体色 = 按到岸距离分档的透出色（三道硬边色带）；
//   天空的倒影 = 按波面法线偏离与视线高度分成的几块反射率（菲涅耳色块），不是一整片渐变；
//   物体的倒影 = 平面倒影按 5 级明度换成水色色阶（色块化），饱和的物体留 0.58 的色相；
//                再按波面法线投到相机右、前两个方向的偏移，以 0.012 一格量化成碎片，碎片之间 1 px 水色细缝；
//   波纹线、主光反光带、四芒星闪点、泡沫线（外侧再一道断续的细线）叠在上面。
//
// 调色板取色、解析天空与雾都用 fog.gdshaderinc（WP-04）：倒影里的天与 WP-40 的天空同源（sky_gradient、sky_side_weight），
// 雾用 fog_eval，湖面雾（fog_low.z）由本着色器另加。
// 远处、掠射角下水面静下来（"静水远景"）：波面、碎片、色带扰动和波纹都按像素足迹衰减，
// 天空倒影收成两三条硬边横带，像镜面一样映出地平线的暖色和金带；碎片只留在近处。
shader_type spatial;
render_mode cull_disabled, diffuse_lambert, specular_disabled, ambient_light_disabled;

#include "res://style/shaders/globals.gdshaderinc"
#include "res://style/shaders/fog.gdshaderinc"

// ---------------------------------------------------------------------------------------------------------------
// 可调参数（评审定值；WLD-WATER 的出处写在每行后面）
// ---------------------------------------------------------------------------------------------------------------
const float WTR_BAND_NEAR = 1.5;      // 近岸带外缘（m，§1.1）
const float WTR_BAND_MID = 4.0;       // 中带外缘
const float WTR_BAND_DEEP = 9.0;      // 湖心一档的起点
const float WTR_THROUGH_DUSK = 0.42;  // 低太阳时透出色的亮度系数（线性）：白天格是清透的青，默认格压成暗青绿（§1.1）
const float WTR_NIGHT_BODY = 0.15;    // 夜里透出色的占比：夜间水面映着夜空（§1.1）
const float WTR_BODY_DUSK_KEEP = 0.35;   // 太阳低时透出色保留的比例，其余换成底色（默认格以天空倒影的蓝紫为主，§1.1）
const float WTR_SKY_LIFT = 0.6;       // 底色取天空倒影时向高处天色抬的比例（底色偏蓝紫而不是地平线的粉橙，§1.1）
const float WTR_SKY_LIFT_E = 0.6;     // 抬向的高度（仰角的正弦）
const float WTR_BASE_DARK = 0.80;     // 底色"再压暗"（线性）
const float WTR_NIGHT_LIFT = 0.2;     // 夜间底色向高处天色抬的比例系数
const float WTR_NIGHT_DARK = 0.96;    // 夜间底色的压暗
// 天空倒影的菲涅耳色块：skyK = 波面偏离 × 10 + (0.6 − 视线高度) × 0.8 + 噪声；两道阈值分三档反射率
const vec2 WTR_SKY_TH = vec2(0.22, 0.60);
const vec3 WTR_SKY_F = vec3(0.08, 0.30, 0.22);   // 基础反射率、第一档增量、第二档增量
const float WTR_SKY_DUSK_F = 0.30;    // 太阳低时天空倒影多占的反射率：默认格仍以天空倒影为主（§1.1）
const float WTR_SKY_BLOCK_LIFT = 0.45;   // 亮一档的天空色块向地平线天色抬的比例
const vec2 WTR_OBJ_F = vec2(0.80, 0.88); // 物体倒影的反射率（暗块、亮块）
// 静水远景：像素足迹（每像素对应的水面米数，随距离和掠射角变大）从 x 到 y，近处的波面、碎片和波纹渐渐静成镜面
const vec2 WTR_CALM = vec2(0.15, 0.8);
const vec3 WTR_FAR_E = vec3(0.0, 2.2, 6.0);   // 远处天空倒影的三条横带各取的仰角（度）；最低一条取地平线霾色
const vec2 WTR_FAR_EDGE = vec2(1.2, 3.5);     // 横带的分界（倒影视线的仰角，度）
const float WTR_FAR_DARK = 0.92;      // 镜面倒影比真天略暗一点
const float WTR_FAR_F = 0.9;          // 远处的反射率：透出色几乎看不见
const float WTR_FAR_OBJ_KEEP = 0.7;   // 远处物体倒影保留本色的比例（近处是水色色阶的色块）
const float WTR_GLINT_STREAK = 7.0;   // 远处碎金带的横纹密度（按到相机水平距离的对数）
// 倒影的碎片（§1.5、§7.2）
const float WTR_DISTORT = 0.32;       // 波面法线 → 倒影 UV 偏移
const float WTR_QUANT = 0.012;        // 偏移按倒影 UV 0.012 一格量化
const float WTR_SEAM_DL = 0.04;       // 碎片之间 1 px 细缝比水色暗的明度
// 倒影的色块化：明度阈值（sRGB 明度）与色相保留
const vec4 WTR_POSTER_TH = vec4(0.17, 0.30, 0.57, 0.86);
const float WTR_HUE_KEEP = 0.58;      // 饱和的物体在色块里留下的原色相（55–60%）
// 波面：三列世界固定方向的正弦波，角频率是 2π/480 的整数倍（480 s 回绕无缝），按 T12 步进
const vec3 WTR_SWELL_LEN = vec3(7.0, 4.3, 2.9);    // 波长（m）
const vec3 WTR_SWELL_SLOPE = vec3(0.032, 0.026, 0.02);    // 坡度振幅
const vec3 WTR_SWELL_K = vec3(192.0, 252.0, 300.0);       // 480 s 里的周期数（另有一列 11 m、150 周期的长波）
const float WTR_SWELL_WARP = 12.0;    // 波面坐标的低频扰动幅度（m）
// 波纹短段（§1.2）
const float WTR_DASH_CELL = 1.6;      // 格（m）；河渠 0.45 m/s × 480 s = 216 m = 135 格，回绕无缝
const float WTR_DASH_DENSITY = 0.55;  // 有亮线短段的格的占比
const vec2 WTR_DASH_HL = vec2(0.16, 0.40);   // 亮线短段半长（格）
const vec2 WTR_FLOW_HL = vec2(0.10, 0.24);   // 河渠上亮线短段的半长（格）：短，免得顺流连成"车道线"
const float WTR_FLOW_DASH = 0.45;     // 河渠上有亮线短段的格的占比
const float WTR_DARK_DENSITY = 0.22;  // 暗线短段
const vec2 WTR_DARK_HL = vec2(0.22, 0.44);
const float WTR_DASH_PERIOD = 3.2;    // 每格亮灭的周期（s，480 的约数）
const float WTR_DASH_DUTY = 0.62;     // 一个周期里亮着的比例
const vec2 WTR_LINE_PX = vec2(1.0, 0.6);   // 线的半宽（px）：近处约 2 px，远处约 1 px（§7.1）
const float WTR_LINE_LIFT = 0.45;     // 亮线：高处天色向亮色抬的比例
const float WTR_DARK_LINE = 0.90;     // 暗线：水色 × 影比再压一点
// 泡沫线（§1.4）
const float WTR_FOAM_W = 0.32;        // 线宽（m，0.3–0.5；冬季 0.5–0.9 读 snow）
const float WTR_FOAM_W_WINTER = 0.75;
const float WTR_FOAM_GAP = 0.42;      // 断口的占比（冬季减半）
const float WTR_FOAM_ALPHA = 0.80;    // 盖住水色的比例（冬季为 1）
const vec2 WTR_FOAM_CHANNEL = vec2(0.6, 0.45);   // 河渠的泡沫线：宽度系数、盖色比例（护岸边水流平缓，也不画外侧那道细线）
const float WTR_FOAM_PULSE = 0.25;    // 线宽随波起伏的幅度
const float WTR_FOAM_K = 122.0;       // 起伏在 480 s 里的周期数
// 主光反光带（§1.3）
const float WTR_GLINT_AZ = 0.06;      // 方位半宽（rad），竖向放宽
const float WTR_GLINT_EL = 0.34;      // 竖向容差（rad）
const float WTR_GLINT_GAIN = 1.7;     // HDR 增益，柔光会抓住它
const float WTR_GLINT_DENSITY = 0.9;  // 反光带里亮短段的格占比
const float WTR_GLINT_PX = 2.2;       // 反光带短段比波纹线粗的倍数
const float WTR_GLINT_SCALE = 2.0;    // 反光带短段的格比波纹格细几倍
// 四芒星闪点（§1.3）
const float WTR_STAR_ANGLE = 0.10;    // 反射视线与主光方向的夹角上限（rad）
const float WTR_STAR_DENSITY = 0.08;  // 亮线短段上出星的概率，随距离收紧（同屏 ≤ 40）
const vec2 WTR_STAR_HDR = vec2(2.0, 2.8);   // HDR 增益
const float WTR_STAR_PX = 8.0;        // 星芒臂长上限（px）

// ---------------------------------------------------------------------------------------------------------------
// 调色板色号（WP-24 的扩展段 214–223：受光偶数、影 = 受光 + 1；随帧色号用 fog.gdshaderinc 的 PAL_*）
// ---------------------------------------------------------------------------------------------------------------
const int WTR_PAL_NEAR = 214;
const int WTR_PAL_MID = 216;
const int WTR_PAL_FAR = 218;
const int WTR_PAL_DEEP = 220;
const int WTR_PAL_FOAM = 222;

uniform int mat_id : hint_range(0, 127) = 0;   // 材质库分配的材质 ID，写进 ROUGHNESS（描线据此不描水面）

varying vec3 v_wpos;
varying vec2 v_flow;
varying float v_shore;
varying flat int v_slot;
varying vec3 v_diff;

vec3 wtr_pal(int col) {
	return pal_cur(col);
}

float wtr_luma(vec3 c) {
	return dot(c, vec3(0.2126, 0.7152, 0.0722));
}

// 阈值化，抗锯齿约 1 px
float wtr_aa_step(float edge, float x) {
	float w = max(fwidth(x), 1e-5) * 0.7;
	return smoothstep(edge - w, edge + w, x);
}

// --- 解析天空：与 WP-40 天空同一规则（五段 OKLab 单调三次、太阳侧权重、地平线霾带）
vec3 wtr_sky(vec3 d) {
	return fog_sky(d);
}

// 天空在某个方位、某个仰角（度）的颜色
vec3 wtr_sky_at(vec2 az, float e_deg) {
	float e = radians(e_deg);
	vec2 h = length(az) > 1e-5 ? normalize(az) : vec2(0.0, -1.0);
	return fog_sky(vec3(h.x * cos(e), sin(e), h.y * cos(e)));
}

// --- 噪声：周期 256 格的值噪声，平移量满足 480 s 回绕无缝（SYS-MOTION §1.6）
float wtr_hash(ivec2 c) {
	uvec2 q = uvec2(c & ivec2(255));
	uint h = q.x * 0x8da6b343u ^ q.y * 0xd8163841u;
	h ^= h >> 15u;
	h *= 0x2c1b3c6du;
	h ^= h >> 12u;
	return float(h >> 8u) / 16777216.0;
}

float wtr_hash3(ivec2 c, uint salt) {
	uint h = uint(c.x) * 0x27d4eb2du ^ uint(c.y) * 0x165667b1u ^ salt * 0x9e3779b9u;
	h ^= h >> 15u;
	h *= 0x85ebca6bu;
	h ^= h >> 13u;
	return float(h >> 8u) / 16777216.0;
}

float wtr_noise(vec2 q) {
	vec2 i = floor(q);
	vec2 f = q - i;
	vec2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
	ivec2 c = ivec2(i);
	float a = wtr_hash(c);
	float b = wtr_hash(c + ivec2(1, 0));
	float d = wtr_hash(c + ivec2(0, 1));
	float e = wtr_hash(c + ivec2(1, 1));
	return mix(mix(a, b, u.x), mix(d, e, u.x), u.y);
}

// 波纹短段（§1.2）：世界里固定的格，每格至多一段横向细长的亮线或暗线，段的方向取相机右向（河渠取流向），
// 所以转镜头时段只绕自己的中心转、不在世界里打转；越远格越密、线按 fwidth 保持 1–2 px，自然"越远越密、越细"。
// g：格坐标；dir：段的方向（格坐标里的单位向量）；fwb：每像素跨过的格数（垂直于段）
struct WtrDash {
	float mask;    // 0–1，抗锯齿后的线
	vec2 centre;   // 段中心（格坐标）
	float on;      // 这一格有没有段
};

WtrDash wtr_dash(vec2 g, vec2 dir, float fwb, float density, vec2 hl_range, float px, uint salt, float phase, int period_x) {
	vec2 cf = floor(g);
	ivec2 c = ivec2(cf);
	ivec2 ch = period_x > 0 ? ivec2(int(mod(cf.x, float(period_x))), c.y) : c;
	float on = step(wtr_hash3(ch, salt), density);
	// 每格按 T12 亮灭（一拍二），周期是 480 s 的约数
	on *= step(fract(phase + wtr_hash3(ch, salt + 1u)), WTR_DASH_DUTY);
	float hl = mix(hl_range.x, hl_range.y, wtr_hash3(ch, salt + 2u));
	vec2 room = vec2(0.5 - hl) - 0.04;
	vec2 ctr = (vec2(wtr_hash3(ch, salt + 3u), wtr_hash3(ch, salt + 4u)) * 2.0 - 1.0) * max(room, vec2(0.0));
	vec2 l = g - cf - 0.5 - ctr;
	float a = dot(l, dir);
	float b = dot(l, vec2(-dir.y, dir.x));
	float taper = sqrt(max(0.0, 1.0 - (a / hl) * (a / hl)));
	float th = px * fwb * taper;
	float m = (1.0 - smoothstep(th - 0.5 * fwb, th + 0.5 * fwb, abs(b))) * step(abs(a), hl);
	return WtrDash(m * on, cf + 0.5 + ctr, on);
}

// 波面法线（世界坐标）：三列固定方向的正弦波；t 是 T12 步进的动画时间
vec3 wtr_swell(vec2 p, float t) {
	// 低频的位置扰动打散三列正弦的周期，高处俯看不成格纹
	p += (vec2(wtr_noise(p * 0.04 + vec2(11.0, 3.0)), wtr_noise(p * 0.04 + vec2(5.0, 23.0))) - 0.5) * WTR_SWELL_WARP;
	vec2 d = vec2(0.0);
	vec2 dir0 = vec2(0.8, 0.6);
	vec2 dir1 = vec2(-0.5, 0.866);
	vec2 dir2 = vec2(0.97, -0.243);
	vec3 k = 2.0 * PI / WTR_SWELL_LEN;
	vec3 w = 2.0 * PI * WTR_SWELL_K / 480.0;
	d += WTR_SWELL_SLOPE.x * cos(k.x * dot(p, dir0) - w.x * t) * dir0;
	d += WTR_SWELL_SLOPE.y * cos(k.y * dot(p, dir1) - w.y * t + 1.7) * dir1;
	d += WTR_SWELL_SLOPE.z * cos(k.z * dot(p, dir2) - w.z * t + 4.1) * dir2;
	vec2 dir3 = vec2(-0.906, -0.423);
	d += WTR_SWELL_SLOPE.y * cos(2.0 * PI / 11.0 * dot(p, dir3) - 2.0 * PI * 150.0 / 480.0 * t + 0.6) * dir3;
	return normalize(vec3(-d.x, 1.0, -d.y));
}

// 倒影的色块化：明度分 5 级换成水色色阶，饱和的物体留一部分原色相（参照"小渔港"的 wPosterRefl）
vec4 wtr_poster(vec4 R, vec3 dark, vec3 deep, vec3 mid, vec3 light, vec3 top) {
	float L = wtr_luma(R.rgb);
	vec3 c = dark;
	c = mix(c, deep, wtr_aa_step(WTR_POSTER_TH.x, L));
	c = mix(c, mid, wtr_aa_step(WTR_POSTER_TH.y, L));
	c = mix(c, light, wtr_aa_step(WTR_POSTER_TH.z, L));
	c = mix(c, top, wtr_aa_step(WTR_POSTER_TH.w, L));
	float mx = max(R.r, max(R.g, R.b));
	float mn = min(R.r, min(R.g, R.b));
	float sat = (mx - mn) / max(mx, 1e-3);
	vec3 hue = R.rgb / max(wtr_luma(R.rgb), 1e-3);
	c = mix(c, wtr_luma(c) * hue, smoothstep(0.18, 0.6, sat) * WTR_HUE_KEEP);
	return vec4(c, wtr_aa_step(0.5, R.a));
}

// 着色在 fragment 里算完；light() 只做两阶合成
void vertex() {
	v_wpos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_flow = UV;
	v_shore = UV2.x;
	v_slot = int(round(UV2.y));
	if (IN_SHADOW_PASS) {
		VERTEX = vec3(0.0);
	}
}

void fragment() {
	vec3 cam = INV_VIEW_MATRIX[3].xyz;
	vec3 ray = v_wpos - cam;
	float dist = max(length(ray), 1e-3);
	vec3 D = ray / dist;                         // 相机 → 水面
	vec3 Vc = -D;                                // 水面 → 相机
	float t12 = floor(anim_time * step_rate) / step_rate;   // 一拍二（SYS-MOTION §2.2）
	float overcast = clamp(weather_a.y, 0.0, 1.0);
	float precip = clamp(weather_a.z, 0.0, 1.0) * (1.0 - step(0.5, weather_a.w));
	float sun_el = asin(clamp(sun_dir.y, -1.0, 1.0));
	float day = smoothstep(radians(-4.0), radians(2.0), sun_el);
	float high = smoothstep(radians(6.0), radians(30.0), sun_el);
	vec2 p = v_wpos.xz;
	vec2 fp = fwidth(p);
	float calm = smoothstep(WTR_CALM.x, WTR_CALM.y, max(fp.x, fp.y));
	vec3 Nw = normalize(mix(wtr_swell(p, t12), vec3(0.0, 1.0, 0.0), calm));

	// ---- 波纹短段 ------------------------------------------------------------------------------------------
	float flow_speed = length(v_flow);
	vec2 g;
	vec2 dir;
	int period_x = 0;
	float phase = t12 / WTR_DASH_PERIOD;
	if (flow_speed > 1e-4) {
		// 河渠：格沿流向排，整体顺流平移；哈希按 135 格回绕，480 s 回绕无缝
		vec2 along = v_flow / flow_speed;
		g = vec2(dot(p, along), dot(p, vec2(-along.y, along.x))) / WTR_DASH_CELL;
		g.x -= t12 * flow_speed / WTR_DASH_CELL;
		dir = vec2(1.0, 0.0);
		period_x = 135;
	} else {
		vec2 right = INV_VIEW_MATRIX[0].xz;
		dir = length(right) > 1e-4 ? normalize(right) : vec2(1.0, 0.0);
		g = p / WTR_DASH_CELL;
	}
	float fwb = max(fwidth(dot(g, vec2(-dir.y, dir.x))), 1e-5);
	float fwa = max(fwidth(dot(g, dir)), 1e-5);
	float line_fade = 1.0 - smoothstep(0.12, 0.3, max(fwa, fwb));   // 格小到三五个像素时淡出，交给雾
	float px = mix(WTR_LINE_PX.x, WTR_LINE_PX.y, smoothstep(15.0, 160.0, dist)) * (1.0 - 0.6 * precip);
	bool channel = flow_speed > 1e-4;
	WtrDash dh = wtr_dash(g, dir, fwb, (channel ? WTR_FLOW_DASH : WTR_DASH_DENSITY) * (1.0 - 0.7 * precip),
			channel ? WTR_FLOW_HL : WTR_DASH_HL, channel ? px * 0.8 : px, 101u, phase, period_x);
	WtrDash dd = wtr_dash(g * 0.73 + 0.37, dir, fwb * 0.73, WTR_DARK_DENSITY, WTR_DARK_HL, px, 211u, phase + 0.5, 0);
	WtrDash dg = wtr_dash(g * WTR_GLINT_SCALE + 0.11, dir, fwb * WTR_GLINT_SCALE, WTR_GLINT_DENSITY, vec2(0.30, 0.46), px * WTR_GLINT_PX, 307u, phase * 1.5, 0);
	float hi = dh.mask * line_fade * (1.0 - calm);
	float lo = dd.mask * line_fade * (1.0 - hi) * (1.0 - calm);

	// ---- 水体：三道硬边色带（到岸距离 = 伪深度，§1.1） ------------------------------------------------------
	float d = v_shore;
	float s1b = wtr_aa_step(WTR_BAND_NEAR, d);
	float s2b = wtr_aa_step(WTR_BAND_MID, d);
	float s3b = wtr_aa_step(WTR_BAND_DEEP, d);
	vec3 near_lit = wtr_pal(WTR_PAL_NEAR);
	vec3 mid_lit = wtr_pal(WTR_PAL_MID);
	vec3 far_lit = wtr_pal(WTR_PAL_FAR);
	vec3 deep_lit = wtr_pal(WTR_PAL_DEEP);
	vec3 body_lit = mix(mix(mix(near_lit, mid_lit, s1b), far_lit, s2b), deep_lit, s3b);
	vec3 body_sh = mix(mix(mix(wtr_pal(WTR_PAL_NEAR + 1), wtr_pal(WTR_PAL_MID + 1), s1b), wtr_pal(WTR_PAL_FAR + 1), s2b), wtr_pal(WTR_PAL_DEEP + 1), s3b);
	// 影比：水面按 r_mat 1.10 推出的影（WP-37）与受光之比，暗线、影里的倒影都用它（ART-STYLE §3.2）
	float shade_ratio = clamp(wtr_luma(wtr_pal(WTR_PAL_FAR + 1)) / max(wtr_luma(far_lit), 1e-4), 0.3, 1.0);
	// 底色：天空倒影抬向高处天色再压暗（默认格约 #5B6FA8 一类的蓝紫）；夜里抬得少、压得轻，
	// 夜空的地平线最亮，水面要比岸上的楼亮，剪影才看得清（§1.1、SYS-TIME §3.4）
	vec3 R0 = reflect(D, vec3(0.0, 1.0, 0.0));
	float sky_lift = WTR_SKY_LIFT * mix(WTR_NIGHT_LIFT, 1.0, day);
	vec3 base = wtr_sky(normalize(vec3(R0.x, mix(max(R0.y, 0.0), WTR_SKY_LIFT_E, sky_lift), R0.z))) * mix(WTR_NIGHT_DARK, WTR_BASE_DARK, day);
	// 透出色：白天格是清透的青；太阳低时压暗，并大半换成底色
	float bright = mix(WTR_THROUGH_DUSK, 1.0, high);
	float keep = mix(WTR_BODY_DUSK_KEEP, 1.0, high);
	body_lit = mix(base, body_lit * bright, keep);
	body_sh = mix(base * shade_ratio, body_sh * bright, keep);
	near_lit = mix(base, near_lit * bright, keep);
	mid_lit = mix(base, mid_lit * bright, keep);
	far_lit = mix(base, far_lit * bright, keep);
	deep_lit = mix(base, deep_lit * bright, keep);

	vec3 lit;
	vec3 sh;
	vec3 glow = vec3(0.0);
	if (FRONT_FACING) {
		// ---- 天空的倒影：波面偏离与视线高度分出的菲涅耳色块
		float ndv = clamp(dot(Nw, Vc), 0.0, 1.0);
		float dev = (Vc.y - ndv) * 10.0;
		float sky_k = dev + (0.6 - Vc.y) * 0.8 + 0.32 * (wtr_noise(p * vec2(0.3, 0.38) + vec2(41.0, 13.0)) - 0.5) * (1.0 - calm);
		float s1 = wtr_aa_step(WTR_SKY_TH.x, sky_k);
		float s2 = wtr_aa_step(WTR_SKY_TH.y, sky_k);
		vec3 Rd = reflect(D, normalize(mix(vec3(0.0, 1.0, 0.0), Nw, 0.5)));
		float el = max(Rd.y, 0.02);
		vec3 sky_c = wtr_sky(vec3(Rd.x, el, Rd.z));
		// 亮一档的色块抬向地平线的天色；贴着地平线的倒影一律是亮的
		vec3 sky_hz = sky_haze_color(sky_side_weight(vec3(Rd.x, 0.0, Rd.z)));   // 地平线的霾色，不必再算两列色带
		float horizon = 1.0 - wtr_aa_step(0.12, el);
		vec3 sky_lo = mix(base, sky_c, 0.5);
		vec3 sky_col = mix(sky_lo, mix(sky_c, sky_hz, 0.6) * 1.06, max(s1 * WTR_SKY_BLOCK_LIFT, horizon));
		float f_sky = WTR_SKY_F.x + WTR_SKY_F.y * s1 + WTR_SKY_F.z * s2 + WTR_SKY_DUSK_F * (1.0 - high);
		// 静水远景：倒影视线的仰角分成三条硬边横带，各取天空在那个仰角的颜色
		// 最低一条贴着地平线，取霾色；另两条只在远处才算（省掉近处两次色带求值）
		float e_r = degrees(asin(clamp(R0.y, 0.0, 1.0)));
		float fw_e = max(fwidth(e_r), 1e-4) * 0.7;
		if (calm > 0.002) {
			vec3 sky_far = sky_haze_color(sky_side_weight(vec3(R0.x, 0.0, R0.z)));
			sky_far = mix(sky_far, wtr_sky_at(R0.xz, WTR_FAR_E.y), smoothstep(WTR_FAR_EDGE.x - fw_e, WTR_FAR_EDGE.x + fw_e, e_r));
			sky_far = mix(sky_far, wtr_sky_at(R0.xz, WTR_FAR_E.z), smoothstep(WTR_FAR_EDGE.y - fw_e, WTR_FAR_EDGE.y + fw_e, e_r));
			sky_col = mix(sky_col, sky_far * WTR_FAR_DARK, calm);
		}
		f_sky = mix(f_sky, max(f_sky, WTR_FAR_F), calm);

		// ---- 物体的倒影：平面倒影 → 色块化 → 碎片（§1.5、§7.2）
		vec4 P = vec4(0.0);
		float crack = 0.0;
		if (reflect_lake >= 0 && v_slot == reflect_lake) {
			vec4 clip = reflect_vp * vec4(v_wpos, 1.0);
			if (clip.w > 1e-4) {
				vec2 uv = clip.xy / clip.w * 0.5 + 0.5;
				uv.y = 1.0 - uv.y;
				// 波面法线投到相机右、前两个方向（掠射时前向 ×1.8），按 0.012 一格量化成碎片；
				// 碎片一像素内就要跳格的地方（远处、掠射）退回连续的偏移，免得抖成噪点
				vec2 ax_r = normalize(INV_VIEW_MATRIX[0].xz + vec2(1e-5, 0.0));
				vec2 ax_f = normalize(-INV_VIEW_MATRIX[2].xz + vec2(1e-5, 0.0));
				vec2 n_low = Nw.xz + vec2(wtr_noise(p * 0.21 + vec2(3.0, 7.0)), wtr_noise(p * 0.21 + vec2(17.3, 9.1))) * 0.06 - 0.03;
				vec2 off_low = vec2(dot(n_low, ax_r), dot(n_low, ax_f) * 1.8) * WTR_DISTORT;
				vec2 off_q = floor(off_low / WTR_QUANT + 0.5) * WTR_QUANT;
				vec2 fq = fwidth(off_low / WTR_QUANT);
				off_q = mix(off_q, off_low, smoothstep(0.25, 0.7, max(fq.x, fq.y))) * (1.0 - calm);
				vec2 ruv = clamp(uv + vec2(off_q.x, -off_q.y), vec2(0.0), vec2(1.0));
				vec4 R_obj = texture(reflect_tex, ruv);
				P = wtr_poster(R_obj, deep_lit * 0.55, deep_lit, far_lit, mid_lit, mix(near_lit, wtr_pal(WTR_PAL_FOAM), 0.5));
				// 远处的镜面倒影保住物体本色（倒影视口存 sRGB 字节）
				P.rgb = mix(P.rgb, toon_srgb_decode(R_obj.rgb) * WTR_FAR_DARK, calm * WTR_FAR_OBJ_KEEP);
				// 碎片之间的细缝：到前向量化边界的像素距离，只画在物体上——倒影被切成一条条横片
				float qd = abs(fract(off_low.y / WTR_QUANT + 0.5) - 0.5);
				float qpx = (0.5 - qd) / max(fq.y, 1e-4);
				crack = (1.0 - smoothstep(0.35, 0.9, qpx)) * (1.0 - smoothstep(0.08, 0.25, max(fq.x, fq.y))) * P.a * (1.0 - calm);
			}
		}
		float f_obj = mix(WTR_OBJ_F.x, WTR_OBJ_F.y, s1);
		float F = clamp(mix(f_sky, f_obj, P.a * (1.0 - crack * 0.7)), 0.0, 1.0);
		vec3 rc = mix(sky_col, P.rgb, P.a);
		// 夜里透出色几乎看不见，水面映着夜空
		float k_body = (1.0 - F) * mix(WTR_NIGHT_BODY, 1.0, day);
		lit = mix(rc, body_lit, k_body);
		sh = mix(rc * mix(1.0, shade_ratio, 0.6), body_sh, k_body);
		float seam_k = crack * WTR_SEAM_DL / max(sqrt(wtr_luma(lit)), 0.05);
		lit *= 1.0 - seam_k;
		sh *= 1.0 - seam_k;

		// ---- 波纹线：亮线是高处天色抬亮，暗线是水色的影
		vec3 R = R0;
		vec3 sky_hi = wtr_sky(normalize(vec3(R.x, max(R.y, 0.0) + 0.5, R.z)));
		vec3 key_col = wtr_pal(PAL_SUN_LIGHT);
		vec3 lift = mix(vec3(wtr_luma(sky_hi) * 0.5 + 0.55), key_col * 0.9, 0.25 + 0.35 * (1.0 - day));
		vec3 line_lit = mix(sky_hi, lift, WTR_LINE_LIFT);
		lit = mix(lit, line_lit, hi);
		sh = mix(sh, line_lit * shade_ratio, hi);
		lit = mix(lit, lit * shade_ratio * WTR_DARK_LINE, lo);
		sh = mix(sh, sh * shade_ratio * WTR_DARK_LINE, lo);

		// ---- 主光反光带（碎金带、月光银带，§1.3、§7.3）：主光倒影方向上，竖向放宽；由横向亮短段堆成
		vec3 K = normalize(key_dir);
		float k_el = asin(clamp(K.y, -1.0, 1.0));
		float r_el = asin(clamp(R.y, -1.0, 1.0));
		vec2 rk = normalize(K.xz + vec2(1e-6));
		vec2 rr = normalize(R.xz + vec2(1e-6));
		float az = acos(clamp(dot(rk, rr), -1.0, 1.0));
		float az_w = WTR_GLINT_AZ * (1.0 + 2.5 * smoothstep(0.0, 0.25, abs(r_el - k_el)));
		float band = (1.0 - smoothstep(az_w * 0.6, az_w, az)) * (1.0 - smoothstep(WTR_GLINT_EL * 0.5, WTR_GLINT_EL, abs(r_el - k_el)));
		// 远处短段小于像素时，反光带并成一条按横向噪声断开的实带
		// 远处并成一条镜面里的金带：按到相机水平距离的对数分出横纹（屏幕上是一道道横向亮纹），一拍二地慢慢移
		float dist_h = max(length(v_wpos.xz - cam.xz), 1.0);
		float streak = step(0.42, fract(log2(dist_h) * WTR_GLINT_STREAK - t12 * 0.6));
		float glint_far = mix(0.35, 1.0, streak) * 0.7;
		float glint_fade = (1.0 - smoothstep(0.12, 0.3, max(fwa, fwb) * WTR_GLINT_SCALE)) * (1.0 - calm);
		float glint_mask = mix(glint_far, max(dg.mask, dh.mask * line_fade), glint_fade);
		float night = 1.0 - day;
		float strength = max(0.0, 1.0 - max(k_el, 0.0) / radians(60.0)) * (1.0 - overcast) * key_strength * mix(1.0, 0.5, night);
		glow += key_col * WTR_GLINT_GAIN * band * glint_mask * strength;

		// ---- 四芒星闪点（太阳较高时，亮线短段中心上零星的白点；阴度 > 0.5 时为 0；同屏不超过 40）
		vec3 Rn = reflect(D, Nw);
		float star_angle = acos(clamp(dot(Rn, K), -1.0, 1.0));
		float star_on = dh.on * (1.0 - step(WTR_STAR_ANGLE, star_angle));
		star_on *= step(wtr_hash3(ivec2(dh.centre * 7.0), 13u), WTR_STAR_DENSITY * (1.0 - smoothstep(12.0, 140.0, dist)));
		star_on *= smoothstep(radians(12.0), radians(28.0), k_el) * (1.0 - smoothstep(0.4, 0.5, overcast));
		if (star_on > 0.0) {
			// 短段中心换回世界坐标（河渠的格沿流向排并随流平移）
			vec2 cw = dh.centre * WTR_DASH_CELL;
			if (flow_speed > 1e-4) {
				vec2 along = v_flow / flow_speed;
				cw = (dh.centre.x * WTR_DASH_CELL + t12 * flow_speed) * along + dh.centre.y * WTR_DASH_CELL * vec2(-along.y, along.x);
			}
			vec3 c_w = vec3(cw.x, v_wpos.y, cw.y);
			vec4 cc = PROJECTION_MATRIX * (VIEW_MATRIX * vec4(c_w, 1.0));
			vec2 cpx = (cc.xy / cc.w * vec2(0.5, -0.5) + 0.5) * VIEWPORT_SIZE;
			vec2 dpx = FRAGCOORD.xy - cpx;
			float cell_px = WTR_DASH_CELL / dist * VIEWPORT_SIZE.y * PROJECTION_MATRIX[1][1] * 0.5;
			float arm = clamp(cell_px * 0.25, 1.5, WTR_STAR_PX);
			float sx = (1.0 - smoothstep(0.35, 0.9, abs(dpx.y))) * max(0.0, 1.0 - abs(dpx.x) / arm);
			float sy = (1.0 - smoothstep(0.35, 0.9, abs(dpx.x))) * max(0.0, 1.0 - abs(dpx.y) / arm);
			float gain = mix(WTR_STAR_HDR.x, WTR_STAR_HDR.y, wtr_hash3(ivec2(dh.centre * 7.0), 23u));
			glow += key_col * gain * max(sx, sy) * star_on;
		}

		// ---- 岸边泡沫线：有断口，线宽随波起伏；外侧再一道断续的细线；冬季更宽更白（§1.4）
		float winter = clamp(snow, 0.0, 1.0);
		float fw_m = mix(WTR_FOAM_W, WTR_FOAM_W_WINTER, winter) * (channel ? WTR_FOAM_CHANNEL.x : 1.0);
		fw_m *= 1.0 + WTR_FOAM_PULSE * sin(2.0 * PI * WTR_FOAM_K / 480.0 * t12 + p.x * 0.9 - p.y * 0.7);
		float gaps = mix(WTR_FOAM_GAP, WTR_FOAM_GAP * 0.5, winter);
		float foam = (1.0 - wtr_aa_step(fw_m, d)) * step(gaps, wtr_noise(p * 0.32 + vec2(13.0, 71.0)));
		float band2 = wtr_aa_step(fw_m * 1.9, d) * (1.0 - wtr_aa_step(fw_m * 1.9 + 0.08, d)) * step(0.52, wtr_noise(p * 1.7 + vec2(5.0, 29.0)));
		foam = max(foam, band2 * (channel ? 0.0 : 0.85)) * mix(channel ? WTR_FOAM_CHANNEL.y : WTR_FOAM_ALPHA, 1.0, winter);
		lit = mix(lit, wtr_pal(WTR_PAL_FOAM), foam);
		sh = mix(sh, wtr_pal(WTR_PAL_FOAM + 1), foam);
		glow *= 1.0 - foam;
	} else {
		// ---- 水下天花板：浅色平涂，波纹线照画（§7.5）
		vec3 ceil_col = mix(mid_lit, wtr_sky(vec3(0.0, 1.0, 0.0)), 0.45);
		lit = mix(ceil_col, ceil_col * 1.25, hi);
		lit = mix(lit, deep_lit, lo * 0.5);
		sh = lit;
	}

	ALBEDO = vec3(1.0);
	METALLIC = 0.0;
	SPECULAR = 0.0;
	ROUGHNESS = float(clamp(mat_id, 0, 127)) / 127.0;
	EMISSION = sh + glow;
	v_diff = lit - sh;
	vec4 fg = fog_eval(v_wpos, cam);
	// 湖面雾（fog_low.z）只在水面上，由水面着色器按同一个量加（fog.gdshaderinc 的约定）
	float a_lake = clamp(fog_low.z, 0.0, 1.0) * (1.0 - exp(-dist / 60.0));
	FOG = vec4(fg.rgb, 1.0 - (1.0 - fg.a) * (1.0 - a_lake));
}

void light() {
	if (LIGHT_IS_DIRECTIONAL) {
		// 两阶：阴影图阈值化后按 shadow_strength 混（月影 0.45、阴天 ≥ 0.2），乘主光强度
		float sh_map = smoothstep(0.4, 0.6, ATTENUATION);
		float sh_key = mix(1.0, sh_map, shadow_strength);
		DIFFUSE_LIGHT += v_diff * sh_key * key_strength;
	}
}
