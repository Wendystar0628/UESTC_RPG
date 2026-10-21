#[versions]

mask = "#define PASS_MASK";
composite = "#define PASS_COMPOSITE";
lut = "#define PASS_LUT";
finish = "#define PASS_FINISH";

#[compute]

#version 450

VERSION_DEFINES

// post_outline.glsl — game/kernel/post/. The one RDShaderFile of the project: every game/kernel/post/ compute kernel is a version here.
// Godot 4.7.2 crashes now and then (0xC0000005) when `--import` imports two or more .glsl files on parallel
// threads; with a single .glsl file no two shader-file imports ever overlap. Versions:
//   mask, composite : the outline in CompositorEffect POST_SKY (below),
//   lut             : the runtime colour-grading LUT (section PASS_LUT),
//   finish  : the compose dispatch in CompositorEffect POST_TRANSPARENT (section PASS_FINISH).
//
// Outline (ART-STYLE §4.5, ENG-PERF), two dispatches:
//   mask      : reads depth + normal_roughness (7-bit material ID in alpha) + mat_table row 0, writes a RG8 mask
//               R = line coverage (sub-pixel widths drawn as coverage),
//               G = material ID of the side the line takes its colour from (bits 0-6) + convex-lit flag (bit 7).
//   composite : fragment filter (3x3 count and 5x5 ring, below),
//               line colour = own base darkened + warm/cool tint + fog (ART-STYLE §4.2), mixed into the colour
//               buffer in place; convex bright line on the lit side (ART-STYLE).
//
// Placement: every line is one-sided and takes its colour from its own pixel, so no pixel ever reads a colour
// that another invocation is writing. Silhouettes sit on the nearer side (inside the foreground object, the
// "colour from the nearer side" of ART-STYLE §4.5); crease and material lines sit on the face turned more toward
// the camera (constant along an edge, so they cannot zigzag), parallel faces use the lower material ID.
//
// Inputs from C#: inv_proj = RenderSceneData.GetCamProjection().Inverse() as is — that projection is already
// the device one (y flipped, reverse-Z remapped).
//
// Edge criteria per neighbour (ENG-PERF):
//   depth  : neighbour farther than the centre's plane predicts by > 1.5 % (plus a normal-quantisation term),
//   normal : soft threshold, coverage = 1 - smoothstep(0.62, 0.78, dot(n_a, n_b)) (ART-STYLE),
//   ID     : material IDs differ (material boundary / object boundary).
// Widths follow ART-STYLE by linear depth (outer 1.25 px at 1080p, crease x0.8, ID x0.6, 0 beyond 400 m),
// scaled by internal_height / 1080 with a minimum width for lines whose nominal width is >= 1 px.
//
// Ported from the reference scenes' line passes (render/太阳雨中的小渔港, edge pass S8 and line pass _8; the same code in
// render/被自然收回的街角), so far-away thin parts draw clean lines or none instead of dashes and specks:
//   speck rule      : a silhouette pixel draws only where the occluder continues along the edge on at least one side,
//   thin-part fade  : the reference fades a part's ink by its projected size (smoothstep(4, 9) px of its bounding
//                     radius, per object); with no per-object data here the mask measures the foreground's thickness
//                     across a silhouette, and the width of a part between two other materials, and fades the lines
//                     of a part about one pixel thick toward THIN_KEEP (steady along the edge, so no dashes),
//   fragment filter : a line pixel keeps its ink only if the line leaves the 5x5 window around it (the reference
//                     counts line pixels in that window; see the composite pass for why the ring is counted here);
//                     the convex bright line (ART-STYLE §4.7) follows the same rule on its own flags,
//   foliage depth   : card canopies outline clump against clump by the reference's foliage depth step (0.07 of 1/z
//                     there, FOLIAGE_DEPTH_REL here, on both sides of the pair), fading out with distance; the speck
//                     rule and the filters above keep the crowns clean.

// ---- Tuning (max-110 110b; values for the main session to set, all in one place)
const float THIN_KEEP = 0.45;       // ink left on a thin part (1 = no fade, 0 = thin parts draw no line)
const float THIN_FULL_PX = 1.0;     // foreground thickness (1080p px) at or below which a part counts as fully thin
const float THIN_NONE_PX = 2.0;     // thickness (1080p px) from which a part is outlined normally
const float THIN_DEPTH_REL = 0.02;  // relative depth step behind a part's plane that counts as background
const float STRIP_FULL_PX = 2.0;    // width (1080p px) of a part between two other materials at or below which its
const float STRIP_NONE_PX = 3.0;    // material and crease lines fade fully, and from which they are drawn normally
const float BRIGHT_RING_LO = 0.5;   // convex bright line: flagged pixels on the 5x5 ring for no lift ...
const float BRIGHT_RING_HI = 2.0;   // ... and for the full lift (a lone flagged bevel pixel would flash as a dash)
const float FOLIAGE_DEPTH_REL = 0.05; // card canopies: depth step between clumps, as a share of the nearer 1/z
const float FOLIAGE_INNER_GAIN = 1.0; // ink of those inner clump lines (the outer contour is always full)
const float FOLIAGE_FADE0 = 30.0;     // inner clump lines fade out between these distances (m), so far crowns
const float FOLIAGE_FADE1 = 80.0;     // read by their contour and do not fill with scribbles
const float FRAG_RING_LO = 0.5;     // fragment filter: line pixels on the 5x5 window's outer ring for no ink ...
const float FRAG_RING_HI = 2.0;     // ... and for full ink (a line through the window marks >= 2 of them)

#if defined(PASS_MASK) || defined(PASS_COMPOSITE)
// RDShaderFile expands only includes written in this main file, not nested includes.
#include "../shaders/style_consts.gdshaderinc"

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(set = 0, binding = 0, std140) uniform OutlineParams {
	mat4 inv_proj;      // (ndc.x, ndc.y with image rows top-down, raw reverse-Z depth, 1) -> view space
	mat4 inv_view;      // view -> world (camera transform)
	vec4 size;          // internal width, height, 1/width, 1/height
	vec4 line_a;        // x: line_darken, y: px scale (internal height / 1080), z: lightning flash, w: padding
	vec4 key_light;     // xyz: direction toward the key light, view space; w: key_strength
	// Copies of global shader parameters, written by C# from the same state that writes the globals
	// (compute shaders cannot read globals, ENG-PERF). Read through the macros below by fog.glslinc.
	vec4 g_fog;          // fog
	vec4 g_weather_fog;  // weather_fog
	vec4 g_map_rect;     // map_rect
	vec4 g_medium;       // medium
	vec4 g_medium_color; // xyz: medium_color (linear), w: edge_fog
	vec4 g_fog_low;      // fog_low
	vec4 g_sun_dir;      // xyz: sun_dir (real sun direction, unquantised), w: season_atmosphere
	vec4 g_moon_dir;     // xyz: moon_dir
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
#define edge_fog (P.g_medium_color.w)
#define sun_dir (P.g_sun_dir.xyz)
#define season_atmosphere (P.g_sun_dir.w)
#define moon_dir (P.g_moon_dir.xyz)
#define medium (P.g_medium)
#define medium_color (P.g_medium_color)
#include "fog.glslinc"
#endif

#include "post_outline_shared.glslinc"
#include "post_outline_mask.glslinc"
#include "post_outline_composite.glslinc"
#endif // PASS_MASK || PASS_COMPOSITE

#include "post_outline_lut.glslinc"
#include "post_outline_finish.glslinc"
