shader_type canvas_item;
uniform sampler2D existing_screen_texture: hint_screen_texture, filter_linear_mipmap;
uniform float frequency = 60;

void fragment() {
	vec2 new_uv = SCREEN_UV;
	new_uv.x += sin(new_uv.y * frequency + TIME)*.01;
	vec3 new_colors = textureLod(existing_screen_texture, new_uv, 0.0).rgb;
	COLOR.rgb = new_colors;
}