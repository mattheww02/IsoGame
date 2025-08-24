shader_type canvas_item;

uniform float zangle : hint_range(0.0, 1.5) = 0.3;      // slope of shadow
uniform vec2 light_dir = vec2(-1.0, -0.5);             // light direction in UV space
uniform float shadow_strength : hint_range(0.0, 0.5) = 0.2;
uniform int steps = 10;                                // how far to trace

void fragment() {
    // Sample the tile color and alpha (height)
    vec4 col = texture(TEXTURE, UV);
    float selfHeight = col.a;  // alpha channel encodes height

    bool in_shadow = false;

    for (int i = 1; i <= steps; i++) {
        float t = float(i) / float(steps);
        vec2 lookupUV = UV + light_dir * t * 0.05;  // trace along light direction
        lookupUV = clamp(lookupUV, vec2(0.0), vec2(1.0));
        float neighborHeight = texture(TEXTURE, lookupUV).a; // read alpha as height

        float projectedHeight = selfHeight + t * tan(zangle);

        // Only cast shadow if neighbor is taller
        if (neighborHeight > projectedHeight) {
            in_shadow = true;
            break;
        }
    }

    if (in_shadow) {
        COLOR = col * vec4(1.0 - shadow_strength,
                           1.0 - shadow_strength*0.75,
                           1.0 - shadow_strength*0.75,
                           1.0);
    } else {
        COLOR = col;
    }
}
