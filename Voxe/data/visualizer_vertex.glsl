#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec4 aColor;

out vec4 vColor;

uniform mat4 viewProj;

void main()
{
    vColor = aColor;
    gl_Position = viewProj * vec4(aPosition, 1.0);
}