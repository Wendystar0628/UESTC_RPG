// toon_static.gdshader：建筑、地面、道具的块几何、独立实例和重复部件的 MultiMesh；外围 O0–O2 也用它（WP-04）
// 规格：ENG-PERF §1.4；ART-STYLE §2、§3。材质只经材质库按 ToonSpec 取得，逐物体的颜色走顶点色号。
shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert, specular_disabled, ambient_light_disabled;

#include "res://style/shaders/toon.gdshaderinc"

void vertex() {
	TOON_VERTEX
}

void fragment() {
	TOON_FRAGMENT_BEGIN
	ToonCol tcol = toon_colors(tctx, tmat);
	TOON_FRAGMENT_END(tcol)
}

void light() {
	TOON_LIGHT
}
