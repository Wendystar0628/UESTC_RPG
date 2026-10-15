// reflect_flat.gdshader：倒影世界里的平涂 REFLECT 代理（WP-24；WLD-WATER §7.2，ENG-PERF §10.4）
//
// 只在倒影 SubViewport 的独立世界里用：没有光、不投影，颜色只读调色板行 0。
// 顶点 COLOR 与 toon 相同（R 是受光色号，影 = 受光 + 1）；按主光方向分受光与影两块，倒影里的明暗与岸上一致。
// 取色与雾用 fog.gdshaderinc（WP-04），与岸上同源。
// 倒影相机在水面以下，4.7 的 Camera3D 没有斜裁剪，所以丢弃水面以下的片元。
shader_type spatial;
render_mode unshaded, cull_back, shadows_disabled, ambient_light_disabled;

#include "res://style/shaders/globals.gdshaderinc"
#include "res://style/shaders/fog.gdshaderinc"

uniform float water_y = -10000.0;   // 当前做倒影的湖的水面高度，倒影节点在换湖时写入

varying flat int v_cid;
varying vec3 v_wpos;
varying vec3 v_nrm;

void vertex() {
	v_cid = int(round(COLOR.r * 255.0));
	v_wpos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_nrm = normalize(mat3(MODEL_MATRIX) * NORMAL);
}

void fragment() {
	if (v_wpos.y < water_y - 0.02) {
		discard;
	}
	float lit = step(-0.03, dot(normalize(v_nrm), normalize(key_dir)));
	lit *= step(0.01, key_strength);
	vec3 c = mix(pal_cur(v_cid + PAL_SHADE_OFFSET), pal_cur(v_cid), lit);
	// 倒影里的远处同样沉进雾里：共用雾函数，按代理到倒影相机的距离
	vec4 fg = fog_eval(v_wpos, INV_VIEW_MATRIX[3].xyz);
	ALBEDO = mix(c, fg.rgb, fg.a);
}
