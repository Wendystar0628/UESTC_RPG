// veg_trunk.gdshader：树干与枝（tree.bark，下半截可刷白）；同一着色器也画 ShadowsOnly 投影代理（木质加 L1 团块）（WLD-VEGETATION）
// 规格：WLD-VEGETATION；ENG-PERF；ART-STYLE（植被三阶）。
// 看得见的树冠团块在 veg_card 里（max-011：团块与叶卡同一面）；这里的团块只出现在投影代理上，挖洞、随叶量收缩。
// 树母型顶点数据：CUSTOM0.w < 0 是木质，其余是团块（CUSTOM2 = 所属团块或小球的球心）；
// 远池替身的木质 CUSTOM2.w = 1，与近池 L1 在 135–165 m 按 Bayer 抖动互补交接（植被公共段 veg_pool_code，23c2c）。
// 每株量读 INSTANCE_CUSTOM（§11.2），树种量读全局纹理 tree_species（§11.6）；按距离的判断只用 MAIN_CAM_INV_VIEW_MATRIX。
shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert, specular_disabled, ambient_light_disabled;

#include "res://style/shaders/toon.gdshaderinc"

// 投影代理（ENG-PERF）：打开后团块按世界坐标噪声挖出 0.4–0.8 m 的洞，形成林荫光斑
instance uniform float shadow_proxy = 0.0;

#include "res://style/shaders/veg_common.gdshaderinc"

const float VEG_WHITEWASH_Y = 1.2;         // 刷白高度（m，WLD-VEGETATION）
const float VEG_HOLE_CELL = 0.75;          // 投影代理挖洞的噪声格（m）
const float VEG_HOLE_COVER = 0.42;         // 投影代理挖掉的比例

// toon.gdshaderinc 已占 25 个 varying 分量，本着色器只剩 7 个（已用满，交接编码并进 v_row）：标志位合进一个整数，t0 直接读 v_spare（同是 COLOR.a）。
varying flat int v_bits;      // 位 0 团块（否则木质），位 1 刷白，位 2 团块开花
varying flat int v_row;       // 位 0–5 树种号（−1 记成 63），位 6 起是远近池交接的编码 × 1000（veg_pool_code，23c2c）
varying flat vec2 v_tint;
varying float v_s;
varying float v_ly;

// 投影代理的洞：三维值噪声
bool veg_hole(vec3 wp) {
	vec3 g = wp / VEG_HOLE_CELL;
	vec3 i = floor(g);
	vec3 f = g - i;
	f = f * f * (3.0 - 2.0 * f);
	float n = 0.0;
	for (int k = 0; k < 8; k++) {
		vec3 o = vec3(float(k & 1), float((k >> 1) & 1), float(k >> 2));
		vec3 c = i + o;
		uint h = uint(int(c.x) * 73856093) ^ uint(int(c.y) * 19349663) ^ uint(int(c.z) * 83492791);
		float w = mix(1.0 - f.x, f.x, o.x) * mix(1.0 - f.y, f.y, o.y) * mix(1.0 - f.z, f.z, o.z);
		n += w * lamp_hash01(h);
	}
	return n < VEG_HOLE_COVER;
}

void vertex() {
	VegInst inst = veg_instance(INSTANCE_CUSTOM);
	VegSpecies sp = veg_species(inst.row);
	float s = veg_local_season(inst);
	bool wood = CUSTOM0.w < 0.0;
	float hb = wood ? 0.0 : CUSTOM0.w;
	int bits = (wood ? 0 : 1) | ((inst.variant & 1) << 1);
	v_ly = VERTEX.y;
	if (!wood) {
		// 团块的季节值再加团块哈希和高度的小偏移：树顶和向阳面先变（§9.4、§11.6）
		s += VEG_LEAD_BLOB * (hb - 0.5) + VEG_LEAD_TOP * (COLOR.a - 0.5);
		float a = veg_anchor(sp.a, s);
		// 团块按 smoothstep(0.15, 0.6, A) 向团心收缩（云团型向所属小球的球心，§9.2）
		VERTEX = mix(CUSTOM2.xyz, VERTEX, smoothstep(0.15, 0.6, a + 0.1 * (hb - 0.5)));
		float b = veg_anchor(sp.b, s);
		if ((sp.flags & VEG_FLAG_BLOOM_BLOBS) != 0 && sp.bloom_cid >= 0 && CUSTOM1.w < b) {
			bits |= 4;
		}
	}
	v_bits = bits;
	v_s = s;
	vec3 origin = MODEL_MATRIX[3].xyz;
	v_tint = veg_tint_noise(origin);
	// 远池替身的木质 CUSTOM2.w = 1（MotherModels.FarStandIn）；投影代理只进阴影通道，不淡化
	float fade = veg_pool_code(MAIN_CAM_INV_VIEW_MATRIX[3].xyz, origin, inst, wood && CUSTOM2.w > 0.5, IN_SHADOW_PASS);
	v_row = (inst.row & 63) | (int(round(fade * 1000.0)) << 6);
	vec3 dir_m = normalize(inverse(mat3(MODEL_MATRIX)) * vec3(wind.x, 0.0, wind.y) + vec3(1e-5, 0.0, 0.0));
	VERTEX = veg_wind(VERTEX, origin, dir_m, inst, sp, hb, wood ? 0.0 : 1.0, 0.0);
	TOON_VERTEX
}

void fragment() {
	if (veg_pool_discard(FRAGCOORD.xy, float(v_row >> 6) / 1000.0)) {
		discard;
	}
	if (shadow_proxy > 0.5 && (v_bits & 1) != 0 && veg_hole((INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz)) {
		discard;
	}
	TOON_FRAGMENT_BEGIN
	VegSpecies sp = veg_species(v_row & 63);
	vec3 lit;
	vec3 shade;
	vec3 lamp;
	float trans_k = 0.0;
	float grad = -1.0;
	if ((v_bits & 1) == 0) {
		// 树干、枝：tree.bark；变体位 1 时下半截刷白（§1.7，借 wall.greyWhite），色号取树种表
		int cid = ((v_bits & 2) != 0 && v_ly < VEG_WHITEWASH_Y) ? sp.whitewash_cid : v_cid;
		lit = pal_cur(cid);
		shade = pal_cur(cid + PAL_SHADE_OFFSET);
		lamp = pal_fetch(cid, PAL_ROW_LAMP);
		// 主枝、二级枝朝上的一面覆雪（§6.4），竖直的树干不挂雪
		veg_snow_apply(sp, veg_snow_cover(tctx.n_w.y, tctx.ao, 0.5), lit, shade, lamp, grad, trans_k);
	} else {
		bool bloom = (v_bits & 4) != 0;
		int cid = bloom ? sp.bloom_cid : v_cid;
		int row = bloom ? sp.bloom_row : sp.leaf_row;
		lit = veg_tint(veg_pal(cid, row, v_s), v_tint);
		shade = veg_tint(veg_pal(cid + PAL_SHADE_OFFSET, row >= 4 ? row + 1 : -1, v_s), v_tint);
		lamp = pal_fetch(cid, PAL_ROW_LAMP);
		trans_k = 1.0;
		grad = v_spare;
	}
	ToonCol tcol = veg_colors(tctx, tmat, lit, shade, lamp, grad, trans_k);
	TOON_FRAGMENT_END(tcol)
	v_band.z += VEG_LIT_SHIFT;
}

void light() {
	TOON_LIGHT
}
