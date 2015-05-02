#version 330 core

uniform vec4 uni_color;
uniform sampler2D uni_texture;

in vec3 normal_cameraspace;
in vec3 direction_cameraspace;
in vec3 vpos;

out vec4 out_color;

void main ()
{
	//vec2 test = vec2(0.5, 0.5);
	//texture(uni_texture, test).rgba;

	vec3 n = normalize (normal_cameraspace);
	vec3 l = normalize (direction_cameraspace);
	float cosTheta = clamp (dot (n, l), 0, 1);


	float torad = 0.0174532925;
	float xTheta = cos (90 * torad);
	float yTheta = sin (90 * torad);

	vec4 texCoords = vec4(vpos, 1);
	vec2 longitudeLatitude = vec2((atan(texCoords.y, texCoords.x) / 3.1415926 + 1.0) * 0.5,
                                  (asin(texCoords.z) / 3.1415926 + 0.5));

    out_color =  texture(uni_texture, vec2(longitudeLatitude.x, -longitudeLatitude.y)) * cosTheta;//uni_color * cosTheta;
}