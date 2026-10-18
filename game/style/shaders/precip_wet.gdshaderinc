// precip_wet.gdshaderinc：湿润与积水的公共函数（WP-45b）。
// 规格：SYS-WEATHER（湿面、积水）、§3 第 5 条、§7（湿润与积水）；ENG-PERF。
// 挂点归 WP-04：toon.gdshaderinc 在 fog.gdshaderinc 之后 include 本文件，toon_wet_exposure、toon_wet、toon_puddle
// 三个挂点的函数体调这里。water 与外围也可以 include（雨点环）。本文件只读 globals 与 fog 的函数，自己不 include。
//
// C# 侧的同一判定（IGroundCover 用，G2 比对）是契约里的 Campus.Contracts.WetSurface，阈值与哈希逐项一致；改这里就同时改那边。
// 主线版：地面遮罩 2 还没有烘焙（R 积水潜势、G 建筑遮蔽、B 树冠遮蔽），积水潜势先用程序噪声，遮蔽只按朝向；
// 积水倒影：天空按视线分成硬边色带（地平线亮、高天暗）；precip_occ 有内容时沿它步进，把楼和树切成剪影色块映进去
// （主线版的 precip_occ 是常量小图，不步进）；夜间灯条与湿路面竖向拉长的倒影带在后置细节里接。
// 做法照参考场景"太阳雨中的小渔港"的积水（wet rim + damp halo、水边内侧略暗、俯视两档菲涅尔，ART-STYLE）。
// 湿痕只随天气：湿润与积水量回到 0 时完全干净，不留水渍（D-174）。

#ifndef PRECIP_WET_GDSHADERINC
#define PRECIP_WET_GDSHADERINC

// ---- 可调参数（与 WetSurface 一致） ----
const float PW_SCALE_A = 2.4;           // 积水潜势：两层值噪声的尺度（m）与权重
const float PW_SCALE_B = 0.9;
const float PW_WEIGHT_A = 0.62;
const float PW_PUDDLE_TOP = 0.80;       // 积水阈值：积水量刚大于 0 时 0.80（只有潜势最高的几处低洼，约 3%），
const float PW_PUDDLE_SPAN = 0.22;      // 积水量 1 时降到 0.58（约三成硬质铺地成水）；轻雨阈值高、覆盖少，干的时候原地缩小
const vec2 PW_FLAT = vec2(0.990, 0.997);   // 只有近乎水平的面积水（法线 y；约 8° 以上的坡不积）
const vec2 PW_RESPONSE = vec2(0.45, 0.55); // 硬质面门：材质湿润响应 ≥ 0.55 才有积水和天光带（沥青 1、铺装 0.6）；草地 0.4、布与树叶更低，只做湿润变暗
const vec2 PW_SHEEN_GRAZE = vec2(0.80, 0.985);   // 湿面掠射天光带：1 − |N·V| 在这一段里渐入天空色（远处的湿路面映天）
const float PW_SHEEN_K = 0.5;           // 天光带的混合量 × 湿润
const float PW_WET_EXTRA = 0.20;        // 湿面在 toon 的湿润变暗之上再压一档（× (1 − 0.20·wet)）
const float PW_WET_CONTRAST = 0.10;     // 影色多压 0.10·wet：明暗对比略增
const float PW_WET_CHROMA = 0.15;       // 湿面彩度 +15%（SYS-WEATHER）
const vec2 PW_POOL_F = vec2(0.72, 0.92);   // 积水里天空倒影的占比：俯视 0.72、平视 0.92（两档硬切）
const float PW_POOL_FLOOR = 0.62;       // 透出的水底：湿地面色 × 0.62
const float PW_MENISCUS = 0.84;         // 水边内侧 4 cm 略暗
const float PW_EDGE_LIGHT = 0.025;      // 水边的细亮线宽（m）
const vec2 PW_RIM_W = vec2(0.045, 0.05);   // 湿边宽 = 0.045 + 0.05 × 噪声（m）
const float PW_RIM_DARK = 0.70;         // 湿边：地面色 × 0.70
const float PW_HALO = 0.25;             // 湿晕从湿边向外破碎延伸的幅度（m）
const float PW_HALO_DARK = 0.90;        // 湿晕：地面色 × 0.90
const float PW_GRAD_EPS = 0.08;         // 积水潜势梯度的差分步长（m），把潜势差换成到水边的米数
const vec4 PW_SKY_BANDS = vec4(0.06, 0.2, 0.45, 0.012);   // 倒影天空的三条硬边（仰角正弦）与边宽
const vec4 PW_SKY_GAIN = vec4(1.14, 1.0, 0.88, 0.76);     // 四条色带的明度系数：地平线亮、高天暗，倒影里读得出渐变
const float PW_SKY_SLOPE = 0.12;        // 色带之内再按仰角连续压暗（上亮下暗）
const float PW_POOL_NEAR = 0.72;        // 1 − |N·V| 小于它算俯视（近处的积水多透出水底）
const vec2 PW_SIL = vec2(0.55, 0.8);    // 倒影里楼和树的剪影色 = mix(该方向地平线天色, 湿地面影色, 0.55) × 0.8
const float PW_MARCH_T0 = 0.6;          // 倒影沿 precip_occ 步进：首步 0.6 m，每步 ×1.6，共 10 步（约 60 m，SYS-WEATHER）
const float PW_MARCH_K = 1.6;
const int PW_MARCH_N = 10;
const float PW_RING_CELL = 0.8;         // 雨点环：0.8 m 网格，一拍二，硬边同心环
const float PW_RING_STEPS = 5.0;        // 一个环从生到灭的拍数
const float PW_RING_R = 0.32;           // 环的最大半径（m）

uint pw_hash(uint x) {
	x ^= x >> 16u;
	x *= 2146121005u;
	x ^= x >> 15u;
	x *= 2221713035u;
	x ^= x >> 16u;
	return x;
}

float pw_cell(ivec2 c) {
	uint h = pw_hash(uint(c.x) * 73856093u ^ pw_hash(uint(c.y) * 19349663u + 1u));
	return float(h >> 8u) / 16777216.0;
}

float pw_value_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = p - i;
	vec2 u = f * f * (3.0 - 2.0 * f);
	ivec2 c = ivec2(i);
	float a = pw_cell(c);
	float b = pw_cell(c + ivec2(1, 0));
	float d = pw_cell(c + ivec2(0, 1));
	float e = pw_cell(c + ivec2(1, 1));
	return mix(mix(a, b, u.x), mix(d, e, u.x), u.y);
}

// 积水潜势 0–1（主线版的程序噪声；地面遮罩 2 的 R 通道交付后改读它）
float precip_puddle_potential(vec2 xz) {
	return PW_WEIGHT_A * pw_value_noise(xz / PW_SCALE_A) + (1.0 - PW_WEIGHT_A) * pw_value_noise(xz / PW_SCALE_B + vec2(17.0, 5.0));
}

// 暴露度 0–1：室内 0；朝天的面 1，立面约 0.6，朝下的面（檐底、雨篷底）0。外围同样按朝向（只做湿润变暗与天光带）
float precip_wet_exposure(vec3 wpos, vec3 n_w, bool interior, bool outskirt) {
	if (interior) {
		return 0.0;
	}
	float ny = clamp(n_w.y, -1.0, 1.0);
	return smoothstep(-0.6, 0.0, ny) * mix(0.6, 1.0, smoothstep(0.0, 0.7, ny));
}

// 积水阈值：积水量越大阈值越低，积水从同一批低洼处长大、也原地缩小（积水量为 0 时调用方直接不画）
float precip_puddle_threshold(float level) {
	return PW_PUDDLE_TOP - PW_PUDDLE_SPAN * clamp(level, 0.0, 1.0);
}

// 雨点环（SYS-WEATHER）：0.8 m 网格，每格一个环，按一拍二长大、变淡。aa 为本片元的世界尺寸（m）
float precip_rain_rings(vec2 xz, float aa) {
	vec2 g = xz / PW_RING_CELL;
	ivec2 c = ivec2(floor(g));
	vec2 f = g - floor(g);
	float s = floor(anim_time * step_rate + 0.5);
	float ph = floor(pw_cell(c + ivec2(311, 97)) * PW_RING_STEPS);
	float t = mod(s + ph, PW_RING_STEPS) / PW_RING_STEPS;
	vec2 ctr = vec2(0.25) + 0.5 * vec2(pw_cell(c + ivec2(53, 7)), pw_cell(c + ivec2(13, 71)));
	float d = length(f - ctr) * PW_RING_CELL;
	float r = (0.15 + 0.85 * t) * PW_RING_R;
	float w = max(aa, 0.008);
	return (1.0 - smoothstep(w * 0.5, w * 1.5, abs(d - r))) * (1.0 - t);
}

// 倒影天空：按反射视线的仰角分成硬边色带，地平线亮、高天暗（取 fog_sky 在各带中点的颜色）
vec3 precip_sky_bands(vec3 r, float d_el) {
	vec2 h = r.xz;
	float lh = length(h);
	h = lh > 1e-4 ? h / lh : vec2(1.0, 0.0);
	float el = clamp(r.y, 0.0, 1.0);
	float w = PW_SKY_BANDS.w + d_el;
	vec3 c = fog_sky(vec3(h.x, 0.0, h.y)) * PW_SKY_GAIN.x;
	c = mix(c, fog_sky(vec3(h.x, 0.13, h.y)) * PW_SKY_GAIN.y, smoothstep(PW_SKY_BANDS.x - w, PW_SKY_BANDS.x + w, el));
	c = mix(c, fog_sky(vec3(h.x, 0.32, h.y)) * PW_SKY_GAIN.z, smoothstep(PW_SKY_BANDS.y - w, PW_SKY_BANDS.y + w, el));
	c = mix(c, fog_sky(vec3(h.x, 0.75, h.y)) * PW_SKY_GAIN.w, smoothstep(PW_SKY_BANDS.z - w, PW_SKY_BANDS.z + w, el));
	return c * (1.0 - PW_SKY_SLOPE * el);
}

// 反射视线沿 precip_occ 步进：碰到顶面以下就是楼或树（返回 1）。主线版的常量小图不步进
float precip_reflect_hit(vec3 p, vec3 r) {
	if (textureSize(precip_occ, 0).x < 16) {
		return 0.0;
	}
	float t = PW_MARCH_T0;
	for (int i = 0; i < PW_MARCH_N; i++) {
		vec3 q = p + r * t;
		if (!in_map(q)) {
			break;
		}
		if (q.y < textureLod(precip_occ, map_uv(q), 0.0).r) {
			return 1.0;
		}
		t *= PW_MARCH_K;
	}
	return 0.0;
}

// 湿面、湿边湿晕与积水（toon_puddle 的函数体调它；在 toon 的湿润变暗之后）。response：材质线表里的湿润响应
void precip_puddle_apply(vec3 wpos, vec3 cam, vec3 n_w, bool interior, bool outskirt, float response,
		inout vec3 lit, inout vec3 shade) {
	// 导数一律在分支之前取
	vec3 v = normalize(wpos - cam);
	float graze = 1.0 - abs(dot(v, n_w));
	vec2 xz = wpos.xz;
	float pot = precip_puddle_potential(xz);
	float gx = precip_puddle_potential(xz + vec2(PW_GRAD_EPS, 0.0)) - pot;
	float gz = precip_puddle_potential(xz + vec2(0.0, PW_GRAD_EPS)) - pot;
	float aa = max(length(fwidth(xz)), 1e-4);
	vec3 rv = reflect(v, vec3(0.0, 1.0, 0.0));
	float d_el = fwidth(rv.y);
	if (interior) {
		return;
	}
	float wet = clamp(weather_b.x, 0.0, 1.0) * precip_wet_exposure(wpos, n_w, interior, outskirt) * response;

	// 硬质面门：草地等软面只变暗，不映天光、不积水
	float hard = smoothstep(PW_RESPONSE.x, PW_RESPONSE.y, response);

	// 湿面：整体再压一档、影色多压一点（对比略增）、彩度 +15%；硬质面的掠射角映天光
	if (wet > 0.0) {
		lit *= 1.0 - PW_WET_EXTRA * wet;
		shade *= 1.0 - (PW_WET_EXTRA + PW_WET_CONTRAST) * wet;
		float kc = 1.0 + PW_WET_CHROMA * wet;
		lit = max(mix(vec3(dot(lit, vec3(0.2126, 0.7152, 0.0722))), lit, kc), vec3(0.0));
		shade = max(mix(vec3(dot(shade, vec3(0.2126, 0.7152, 0.0722))), shade, kc), vec3(0.0));
		float band = smoothstep(PW_SHEEN_GRAZE.x, PW_SHEEN_GRAZE.y, graze);
		vec3 sky = fog_sky(reflect(v, n_w));
		float k = PW_SHEEN_K * wet * band * hard;
		lit = mix(lit, sky, k);
		shade = mix(shade, sky, k);
	}

	// 积水：近乎水平的硬质面，只在潜势高的低洼处按阈值长出；外围 O1、O2 不积水
	float level = clamp(weather_b.y, 0.0, 1.0) * hard * smoothstep(PW_FLAT.x, PW_FLAT.y, n_w.y);
	if (outskirt || level <= 0.0) {
		return;
	}
	float th = precip_puddle_threshold(level);
	float g = max(length(vec2(gx, gz)) / PW_GRAD_EPS, 0.05);
	float d = (th - pot) / g;                       // 到水边的米数，水里为负
	float inside = 1.0 - smoothstep(-aa, aa, d);

	// 水外：一圈深色湿边，再往外是破碎的湿晕
	float rim_w = PW_RIM_W.x + PW_RIM_W.y * pw_value_noise(xz * 3.1 + 7.0);
	float halo_n = 0.65 * pw_value_noise(xz * 1.9 + 3.0) + 0.35 * pw_value_noise(xz * 5.3 - 1.0);
	float rim = 1.0 - smoothstep(rim_w - aa, rim_w + aa, d);
	float halo = 1.0 - smoothstep(0.06 - aa, 0.06 + aa, d - rim_w - PW_HALO * halo_n);
	float damp = max(rim * (1.0 - PW_RIM_DARK), halo * (1.0 - PW_HALO_DARK)) * (1.0 - inside);
	lit *= 1.0 - damp;
	shade *= 1.0 - damp;
	if (inside <= 0.0) {
		return;
	}

	// 水里：天空色带倒影（有遮挡数据时映楼和树的剪影），俯视时多透出一点暗的水底
	vec3 sky = precip_sky_bands(rv, d_el);
	vec3 sil = mix(fog_sky(vec3(rv.x, 0.0, rv.z)), shade, PW_SIL.x) * PW_SIL.y;
	vec3 refl = mix(sky, sil, precip_reflect_hit(wpos, rv));
	float f = mix(PW_POOL_F.x, PW_POOL_F.y, smoothstep(PW_POOL_NEAR - 0.02, PW_POOL_NEAR + 0.02, graze));
	vec3 water = mix(shade * PW_POOL_FLOOR, refl, f);
	water *= mix(PW_MENISCUS, 1.0, smoothstep(0.04 - aa, 0.04 + aa, -d));
	// 水边的细亮线
	float edge = 1.0 - smoothstep(PW_EDGE_LIGHT - aa, PW_EDGE_LIGHT + aa, -d);
	water = mix(water, fog_sky(vec3(rv.x, 0.03, rv.z)) * 1.08, 0.75 * edge);
	// 下雨时积水里有雨点环
	float raining = clamp(weather_a.z * 4.0, 0.0, 1.0) * (1.0 - step(0.5, weather_a.w));
	if (raining > 0.0) {
		water = mix(water, sky * 1.12, 0.7 * raining * precip_rain_rings(xz, aa));
	}
	lit = mix(lit, water, inside);
	shade = mix(shade, water, inside);
}

#endif
