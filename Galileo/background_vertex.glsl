#version 330 core

uniform mat4 uni_projection;

layout(location = 0) in vec3 vertexPosition_modelspace;
out vec3 vpos;

void main ()
{
	gl_Position = uni_projection * vec4 (vertexPosition_modelspace, 1);
	vpos = vertexPosition_modelspace;
}