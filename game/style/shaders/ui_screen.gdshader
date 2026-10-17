// ui_screen.gdshader（WP-35，白名单；UI-VISUAL）
// 界面层唯一读屏幕纹理的着色器，只在低频场合挂上：
//   mode 0 整页揭示：挂在 CanvasGroup 上（只在揭示的那几百毫秒把页挂进组），
//     reveal_kind 0 = M2 扫描揭示（前沿 1 物理像素亮线 + 12 px 指数淡出带，扫过之后不留痕）、
//     1 = M3 网格逐格点亮（构造网格落定后消失）、2 = M6 量化出入场（Bayer 4×4，4 阶）。
//     CanvasGroup 的缓冲是预乘 alpha，读出后先除回。
//   mode 1 大页磨砂：配 BackBufferCopy(Rect)，在页面形状（左上切角）里画 LOD 3.5 的模糊背景；
//     页面底色（surface.frost，84% / 80%）由压在上面的 ui_panel 面板画。
// 只在这里出现 hint_screen_texture：引擎按着色器静态判定要不要先拷贝背景，所以读屏的效果不能并进 ui_panel。
// 颜色只来自 front_day / front_night（UiMaterials 从 ui.json 的 trace 令牌写入）；不写字面色值。
// 全局参数只读 ui_night；不用 TIME。
shader_type canvas_item;
render_mode unshaded;

#include "res://style/shaders/globals.gdshaderinc"

uniform int mode : hint_enum("Reveal", "Frost") = 0;
uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_linear_mipmap;
uniform vec4 front_day : source_color;   // themes.light.trace
uniform vec4 front_night : source_color; // themes.dark.trace
uniform float front_delta = 0.20;        // motion.limits.frontDeltaMax
uniform float fade_band = 12.0;          // motion.durations.scan.fadeBand（逻辑像素）
uniform vec2 grid_geo = vec2(16.0, 0.08); // 格子边长（逻辑像素）、网格线透明度
uniform float grid_wave = 0.78;          // 最后一格开始点亮的进度
uniform float frost_lod = 3.5;           // metrics.material.frost.lod
uniform float corner_minor = 2.0;
uniform float front_width = 0.02;

instance uniform int reveal_kind = 0;
instance uniform float reveal = 1.0;                       // 0 → 1；1 = 揭示完成
instance uniform vec4 origin_dir = vec4(0.0, 0.0, 1.0, 1.0); // 起点（UV）与扫描方向
instance uniform float chamfer = 14.0;                     // mode 1 的页面切角（逻辑像素）
instance uniform float pin_night = 0.0;

varying vec2 v_scale;

void vertex() {
	mat4 m = CANVAS_MATRIX * MODEL_MATRIX;
	v_scale = vec2(length(m[0].xy), length(m[1].xy));
}

const int BAYER[16] = int[16](0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5);

float night_at(vec2 frag, float screen_w) {
	float u = frag.x / screen_w;
	float front = mix(-front_width, 1.0 + front_width, ui_night);
	return clamp((front - u) / front_width + 0.5, 0.0, 1.0);
}

float band(float x, float a, float b) {
	return clamp(min(x + 0.5, b) - max(x - 0.5, a), 0.0, 1.0);
}

float hash12(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float sd_chip(vec2 p, vec2 size, float c, float k) {
	float s = 0.70710678;
	float d = max(max(-p.x, p.x - size.x), max(-p.y, p.y - size.y));
	d = max(d, (c - p.x - p.y) * s);
	d = max(d, (k - (size.x - p.x) - p.y) * s);
	d = max(d, (k - p.x - (size.y - p.y)) * s);
	d = max(d, (k - (size.x - p.x) - (size.y - p.y)) * s);
	return d;
}

void fragment() {
	float screen_w = 1.0 / SCREEN_PIXEL_SIZE.x;
	float s1 = max(v_scale.x, 0.0001);
	vec2 du = abs(dFdx(UV)) + abs(dFdy(UV));
	vec2 box = 1.0 / max(du, vec2(1e-6));
	vec2 org = floor(FRAGCOORD.xy - UV * box + 0.5);
	vec2 p = FRAGCOORD.xy - org;
	vec2 size = floor(box + 0.5);

	if (mode == 1) {
		// 大页磨砂：只在页面形状里画模糊背景
		vec4 b = textureLod(screen_tex, SCREEN_UV, frost_lod);
		float d = sd_chip(p, size, floor(chamfer * s1 + 0.5), floor(corner_minor * s1 + 0.5));
		COLOR = vec4(b.rgb, clamp(0.5 - d, 0.0, 1.0) * COLOR.a);
	} else {
		vec4 c = textureLod(screen_tex, SCREEN_UV, 0.0);
		if (c.a > 0.0001) {
			c.rgb /= c.a;
		}
		float n = max(night_at(FRAGCOORD.xy, screen_w), pin_night);
		vec3 front_col = mix(front_day.rgb, front_night.rgb, n);
		float vis = 1.0;
		if (reveal < 1.0) {
			if (reveal_kind == 0) {
				// M2：前沿按方向扫过，前沿之后是内容，前沿本身 1 物理像素亮线，后面 12 px 淡出带
				vec2 dir = normalize(origin_dir.zw);
				vec2 o = origin_dir.xy * size;
				float proj = dot(p - o, dir);
				float span = abs(dir.x) * size.x + abs(dir.y) * size.y;
				float band_px = fade_band * s1;
				float front = reveal * (span + band_px);
				float behind = front - proj;
				vis = clamp(behind + 0.5, 0.0, 1.0);
				float line = band(behind, 0.0, 1.0);
				float fade = behind > 0.0 ? exp(-behind / (band_px / 3.0)) : 0.0;
				c.rgb = mix(c.rgb, front_col, clamp((line * 0.6 + fade * 0.35) * front_delta / 0.2, 0.0, 1.0) * step(0.001, c.a));
			} else if (reveal_kind == 1) {
				// M3：格子按到起点的距离加一点哈希错开，每格 4 阶点亮，中途亮一下，落定后网格消失
				float cell = max(floor(grid_geo.x * s1 + 0.5), 4.0);
				vec2 id = floor(p / cell);
				vec2 o = origin_dir.xy * size;
				float maxd = length(max(o, size - o));
				float t0 = length((id + 0.5) * cell - o) / max(maxd, 1.0) * (grid_wave - 0.08) + hash12(id) * 0.08;
				float lp = clamp((reveal - t0) / (1.0 - grid_wave), 0.0, 1.0);
				float lq = floor(lp * 4.0) / 4.0;
				vis = lq;
				float bump = (lq > 0.0 && lq < 1.0) ? sin(3.14159265 * lq) : 0.0;
				c.rgb = mix(c.rgb, front_col, bump * front_delta);
				// 构造网格：1 物理像素的格线，只在还没落定的格子上，落定后消失
				vec2 f = mod(p, cell);
				float grid = (f.x < 1.0 || f.y < 1.0) ? 1.0 : 0.0;
				float ga = grid * grid_geo.y * (1.0 - lq);
				float ca = c.a * lq;
				float oa = ga + ca * (1.0 - ga);
				vec3 orgb = (front_col * ga + c.rgb * ca * (1.0 - ga)) / max(oa, 0.0001);
				c = vec4(orgb, oa);
				vis = 1.0;
			} else {
				// M6：Bayer 4×4，每块 2×2 或 3×3 物理像素
				float blk = s1 > 1.25 ? 3.0 : 2.0;
				ivec2 q = ivec2(mod(floor(p / blk), 4.0));
				vis = step(float(BAYER[q.x + q.y * 4]) / 16.0, reveal - 0.0001);
			}
		}
		COLOR *= vec4(c.rgb, c.a * vis);
	}
}
