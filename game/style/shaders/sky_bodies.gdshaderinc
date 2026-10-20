// ---------------------------------------------------------------------------
// 五段渐变、霾带、定侧（太阳侧 / 背阳侧，入夜换月侧）、OKLab 换算与调色板取色都在 fog.gdshaderinc 里，
// 与共用雾函数、玻璃映天同一份（ART-SKYLIGHT），地平线处天色等于雾色。
// 五段的仰角 [0°, 4°, 12°, 28°, 90°] 是那里的 SKY_STOP_DEG。
// ---------------------------------------------------------------------------
float aastep(float edge, float x) {
	float w = max(fwidth(x), 1e-5);
	return smoothstep(edge - w, edge + w, x);
}

// ---------------------------------------------------------------------------
// 日盘、月盘、星空（全分辨率）
// ---------------------------------------------------------------------------
vec3 add_sun(vec3 c, vec3 dir, float px, float w_sun) {
	vec3 s = sky_sun_vec();
	float h = sky_alt_deg(s);
	if (h < -2.0 * SUN_RADIUS_DEG) {
		return c;
	}
	float chord = length(dir - s);
	float r = radians(SUN_RADIUS_DEG);
	float low = 1.0 - smoothstep(SUN_LOW_FROM, SUN_LOW_TO, h);
	vec3 light = pal_cur(PAL_SUN_LIGHT);
	vec3 disc = mix(light * SUN_HIGH_GAIN, mix(light, pal_cur(PAL_SKY_SUN0 + 1), SUN_LOW_ORANGE) * SUN_LOW_GAIN, low);
	float above = smoothstep(-px, px, dir.y);
	float fogk = sky_fog_weather_k();
	// 雾天：一枚隔雾可见的淡白圆，不带光晕（ART-SKYLIGHT）
	disc = mix(disc, mix(pal_cur(PAL_WEATHER_FOG), light, 0.35) * 1.05, fogk);
	// 低于霾带时日盘被霾染色、压暗
	float e = degrees(asin(clamp(dir.y, -1.0, 1.0)));
	float hz = exp2(-max(e, 0.0) / (0.5 * sky_haze_deg()));
	disc = mix(disc, mix(disc, sky_haze_color(w_sun), 0.55), hz);
	float halo_amp = mix(SUN_HALO_HIGH, SUN_HALO_LOW, low) * (1.0 - fogk);
	float halo_w = radians(mix(4.5, 1.6, low));
	c += light * halo_amp * exp(-chord / halo_w) * above;
	float k = (1.0 - smoothstep(r - px, r + px, chord)) * above;
	return mix(c, disc, k);
}

vec3 moon_color() {
	vec3 m = pal_cur(PAL_SKY_MOON);
	if (dot(m, vec3(1.0)) < 0.03) {
		float low = 1.0 - smoothstep(4.0, 30.0, sky_alt_deg(sky_moon_vec()));
		m = mix(pal_cur(PAL_WINDOW_COOL), pal_cur(PAL_WINDOW_WARM), mix(0.18, 0.45, low));
	}
	return m * MOON_GAIN;
}

vec3 add_moon(vec3 c, vec3 dir, float px) {
	vec3 m = sky_moon_vec();
	if (m.y < -0.02) {
		return c;
	}
	float r = radians(MOON_RADIUS_DEG);
	vec3 off = dir - m;
	float chord = length(off);
	vec3 col = moon_color();
	float above = smoothstep(-px, px, dir.y);
	// 月晕：一圈很淡的光晕，不出镜头光斑（ART-STYLE）
	c += col * MOON_HALO * (exp(-chord / radians(2.2)) + 0.4 * exp(-chord / radians(9.0))) * above;
	if (chord > r + 2.0 * px) {
		return c;
	}
	vec3 t = normalize(cross(m, vec3(0.0, 1.0, 0.0)) + vec3(1e-5, 0.0, 0.0));
	vec3 b = cross(t, m);
	vec2 q = vec2(dot(off, t), dot(off, b)) / r;
	float z = sqrt(max(1.0 - dot(q, q), 0.0));
	vec3 n = t * q.x + b * q.y - m * z;
	vec3 lit_dir = length(moon_lit.xyz) > 0.5 ? normalize(moon_lit.xyz) : -m;
	float lit = smoothstep(-0.03, 0.03, dot(n, lit_dir));
	// 月面只画两三块淡灰斑，不画写实月面
	float maria = max(max(
		1.0 - smoothstep(0.26, 0.30, length(q - vec2(-0.28, 0.22))),
		1.0 - smoothstep(0.16, 0.20, length(q - vec2(0.22, 0.34)))),
		1.0 - smoothstep(0.19, 0.23, length(q - vec2(0.06, -0.26))));
	float limb = 1.0 - smoothstep(0.86, 0.90, length(q));
	vec3 disc = col * mix(1.0, MOON_MARIA, maria) * mix(MOON_LIMB, 1.0, limb);
	// 背光的一侧不画成黑：只比周围的天亮一点点
	disc = mix(mix(c, disc, 0.12), disc, lit);
	float k = (1.0 - smoothstep(r - px, r + px, chord)) * above;
	return mix(c, disc, k);
}

uvec3 pcg3(uvec3 v) {
	v = v * 1664525u + 1013904223u;
	v.x += v.y * v.z;
	v.y += v.z * v.x;
	v.z += v.x * v.y;
	v ^= v >> 16u;
	v.x += v.y * v.z;
	v.y += v.z * v.x;
	v.z += v.x * v.y;
	return v;
}

vec3 face_dir(int face, vec2 uv) {
	if (face == 0) { return normalize(vec3(1.0, uv.y, uv.x)); }
	if (face == 1) { return normalize(vec3(-1.0, uv.y, uv.x)); }
	if (face == 2) { return normalize(vec3(uv.x, 1.0, uv.y)); }
	if (face == 3) { return normalize(vec3(uv.x, -1.0, uv.y)); }
	if (face == 4) { return normalize(vec3(uv.x, uv.y, 1.0)); }
	return normalize(vec3(uv.x, uv.y, -1.0));
}

float star_field(vec3 dir, float px, out vec3 tint) {
	tint = vec3(1.0);
	vec3 a = abs(dir);
	int face = 0;
	vec2 uv = vec2(0.0);
	if (a.x >= a.y && a.x >= a.z) {
		face = dir.x > 0.0 ? 0 : 1;
		uv = dir.zy / a.x;
	} else if (a.y >= a.z) {
		face = dir.y > 0.0 ? 2 : 3;
		uv = dir.xz / a.y;
	} else {
		face = dir.z > 0.0 ? 4 : 5;
		uv = dir.xy / a.z;
	}
	vec2 g = (uv * 0.5 + 0.5) * STAR_GRID;
	vec2 cell = floor(g);
	uvec3 h = pcg3(uvec3(uint(cell.x), uint(cell.y), uint(face) + 977u));
	vec3 rnd = vec3(h & uvec3(65535u)) / 65535.0;
	if (rnd.x > STAR_P) {
		return 0.0;
	}
	vec2 j = vec2(fract(rnd.y * 7.31), fract(rnd.z * 3.17)) * 0.6 + 0.2;
	vec2 suv = (cell + j) / STAR_GRID * 2.0 - 1.0;
	vec3 sd = face_dir(face, suv);
	float bright = pow(fract(rnd.y * 13.7 + rnd.z * 5.3), 3.0);
	float size = mix(0.75, 1.9, bright);
	float chord = length(dir - sd);
	float k = 1.0 - smoothstep(px * size * 0.5, px * size * 1.25, chord);
	// 闪烁按一拍二步进；频率取 480 s 的整数倍，anim_time 回卷时不跳
	float steps = max(step_rate, 1.0);
	float ts = floor(anim_time * steps) / steps;
	float f = TAU * floor(mix(70.0, 190.0, rnd.z)) / anim_loop_seconds;
	float tw = 0.78 + 0.22 * sin(ts * f + rnd.x * 40.0);
	tint = mix(pal_cur(PAL_WINDOW_COOL), pal_cur(PAL_WINDOW_WARM), fract(rnd.z * 11.1) * 0.35);
	return k * mix(0.35, 1.0, bright) * tw;
}

vec3 full_res_sky(vec3 dir, float pix) {
	float w_sun = sky_side_weight(dir);
	vec3 c = sky_gradient(dir, w_sun);
	// 星空：可见度是全局 stars（SkyNode 按色彩脚本、阴度与雾写入）；近地平线、近月处淡出，被云挡住的地方不画
	if (stars > 0.001 && dir.y > 0.0) {
		vec3 tint;
		float st = star_field(dir, pix, tint);
		float vis = stars * smoothstep(2.0, 10.0, degrees(asin(dir.y)));
		vec3 m = sky_moon_vec();
		if (m.y > -0.05) {
			vis *= smoothstep(radians(4.0), radians(16.0), length(dir - m));
		}
		c += tint * st * vis * STAR_GAIN;
	}
	c = add_moon(c, dir, pix);
	return add_sun(c, dir, pix, w_sun);
}

