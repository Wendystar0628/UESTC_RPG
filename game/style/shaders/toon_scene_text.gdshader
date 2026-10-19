// Printed scene lettering: native Godot MSDF coverage mixes registered ink and board palette colours.
shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert, specular_disabled, ambient_light_disabled;

#include "res://style/shaders/toon.gdshaderinc"

uniform sampler2DArray scene_text_pages : filter_linear, repeat_disable;
uniform float scene_text_range;

void vertex() {
	TOON_VERTEX
}

void fragment() {
	TOON_FRAGMENT_BEGIN
	vec3 msdf = texture(scene_text_pages, vec3(UV, UV2.x)).rgb;
	float sd = max(min(msdf.r, msdf.g), min(max(msdf.r, msdf.g), msdf.b)) - 0.5;
	vec2 dimensions = vec2(textureSize(scene_text_pages, 0).xy);
	vec2 pixel_uv = max(fwidth(UV), vec2(1e-6));
	float screen_range = max(0.5 * dot(vec2(scene_text_range) / dimensions, 1.0 / pixel_uv), 1.0);
	float cover = clamp(sd * screen_range + 0.5, 0.0, 1.0);
	int base = toon_decal_base(tctx);
	vec3 lit = mix(pal_fetch(base, PAL_ROW_CUR), pal_fetch(tctx.cid, PAL_ROW_CUR), cover);
	vec3 shade = mix(pal_fetch(base + PAL_SHADE_OFFSET, PAL_ROW_CUR), pal_fetch(tctx.cid + PAL_SHADE_OFFSET, PAL_ROW_CUR), cover);
	vec3 lamp = mix(pal_fetch(base, PAL_ROW_LAMP), pal_fetch(tctx.cid, PAL_ROW_LAMP), cover);
	ToonCol tcol = toon_colors_from(tctx, tmat, lit, shade, lamp);
	TOON_FRAGMENT_END(tcol)
}

void light() {
	TOON_LIGHT
}
