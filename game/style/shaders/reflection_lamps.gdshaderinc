#ifndef CAMPUS_REFLECTION_LAMPS_INCLUDED
#define CAMPUS_REFLECTION_LAMPS_INCLUDED

#include "res://style/shaders/lamp.gdshaderinc"

// 夜景层唯一生成的 refl_lamps：16×2 RGBAF，每列一个引用，合计最多16个。
// 行0 = (type, ref, baseId, slot + 32*cols)：type 0空、1灯具、2窗；灯具ref为lamp_table灯号，窗ref为窗号。
// 行1 = (窗中心x,y,z,外围标志)，直接取同一次windowSet.Buffer；灯具该行为空。
// 水面与湿地面只改长度比例（1 与 0.5）；开灯、覆盖与冷暖只调用原灯/窗公共函数。
const int REFLECTION_LAMP_CAPACITY = 16;
const float REFLECTION_LAMP_WIDTH = 0.5;
const float REFLECTION_LAMP_LENGTH = 8.0;
const float REFLECTION_LAMP_BOUNDARY_JITTER = 0.18;
const vec2 REFLECTION_LAMP_DUTY = vec2(0.50, 0.72);
const float REFLECTION_LAMP_CUT_SKEW = 0.08;
const float REFLECTION_LAMP_END_SOFT = 0.06;
const float REFLECTION_LAMP_SIDE_SOFT = 0.05;
const float REFLECTION_LAMP_TIP_WIDTH = 0.40;

// 相邻截共用同一条边界；扰动小于半截，边界始终有序，可直接定位当前截。
float reflection_lamp_boundary(uint seed, float index, float count, float strip_length, float across) {
	if (index <= 0.0) return 0.0;
	if (index >= count) return strip_length;
	uint key = seed + uint(index) * 17u;
	float jitter = (lamp_hash01(key) * 2.0 - 1.0) * REFLECTION_LAMP_BOUNDARY_JITTER;
	float skew = (lamp_hash01(key + 1u) * 2.0 - 1.0) * REFLECTION_LAMP_CUT_SKEW;
	float cross_ratio = clamp(across / (REFLECTION_LAMP_WIDTH * 0.5), -1.0, 1.0);
	return (index + jitter) * strip_length / count + cross_ratio * skew;
}

// 返回加到本表面雾处理之前的线性 RGB；湿润遮罩和表面朝向由消费方提供。
vec3 reflection_lamps_color(vec3 p_world, vec3 camera_world, float length_scale) {
	vec3 color = vec3(0.0);
	float h_eff = lamp_heff();
	float gain = LAMP_POOL_GAIN * 0.5 * lamp_pool_visibility(h_eff);
	float strip_length = REFLECTION_LAMP_LENGTH * length_scale;
	float pixel = max(length(fwidth(p_world.xz)), 1e-4);
	float camera_height = camera_world.y - p_world.y;
	if (camera_height <= 0.0) return color;
	for (int i = 0; i < REFLECTION_LAMP_CAPACITY; i++) {
		vec4 reference = texelFetch(refl_lamps, ivec2(i, 0), 0);
		int kind = int(round(reference.x));
		if (kind == 0) continue;
		vec3 source;
		vec3 lamp_color;
		float level;
		if (kind == 1) {
			LampData lamp = lamp_fetch(int(round(reference.y)));
			source = lamp.base + vec3(0.0, lamp.head, 0.0);
			lamp_color = pal_cur(lamp.col);
			level = lamp_level(lamp, h_eff);
		} else {
			vec4 window = texelFetch(refl_lamps, ivec2(i, 1), 0);
			int id = int(round(reference.y));
			int base_id = int(round(reference.z));
			int slot = int(mod(reference.w, 32.0));
			int cols = int(floor(reference.w / 32.0));
			bool outskirts = (int(round(window.w)) & 1) != 0;
			vec4 override_value = outskirts ? vec4(0.0) : lamp_window_override(id);
			source = window.xyz;
			level = lamp_window_on(id, base_id, cols, slot, override_value);
			lamp_color = pal_cur(lamp_window_cool(id, override_value) ? PAL_WINDOW_COOL : PAL_WINDOW_WARM);
		}
		float source_height = source.y - p_world.y;
		if (source_height <= 0.0) continue;
		vec2 toward = camera_world.xz - source.xz;
		float camera_distance = length(toward);
		if (camera_distance <= pixel) continue;
		vec2 axis = toward / camera_distance;
		// 相机连到水平面下的镜像灯头，与本片元所在平面的交点；灯条从这里朝相机延伸。
		vec2 reflection_origin = source.xz + toward * source_height / (source_height + camera_height);
		vec2 offset = p_world.xz - reflection_origin;
		float along = dot(offset, axis);
		float across = dot(offset, vec2(-axis.y, axis.x));
		float t = along / strip_length;
		float half_width = REFLECTION_LAMP_WIDTH * 0.5 * mix(1.0, REFLECTION_LAMP_TIP_WIDTH, clamp(t, 0.0, 1.0));
		if (along < 0.0 || along > strip_length || abs(across) > half_width + pixel) continue;
		float width = 1.0 - smoothstep(half_width - REFLECTION_LAMP_SIDE_SOFT - pixel,
			half_width + pixel, abs(across));
		// 稳定引用作种子，不读灯条表槽号，排序变化不会换一套断口。
		uint seed = lamp_hash_u(uint(round(reference.y)) * 131u + uint(kind) * 193u);
		float count = 5.0 + floor(lamp_hash01(seed) * 3.0);
		float segment = min(floor(t * count), count - 1.0);
		float lower = reflection_lamp_boundary(seed, segment, count, strip_length, across);
		float upper = reflection_lamp_boundary(seed, segment + 1.0, count, strip_length, across);
		if (along < lower) segment -= 1.0;
		else if (along >= upper) segment += 1.0;
		lower = reflection_lamp_boundary(seed, segment, count, strip_length, across);
		upper = reflection_lamp_boundary(seed, segment + 1.0, count, strip_length, across);
		uint key = seed + uint(segment) * 31u + 101u;
		float duty = mix(REFLECTION_LAMP_DUTY.x, REFLECTION_LAMP_DUTY.y, lamp_hash01(key));
		float gap = (upper - lower) * (1.0 - duty);
		float start = lower + gap * lamp_hash01(key + 1u);
		float finish = start + (upper - lower) * duty;
		float soft = REFLECTION_LAMP_END_SOFT * length_scale + pixel;
		float dash = smoothstep(start, start + soft, along) * (1.0 - smoothstep(finish - soft, finish, along));
		float shape = width * dash * (1.0 - t);
		vec3 contribution = lamp_color * level * shape * gain;
		// 与灯下光斑一致，重叠处只取最亮的一盏，避免叠成路面标线。
		if (lamp_luma(contribution) > lamp_luma(color)) color = contribution;
	}
	return color;
}

#endif
