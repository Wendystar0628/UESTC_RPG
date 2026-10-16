shader_type spatial;
render_mode diffuse_lambert, specular_disabled;
// D0 instance-pool contract probes; these uniforms do not affect the stub's appearance.
instance uniform vec4 precip_d = vec4(0.0, 0.0, 0.0, 18.0);
instance uniform vec4 precip_v = vec4(0.0, -1.0, 0.0, 0.58);
instance uniform vec4 precip_k = vec4(0.0);
instance uniform float shadow_proxy = 0.0;
void fragment() {
    ALBEDO = COLOR.rgb;
    ROUGHNESS = 1.0;
}
