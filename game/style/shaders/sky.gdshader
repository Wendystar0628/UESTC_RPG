// sky.gdshader：全日天空（ART-SKYLIGHT-ATMOSPHERE）
// 规格：ART-SKYLIGHT；SYS-TIME；SYS-WEATHER；ENG-PERF。
// 主线版：五段渐变（太阳侧 / 背阳侧，夜间换月侧）、霾带、日盘与柔光、月盘（球面明暗与月晕）、星空、
// 半分辨率云层（四分之一分辨率通道只记轮廓朝向，给金边与下沿高光用；积云：1.5 km 高的云底平面 + 高度场团块，硬边三阶；层云：满覆盖、两阶、留亮缝）。
// 颜色只读调色板纹理第 0 行（ENG-PERF 的行布局，色号与 game/style/palette/core.json 一致），着色器里不写颜色。
// 不用内置 TIME；时间只读 anim_time、step_rate；日月方向读全局参数里的真实方向（sun_dir、moon_dir），不读 LIGHT0。
// 当前帧的云量与阈值由 SkyNode 写进本材质的两个 uniform（云量来自 IColorScriptFrame，不从脚本读回全局参数）。
// 后置细节（ART-SKYLIGHT-ATMOSPHERE）：银河、城市天光方位、九项光效、远处雨幕与云内闪、彩虹、积雨云卡片。

shader_type sky;
render_mode use_half_res_pass, use_quarter_res_pass;

#include "res://style/shaders/globals.gdshaderinc"
#include "res://style/shaders/fog.gdshaderinc"

#include "res://style/shaders/sky_params.gdshaderinc"
#include "res://style/shaders/sky_bodies.gdshaderinc"
#include "res://style/shaders/sky_clouds.gdshaderinc"

void sky() {
	vec3 dir = normalize(EYEDIR);
	float pix = max(length(fwidth(EYEDIR)), 1e-5);
	vec3 sx = dFdx(EYEDIR);
	vec3 sy = dFdy(EYEDIR);
	if (AT_CUBEMAP_PASS) {
		// 卡通材质不读天空的环境光和反射（ENG-BUDGET）：辐照度图只给常量，云和天气每帧变也不让它变贵
		COLOR = vec3(0.5);
	} else if (AT_QUARTER_RES_PASS) {
		vec4 q = vec4(0.0);
		if (dir.y > 0.0 && deck_amount() < 0.999) {
			q = cumulus(dir, POSITION, pix, vec3(0.0), true, sx, sy);
		}
		COLOR = q.rgb;
		ALPHA = q.a;
	} else if (AT_HALF_RES_PASS) {
		vec4 o = half_res_clouds(dir, pix);
		COLOR = o.rgb;
		ALPHA = o.a;
	} else {
		COLOR = composite_clouds(full_res_sky(dir, pix), dir, HALF_RES_COLOR, QUARTER_RES_COLOR);
	}
}
