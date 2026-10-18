// veg_card.gdshader：整个树冠（团块与小球，叶片、花、枝条笔触的插片）（WP-23）
// 规格：WLD-VEGETATION；ENG-PERF；max-011 评审（统一树冠法线，团块与叶卡同一面）。
// 团块和叶卡是同一个表面、同一个材质 ID，描线只在树冠外轮廓出线；法线来自一个平滑外壳（生成时烘进 NORMAL），着色时再与所属团块
// 自己的法线各半混合（max-110 110c）：整冠一个光，每个团块各有受光面、半受光与影面。alpha scissor，留在不透明通道；双面。
// 插片朝向 = mix(外壳法线, 主相机视线, 0.5)，侧对镜头时缩小；只在外轮廓一圈保持全尺寸，朝向镜头的内部插片只留少数。
// 插片 3–5 张一簇（簇心压进团块），一簇同去同留、同明度；团块在外轮廓一圈按树种稳定的噪声剪成叶团锯齿边（max-011 评审③④）。
// 人能站到冠下的树另有一层冠底叶簇，只在近处出，从树下仰看时冠底读成松软的叶团（max-011 23c3）。
// 叶卡在主线版由本着色器按叶形层号程序化画出，细节版换成 Texture2DArray 的同号层。
// 远近池交接（23c2c）：近池 L1 与远池替身在 135–165 m 按 Bayer 抖动互补交接，林下地被在绘制距离前抖动淡出（植被公共段 veg_pool_code）；
// 远处的碎边、叶形和团块法线按像素收拢，远处的树读成干净的绿色体块。
// 顶点数据见 MotherModels：插片 CUSTOM0 = (插片中心, h2)，CUSTOM1 = (枝梢锚点, h3)，CUSTOM2 = (团心, 重要度序)；
// 团块 CUSTOM0 = (团心, 团块哈希)，CUSTOM1 = (枝梢锚点, 开花哈希)，CUSTOM2 = (所属小球的球心, −1)。
shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_disabled, diffuse_lambert, specular_disabled, ambient_light_disabled;

#include "res://style/shaders/toon.gdshaderinc"
#include "res://style/shaders/veg_common.gdshaderinc"

const float VEG_NEAR_COLLAPSE = 1.5;       // 1.5 m 内插片塌缩（§11.3）
const float VEG_LOD_L0 = 15.0;             // 近池三级的切换距离（12 m 树，ENG-PERF），乘 树高/12 × 细节系数
const float VEG_LOD_L1 = 40.0;
const float VEG_KEEP_L0 = 0.4;             // L0 留下的插片比例（MotherModels.CardCount 的前缀）
const float VEG_KEEP_L1 = 0.0556;          // L1 留下的插片比例
const float VEG_COVER_GROW = 1.5;          // 远处按像素外扩叶形的上限，保住树冠覆盖率（§11.1）
// 远处叶卡化简（23c2c）：一张插片在屏幕上只剩几个像素时，叶形的细节逐像素闪烁；按 UV 的像素导数从前者到后者，叶形换成一个圆润的叶团
// （覆盖率与叶簇相近）。枝条笔触、棕榈扇叶不换
const vec2 VEG_CARD_SIMPLE = vec2(0.1, 0.25);
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
// 远处碎边按像素收（23c2c）：噪声格在屏幕上小于几个像素时，逐像素的剪切会随镜头移动闪烁、出锯齿。按噪声格的像素宽，从前者到后者
// 把剪切量从 0 放到满（1080p、60° 视角下约 80 m 以内满剪，200 m 以外不剪），远处的树冠轮廓只剩团块本身的外形，读成干净的体块
const vec2 VEG_EDGE_PX = vec2(2.0, 5.0);
// 近处（L0+ 以内，max-011 23b 评审）：只剪最外面一圈轮廓带，带内不出孔。噪声沿"朝轮廓的方向"取（外壳法线去掉视线分量），
// 同一条朝轮廓的径向线上取值相同，剪的比例又朝轮廓单调变大，所以剪掉的部分总连着轮廓。
// 冠底（外壳法线朝下）从树下仰看时大片斜对镜头，rim 整片偏高，按 rim 定带会在冠底剪出大块。冠底改按屏幕上离轮廓的距离定带
// （rim 的一阶外推，折成该深度上的米）：斜对镜头的大片冠底 rim 虽高却变化慢，离轮廓远，不进带；强度不变（23c3）。
const float VEG_EDGE_NEAR = 0.35;          // 近处的碎边强度，叶卡已打碎轮廓
const float VEG_EDGE_NEAR_RIM0 = 0.72;     // 近处：rim 从这里起进入碎边带（只剩最外一圈）
const float VEG_EDGE_NEAR_RIM1 = 0.95;     // 近处：rim 到这里碎边最深
const float VEG_EDGE_NEAR_SPAN = 3.0;      // 缺口沿轮廓的疏密：按这个冠半径（m）铺噪声格
const vec2 VEG_EDGE_NEAR_DOWN = vec2(0.3, 0.7);     // 外壳法线朝下的分量从前者到后者，远近两种碎边带都换成冠底的碎边带
const float VEG_EDGE_NEAR_UNDER_W = 0.6;   // 冠底的碎边带宽（m，按上面的外推量；外推约是实际距离的两倍）
const float VEG_EDGE_FAR_UNDER_W = 1.2;    // 远处冠底的碎边带宽（m，同上），冠底以外照 23b 的远处碎边
// 团块法线（max-110 110c）：照参考小渔港树叶的法线（冠形椭球与所属团块自己的椭球各半），逐片元把外壳法线与团块自己的法线混合，
// 整冠仍是一个光，每个团块各有受光面与影面，树冠读成一团一团。团块自己的法线由团心（CUSTOM2.xyz，经 toon 的 v_center 传到片元，
// 树冠不用 toon 的解析法线）指向片元，底部压扁 0.7。外壳法线仍烘在 NORMAL 里，叶卡的去留、外轮廓碎边照旧按外壳法线算。
const float VEG_CLUMP_NORMAL = 0.75;      // 团块自己的法线所占的比例
// 远处团块法线减半（23c2c）：远处一株树只有十几个像素，每个团块各分明暗会碎成斑点；从前者（m）到后者，团块法线的比例乘到 VEG_CLUMP_FAR，
// 整株读成一个受光面加一个影面的绿色体块
const vec2 VEG_CLUMP_FADE = vec2(150.0, 400.0);
const float VEG_CLUMP_FAR = 0.45;
// 逆光一侧团块外缘的透光亮边（110c 第二轮，参考的边缘透光）：按团块自己的 rim 和视线朝太阳的程度加大透光，走 toon 的 BACKLIGHT，
// 所以只在背光面出现、被别的物体挡住时不透光
const float VEG_TRANS_RIM = 3.0;          // 外缘透光的加大倍数
// 逆光外缘亮边（110c 第三轮）：照参考的 ocTranslucent（back = (−V·L)^2.5，thru = 0.45 + 0.55·max(−N·L, 0)），只取团块外缘一圈，
// 不乘阴影：林荫里树冠都在别的树冠的投影里，走 BACKLIGHT 的透光读不出来（与 ART-STYLE"被挡住不透光"不一致，交主会话定）
const float VEG_RIM_GLOW = 0.45;          // 外缘亮边强度：影面往受光色补的比例，近处（23d：原为在受光色上整份叠加的 0.6）
const vec2 VEG_RIM_GLOW_AWAY = vec2(-0.25, 0.15);  // 片元法线·主光从前者到后者，亮边从满到 0：只出在背光的影面上（23d）
// 外缘亮边按到主相机的距离淡出（110c-4）：从上往下俯瞰时满树都是逆光外缘，远处整片发光；树下人视（60 m 内）不变
const float VEG_RIM_GLOW_FAR = 0.15;      // 远处的外缘亮边强度
const vec2 VEG_RIM_GLOW_FADE = vec2(60.0, 150.0);  // 从前者（m）到后者，强度从 VEG_RIM_GLOW 平滑降到 VEG_RIM_GLOW_FAR
const vec2 VEG_TRANS_RIM_BAND = vec2(0.55, 0.9);   // 团块 rim 从前者到后者，透光从不加到加满
// 团块外缘碎边（max-110 110c）：近处碎边另按每个团块自己的法线取 rim（团块各自的外轮廓一圈），团块外缘的折线轮廓也读成叶团锯齿边，
// 前面的团块咬进后面的团块，团与团之间分出层次；规则照 23c1 的近处碎边（强度 0.35、只剪最外一圈），远处同样强度
const float VEG_EDGE_CLUMP = 1.0;          // 团块外缘碎边的强度系数（乘在近处碎边上）
const vec2 VEG_EDGE_CLUMP_SHELL = vec2(0.5, 0.8);  // 外壳 rim 从前者到后者，团块外缘碎边从 0 到满：只在树冠外轮廓一带出（23d）
// 冠底叶卡（max-011 23c3）：人能站到冠下的树（冠底高于 MotherModels.UnderClearance）在最低的团块底面补一层叶簇。
// CUSTOM2.w = 1 + 序（画到 L0）或 2 + 序（只在 L0+，云团型）；不受外轮廓一圈的限制，从下面看保持全尺寸；
// 顶点沿主相机视线朝镜头拉近（屏幕上的位置不变，只改深度），盖住簇心压进去的那块团块底面。
const float VEG_UNDER_KEEP_L0 = 0.5;       // L0 留下的冠底叶簇比例（MotherModels.UnderKeepL0），L1 一张不留
const float VEG_UNDER_PULL = 1.3;          // 拉近的距离，按插片半径（簇心压进团块至多约 0.75 个半径，加团块面的下陷）
const float VEG_UNDER_DL = 0.035;          // 冠底叶簇的明度起伏（OKLab L，±，一簇一个值）：仰看时一簇簇分得开，又不花
const float VEG_UNDER_FIT = 1.3;           // 冠底叶卡正对镜头：卡片放大这么多（MotherModels.UnderCardScale），叶形按叶柄端缩回原大，不让卡片的直边切出叶形

// toon.gdshaderinc 已占 25 个 varying 分量，本着色器只剩 7 个（已用满，交接编码并进 v_row）：层号与标志合进一个整数，t0 直接读 v_spare（同是 COLOR.a）。
varying flat int v_bits;      // 位 0–3 叶卡层号，位 4 开花，位 5 枝条笔触，位 6 团块（不裁切），位 7 冠底叶卡，位 8–15 插片自己的叶形种子
varying flat int v_row;       // 位 0–5 树种号（−1 记成 63），位 6 起是远近池交接的编码 × 1000（veg_pool_code，23c2c）
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

// 冠底叶卡的去留（23c3）：只在近处出。L0 只留前一半叶簇，换到 L1 之前全部缩到 0；只在 L0+ 画的（云团型）换到 L0 之前缩到 0
float veg_under_keep(float w, float dist, float height) {
	float k = max(height, 1.0) / 12.0 * max(quality.x, 0.5);
	float l0 = VEG_LOD_L0 * k;
	float l1 = VEG_LOD_L1 * k;
	float keep = mix(1.0, w >= 2.0 ? 0.0 : VEG_UNDER_KEEP_L0, smoothstep(l0 * 0.9, l0, dist));
	keep = mix(keep, 0.0, smoothstep(l1 * 0.9, l1, dist));
	return 1.0 - smoothstep(keep - 0.02, keep, fract(w));
}

void vertex() {
	VegInst inst = veg_instance(INSTANCE_CUSTOM);
	VegSpecies sp = veg_species(inst.row);
	vec3 origin = MODEL_MATRIX[3].xyz;
	mat3 inv3 = inverse(mat3(MODEL_MATRIX));
	vec3 dir_m = normalize(inv3 * vec3(wind.x, 0.0, wind.y) + vec3(1e-5, 0.0, 0.0));
	v_tint = veg_tint_noise(origin);
	// 远池替身的团块 CUSTOM2.w = −2（MotherModels.FarStandIn）
	float fade = veg_pool_code(MAIN_CAM_INV_VIEW_MATRIX[3].xyz, origin, inst, CUSTOM2.w < -1.5, IN_SHADOW_PASS);
	v_row = (inst.row & 63) | (int(round(fade * 1000.0)) << 6);
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
		bool under = CUSTOM2.w >= 1.0;
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
		v_bits = (layer & 15) | ((bloom && !twig) ? 16 : 0) | (twig ? 32 : 0) | (under ? 128 : 0) | (shape << 8);
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
		size *= under ? veg_under_keep(CUSTOM2.w, dist, inst.height) : veg_card_keep(CUSTOM2.w, dist, inst.height);
		size *= smoothstep(VEG_NEAR_COLLAPSE * 0.6, VEG_NEAR_COLLAPSE, dist);
		// 叶卡集中在外轮廓一圈，打碎轮廓边；朝向镜头的内部插片只留少数、缩小（max-011）。
		// 冬季的枝条笔触不受此限（按叶量过渡），棕榈的扇叶本身就是树冠，也不受此限；冠底叶卡要在仰看时正对镜头，也不受此限。
		if (sp.leaf_card != VEG_CARD_FROND && !under) {
			float rim = 1.0 - abs(dot(nb, view_m));
			float inner = veg_hash(h2, 31u) < VEG_INNER_KEEP ? VEG_INNER_SIZE : 0.0;
			size *= mix(1.0, mix(inner, 1.0, smoothstep(VEG_RIM_IN, VEG_RIM_OUT, rim)), leafy);
		}
		VERTEX = center + (uw.x * t1 + uw.y * b1) * size;
		// 插片用外壳法线（max-011 的统一树冠法线；棕榈扇叶用自己的法线）
		NORMAL = nb;
		VERTEX = veg_wind(VERTEX, origin, dir_m, inst, sp, h2, 1.0, 1.0);
		if (under) {
			// 沿主相机视线拉近，屏幕上的位置不变；冬季换成枝条笔触时不拉
			vec3 to_cam = inv3 * (cam_w - origin) - VERTEX;
			float len = length(to_cam);
			VERTEX += to_cam / max(len, 1e-3) * min(VEG_UNDER_PULL * length(off) * leafy, 0.5 * len);
		}
	}
	TOON_VERTEX
	// 树冠不用 toon 的解析法线：v_center 改传所属团块的团心（视空间），片元里算团块法线
	v_center = (MODELVIEW_MATRIX * vec4(CUSTOM2.xyz, 1.0)).xyz;
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
	if (veg_pool_discard(FRAGCOORD.xy, float(v_row >> 6) / 1000.0)) {
		discard;
	}
	// 背面也用外壳法线（引擎给背面翻过一次法线，这里翻回来）
	if (!FRONT_FACING) {
		NORMAL = -NORMAL;
	}
	int row = v_row & 63;
	VegSpecies sp = veg_species(row);
	bool mass = (v_bits & 64) != 0;
	// 团块自己的法线（视空间）：团心指向片元，底部按世界的 y 压扁 0.7（树只绕竖轴转，模型的 y 就是世界的 y）
	vec3 clump_w = mat3(INV_VIEW_MATRIX) * (VERTEX - v_center);
	clump_w.y /= clump_w.y < 0.0 ? 0.49 : 1.0;
	vec3 clump = normalize(mat3(VIEW_MATRIX) * clump_w + vec3(0.0, 1e-5, 0.0));
	if (mass) {
		// 外轮廓碎边（max-011 评审③）：外壳法线侧对镜头的一圈按噪声剪掉，轮廓读成叶团的锯齿边。本体不投影（投影走代理），
		// 所以这里的距离就是主相机到片元的距离（视空间长度），不会在阴影通道里按灯算。
		vec3 n = normalize(NORMAL);
		float facing = dot(n, VIEW);
		float rim = 1.0 - abs(facing);
		// 片元里没有树高，按 12 m 的树取 L0 的切换距离（ENG-PERF），乘细节系数
		float l0 = VEG_LOD_L0 * max(quality.x, 0.5);
		float far_k = smoothstep(l0 * 0.6, l0, length(VERTEX));
		// 冠底（外壳法线朝下）按屏幕上离轮廓的距离定带（23c3），远近两种碎边都一样，仰看时不在冠底剪出大块
		float down = smoothstep(VEG_EDGE_NEAR_DOWN.x, VEG_EDGE_NEAR_DOWN.y, -(mat3(INV_VIEW_MATRIX) * n).y);
		float px_m = -VERTEX.z * 2.0 / (PROJECTION_MATRIX[1][1] * VIEWPORT_SIZE.y);
		float to_edge = (1.0 - rim) / max(fwidth(rim), 1e-5) * px_m;
		float under_rim = smoothstep(0.45, 0.65, rim);
		// 远处：按世界坐标噪声在 rim 带里剪（23b 定稿）
		float band_far = mix(smoothstep(VEG_EDGE_RIM0, VEG_EDGE_RIM1, rim), (1.0 - smoothstep(0.0, VEG_EDGE_FAR_UNDER_W, to_edge)) * under_rim, down);
		// 团块外缘在远处也剪最外一圈，强度照近处（0.35），不在冠内剪出大洞
		// 只在树冠外轮廓一带剪（23d）：冠内团块的外缘不剪，前面的团块后面没有别的团块时，不在冠内开出带描线的透镜形大洞
		float clump_band = VEG_EDGE_CLUMP * smoothstep(VEG_EDGE_NEAR_RIM0, VEG_EDGE_NEAR_RIM1, 1.0 - abs(dot(clump, VIEW)))
				* smoothstep(VEG_EDGE_CLUMP_SHELL.x, VEG_EDGE_CLUMP_SHELL.y, rim);
		band_far = max(band_far, clump_band * VEG_EDGE_NEAR / VEG_EDGE_FAR);
		float px_k = smoothstep(VEG_EDGE_PX.x, VEG_EDGE_PX.y, VEG_EDGE_CELL / max(px_m, 1e-4));
		float cut = VEG_EDGE_DEPTH * VEG_EDGE_FAR * band_far * px_k;
		// 不剪时噪声整体抬过阈值，不留零星的孔
		float e_far = veg_edge_noise((INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz, row, sp.leaf_card) - cut + 0.5 * (1.0 - px_k);
		// 近处：缺口噪声取朝轮廓的方向，按树错开（v_tint 是每株一个值）
		float band_near = mix(smoothstep(VEG_EDGE_NEAR_RIM0, VEG_EDGE_NEAR_RIM1, rim), (1.0 - smoothstep(0.0, VEG_EDGE_NEAR_UNDER_W, to_edge)) * under_rim, down);
		band_near = max(band_near, clump_band);
		float cut_near = VEG_EDGE_DEPTH * VEG_EDGE_NEAR * band_near;
		vec3 toward = mat3(INV_VIEW_MATRIX) * normalize(n - facing * VIEW + vec3(0.0, 1e-5, 0.0));
		float e_near = veg_edge_noise(toward * VEG_EDGE_NEAR_SPAN + vec3(53.1 * v_tint.x, 41.7 * v_tint.y, 29.3 * (v_tint.x + v_tint.y)), row, sp.leaf_card) - cut_near;
		float e = mix(e_near, e_far, far_k);
		ALPHA = clamp(e / max(fwidth(e), 1e-4) + 0.5, 0.0, 1.0);
	} else {
		// v_bits 是 flat 的，同一个图元内分支一致，fwidth 不受影响
		vec2 uv = (v_bits & 128) != 0 ? vec2(0.5, 0.0) + (UV - vec2(0.5, 0.0)) * VEG_UNDER_FIT : UV;
		int layer = v_bits & 15;
		float d = veg_card_sdf(layer, uv, float((v_bits >> 8) & 255) / 256.0);
		if (layer != VEG_CARD_TWIG && layer != VEG_CARD_FROND) {
			float uv_px = max(fwidth(uv.x), fwidth(uv.y));
			float blob = (length((uv - vec2(0.5, 0.5)) / vec2(0.42, 0.46)) - 1.0) * 0.42;
			d = mix(d, blob, smoothstep(VEG_CARD_SIMPLE.x, VEG_CARD_SIMPLE.y, uv_px));
		}
		float fw = max(fwidth(d), 1e-4);
		// 远处按像素外扩，叶形不至于在远距离上变稀（主线版代替保覆盖率的 mip，§11.1）
		d -= fw * (0.35 + min(fw * 30.0, VEG_COVER_GROW));
		ALPHA = clamp(0.5 - d / fw, 0.0, 1.0);
	}
	ALPHA_SCISSOR_THRESHOLD = 0.5;
	// 着色用团块法线：外壳法线与团块自己的法线混合（棕榈扇叶用自己的法线）
	if (sp.leaf_card != VEG_CARD_FROND) {
		float clump_k = VEG_CLUMP_NORMAL * mix(1.0, VEG_CLUMP_FAR, smoothstep(VEG_CLUMP_FADE.x, VEG_CLUMP_FADE.y, length(VERTEX)));
		NORMAL = normalize(mix(normalize(NORMAL), clump, clump_k));
	}
	TOON_FRAGMENT_BEGIN
	vec3 lit;
	vec3 shade;
	vec3 lamp;
	float back_view = smoothstep(0.0, 0.6, dot(-VIEW, normalize(mat3(VIEW_MATRIX) * key_dir)));
	float trans_k = 1.0 + VEG_TRANS_RIM * smoothstep(VEG_TRANS_RIM_BAND.x, VEG_TRANS_RIM_BAND.y, 1.0 - abs(dot(clump, VIEW))) * back_view;
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
			// 叶卡和底下的团块同一个法线（团块法线）、同一个材质，只靠一点明度起伏读出叶簇（max-011）
			float dl = (veg_hash(v_seed, 37u) - 0.5) * 2.0 * ((v_bits & 128) != 0 ? VEG_UNDER_DL : VEG_CARD_DL);
			lit = ok_adjust(lit, 1.0, 1.0, dl);
			shade = ok_adjust(shade, 1.0, 1.0, dl);
		}
	}
	ToonCol tcol = veg_colors(tctx, tmat, lit, shade, lamp, twig ? -1.0 : v_spare, trans_k);
	if (!twig) {
		vec3 lv = normalize(mat3(VIEW_MATRIX) * key_dir);
		float glow = pow(clamp(dot(-VIEW, lv), 0.0, 1.0), 2.5) * (0.45 + 0.55 * clamp(-dot(clump, lv), 0.0, 1.0))
				* smoothstep(VEG_TRANS_RIM_BAND.x, VEG_TRANS_RIM_BAND.y, 1.0 - abs(dot(clump, VIEW))) * key_strength * smoothstep(-0.02, 0.05, sun_dir.y);
		// 本体不投影（投影走代理），片元的视空间长度就是到主相机的距离（与外轮廓碎边同理）；MAIN_CAM_INV_VIEW_MATRIX 只在 vertex() 里有
		float glow_dist = length(VERTEX);
		// 只在背光的影面上把影色往受光色补（23d），不在受光色上再叠一份：逆光外缘读成亮边，受光面不冲白，团块明暗留住
		float away = 1.0 - smoothstep(VEG_RIM_GLOW_AWAY.x, VEG_RIM_GLOW_AWAY.y, dot(NORMAL, lv));
		tcol.extra += max(tcol.lit - tcol.shade, vec3(0.0)) * mix(VEG_RIM_GLOW, VEG_RIM_GLOW_FAR, smoothstep(VEG_RIM_GLOW_FADE.x, VEG_RIM_GLOW_FADE.y, glow_dist)) * glow * away;
	}
	TOON_FRAGMENT_END(tcol)
	v_band.z += VEG_LIT_SHIFT;
}

void light() {
	TOON_LIGHT
}
