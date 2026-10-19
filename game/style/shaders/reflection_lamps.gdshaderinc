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
const float REFLECTION_LAMP_SEGMENTS = 6.0;

// 返回加到本表面雾处理之前的线性 RGB；湿润遮罩和表面朝向由消费方提供。
vec3 reflection_lamps_color(vec3 p_world, vec3 camera_world, float length_scale) {
	vec3 color = vec3(0.0);
	float h_eff = lamp_heff();
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
		float across = abs(dot(offset, vec2(-axis.y, axis.x)));
		float width = 1.0 - smoothstep(REFLECTION_LAMP_WIDTH * 0.5 - pixel,
			REFLECTION_LAMP_WIDTH * 0.5 + pixel, across);
		float t = along / strip_length;
		float ends = step(0.0, along) * (1.0 - step(strip_length, along));
		// 六个等距短段与断口，沿离灯的方向逐渐淡出（WLD-WATER）。
		float dash = 1.0 - step(0.5, fract(t * REFLECTION_LAMP_SEGMENTS));
		float shape = width * ends * dash * (1.0 - clamp(t, 0.0, 1.0));
		color += lamp_color * level * shape;
	}
	return color;
}

#endif
