// ---------------------------------------------------------------------------
// 材质表 mat_table（128×4 RGBA8，最近邻；行 0 线表归 game/kernel/post/ 的布局）
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
	float band = fw_band ? r1.b * 255.0 / MAT_FWIDTH_BAND_SCALE : r1.b * 255.0 / MAT_FIXED_BAND_SCALE;
	int normal_fx = int(round(r3.r * 255.0));
	return ToonMat(steps, r1.g, band, r1.a, r2.r, r2.g, r2.b,
			normal_fx & 3, r3.g * MAT_BEVEL_SCALE, r3.b * MAT_BEVEL_SCALE, int(round(r2.a * 255.0)) - 1, r3.a, (normal_fx & MAT_ESCALATOR_FLAG) != 0);
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
	return int(round(c.spare * 255.0)) + ((c.flags & TOON_F_DECAL_BANK) != 0 ? PAL_WIDTH : 0);
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
// 湿润的暴露度（SYS-WEATHER）：公共函数归 game/systems/weather/precip/，要读地面遮罩 2 的建筑遮蔽和树冠遮蔽。
// 世界坐标、世界法线与室内/外围标志直接交给 game/systems/weather/precip/ 的公共求值。
float toon_wet_exposure(ToonCtx c) {
	return precip_wet_exposure(c.wpos, c.n_w,
			(c.flags & TOON_F_INTERIOR) != 0, (c.flags & TOON_F_OUTSKIRT) != 0);
}

// wet = 湿润 × 暴露 × 材质响应（材质响应在 mat_table 行 1 的 A）
float toon_wet(ToonCtx c, ToonMat m) {
	return clamp(weather_b.x, 0.0, 1.0) * toon_wet_exposure(c) * m.wet;
}

// 积水（SYS-WEATHER，公共函数归 game/systems/weather/precip/）：在湿润变暗之后，按积水遮罩把受光色、影色换成积水色；ROUGHNESS 不动。
// response 使用材质表行 1 的 A，已经由 toon_mat 解码为 m.wet。
void toon_puddle(ToonCtx c, ToonMat m, inout vec3 lit, inout vec3 shade) {
	precip_puddle_apply(c.wpos, c.cam, c.n_w,
			(c.flags & TOON_F_INTERIOR) != 0, (c.flags & TOON_F_OUTSKIRT) != 0, m.wet, lit, shade);
}

// 地面云影（SYS-WEATHER，云噪声归 game/systems/sky/）：主线版没有云影，恒为 1。
float toon_cloud_shadow(vec3 wpos) {
	return 1.0;
}

// ---------------------------------------------------------------------------
// 窗灯（ENG-PERF）：亮灭只由窗号哈希、灯光通道和窗覆盖决定，与怎样到达这个钟点无关。
// 哈希、阈值、亮灭与冷暖用 lamp.gdshaderinc 的 lamp_hash01、lamp_window_*（与 fx_glow、C# 的 ScheduleMath 同一份）。
// window_override（RGBA8，纹素 = (id % 256, id / 256)，归 game/systems/nightlight/）：R 亮灭（0 不覆盖、128 强制灭、255 强制亮），
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
// 路灯光斑（SYS-TIME、ENG-PERF；公共求值 lamp_pool_shade 归 game/systems/nightlight/）：
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
// 自发光（ToonSpec.Emissive；SYS-TIME、ENG-PERF）：亮度读材质登记的灯光通道，随时刻表亮灭（lamp_fixture_level，MAP-QSH-ATMOSPHERE）。
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
	shade *= 1.0 + TOON_SKY_GRAD * c.n_w.y * (interior ? 0.0 : 1.0);
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

