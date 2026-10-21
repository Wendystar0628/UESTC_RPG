// diorama_far.gdshader：远景环与雾海（WLD-OUTSKIRTS-DIORAMA；WLD-OUTSKIRTS-DIORAMA；ENG-PERF）
//
// 不透明、双面、不受影也不投影（顶点标志 8，宿主关投影）；颜色全部来自调色板与共用雾函数（fog.gdshaderinc），着色器里不写色值。
// 顶点格式 VF-B（与水面同）：COLOR = 受光色号（影 = +1）、AO 字节、标志、备用字节 = 种类；UV、UV2 按远景顶点种类：
//   种类 0 雾海：UV = (离内缘的外向距离 m, 雾天顶面上限 y)
//   种类 1 城市环：UV = (沿立面 m, 离地 m)，UV2 = (视差 k, 雾化量)，AO = 楼的种子（0 = 屋顶机房，不开窗）
//   种类 2 远层（雪山与远山）：UV = (积雪潜势, 山脊标记)，UV2 = (四季可见度字, 四季雪线字)，每季 6 位，春在低位
// 视差（§5.4）：顶点水平加 k ×（相机 − 中心），中心取 map_rect 的中心；远层 k = 1，并且竖直也随相机，看起来在无穷远。
// 受光一律按真实太阳（入夜换月亮）的方向判定；天气只读 weather_a、weather_b、weather_fog（SYS-WEATHER），不自己判断天气。
// 主线版：远景是程序生成的体块与高度场，不是手绘卡片贴图（通道打包的卡片留到 §5.4 的贴图那一步）；雾海没有清水河银带和顶面下的分层环。
shader_type spatial;
render_mode unshaded, cull_disabled, shadows_disabled, world_vertex_coords, depth_draw_opaque;

#include "res://style/shaders/globals.gdshaderinc"
#include "res://style/shaders/fog.gdshaderinc"

// ---------------------------------------------------------------------------------------------------------------
// 可调参数（评审定值；按图的数在 backdrop.json，这里只有全局美术规则）
// ---------------------------------------------------------------------------------------------------------------
const int KIND_FOG = 0;
const int KIND_CITY = 1;
const int KIND_FAR = 2;
const int FLAG_COLOR_BANK = 16;

// 受光两阶：N·L 的过渡带
const vec2 DF_LIT_BAND = vec2(-0.03, 0.10);
// 城市环
const float DF_CITY_JITTER = 0.14;      // 楼与楼的明度差（按种子）
const float DF_ROOF_LIFT = 0.05;        // 屋顶比受光立面亮的明度（OKLab L）
const float DF_SHADE_MIX = 0.82;        // 影面取影色的比例（远处影面不至于压成死黑）
const float DF_LIT_KEEP = 0.62;         // 受光面取受光色的比例（其余取影色）：远处受光面不刺眼，明暗只差一阶
const vec2 DF_WIN_CELL = vec2(3.6, 3.0);   // 窗格：开间宽、层高（m）
const vec2 DF_WIN_DENSITY = vec2(0.28, 0.5);   // 远处城市的亮窗比例（按楼）30%–50%（§5 表）
const float DF_WIN_COOL = 0.18;         // 冷白窗点的比例
const float DF_WIN_THROUGH = 0.6;       // 窗光穿过雾化的保留：亮窗 = 窗色 × (1 − 雾化 × (1 − 本值))
const float DF_WIN_GAIN = 1.25;
const vec2 DF_NIGHT_SUN = vec2(-4.0, 3.0);    // 太阳高度（度）从 y 降到 x 时窗点亮起
const float DF_NIGHT_SIL = 0.8;         // 夜里楼群剪影 = 雾色 × 本值（OKLab L）
const float DF_NIGHT_SIL_MIX = 0.85;
const float DF_STREAK_NIGHT = 0.3;      // 夜里亮纹保留的比例（月光下只剩淡淡几道）
// 天气对雾化的作用（清透度乘子；雨后初晴 ×1.6，§5.3）
const float DF_OVERCAST_CLARITY = 0.45;
const float DF_RAIN_CLARITY = 0.3;
const float DF_FOG_CLARITY = 0.3;       // 雾天：清透度 × (1 − 雾 × (本值 + k))，近层留剪影、中层几乎没进雾里
const float DF_AFTER_RAIN = 1.6;
const vec2 DF_SEASON_CLARITY = vec2(1.08, 0.88);   // 夏、冬的清透度（春秋为 1）
// 远层
const float DF_SNOW_NOISE = 0.16;       // 雪线的噪声起伏（潜势单位）
const float DF_SNOW_NOISE_SCALE = 900.0;   // 雪线噪声的平铺米数
const float DF_ROCK_LIT = 0.95;         // 岩石受光面相对背后天色的明度（OKLab L 比例）
const float DF_ROCK_SHADE = 0.84;       // 岩石背光面
const float DF_GLOW_MIX = 0.5;          // 低太阳时迎光雪坡染云边色（九顶山的淡玫瑰、日照金山的金色）
const vec2 DF_GLOW_SUN = vec2(2.0, 24.0);   // 太阳高度（度）在 x–y 之间时染色由满到无
const float DF_RIM_PX = 2.2;            // 山脊金边的像素宽
const float DF_RIM_MIX = 0.85;
const vec2 DF_RIM_SUN = vec2(-1.5, 26.0);   // 有金边的太阳高度范围（度）
const float DF_SKIRT_FADE = 140.0;      // 山脚以下多少米渐隐到雾色（下缘不收底，§4.1）
const float DF_VIS_CLEAR_FLOOR = 0.35;  // 晴天可见度下限（§6.3）
const float DF_VIS_BAD_FLOOR = 0.15;    // 阴、雾、雨的下限
const float DF_VIS_AFTER_RAIN = 0.95;   // 雨后初晴
const float DF_VIS_HAZE = 0.3;          // 霾带高（fog.y 1°→8°）时可见度最多压掉的比例
// 雾海
const float DF_SEA_LIFT = 0.035;        // 近处顶面比地平线霾色亮的明度（OKLab L），远处收回到霾色，与天空无缝
const vec2 DF_SEA_LIFT_RANGE = vec2(1500.0, 7000.0);
const float DF_STREAK_SCALE = 2600.0;   // 亮纹噪声的平铺米数
const vec2 DF_STREAK_DRIFT = vec2(0.55, 0.32);   // 漂移（m/s，按 anim_time）
const vec2 DF_STREAK_TH = vec2(0.6, 0.66);
const float DF_STREAK_MIX = 0.16;
const float DF_DEEP_NIGHT_LIFT = 0.55;  // 深夜顶面的平涂提亮（太阳 −12° → −24° 亮起，§4.2）

varying vec3 v_world;
varying vec3 v_model;
varying vec3 v_nrm;
varying vec2 v_uv;
varying vec2 v_uv2;
varying flat int v_kind;
varying flat int v_cid;
varying flat float v_seed;

vec2 df_centre() {
	return map_rect.xy + 0.5 / max(map_rect.zw, vec2(1e-6));
}

float df_hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// 四季字（每季 6 位，春在低位）按连续季节 s 循环插值
float df_season_word(float word, float s) {
	int w = int(round(word));
	float x = mod(s, 4.0);
	int i0 = int(floor(x)) & 3;
	int i1 = (i0 + 1) & 3;
	float a = float((w >> (6 * i0)) & 63) / 63.0;
	float b = float((w >> (6 * i1)) & 63) / 63.0;
	return mix(a, b, fract(x));
}

// 季节权重：夏 1 与冬 3 处为 1
float df_season_near(float s, float anchor) {
	float d = abs(mod(s - anchor + 2.0, 4.0) - 2.0);
	return clamp(1.0 - d, 0.0, 1.0);
}

float df_night() {
	return 1.0 - smoothstep(DF_NIGHT_SUN.x, DF_NIGHT_SUN.y, sky_alt_deg(sky_sun_vec()));
}

// 主光方向：白天真实太阳，入夜换月亮（与天空定侧同一个权重）
vec3 df_light_dir() {
	vec3 s = sky_sun_vec();
	vec3 m = sky_moon_vec();
	return normalize(mix(s, m, sky_night_k()) + vec3(0.0, 1e-4, 0.0));
}

// 天气：阴度、雾、降水、雨后初晴
float df_overcast() { return clamp(weather_a.y, 0.0, 1.0); }
float df_rain() { return smoothstep(0.02, 0.4, weather_a.z); }
float df_after_rain() {
	return clamp(weather_b.x, 0.0, 1.0) * (1.0 - df_overcast()) * (1.0 - df_rain()) * (1.0 - sky_fog_weather_k());
}

// 雾化量的天气、季节修正（清透度乘在"不雾化"的那一份上）
float df_haze(float haze, float k) {
	float s = season.x;
	float c = (1.0 - DF_OVERCAST_CLARITY * df_overcast()) * (1.0 - DF_RAIN_CLARITY * df_rain());
	c *= 1.0 - sky_fog_weather_k() * clamp(DF_FOG_CLARITY + k, 0.0, 1.0);
	c *= mix(1.0, DF_AFTER_RAIN, df_after_rain());
	c *= mix(1.0, DF_SEASON_CLARITY.x, df_season_near(s, 1.0)) * mix(1.0, DF_SEASON_CLARITY.y, df_season_near(s, 3.0));
	float h = 1.0 - (1.0 - haze) * c;
	return clamp(h, 0.0, 1.0);
}

vec3 df_ok_scale(vec3 c, float k) {
	vec3 o = ok_from_linear(c);
	o.x *= k;
	return ok_to_linear(o);
}

vec3 df_ok_lift(vec3 c, float d) {
	vec3 o = ok_from_linear(c);
	o.x = clamp(o.x + d, 0.0, 1.0);
	return ok_to_linear(o);
}

void vertex() {
	int flags = int(round(COLOR.b * 255.0));
	v_cid = int(round(COLOR.r * 255.0)) + ((flags & FLAG_COLOR_BANK) != 0 ? 256 : 0);
	v_kind = int(round(COLOR.a * 255.0));
	v_seed = COLOR.g;
	v_uv = UV;
	v_uv2 = UV2;
	v_model = VERTEX;
	v_nrm = NORMAL;
	vec3 cam = MAIN_CAM_INV_VIEW_MATRIX[3].xyz;
	vec2 off = cam.xz - df_centre();
	if (v_kind == KIND_CITY) {
		VERTEX.xz += UV2.x * off;
	} else if (v_kind == KIND_FAR) {
		VERTEX.xz += off;
		VERTEX.y += max(cam.y, 0.0);
	} else {
		// 雾天雾海上升到截面上缘以下（§4.3）：低于上限的部分按雾密度连续抬起，散雾时原路回落
		float rise = UV.y;
		VERTEX.y += max(rise - VERTEX.y, 0.0) * sky_fog_weather_k();
	}
	v_world = VERTEX;
}

vec3 df_city(vec3 dir, vec3 haze_col) {
	vec3 n = normalize(v_nrm);
	vec3 L = df_light_dir();
	float lit = smoothstep(DF_LIT_BAND.x, DF_LIT_BAND.y, dot(n, L));
	vec3 c_lit = pal_cur(v_cid);
	vec3 c_shade = mix(c_lit, pal_cur(v_cid + 1), DF_SHADE_MIX);
	float seed = floor(v_seed * 255.0 + 0.5);
	float jitter = 1.0 + DF_CITY_JITTER * (df_hash(vec2(seed, 7.0)) - 0.5);
	vec3 col = df_ok_scale(mix(c_shade, mix(c_shade, c_lit, DF_LIT_KEEP), lit), jitter);
	if (n.y > 0.5) {
		col = df_ok_lift(col, DF_ROOF_LIFT * lit);
	}
	float haze = df_haze(v_uv2.y, v_uv2.x);
	col = mix(col, haze_col, haze);
	// 夜里楼群剪影比背后的城市天光暗一阶（§5.3），窗点才压得住
	float night = df_night();
	col = mix(col, df_ok_scale(haze_col, DF_NIGHT_SIL), night * DF_NIGHT_SIL_MIX);
	// 夜里的窗点：只在立面、只在有种子的楼上；像素足迹大于窗格时换成平均亮度，不闪
	if (night > 0.0 && seed > 0.5 && abs(n.y) < 0.5) {
		vec2 cell = floor(v_uv / DF_WIN_CELL);
		vec2 f = fract(v_uv / DF_WIN_CELL);
		float density = mix(DF_WIN_DENSITY.x, DF_WIN_DENSITY.y, df_hash(vec2(seed, 3.0)));
		float on = step(df_hash(cell + vec2(seed * 17.0, seed * 3.0)), density * night);
		float shape = step(0.18, f.x) * step(f.x, 0.82) * step(0.28, f.y) * step(f.y, 0.78);
		vec2 fp = fwidth(v_uv);
		float detail = 1.0 - smoothstep(0.9, 2.4, max(fp.x, fp.y));
		float mask = mix(density * night * 0.34, on * shape, detail);
		vec3 wc = mix(pal_cur(PAL_WINDOW_WARM), pal_cur(PAL_WINDOW_COOL), step(1.0 - DF_WIN_COOL, df_hash(cell + vec2(5.3, seed))));
		col += wc * mask * DF_WIN_GAIN * (1.0 - haze * (1.0 - DF_WIN_THROUGH));
	}
	return col;
}

vec3 df_far(vec3 dir) {
	vec3 n = normalize(v_nrm);
	vec3 sun = sky_sun_vec();
	float alt = sky_alt_deg(sun);
	vec3 L = df_light_dir();
	float lit = smoothstep(DF_LIT_BAND.x, DF_LIT_BAND.y, dot(n, L));
	float s = season.x;
	float overcast = max(df_overcast(), max(sky_fog_weather_k(), df_rain()));

	// 背后的天色：这一方位、略高于地平线的天空（天空函数，§6.4）
	vec3 sky_dir = normalize(vec3(dir.x, max(dir.y, 0.03), dir.z));
	vec3 sky_c = fog_sky(sky_dir);

	// 积雪：潜势 + 噪声 对 雪线（季节参数，§6.3、§6.4）
	float line = df_season_word(v_uv2.y, s);
	float noise = texture(cloud_noise, v_model.xz / DF_SNOW_NOISE_SCALE + vec2(v_model.y / DF_SNOW_NOISE_SCALE * 3.0, 0.0)).g - 0.5;
	float pot = v_uv.x + noise * DF_SNOW_NOISE;
	float fw = fwidth(pot) + 0.004;
	float snowk = smoothstep(line - fw, line + fw, pot);

	vec3 rock_lit = df_ok_scale(sky_c, DF_ROCK_LIT);
	vec3 rock_shade = df_ok_scale(sky_c, DF_ROCK_SHADE);
	vec3 snow_lit = pal_cur(v_cid);
	vec3 snow_shade = pal_cur(v_cid + 1);
	// 低太阳：迎光的雪坡染云边色（暖金、淡玫瑰），阴雨天不染（§6.2）
	float low = (1.0 - smoothstep(DF_GLOW_SUN.x, DF_GLOW_SUN.y, alt)) * step(-0.5, alt) * (1.0 - overcast);
	snow_lit = mix(snow_lit, pal_cur(PAL_SKY_CLOUD_RIM), low * DF_GLOW_MIX);
	vec3 col = mix(mix(rock_shade, rock_lit, lit), mix(snow_shade, snow_lit, lit), snowk);

	// 背光的山脊镶一线金边：太阳在山后、低悬，晴天才有，夜里没有
	vec2 dh = normalize(dir.xz + vec2(1e-5));
	vec2 sh = normalize(sun.xz + vec2(1e-5));
	float behind = smoothstep(0.35, 0.85, dot(dh, sh));
	float rim_sun = smoothstep(DF_RIM_SUN.x, DF_RIM_SUN.x + 2.0, alt) * (1.0 - smoothstep(DF_RIM_SUN.y * 0.5, DF_RIM_SUN.y, alt));
	float e = v_uv.y;
	float rim = 1.0 - smoothstep(0.0, fwidth(e) * DF_RIM_PX, 1.0 - e);
	col = mix(col, pal_cur(PAL_RIM), rim * behind * rim_sun * (1.0 - overcast) * DF_RIM_MIX);

	// 可见度 = max(下限, 季节可见度 × 霾带因子 × 天气因子)；雨后初晴取 0.95（§6.3、§6.4）
	float vis_season = df_season_word(v_uv2.x, s);
	float haze_k = 1.0 - DF_VIS_HAZE * smoothstep(1.0, 8.0, sky_haze_deg());
	float bad = clamp(overcast, 0.0, 1.0);
	float snowing = clamp(weather_a.w, 0.0, 1.0) * df_rain();
	float floor_v = mix(DF_VIS_CLEAR_FLOOR, mix(DF_VIS_BAD_FLOOR, DF_VIS_CLEAR_FLOOR, snowing), bad);
	float vis = max(floor_v, vis_season * haze_k * (1.0 - bad));
	vis = mix(vis, DF_VIS_AFTER_RAIN, df_after_rain());
	col = mix(sky_c, col, vis);

	// 山脚以下（裙边）渐隐成该方向的雾色，与雾海、地平线以下的天空同色
	float skirt = smoothstep(0.0, -DF_SKIRT_FADE, v_model.y);
	return mix(col, sky_haze_color(sky_side_weight(dir)), skirt);
}

vec3 df_sea(vec3 dir, vec3 cam, vec3 haze_col) {
	float d = length(v_world.xz - cam.xz);
	vec3 col = df_ok_lift(haze_col, DF_SEA_LIFT * (1.0 - smoothstep(DF_SEA_LIFT_RANGE.x, DF_SEA_LIFT_RANGE.y, d)));
	// 顶面的亮纹：阈值噪声，按 anim_time 缓慢漂移；颜色随主光（白天暖、夜里月光银灰），远处淡掉以免闪
	vec2 p = (v_world.xz + DF_STREAK_DRIFT * anim_time) / DF_STREAK_SCALE;
	float nz = texture(cloud_noise, p).b;
	float fwn = fwidth(nz) + 0.01;
	float streak = smoothstep(DF_STREAK_TH.x - fwn, DF_STREAK_TH.y + fwn, nz) * (1.0 - smoothstep(2500.0, 8000.0, d));
	vec3 light = mix(pal_cur(PAL_SUN_LIGHT), pal_cur(PAL_SKY_MOON), sky_night_k());
	col = mix(col, max(col, light), streak * DF_STREAK_MIX * (1.0 - sky_fog_weather_k()) * mix(1.0, DF_STREAK_NIGHT, sky_night_k()));
	// 深夜顶面微微发光，做城市与雪山剪影的底
	float deep = smoothstep(-12.0, -24.0, sky_alt_deg(sky_sun_vec()));
	col *= 1.0 + DF_DEEP_NIGHT_LIFT * deep;
	return col;
}

void fragment() {
	vec3 cam = INV_VIEW_MATRIX[3].xyz;
	vec3 ray = v_world - cam;
	vec3 dir = normalize(ray);
	vec3 haze_col = sky_haze_color(sky_side_weight(dir));
	vec3 col;
	if (v_kind == KIND_CITY) {
		col = df_city(dir, haze_col);
	} else if (v_kind == KIND_FAR) {
		col = df_far(dir);
	} else {
		col = df_sea(dir, cam, haze_col);
	}
	ALBEDO = col;
	// 雾已按本层的规则合进颜色（色调读共用雾函数的雾色、天色）；介质（楼里、地下、水下）照共用规则盖上
	FOG = medium.z > 0.0 ? vec4(medium_color.rgb, clamp(medium.z, 0.0, 1.0)) : vec4(0.0);
}
