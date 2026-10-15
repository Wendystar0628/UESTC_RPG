// ui_panel.gdshader（WP-35，白名单；UI-VISUAL §11.5）
// 界面层唯一的不读屏幕的画布着色器。kind 区分四种用法：
//   0 面板：StyleBoxFlat 只作载体（白色不透明底，ExpandMargin = margin），本着色器画切角 SDF、
//     物理像素发丝线、顶边内高光、悬停、焦点衍射边（M5）、走线流光（M1）、量化出入场（M6）、
//     选中色散线、过孔签名、晶白环境影与浮层影、深屏解析辉光；同一控件画出的文字和图标
//     （带贴图的绘制）按 role 改色。
//   1 线：Line2D（texture_mode = stretch，UV.x 沿线、UV.y 横跨线宽）画 1 物理像素的线、虚线、
//     M1 流光；flags 含 F_WAVE 时在矩形里画 M7 示波器波形。
//   2 图标：TextureRect 上的横排帧图集，按 step_rate 换帧（M6 的图标换帧），单色取 role。
//   3 文字：Label 等只改 COLOR.rgb（MSDF 的 alpha 已在用户代码之前解出）。
// 颜色只来自色表 pal_day / pal_night（下标见 R_*，与 ui.json 的 shader.panelRoles 同序，
// 由 UiMaterials 从 ui.json 写入）；这里不写字面色值。日夜按 night_at() 取：屏幕 x 小于
// 扫描前沿的像素是夜态，常驻件用 pin_night = 1 固定深屏（UI-VISUAL §11.6）。
// 全局参数只读 ui_night、anim_time、step_rate；不用 TIME。
// 输出是预乘 alpha（blend_premul_alpha），这样辉光能做成真正的叠加光。
shader_type canvas_item;
render_mode blend_premul_alpha, unshaded;

#include "res://style/shaders/globals.gdshaderinc"

// ---- 色表下标（= ui.json shader.panelRoles 的顺序，只在末尾追加） ----
const int R_SURFACE_PANEL = 0;
const int R_SURFACE_SOLID = 1;
const int R_SURFACE_RAISED = 2;
const int R_SURFACE_SUNK = 3;
const int R_SURFACE_SELECTED = 4;
const int R_SURFACE_FROST = 5;
const int R_SURFACE_HOVER = 6;
const int R_SURFACE_ROW_HOVER = 7;
const int R_TEXT_1 = 8;
const int R_TEXT_2 = 9;
const int R_TEXT_OFF = 10;
const int R_TEXT_ACCENT = 11;
const int R_LINE_HAIR = 12;
const int R_LINE_PANEL = 13;
const int R_LINE_CONTROL = 14;
const int R_LINE_HIGHLIGHT = 15;
const int R_LINE_CARD_HOVER = 16;
const int R_PRIMARY_FILL = 17;
const int R_PRIMARY_HOVER = 18;
const int R_PRIMARY_PRESS = 19;
const int R_PRIMARY_INK = 20;
const int R_PRIMARY_LINE = 21;
const int R_GRAPHIC = 22;
const int R_LED_ON = 23;
const int R_LED_HALO = 24;
const int R_GLOW = 25;
const int R_NOW = 26;
const int R_NOW_OUTLINE = 27;
const int R_SCRIM = 28;
const int R_SHADOW_AMBIENT = 29;
const int R_SHADOW_FLOAT = 30;
const int R_SHADOW_CONTACT = 31;
const int R_IRIS_A = 32;
const int R_IRIS_B = 33;
const int R_FOCUS_BACKING = 34;
const int R_TRACE = 35;
const int ROLE_COUNT = 64;

// 4×4 有序抖动矩阵（Bayer），按行展开，取值 0..15
const int BAYER[16] = int[16](0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5);

// ---- flags 位 ----
const int F_HIGHLIGHT = 1;      // 顶边 1 px 内高光（面板）
const int F_AMBIENT = 2;        // shadow.ambient（只在晶白；常驻件设为"随主题"时）
const int F_FLOAT = 4;          // shadow.float + shadow.float.near（浮层）
const int F_DASHED = 8;         // 边改虚线（禁用、空槽）
const int F_PRIMARY = 16;       // 主按钮：悬停时填色换 primary.hover，深屏另加辉光
const int F_SELECTED_IRIS = 32; // 卡片选中：顶边 1.5 px 色散线
const int F_VIA = 64;           // 切角旁 Ø4 过孔签名（空心）
const int F_VIA_ON = 128;       // 过孔点亮（led.on 实心，深屏带晕）
const int F_GLOW = 256;         // 形状外沿的解析辉光（只在深屏）：过孔、把手、玩家箭头
const int F_CIRCLE = 512;       // 形状是正圆（过孔灯、把手、环）
const int F_CARD_HOVER = 1024;  // 卡片悬停：边换 line.card.hover 并淡入浮层影
const int F_WAVE = 2048;        // kind 1：M7 示波器波形
const int F_ROW_BAR = 4096;     // 列表项悬停的左侧 2 px 指示条
const int F_HOVER_SELECTED = 8192; // 次按钮悬停：底换成 surface.selected（不叠 6% 主色）
const int F_SOLID = 16384;      // 实底按钮（危险按钮）：悬停只把边换成 primary.line，不叠色、不改字色

uniform int kind : hint_enum("Panel", "Line", "Icon", "Text") = 0;
uniform vec4 pal_day[64] : source_color;
uniform vec4 pal_night[64] : source_color;

// 数值令牌（UiMaterials 从 ui.json 的 metrics / motion 写入；默认值与 ui.json 一致）
uniform float corner_minor = 2.0;                     // metrics.shape.cornerMinor
uniform vec4 focus_geo = vec4(3.0, 2.0, 0.7, 0.35);   // offset、tightenFrom、irisPhys、irisMax
uniform vec2 glow_geo = vec2(4.0, 0.30);              // radius、max
uniform vec4 ambient_geo = vec4(0.0, 2.0, 12.0, 0.0); // x、y、blur
uniform vec4 float_geo_day = vec4(0.0, 8.0, 24.0, 0.0);
uniform vec4 float_geo_night = vec4(0.0, 10.0, 30.0, 0.0);
uniform vec4 contact_geo = vec4(0.0, 1.0, 2.0, 0.0);
uniform vec2 dash_geo = vec2(4.0, 3.0);               // metrics.line.dash
uniform vec2 trace_geo = vec2(8.0, 120.0);            // 头部、拖尾（motion.durations.trace）
uniform float front_width = 0.02;                     // themeSwitch.frontWidth
uniform float front_delta = 0.16;                     // 交班前沿的亮线增量（≤ motion.limits.frontDeltaMax）

// ---- 单个控件的效果：instance uniform（UI-VISUAL §11.5；上限 16 个，这里用 12 个） ----
instance uniform int fill_role = -1;     // 底色；-1 不画
instance uniform int line_role = -1;     // 边线；-1 不画
instance uniform int role = 8;           // 内容色（文字、图标、线、波形）；默认 text.1
instance uniform int flags = 0;
instance uniform float chamfer = 0.0;    // 左上切角边长（逻辑像素）
instance uniform float margin = 6.0;     // StyleBox 的 ExpandMargin（逻辑像素）
instance uniform float hover = 0.0;      // 悬停强度 0–1（效果弹簧）
instance uniform float focus = 0.0;      // 0 = 无焦点；(0, 1] = M5 收紧进度（已缓动）
instance uniform float trace = -1.0;     // < 0 无；[0, 1] M1 头部行进（已缓动）；(1, 2] 落定淡出
instance uniform float reveal = 1.0;     // M6：1 = 完全显示；C# 按 step_rate 给出 4 阶
instance uniform float pin_night = 0.0;  // 1 = 常驻件固定深屏
instance uniform vec4 extra = vec4(0.0); // kind 1：线宽、虚线实长、虚线空长、—；F_WAVE：振幅、周期数、相速、线宽
                                         // kind 2：帧数、起播的 anim_time、是否在播、—

varying vec2 v_scale;
varying vec4 v_color;

void vertex() {
	mat4 m = CANVAS_MATRIX * MODEL_MATRIX;
	v_scale = vec2(length(m[0].xy), length(m[1].xy));
	v_color = COLOR;
}

// 交班扫描前沿：0 日态，1 夜态；只在前沿那 2% 宽的一条里处于中间值。
float night_at(vec2 frag, float screen_w) {
	float u = frag.x / screen_w;
	float front = mix(-front_width, 1.0 + front_width, ui_night);
	return clamp((front - u) / front_width + 0.5, 0.0, 1.0);
}

vec4 pal(int i, float n) {
	if (i < 0 || i >= ROLE_COUNT) {
		return vec4(0.0);
	}
	return mix(pal_day[i], pal_night[i], n);
}

// 像素 [x − 0.5, x + 0.5] 与区间 [a, b] 的覆盖率
float band(float x, float a, float b) {
	return clamp(min(x + 0.5, b) - max(x - 0.5, a), 0.0, 1.0);
}

bool has(int f) {
	return (flags & f) != 0;
}

// 左上切角 c，其余三角 k 的矩形；返回 (全形 SDF, 去掉顶边半平面后的 SDF)。内负外正，单位物理像素。
vec2 sd_chip(vec2 p, vec2 size, float c, float k) {
	float s = 0.70710678;
	float sides = max(-p.x, p.x - size.x);
	sides = max(sides, p.y - size.y);
	sides = max(sides, (c - p.x - p.y) * s);
	sides = max(sides, (k - (size.x - p.x) - p.y) * s);
	sides = max(sides, (k - p.x - (size.y - p.y)) * s);
	sides = max(sides, (k - (size.x - p.x) - (size.y - p.y)) * s);
	return vec2(max(sides, -p.y), sides);
}

float bayer4(vec2 cell) {
	ivec2 q = ivec2(mod(cell, 4.0));
	return float(BAYER[q.x + q.y * 4]) / 16.0;
}

// 把直通 alpha 的颜色 c 以覆盖率 cov 叠到预乘的 acc 上面
void put(inout vec4 acc, vec4 c, float cov) {
	float a = clamp(c.a * cov, 0.0, 1.0);
	acc.rgb = c.rgb * a + acc.rgb * (1.0 - a);
	acc.a = a + acc.a * (1.0 - a);
}

// M1 的亮度：along 是该像素沿线的位置，len 是全长（物理像素）
float trace_level(float along, float len, float sc) {
	if (trace < 0.0) {
		return 0.0;
	}
	float land = clamp(trace - 1.0, 0.0, 1.0);
	float head = clamp(trace, 0.0, 1.0) * len;
	float head_len = trace_geo.x * sc;
	float tail_len = trace_geo.y * sc * (1.0 - 0.7 * land);
	float ahead = head - along;
	if (ahead < -0.5) {
		return 0.0;
	}
	float t = clamp((ahead - head_len) / max(tail_len, 1.0), 0.0, 1.0);
	float lvl = ahead <= head_len ? 1.0 : (1.0 - t) * (1.0 - t);
	return lvl * (1.0 - land);
}

float reveal_mask(vec2 frag, vec2 org, float sc) {
	if (reveal >= 1.0) {
		return 1.0;
	}
	float blk = sc > 1.25 ? 3.0 : 2.0;
	vec2 cell = floor((frag - org) / blk);
	return step(bayer4(cell), reveal - 0.0001);
}

vec4 shade_panel(vec2 frag, vec2 uv, float screen_w, vec4 tex_color, bool textured) {
	float n = max(night_at(frag, screen_w), pin_night);
	vec2 sc = max(v_scale, vec2(0.0001));
	float s1 = sc.x;

	vec2 du = abs(dFdx(uv)) + abs(dFdy(uv));
	vec2 box = 1.0 / max(du, vec2(1e-6));
	vec2 org = floor(frag - uv * box + 0.5);
	vec2 m = floor(margin * sc + 0.5);
	vec2 p = frag - org - m;
	vec2 size = floor(box - 2.0 * m + 0.5);

	// 同一控件画出的文字、图标：按 role 改色（悬停时换成 text.accent）
	if (textured) {
		float hc = (has(F_PRIMARY) || has(F_SOLID)) ? 0.0 : hover;
		vec4 c = mix(pal(role, n), pal(R_TEXT_ACCENT, n), hc);
		float a = tex_color.a * c.a;
		return vec4(c.rgb * a, a) * reveal_mask(frag, org, s1);
	}

	float c_tl = floor(chamfer * s1 + 0.5);
	float k = floor(corner_minor * s1 + 0.5);
	vec2 dd;
	if (has(F_CIRCLE)) {
		float r = 0.5 * min(size.x, size.y);
		float dc = length(p - 0.5 * size) - r;
		dd = vec2(dc, dc);
	} else {
		dd = sd_chip(p, size, c_tl, k);
	}
	float d = dd.x;
	vec4 acc = vec4(0.0);

	// 影：晶白环境影只在晶白；浮层影两套各一组参数
	if (has(F_AMBIENT)) {
		vec4 sc_col = pal(R_SHADOW_AMBIENT, n);
		vec2 o = ambient_geo.xy * sc;
		float blur = max(ambient_geo.z * s1, 1.0);
		float ds = sd_chip(p - o, size, c_tl, k).x;
		put(acc, sc_col, (1.0 - smoothstep(-blur, blur, ds)) * (1.0 - n));
	}
	float float_amt = has(F_FLOAT) ? 1.0 : (has(F_CARD_HOVER) ? hover : 0.0);
	if (float_amt > 0.0) {
		vec4 g = mix(float_geo_day, float_geo_night, n);
		vec2 o = g.xy * sc;
		float blur = max(g.z * s1, 1.0);
		float ds = sd_chip(p - o, size, c_tl, k).x;
		put(acc, pal(R_SHADOW_FLOAT, n), (1.0 - smoothstep(-blur, blur, ds)) * float_amt);
		vec2 oc = contact_geo.xy * sc;
		float bc = max(contact_geo.z * s1, 1.0);
		float dsc = sd_chip(p - oc, size, c_tl, k).x;
		put(acc, pal(R_SHADOW_CONTACT, n), (1.0 - smoothstep(-bc, bc, dsc)) * float_amt);
	}

	// 底
	float inside = clamp(0.5 - d, 0.0, 1.0);
	vec4 fill = pal(fill_role, n);
	if (has(F_PRIMARY)) {
		fill = mix(fill, pal(R_PRIMARY_HOVER, n), hover);
	} else if (has(F_HOVER_SELECTED)) {
		vec4 sel = pal(R_SURFACE_SELECTED, n);
		fill = fill_role >= 0 ? mix(fill, sel, hover) : vec4(sel.rgb, sel.a * hover);
	}
	put(acc, fill, inside);
	if (has(F_ROW_BAR)) {
		put(acc, pal(R_SURFACE_ROW_HOVER, n), inside * hover);
	} else if (!has(F_PRIMARY) && !has(F_HOVER_SELECTED) && !has(F_SOLID) && hover > 0.0 && fill_role >= 0) {
		put(acc, pal(R_SURFACE_HOVER, n), inside * hover);
	}
	if (has(F_ROW_BAR)) {
		put(acc, pal(R_PRIMARY_LINE, n), band(p.x, 0.0, floor(2.0 * s1 + 0.5)) * inside * hover);
	}

	// 顶边内高光：顶边往里第二行物理像素
	if (has(F_HIGHLIGHT)) {
		float cov = band(p.y, 1.0, 2.0) * clamp(-dd.y - 0.5, 0.0, 1.0);
		put(acc, pal(R_LINE_HIGHLIGHT, n), cov);
	}

	// 边：最外一圈内侧的 1 物理像素
	vec4 edge = pal(line_role, n);
	if ((line_role >= 0 || has(F_SOLID)) && hover > 0.0 && !has(F_PRIMARY)) {
		edge = mix(edge, pal(has(F_CARD_HOVER) ? R_LINE_CARD_HOVER : R_PRIMARY_LINE, n), hover);
	}
	float edge_cov = band(d, -1.0, 0.0);
	if (has(F_DASHED)) {
		float along = (p.y < 1.5 || p.y > size.y - 1.5) ? p.x : p.y;
		float period = (dash_geo.x + dash_geo.y) * s1;
		edge_cov *= step(mod(along, period), dash_geo.x * s1);
	}
	put(acc, edge, edge_cov);

	// M1 走线流光：从切角沿两条边同时跑到对角
	if (trace >= 0.0 && !has(F_CIRCLE)) {
		float along = max(p.x + p.y - c_tl, 0.0);
		float len = size.x + size.y - c_tl;
		float lvl = trace_level(along, len, s1);
		put(acc, pal(R_TRACE, n), edge_cov * lvl);
		float gd = max(d + 0.5, 0.0);
		float g = lvl * glow_geo.y * n * exp(-gd / (2.0 * s1)) * step(gd, glow_geo.x * s1) * step(-1.5, d);
		acc.rgb += pal(R_GLOW, n).rgb * g;
	}

	// 选中：顶边 1.5 px 色散线（静止）
	if (has(F_SELECTED_IRIS)) {
		float cov = band(p.y, 0.0, 1.5 * s1) * clamp(-dd.y + 0.5, 0.0, 1.0);
		float x = clamp(p.x / max(size.x, 1.0), 0.0, 1.0);
		vec4 core = pal(R_PRIMARY_LINE, n);
		vec4 iris = mix(pal(R_IRIS_A, n), pal(R_IRIS_B, n), x);
		put(acc, mix(core, iris, focus_geo.w), cov);
	}

	// 过孔签名：切角旁 Ø4
	if ((flags & (F_VIA | F_VIA_ON)) != 0) {
		vec2 cv = vec2(chamfer * 0.5 + 3.0) * sc;
		float rv = 2.0 * s1;
		float dv = length(p - cv) - rv;
		if (has(F_VIA_ON)) {
			put(acc, pal(R_LED_ON, n), clamp(0.5 - dv, 0.0, 1.0));
			float halo = exp(-max(dv, 0.0) / s1) * step(0.0, dv) * step(dv, 2.0 * s1);
			put(acc, pal(R_LED_HALO, n), halo * 0.5);
		} else {
			put(acc, pal(R_GRAPHIC, n), band(dv, -1.0, 0.0));
		}
	}

	// M5 衍射焦点边：外侧 3 px 的 1 物理像素实线核，核外衬 1 px 面板底色，两侧各一条冷色色散
	if (focus > 0.0) {
		float o = floor(focus_geo.x * s1 + 0.5) + focus_geo.y * s1 * (1.0 - clamp(focus, 0.0, 1.0));
		float w = focus_geo.z;
		put(acc, pal(R_IRIS_A, n), band(d, o - 1.0 - w, o - 1.0) * focus_geo.w);
		put(acc, pal(R_FOCUS_BACKING, n), band(d, o, o + 1.0));
		put(acc, pal(R_IRIS_B, n), band(d, o + 1.0, o + 1.0 + w) * focus_geo.w);
		float core = band(d, o - 1.0, o);
		put(acc, pal(R_PRIMARY_LINE, n), core);
		float gd = abs(d - (o - 0.5));
		float g = glow_geo.y * n * exp(-gd / (2.0 * s1)) * step(gd, glow_geo.x * s1) * (1.0 - core);
		acc.rgb += pal(R_GLOW, n).rgb * g * 0.6;
	}

	// 解析辉光（只在深屏）：过孔、把手、主按钮悬停
	float glow_amt = has(F_GLOW) ? 1.0 : (has(F_PRIMARY) ? hover : 0.0);
	if (glow_amt > 0.0) {
		float gd = max(d, 0.0);
		float g = glow_amt * glow_geo.y * n * exp(-gd / (2.0 * s1)) * step(gd, glow_geo.x * s1) * step(0.0, d);
		acc.rgb += pal(R_GLOW, n).rgb * g;
	}

	// 交班前沿：扫过时在面板底上留一条 1 物理像素的亮线和 12 px 的淡出带，之后不留痕
	if (pin_night < 0.5 && ui_night > 0.0 && ui_night < 1.0) {
		float front_px = mix(-front_width, 1.0 + front_width, ui_night) * screen_w;
		float behind = front_px - frag.x;
		float line = band(behind, 0.0, 1.0);
		float fade = behind > 0.0 ? exp(-behind / (4.0 * s1)) * 0.5 : 0.0;
		acc.rgb += pal(R_TRACE, n).rgb * (line + fade) * front_delta * inside;
	}

	return acc * reveal_mask(frag, org, s1);
}

vec4 shade_line(vec2 frag, vec2 uv, float screen_w) {
	float n = max(night_at(frag, screen_w), pin_night);
	float s1 = max(v_scale.x, 0.0001);
	vec4 col = mix(pal(role, n), pal(R_TEXT_ACCENT, n), hover);
	vec4 acc = vec4(0.0);

	if (has(F_WAVE)) {
		// M7：单线波形，振幅与频率表示真实数据；相位按 step_rate 推进，暂停时振幅为 0
		vec2 du = abs(dFdx(uv)) + abs(dFdy(uv));
		vec2 box = 1.0 / max(du, vec2(1e-6));
		float amp = clamp(extra.x, 0.0, 0.4);
		float cycles = extra.y;
		float rate = max(step_rate, 1.0);
		float phase = floor(anim_time * rate) / rate * extra.z;
		float a = 6.28318531 * (cycles * uv.x - phase);
		float yc = 0.5 + amp * sin(a);
		float dy = (uv.y - yc) * box.y;
		float slope = amp * box.y * 6.28318531 * cycles / box.x * cos(a);
		float dist = abs(dy) / sqrt(1.0 + slope * slope);
		float w = max(extra.w, 1.0) * s1;
		put(acc, col, band(dist, -0.5 * w, 0.5 * w));
		return acc * v_color.a;
	}

	vec2 gx = vec2(dFdx(uv.x), dFdy(uv.x));
	vec2 gy = vec2(dFdx(uv.y), dFdy(uv.y));
	float len = 1.0 / max(length(gx), 1e-6);
	float wid = 1.0 / max(length(gy), 1e-6);
	float along = uv.x * len;
	float across = (uv.y - 0.5) * wid;
	float w = extra.x <= 1.0 ? 1.0 : extra.x * s1;
	float cov = band(across, -0.5 * w, 0.5 * w);
	if (extra.y > 0.0) {
		float period = (extra.y + extra.z) * s1;
		cov *= step(mod(along, period), extra.y * s1);
	}
	put(acc, col, cov);
	if (trace >= 0.0) {
		float lvl = trace_level(along, len, s1);
		put(acc, pal(R_TRACE, n), cov * lvl);
		float g = lvl * glow_geo.y * n * exp(-abs(across) / (2.0 * s1)) * step(abs(across), glow_geo.x * s1);
		acc.rgb += pal(R_GLOW, n).rgb * g;
	}
	if (has(F_GLOW)) {
		float g = glow_geo.y * n * exp(-max(abs(across) - 0.5 * w, 0.0) / (2.0 * s1)) * step(abs(across), glow_geo.x * s1);
		acc.rgb += pal(R_GLOW, n).rgb * g * (1.0 - cov);
	}
	return acc * v_color.a;
}

void fragment() {
	float screen_w = 1.0 / SCREEN_PIXEL_SIZE.x;
	if (kind == 0) {
		// 无贴图的 StyleBox 绑的是 1×1 的默认白贴图；字形和图标的贴图远大于 1 px
		bool textured = TEXTURE_PIXEL_SIZE.x < 0.25;
		vec4 c = shade_panel(FRAGCOORD.xy, UV, screen_w, COLOR, textured);
		COLOR = c * (textured ? 1.0 : v_color.a);
	} else if (kind == 1) {
		COLOR = shade_line(FRAGCOORD.xy, UV, screen_w);
	} else if (kind == 2) {
		float n = max(night_at(FRAGCOORD.xy, screen_w), pin_night);
		float frames = max(extra.x, 1.0);
		float frame = 0.0;
		if (extra.z > 0.5) {
			float dt = mod(anim_time - extra.y + 480.0, 480.0);
			frame = clamp(floor(dt * max(step_rate, 1.0)), 0.0, frames - 1.0);
		}
		vec2 uv = vec2((UV.x + frame) / frames, UV.y);
		float a = texture(TEXTURE, uv).a * v_color.a;
		vec4 c = mix(pal(role, n), pal(R_TEXT_ACCENT, n), hover);
		a *= c.a;
		vec2 du = abs(dFdx(UV)) + abs(dFdy(UV));
		vec2 org = floor(FRAGCOORD.xy - UV / max(du, vec2(1e-6)) + 0.5);
		a *= reveal_mask(FRAGCOORD.xy, org, max(v_scale.x, 0.0001));
		COLOR = vec4(c.rgb * a, a);
	} else {
		float n = max(night_at(FRAGCOORD.xy, screen_w), pin_night);
		vec4 c = mix(pal(role, n), pal(R_TEXT_ACCENT, n), hover);
		float a = COLOR.a * c.a;
		COLOR = vec4(c.rgb * a, a);
	}
}
