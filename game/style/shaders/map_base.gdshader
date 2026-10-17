// map_base.gdshader（WP-48b，白名单；SYS-ATLAS、ENG-PERF）
// 平面地图唯一的画布着色器，小地图与大地图共用。两种用法由材质 uniform mode 区分（不用 #define）：
//   0 底图：按本机烘焙的精确矢量距离场（WP-48a）逐层上色，画层顺序与色名见 SYS-ATLAS；
//   1 地标线描：从 map_glyphs（MSDF 图集）里画一枚，颜色取 map.landmark 的描边色。
// 颜色只来自色表 pal_day / pal_night（下标见 C_*，与 Campus.Hud.Map.MapPalette.Roles 同序，
// 由界面从 ui.json 的 map.light / map.dark 写入）；这里不写字面色值。
// 日夜：theme_front 与界面的 ui_night 同一语义（扫描前沿的位置），由界面写成材质 uniform；
// 常驻的小地图写 instance uniform pin_night = 1，固定深屏（UI-VISUAL）。
// 全局参数只读 season（绿地底色随季节轻微变化，SYS-ATLAS）。不用 TIME。
// 线一律不细于 1 物理像素，界面缩放 ≥ 150% 时按缩放取整加宽（SYS-ATLAS 末条）。
shader_type canvas_item;
render_mode unshaded;

#include "res://style/shaders/globals.gdshaderinc"

// ---- 色表下标（= MapPalette.Roles，只在末尾追加） ----
const int C_BASE = 0;
const int C_BASE_CROSS = 1;
const int C_OUT_LINE = 2;
const int C_OUT_FILL = 3;
const int C_EDGE = 4;
const int C_LAND = 5;
const int C_GREEN_SPRING = 6;   // 6–9 四季，按 season.x 循环插值
const int C_GREEN_DOTS = 10;
const int C_WATER = 11;
const int C_WATER_HATCH = 12;
const int C_WATER_SHORE = 13;
const int C_PLAZA = 14;
const int C_ROAD = 15;
const int C_ROAD_LINE = 16;
const int C_SPORT_LINE = 17;
const int C_SPORT_INNER = 18;
const int C_BUILDING = 19;
const int C_BUILDING_LINE = 20;
const int C_LANDMARK = 21;
const int C_LANDMARK_LINE = 22;
const int C_BOUND = 23;
const int C_INTERIOR = 24;
const int C_INTERIOR_LINE = 25;
const int ROLE_COUNT = 32;

// ---- map_sdf_b.G 的要素类别（最近邻、膨胀 2 纹素；编码由 WP-48a 的 map.manifest.json 定，这里是约定的初值） ----
const int CAT_BUILDING = 1;
const int CAT_LANDMARK = 2;
const int CAT_PLAZA = 3;
const int CAT_SPORT = 4;
const int CAT_BRIDGE = 5;

// ---- instance flags 位 ----
const int F_CROSSES = 1;    // 地图页底的 100 m 十字测试点（只在大地图上）
const int F_HIGHLIGHT = 2;  // 按 highlight 编号高亮一个地点

uniform int mode : hint_enum("Base", "Glyph") = 0;
uniform vec4 pal_day[32] : source_color;
uniform vec4 pal_night[32] : source_color;
uniform float theme_front = 0.0;        // 0 全屏晶白，1 全屏深屏；切换时 0.6 s 走完（UI-VISUAL）
uniform float front_width = 0.02;       // themeSwitch.frontWidth

// 底图数据纹理（SYS-ATLAS）：不压缩 RGBA8，带 mipmap。距离夹在 ±8 m，128 是边界，内负外正。
uniform sampler2D map_sdf_a : filter_linear_mipmap, repeat_disable;     // R 水，G 绿地，B 道路，A 建筑
uniform sampler2D map_sdf_b : filter_linear_mipmap, repeat_disable;     // R 地点编号，G 类别（texelFetch），B 运动场与广场，A 校界
uniform sampler2D map_sdf_out : filter_linear_mipmap, repeat_disable;   // R 外围街区，G 外围道路，B 外围水体，A 箱庭外缘
uniform sampler2D map_sdf_ring : filter_linear_mipmap, repeat_disable;  // 同上，atlasRect 外圈
uniform sampler2D map_glyphs : filter_linear_mipmap, repeat_disable;    // 地标线描 MSDF 图集
uniform vec4 rect_a = vec4(-1056.0, -1056.0, 2112.0, 2112.0);    // map_sdf_a / b 的范围：min.x、min.z、宽、高（m）
uniform vec4 rect_out = vec4(-1280.0, -1280.0, 2560.0, 2560.0);  // map_sdf_out：dataRect = map_rect
uniform vec4 rect_ring = vec4(-1536.0, -1536.0, 3072.0, 3072.0); // map_sdf_ring：atlasRect
uniform float sdf_range = 8.0;          // 编码的距离上限（m）
uniform float seam_band = 24.0;         // ±1280 m 边线内侧的混合带（m）
uniform float data_ready = 0.0;         // 0：底图还没烘好，只画校内用地一种颜色（SYS-ATLAS）
uniform uvec2 reveal = uvec2(4294967295u, 4294967295u);   // 64 位揭示掩码，每位一个大区（SYS-ATLAS）；观光全为 1
uniform int place_region[256];          // 地点编号 → 大区位号（0–63），供 reveal 用；观光期不读

// 版图的几何（逻辑像素，1920×1080 基准；地图色的几何在 ui_metrics.json 的 metrics.map）
uniform vec2 bound_dash = vec2(5.0, 3.0);   // map.bound：5/3 虚线
uniform float dot_pitch = 8.0;              // 绿地点阵间距
uniform float dot_radius = 0.75;
uniform float hatch_pitch = 6.0;            // 水体 45° 影线间距
uniform float cross_pitch = 100.0;          // 十字测试点间距（m）
uniform float cross_arm = 3.0;              // 十字半臂长
uniform float sport_inset = 5.0;            // 跑道内线离运动场边线（m）
uniform float glyph_px_range = 4.0;         // MSDF 的像素距离范围

// ---- 单个控件：instance uniform（上限 16，这里用 7 个） ----
instance uniform vec4 view = vec4(0.0, 0.0, 1.0, 0.0);   // 视图中心 x、z（m），每米逻辑像素，旋转（弧度；随镜头时 = −偏航）
instance uniform vec2 view_size = vec2(226.0, 226.0);   // 控件尺寸（逻辑像素）
instance uniform float pin_night = 0.0;                 // 1 = 常驻件固定深屏
instance uniform int flags = 0;
instance uniform int highlight = -1;                    // 地点编号；-1 不高亮
instance uniform vec4 glyph_rect = vec4(0.0, 0.0, 1.0, 1.0);   // mode 1：图集里的 UV 矩形（左上、右下）
instance uniform float chamfer = 0.0;                   // 左上切角边长（逻辑像素）：大地图的地图纸切 14（UI-VISUAL）

varying vec2 v_local;

void vertex() {
	v_local = VERTEX;
}

float night_at(float u) {
	float front = mix(-front_width, 1.0 + front_width, theme_front);
	return clamp((front - u) / front_width + 0.5, 0.0, 1.0);
}

vec3 pal(int i, float n) {
	return mix(pal_day[i].rgb, pal_night[i].rgb, n);
}

float pal_a(int i, float n) {
	return mix(pal_day[i].a, pal_night[i].a, n);
}

// d：有符号距离（m，内负外正）；mpp：每物理像素多少米
float fill_cov(float d, float mpp) {
	return clamp(0.5 - d / mpp, 0.0, 1.0);
}

// 以 d = 0 为中线、宽 w 物理像素的线
float line_cov(float d, float mpp, float w) {
	return clamp(0.5 * w + 0.5 - abs(d) / mpp, 0.0, 1.0);
}

float decode(float v) {
	return (v - 0.5) * 2.0 * sdf_range;
}

vec2 rect_uv(vec2 wp, vec4 r) {
	return (wp - r.xy) / r.zw;
}

bool in_rect(vec2 wp, vec4 r) {
	vec2 uv = rect_uv(wp, r);
	return all(greaterThanEqual(uv, vec2(0.0))) && all(lessThanEqual(uv, vec2(1.0)));
}

// 到矩形边的距离，内正外负（m）
float inside_dist(vec2 wp, vec4 r) {
	vec2 lo = wp - r.xy;
	vec2 hi = r.xy + r.zw - wp;
	return min(min(lo.x, hi.x), min(lo.y, hi.y));
}

float median3(vec3 m) {
	return max(min(m.r, m.g), min(max(m.r, m.g), m.b));
}

vec3 green_now(float n) {
	float s = mod(season.x, 4.0);
	int i = int(floor(s));
	int j = (i + 1) % 4;
	return mix(pal(C_GREEN_SPRING + i, n), pal(C_GREEN_SPRING + j, n), fract(s));
}

bool revealed(int place) {
	if (place <= 0) {
		return true;
	}
	int bit = clamp(place_region[clamp(place, 0, 255)], 0, 63);
	uint word = bit < 32 ? reveal.x : reveal.y;
	return ((word >> uint(bit & 31)) & 1u) == 1u;
}

void fragment() {
	float n = max(night_at(FRAGCOORD.x * SCREEN_PIXEL_SIZE.x), pin_night);
	if (mode == 1) {
		// 地标线描：MSDF，屏幕像素距离按图集纹素与屏幕像素之比换算，任意缩放都锐利
		vec2 guv = mix(glyph_rect.xy, glyph_rect.zw, UV);
		float sd = median3(texture(map_glyphs, guv).rgb) - 0.5;
		vec2 unit = vec2(glyph_px_range) / vec2(textureSize(map_glyphs, 0));
		vec2 screen_tex = vec2(1.0) / max(fwidth(guv), vec2(1e-6));
		float spr = max(0.5 * dot(unit, screen_tex), 1.0);
		COLOR = vec4(pal(C_LANDMARK_LINE, n), clamp(sd * spr + 0.5, 0.0, 1.0) * COLOR.a);
	} else {
		// 屏幕 → 地图：北朝上时屏幕 +x 是东（+x），屏幕 +y 是南（+z）
		vec2 q = v_local - view_size * 0.5;
		float c = cos(view.w);
		float s = sin(view.w);
		vec2 wp = view.xy + vec2(c * q.x - s * q.y, s * q.x + c * q.y) / view.z;
		float lpp = max(length(vec2(dFdx(v_local.x), dFdy(v_local.x))), 1e-4);   // 每物理像素多少逻辑像素
		float mpp = lpp / view.z;                                                   // 每物理像素多少米
		float scale = 1.0 / lpp;
		float hair = scale >= 1.5 ? floor(scale + 0.5) : 1.0;                       // 发丝线宽（物理像素）
		vec2 hp = wp * view.z;                                                      // 随地图走的逻辑像素坐标，图案锚在地面上
		float cut = chamfer > 0.0 ? clamp((v_local.x + v_local.y - chamfer) * 0.70710678 / lpp + 0.5, 0.0, 1.0) : 1.0;

		vec3 col = pal(C_BASE, n);
		bool crosses = (flags & F_CROSSES) != 0;

		if (data_ready < 0.5) {
			// 底图未就绪：只画校内用地一种颜色，大地图上叠十字测试点
			col = pal(C_LAND, n);
			if (crosses) {
				vec2 f = (fract(wp / cross_pitch + 0.5) - 0.5) * cross_pitch / mpp;
				float arm = cross_arm / lpp;
				float k = max(step(abs(f.x), arm) * line_cov(f.y * mpp, mpp, hair),
						step(abs(f.y), arm) * line_cov(f.x * mpp, mpp, hair));
				col = mix(col, pal(C_BASE_CROSS, n), k);
			}
			COLOR = vec4(col, COLOR.a * cut);
		} else {
			// ---- 外围与箱庭外缘：±1280 m 以内用全分辨率，外圈用半分辨率，边线内侧 24 m 混合 ----
			vec4 o = vec4(1.0);
			if (in_rect(wp, rect_ring)) {
				o = texture(map_sdf_ring, rect_uv(wp, rect_ring));
			}
			float inner = inside_dist(wp, rect_out);
			if (inner > 0.0) {
				vec4 full = texture(map_sdf_out, rect_uv(wp, rect_out));
				o = mix(o, full, clamp(inner / seam_band, 0.0, 1.0));
			}
			float d_block = decode(o.r);
			float d_oroad = decode(o.g);
			float d_owater = decode(o.b);
			float d_edge = decode(o.a);

			// ---- 校内：map_sdf_a、map_sdf_b ----
			vec4 a = vec4(1.0);
			vec4 b = vec4(1.0);
			int place = 0;
			int cat = 0;
			bool inside_a = in_rect(wp, rect_a);
			if (inside_a) {
				vec2 uva = rect_uv(wp, rect_a);
				a = texture(map_sdf_a, uva);
				b = texture(map_sdf_b, uva);
				ivec2 bs = textureSize(map_sdf_b, 0);
				vec4 ids = texelFetch(map_sdf_b, clamp(ivec2(uva * vec2(bs)), ivec2(0), bs - 1), 0);
				place = int(ids.r * 255.0 + 0.5);
				cat = int(ids.g * 255.0 + 0.5);
			}
			float d_water = decode(a.r);
			float d_green = decode(a.g);
			float d_road = decode(a.b);
			float d_build = decode(a.a);
			float d_area = decode(b.b);
			float d_bound = decode(b.a);
			bool shown = revealed(place);

			// 地图页底与十字测试点（只在校外空白区）
			float land = fill_cov(d_bound, mpp);
			if (crosses && land < 1.0 && fill_cov(d_block, mpp) < 0.5) {
				vec2 f = (fract(wp / cross_pitch + 0.5) - 0.5) * cross_pitch / mpp;
				float arm = cross_arm / lpp;
				float k = max(step(abs(f.x), arm) * line_cov(f.y * mpp, mpp, hair),
						step(abs(f.y), arm) * line_cov(f.x * mpp, mpp, hair));
				col = mix(col, pal(C_BASE_CROSS, n), k * (1.0 - land));
			}

			// 外围街区：极淡的填色与轮廓淡线；外围道路、河道只画淡线
			col = mix(col, pal(C_OUT_FILL, n), fill_cov(d_block, mpp) * pal_a(C_OUT_FILL, n));
			col = mix(col, pal(C_OUT_FILL, n), fill_cov(d_owater, mpp) * pal_a(C_OUT_FILL, n));
			col = mix(col, pal(C_OUT_LINE, n), line_cov(d_block, mpp, hair));
			col = mix(col, pal(C_OUT_LINE, n), line_cov(d_oroad, mpp, hair));
			col = mix(col, pal(C_OUT_LINE, n), line_cov(d_owater, mpp, hair));
			// 箱庭外缘：发丝线
			col = mix(col, pal(C_EDGE, n), line_cov(d_edge, mpp, hair));

			if (inside_a) {
				// 校内用地
				col = mix(col, pal(C_LAND, n), land);
				if (shown) {
					// 地点高亮（选中）：用地染一层 map.interior 的底
					if ((flags & F_HIGHLIGHT) != 0 && place == highlight) {
						col = mix(col, pal(C_INTERIOR, n), 0.6 * land);
					}
					// 绿地：平涂 + 8 px 点阵
					float g = fill_cov(d_green, mpp);
					col = mix(col, green_now(n), g);
					vec2 cell = (fract(hp / dot_pitch) - 0.5) * dot_pitch;
					float dotk = clamp((dot_radius - length(cell)) / lpp + 0.5, 0.0, 1.0);
					col = mix(col, pal(C_GREEN_DOTS, n), dotk * g);
					// 水体：平涂 + 45° 影线（版图图层的填充语言）
					float w = fill_cov(d_water, mpp);
					col = mix(col, pal(C_WATER, n), w);
					float u = (hp.x + hp.y) * 0.70710678;
					float hd = abs(fract(u / hatch_pitch + 0.5) - 0.5) * hatch_pitch / lpp;
					col = mix(col, pal(C_WATER_HATCH, n), clamp(0.5 * hair + 0.5 - hd, 0.0, 1.0) * w);
					// 广场、铺装、栈道、桥：比用地略深一档
					if (cat == CAT_PLAZA || cat == CAT_BRIDGE) {
						col = mix(col, pal(C_PLAZA, n), fill_cov(d_area, mpp));
					}
					// 道路：填色加发丝描边（上下文，不承载关键信息）。窄于一个纹素的小路在烘焙时不被侵蚀，
					// 中线处的距离只比 0 略小，按半个物理像素判定，所以至少画出 1 物理像素
					col = mix(col, pal(C_ROAD, n), fill_cov(d_road, mpp));
					col = mix(col, pal(C_ROAD_LINE, n), line_cov(d_road, mpp, hair));
					// 运动场：只描边，跑道画一圈内线；不用暖色
					if (cat == CAT_SPORT) {
						col = mix(col, pal(C_SPORT_LINE, n), line_cov(d_area, mpp, hair));
						col = mix(col, pal(C_SPORT_INNER, n), line_cov(d_area + sport_inset, mpp, hair));
					}
				}
				// 水岸：1 物理像素（未揭示时也画，像"还没曝光的版图"）
				col = mix(col, pal(C_WATER_SHORE, n), line_cov(d_water, mpp, hair));
				// 建筑：填色加 1 物理像素描边；地标填色和描边各深一档
				bool lm = cat == CAT_LANDMARK;
				if (shown) {
					col = mix(col, pal(lm ? C_LANDMARK : C_BUILDING, n), fill_cov(d_build, mpp));
				}
				col = mix(col, pal(lm ? C_LANDMARK_LINE : C_BUILDING_LINE, n), line_cov(d_build, mpp, hair));
				// 校界：5/3 虚线。切线取距离场在地面上的梯度，虚线锚在地面上，平移时不爬动
				if (abs(d_bound) < 2.0 * hair * mpp + mpp) {
					vec2 e = vec2(rect_a.z / float(textureSize(map_sdf_b, 0).x), 0.0);
					float gx = decode(texture(map_sdf_b, rect_uv(wp + e.xy, rect_a)).a) - decode(texture(map_sdf_b, rect_uv(wp - e.xy, rect_a)).a);
					float gz = decode(texture(map_sdf_b, rect_uv(wp + e.yx, rect_a)).a) - decode(texture(map_sdf_b, rect_uv(wp - e.yx, rect_a)).a);
					vec2 t = normalize(vec2(-gz, gx) + vec2(1e-6, 0.0));
					float along = mod(dot(hp, t), bound_dash.x + bound_dash.y);
					col = mix(col, pal(C_BOUND, n), line_cov(d_bound, mpp, hair) * step(along, bound_dash.x));
				}
			}
			COLOR = vec4(col, COLOR.a * cut);
		}
	}
}
