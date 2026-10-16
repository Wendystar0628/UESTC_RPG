// veg_card.gdshader：整个树冠（团块与小球，叶片、花、枝条笔触的插片）（WP-23）
// 规格：WLD-VEGETATION §1.1、§1.8、§9.2–§9.3、§11.1–§11.3；ENG-PERF §9；max-011 评审（统一树冠法线，团块与叶卡同一面）。
// 团块和叶卡是同一个表面、同一个材质 ID，描线只在树冠外轮廓出线；法线全部来自一个平滑外壳（生成时烘进 NORMAL），
// 整冠读成一块受光面加一条影带。alpha scissor，留在不透明通道；双面。
// 插片朝向 = mix(外壳法线, 主相机视线, 0.5)，侧对镜头时缩小；只在外轮廓一圈保持全尺寸，朝向镜头的内部插片只留少数。
// 插片 3–5 张一簇（簇心压进团块），一簇同去同留、同明度；团块在外轮廓一圈按树种稳定的噪声剪成叶团锯齿边（max-011 评审③④）。
// 叶卡在主线版由本着色器按层号程序化画出（层号 = Campus.Gen.Vegetation.CardShape），细节版换成 Texture2DArray 的同号层。
// 顶点数据见 MotherModels：插片 CUSTOM0 = (插片中心, h2)，CUSTOM1 = (枝梢锚点, h3)，CUSTOM2 = (团心, 重要度序)；
// 团块 CUSTOM0 = (团心, 团块哈希)，CUSTOM1 = (枝梢锚点, 开花哈希)，CUSTOM2 = (所属小球的球心, −1)。
shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_disabled, diffuse_lambert, specular_disabled, ambient_light_disabled;

#include "res://style/shaders/toon.gdshaderinc"

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

// 片元取色：照 toon_colors 的室外分支（天光渐变、地面反光、路灯光斑、200 m 降饱和、深影、透光、暖边），
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
	// 路灯光斑（WP-39，与 toon 同一个函数）：树干与 4 m 以下的树冠接路灯的暖光，夜里不只剩调色板的月光色
	toon_lamp_pool(c, lamp, lit, shade);
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

const float VEG_NEAR_COLLAPSE = 1.5;       // 1.5 m 内插片塌缩（§11.3）
const float VEG_LOD_L0 = 15.0;             // 近池三级的切换距离（12 m 树，ENG-PERF §9），乘 树高/12 × 细节系数
const float VEG_LOD_L1 = 40.0;
const float VEG_KEEP_L0 = 0.4;             // L0 留下的插片比例（MotherModels.CardCount 的前缀）
const float VEG_KEEP_L1 = 0.0556;          // L1 留下的插片比例
const float VEG_COVER_GROW = 1.5;          // 远处按像素外扩叶形的上限，保住树冠覆盖率（§11.1）
// 叶卡集中在外轮廓一圈（max-011 评审）：rim = 1 − |外壳法线·视线|，轮廓处为 1、正对镜头为 0
const float VEG_RIM_IN = 0.2;              // rim 低于它算树冠内部
const float VEG_RIM_OUT = 0.55;            // rim 高于它算外轮廓一圈，插片全尺寸
const float VEG_INNER_KEEP = 0.3;          // 内部插片留下的比例（按插片哈希）
const float VEG_INNER_SIZE = 0.75;         // 留下的内部插片的尺寸
const float VEG_CARD_DL = 0.03;            // 插片的明度起伏（OKLab L，±，一簇一个值），内部不画轮廓，只靠它读出叶簇
// 外轮廓碎边（max-011 评审③）：团块在外轮廓一圈按树种稳定的噪声剪掉像素（alpha scissor），描线跟着剪后的轮廓走
const float VEG_EDGE_RIM0 = 0.3;           // 远处：rim 从这里起进入碎边带
const float VEG_EDGE_RIM1 = 0.82;          // 远处：rim 到这里碎边最深
const float VEG_EDGE_DEPTH = 0.62;         // 远处：带内最深处剪掉的比例
const float VEG_EDGE_CELL = 0.42;          // 噪声格（m），乘树种叶卡的格子系数；约一个叶团大小
const float VEG_EDGE_FAR = 1.0;            // L0 以外的碎边强度
const float VEG_EDGE_CONTRAST = 2.2;       // 噪声对比
// 近处（L0+ 以内，max-011 23b 评审）：只剪最外面一圈轮廓带，带内不出孔。噪声沿"朝轮廓的方向"取（外壳法线去掉视线分量），
// 同一条朝轮廓的径向线上取值相同，剪的比例又朝轮廓单调变大，所以剪掉的部分总连着轮廓。
// 冠底（外壳法线朝下）从树下仰看时大片斜对镜头，不是轮廓，不剪；冠底的轮廓由叶卡打碎。
const float VEG_EDGE_NEAR = 0.35;          // 近处的碎边强度，叶卡已打碎轮廓
const float VEG_EDGE_NEAR_RIM0 = 0.72;     // 近处：rim 从这里起进入碎边带（只剩最外一圈）
const float VEG_EDGE_NEAR_RIM1 = 0.95;     // 近处：rim 到这里碎边最深
const float VEG_EDGE_NEAR_SPAN = 3.0;      // 缺口沿轮廓的疏密：按这个冠半径（m）铺噪声格
const vec2 VEG_EDGE_NEAR_DOWN = vec2(0.3, 0.7);     // 外壳法线朝下的分量从前者到后者，近处碎边从全剪退到不剪

// toon.gdshaderinc 已占 25 个 varying 分量，本着色器只剩 7 个：层号与标志合进一个整数，t0 直接读 v_spare（同是 COLOR.a）。
varying flat int v_bits;      // 位 0–3 叶卡层号，位 4 开花，位 5 枝条笔触，位 6 团块（不裁切），位 8–15 插片自己的叶形种子
varying flat int v_row;
varying flat float v_seed;
varying flat vec2 v_tint;
varying float v_s;

vec3 veg_tangent(vec3 n) {
	vec3 t = cross(vec3(0.0, 1.0, 0.0), n);
	return dot(t, t) > 1e-6 ? normalize(t) : vec3(1.0, 0.0, 0.0);
}

// 近池换级不跳变（§11.4）：高一级比低一级多出来的插片在过渡带里按重要度缩到 0
float veg_card_keep(float imp, float dist, float height) {
	float k = max(height, 1.0) / 12.0 * max(quality.x, 0.5);
	float l0 = VEG_LOD_L0 * k;
	float l1 = VEG_LOD_L1 * k;
	float keep = mix(1.0, VEG_KEEP_L0, smoothstep(l0 * 0.9, l0, dist));
	keep = mix(keep, VEG_KEEP_L1, smoothstep(l1 * 0.9, l1, dist));
	return 1.0 - smoothstep(keep - 0.02, keep, imp);
}

void vertex() {
	VegInst inst = veg_instance(INSTANCE_CUSTOM);
	VegSpecies sp = veg_species(inst.row);
	vec3 origin = MODEL_MATRIX[3].xyz;
	mat3 inv3 = inverse(mat3(MODEL_MATRIX));
	vec3 dir_m = normalize(inv3 * vec3(wind.x, 0.0, wind.y) + vec3(1e-5, 0.0, 0.0));
	v_row = inst.row;
	v_tint = veg_tint_noise(origin);
	if (CUSTOM2.w < 0.0) {
		// 团块与小球：NORMAL 是烘好的外壳法线，不再混合。季节值加团块哈希和高度的小偏移：树顶和向阳面先变（§9.4、§11.6）；
		// 叶量按 smoothstep(0.15, 0.6, A) 向所属小球的球心收缩（§9.2）；云团型开花时小球按哈希换花色（§9.3）
		float hb = CUSTOM0.w;
		float s = veg_local_season(inst) + VEG_LEAD_BLOB * (hb - 0.5) + VEG_LEAD_TOP * (COLOR.a - 0.5);
		float a = veg_anchor(sp.a, s);
		float b = veg_anchor(sp.b, s);
		VERTEX = mix(CUSTOM2.xyz, VERTEX, smoothstep(0.15, 0.6, a + 0.1 * (hb - 0.5)));
		bool bloom = (sp.flags & VEG_FLAG_BLOOM_BLOBS) != 0 && sp.bloom_cid >= 0 && CUSTOM1.w < b;
		v_bits = 64 | (bloom ? 16 : 0);
		v_seed = hb;
		v_s = s;
		VERTEX = veg_wind(VERTEX, origin, dir_m, inst, sp, hb, 1.0, 0.0);
	} else {
		vec3 c = CUSTOM0.xyz;
		float h2 = CUSTOM0.w;
		float s = veg_local_season(inst) + VEG_LEAD_TOP * (COLOR.a - 0.5);
		float a = veg_anchor(sp.a, s);
		float b = veg_anchor(sp.b, s);
		// 叶量缩放与冬季裸枝（§9.2、§11.6）：落叶树缩到 0.6 换成枝条笔触，并移到枝梢锚点
		float k = smoothstep(h2, h2 + 0.08, a);
		bool bloom = sp.bloom_cid >= 0 && CUSTOM1.w < b;
		bool twig = false;
		float size = k;
		float leafy = 1.0;
		vec3 center = c;
		if ((sp.flags & VEG_FLAG_DECIDUOUS) != 0) {
			size = mix(0.6, 1.0, k);
			center = mix(CUSTOM1.xyz, c, k);
			twig = k < 0.5;
			leafy = k;
		}
		int layer = twig ? sp.twig_card : (bloom ? sp.bloom_card : sp.leaf_card);
		// 一簇共用 h2（同落、同明度、同去留）；叶形各张不同，种子取每张自己的 h3
		int shape = int(CUSTOM1.w * 255.0) & 255;
		v_bits = (layer & 15) | ((bloom && !twig) ? 16 : 0) | (twig ? 32 : 0) | (shape << 8);
		v_seed = h2;
		v_s = s;

		// 朝向：mix(外壳法线, 视线, 0.5)，按主相机算，阴影通道也一样（§11.3）
		vec3 cam_w = MAIN_CAM_INV_VIEW_MATRIX[3].xyz;
		vec3 center_w = (MODEL_MATRIX * vec4(center, 1.0)).xyz;
		float dist = length(cam_w - center_w);
		vec3 view_m = normalize(inv3 * (cam_w - center_w));
		vec3 nb = normalize(NORMAL);
		vec3 n2 = normalize(nb + view_m + vec3(0.0, 1e-4, 0.0));
		vec3 t0 = veg_tangent(nb);
		vec3 off = VERTEX - c;
		vec2 uw = vec2(dot(off, t0), dot(off, cross(nb, t0)));
		vec3 t1 = veg_tangent(n2);
		vec3 b1 = cross(n2, t1);
		size *= smoothstep(0.08, 0.25, abs(dot(n2, view_m)));
		size *= veg_card_keep(CUSTOM2.w, dist, inst.height);
		size *= smoothstep(VEG_NEAR_COLLAPSE * 0.6, VEG_NEAR_COLLAPSE, dist);
		// 叶卡集中在外轮廓一圈，打碎轮廓边；朝向镜头的内部插片只留少数、缩小（max-011）。
		// 冬季的枝条笔触不受此限（按叶量过渡），棕榈的扇叶本身就是树冠，也不受此限。
		if (sp.leaf_card != VEG_CARD_FROND) {
			float rim = 1.0 - abs(dot(nb, view_m));
			float inner = veg_hash(h2, 31u) < VEG_INNER_KEEP ? VEG_INNER_SIZE : 0.0;
			size *= mix(1.0, mix(inner, 1.0, smoothstep(VEG_RIM_IN, VEG_RIM_OUT, rim)), leafy);
		}
		VERTEX = center + (uw.x * t1 + uw.y * b1) * size;
		// 插片用外壳法线（max-011 的统一树冠法线；棕榈扇叶用自己的法线）
		NORMAL = nb;
		VERTEX = veg_wind(VERTEX, origin, dir_m, inst, sp, h2, 1.0, 1.0);
	}
	TOON_VERTEX
}

// ---------------------------------------------------------------------------
// 叶卡（主线版程序化）：UV 的 v = 0 是叶柄端，叶形的距离以 UV 为单位，负值在里面
// ---------------------------------------------------------------------------
float veg_seg(vec2 p, vec2 a, vec2 b, float w0, float w1) {
	vec2 pa = p - a;
	vec2 ba = b - a;
	float h = clamp(dot(pa, ba) / max(dot(ba, ba), 1e-6), 0.0, 1.0);
	return length(pa - ba * h) - mix(w0, w1, h);
}

vec2 veg_rot(vec2 p, float a) {
	float c = cos(a);
	float s = sin(a);
	return vec2(c * p.x + s * p.y, -s * p.x + c * p.y);
}

// 单片叶，叶柄在原点、朝 +y，长 l
float veg_leaf(int layer, vec2 q, float l) {
	if (layer == VEG_CARD_FAN) {
		// 银杏：扇形叶片，顶缘中间一道浅缺，带细叶柄
		vec2 p = q - vec2(0.0, 0.32 * l);
		float r = length(p);
		float a = atan(p.x, p.y);
		float rr = 0.68 * l * (1.0 - 0.2 * exp(-a * a * 90.0)) * (1.0 - 0.06 * cos(a * 9.0));
		float blade = max(r - rr, (abs(a) - 0.95) * r);
		return min(blade, veg_seg(q, vec2(0.0), vec2(0.0, 0.34 * l), 0.018 * l, 0.012 * l));
	}
	if (layer == VEG_CARD_PALMATE) {
		// 红枫、木芙蓉：五裂掌状
		vec2 p = q - vec2(0.0, 0.5 * l);
		float a = atan(p.x, p.y);
		float rr = 0.48 * l * (0.5 + 0.5 * pow(abs(cos(2.5 * a)), 0.6));
		return min(length(p) - rr, veg_seg(q, vec2(0.0), vec2(0.0, 0.3 * l), 0.015 * l, 0.01 * l));
	}
	if (layer == VEG_CARD_NEEDLE || layer == VEG_CARD_PINNATE) {
		// 水杉的羽状小枝、蓝花楹与栾树的羽状复叶：叶轴加梳齿状小叶
		bool fine = layer == VEG_CARD_NEEDLE;
		float y = clamp(q.y / l, 0.0, 1.0);
		float w = (fine ? 0.2 : 0.3) * l * pow(sin(3.14159 * clamp(y * 0.92 + 0.04, 0.0, 1.0)), 0.6);
		float n = fine ? 22.0 : 9.0;
		float f = fract(q.y / l * n);
		float comb = (abs(f - 0.5) - (fine ? 0.3 : 0.36)) * l / n;
		float leaflets = max(abs(q.x) - w, comb);
		leaflets = max(leaflets, max(-q.y, q.y - l));
		return min(leaflets, veg_seg(q, vec2(0.0), vec2(0.0, l), 0.014 * l, 0.008 * l));
	}
	if (layer == VEG_CARD_STRAND) {
		// 垂柳：细长的披针形叶串
		vec2 p = q - vec2(0.0, 0.5 * l);
		float k = length(p / vec2(0.065 * l, 0.5 * l));
		return (k - 1.0) * 0.065 * l;
	}
	// 卵形叶（香樟、楠木、桂花与一般阔叶）：尖头
	vec2 p = q - vec2(0.0, 0.5 * l);
	float wx = 0.3 * l * (1.0 - 0.5 * smoothstep(0.0, 0.5 * l, p.y));
	float k = length(p / vec2(wx, 0.5 * l));
	return min((k - 1.0) * wx, veg_seg(q, vec2(0.0), vec2(0.0, 0.12 * l), 0.014 * l, 0.01 * l));
}

// 花：五瓣（樱、梅、海棠、玉兰、木芙蓉）或小簇（桂花、紫薇、栾树蒴果、蓝花楹）
float veg_flower(int layer, vec2 p, float r) {
	if (layer == VEG_CARD_SPRAY) {
		return length(p) - r;
	}
	float a = atan(p.x, p.y);
	return length(p) - r * (0.62 + 0.38 * pow(abs(cos(2.5 * a)), 0.7));
}

// 枝条笔触（§1.8）：一根主梢加两根侧梢，越往外越细，叶柄端接枝梢锚点
float veg_twig(vec2 p, float seed) {
	float bend = (seed - 0.5) * 0.2;
	float d = veg_seg(p, vec2(0.0, 0.0), vec2(bend, 0.55), 0.03, 0.02);
	d = min(d, veg_seg(p, vec2(bend, 0.55), vec2(bend * 2.0 + 0.04, 0.96), 0.02, 0.008));
	d = min(d, veg_seg(p, vec2(bend * 0.6, 0.33), vec2(-0.3, 0.72), 0.016, 0.006));
	d = min(d, veg_seg(p, vec2(bend * 1.2, 0.6), vec2(0.26, 0.88), 0.014, 0.005));
	return d;
}

// 棕榈：掌状扇叶，裂片到叶缘分开
float veg_frond(vec2 p) {
	vec2 q = p - vec2(0.0, 0.06);
	float r = length(q);
	float a = atan(q.x, q.y);
	float fan = max(r - 0.9 * (1.0 - 0.15 * a * a), (abs(a) - 1.3) * r);
	float seg = fract((a + 1.3) / 2.6 * 15.0);
	float split = (abs(seg - 0.5) - 0.38) * r * 0.42;
	fan = max(fan, r > 0.32 ? split : -1.0);
	return min(fan, veg_seg(p, vec2(0.0), vec2(0.0, 0.12), 0.02, 0.016));
}

float veg_card_sdf(int layer, vec2 uv, float seed) {
	vec2 p = uv - vec2(0.5, 0.0);
	if (layer == VEG_CARD_TWIG) {
		return veg_twig(p, seed);
	}
	if (layer == VEG_CARD_FROND) {
		return veg_frond(p);
	}
	uint base = uint(seed * 65536.0) * 7u;
	float d = 1e3;
	if (layer == VEG_CARD_BLOSSOM || layer == VEG_CARD_SPRAY) {
		bool spray = layer == VEG_CARD_SPRAY;
		int n = spray ? 9 : 4;
		for (int i = 0; i < 9; i++) {
			if (i >= n) {
				break;
			}
			float hx = lamp_hash01(base + uint(i) * 13u + 1u);
			float hy = lamp_hash01(base + uint(i) * 13u + 2u);
			vec2 c = spray ? vec2((hx - 0.5) * 0.6, 0.3 + 0.55 * hy) : vec2((hx - 0.5) * 0.52, 0.28 + 0.48 * hy);
			float r = spray ? 0.065 + 0.03 * hy : 0.15 + 0.05 * hx;
			d = min(d, veg_flower(layer, veg_rot(p - c, hx * 6.2832), r));
		}
		return min(d, veg_seg(p, vec2(0.0), vec2(0.0, 0.3), 0.014, 0.008));
	}
	int n = 6;
	float l = 0.42;
	float spread = 2.4;
	if (layer == VEG_CARD_FAN) {
		n = 4;
		l = 0.56;
		spread = 2.0;
	} else if (layer == VEG_CARD_PALMATE) {
		n = 3;
		l = 0.58;
		spread = 1.7;
	} else if (layer == VEG_CARD_NEEDLE) {
		n = 3;
		l = 0.9;
		spread = 0.9;
	} else if (layer == VEG_CARD_PINNATE) {
		n = 2;
		l = 0.92;
		spread = 0.8;
	} else if (layer == VEG_CARD_STRAND) {
		n = 5;
		l = 0.92;
		spread = 0.35;
	}
	for (int i = 0; i < 6; i++) {
		if (i >= n) {
			break;
		}
		float h = lamp_hash01(base + uint(i) * 13u + 3u);
		float a = (float(i) / float(max(n - 1, 1)) - 0.5) * spread + (h - 0.5) * 0.35;
		vec2 o = vec2((h - 0.5) * 0.12, 0.04 + 0.1 * fract(h * 7.0));
		d = min(d, veg_leaf(layer, veg_rot(p - o, a), l * (0.82 + 0.3 * fract(h * 13.0))));
	}
	return min(d, veg_seg(p, vec2(0.0), vec2(0.0, 0.1), 0.014, 0.01));
}

// 碎边噪声：世界坐标的三维值噪声，两级叠加，按树种行错开；格子随叶卡形状（细叶小、阔叶大）
float veg_edge_noise(vec3 wp, int row, int layer) {
	float cell = VEG_EDGE_CELL * (layer == VEG_CARD_NEEDLE || layer == VEG_CARD_STRAND ? 0.75 : (layer == VEG_CARD_FAN || layer == VEG_CARD_PALMATE ? 1.15 : 1.0));
	uint salt = uint(max(row, 0)) * 2246822519u + 97u;
	float n = 0.0;
	float w = 0.65;
	vec3 g = wp / cell;
	for (int o = 0; o < 2; o++) {
		vec3 i = floor(g);
		vec3 f = g - i;
		f = f * f * (3.0 - 2.0 * f);
		float v = 0.0;
		for (int k = 0; k < 8; k++) {
			vec3 c = i + vec3(float(k & 1), float((k >> 1) & 1), float(k >> 2));
			uint h = (uint(int(c.x) * 73856093) ^ uint(int(c.y) * 19349663) ^ uint(int(c.z) * 83492791)) + salt;
			float wk = mix(1.0 - f.x, f.x, float(k & 1)) * mix(1.0 - f.y, f.y, float((k >> 1) & 1)) * mix(1.0 - f.z, f.z, float(k >> 2));
			v += wk * lamp_hash01(h);
		}
		n += w * v;
		w = 0.35;
		g = g * 2.3 + vec3(17.1, 5.3, 11.7);
	}
	// 值噪声的和挤在 0.5 附近，拉开对比，剪切比例才跟 cut 接近线性
	return clamp((n - 0.5) * VEG_EDGE_CONTRAST + 0.5, 0.0, 1.0);
}

void fragment() {
	// 背面也用外壳法线（引擎给背面翻过一次法线，这里翻回来）
	if (!FRONT_FACING) {
		NORMAL = -NORMAL;
	}
	VegSpecies sp = veg_species(v_row);
	bool mass = (v_bits & 64) != 0;
	if (mass) {
		// 外轮廓碎边（max-011 评审③）：外壳法线侧对镜头的一圈按噪声剪掉，轮廓读成叶团的锯齿边。本体不投影（投影走代理），
		// 所以这里的距离就是主相机到片元的距离（视空间长度），不会在阴影通道里按灯算。
		vec3 n = normalize(NORMAL);
		float facing = dot(n, VIEW);
		float rim = 1.0 - abs(facing);
		// 片元里没有树高，按 12 m 的树取 L0 的切换距离（ENG-PERF §9），乘细节系数
		float l0 = VEG_LOD_L0 * max(quality.x, 0.5);
		float far_k = smoothstep(l0 * 0.6, l0, length(VERTEX));
		// 远处：按世界坐标噪声在 rim 带里剪（23b 定稿）
		float cut = VEG_EDGE_DEPTH * VEG_EDGE_FAR * smoothstep(VEG_EDGE_RIM0, VEG_EDGE_RIM1, rim);
		float e_far = veg_edge_noise((INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz, v_row, sp.leaf_card) - cut;
		// 近处：缺口噪声取朝轮廓的方向，按树错开（v_tint 是每株一个值）；冠底不剪
		float down = -(mat3(INV_VIEW_MATRIX) * n).y;
		float cut_near = VEG_EDGE_DEPTH * VEG_EDGE_NEAR * smoothstep(VEG_EDGE_NEAR_RIM0, VEG_EDGE_NEAR_RIM1, rim)
				* (1.0 - smoothstep(VEG_EDGE_NEAR_DOWN.x, VEG_EDGE_NEAR_DOWN.y, down));
		vec3 toward = mat3(INV_VIEW_MATRIX) * normalize(n - facing * VIEW + vec3(0.0, 1e-5, 0.0));
		float e_near = veg_edge_noise(toward * VEG_EDGE_NEAR_SPAN + vec3(53.1 * v_tint.x, 41.7 * v_tint.y, 29.3 * (v_tint.x + v_tint.y)), v_row, sp.leaf_card) - cut_near;
		float e = mix(e_near, e_far, far_k);
		ALPHA = clamp(e / max(fwidth(e), 1e-4) + 0.5, 0.0, 1.0);
	} else {
		// v_bits 是 flat 的，同一个图元内分支一致，fwidth 不受影响
		float d = veg_card_sdf(v_bits & 15, UV, float((v_bits >> 8) & 255) / 256.0);
		float fw = max(fwidth(d), 1e-4);
		// 远处按像素外扩，叶形不至于在远距离上变稀（主线版代替保覆盖率的 mip，§11.1）
		d -= fw * (0.35 + min(fw * 30.0, VEG_COVER_GROW));
		ALPHA = clamp(0.5 - d / fw, 0.0, 1.0);
	}
	ALPHA_SCISSOR_THRESHOLD = 0.5;
	TOON_FRAGMENT_BEGIN
	vec3 lit;
	vec3 shade;
	vec3 lamp;
	float trans_k = 1.0;
	bool twig = (v_bits & 32) != 0;
	if (twig) {
		// 枝条笔触用 tree.bare，不透光（§1.4、§1.8）
		int cid = sp.twig_cid;
		lit = pal_cur(cid);
		shade = pal_cur(cid + PAL_SHADE_OFFSET);
		lamp = pal_fetch(cid, PAL_ROW_LAMP);
		trans_k = 0.0;
	} else {
		bool bloom = (v_bits & 16) != 0;
		int cid = bloom ? sp.bloom_cid : v_cid;
		int row = bloom ? sp.bloom_row : sp.leaf_row;
		lit = veg_tint(veg_pal(cid, row, v_s), v_tint);
		shade = veg_tint(veg_pal(cid + PAL_SHADE_OFFSET, row >= 4 ? row + 1 : -1, v_s), v_tint);
		lamp = pal_fetch(cid, PAL_ROW_LAMP);
		if (!mass) {
			// 叶卡和底下的团块同一个法线、同一个材质，只靠一点明度起伏读出叶簇（max-011）
			float dl = (veg_hash(v_seed, 37u) - 0.5) * 2.0 * VEG_CARD_DL;
			lit = ok_adjust(lit, 1.0, 1.0, dl);
			shade = ok_adjust(shade, 1.0, 1.0, dl);
		}
	}
	ToonCol tcol = veg_colors(tctx, tmat, lit, shade, lamp, twig ? -1.0 : v_spare, trans_k);
	TOON_FRAGMENT_END(tcol)
	v_band.z += VEG_LIT_SHIFT;
}

void light() {
	TOON_LIGHT
}
