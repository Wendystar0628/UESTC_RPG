// toon.gdshaderinc：全校共用的卡通材质核心（WP-04）
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

// 灯光通道、窗灯、灯具亮灭与路灯光斑的公共求值（lamp_channel、lamp_window_*、lamp_fixture_level、lamp_pool_shade；WP-39），fx_glow、降水与倒影代理共用同一份
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
// 顶点数据（ENG-PERF）：COLOR RGBA8 = 色号、AO、标志、备用字节
// 标志字节的位（WP-04 登记，暂定）：
// ---------------------------------------------------------------------------
const int TOON_F_INTERIOR = 1;   // 室内：影取行 2、受光取行 3 并乘光斑增益
const int TOON_F_OUTSKIRT = 2;   // 外围：窗跳过 window_override；季节覆盖潜势读顶点而不读全图纹理
const int TOON_F_DECAL = 4;      // 贴面贴花：备用字节是底面色号；不投影
const int TOON_F_NOSHADOW = 8;   // 小件不投影：阴影通道里塌成一点（ART-MODELING）
// 高两位是色号的 bank（contract-28，PaletteEncoding.EncodeColor）：bit 4 主色色号 +256，bit 5 贴花底色色号 +256。
// 低四位的语义不变；旧几何（palette encoding 1）这两位恒为 0，解出来的色号与原来逐个相同。
const int TOON_F_COLOR_BANK = 16;
const int TOON_F_DECAL_BANK = 32;

// 明暗分阶（ART-STYLE）
const int TOON_STEPS_TWO = 0;        // 建筑、地面、硬质大件
const int TOON_STEPS_TWO_HALF = 1;   // 曲面道具、金属件
const int TOON_STEPS_SOFT = 2;       // 软物（两阶半，带宽 ×2.2）
const int TOON_STEPS_THREE = 3;      // 植被团块
const float TOON_THR = -0.03;        // 影 / 受光的阈值（受光多吃一点）
const float TOON_THR_HALF = 0.30;    // 两阶半的半受光阈值
const float TOON_THR_DEEP = -0.25;   // 植被深影阈值
const float TOON_BAND_OVERCAST = 0.06;   // 阴雨天按天光方向时的过渡带
const float TOON_AO_PUSH = 0.3;      // 遮蔽推阈值的上限
const float TOON_GRAZE = 0.06;       // 掠射面判进影里：受光再乘 smoothstep(0, 0.06, N·L)（ENG-PERF）
const vec2 TOON_SHADOW_THR = vec2(0.4, 0.6);     // 阴影图衰减的阈值化：窄而干脆；影边的颗粒来自 PCSS 采样噪声，不靠放宽阈值掩盖
const float TOON_TRANS_K = 0.25;     // 透光整体系数（ART-STYLE，P0 看图定）
const float TOON_WET_DARKEN = 0.28;  // 湿润变暗：受光色和影色 × (1 − 0.28·wet)
const float TOON_SKY_GRAD = 0.06;    // 影里随法线 y 的天光渐变（约 ±0.03 L）
const float TOON_BOUNCE = 0.15;      // 朝下面的地面暖反光

// ---------------------------------------------------------------------------
// 质感移植（max-110 110a，样板取值，交主会话定值）。参照"午后打烊的花房咖啡馆"的 RE_Direct_Toon、
// RE_IndirectDiffuse_Toon 与 lights_fragment_end 补丁（暖边、硬高光、半球天光），"小渔港""街角"的 OC_RIM 同式。
// 全部是风格底线：只多几次 OKLab 换算，没有贴图采样，不加 varying（高光与云影合用一个槽）；室内、夜间、阴雨按门控退回原样。
// ---------------------------------------------------------------------------
// 明暗层次：影的明度差 ΔL = 受光 L − 影 L 按材质类缩放。整体的压深在 WP-37 关键帧的影明度比 r 里（max-110 110d：
// 参考里影 = 固有色 × 半球天光，折成 OKLab 明度比约 0.70，默认格 r 取 0.72），这里只留按材质的相对分级：
// 分量依次是两阶（建筑、地面）、两阶半（曲面道具、金属）、软物、植被三阶；1 = 照调色板。软物比建筑浅；
// 植被三阶的影深浅由调色板的 vegDepth 定（树冠由 veg_* 自己拼色，同样照调色板）。白天、晴天满量，夜间与阴天退回 1。
const vec4 TOON_PORT_DEPTH = vec4(1.0, 1.0, 0.85, 1.0);
// 影的明度差缩放时彩度同向（× 1 + CK·(depth − 1)），影是有颜色的深，不是发灰的深
const float TOON_PORT_SHADE_CK = 0.5;
// 影色冷暖（半球天光：参考的 HemisphereLight 天色 / 地色按法线 y 混合）：
// 影的 OKLab a、b 加上 天色方向 × SKY_C × (0.5 + 0.5·n.y) + 地色方向 × GND_C × max(−n.y, 0)：朝上与竖直的影偏 ambient.sky（冷），
// 只有朝下的面（檐底、冠底、雨篷底）偏 bounce.ground（暖）。竖直面不混地色：天色偏紫时，紫加暖会混成发灰的藕色。
// 明度随法线 y 加 HEMI_L·n.y（天比地亮）。
const float TOON_HEMI_SKY_C = 0.018;
const float TOON_HEMI_GND_C = 0.025;
const float TOON_HEMI_L = 0.04;       // 影的明度随法线 y：朝上 +、朝下 −（OKLab L）
// 向阳轮廓暖边（参考 uRim 默认 0.35，作用于全部卡通材质）：已登记 k_rim 的材质乘 GAIN；没登记的按材质类给底值，
// 分量顺序同 TOON_PORT_DEPTH；朝上的面（地面、屋面）不加底值，免得远处掠射的地面整片发亮。
const float TOON_RIM_GAIN = 1.5;
const vec4 TOON_RIM_FLOOR = vec4(0.15, 0.20, 0.20, 0.0);
// 硬卡通高光（参考 uSpec / uSpecSize：smoothstep(size ± fwidth, N·H) × 主光色 × 0.22 × 强度）：
// 只给 mat_table 行 3 A 登记了强度的材质（金属件，ToonFx.AnisoBand），只在曲面上（法线曲率 κ 过 KAPPA），影里没有。
const float TOON_SPEC_SIZE = 0.975;
const float TOON_SPEC_K = 0.75;       // 参考 0.22 × 主光强度 3.3 ≈ 0.73，HDR 超过 1 的部分交给柔光
const vec2 TOON_SPEC_KAPPA = vec2(0.5, 2.0);

// 自发光（ToonSpec.Emissive，ART-STYLE）：亮时整面取色名本身的颜色，不分阶、不吃主光与湿润，描线照常。
// 色名本身的颜色由灯照色（调色板行 1，不随时刻）去掉暖灯染色还原：行 1 = M × (L, a + K·a_lamp, b + K·b_lamp)，
// M、K 与 WP-37 的 ColorScriptDerive.LampLit、derive.json 的 lit.tintK 一致，行 1 的规则改了要同步。
const float TOON_EMIT_ROW1_M = 0.95;     // 行 1 的明度系数
const float TOON_EMIT_ROW1_TINT = 0.45;  // 行 1 的暖灯染色系数
const float TOON_EMIT_KL = 1.0;          // 自发光明度：L × KL + DL，再封顶 LMAX（不过曝成纯白）
const float TOON_EMIT_DL = 0.03;
const float TOON_EMIT_LMAX = 0.92;
const float TOON_EMIT_KC = 1.1;          // 自发光彩度倍数（OKLab 的 a、b）
// 顶点色号落在 40–63（天气色与发光常量，ART-STYLE）：单色常量，没有影色对，也没有行 1–3。
// 自发光牌面按色名引用发光常量（例如 sign.exitGreen），亮时就是常量本身，不再调；
// 灭时由常量推出暗的一对：明度 × OFF_KL、彩度 × OFF_KC，受光乘主光色 sun.light，影按 shadow.1 与其参照面之比。
const int TOON_CONST_FIRST = 40;
const int TOON_CONST_LAST = 63;
const float TOON_EMIT_OFF_KL = 0.7;
const float TOON_EMIT_OFF_KC = 0.6;
const float TOON_SHADOW1_REF = 0.729;    // shadow.1 的参照面：OKLab L 0.90 的中性灰，线性值 0.9³

// 室内窗光斑的辉光（WLD-INTERIORS）：调色板第 3 行的受光明度封顶 0.9 不放开，改在光斑亮芯加一层无色提亮
// K ×（1 − smoothstep(C0, C1, C)）× smoothstep(L0, L1, L)，C、L 是受光色的 OKLab 彩度与明度。
// 浅色、低彩度的地面和墙在光斑里略超过 1，让后期辉光抓住；高彩度表面（消防红的门、砖红）不加，不发光也不变粉；
// 深色表面（深色玻璃、深灰件）也不加，免得发灰发白。L0、L1 取 0、0.01 即退回只看彩度的原式。
const float TOON_IN_GLOW_K = 0.12;
const float TOON_IN_GLOW_C0 = 0.03;
const float TOON_IN_GLOW_C1 = 0.12;
const float TOON_IN_GLOW_L0 = 0.55;
const float TOON_IN_GLOW_L1 = 0.75;

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
	return float(clamp(id, 0, 127)) / 127.0;
}

float toon_overcast() {
	return clamp(weather_a.y, 0.0, 1.0);
}

// ---------------------------------------------------------------------------
// 材质表 mat_table（128×4 RGBA8，最近邻；行 0 线表归 WP-05 的布局）
//   行 1：R 明暗档；G half_level×255；B 过渡带（两阶、三阶存固定半宽 ×1000，两阶半、软物存 fwidth 倍数 ×50）；A 湿润响应×255
//   行 2：R 透光 k_t×255；G 受光侧暖边 k_rim×255；B 闪点系数×255；A 自发光通道（槽位 + 1，0 = 不自发光）
//   行 3：R 低两位解析法线（0 不用、1 球、2 柱）、bit7 扶梯滚动；G 倒角提亮的受光幅度、B 影里幅度（值/255×0.25 L，0 = 不提亮）；A 硬卡通高光强度
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
	int emit_slot;     // 自发光的灯光通道槽位，−1 = 不自发光
	float k_spec;      // 硬卡通高光强度，0 = 没有
	bool escalator;    // 静止带面上的上行踏级纹，不改变顶点与碰撞
};

ToonMat toon_mat(int id) {
	vec4 r1 = texelFetch(mat_table, ivec2(id, 1), 0);
	vec4 r2 = texelFetch(mat_table, ivec2(id, 2), 0);
	vec4 r3 = texelFetch(mat_table, ivec2(id, 3), 0);
	int steps = int(round(r1.r * 255.0));
	bool fw_band = steps == TOON_STEPS_TWO_HALF || steps == TOON_STEPS_SOFT;
	float band = fw_band ? r1.b * 255.0 / 50.0 : r1.b * 255.0 / 1000.0;
	int normal_fx = int(round(r3.r * 255.0));
	return ToonMat(steps, r1.g, band, r1.a, r2.r, r2.g, r2.b,
			normal_fx & 3, r3.g * 0.25, r3.b * 0.25, int(round(r2.a * 255.0)) - 1, r3.a, (normal_fx & 128) != 0);
}

// 按材质类取 vec4 参数的一个分量（顺序：两阶、两阶半、软物、植被三阶）
float toon_by_steps(vec4 v, int steps) {
	return steps == TOON_STEPS_TWO ? v.x : (steps == TOON_STEPS_TWO_HALF ? v.y : (steps == TOON_STEPS_SOFT ? v.z : v.w));
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
	int cid;       // 色号 0–511（低字节 + bank 位，见 TOON_F_COLOR_BANK）
	int flags;     // 标志字节：低四位语义，bit 4、5 是色号的 bank
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

// 贴花的底面色号 0–511（只在 TOON_F_DECAL 时有意义）：备用字节是低字节，bit 5 是 bank（PaletteEncoding.DecodeDecalBase）。
// 非贴花的备用字节（窗型、渐变、权重）不是色号，不经这里解。
int toon_decal_base(ToonCtx c) {
	return int(round(c.spare * 255.0)) + ((c.flags & TOON_F_DECAL_BANK) != 0 ? 256 : 0);
}

// 解析法线（ART-MODELING、ENG-PERF）：球取 P − C，柱减去轴向分量；都在视空间里算
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

// 4×4 Bayer 抖动淡化（ENG-PERF）：进度 = (fade_clock − |fade_t0|) / 0.25 s
const float TOON_BAYER[16] = float[](
		0.0, 8.0, 2.0, 10.0,
		12.0, 4.0, 14.0, 6.0,
		3.0, 11.0, 1.0, 9.0,
		15.0, 7.0, 13.0, 5.0);

bool toon_fade_discard(vec2 frag_xy) {
	if (fade_t0 == 0.0) {
		return false;
	}
	float p = clamp(mod(fade_clock - abs(fade_t0), anim_loop_seconds) / 0.25, 0.0, 1.0);
	int ix = int(mod(frag_xy.x, 4.0));
	int iy = int(mod(frag_xy.y, 4.0));
	float b = (TOON_BAYER[iy * 4 + ix] + 0.5) / 16.0;
	return fade_t0 > 0.0 ? (b >= p) : (b < p);
}

// ---------------------------------------------------------------------------
// 天气挂点
// ---------------------------------------------------------------------------
// 湿润的暴露度（SYS-WEATHER）：公共函数归 WP-45b，要读地面遮罩 2 的建筑遮蔽和树冠遮蔽。
// 世界坐标、世界法线与室内/外围标志直接交给 WP-45b 的公共求值。
float toon_wet_exposure(ToonCtx c) {
	return precip_wet_exposure(c.wpos, c.n_w,
			(c.flags & TOON_F_INTERIOR) != 0, (c.flags & TOON_F_OUTSKIRT) != 0);
}

// wet = 湿润 × 暴露 × 材质响应（材质响应在 mat_table 行 1 的 A）
float toon_wet(ToonCtx c, ToonMat m) {
	return clamp(weather_b.x, 0.0, 1.0) * toon_wet_exposure(c) * m.wet;
}

// 积水（SYS-WEATHER，公共函数归 WP-45b）：在湿润变暗之后，按积水遮罩把受光色、影色换成积水色；ROUGHNESS 不动。
// response 使用材质表行 1 的 A，已经由 toon_mat 解码为 m.wet。
void toon_puddle(ToonCtx c, ToonMat m, inout vec3 lit, inout vec3 shade) {
	precip_puddle_apply(c.wpos, c.cam, c.n_w,
			(c.flags & TOON_F_INTERIOR) != 0, (c.flags & TOON_F_OUTSKIRT) != 0, m.wet, lit, shade);
}

// 地面云影（SYS-WEATHER，云噪声归 WP-40）：主线版没有云影，恒为 1。
float toon_cloud_shadow(vec3 wpos) {
	return 1.0;
}

// ---------------------------------------------------------------------------
// 窗灯（ENG-PERF）：亮灭只由窗号哈希、灯光通道和窗覆盖决定，与怎样到达这个钟点无关。
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
// 路灯光斑（SYS-TIME、ENG-PERF；公共求值 lamp_pool_shade 归 WP-39）：
// 地面和 4 m 以下立面上的两阶光斑（亮芯 + 收边外圈），灯光加在受光色和影色上、不替换底色，强度随环境变暗升起；
// 观感参数都在 lamp.gdshaderinc 开头。室内不画（室内灯另走室内光）。
// 竖直面按世界法线 c.n_w 判（含插值与解析法线），曲面上光斑的分界是光滑的；边缘过渡宽度 c.pool_aa 在 TOON_FRAGMENT_BEGIN 里按统一控制流算好。
// ---------------------------------------------------------------------------
void toon_lamp_pool(ToonCtx c, vec3 lamp, inout vec3 lit, inout vec3 shade) {
	if ((c.flags & TOON_F_INTERIOR) != 0 || lamp_heff() >= LAMP_POOL_DAY_DEG.y) {
		return;
	}
	lamp_pool_shade(c.wpos, c.n_w, c.pool_aa, lamp, lit, shade);
}

// ---------------------------------------------------------------------------
// 自发光（ToonSpec.Emissive；SYS-TIME、ENG-PERF）：亮度读材质登记的灯光通道，随时刻表亮灭（lamp_fixture_level，WP-39）。
// 窗灯类通道取 L。灯具类通道（灯罩、灯牌）跟着灯头在 1 m 以内的那盏灯，与它的光斑、光晕同时亮灭：白天灯灭，灯罩也不亮；
// 附近没有登记的灯时取 L（保留比例 ×(1 + Δ)，夹到 0–1，"熄一半"这类时段整面按比例变暗）。
// ---------------------------------------------------------------------------
float toon_emissive_level(ToonCtx c, ToonMat m) {
	if (m.emit_slot < 0) {
		return 0.0;
	}
	return lamp_fixture_level(c.wpos, m.emit_slot);
}

bool toon_is_const(int cid) {
	return cid >= TOON_CONST_FIRST && cid <= TOON_CONST_LAST;
}

// 色名本身的颜色：发光常量直接取第 0 行；表面色对由灯照色去掉暖灯染色还原，再按 KL、DL、KC 调亮调艳，明度封顶 LMAX
vec3 toon_emissive_color(ToonCtx c, vec3 lamp_lit) {
	if (toon_is_const(c.cid)) {
		return pal_cur(c.cid);
	}
	vec3 o = ok_from_linear(lamp_lit) / TOON_EMIT_ROW1_M;
	o.yz -= TOON_EMIT_ROW1_TINT * ok_from_linear(pal_cur(PAL_LAMP_WARM)).yz;
	o.x = min(o.x * TOON_EMIT_KL + TOON_EMIT_DL, TOON_EMIT_LMAX);
	o.yz *= TOON_EMIT_KC;
	return ok_to_linear(o);
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

// 明暗层次与影色冷暖（max-110 110a，参数见 TOON_PORT_DEPTH、TOON_HEMI_*）：在调色板给的受光、影上改影色，受光色不动。
// 参考里影 = 固有色 × 半球天光（天色、地色按法线 y 混合），受光 = 影 + 主光；这里按色名的受光、影对做同样的事：
// 先把 ΔL 按材质类放大，再按法线 y 给影加天色或地色的色相与少量明度。k 是白天 × 晴天的门控（夜间、阴雨退回原影色）。
// 植被、树干的着色器自己拼 ToonCol 的，要用时调这一个函数。
vec3 toon_port_shade(ToonCtx c, ToonMat m, vec3 lit, vec3 shade, float k) {
	if (k <= 0.0) {
		return shade;
	}
	vec3 lo = ok_from_linear(lit);
	vec3 so = ok_from_linear(shade);
	float depth = mix(1.0, toon_by_steps(TOON_PORT_DEPTH, m.steps), k);
	so.x = lo.x - max(lo.x - so.x, 0.0) * depth - min(lo.x - so.x, 0.0);
	so.yz *= 1.0 + TOON_PORT_SHADE_CK * (depth - 1.0);
	vec2 sky = ok_from_linear(pal_cur(PAL_AMBIENT_SKY)).yz;
	vec2 gnd = ok_from_linear(pal_cur(PAL_BOUNCE_GROUND)).yz;
	sky /= max(length(sky), 1e-4);
	gnd /= max(length(gnd), 1e-4);
	float ny = clamp(c.n_w.y, -1.0, 1.0);
	so.yz += k * (TOON_HEMI_SKY_C * (0.5 + 0.5 * ny) * sky + TOON_HEMI_GND_C * max(-ny, 0.0) * gnd);
	so.x += k * TOON_HEMI_L * ny;
	return ok_to_linear(so);
}

// 硬卡通高光（max-110 110a）：sp = N·H、spw = fwidth(sp)，都在 TOON_FRAGMENT_BEGIN 的统一控制流里算好
vec3 toon_spec(ToonCtx c, ToonMat m, float sp, float spw) {
	if (m.k_spec <= 0.0 || (c.flags & TOON_F_INTERIOR) != 0) {
		return vec3(0.0);
	}
	float mask = smoothstep(TOON_SPEC_SIZE - spw, TOON_SPEC_SIZE + spw, sp) * smoothstep(TOON_SPEC_KAPPA.x, TOON_SPEC_KAPPA.y, c.kappa);
	return pal_cur(PAL_SUN_LIGHT) * (TOON_SPEC_K * m.k_spec * mask * (1.0 - toon_overcast()));
}

// 给定颜色的取色入口（contract-24 第 2 条）：lit、shade、lamp 是调用方取好的受光色、影色、灯照色（线性），
// 例如植被按局部季节从季相行取样（WLD-VEGETATION）。其后的处理与 toon_colors 完全相同：
// 天光渐变、地面反光、湿润与积水（按 mat_table 的湿润响应，不湿的材质登记 Wet = 0）、路灯光斑、倒角提亮
// （只给登记了 BevelLift 的材质）、自发光、200 m 降饱和、明暗档、透光、暖边；室内标志位照旧乘光斑增益，并加窗光斑辉光。
ToonCol toon_colors_from(ToonCtx c, ToonMat m, vec3 lit, vec3 shade, vec3 lamp) {
	bool interior = (c.flags & TOON_F_INTERIOR) != 0;
	float gain = interior ? interior_light.x : 1.0;

	// 自发光的颜色取湿润之前的灯照色：亮着的灯牌不因下雨变暗
	float emit_k = toon_emissive_level(c, m);
	vec3 emit_c = emit_k > 0.0 ? toon_emissive_color(c, lamp) : vec3(0.0);

	// 明暗层次与影色冷暖（max-110 110a）：室外、白天、晴天
	float day = smoothstep(-0.02, 0.05, sun_dir.y);
	shade = toon_port_shade(c, m, lit, shade, interior ? 0.0 : day * (1.0 - toon_overcast()));

	// 影里的两条连续项（ART-STYLE）：天光渐变；朝下面的地面暖反光（室内和夜间为 0）
	shade *= 1.0 + TOON_SKY_GRAD * c.n_w.y;
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

	// 倒角带提亮（ART-STYLE）：按法线曲率判倒角带，只改明暗，不写 NORMAL
	float bevel = smoothstep(2.0, 4.0, c.kappa) * step(1e-4, m.bevel_lit);
	if (bevel > 0.0) {
		lit = ok_adjust(lit, 1.0, 1.0, m.bevel_lit * bevel);
		shade = ok_adjust(shade, 1.0, 1.0, m.bevel_shade * bevel);
	}

	// 自发光：受光色、影色、灯照色一起换成色名本身的颜色，"受光 − 影"归零，不再分阶，也不吃点光
	if (emit_k > 0.0) {
		lit = mix(lit, emit_c, emit_k);
		shade = mix(shade, emit_c, emit_k);
		lamp = mix(lamp, emit_c, emit_k);
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

	// 受光侧暖边：rim × 受光色 × k_rim；登记的 k_rim 乘 TOON_RIM_GAIN，没登记的按材质类给底值（朝上的面与室内不给）
	float rim_floor = toon_by_steps(TOON_RIM_FLOOR, m.steps) * (1.0 - smoothstep(0.55, 0.85, c.n_w.y)) * (interior ? 0.0 : 1.0);
	vec3 rim = pal_cur(PAL_RIM) * lit * max(m.k_rim * TOON_RIM_GAIN, rim_floor);

	// 自发光亮着时没有透光、暖边和深影
	if (emit_k > 0.0) {
		trans *= 1.0 - emit_k;
		rim *= 1.0 - emit_k;
		deep_mix *= 1.0 - emit_k;
	}

	// 室内窗光斑的无色提亮：折进受光色并除以增益，light() 乘过增益和主光强度后，光斑里正好多出 lift × 主光强度，
	// 影一侧不变；透光、暖边在这之前算好，不受影响。自发光亮着时不加
	if (interior) {
		vec3 lo = ok_from_linear(lit);
		float glow = TOON_IN_GLOW_K * (1.0 - smoothstep(TOON_IN_GLOW_C0, TOON_IN_GLOW_C1, length(lo.yz)))
				* smoothstep(TOON_IN_GLOW_L0, TOON_IN_GLOW_L1, lo.x);
		lit += vec3(glow * (1.0 - emit_k) / max(gain, 1e-3));
	}

	return ToonCol(lit, shade, lamp, trans, rim, vec3(0.0), deep_mix, bw, half_level, push, gain);
}

// 按色号取色：室外取调色板行 0 的受光、影，室内取行 3（室内受光色）、行 2（室内影色）；灯照色取行 1。
// 发光常量（40–63）没有这些行，灭时的一对由常量推出（见 TOON_EMIT_OFF_*）。
ToonCol toon_colors(ToonCtx c, ToonMat m) {
	if (toon_is_const(c.cid)) {
		vec3 e = ok_adjust(pal_cur(c.cid), TOON_EMIT_OFF_KL, TOON_EMIT_OFF_KC, 0.0);
		return toon_colors_from(c, m, e * pal_cur(PAL_SUN_LIGHT), e * pal_cur(PAL_SHADOW_1) / TOON_SHADOW1_REF,
				e * pal_cur(PAL_LAMP_WARM));
	}
	bool interior = (c.flags & TOON_F_INTERIOR) != 0;
	vec3 lit;
	vec3 shade;
	if (interior) {
		shade = pal_fetch(c.cid, PAL_ROW_IN_SHADE);
		lit = pal_fetch(c.cid, PAL_ROW_IN_LIT);
	} else {
		lit = pal_fetch(c.cid, PAL_ROW_CUR);
		shade = pal_fetch(c.cid + PAL_SHADE_OFFSET, PAL_ROW_CUR);
	}
	return toon_colors_from(c, m, lit, shade, pal_fetch(c.cid, PAL_ROW_LAMP));
}

// EMISSION：影（植被深影按 L ×0.88、C ×0.95 由影色算出）+ 闪光帧 + 额外自发光
vec3 toon_emission(ToonCol t, ToonCtx c) {
	vec3 e = t.shade;
	if (t.deep_mix > 0.0) {
		e = mix(e, ok_adjust(t.shade, 0.88, 0.95, 0.0), t.deep_mix);
	}
	// 闪光帧（SYS-WEATHER）：全场一瞬间翻到受光色，没有投影
	e += clamp(lightning.x, 0.0, 1.0) * (t.lit - t.shade) * (0.5 + 0.5 * c.n_w.y);
	return e + t.extra;
}

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
	v_cid = int(round(COLOR.r * 255.0)) + ((v_flags & TOON_F_COLOR_BANK) != 0 ? 256 : 0); \
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
