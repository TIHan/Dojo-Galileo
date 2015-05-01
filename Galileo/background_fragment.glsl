#version 330

uniform sampler2D uni_texture;

out vec4 out_color;

void main ()
{
    out_color = texture(uni_texture, vec2(0));
}
