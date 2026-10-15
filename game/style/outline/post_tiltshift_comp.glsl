#[compute]

#version 450

// post_tiltshift_comp.glsl — WP-05. CompositorEffect POST_TRANSPARENT, the compose dispatch of the tilt-shift pair
// (ART-STYLE §7.2, §7.4, §7.8; ENG-PERF §10.2). Runs every frame even when tilt-shift is off, because the
// highlight shoulder (style floor) and the vignette live here:
//   1. tilt-shift blend: mix(c, blur, smoothstep(0.66, 1.05, |uv.y - 0.47| * 2) * 0.25 * amount)
//      (amount 0 when the player turned tilt-shift off; the two blur dispatches are then skipped),
//   2. highlight shoulder on the overflow only: registered palette colours (lit colours reach 1.0) pass unchanged
//      (ΔE <= 2 required, here 0); above the start the peak is rolled off into a small headroom and desaturated
//      toward white with Khronos PBR Neutral's term, so glow (threshold at the start) still blooms only the real
//      highlights and the final clip falls on near-white instead of shifting hue,
//   3. vignette: c *= mix(1 - k * (1 - tint), 1, smoothstep(1.15, 0.25, length((uv - 0.5) * vec2(aspect, 1)))).
// Input and output are the linear HDR colour buffer before the engine's glow / tonemap / LUT.

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba16f, set = 0, binding = 0) uniform restrict image2D color_image;
layout(set = 0, binding = 1) uniform sampler2D blur_tex;   // 1/2 or 1/4 resolution blur, bilinear

layout(push_constant, std430) uniform CompParams {
	vec4 size;       // internal width, height, 1/width, 1/height
	vec4 tilt;       // x: tilt-shift amount 0..1 (fades to 0 in 0.5 s when switched off), y: max blend (0.25), zw: reserved
	vec4 vignette;   // rgb: vignette tint, w: strength k (0.10 day, 0.12 night; 0 when switched off)
	vec4 misc;       // x: shoulder start (1.0, above every registered lit colour), y: headroom (0.5), zw: reserved
} pc;

vec3 highlight_shoulder(vec3 c, float start, float headroom) {
	float peak = max(c.r, max(c.g, c.b));
	if (peak > start) {
		float np = start + headroom * (1.0 - exp(-(peak - start) / headroom));
		c *= np / peak;
		float g = 1.0 - 1.0 / (0.15 * (peak - np) + 1.0);
		c = mix(c, vec3(np), g);
	}
	return c;
}

void main() {
	ivec2 p = ivec2(gl_GlobalInvocationID.xy);
	if (any(greaterThanEqual(p, ivec2(pc.size.xy)))) {
		return;
	}
	vec2 uv = (vec2(p) + 0.5) * pc.size.zw;
	vec4 col = imageLoad(color_image, p);
	vec3 c = col.rgb;

	if (pc.tilt.x > 0.0) {
		float w = smoothstep(0.66, 1.05, abs(uv.y - 0.47) * 2.0) * pc.tilt.y * pc.tilt.x;
		if (w > 0.0) {
			c = mix(c, textureLod(blur_tex, uv, 0.0).rgb, w);
		}
	}

	c = highlight_shoulder(max(c, vec3(0.0)), pc.misc.x, max(pc.misc.y, 1e-3));

	if (pc.vignette.w > 0.0) {
		float aspect = pc.size.x * pc.size.w;
		float r = length((uv - 0.5) * vec2(aspect, 1.0));
		c *= mix(vec3(1.0) - pc.vignette.w * (vec3(1.0) - pc.vignette.rgb), vec3(1.0), smoothstep(1.15, 0.25, r));
	}

	imageStore(color_image, p, vec4(c, col.a));
}
