// veg_grass.gdshader：近处卡片草丛（WP-23）
// 规格：WLD-VEGETATION §3.5（D-205）；ENG-PERF（草不投影，第 6 层）。
// 草丛是一簇有弯度的尖叶（每叶两节加叶尖，GrassClumps），材质就是草坪自己的（grass.* 一对色、草坪的湿润响应），色号按脚下草坪的
// 色调（INSTANCE_CUSTOM.y，GrassField 按 WP-15 的草坪噪声与边带取），法线一律朝上：叶片与脚下的地面同一个明暗、同一片树影楼影，
// 只靠叶尖提亮、叶根压暗读出叶形。远处退成草坪本身的草色场：叶尖提亮与叶根压暗先淡到 0，叶片再按序号变稀，最后整簇沉进草坪面，
// 所以近远交界没有色差的圈，也没有在远处闪的细线（叶宽至少保持约一个像素）。盖雪处叶片缩到 0。
// 画面设置：近处半径读全局 quality.z（草地范围，m），草密度读 quality.y：按每簇种子留下这个比例的簇，均匀分布，不整片消失。
// 顶点数据：UV = (左 −1 / 中 0 / 右 +1, 叶高比例 t)，CUSTOM1 = (叶根, 叶序 0–1)，CUSTOM2 = (顶点离叶中线的偏移, 叶相位)。
// INSTANCE_CUSTOM = (种子, 草坪色调色号, 风向位置, 0)。
shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_disabled, diffuse_lambert, specular_disabled, ambient_light_disabled;

#include "res://style/shaders/toon.gdshaderinc"

const vec2 GRASS_TONE_FADE = vec2(0.3, 0.7);   // × 半径：叶尖提亮、叶根压暗从满淡到 0
const vec2 GRASS_THIN = vec2(0.25, 0.8);   // × 半径：叶片从全留稀到 GRASS_KEEP_FAR
const float GRASS_DENSITY_EDGE = 0.05;     // 草密度的留簇判据在这个宽度内收到 0，避免边缘簇抖动，密度 1 时全留
const float GRASS_KEEP_FAR = 0.35;         // 远处留下的叶片比例（按叶序与每簇种子）
const vec2 GRASS_SINK = vec2(0.7, 1.0);    // × 半径：整簇沉进草坪面
const float GRASS_SINK_DEPTH = 0.04;       // 沉到草坪面以下的深度（m），不与地面争深度
const float GRASS_MIN_PX = 1.1;            // 叶宽在屏幕上至少这么多像素，远处不碎成闪烁的细线
const float GRASS_WIDEN_MAX = 3.0;         // 叶宽最多放宽到原来的这么多倍
const float GRASS_TIP_DL = 0.07;           // 叶尖提亮（OKLab L，按 t^1.5）
const float GRASS_ROOT_DL = -0.04;         // 叶根压暗（OKLab L，按 (1 − t)²）
const float GRASS_CLUMP_DL = 0.018;        // 每簇的明度起伏（±L）
const float GRASS_BREEZE = 0.16;           // 无风时也有的轻风：叶尖摆幅，按叶高的比例
const float GRASS_WIND_GAIN = 0.35;        // 风的幅度系数 wind.z 每单位再加的摆幅（按叶高）
const float GRASS_WIND_SPEED = 3.5;        // 风浪沿风向传播的速度（m/s）
const vec2 GRASS_SNOW = vec2(0.2, 0.6);    // 积雪量从前者到后者，叶片缩到 0

varying float v_t;
varying flat float v_k;
varying flat float v_dl;

void vertex() {
	vec3 root = CUSTOM1.xyz;
	float rank = CUSTOM1.w;
	vec3 side = CUSTOM2.xyz;
	float phase = CUSTOM2.w;
	float t = UV.y;
	float seed = INSTANCE_CUSTOM.x;
	vec3 origin = MODEL_MATRIX[3].xyz;
	float dist = length(MAIN_CAM_INV_VIEW_MATRIX[3].xyz - origin);
	float r = max(quality.z, 1.0);
	// 草密度：每簇一个稳定的 0–1 值，低于密度的簇留下
	float clump_keep = clamp((quality.y * (1.0 + GRASS_DENSITY_EDGE) - fract(seed * 29.37)) / GRASS_DENSITY_EDGE, 0.0, 1.0);
	// 远处变稀：每簇按种子错开留哪几片
	float keep = mix(1.0, GRASS_KEEP_FAR, smoothstep(GRASS_THIN.x * r, GRASS_THIN.y * r, dist));
	float order = fract(rank + seed * 3.17);
	float alive = 1.0 - smoothstep(keep - 0.06, keep, order);
	float sink = 1.0 - smoothstep(GRASS_SINK.x * r, GRASS_SINK.y * r, dist);
	float grow = alive * sink * clump_keep * (1.0 - smoothstep(GRASS_SNOW.x, GRASS_SNOW.y, snow));
	// 轻风：叶尖顺风摆，相位沿风向传开，读成草面上走过的风
	vec3 center = VERTEX - side;
	mat3 inv3 = inverse(mat3(MODEL_MATRIX));
	vec3 dir_m = normalize(inv3 * vec3(wind.x, 0.0, wind.y) + vec3(1e-5, 0.0, 0.0));
	float w = anim_time * 1.9 - dot(origin.xz, wind.xy) / GRASS_WIND_SPEED + phase * 1.3;
	float gust = 0.55 + 0.45 * sin(anim_time * 0.41 - dot(origin.xz, wind.xy) * 0.06 + seed * 6.2832);
	float amp = (GRASS_BREEZE + GRASS_WIND_GAIN * max(wind.z, 0.0)) * gust;
	float up = center.y - root.y;
	center += dir_m * (amp * up * t * (0.6 + 0.4 * sin(w)));
	// 变稀、沉下、盖雪：叶中线向叶根收，整簇往下沉
	center = root + (center - root) * grow;
	center.y -= GRASS_SINK_DEPTH * (1.0 - sink);
	// 远处叶宽至少一个像素左右
	float px_m = dist * 2.0 / (abs(PROJECTION_MATRIX[1][1]) * VIEWPORT_SIZE.y);
	float half_w = length(mat3(MODEL_MATRIX) * side);
	float widen = half_w > 1e-5 ? clamp(GRASS_MIN_PX * 0.5 * px_m / half_w, 1.0, GRASS_WIDEN_MAX) : 1.0;
	VERTEX = center + side * widen * (grow > 0.0 ? 1.0 : 0.0);
	NORMAL = vec3(0.0, 1.0, 0.0);
	TOON_VERTEX
	// 色号取脚下草坪的色调（GrassField），不用原型顶点里的
	v_cid = int(round(INSTANCE_CUSTOM.y));
	v_t = t;
	v_k = 1.0 - smoothstep(GRASS_TONE_FADE.x * r, GRASS_TONE_FADE.y * r, dist);
	v_dl = (fract(seed * 13.71) - 0.5) * 2.0 * GRASS_CLUMP_DL;
}

void fragment() {
	// 法线朝上（不分正反面）：与草坪面同一个明暗
	NORMAL = normalize(mat3(VIEW_MATRIX) * vec3(0.0, 1.0, 0.0));
	TOON_FRAGMENT_BEGIN
	float dl = (GRASS_TIP_DL * pow(v_t, 1.5) + GRASS_ROOT_DL * (1.0 - v_t) * (1.0 - v_t) + v_dl) * v_k;
	ToonCol tcol;
	if (toon_is_const(tctx.cid) || (tctx.flags & TOON_F_INTERIOR) != 0) {
		tcol = toon_colors(tctx, tmat);
	} else {
		vec3 lit = ok_adjust(pal_fetch(tctx.cid, PAL_ROW_CUR), 1.0, 1.0, dl);
		vec3 shade = ok_adjust(pal_fetch(tctx.cid + PAL_SHADE_OFFSET, PAL_ROW_CUR), 1.0, 1.0, dl);
		vec3 lamp = ok_adjust(pal_fetch(tctx.cid, PAL_ROW_LAMP), 1.0, 1.0, dl);
		tcol = toon_colors_from(tctx, tmat, lit, shade, lamp);
	}
	TOON_FRAGMENT_END(tcol)
}

void light() {
	TOON_LIGHT
}
