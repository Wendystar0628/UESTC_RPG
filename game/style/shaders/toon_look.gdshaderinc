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
// 高两位是色号的 bank（contract-28，PaletteEncoding.EncodeColor）：bit 4 主色色号 +256，bit 5 贴花底色色号 +256。
// 低四位的语义不变；旧几何（palette encoding 1）这两位恒为 0，解出来的色号与原来逐个相同。

// 明暗分阶（ART-STYLE）
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

