#[versions]

mask = "#define PASS_MASK";
composite = "#define PASS_COMPOSITE";

#[compute]

#version 450

VERSION_DEFINES

// post_outline.glsl — WP-05. Post-process outline in CompositorEffect POST_SKY (ART-STYLE §4.5, ENG-PERF §10.2).
//
// Two dispatches from this one file:
//   mask      : reads depth + normal_roughness (7-bit material ID in alpha) + mat_table row 0, writes a RG8 mask
//               R = line coverage (sub-pixel widths drawn as coverage),
//               G = material ID of the side the line takes its colour from (bits 0-6) + convex-lit flag (bit 7).
//   composite : isolated-speck removal (fewer than 3 line pixels in 3x3 -> dropped), +-0.5 px tent filter,
//               line colour = own base darkened + warm/cool tint + fog (ART-STYLE §4.2), mixed into the colour
//               buffer in place; convex bright line on the lit side (ART-STYLE §4.7); screen-space rim (interface).
//
// Placement: every line is one-sided and takes its colour from its own pixel, so no pixel ever reads a colour
// that another invocation is writing. Silhouettes sit on the nearer side (inside the foreground object, the
// "colour from the nearer side" of ART-STYLE §4.5); crease and material lines sit on the face turned more toward
// the camera (constant along an edge, so they cannot zigzag), parallel faces fall back to the lower material ID.
//
// Inputs from C#: inv_proj = RenderSceneData.GetCamProjection().Inverse() as is — that projection is already
// the device one (y flipped, reverse-Z remapped). Debug views (line_a.w): 1 mask, 2 material ID, 3 normal,
// 4 reconstructed world position, 5 fog amount (linear, for the fog parity test against fog.gdshaderinc).
//
// Edge criteria per neighbour (ENG-PERF §10.2):
//   depth  : neighbour farther than the centre's plane predicts by > 1.5 % (plus a normal-quantisation term),
//   normal : soft threshold, coverage = 1 - smoothstep(0.62, 0.78, dot(n_a, n_b)) (ART-STYLE §4.6),
//   ID     : material IDs differ (material boundary / object boundary).
// Widths follow ART-STYLE §4.3 by linear depth (outer 1.25 px at 1080p, crease x0.8, ID x0.6, 0 beyond 400 m),
// scaled by internal_height / 1080 with a minimum width for lines whose nominal width is >= 1 px.

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(set = 0, binding = 0, std140) uniform OutlineParams {
	mat4 inv_proj;      // (ndc.x, ndc.y with image rows top-down, raw reverse-Z depth, 1) -> view space
	mat4 inv_view;      // view -> world (camera transform)
	vec4 size;          // internal width, height, 1/width, 1/height
	vec4 line_a;        // x: line_darken, y: px scale (internal height / 1080), z: tent mix, w: debug view
	vec4 key_light;     // xyz: direction toward the key light, view space; w: key_strength
	vec4 rim_a;         // rgb: rim colour (linear); w: rim strength, 0 = off (screen-space rim interface)
	vec4 rim_b;         // xy: screen-space direction toward the key light (image space, y down); zw: reserved
	vec4 weather;       // x: far rain amount (reserved), y: lightning flash 0..1, zw: reserved
	// Copies of global shader parameters, written by C# from the same state that writes the globals
	// (compute shaders cannot read globals, ENG-PERF §1.5). Read through the macros below by fog.glslinc.
	vec4 g_fog;          // fog
	vec4 g_weather_fog;  // weather_fog
	vec4 g_map_rect;     // map_rect
	vec4 g_key_dir;      // xyz: key_dir, w: edge_fog
	vec4 g_medium;       // medium
	vec4 g_medium_color; // medium_color (linear)
	vec4 g_fog_low;      // fog_low
	vec4 g_misc;         // x: veil, yzw: reserved
} P;

layout(set = 0, binding = 1) uniform sampler2D depth_tex;
layout(set = 0, binding = 2) uniform sampler2D normal_roughness_tex;
layout(set = 0, binding = 3) uniform sampler2D mat_table;

#ifdef PASS_MASK
layout(rg8, set = 0, binding = 4) uniform restrict writeonly image2D line_mask;
#endif

#ifdef PASS_COMPOSITE
layout(rg8, set = 0, binding = 4) uniform restrict readonly image2D line_mask;
layout(rgba16f, set = 0, binding = 5) uniform restrict image2D color_image;
layout(set = 0, binding = 6) uniform sampler2D palette;   // global palette, sRGB view (linear on fetch)

#define fog (P.g_fog)
#define fog_low (P.g_fog_low)
#define weather_fog (P.g_weather_fog)
#define map_rect (P.g_map_rect)
#define key_dir (P.g_key_dir.xyz)
#define edge_fog (P.g_key_dir.w)
#define medium (P.g_medium)
#define medium_color (P.g_medium_color)
#define veil (P.g_misc.x)
// Godot re-imports a .glsl only when its own bytes change, not when an included file does: bump this stamp
// whenever fog.glslinc is re-synced, or the old fog stays compiled in.
// fog.glslinc mirror stamp: fog.gdshaderinc @ 92c31b4
#include "fog.glslinc"
#endif

// ---------------------------------------------------------------------------------------------------------------
// Shared helpers

const float LINE_FAR = 400.0;          // beyond this no line at all (ART-STYLE §4.3)
const float BRIGHT_FAR0 = 120.0;       // convex bright line fades out 120-150 m (ENG-PERF §10.2)
const float BRIGHT_FAR1 = 150.0;

// mat_table row 0 flags (B channel bits)
const uint LF_ERASE = 1u;              // no line drawn on this material
const uint LF_OUTER_ONLY = 2u;         // only the outer contour against other materials (card / mixed canopies)
const uint LF_INNER_SOFT = 4u;         // internal lines x0.22 when both sides carry this flag (cloud canopies)
const uint LF_NOLINE = 8u;             // never drawn; a nearer neighbour of this class counts as same depth

const float INNER_SOFT_GAIN = 0.22;

struct Surf {
	vec3 pos;     // view space
	vec3 nrm;     // view space
	float z;      // linear depth (m)
	uint id;      // 7-bit material ID
	bool sky;
};

struct LineCls {
	float width;      // width coefficient
	float tint;       // tint strength (default 0.22)
	uint flags;
	float fade_end;   // small-item fade end distance (m), 0 = none
};

ivec2 clamp_px(ivec2 c) {
	return clamp(c, ivec2(0), ivec2(P.size.xy) - 1);
}

vec3 view_pos(ivec2 c, float d) {
	vec2 ndc = (vec2(c) + 0.5) * P.size.zw * 2.0 - 1.0;
	vec4 v = P.inv_proj * vec4(ndc, d, 1.0);
	return v.xyz / v.w;
}

uint decode_id(float a) {
	// ROUGHNESS = id / 127 is stored as roughness * 127/255; dynamic objects store 1 - that (ART-STYLE §4.5).
	float r = a > 0.5 ? 1.0 - a : a;
	return uint(round(r * 255.0)) & 127u;
}

Surf fetch_surf(ivec2 c) {
	Surf s;
	float d = texelFetch(depth_tex, c, 0).r;
	s.sky = d <= 0.0;
	s.pos = view_pos(c, d);
	s.z = -s.pos.z;
	vec4 nr = texelFetch(normal_roughness_tex, c, 0);
	s.nrm = normalize(nr.xyz * 2.0 - 1.0);
	s.id = decode_id(nr.w);
	return s;
}

LineCls line_class(uint id) {
	vec4 t = texelFetch(mat_table, ivec2(int(id), 0), 0);
	LineCls l;
	l.width = t.r * 2.0;
	l.tint = t.g;
	l.flags = uint(round(t.b * 255.0));
	l.fade_end = t.a * 510.0;
	return l;
}

// mat_table row 3: G = bevel lift on the lit side, B = in shadow, both value/255 x 0.25 OKLab L.
float bevel_lift(uint id) {
	return texelFetch(mat_table, ivec2(int(id), 3), 0).g * 0.25;
}

vec3 linear_to_oklab(vec3 c) {
	float l = 0.4122214708 * c.r + 0.5363325363 * c.g + 0.0514459929 * c.b;
	float m = 0.2119034982 * c.r + 0.6806995451 * c.g + 0.1073969566 * c.b;
	float s = 0.0883024619 * c.r + 0.2817188376 * c.g + 0.6299787005 * c.b;
	l = pow(max(l, 0.0), 1.0 / 3.0);
	m = pow(max(m, 0.0), 1.0 / 3.0);
	s = pow(max(s, 0.0), 1.0 / 3.0);
	return vec3(
			0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
			1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
			0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s);
}

vec3 oklab_to_linear(vec3 c) {
	float l = c.x + 0.3963377774 * c.y + 0.2158037573 * c.z;
	float m = c.x - 0.1055613458 * c.y - 0.0638541728 * c.z;
	float s = c.x - 0.0894841775 * c.y - 1.2914855480 * c.z;
	l = l * l * l;
	m = m * m * m;
	s = s * s * s;
	return vec3(
			4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
			-1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
			-0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s);
}

float oklab_b(vec3 c) {
	return linear_to_oklab(c).z;
}

// ---------------------------------------------------------------------------------------------------------------
#ifdef PASS_MASK

// Nominal outer-contour width in px at 1080p by linear depth (ART-STYLE §4.3).
float outer_width_nominal(float z) {
	float w;
	if (z < 5.0) {
		w = mix(1.6, 1.25, z / 5.0);
	} else if (z < 30.0) {
		w = 1.25;
	} else if (z < 60.0) {
		w = mix(1.25, 1.1, (z - 30.0) / 30.0);
	} else if (z < 150.0) {
		w = mix(1.1, 0.75, (z - 60.0) / 90.0);
	} else {
		w = mix(0.75, 0.0, clamp((z - 150.0) / (LINE_FAR - 150.0), 0.0, 1.0));
	}
	return min(w, 2.0);   // close-up cap 2 px
}

// Nominal -> internal pixels. Lines whose nominal width reaches 1 px keep at least one internal pixel so that
// they stay a clear ~1 px line after FSR1 upscaling (SYS-GRAPHICS §2); the floor ramps in over 0.85-1.0 px.
float to_internal(float w_nom) {
	return max(w_nom * P.line_a.y, smoothstep(0.85, 1.0, w_nom));
}

// Coverage of a line of width w (px) on a pixel whose centre is d px from the edge (1 px box filter).
float box_cov(float w, float d) {
	return clamp(w + 0.5 - d, 0.0, 1.0);
}

uint hash_u(uvec4 v) {
	uint h = v.x * 0x8da6b343u ^ v.y * 0xd8163841u ^ v.z * 0xcb1ab31fu ^ v.w * 0x165667b1u;
	h ^= h >> 15;
	h *= 0x2c1b3c6du;
	h ^= h >> 12;
	h *= 0x297a2d39u;
	h ^= h >> 15;
	return h;
}

float hash_f(ivec3 cell, uint seed) {
	return float(hash_u(uvec4(uvec3(cell + ivec3(1 << 20)), seed)) & 0xffffu) / 65535.0;
}

// Line weight variation (ART-STYLE §4.4): smooth value noise on a 0.4 m world grid, seeded by material ID,
// so it is anchored to geometry and does not swim with the camera or change over time.
float weight_noise(vec3 world, uint id) {
	vec3 g = world / 0.4;
	ivec3 i = ivec3(floor(g));
	vec3 f = fract(g);
	f = f * f * (3.0 - 2.0 * f);
	float n000 = hash_f(i + ivec3(0, 0, 0), id);
	float n100 = hash_f(i + ivec3(1, 0, 0), id);
	float n010 = hash_f(i + ivec3(0, 1, 0), id);
	float n110 = hash_f(i + ivec3(1, 1, 0), id);
	float n001 = hash_f(i + ivec3(0, 0, 1), id);
	float n101 = hash_f(i + ivec3(1, 0, 1), id);
	float n011 = hash_f(i + ivec3(0, 1, 1), id);
	float n111 = hash_f(i + ivec3(1, 1, 1), id);
	return mix(mix(mix(n000, n100, f.x), mix(n010, n110, f.x), f.y),
			mix(mix(n001, n101, f.x), mix(n011, n111, f.x), f.y), f.z);
}

const int RING1 = 8;
const ivec2 RING1_OFS[8] = ivec2[8](ivec2(1, 0), ivec2(-1, 0), ivec2(0, 1), ivec2(0, -1),
		ivec2(1, 1), ivec2(-1, 1), ivec2(1, -1), ivec2(-1, -1));
// Diagonal-only hits come from raster stair corners next to an axis-detected line, whose true distance to the
// edge is >= ~1.2 px; counting them at 0.71 px grows "ticks" under near-horizontal edges. They only add width
// to lines wider than one pixel.
const float RING1_D[8] = float[8](0.5, 0.5, 0.5, 0.5, 1.5, 1.5, 1.5, 1.5);
const int RING2 = 4;
const ivec2 RING2_OFS[4] = ivec2[4](ivec2(2, 0), ivec2(-2, 0), ivec2(0, 2), ivec2(0, -2));
const float RING2_D = 1.5;

// Edge strengths between centre s0 and neighbour at c: x depth (silhouette), y normal crease, z material ID.
// All are already restricted to the case where the centre is the nearer side. w: 1 if convex and the centre is
// the more lit face (candidate for the bright line), else 0.
vec4 edge_strengths(Surf s0, LineCls l0, ivec2 c) {
	Surf q = fetch_surf(c);
	if (q.sky) {
		// Background is sky: a pure silhouette.
		return vec4(1.0, 0.0, 0.0, 0.0);
	}
	LineCls lq = q.id == s0.id ? l0 : line_class(q.id);
	bool q_noline = (lq.flags & LF_NOLINE) != 0u;
	if (q_noline && q.z <= s0.z) {
		// A nearer "no line" neighbour (petal, cloud card, particle) counts as same depth: nothing is outlined.
		return vec4(0.0);
	}

	// Depth: compare the neighbour with the centre's plane along the neighbour's view ray.
	// s = |predicted| / |actual|; rel = 1 - s > 0 means the neighbour lies behind the centre's plane.
	float pn0 = dot(s0.pos, s0.nrm);
	float pnq = dot(q.pos, s0.nrm);
	float rel = -1.0;
	if (pn0 * pnq > 0.0) {
		rel = 1.0 - pn0 / pnq;
	}
	// 8-bit normals make the prediction noisy where the plane is seen edge-on; widen the threshold by the
	// expected error (normal quantisation ~0.004) so grazing ground does not break into false lines.
	float slack = 2.0 * 0.004 * length(q.pos - s0.pos) / max(abs(pn0), 1e-3);
	float t0 = 0.012 + slack;
	float e_depth = smoothstep(t0, t0 + 0.008, rel) * step(s0.z, q.z);

	// Side for crease and ID lines (one-sided, full width): the face turned more toward the camera draws, which is
	// constant along an edge and so cannot zigzag; parallel faces (an ID boundary on one plane, a protruding
	// strip) fall back to the lower material ID. Silhouettes always sit on the nearer side (e_depth above).
	float f0 = dot(s0.nrm, -normalize(s0.pos));
	float fq = dot(q.nrm, -normalize(q.pos));
	float side;
	if (abs(f0 - fq) > 0.02) {
		side = clamp(0.5 + (f0 - fq) / 0.04, 0.0, 1.0);
	} else {
		side = s0.id < q.id ? 1.0 : (s0.id > q.id ? 0.0 : 0.5);
	}

	float e_crease = 0.0;
	float e_id = 0.0;
	float convex_lit = 0.0;
	bool same_id = q.id == s0.id;
	bool outer_only = (l0.flags & LF_OUTER_ONLY) != 0u;
	if (!q_noline) {
		if (!(outer_only && same_id)) {
			float nd = dot(s0.nrm, q.nrm);
			e_crease = (1.0 - smoothstep(0.62, 0.78, nd));
			if ((l0.flags & LF_INNER_SOFT) != 0u && (lq.flags & LF_INNER_SOFT) != 0u) {
				e_crease *= INNER_SOFT_GAIN;
			}
			// Convex bright line candidate (ART-STYLE §4.7): convex when (P_b - P_a) . N_a < 0, and the centre
			// is the face turned more toward the key light.
			if (e_crease > 0.25 && s0.z < BRIGHT_FAR1) {
				float lp = dot(s0.nrm, P.key_light.xyz);
				float lq_dot = dot(q.nrm, P.key_light.xyz);
				bool convex = dot(q.pos - s0.pos, s0.nrm) < 0.0;
				if (convex && lp > 0.0 && lp >= lq_dot) {
					convex_lit = 1.0;
				}
			}
			e_crease *= side;
		}
		if (!same_id && !outer_only) {
			e_id = side;
		}
	} else {
		// Farther "no line" neighbour (water behind a wall, a cloud card behind a roof): keep the silhouette
		// and the object boundary on this side (the no-line side never draws), skip creases.
		e_id = 1.0;
	}
	if (outer_only && same_id) {
		// Card / mixed canopies: cards of the same canopy never outline each other.
		e_depth = 0.0;
	}
	return vec4(e_depth, e_crease, e_id, convex_lit);
}

void main() {
	ivec2 c = ivec2(gl_GlobalInvocationID.xy);
	if (any(greaterThanEqual(c, ivec2(P.size.xy)))) {
		return;
	}
	Surf s0 = fetch_surf(c);
	// Early out: sky and beyond 400 m (ENG-PERF §10.2). Not by tilt-shift blur circle.
	if (s0.sky || s0.z >= LINE_FAR) {
		imageStore(line_mask, c, vec4(0.0));
		return;
	}
	LineCls l0 = line_class(s0.id);
	float id_bits = float(s0.id);
	if ((l0.flags & (LF_ERASE | LF_NOLINE)) != 0u) {
		imageStore(line_mask, c, vec4(0.0, id_bits / 255.0, 0.0, 0.0));
		return;
	}

	vec4 e1[8];
	float any_edge = 0.0;
	float convex_lit = 0.0;
	for (int k = 0; k < RING1; k++) {
		e1[k] = edge_strengths(s0, l0, clamp_px(c + RING1_OFS[k]));
		any_edge = max(any_edge, max(e1[k].x, max(e1[k].y, e1[k].z)));
		convex_lit = max(convex_lit, e1[k].w);
	}

	float bright_ok = bevel_lift(s0.id) > 0.0 ? 1.0 : 0.0;
	uint flag_bits = (convex_lit * bright_ok > 0.5) ? 128u : 0u;
	float g_out = float(s0.id | flag_bits) / 255.0;

	if (any_edge <= 0.0) {
		imageStore(line_mask, c, vec4(0.0, g_out, 0.0, 0.0));
		return;
	}

	// Width per line kind at this depth, with weight variation and the material's width coefficient.
	vec3 world = (P.inv_view * vec4(s0.pos, 1.0)).xyz;
	float n = weight_noise(world, s0.id);
	float strength = mix(0.85, 1.0, n);
	float wmul = mix(0.9, 1.1, n) * l0.width;
	float w_nom = outer_width_nominal(s0.z) * wmul;
	float w_outer = to_internal(min(w_nom, 2.0));
	float w_crease = to_internal(min(w_nom * 0.8, 2.0));
	float w_id = to_internal(min(w_nom * 0.6, 2.0));

	float cov = 0.0;
	for (int k = 0; k < RING1; k++) {
		float d = RING1_D[k];
		cov = max(cov, e1[k].x * box_cov(w_outer, d));
		cov = max(cov, e1[k].y * box_cov(w_crease, d));
		cov = max(cov, e1[k].z * box_cov(w_id, d));
	}
	// Second ring only where a line is wider than one internal pixel (close range, high internal resolution).
	if (max(w_outer, max(w_crease, w_id)) > 1.0) {
		for (int k = 0; k < RING2; k++) {
			vec4 e = edge_strengths(s0, l0, clamp_px(c + RING2_OFS[k]));
			cov = max(cov, e.x * box_cov(w_outer, RING2_D));
			cov = max(cov, e.y * box_cov(w_crease, RING2_D));
			cov = max(cov, e.z * box_cov(w_id, RING2_D));
		}
	}

	// Small-item fade (lines go before the geometry is culled, ART-STYLE §4.1).
	if (l0.fade_end > 0.0) {
		cov *= 1.0 - smoothstep(0.42 * l0.fade_end, l0.fade_end, s0.z);
	}
	cov *= strength;

	imageStore(line_mask, c, vec4(cov, g_out, 0.0, 0.0));
}

#endif // PASS_MASK

// ---------------------------------------------------------------------------------------------------------------
#ifdef PASS_COMPOSITE

const ivec2 N3[9] = ivec2[9](ivec2(-1, -1), ivec2(0, -1), ivec2(1, -1),
		ivec2(-1, 0), ivec2(0, 0), ivec2(1, 0),
		ivec2(-1, 1), ivec2(0, 1), ivec2(1, 1));
// Separable +-0.5 px tent through bilinear taps: 1D weights (1/8, 3/4, 1/8).
const float TENT[9] = float[9](1.0 / 64.0, 6.0 / 64.0, 1.0 / 64.0,
		6.0 / 64.0, 36.0 / 64.0, 6.0 / 64.0,
		1.0 / 64.0, 6.0 / 64.0, 1.0 / 64.0);
const float LINE_PIXEL = 0.04;   // a mask value counted as a "line pixel" for speck removal (thin far lines are
                                 // drawn as low coverage and must still count, or they vanish whole)

vec3 line_colour(vec3 base, uint id, ivec2 at) {
	LineCls l = line_class(id);
	base = min(base, vec3(1.0));
	float bl = dot(base, vec3(0.2126, 0.7152, 0.0722));
	float warm = smoothstep(-0.01, 0.03, oklab_b(base));   // warm base -> warm brown, cool -> blue-grey
	vec3 tint = mix(pal_cur(PAL_LINE_COOL), pal_cur(PAL_LINE_WARM), warm);   // line.cool / line.warm
	float darken = P.line_a.x * (1.0 - 0.3 * P.weather.y);   // lightning flash: line_darken x 0.7 (SYS-WEATHER §5)
	vec3 lc = mix(base, vec3(bl), 0.35) * darken;
	lc = mix(lc, tint, l.tint);
	// Toward the fog with distance, same fog function as the materials (ART-STYLE §4.2).
	float d = texelFetch(depth_tex, at, 0).r;
	vec3 vp = view_pos(at, d);
	vec3 wp = (P.inv_view * vec4(vp, 1.0)).xyz;
	vec4 f = fog_eval(wp, P.inv_view[3].xyz);
	return mix(lc, f.rgb, f.a);
}

// Screen-space rim on the backlit side of silhouettes (ART-STYLE §5.4). Interface for the mainline: off while
// rim strength is 0; the full rule set (night silver rim, no rim in the shadowless blue hour, x1.3 after rain)
// is driven entirely by the C# side through rim_a / rim_b.
vec3 rim_term(ivec2 c, Surf s) {
	if (P.rim_a.w <= 0.0 || s.sky || s.z >= LINE_FAR) {
		return vec3(0.0);
	}
	float nl = dot(s.nrm, P.key_light.xyz);
	if (nl >= 0.0) {
		return vec3(0.0);
	}
	// Probe 2.5 px toward the light: the outermost pixel carries the dark contour, the rim sits just inside it.
	vec2 off = P.rim_b.xy * 2.5 * P.line_a.y;
	ivec2 q = clamp_px(ivec2(floor(vec2(c) + 0.5 + off)));
	float dq = texelFetch(depth_tex, q, 0).r;
	bool far = dq <= 0.0;
	if (!far) {
		far = -view_pos(q, dq).z > s.z * 1.5 + 2.0;
	}
	if (!far) {
		return vec3(0.0);
	}
	vec3 wp = (P.inv_view * vec4(s.pos, 1.0)).xyz;
	float fog_a = fog_eval(wp, P.inv_view[3].xyz).a;
	return P.rim_a.rgb * P.rim_a.w * smoothstep(0.0, -0.25, nl) * P.key_light.w * (1.0 - fog_a);
}

vec3 debug_id_colour(uint id) {
	uint h = id * 2654435761u;
	return vec3(float((h >> 8) & 255u), float((h >> 16) & 255u), float((h >> 24) & 255u)) / 255.0;
}

void main() {
	ivec2 c = ivec2(gl_GlobalInvocationID.xy);
	if (any(greaterThanEqual(c, ivec2(P.size.xy)))) {
		return;
	}

	float m[9];
	uint g[9];
	int count = 0;
	float tent = 0.0;
	float best = 0.0;
	int best_k = 4;
	bool any_flag = false;
	for (int k = 0; k < 9; k++) {
		vec2 mv = imageLoad(line_mask, clamp_px(c + N3[k])).rg;
		m[k] = mv.r;
		g[k] = uint(round(mv.g * 255.0));
		count += m[k] > LINE_PIXEL ? 1 : 0;
		tent += TENT[k] * m[k];
		any_flag = any_flag || (g[k] & 128u) != 0u;
		if (k != 4 && m[k] > best) {
			best = m[k];
			best_k = k;
		}
	}
	float m0 = m[4];

	// Isolated speck removal: fewer than 3 line pixels in the 3x3 block are dropped.
	float cov;
	ivec2 src = c;   // pixel whose depth / ID the line colour is taken from
	uint src_id = g[4] & 127u;
	if (m0 > 0.0) {
		cov = count >= 3 ? mix(m0, tent, P.line_a.z) : 0.0;
	} else if (count >= 2) {
		// Skirt pixel next to a line: only the tent's spill, coloured from its own base but fogged and tinted
		// by the line pixel it spills from.
		cov = tent * P.line_a.z;
		src = clamp_px(c + N3[best_k]);
		src_id = g[best_k] & 127u;
	} else {
		cov = 0.0;
	}

	int debug_view = int(P.line_a.w + 0.5);
	if (debug_view != 0) {
		vec3 dbg;
		Surf s = fetch_surf(c);
		if (debug_view == 1) {
			dbg = mix(vec3(1.0), vec3(0.0), cov);
			if ((g[4] & 128u) != 0u) {
				dbg = mix(dbg, vec3(1.0, 0.6, 0.0), 0.8);
			}
		} else if (debug_view == 2) {
			dbg = s.sky ? vec3(0.0) : debug_id_colour(s.id);
		} else if (debug_view == 3) {
			dbg = s.sky ? vec3(0.0) : s.nrm * 0.5 + 0.5;
		} else if (debug_view == 4) {
			vec3 wp = (P.inv_view * vec4(s.pos, 1.0)).xyz;
			dbg = s.sky ? vec3(0.0) : vec3(fract(wp.y + 0.5), fract(wp.x * 0.1), fract(wp.z * 0.1));
		} else {
			vec3 wp = (P.inv_view * vec4(s.pos, 1.0)).xyz;
			dbg = s.sky ? vec3(1.0, 0.0, 0.0) : vec3(fog_eval(wp, P.inv_view[3].xyz).a);
		}
		// Squared so debug colours read roughly as sRGB on screen; the fog view stays linear for parity tests.
		imageStore(color_image, c, vec4(debug_view == 5 ? dbg : dbg * dbg, 1.0));
		return;
	}

	bool rim_on = P.rim_a.w > 0.0;
	if (cov < 1.0 / 255.0 && !any_flag && !rim_on) {
		return;
	}

	vec4 col = imageLoad(color_image, c);
	vec3 rgb = col.rgb;

	// Convex bright line on the lit side, next to the dark crease line (ART-STYLE §4.7, ENG-PERF §10.2).
	if (any_flag && m0 < 0.5) {
		bool bright = (g[4] & 128u) != 0u;
		Surf s = fetch_surf(c);
		if (!bright) {
			// The dark line sits on a flagged pixel of this same face: put the bright line just inside it.
			const int AX[4] = int[4](1, 3, 5, 7);
			for (int i = 0; i < 4; i++) {
				int k = AX[i];
				if ((g[k] & 128u) != 0u && m[k] >= 0.5) {
					vec3 nk = normalize(texelFetch(normal_roughness_tex, clamp_px(c + N3[k]), 0).xyz * 2.0 - 1.0);
					if (dot(nk, s.nrm) > 0.95) {
						bright = true;
						break;
					}
				}
			}
		}
		if (bright && !s.sky && dot(s.nrm, P.key_light.xyz) > 0.0) {
			// Fades with distance and sinks into the fog like the surface it sits on (also under the veil).
			vec3 wp = (P.inv_view * vec4(s.pos, 1.0)).xyz;
			float fog_a = fog_eval(wp, P.inv_view[3].xyz).a;
			float lift = bevel_lift(s.id) * P.key_light.w * (1.0 - smoothstep(BRIGHT_FAR0, BRIGHT_FAR1, s.z)) * (1.0 - fog_a);
			if (lift > 0.0) {
				vec3 lab = linear_to_oklab(max(rgb, vec3(0.0)));
				lab.x += lift;
				rgb = max(oklab_to_linear(lab), vec3(0.0));
			}
		}
	}

	if (rim_on && m0 < 0.5) {
		rgb += rim_term(c, fetch_surf(c));
	}

	if (cov >= 1.0 / 255.0) {
		vec3 lc = line_colour(rgb, src_id, src);
		rgb = mix(rgb, lc, clamp(cov, 0.0, 1.0));
	}

	imageStore(color_image, c, vec4(rgb, col.a));
}

#endif // PASS_COMPOSITE
