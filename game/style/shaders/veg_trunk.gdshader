// veg_trunk.gdshader：树干与枝（tree.bark，下半截可刷白）；同一着色器也画 ShadowsOnly 投影代理（木质加 L1 团块）（WP-23）
// 规格：WLD-VEGETATION §1、§9、§11；ENG-PERF §1.4、§9；ART-STYLE §2.1（植被三阶）。
// 看得见的树冠团块在 veg_card 里（max-011：团块与叶卡同一面）；这里的团块只出现在投影代理上，挖洞、随叶量收缩。
// 顶点数据见 Campus.Gen.Vegetation.MotherModels：CUSTOM0.w < 0 是木质，其余是团块（CUSTOM2 = 所属团块或小球的球心）。
// 每株量读 INSTANCE_CUSTOM（§11.2），树种量读全局纹理 tree_species（§11.6）；按距离的判断只用 MAIN_CAM_INV_VIEW_MATRIX。
shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert, specular_disabled, ambient_light_disabled;

#include "res://style/shaders/toon.gdshaderinc"

// 投影代理（ENG-PERF §9）：打开后团块按世界坐标噪声挖出 0.4–0.8 m 的洞，形成林荫光斑
instance uniform float shadow_proxy = 0.0;

// ===========================================================================
// 植被公共段：veg_trunk 与 veg_card 逐字相同（WLD-VEGETATION §11 的"植被 include"）。
// 植被 include 进白名单前两份同步修改；进白名单后整段挪进 veg_common.gdshaderinc（契约申请 contract-24）。
// 可调参数集中在这一段的常量里。
// ===========================================================================
const float VEG_TABLE_TAG = 23.0;          // 树种表行已写入的标记（SpeciesTable.Tag）
const int VEG_FLAG_DECIDUOUS = 1;
const int VEG_FLAG_BLOOM_BLOBS = 2;
const int VEG_CARD_FAN = 0;
const int VEG_CARD_NEEDLE = 1;
const int VEG_CARD_STRAND = 2;
const int VEG_CARD_PALMATE = 3;
const int VEG_CARD_OVATE = 4;
const int VEG_CARD_PINNATE = 5;
const int VEG_CARD_BLOSSOM = 6;
const int VEG_CARD_SPRAY = 7;
const int VEG_CARD_TWIG = 8;
const int VEG_CARD_FROND = 9;
const float VEG_SEASON_JITTER = 0.12;      // 逐株季节错相 ±0.06（§9.1）
const float VEG_LEAD_BLOB = 0.03;          // 团块哈希带来的先后（季节单位）
const float VEG_LEAD_TOP = 0.04;           // 树顶、向阳面先变（按 t0，§9.4）
const float VEG_LIT_SHIFT = 0.08;          // 影 → 受光的阈值从全场的 −0.03 挪到植被的 0.05（ENG-PERF §9）
const float VEG_TOP_LIFT = 0.06;           // 冠顶亮：受光 L +0.06（§1.3）
const float VEG_GRAD_SHADE = 0.04;         // 冠内梯度在影里的连续项（L，±0.02）
const float VEG_HUE_JITTER = 0.10;         // 世界坐标色相噪声 ±0.10 rad ≈ ±6°（ART-MODELING §3）
const float VEG_L_JITTER = 0.04;           // 世界坐标明度噪声 ±4%
const float VEG_TINT_CELL = 26.0;          // 色相噪声的格子（m），相邻的树颜色接近
const float VEG_WIND_SPEED = 6.0;          // 风的相位沿风向传播的速度（m/s，WP-26 风场进来前的桩）
const float VEG_BLOB_SWAY = 0.045;         // 团块摆动（m，3–6 cm，0.6–1.2 Hz）
const float VEG_CARD_FLUTTER = 0.015;      // 叶片颤动（m，1–2 cm，3–6 Hz）

struct VegInst {
	float seed;       // 稳定种子（实体 ID 哈希）
	int row;          // 树种号
	int variant;      // 外观变体号：位 1 = 树干下半截刷白
	float wind_pos;   // 风向位置 0–1
	float height;     // 树高 H（m）
};

struct VegSpecies {
	bool valid;
	vec4 a;           // 叶量 A 的四季锚点
	vec4 b;           // 开花 B 的四季锚点
	float sway;       // 最大摆角（度）
	int leaf_row;     // 叶色受光的季相行（影 = 行 + 1），−1 = 取第 0 行
	int bloom_row;    // 花色受光的季相行，−1 = 花色在第 0 行
	int bloom_cid;    // 花色受光色号，−1 = 不开花
	int leaf_card;
	int bloom_card;
	int twig_card;
	int flags;
	int twig_cid;     // 枝条笔触的受光色号（tree.bare）
};

// 每株量（WLD-VEGETATION §11.2）：x 种子，y 树种号 × 16 + 变体号，z 风向位置，w 树高。
// 不是 MultiMesh 的挂载读到的是 0：按"树种表缺省"处理，满冠、不开花、颜色取第 0 行。
VegInst veg_instance(vec4 ic) {
	if (ic.w <= 0.0) {
		return VegInst(0.37, -1, 0, 0.5, 10.0);
	}
	int code = int(round(ic.y));
	int row = code / 16;
	return VegInst(ic.x, row, code - row * 16, ic.z, ic.w);
}

VegSpecies veg_species(int row) {
	VegSpecies s;
	s.valid = false;
	s.a = vec4(1.0);
	s.b = vec4(0.0);
	s.sway = 2.0;
	s.leaf_row = -1;
	s.bloom_row = -1;
	s.bloom_cid = -1;
	s.leaf_card = VEG_CARD_OVATE;
	s.bloom_card = VEG_CARD_BLOSSOM;
	s.twig_card = VEG_CARD_TWIG;
	s.flags = 0;
	s.twig_cid = 0;
	if (row < 0 || row > 31) {
		return s;
	}
	// 树种表由 WP-38 启动时写入（WLD-VEGETATION §11.6）；没写时读不到标记，走上面的缺省。
	if (abs(texelFetch(tree_species, ivec2(7, row), 0).w - VEG_TABLE_TAG) > 0.5) {
		return s;
	}
	vec4 t2 = texelFetch(tree_species, ivec2(2, row), 0);
	vec4 t3 = texelFetch(tree_species, ivec2(3, row), 0);
	vec4 t4 = texelFetch(tree_species, ivec2(4, row), 0);
	s.valid = true;
	s.a = texelFetch(tree_species, ivec2(0, row), 0);
	s.b = texelFetch(tree_species, ivec2(1, row), 0);
	s.sway = t2.x;
	s.leaf_row = int(round(t2.y));
	s.bloom_row = int(round(t2.z));
	s.bloom_cid = int(round(t2.w));
	s.leaf_card = int(round(t3.x));
	s.bloom_card = int(round(t3.y));
	s.twig_card = int(round(t3.z));
	s.flags = int(round(t3.w));
	s.twig_cid = int(round(t4.z));
	return s;
}

float veg_hash(float seed, uint salt) {
	return lamp_hash01(uint(seed * 16777216.0) * 2654435761u + salt);
}

// 局部季节（WLD-VEGETATION §11.6）：φ = 0.6·seed + 0.4·z；转场按 φ 先后，再加逐株错相
float veg_local_season(VegInst inst) {
	float phi = 0.6 * inst.seed + 0.4 * inst.wind_pos;
	float k = clamp((season.w - 0.3 * phi) / 0.7, 0.0, 1.0);
	return mix(season.y, season.z, k) + (veg_hash(inst.seed, 9u) - 0.5) * VEG_SEASON_JITTER;
}

// 四季锚点之间 smootherstep 插值，冬 → 春循环（§9.2）
float veg_anchor(vec4 v, float s) {
	float x = mod(s, 4.0);
	float i0 = floor(x);
	float f = x - i0;
	float t = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
	vec4 e0 = vec4(equal(vec4(i0), vec4(0.0, 1.0, 2.0, 3.0)));
	vec4 e1 = vec4(equal(vec4(mod(i0 + 1.0, 4.0)), vec4(0.0, 1.0, 2.0, 3.0)));
	return mix(dot(v, e0), dot(v, e1), t);
}

// 季相色：有季相行时按局部季节取样（§9.4），否则取第 0 行（全局季节的当前色）
vec3 veg_pal(int cid, int row, float s) {
	return row >= 4 ? pal_season(row, s) : pal_cur(cid);
}

// 世界坐标的低频噪声 → (色相, 明度)，相邻的树颜色接近（ART-MODELING §3）
vec2 veg_tint_noise(vec3 origin) {
	vec2 g = origin.xz / VEG_TINT_CELL;
	vec2 i = floor(g);
	vec2 f = g - i;
	f = f * f * (3.0 - 2.0 * f);
	vec2 r = vec2(0.0);
	for (int k = 0; k < 4; k++) {
		vec2 o = vec2(float(k & 1), float(k >> 1));
		vec2 c = i + o;
		uint h = uint(int(c.x) * 73856093) ^ uint(int(c.y) * 19349663);
		float w = mix(1.0 - f.x, f.x, o.x) * mix(1.0 - f.y, f.y, o.y);
		r += w * vec2(lamp_hash01(h * 2u + 1u), lamp_hash01(h * 2u + 2u));
	}
	return r;
}

vec3 veg_tint(vec3 c, vec2 tint) {
	vec3 o = ok_from_linear(c);
	float a = (tint.x - 0.5) * 2.0 * VEG_HUE_JITTER;
	float cs = cos(a);
	float sn = sin(a);
	o.yz = vec2(o.y * cs - o.z * sn, o.y * sn + o.z * cs);
	o.x *= 1.0 + (tint.y - 0.5) * 2.0 * VEG_L_JITTER;
	return ok_to_linear(o);
}

// 风（WP-26 的 motion_wind.gdshaderinc 进来前的桩，同一组参数）：主干 (y/H)^1.5 弯曲、团块摆动、叶片颤动。
// wind = (风向 x, 风向 z, 幅度系数, 湍流)；幅度系数为 0 时不动。dir_m 是模型空间的风向。
vec3 veg_wind(vec3 v, vec3 origin, vec3 dir_m, VegInst inst, VegSpecies sp, float blob_hash, float crown, float card) {
	float amp = wind.z;
	if (amp <= 0.0) {
		return v;
	}
	float t = anim_time - dot(origin.xz, wind.xy) / VEG_WIND_SPEED;
	float gust = 0.6 + 0.4 * sin(t * 1.3 + inst.seed * 6.2832) + 0.15 * wind.w * sin(t * 4.1 + blob_hash * 6.2832);
	float h = clamp(v.y / max(inst.height, 0.5), 0.0, 1.4);
	float bend = radians(sp.sway) * amp * gust * pow(h, 1.5);
	vec3 o = dir_m * (bend * inst.height);
	o.y -= 0.5 * bend * bend * inst.height * h;
	o += dir_m * (crown * VEG_BLOB_SWAY * amp * sin(t * 5.65 + blob_hash * 6.2832));
	o += vec3(0.0, card * VEG_CARD_FLUTTER * amp * sin(t * 28.3 + blob_hash * 40.0), 0.0);
	return v + o;
}

// 片元取色：照 toon_colors 的室外分支（天光渐变、地面反光、200 m 降饱和、深影、透光、暖边），
// 颜色由调用方从季相行给出；植被不做湿润变暗和倒角提亮（WLD-VEGETATION §9.4）。
// t_grad < 0 时不加冠内梯度（木质部分）。
ToonCol veg_colors(ToonCtx c, ToonMat m, vec3 lit, vec3 shade, vec3 lamp, float t_grad, float trans_k) {
	if (t_grad >= 0.0) {
		float t = t_grad + 0.32 * dot(c.n_w, normalize(key_dir));
		lit = ok_adjust(lit, 1.0, 1.0, VEG_TOP_LIFT * smoothstep(0.62, 0.82, t));
		shade = ok_adjust(shade, 1.0, 1.0, VEG_GRAD_SHADE * (clamp(t, 0.0, 1.0) - 0.5));
	}
	shade *= 1.0 + TOON_SKY_GRAD * c.n_w.y;
	float day = smoothstep(-0.02, 0.05, sun_dir.y);
	float bounce = TOON_BOUNCE * clamp(-c.n_w.y, 0.0, 1.0) * key_strength * day;
	shade = mix(shade, shade * pal_cur(PAL_BOUNCE_GROUND) * 1.6, bounce);
	float fd = fog_equiv_dist(c.wpos, c.cam);
	lit = fog_desaturate(lit, fd);
	shade = fog_desaturate(shade, fd);
	lamp = fog_desaturate(lamp, fd);
	float bw = max(m.band, 1e-3);
	float push = TOON_AO_PUSH * (1.0 - clamp(c.ao, 0.0, 1.0));
	float deep_mix = 0.0;
	if (m.steps == TOON_STEPS_THREE) {
		float bwd = mix(bw, max(bw, TOON_BAND_OVERCAST), toon_overcast());
		deep_mix = 1.0 - smoothstep(TOON_THR_DEEP - bwd, TOON_THR_DEEP + bwd, c.d - push);
	}
	vec3 trans = vec3(0.0);
	if (m.k_t * trans_k > 0.0) {
		trans = ok_adjust(lit, 1.0, 1.2, 0.08) * m.k_t * trans_k * (1.0 - smoothstep(0.6, 0.8, toon_overcast()));
	}
	vec3 rim = pal_cur(PAL_RIM) * lit * m.k_rim;
	return ToonCol(lit, shade, lamp, trans, rim, vec3(0.0), deep_mix, bw, 1.0, push, 1.0);
}
// ===========================================================================
// 植被公共段结束
// ===========================================================================

const float VEG_WHITEWASH_Y = 1.2;         // 刷白高度（m，WLD-VEGETATION §1.7）
const float VEG_HOLE_CELL = 0.75;          // 投影代理挖洞的噪声格（m）
const float VEG_HOLE_COVER = 0.42;         // 投影代理挖掉的比例

// toon.gdshaderinc 已占 25 个 varying 分量，本着色器只剩 7 个：标志位合进一个整数，t0 直接读 v_spare（同是 COLOR.a）。
varying flat int v_bits;      // 位 0 团块（否则木质），位 1 刷白，位 2 团块开花
varying flat int v_row;
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
	v_row = inst.row;
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
	vec3 dir_m = normalize(inverse(mat3(MODEL_MATRIX)) * vec3(wind.x, 0.0, wind.y) + vec3(1e-5, 0.0, 0.0));
	VERTEX = veg_wind(VERTEX, origin, dir_m, inst, sp, hb, wood ? 0.0 : 1.0, 0.0);
	TOON_VERTEX
}

void fragment() {
	if (shadow_proxy > 0.5 && (v_bits & 1) != 0 && veg_hole((INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz)) {
		discard;
	}
	TOON_FRAGMENT_BEGIN
	VegSpecies sp = veg_species(v_row);
	vec3 lit;
	vec3 shade;
	vec3 lamp;
	float trans_k = 0.0;
	float grad = -1.0;
	if ((v_bits & 1) == 0) {
		// 树干、枝：tree.bark；变体位 1 时下半截刷白（§1.7，借 wall.greyWhite），色号存在备用字节里
		int cid = ((v_bits & 2) != 0 && v_ly < VEG_WHITEWASH_Y) ? int(round(v_spare * 255.0)) : v_cid;
		lit = pal_cur(cid);
		shade = pal_cur(cid + PAL_SHADE_OFFSET);
		lamp = pal_fetch(cid, PAL_ROW_LAMP);
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
