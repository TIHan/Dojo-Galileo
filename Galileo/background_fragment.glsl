#version 330 core

uniform sampler2D uni_texture;

in vec3 vpos;

out vec4 out_color;

// ALL HARD-CODED
void main ()
{
	float imageWidth = 2048;
	float imageHeight = 1024;
	float windowWidth = 1280;
	float windowHeight = 720;

	float x = imageWidth / windowWidth;
	float y = imageHeight / windowHeight;

    out_color = texture(uni_texture, vec2(1.5 + vpos.x / x, 1.5 - (vpos.y / y)));
}
