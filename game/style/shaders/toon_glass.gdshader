// toon_glass.gdshader：窗玻璃（WP-04）。映天、斜条高光、低太阳眩光、窗灯与简化的室内视差。
// 规格：ART-STYLE §5.1、§5.4、§10；ENG-PERF §1.4、§10.3；WLD-INTERIORS §5.1。
// 顶点数据（VF-B，暂定，待与 WP-03、WP-14 对齐）：
//   UV  ：窗格坐标，floor(UV) = (列, 行)，fract(UV) = 格内位置；UV.x 沿立面向右，UV.y 向上
//   UV2 ：x = 本块玻璃的窗号基址；y = 灯光通道号 + 32 × 列数
//   窗号 = 基址 + 行 × 列数 + 列
// 主线版的室内是程序化的房间盒（背墙、侧墙、顶棚、地面和一条家具剪影），窗卡图层（Texture2DArray）接入后替换。
shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert, specular_disabled, ambient_light_disabled;

#include "res://style/shaders/toon.gdshaderinc"

const vec2 GLASS_PANE_M = vec2(1.6, 1.9);   // 窗格的名义尺寸（m），室内视差用
const float GLASS_STREAK = 0.55;            // 斜条强度（40%–70%）
const float GLASS_STREAK_SLOPE = 0.75;      // 斜条方向：与竖直约成 37°
const float GLASS_REFLECT_MIN = 0.28;       // 正视时映天的比例
const float GLASS_REFLECT_MAX = 0.85;       // 掠射时映天的比例
const float GLASS_LAMP_DAY = 0.6;           // 白天亮窗的亮度（太阳高时）
const float GLASS_LAMP_NIGHT = 1.1;         // 黄昏以后亮窗的亮度
const float GLASS_ROOM_BACK = 0.78;         // 室内各面的明度：背墙、侧墙、地面、顶棚、顶棚灯带、家具剪影
const float GLASS_ROOM_SIDE = 0.6;
const float GLASS_ROOM_FLOOR = 0.42;
const float GLASS_ROOM_CEIL = 0.92;
const float GLASS_ROOM_STRIP = 0.45;
const float GLASS_ROOM_FURNITURE = 0.32;
const float GLASS_COOL_ROOM = 0.8;           // 冷白窗室内的明度系数
const float GLASS_SKY_LIFT = 0.35;          // 映天时把反射方向抬高一点，取更冷、更亮的天色
const float GLASS_GLARE_GAIN = 1.25;        // 眩光的亮度（略超 1，让柔光抓住）

float glass_band(float x, float c, float hw, float fw) {
	return 1.0 - smoothstep(hw - fw, hw + fw, abs(x - c));
}

// 室内视差：从窗面沿视线进房间，求与房间盒的交点，按面给明度
vec3 glass_room(vec2 puv, vec3 rd, float depth, vec3 lamp, float desk_x0, float desk_x1) {
	vec3 p0 = vec3(puv * GLASS_PANE_M, 0.0);
	vec3 bmin = vec3(-0.6, -0.9, 0.0);
	vec3 bmax = vec3(GLASS_PANE_M.x + 0.6, GLASS_PANE_M.y + 0.4, depth);
	float tz = (bmax.z - p0.z) / max(rd.z, 1e-4);
	float tx = abs(rd.x) > 1e-5 ? ((rd.x > 0.0 ? bmax.x : bmin.x) - p0.x) / rd.x : 1e6;
	float ty = abs(rd.y) > 1e-5 ? ((rd.y > 0.0 ? bmax.y : bmin.y) - p0.y) / rd.y : 1e6;
	float t = min(tz, min(tx, ty));
	vec3 h = p0 + rd * t;
	float k;
	if (t == tz) {
		k = GLASS_ROOM_BACK;
		// 背墙下部一条家具剪影（桌、柜）
		float fx = (h.x - bmin.x) / (bmax.x - bmin.x);
		if (h.y < bmin.y + 0.8 && fx > desk_x0 && fx < desk_x1) {
			k = GLASS_ROOM_FURNITURE;
		}
	} else if (t == ty) {
		if (rd.y > 0.0) {
			// 顶棚，进深中间一条灯带
			k = GLASS_ROOM_CEIL + GLASS_ROOM_STRIP * (1.0 - smoothstep(0.15, 0.3, abs(h.z - depth * 0.45)));
		} else {
			k = GLASS_ROOM_FLOOR;
		}
	} else {
		k = GLASS_ROOM_SIDE;
	}
	k *= mix(1.0, 0.8, clamp(h.z / depth, 0.0, 1.0));
	return lamp * k;
}

void vertex() {
	TOON_VERTEX
}

void fragment() {
	TOON_FRAGMENT_BEGIN
	ToonCol tcol = toon_colors(tctx, tmat);

	// 窗号与灯
	ivec2 pane = ivec2(floor(UV));
	vec2 puv = fract(UV);
	float packed = round(UV2.y);
	int ch = int(mod(packed, 32.0));
	int cols = int(floor(packed / 32.0));
	int wbase = int(round(UV2.x));
	int wid = wbase + pane.y * cols + pane.x;
	vec4 wov = toon_window_override(wid, (tctx.flags & TOON_F_OUTSKIRT) == 0);
	float on = lamp_window_on(wid, wbase, cols, ch, wov);
	bool cool = lamp_window_cool(wid, wov);
	// 冷白窗的室内压低一点明度，夜里近看不成白块
	vec3 lampc = cool ? pal_cur(PAL_WINDOW_COOL) * GLASS_COOL_ROOM : pal_cur(PAL_WINDOW_WARM);

	// 视线与立面基
	vec3 vdir = normalize(tctx.wpos - tctx.cam);
	vec3 n = tctx.n_w;
	vec3 right = cross(vec3(0.0, 1.0, 0.0), n);
	right = length(right) > 1e-3 ? normalize(right) : vec3(1.0, 0.0, 0.0);
	vec3 up = cross(n, right);
	float nv = clamp(dot(-vdir, n), 0.0, 1.0);

	// 1. 映天：反射方向的天空色带，掠射越强映得越多
	vec3 rdir = reflect(vdir, n);
	rdir.y = max(rdir.y, 0.0) * (1.0 - GLASS_SKY_LIFT) + GLASS_SKY_LIFT;
	vec3 sky = fog_sky(rdir);
	float fres = mix(GLASS_REFLECT_MIN, GLASS_REFLECT_MAX, pow(1.0 - nv, 4.0));
	tcol.lit = mix(tcol.lit, sky, fres);
	tcol.shade = mix(tcol.shade, sky * 0.85, fres);

	// 2. 低太阳眩光（ART-STYLE §5.1）：只在受光的玻璃上，经"受光 − 影"交给 light()
	float h_sun = degrees(asin(clamp(sun_dir.y, -1.0, 1.0)));
	vec2 sh = sun_dir.xz;
	vec2 nh = n.xz;
	float facing = 0.0;
	if (length(sh) > 1e-4 && length(nh) > 1e-4) {
		facing = smoothstep(0.5, 0.9, dot(normalize(nh), normalize(sh)));
	}
	float glare = (1.0 - smoothstep(6.0, 14.0, h_sun)) * smoothstep(-2.0, 4.0, h_sun) * facing
			* (1.0 - toon_overcast()) * (1.0 - smoothstep(1.0, 2.5, fog_weather_mult()));
	tcol.lit = mix(tcol.lit, pal_cur(PAL_GLASS_GLARE) * GLASS_GLARE_GAIN, glare * (1.0 - 0.5 * on));

	// 3. 斜条：一宽一窄，滤色叠在受光色和影色上；夜间暗窗减半，亮窗不叠
	float night = smoothstep(-0.03, -0.12, sun_dir.y);
	float hj = lamp_hash01(uint(wid) * 2u + 3u) - 0.5;
	float u = puv.x - puv.y * GLASS_STREAK_SLOPE + 0.12 * hj;
	float fw = max(fwidth(u), 1e-4);
	float a_wide = glass_band(u, 0.08, 0.10, fw) * 0.5;
	float a_thin = glass_band(u, 0.38, 0.03, fw);
	float a = 1.0 - (1.0 - a_wide) * (1.0 - a_thin);
	a *= GLASS_STREAK * (1.0 - 0.5 * night) * (1.0 - on);
	tcol.lit += a * (vec3(1.0) - min(tcol.lit, vec3(1.0)));
	tcol.shade += a * (vec3(1.0) - min(tcol.shade, vec3(1.0)));

	// 4. 窗灯：亮窗里是一间房（室内视差），玻璃本身的明暗退到很弱
	if (on > 0.0) {
		vec3 rd = vec3(dot(vdir, right), dot(vdir, up), dot(vdir, -n));
		float hd = lamp_hash01(uint(wid) * 2u + 4u);
		float depth = hd < 0.33 ? 3.0 : (hd < 0.66 ? 4.5 : 6.0);
		float dx0 = 0.15 + 0.35 * lamp_hash01(uint(wid) * 2u + 5u);
		vec3 room = glass_room(puv, rd, depth, lampc, dx0, dx0 + 0.3);
		// 一侧窗帘：在窗面上，透着灯光
		int cs = toon_window_curtain(wov);
		float cw = cs == 2 ? 1.01 : (cs == 1 ? 0.0 : 0.25 * step(0.55, lamp_hash01(uint(wid) * 2u + 6u)));
		float curtain = 1.0 - smoothstep(cw - 0.01, cw + 0.01, puv.x);
		room = mix(room, lampc * 0.93, curtain);
		float keep = fres * 0.5;
		tcol.lit *= mix(1.0, keep, on);
		tcol.shade *= mix(1.0, keep, on);
		float dusk = 1.0 - smoothstep(-2.0, 14.0, h_sun);
		tcol.extra = room * mix(GLASS_LAMP_DAY, GLASS_LAMP_NIGHT, dusk) * on * (1.0 - keep);
	}

	TOON_FRAGMENT_END(tcol)
}

void light() {
	TOON_LIGHT
}
