shader_type canvas_item;

uniform float tile_height : hint_range(0.0, 1.0) = 1.0;

void fragment() {
    vec4 texColor = texture(TEXTURE, UV);
    COLOR = vec4(texColor.rgb, tile_height); // alpha = height
}
