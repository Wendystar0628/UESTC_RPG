// toon_static.gdshader：建筑、地面、道具的块几何、独立实例和重复部件的 MultiMesh；外围 O0–O2 也用它（WP-04）
// 规格：ENG-PERF；ART-STYLE。材质只经材质库按 ToonSpec 取得，逐物体的颜色走顶点色号。
shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert, specular_disabled, ambient_light_disabled;

#include "res://style/shaders/toon.gdshaderinc"

// ---------------------------------------------------------------------------
// 室外构造图案线（ART-STYLE，D-195；ToonSpec.SurfacePattern，材质库按材质类写下面三个 uniform）
// 几何给连续的米 UV（铺向与相位在 UV 里）和 UV2 = (gate, K)：gate 0 的三角形不画（K4、K5 或非目标面）。
// 只改明度、不改色相：受光色、影色、灯照色同加 ΔL（OKLab L），影仍以明度为主；不画污渍、裂缝、做旧（D-174）。
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

void vertex() {
	TOON_VERTEX
}

void fragment() {
	TOON_FRAGMENT_BEGIN
	vec2 pat_fw = fwidth(UV);
	ToonCol tcol;
	if (pattern_layer == 0) {
		tcol = toon_colors(tctx, tmat);
	} else {
		tcol = pat_colors(tctx, tmat, pat_dl(UV, pat_fw, UV2, tctx.wpos, tctx.n_w));
	}
	TOON_FRAGMENT_END(tcol)
}

void light() {
	TOON_LIGHT
}
