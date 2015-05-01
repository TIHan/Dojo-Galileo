#version 330 core

uniform mat4 uni_projection;
uniform mat4 uni_view;
uniform mat4 uni_model;
uniform vec3 uni_cameraPosition;

layout(location = 0) in vec3 vertexPosition_modelspace;
layout(location = 1) in vec3 vertexNormal_modelspace;

out vec3 normal_cameraspace;
out vec3 direction_cameraspace;
out vec3 vpos;

void main ()
{
	gl_Position = (uni_projection * uni_view * uni_model) * vec4 (vertexPosition_modelspace, 1);
	vpos = vertexPosition_modelspace;

	// Position of the vertex, in worldspace : M * position
	vec3 Position_worldspace = (uni_model * vec4(vertexPosition_modelspace,1)).xyz;
	
	// Vector that goes from the vertex to the camera, in camera space.
	// In camera space, the camera is at the origin (0,0,0).
	vec3 vertexPosition_cameraspace = ( uni_view * uni_model * vec4(vertexPosition_modelspace,1)).xyz;
	vec3 EyeDirection_cameraspace = vec3(0,0,0) - vertexPosition_cameraspace;

	// Vector that goes from the vertex to the light, in camera space. M is ommited because it's identity.
	vec3 LightPosition_cameraspace = ( uni_view * vec4(uni_cameraPosition,1)).xyz;
	direction_cameraspace = LightPosition_cameraspace + EyeDirection_cameraspace;
	
	// Normal of the the vertex, in camera space
	normal_cameraspace = ( uni_view * uni_model * vec4(vertexNormal_modelspace,0)).xyz; // Only correct if ModelMatrix does not scale the model ! Use its inverse transpose if not.
}