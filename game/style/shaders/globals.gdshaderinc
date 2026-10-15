// D0 declarations from ENG-PERF 1.5. Project defaults are placeholders for the owning systems.
#ifndef CAMPUS_GLOBALS_INCLUDED
#define CAMPUS_GLOBALS_INCLUDED

global uniform float clock;
global uniform float anim_time;
global uniform float fade_clock;
global uniform float step_rate;
global uniform vec4 season;
global uniform vec3 sun_dir;
global uniform vec3 moon_dir;
global uniform vec4 moon_lit;
global uniform vec3 key_dir;
global uniform float key_strength;
global uniform float shadow_strength;
global uniform float retreat_y;
global uniform vec4 wind;
global uniform float snow;
global uniform vec4 ground_amt;
global uniform float veil;
global uniform float stars;
global uniform vec4 fog;
global uniform vec4 fog_low;
global uniform vec4 medium;
global uniform vec4 medium_color : source_color;
global uniform float ui_night;
global uniform mat4 reflect_vp;
global uniform int reflect_lake;
global uniform vec4 incident;
global uniform vec4 incident_route;
global uniform vec4 weather_a;
global uniform vec4 weather_b;
global uniform vec4 weather_fog;
global uniform vec4 cloud;
global uniform vec4 lightning;
global uniform vec4 snow_fx;
global uniform vec3 overcast_dir;
global uniform vec4 quality;
global uniform vec4 map_rect;
global uniform float edge_fog;
global uniform vec4 shaft;
global uniform vec4 interior_light;
global uniform sampler2D palette : source_color, filter_nearest, repeat_disable;
global uniform sampler2D light_channels : filter_nearest, repeat_disable;
global uniform sampler2D window_override : filter_nearest, repeat_disable;
global uniform sampler2D lamp_grid : filter_nearest, repeat_disable;
global uniform sampler2D lamp_table : filter_nearest, repeat_disable;
global uniform sampler2D tree_species : filter_nearest, repeat_disable;
global uniform sampler2D mat_table : filter_nearest, repeat_disable;
global uniform sampler2D ground_mask : filter_nearest, repeat_disable;
global uniform sampler2D terrain_height : filter_nearest, repeat_disable;
global uniform sampler2D reflect_tex : filter_linear, repeat_disable;
global uniform sampler2D cloud_noise : filter_linear_mipmap, repeat_enable;
global uniform sampler2D precip_occ : filter_nearest, repeat_disable;
global uniform sampler2D refl_lamps : filter_nearest, repeat_disable;
global uniform sampler2D ground_mask2 : filter_nearest, repeat_disable;

vec2 map_uv(vec3 world_position) {
    return (world_position.xz - map_rect.xy) * map_rect.zw;
}

bool in_map(vec3 world_position) {
    vec2 uv = map_uv(world_position);
    return all(greaterThanEqual(uv, vec2(0.0))) && all(lessThanEqual(uv, vec2(1.0)));
}

#endif
