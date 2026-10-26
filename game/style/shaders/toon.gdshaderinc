// toon.gdshaderinc：全校共用的卡通材质核心（ART-STYLE）
// 规格：ART-STYLE（明暗分阶）、§3（影色）、§5（暖边、透光、湿面）；ENG-PERF；SYS-WEATHER。
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
//       ToonCol tcol = toon_colors(tctx, tmat);      // 调色板取色、天光渐变、地面反光、湿润、倒角提亮、自发光、降饱和
//       // 颜色不按色号取第 0 行时（例如植被按季相行取样），自己取好受光色、影色、灯照色再调
//       // ToonCol tcol = toon_colors_from(tctx, tmat, lit, shade, lamp);
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
#include "res://style/shaders/precip_wet.gdshaderinc"

// 灯光通道、窗灯、灯具亮灭与路灯光斑的公共求值（lamp_channel、lamp_window_*、lamp_fixture_level、lamp_pool_surface、lamp_pool_shade；MAP-QSH-ATMOSPHERE），fx_glow、降水与倒影代理共用同一份
#include "res://style/shaders/lamp.gdshaderinc"

#include "res://style/shaders/toon_look.gdshaderinc"

// 材质 uniform：材质库按 ToonSpec 分配的 7 位材质 ID（0 = 未分配）
uniform int mat_id : hint_range(0, 127) = 0;

// instance uniform（ENG-PERF 登记）
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
varying vec4 v_spec_cloud; // xyz 硬卡通高光色 × 遮罩（light() 里再乘阈值化阴影与主光强度）；w 地面云影乘子。合用一个槽：植被着色器已用满 32 个 varying

float toon_id_rough(int id) {
	return float(clamp(id, 0, MAT_TABLE_WIDTH - 1)) / float(MAT_TABLE_WIDTH - 1);
}

float toon_overcast() {
	return clamp(weather_a.y, 0.0, 1.0);
}

#include "res://style/shaders/toon_surface.gdshaderinc"

// ---------------------------------------------------------------------------
// light()：方向光（主光）与点光（重点灯）
// ---------------------------------------------------------------------------
vec3 toon_key_light(vec3 n, vec3 l, vec3 v, mat3 view3, float att, vec3 backlight,
		vec3 diff, vec4 band, vec3 rimcol, vec3 spec, float wy, float cloud_k) {
	// 阴、雨、雪时按天光方向判明暗（SYS-WEATHER），带宽放到 0.06；主光方向本身不变
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

	// 透光（ART-STYLE）：被别的物体挡住时不透光
	float fwd = pow(clamp(dot(-v, l), 0.0, 1.0), 3.0);
	float back = smoothstep(0.15, -0.35, d);
	res += backlight * (0.2 + 0.8 * fwd) * back * sh_raw * k * TOON_TRANS_K;

	// 受光侧剪影暖边（ART-STYLE）：乘过阈值化阴影，影里不出亮边
	float nv = clamp(dot(n, v), 0.0, 1.0);
	float rm = smoothstep(0.32, 0.55, pow(1.0 - nv, 3.0)) * clamp(dot(n, l) + 0.2, 0.0, 1.0);
	res += rimcol * rm * sh_key * k;

	// 硬卡通高光（max-110 110a）：遮罩在 fragment() 里算好，这里只乘阈值化阴影与主光强度
	res += spec * sh_key * k;
	return res;
}

// 点光只给重点灯（不超过 32 盏，ENG-PERF）：灯照色替换影色，仍是两阶
vec3 toon_point_light(vec3 n, vec3 l, float att, vec3 lampdiff, float push) {
	float a = att * clamp(dot(n, l) - push, 0.0, 1.0);
	return lampdiff * smoothstep(0.02, 0.08, a);
}

// ---------------------------------------------------------------------------
// 宏：varying 只能在 vertex()、fragment() 本体里赋值
// ---------------------------------------------------------------------------
#define TOON_VERTEX \
	v_flags = int(round(COLOR.b * 255.0)); \
	v_cid = int(round(COLOR.r * 255.0)) + ((v_flags & TOON_F_COLOR_BANK) != 0 ? PAL_WIDTH : 0); \
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
	float tn_sp = dot(NORMAL, normalize(normalize(mat3(VIEW_MATRIX) * key_dir) + VIEW)); \
	float tn_spw = max(fwidth(tn_sp), 1e-3); \
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
	v_spec_cloud = vec4(toon_spec(tctx, tmat, tn_sp, tn_spw), toon_cloud_shadow(tctx.wpos));

#define TOON_LIGHT \
	if (LIGHT_IS_DIRECTIONAL) { \
		DIFFUSE_LIGHT += toon_key_light(NORMAL, LIGHT, VIEW, mat3(VIEW_MATRIX), ATTENUATION, BACKLIGHT, v_diff, v_band, v_rim, v_spec_cloud.xyz, v_wy, v_spec_cloud.w); \
	} else { \
		DIFFUSE_LIGHT += toon_point_light(NORMAL, LIGHT, ATTENUATION, v_lampdiff, v_band.z); \
	}

#endif
