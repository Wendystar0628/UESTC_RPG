// toon.gdshaderinc：全校共用的卡通材质核心（WP-04）
// 规格：ART-STYLE §2（明暗分阶）、§3（影色）、§5（暖边、透光、湿面）；ENG-PERF §1.4、§1.5、§2、§10.1；SYS-WEATHER §5、§7。
//
// 合成方式：ALBEDO = 1，影色写 EMISSION，"受光 − 影"在 light() 的方向光分支里乘分阶遮罩加进 DIFFUSE_LIGHT。
// 颜色只从调色板来，着色器不读 LIGHT_COLOR，不用 TIME。没有接触影，遮蔽只推阈值。
//
// 白名单着色器的写法：
//   shader_type spatial;
//   render_mode cull_back, diffuse_lambert, specular_disabled, ambient_light_disabled;
//   #include "res://style/shaders/toon.gdshaderinc"
//   void vertex()   { TOON_VERTEX }
//   void fragment() {
//       TOON_FRAGMENT_BEGIN                          // 得到 tctx（片元上下文）、tmat（材质表一行）
//       ToonCol tcol = toon_colors(tctx, tmat);      // 调色板取色、天光渐变、地面反光、湿润、倒角提亮、降饱和
//       ...                                          // 本着色器自己的改色（例如玻璃的斜条、眩光、窗灯）
//       TOON_FRAGMENT_END(tcol)                      // 写 ALBEDO、ROUGHNESS（材质 ID）、EMISSION、BACKLIGHT、FOG 和传给 light() 的量
//   }
//   void light()    { TOON_LIGHT }
// 本文件不定义 vertex()、fragment()、light()；varying 只能在 fragment() 本体里赋值，所以写成宏。
// 抖动淡化的 discard 放在 TOON_FRAGMENT_END 开头：之前的取色、雾、湿润、积水里的 fwidth 都在统一控制流里算。

#ifndef CAMPUS_TOON_INCLUDED
#define CAMPUS_TOON_INCLUDED

#include "res://style/shaders/globals.gdshaderinc"

// 调色板取色（pal_fetch、pal_cur、pal_season、PAL_*）和共用雾函数都在 fog.gdshaderinc，天空着色器也要用
#include "res://style/shaders/fog.gdshaderinc"

// 灯光通道、窗灯与路灯光斑的公共求值（lamp_channel、lamp_window_*、lamp_pool；WP-39），fx_glow、降水与倒影代理共用同一份
#include "res://style/shaders/lamp.gdshaderinc"

// OKLab 换算（ok_from_linear、ok_to_linear）在 fog.gdshaderinc 里，天空也要用

// 明度乘 kl、彩度乘 kc（OKLab 的 a、b 同乘），再加 dl
vec3 ok_adjust(vec3 c, float kl, float kc, float dl) {
	vec3 o = ok_from_linear(c);
	o.x = o.x * kl + dl;
	o.yz *= kc;
	return ok_to_linear(o);
}

// ---------------------------------------------------------------------------
// 顶点数据（ENG-PERF §1.5）：COLOR RGBA8 = 色号、AO、标志、备用字节
// 标志字节的位（WP-04 登记，暂定）：
// ---------------------------------------------------------------------------
const int TOON_F_INTERIOR = 1;   // 室内：影取行 2、受光取行 3 并乘光斑增益
const int TOON_F_OUTSKIRT = 2;   // 外围：窗跳过 window_override；季节覆盖潜势读顶点而不读全图纹理
const int TOON_F_DECAL = 4;      // 贴面贴花：备用字节是底面色号；不投影
const int TOON_F_NOSHADOW = 8;   // 小件不投影：阴影通道里塌成一点（ART-MODELING §7.4）

// 明暗分阶（ART-STYLE §2.1）
const int TOON_STEPS_TWO = 0;        // 建筑、地面、硬质大件
const int TOON_STEPS_TWO_HALF = 1;   // 曲面道具、金属件
const int TOON_STEPS_SOFT = 2;       // 软物（两阶半，带宽 ×2.2）
const int TOON_STEPS_THREE = 3;      // 植被团块
const float TOON_THR = -0.03;        // 影 / 受光的阈值（受光多吃一点）
const float TOON_THR_HALF = 0.30;    // 两阶半的半受光阈值
const float TOON_THR_DEEP = -0.25;   // 植被深影阈值
const float TOON_BAND_OVERCAST = 0.06;   // 阴雨天按天光方向时的过渡带
const float TOON_AO_PUSH = 0.3;      // 遮蔽推阈值的上限
const float TOON_GRAZE = 0.06;       // 掠射面判进影里：受光再乘 smoothstep(0, 0.06, N·L)（ENG-PERF §10.1）
const vec2 TOON_SHADOW_THR = vec2(0.4, 0.6);     // 阴影图衰减的阈值化：窄而干脆；影边的颗粒来自 PCSS 采样噪声，不靠放宽阈值掩盖
const float TOON_TRANS_K = 0.25;     // 透光整体系数（ART-STYLE §5.6，P0 看图定）
const float TOON_WET_DARKEN = 0.28;  // 湿润变暗：受光色和影色 × (1 − 0.28·wet)
const float TOON_SKY_GRAD = 0.06;    // 影里随法线 y 的天光渐变（约 ±0.03 L）
const float TOON_BOUNCE = 0.15;      // 朝下面的地面暖反光

// 材质 uniform：材质库按 ToonSpec 分配的 7 位材质 ID（0 = 未分配）
uniform int mat_id : hint_range(0, 127) = 0;

// instance uniform（ENG-PERF §1.5 登记）
instance uniform float fade_t0 = 0.0;        // 抖动淡化起点（fade_clock 计，正负号区分淡入淡出，0 = 不在淡化中）
instance uniform float retreat_bias = 0.0;   // 退光线偏置（只有宣邦楼穹顶设 6 m）
instance uniform float fog_exempt = 0.0;     // 地标雾豁免系数 0–1（只有八角穹顶与宣邦楼钟楼设值）

// vertex → fragment / light
varying flat int v_cid;
varying flat int v_flags;
varying float v_ao;
varying float v_spare;
varying float v_wy;
varying flat vec3 v_center;
varying flat vec3 v_axis;
// fragment → light
varying vec3 v_diff;       // 受光 − 影
varying vec4 v_band;       // x 过渡带半宽，y 半受光档，z 遮蔽推阈值，w 受光增益（室内光斑增益）
varying vec3 v_rim;        // 受光侧暖边色 × k_rim
varying vec3 v_lampdiff;   // 灯照色 − 影（点光分支）
varying float v_cloud;     // 地面云影乘子

float toon_id_rough(int id) {
	return float(clamp(id, 0, 127)) / 127.0;
}

float toon_overcast() {
	return clamp(weather_a.y, 0.0, 1.0);
}

// ---------------------------------------------------------------------------
// 材质表 mat_table（128×4 RGBA8，最近邻；行 0 线表归 WP-05 的布局）
//   行 1：R 明暗档；G half_level×255；B 过渡带（两阶、三阶存固定半宽 ×1000，两阶半、软物存 fwidth 倍数 ×50）；A 湿润响应×255
//   行 2：R 透光 k_t×255；G 受光侧暖边 k_rim×255；B 闪点系数×255；A 保留
//   行 3：R 解析法线（0 不用、1 球、2 柱）；G 倒角提亮的受光幅度、B 影里幅度（值/255×0.25 L，0 = 不提亮）；A 保留
// ---------------------------------------------------------------------------
struct ToonMat {
	int steps;
	float half_level;
	float band;
	float wet;
	float k_t;
	float k_rim;
	float k_spark;
	int nmode;
	float bevel_lit;
	float bevel_shade;
};

ToonMat toon_mat(int id) {
	vec4 r1 = texelFetch(mat_table, ivec2(id, 1), 0);
	vec4 r2 = texelFetch(mat_table, ivec2(id, 2), 0);
	vec4 r3 = texelFetch(mat_table, ivec2(id, 3), 0);
	int steps = int(round(r1.r * 255.0));
	bool fw_band = steps == TOON_STEPS_TWO_HALF || steps == TOON_STEPS_SOFT;
	float band = fw_band ? r1.b * 255.0 / 50.0 : r1.b * 255.0 / 1000.0;
	return ToonMat(steps, r1.g, band, r1.a, r2.r, r2.g, r2.b,
			int(round(r3.r * 255.0)), r3.g * 0.25, r3.b * 0.25);
}

// ---------------------------------------------------------------------------
// 片元上下文
// ---------------------------------------------------------------------------
struct ToonCtx {
	vec3 wpos;     // 世界坐标
	vec3 cam;      // 相机世界坐标
	vec3 n_w;      // 世界法线
	float dist;    // 到相机的距离
	float d;       // N·L（片元里，含阴雨天的天光方向）
	float fw;      // fwidth(N·L)，两阶半的过渡带用
	float kappa;   // 法线曲率（1/m），判倒角带
	int cid;       // 色号
	int flags;     // 标志字节
	float ao;      // 顶点 AO 字节：1 = 不遮蔽（GeometryBundle 缺省值），0 = 窄凹处最深；只推阈值
	float spare;   // 备用字节
	float pool_aa; // 路灯光斑边缘的过渡宽度（m）：max(fwidth(|p.xz|), 0.02)，在宏里统一控制流中算好
};

ToonCtx toon_ctx(vec3 vtx, vec3 n_v, mat4 inv_view, float d, float fw, float kappa, int cid, int flags, float ao, float spare,
		float pool_aa) {
	vec3 wpos = (inv_view * vec4(vtx, 1.0)).xyz;
	vec3 cam = inv_view[3].xyz;
	vec3 n_w = normalize(mat3(inv_view) * n_v);
	return ToonCtx(wpos, cam, n_w, length(wpos - cam), d, fw, kappa, cid, flags, ao, spare, pool_aa);
}

// 解析法线（ART-MODELING §7.6、ENG-PERF §2）：球取 P − C，柱减去轴向分量；都在视空间里算
vec3 toon_analytic_normal(vec3 vtx, vec3 n_mesh, vec3 c, vec3 axis, int mode) {
	vec3 d = vtx - c;
	vec3 n = n_mesh;
	if (mode == 1) {
		n = d;
	} else if (mode == 2) {
		n = d - dot(d, axis) * axis;
	}
	float l = length(n);
	return l > 1e-5 ? n / l : n_mesh;
}

// 4×4 Bayer 抖动淡化（ENG-PERF §2）：进度 = (fade_clock − |fade_t0|) / 0.25 s
const float TOON_BAYER[16] = float[](
		0.0, 8.0, 2.0, 10.0,
		12.0, 4.0, 14.0, 6.0,
		3.0, 11.0, 1.0, 9.0,
		15.0, 7.0, 13.0, 5.0);

bool toon_fade_discard(vec2 frag_xy) {
	if (fade_t0 == 0.0) {
		return false;
	}
	float p = clamp(mod(fade_clock - abs(fade_t0), 480.0) / 0.25, 0.0, 1.0);
	int ix = int(mod(frag_xy.x, 4.0));
	int iy = int(mod(frag_xy.y, 4.0));
	float b = (TOON_BAYER[iy * 4 + ix] + 0.5) / 16.0;
	return fade_t0 > 0.0 ? (b >= p) : (b < p);
}

// ---------------------------------------------------------------------------
// 天气挂点
// ---------------------------------------------------------------------------
// 湿润的暴露度（SYS-WEATHER §7）：公共函数归 WP-45b，要读地面遮罩 2 的建筑遮蔽和树冠遮蔽。
// 主线版只分室内（0）与室外（1）。
float toon_wet_exposure(ToonCtx c) {
	return (c.flags & TOON_F_INTERIOR) != 0 ? 0.0 : 1.0;
}

// wet = 湿润 × 暴露 × 材质响应（材质响应在 mat_table 行 1 的 A）
float toon_wet(ToonCtx c, ToonMat m) {
	return clamp(weather_b.x, 0.0, 1.0) * toon_wet_exposure(c) * m.wet;
}

// 积水（SYS-WEATHER §7，公共函数归 WP-45b）：在湿润变暗之后，按积水遮罩把受光色、影色换成积水色；ROUGHNESS 不动。
// precip_wet.gdshaderinc 进白名单前是空实现。
void toon_puddle(ToonCtx c, ToonMat m, inout vec3 lit, inout vec3 shade) {
}

// 地面云影（SYS-WEATHER §7，云噪声归 WP-40）：主线版没有云影，恒为 1。
float toon_cloud_shadow(vec3 wpos) {
	return 1.0;
}

// ---------------------------------------------------------------------------
// 窗灯（ENG-PERF §10.3）：亮灭只由窗号哈希、灯光通道和窗覆盖决定，与怎样到达这个钟点无关。
// 哈希、阈值、亮灭与冷暖用 lamp.gdshaderinc 的 lamp_hash01、lamp_window_*（与 fx_glow、C# 的 ScheduleMath 同一份）。
// window_override（RGBA8，纹素 = (id % 256, id / 256)，归 WP-39）：R 亮灭（0 不覆盖、128 强制灭、255 强制亮），
// G 冷暖（0 按哈希、128 强制暖、255 强制冷），B 窗帘（0 按哈希、128 无帘、255 拉上），A 保留。
// ---------------------------------------------------------------------------

// 外围的窗不读 window_override（标志位 TOON_F_OUTSKIRT），一律按哈希
vec4 toon_window_override(int id, bool use_override) {
	return use_override ? lamp_window_override(id) : vec4(0.0);
}

// 窗帘：B 通道 0 = 按哈希（返回 0）、128 = 无帘（1）、255 = 拉上（2）
int toon_window_curtain(vec4 o) {
	return o.b >= 0.75 ? 2 : (o.b >= 0.25 ? 1 : 0);
}

// ---------------------------------------------------------------------------
// 路灯光斑（SYS-TIME §6、ENG-PERF §10.3；公共求值 lamp_pool 归 WP-39）：
// 地面和 4 m 以下立面上的平涂光斑，影色与受光色都向灯照色（调色板行 1）靠，按该盏灯的色号相对 lamp.warm 着色。
// 室内不画（室内灯另走室内光）。边缘过渡宽度 c.pool_aa 在 TOON_FRAGMENT_BEGIN 里按统一控制流算好。
// ---------------------------------------------------------------------------
void toon_lamp_pool(ToonCtx c, vec3 lamp, inout vec3 lit, inout vec3 shade) {
	if ((c.flags & TOON_F_INTERIOR) != 0 || lamp_heff() >= LAMP_POOL_DAY_DEG.y) {
		return;
	}
	vec2 pool = lamp_pool(c.wpos, c.pool_aa);
	if (pool.y < 0.0 || pool.x <= 0.0) {
		return;
	}
	vec3 tint = pal_cur(int(pool.y)) / max(pal_cur(PAL_LAMP_WARM), vec3(0.02));
	vec3 lc = lamp * tint;
	shade = mix(shade, lc, pool.x);
	lit = mix(lit, max(lit, lc), pool.x);
}

// ---------------------------------------------------------------------------
// 取色与片元里的明暗量
// ---------------------------------------------------------------------------
struct ToonCol {
	vec3 lit;          // 受光色（线性）
	vec3 shade;        // 影色（线性），写进 EMISSION
	vec3 lamp;         // 灯照色（行 1）
	vec3 trans;        // 透光色 × k_t，写进 BACKLIGHT
	vec3 rim;          // 受光侧暖边色 × k_rim
	vec3 extra;        // 不参与明暗的自发光（窗灯等）
	float deep_mix;    // 植被深影的权重
	float bw;          // 过渡带半宽
	float half_level;  // 半受光档（两阶为 1）
	float push;        // 遮蔽推阈值
	float gain;        // 受光增益（室内光斑增益）
};

ToonCol toon_colors(ToonCtx c, ToonMat m) {
	bool interior = (c.flags & TOON_F_INTERIOR) != 0;
	vec3 lit;
	vec3 shade;
	float gain = 1.0;
	if (interior) {
		shade = pal_fetch(c.cid, PAL_ROW_IN_SHADE);
		lit = pal_fetch(c.cid, PAL_ROW_IN_LIT);
		gain = interior_light.x;
	} else {
		lit = pal_fetch(c.cid, PAL_ROW_CUR);
		shade = pal_fetch(c.cid + PAL_SHADE_OFFSET, PAL_ROW_CUR);
	}
	vec3 lamp = pal_fetch(c.cid, PAL_ROW_LAMP);

	// 影里的两条连续项（ART-STYLE §3.3）：天光渐变；朝下面的地面暖反光（室内和夜间为 0）
	shade *= 1.0 + TOON_SKY_GRAD * c.n_w.y;
	float day = smoothstep(-0.02, 0.05, sun_dir.y);
	float bounce = TOON_BOUNCE * clamp(-c.n_w.y, 0.0, 1.0) * key_strength * day * (interior ? 0.0 : 1.0);
	shade = mix(shade, shade * pal_cur(PAL_BOUNCE_GROUND) * 1.6, bounce);

	// 湿润变暗（ROUGHNESS 不动）
	float wet = toon_wet(c, m);
	float wk = 1.0 - TOON_WET_DARKEN * wet;
	lit *= wk;
	shade *= wk;
	lamp *= wk;
	toon_puddle(c, m, lit, shade);

	// 路灯光斑：在湿润与积水之后，所以湿地面上的光斑也跟着暗、积水里也亮
	toon_lamp_pool(c, lamp, lit, shade);

	// 倒角带提亮（ART-STYLE §2.6）：按法线曲率判倒角带，只改明暗，不写 NORMAL
	float bevel = smoothstep(2.0, 4.0, c.kappa) * step(1e-4, m.bevel_lit);
	if (bevel > 0.0) {
		lit = ok_adjust(lit, 1.0, 1.0, m.bevel_lit * bevel);
		shade = ok_adjust(shade, 1.0, 1.0, m.bevel_shade * bevel);
	}

	// 200 m 起降饱和（雾函数的一段，作用在受光色和影色上）
	float fd = fog_equiv_dist(c.wpos, c.cam);
	lit = fog_desaturate(lit, fd);
	shade = fog_desaturate(shade, fd);
	lamp = fog_desaturate(lamp, fd);

	// 明暗档
	float bw = m.band;
	float half_level = 1.0;
	if (m.steps == TOON_STEPS_TWO_HALF || m.steps == TOON_STEPS_SOFT) {
		bw = c.fw * m.band;
		half_level = m.half_level;
	}
	bw = max(bw, 1e-3);
	float push = TOON_AO_PUSH * (1.0 - clamp(c.ao, 0.0, 1.0));
	float deep_mix = 0.0;
	if (m.steps == TOON_STEPS_THREE) {
		float bwd = mix(bw, max(bw, TOON_BAND_OVERCAST), toon_overcast());
		deep_mix = 1.0 - smoothstep(TOON_THR_DEEP - bwd, TOON_THR_DEEP + bwd, c.d - push);
	}

	// 透光色：受光色 L +0.08、C ×1.2；阴度 > 0.7 时为 0
	vec3 trans = vec3(0.0);
	if (m.k_t > 0.0) {
		trans = ok_adjust(lit, 1.0, 1.2, 0.08) * m.k_t * (1.0 - smoothstep(0.6, 0.8, toon_overcast()));
	}

	// 受光侧暖边：rim × 受光色 × k_rim
	vec3 rim = pal_cur(PAL_RIM) * lit * m.k_rim;

	return ToonCol(lit, shade, lamp, trans, rim, vec3(0.0), deep_mix, bw, half_level, push, gain);
}

// EMISSION：影（植被深影按 L ×0.88、C ×0.95 由影色算出）+ 闪光帧 + 额外自发光
vec3 toon_emission(ToonCol t, ToonCtx c) {
	vec3 e = t.shade;
	if (t.deep_mix > 0.0) {
		e = mix(e, ok_adjust(t.shade, 0.88, 0.95, 0.0), t.deep_mix);
	}
	// 闪光帧（SYS-WEATHER §5）：全场一瞬间翻到受光色，没有投影
	e += clamp(lightning.x, 0.0, 1.0) * (t.lit - t.shade) * (0.5 + 0.5 * c.n_w.y);
	return e + t.extra;
}

// ---------------------------------------------------------------------------
// light()：方向光（主光）与点光（重点灯）
// ---------------------------------------------------------------------------
vec3 toon_key_light(vec3 n, vec3 l, vec3 v, mat3 view3, float att, vec3 backlight,
		vec3 diff, vec4 band, vec3 rimcol, float wy, float cloud_k) {
	// 阴、雨、雪时按天光方向判明暗（SYS-WEATHER §7），带宽放到 0.06；主光方向本身不变
	float g = toon_overcast();
	vec3 ld = normalize(mix(l, normalize(view3 * overcast_dir), g));
	float bw = mix(band.x, max(band.x, TOON_BAND_OVERCAST), g);
	float d = dot(n, ld) - band.z;
	float m0 = smoothstep(TOON_THR - bw, TOON_THR + bw, d);
	float mh = mix(band.y, 1.0, smoothstep(TOON_THR_HALF - bw, TOON_THR_HALF + bw, d));

	// 阴影：阈值化后按 shadow_strength 混（月影 0.45、阴天 ≥ 0.2，不用 ShadowOpacity）
	float sh_map = smoothstep(TOON_SHADOW_THR.x, TOON_SHADOW_THR.y, att);
	float graze = smoothstep(0.0, TOON_GRAZE, dot(n, l));
	float sh_key = mix(1.0, sh_map * graze, shadow_strength);
	float sh_raw = mix(1.0, sh_map, shadow_strength);

	// 主光强度 × 退光线 × 地面云影
	float k = key_strength * smoothstep(retreat_y - 1.5, retreat_y + 1.5, wy) * cloud_k;

	vec3 res = diff * (m0 * mh * sh_key * k * band.w);

	// 透光（ART-STYLE §5.6）：被别的物体挡住时不透光
	float fwd = pow(clamp(dot(-v, l), 0.0, 1.0), 3.0);
	float back = smoothstep(0.15, -0.35, d);
	res += backlight * (0.2 + 0.8 * fwd) * back * sh_raw * k * TOON_TRANS_K;

	// 受光侧剪影暖边（ART-STYLE §5.3）：乘过阈值化阴影，影里不出亮边
	float nv = clamp(dot(n, v), 0.0, 1.0);
	float rm = smoothstep(0.32, 0.55, pow(1.0 - nv, 3.0)) * clamp(dot(n, l) + 0.2, 0.0, 1.0);
	res += rimcol * rm * sh_key * k;
	return res;
}

// 点光只给重点灯（不超过 32 盏，ENG-PERF §10.3）：灯照色替换影色，仍是两阶
vec3 toon_point_light(vec3 n, vec3 l, float att, vec3 lampdiff, float push) {
	float a = att * clamp(dot(n, l) - push, 0.0, 1.0);
	return lampdiff * smoothstep(0.02, 0.08, a);
}

// ---------------------------------------------------------------------------
// 宏：varying 只能在 vertex()、fragment() 本体里赋值
// ---------------------------------------------------------------------------
#define TOON_VERTEX \
	v_cid = int(round(COLOR.r * 255.0)); \
	v_flags = int(round(COLOR.b * 255.0)); \
	v_ao = COLOR.g; \
	v_spare = COLOR.a; \
	v_wy = (MODEL_MATRIX * vec4(VERTEX, 1.0)).y + retreat_bias; \
	v_center = (MODELVIEW_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz; \
	v_axis = normalize(mat3(MODELVIEW_MATRIX) * vec3(0.0, 1.0, 0.0)); \
	if (IN_SHADOW_PASS && (v_flags & (TOON_F_NOSHADOW | TOON_F_DECAL)) != 0) { \
		VERTEX = vec3(0.0); \
	}

#define TOON_FRAGMENT_BEGIN \
	float tn_kappa = length(fwidth(NORMAL)) / max(length(fwidth(VERTEX)), 1e-5); \
	ToonMat tmat = toon_mat(mat_id); \
	if (tmat.nmode != 0) { \
		NORMAL = toon_analytic_normal(VERTEX, NORMAL, v_center, v_axis, tmat.nmode); \
	} \
	vec3 tn_ldir = normalize(mix(normalize(mat3(VIEW_MATRIX) * key_dir), normalize(mat3(VIEW_MATRIX) * overcast_dir), toon_overcast())); \
	float tn_d = dot(NORMAL, tn_ldir); \
	float tn_fw = max(fwidth(tn_d), 1e-3); \
	float tn_paa = max(fwidth(length((INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xz)), 0.02); \
	ToonCtx tctx = toon_ctx(VERTEX, NORMAL, INV_VIEW_MATRIX, tn_d, tn_fw, tn_kappa, v_cid, v_flags, v_ao, v_spare, tn_paa);

#define TOON_FRAGMENT_END(T) \
	if (toon_fade_discard(FRAGCOORD.xy)) { \
		discard; \
	} \
	ALBEDO = vec3(1.0); \
	METALLIC = 0.0; \
	SPECULAR = 0.0; \
	ROUGHNESS = toon_id_rough(mat_id); \
	EMISSION = toon_emission(T, tctx); \
	BACKLIGHT = T.trans; \
	FOG = fog_eval_biased(tctx.wpos, tctx.cam, fog_exempt); \
	v_diff = T.lit - T.shade; \
	v_band = vec4(T.bw, T.half_level, T.push, T.gain); \
	v_rim = T.rim; \
	v_lampdiff = T.lamp - T.shade; \
	v_cloud = toon_cloud_shadow(tctx.wpos);

#define TOON_LIGHT \
	if (LIGHT_IS_DIRECTIONAL) { \
		DIFFUSE_LIGHT += toon_key_light(NORMAL, LIGHT, VIEW, mat3(VIEW_MATRIX), ATTENUATION, BACKLIGHT, v_diff, v_band, v_rim, v_wy, v_cloud); \
	} else { \
		DIFFUSE_LIGHT += toon_point_light(NORMAL, LIGHT, ATTENUATION, v_lampdiff, v_band.z); \
	}

#endif
