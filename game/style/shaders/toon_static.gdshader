// toon_static.gdshader：建筑、地面、道具的块几何、独立实例和重复部件的 MultiMesh；外围 O0–O2 也用它（ART-STYLE）
// 规格：ENG-PERF；ART-STYLE。材质只经材质库按 ToonSpec 取得，逐物体的颜色走顶点色号。
shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert, specular_disabled, ambient_light_disabled;

#include "res://style/shaders/toon.gdshaderinc"
#include "res://style/shaders/reflection_lamps.gdshaderinc"

// ---------------------------------------------------------------------------
// 室外构造图案线（ART-STYLE，DECISION-195；ToonSpec.SurfacePattern，材质库按材质类写下面三个 uniform）
// 几何给连续的米 UV（铺向与相位在 UV 里）和 UV2 = (gate, K)：gate 0 的三角形不画（K4、K5 或非目标面）。
// 只改明度、不改色相：受光色、影色、灯照色同加 ΔL（OKLab L），影仍以明度为主；不画污渍、裂缝、做旧（DECISION-174）。
// 远处按屏幕上的格子大小淡出：缝线先按像素足迹做盒式滤波（滤波本身就收敛到面积平均），格子小于 PAT_FADE_PX
// 时再整体过渡到"砖面与缝按面积加权"的平均明度，不出摩尔纹、不闪。层号登记在 SurfacePatternLayers.cs。
// ---------------------------------------------------------------------------
uniform int pattern_layer = 0;               // 0 = 无图样；1 = 直缝方砖（StackBond）；2 = 错缝（RunningBond，V 向成行、竖缝隔行错半格）
uniform vec2 pattern_period = vec2(1.0);     // 含缝的 U、V 模数（m）
uniform vec2 pattern_joint = vec2(0.0);      // 缝带总宽（m），居中在格界上

const float PAT_JOINT_DL = -0.04;            // 缝线的明度差（OKLab L，样板初值，主会话定值）
const float PAT_MOTTLE_DL = 0.03;            // 弱斑驳的幅度上限（±L，ART-STYLE）
const float PAT_MOTTLE_SCALE = 4.0;          // 斑驳的基本尺度（m，§6.2 的 2–8 m 里取中）
const vec2 PAT_FADE_PX = vec2(4.0, 8.0);     // 格子在屏幕上的边长（px）从 .y 到 .x：缝线过渡到面积平均（掠射中景不出断续短划）
const float PAT_MIN_LINE_PX = 0.75;          // 缝线在屏幕上的最小宽度（px）：中景仍读得出格子，过了 PAT_FADE_PX 再回到真实面积平均
const vec2 PAT_MOTTLE_FADE_PX = vec2(6.0, 16.0);   // 斑驳更早淡出：中远景读作干净的平涂

// 宽 j（以周期计）、居中在整数处的脉冲串，在宽 w（以周期计）的盒式足迹里的平均覆盖率
float pat_line(float x, float j, float w) {
	float t = x + 0.5 * j;
	if (w < 1e-4) {
		return fract(t) < j ? 1.0 : 0.0;
	}
	float a = t - 0.5 * w;
	float b = t + 0.5 * w;
	float ia = floor(a) * j + min(fract(a), j);
	float ib = floor(b) * j + min(fract(b), j);
	return clamp((ib - ia) / w, 0.0, 1.0);
}

// 三台扶梯共用：米 UV 的 +V 朝上；0.8 s 一节，480 s 动画回卷时相位连续。
// 只把既有金属灰压出踏级分界，不动几何、碰撞、法线或材质 ID。
const float ESCALATOR_SPEED = 0.5;          // m/s，SYS-MOTION-ENV
const float ESCALATOR_PITCH = 0.4;          // m，SYS-MOTION-ENV

float escalator_dl(float v, float fw) {
	float w = max(fw / ESCALATOR_PITCH, 1e-6);
	float j = min(PAT_MIN_LINE_PX * w, 0.5);
	float cover = pat_line((v - anim_time * ESCALATOR_SPEED) / ESCALATOR_PITCH, j, w);
	return PAT_JOINT_DL * cover * smoothstep(PAT_FADE_PX.x, PAT_FADE_PX.y, 1.0 / w);
}

// 错缝的竖缝：偶数行缝在整数处，奇数行错半格；足迹跨行时按落在奇数行的份额混合两种相位（仍是盒式滤波）
float pat_bond_line(vec2 g, float j, vec2 w) {
	float even = pat_line(g.x, j, w.x);
	float odd = pat_line(g.x + 0.5, j, w.x);
	float in_odd = pat_line(0.5 * g.y - 0.75, 0.5, 0.5 * w.y);
	return mix(even, odd, in_odd);
}

float pat_hash(ivec2 i) {
	return lamp_hash01(lamp_hash_u(uint(i.x)) ^ (uint(i.y) * 2654435769u));
}

// 世界空间里各向同性的低频值噪声，−1…1
float pat_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 s = f * f * (3.0 - 2.0 * f);
	ivec2 c = ivec2(i);
	float n00 = pat_hash(c);
	float n10 = pat_hash(c + ivec2(1, 0));
	float n01 = pat_hash(c + ivec2(0, 1));
	float n11 = pat_hash(c + ivec2(1, 1));
	return mix(mix(n00, n10, s.x), mix(n01, n11, s.x), s.y) * 2.0 - 1.0;
}

// 图案的明度差。uv、fw（米 UV 及其 fwidth）在统一控制流里取好；gate 0 时为 0
float pat_dl(vec2 uv, vec2 fw, vec2 gate_k, vec3 wpos, vec3 n_w) {
	if (pattern_layer != 1 && pattern_layer != 2) {
		return 0.0;
	}
	vec2 period = max(pattern_period, vec2(1e-3));
	vec2 j = clamp(pattern_joint / period, vec2(0.0), vec2(0.999));
	vec2 g = uv / period;
	vec2 w = fw / period;
	float cell_px = min(period.x / max(fw.x, 1e-6), period.y / max(fw.y, 1e-6));
	vec2 jl = min(max(j, PAT_MIN_LINE_PX * w), vec2(0.5));
	float cu = pattern_layer == 2 ? pat_bond_line(g, jl.x, w) : pat_line(g.x, jl.x, w.x);
	float cv = pat_line(g.y, jl.y, w.y);
	float cover = 1.0 - (1.0 - cu) * (1.0 - cv);
	float avg = 1.0 - (1.0 - j.x) * (1.0 - j.y);
	cover = mix(avg, cover, smoothstep(PAT_FADE_PX.x, PAT_FADE_PX.y, cell_px));
	// 斑驳在世界空间里各向同性：地面取 xz，墙面取（沿墙水平, 高度），不随 UV 的分段铺向跳变
	vec2 along = normalize(vec2(-n_w.z, n_w.x) + vec2(1e-5, 0.0));
	vec2 q = (abs(n_w.y) > 0.7 ? wpos.xz : vec2(dot(wpos.xz, along), wpos.y)) / PAT_MOTTLE_SCALE;
	float mottle = (0.65 * pat_noise(q) + 0.35 * pat_noise(q * 2.03 + vec2(17.3, 5.1))) * PAT_MOTTLE_DL;
	mottle *= smoothstep(PAT_MOTTLE_FADE_PX.x, PAT_MOTTLE_FADE_PX.y, cell_px);
	float on = gate_k.x > 0.5 ? 1.0 : 0.0;
	return on * (PAT_JOINT_DL * cover + mottle);
}

// toon_colors 的同一套取色，受光、影、灯照色先加图案的 ΔL，再走 toon_colors_from 的全部处理（湿润、积水、光斑……）
ToonCol pat_colors(ToonCtx c, ToonMat m, float dl) {
	if (toon_is_const(c.cid) || (c.flags & TOON_F_INTERIOR) != 0) {
		return toon_colors(c, m);
	}
	vec3 lit = ok_adjust(pal_fetch(c.cid, PAL_ROW_CUR), 1.0, 1.0, dl);
	vec3 shade = ok_adjust(pal_fetch(c.cid + PAL_SHADE_OFFSET, PAL_ROW_CUR), 1.0, 1.0, dl);
	vec3 lamp = ok_adjust(pal_fetch(c.cid, PAL_ROW_LAMP), 1.0, 1.0, dl);
	return toon_colors_from(c, m, lit, shade, lamp);
}

// 地面覆盖只读同图的潜势与 game/systems/vegetation/seasons/ 的季节量；不切材质、不随换季重建几何。
// 色号来自既有调色板；图样复用湿面的低频潜势，不另加一套噪声频率。
const int GROUND_SNOW = 64;
const int GROUND_GINKGO = 118;
const int GROUND_PETAL = 158;
const int GROUND_DECIDUOUS = 130;
const int GROUND_MAPLE = 124;
// 近看的单叶轮廓只在着色器里表现；中央参数留给主会话看图取值。
const float GROUND_LEAF_CELL = 0.12;
const vec2 GROUND_LEAF_RESOLVE_PX = vec2(4.0, 12.0);

bool ground_cover_surface(ToonCtx c) {
	bool ground = c.cid == 150 || c.cid == 88 || (c.cid >= 178 && c.cid <= 199);
	return ground && (c.flags & TOON_F_NOSHADOW) != 0 && (c.flags & TOON_F_INTERIOR) == 0 && c.n_w.y > 0.0 && in_map(c.wpos);
}

float ground_cover_edge(float amount, float rank, float aa) {
	if (amount <= 0.0) {
		return 0.0;
	}
	if (amount >= 1.0) {
		return 1.0;
	}
	return smoothstep(rank - aa, rank + aa, amount);
}

// 四点线性映射；mask B 的草坪/步道/车道为 1/.5/.2，檐下为零。
float ground_snow_coverage(float potential, float normalized_amount) {
	float amount = clamp(normalized_amount, 0.0, 1.0) * 1.5;
	float at07 = 0.7 * potential;
	float at1 = min(1.0, 1.2 * potential);
	float at15 = min(1.0, 2.0 * potential);
	if (amount <= 0.7) {
		return at07 * amount / 0.7;
	}
	if (amount <= 1.0) {
		return mix(at07, at1, (amount - 0.7) / 0.3);
	}
	return mix(at1, at15, (amount - 1.0) / 0.5);
}

// 规则格只负责单叶形状；旋转和阈值固定在世界空间，镜头移动与换季不重排。
// 远处收回面积覆盖，避免叶形成为闪烁的细碎点。
float ground_leaf_rank(vec2 xz, bool fan, bool petal, float broad_rank) {
	vec2 grid = xz / GROUND_LEAF_CELL;
	ivec2 cell = ivec2(floor(grid));
	float h = pat_hash(cell);
	float angle = 6.2831853 * h;
	vec2 p = fract(grid) - vec2(0.5);
	p = mat2(vec2(cos(angle), sin(angle)), vec2(-sin(angle), cos(angle))) * p;
	float shape;
	if (fan) {
		vec2 q = p + vec2(0.0, 0.28);
		shape = max(length(q) / 0.72, abs(q.x) / max(q.y, 0.02));
	} else {
		shape = abs(p.x) / (petal ? 0.27 : 0.30) + p.y * p.y / 0.20;
	}
	float single = mix(h * 0.55, 0.55 + h * 0.45, smoothstep(0.85, 1.0, shape));
	float leaf_px = 1.0 / max(length(fwidth(grid)), 1e-5);
	return mix(broad_rank, single, smoothstep(GROUND_LEAF_RESOLVE_PX.x, GROUND_LEAF_RESOLVE_PX.y, leaf_px));
}

ToonCol ground_cover_colors(ToonCtx c, ToonMat m, ToonCol base, float rank, float rank_aa, vec2 leaf_rank, vec2 leaf_aa, float leaf_detail, vec3 wet_reflection) {
	vec4 mask = texture(ground_mask, map_uv(c.wpos));
	vec4 shelter = texture(ground_mask2, map_uv(c.wpos));
	// 银杏叶在其他落叶/花瓣上，雪在最上；雪下仍能露出金叶。树冠投影下的雪潜势乘 0.6。
	float gold_leaf = ground_cover_edge(mask.r * ground_amt.x, leaf_rank.y, leaf_aa.y);
	float snow_coverage = ground_snow_coverage(mask.b, ground_amt.z) * mix(1.0, 0.6, shelter.b) * shelter.g;
	float snow_cover = ground_cover_edge(snow_coverage * clamp(c.n_w.y, 0.0, 1.0), rank, rank_aa);
	// 春季花瓣只在春季一侧出现，换色按阈值切换，避免粉色与秋叶混色。
	float spring = 1.0 - step(0.5, min(mod(season.x, 4.0), 4.0 - mod(season.x, 4.0)));
	float mixed_leaf = ground_cover_edge(mask.g * ground_amt.y, leaf_rank.x, leaf_aa.x);
	// A 是 G 中红枫/鸡爪槭的潜势，按实际比重作阈值切色，春季统一为花瓣。
	float maple_share = mask.g > 0.0 ? clamp(mask.a / mask.g, 0.0, 1.0) : 0.0;
	int mixed_leaf_cid = spring > 0.5 ? GROUND_PETAL : (maple_share > 1.0 - rank ? GROUND_MAPLE : GROUND_DECIDUOUS);
	ToonCol result = base;
	// 两层分别叠放，银杏边缘的抗锯齿不会吞掉下面的其他落叶。
	for (int layer = 0; layer < 2; layer++) {
		int cid = layer == 1 ? GROUND_GINKGO : mixed_leaf_cid;
		float cover = layer == 1 ? gold_leaf : mixed_leaf;
		if (cover <= 0.0) {
			continue;
		}
		ToonCtx leaf_ctx = c;
		leaf_ctx.cid = cid;
		ToonMat leaf_mat = m;
		// 湿叶沿用铺装响应，低于沥青；不把草坪材质的湿润值带到叶毯上。
		leaf_mat.wet = 0.6;
		// 满铺时仍能辨认单叶；明度差与铺装缝线共用同一可调参数，远处归回平涂。
		float detail = step(0.55, layer == 1 ? leaf_rank.y : leaf_rank.x) * leaf_detail * PAT_JOINT_DL;
		ToonCol leaf = pat_colors(leaf_ctx, leaf_mat, detail);
		leaf.lit += wet_reflection * leaf_mat.wet;
		leaf.shade += wet_reflection * leaf_mat.wet;
		leaf.lamp += wet_reflection * leaf_mat.wet;
		result.lit = mix(result.lit, leaf.lit, cover);
		result.shade = mix(result.shade, leaf.shade, cover);
		result.lamp = mix(result.lamp, leaf.lamp, cover);
	}
	if (snow_cover > 0.0) {
		ToonCtx snow_ctx = c;
		snow_ctx.cid = GROUND_SNOW;
		ToonMat snow_mat = m;
		snow_mat.wet = 0.0;
		ToonCol snow_color = toon_colors(snow_ctx, snow_mat);
		result.lit = mix(result.lit, snow_color.lit, snow_cover);
		result.shade = mix(result.shade, snow_color.shade, snow_cover);
		result.lamp = mix(result.lamp, snow_color.lamp, snow_cover);
	}
	return result;
}

void vertex() {
	TOON_VERTEX
}

void fragment() {
	TOON_FRAGMENT_BEGIN
	vec2 pat_fw = fwidth(UV);
	ToonCol tcol;
	if (tmat.escalator) {
		tcol = pat_colors(tctx, tmat, escalator_dl(UV.y, pat_fw.y));
	} else if (pattern_layer == 0) {
		tcol = toon_colors(tctx, tmat);
	} else {
		tcol = pat_colors(tctx, tmat, pat_dl(UV, pat_fw, UV2, tctx.wpos, tctx.n_w));
	}
	// 与积水/湿面天光同一材质资格门；先筛底面，覆盖其上的落叶也不能把软地面重新点亮。
	vec3 wet_reflection = vec3(0.0);
	if (weather_b.x > 0.0) {
		float wet_gate = clamp(weather_b.x, 0.0, 1.0) * toon_wet_exposure(tctx) * smoothstep(PW_FLAT.x, PW_FLAT.y, tctx.n_w.y)
			* smoothstep(PW_RESPONSE.x, PW_RESPONSE.y, tmat.wet);
		wet_reflection = reflection_lamps_color(tctx.wpos, tctx.cam, 0.5) * wet_gate;
	}
	tcol.lit += wet_reflection * tmat.wet;
	tcol.shade += wet_reflection * tmat.wet;
	tcol.lamp += wet_reflection * tmat.wet;
	if (ground_cover_surface(tctx)) {
		float cover_rank = pw_value_noise(tctx.wpos.xz / PW_SCALE_A);
		float cover_aa = max(fwidth(cover_rank), 1e-5);
		float season_pos = mod(season.x, 4.0);
		bool petals = min(season_pos, 4.0 - season_pos) < 0.5;
		float leaf_rank = ground_leaf_rank(tctx.wpos.xz, false, petals, cover_rank);
		float gold_leaf_rank = ground_leaf_rank(tctx.wpos.xz, true, false, 1.0 - cover_rank);
		vec2 ranks = vec2(leaf_rank, gold_leaf_rank);
		vec2 leaf_aa = max(fwidth(ranks), vec2(1e-5));
		float leaf_px = GROUND_LEAF_CELL / max(length(fwidth(tctx.wpos.xz)), 1e-5);
		float detail = smoothstep(GROUND_LEAF_RESOLVE_PX.x, GROUND_LEAF_RESOLVE_PX.y, leaf_px);
		tcol = ground_cover_colors(tctx, tmat, tcol, cover_rank, cover_aa, ranks, leaf_aa, detail, wet_reflection);
	}
	TOON_FRAGMENT_END(tcol)
}

void light() {
	TOON_LIGHT
}
