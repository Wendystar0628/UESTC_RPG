shader_type spatial;
render_mode diffuse_lambert, specular_disabled;
void fragment() {
    ALBEDO = COLOR.rgb;
    ROUGHNESS = 1.0;
}
