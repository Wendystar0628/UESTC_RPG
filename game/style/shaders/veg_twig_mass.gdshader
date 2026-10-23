// veg_twig_mass.gdshader：冬季中距的细枝体（WLD-VEGETATION，GAME-057）
// 规格：WLD-VEGETATION（冬季裸枝）；ART-STYLE（植被三阶）。
// 落叶针叶树（水杉）裸冠的中远景替身：沿树冠轴的一个收窄锥形外壳（MotherModels.TwigMass），alpha 混合成一层暖红褐色的细枝体，
// 主枝、树干和后面的湖面从里面透出来。借的是落叶树中远景的通行做法：树冠外壳上的半透明细枝层，与近处真实细枝交叉淡化。
// 只剔背面，一株一层；不写深度，所以不进描线、不挡后面的东西；不投影（顶点标志 TOON_F_NOSHADOW）。
// 同一个 MultiMesh 里不逐株排序：颜色统一，错序看不出来。
// 叶量按 smoothstep(0.15, 0.6, A) 收团块时（与 veg_card 团块同一个量）细枝体按裸的程度淡入；近处按 veg_winter_far 淡出，
// 与 veg_card 的枝梢笔触互补。按轮生分层（GAME-211）：每层主枝上一片稍向下垂的细枝层，层与层之间稀，轮廓一层一层收进，
// 层里是横向拉长的细碎疏密，读成一层层细枝而不是一团雾；不透明度集中在层里，层间透出后面的景。
// 顶点数据：CUSTOM0 = (按主枝层数计的高度，层上为整数, 0, 0, 每株哈希)，CUSTOM2.w = 1 是远池替身里的那份（与远近池交接同一套抖动）。
shader_type spatial;
render_mode blend_mix, depth_draw_never, cull_back, diffuse_lambert, specular_disabled, ambient_light_disabled;

#include "res://style/shaders/toon.gdshaderinc"
#include "res://style/shaders/veg_common.gdshaderinc"

const float VEG_TWIG_MASS_OPACITY = 0.8;           // 细枝层里的不透明度
const float VEG_TWIG_MASS_GAP = 0.3;                // 层与层之间留下的不透明度比例
const vec2 VEG_TWIG_MASS_LAYER = vec2(0.15, 0.45);  // 离细枝层中心（层间距的比例）从前者到后者，由层里过渡到层间
const float VEG_TWIG_MASS_LAYER_FAR = 0.7;          // 层分不出来的远处，整体取这个层量（比层里稀、比层间密）
const float VEG_TWIG_MASS_DROOP = 0.12;             // 细枝层中心在主枝下方这么多个层间距（细枝稍下垂）
const vec2 VEG_TWIG_MASS_SOFT = vec2(0.55, 0.98);   // 外壳 rim（1 − |法线·视线|）从前者到后者，不透明度降到 0：柔边只在轮廓最外一圈，细枝层横着伸到轮廓
const float VEG_TWIG_MASS_BREAK = 0.5;              // 碎边：噪声把柔边带前后推这么多（粗细两级噪声）
const float VEG_TWIG_MASS_COARSE = 0.4;             // 粗一级噪声的坐标缩放（格子约是细一级的 2.5 倍，约一簇侧枝）
const float VEG_TWIG_MASS_TIER = 0.35;              // 层间的轮廓比层里早这么多淡掉：轮廓一层一层收进
const float VEG_TWIG_MASS_MOTTLE = 0.6;             // 层里不透明度的起伏（±一半），细枝疏密不匀
const vec3 VEG_TWIG_MASS_STREAK = vec3(1.0, 3.0, 1.0);  // 疏密噪声的坐标缩放：竖向更密，碎块横向拉长，读成横伸的细枝
const vec2 VEG_TWIG_MASS_PX = vec2(1.5, 4.0);       // 噪声格在屏幕上从前者到后者个像素，碎边与起伏从 0 放到满（远处不闪）
const float VEG_TWIG_MASS_SNOW = 0.35;              // 覆雪只留这个比例：落叶树只在枝梢镶一线雪，不像常绿树冠顶一层白

varying flat int v_row;       // 位 0–5 树种号（−1 记成 63），位 6 起是远近池交接的10-bit coverage interval（veg_pool_code）
varying flat vec2 v_tint;
varying flat float v_hash;
varying float v_weight;
varying float v_tier;

void vertex() {
	VegInst inst = veg_instance(INSTANCE_CUSTOM);
	VegSpecies sp = veg_species(inst.row);
	vec3 origin = MODEL_MATRIX[3].xyz;
	vec3 cam = MAIN_CAM_INV_VIEW_MATRIX[3].xyz;
	v_tint = veg_tint_noise(origin);
	v_hash = CUSTOM0.w;
	v_tier = CUSTOM0.x;
	int fade = veg_pool_code(cam, origin, inst, CUSTOM2.w > 0.5, IN_SHADOW_PASS, INSTANCE_ID);
	v_row = (inst.row & 63) | (fade << 6);
	float a = veg_anchor(sp.a, veg_local_season(inst));
	v_weight = (1.0 - smoothstep(0.15, 0.6, a)) * veg_winter_far(cam, origin, inst);
	vec3 dir_m = normalize(inverse(mat3(MODEL_MATRIX)) * vec3(wind.x, 0.0, wind.y) + vec3(1e-5, 0.0, 0.0));
	VERTEX = veg_wind(VERTEX, origin, dir_m, inst, sp, v_hash, 1.0, 0.0);
	// 有叶时、近处没有细枝体：塌成一点，不进光栅
	if (v_weight <= 0.002) {
		VERTEX = vec3(0.0);
	}
	TOON_VERTEX
}

void fragment() {
	if (veg_pool_discard(FRAGCOORD.xy, v_row >> 6)) {
		discard;
	}
	TOON_FRAGMENT_BEGIN
	int row = v_row & 63;
	VegSpecies sp = veg_species(row);
	// 柔边与碎边：外壳侧对镜头的一圈淡到 0，噪声把这一圈前后推开；体内按另一层噪声略有疏密
	float rim = 1.0 - abs(dot(normalize(NORMAL), VIEW));
	vec3 wp = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float px_m = -VERTEX.z * 2.0 / (PROJECTION_MATRIX[1][1] * VIEWPORT_SIZE.y);
	float px_k = smoothstep(VEG_TWIG_MASS_PX.x, VEG_TWIG_MASS_PX.y, VEG_EDGE_CELL * 0.75 / max(px_m, 1e-4));
	float nf = veg_edge_noise(wp * VEG_TWIG_MASS_STREAK, row, VEG_CARD_NEEDLE);
	float nc = veg_edge_noise(wp * VEG_TWIG_MASS_COARSE + vec3(11.3, 7.1, 3.7), row, VEG_CARD_NEEDLE);
	// 细枝层：每层主枝（v_tier 的整数处）稍下方一片，层间稀；层的疏密按屏幕上层间距的像素收拢，远处读成均匀的一层红褐
	float f = fract(v_tier + VEG_TWIG_MASS_DROOP + 0.5) - 0.5;
	float tier_px = smoothstep(2.0, 6.0, abs(fwidth(v_tier)) > 1e-5 ? 1.0 / fwidth(v_tier) : 1e3);
	float layer = mix(VEG_TWIG_MASS_LAYER_FAR, 1.0 - smoothstep(VEG_TWIG_MASS_LAYER.x, VEG_TWIG_MASS_LAYER.y, abs(f)), tier_px);
	float push = ((0.6 * nc + 0.4 * nf - 0.5) * VEG_TWIG_MASS_BREAK + (1.0 - layer) * VEG_TWIG_MASS_TIER) * px_k;
	float edge = 1.0 - smoothstep(VEG_TWIG_MASS_SOFT.x, VEG_TWIG_MASS_SOFT.y, rim + push);
	float mottle = (1.0 + (nf - 0.5) * VEG_TWIG_MASS_MOTTLE * px_k) * mix(VEG_TWIG_MASS_GAP, 1.0, layer);
	// 颜色：tree.winterTwig（顶点色号），不透光、不加冠内梯度
	vec3 lit = veg_tint(pal_cur(v_cid), v_tint);
	vec3 shade = veg_tint(pal_cur(v_cid + PAL_SHADE_OFFSET), v_tint);
	vec3 lamp = pal_fetch(v_cid, PAL_ROW_LAMP);
	float t_grad = -1.0;
	float trans_k = 0.0;
	veg_snow_apply(sp, veg_snow_cover(tctx.n_w.y, 1.0, v_hash) * VEG_TWIG_MASS_SNOW, lit, shade, lamp, t_grad, trans_k);
	ToonCol tcol = veg_colors(tctx, tmat, lit, shade, lamp, -1.0, 0.0);
	TOON_FRAGMENT_END(tcol)
	ALPHA = clamp(VEG_TWIG_MASS_OPACITY * v_weight * edge * mottle, 0.0, 1.0);
	v_band.z += VEG_LIT_SHIFT;
}

void light() {
	TOON_LIGHT
}
