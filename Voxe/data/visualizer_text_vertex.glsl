#version 330 core
layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aUv;
layout (location = 2) in vec4 aColor;

out vec2 vUv;
out vec4 vColor;

uniform mat3 transform;

void main()
{
    vUv = aUv;
    vColor = aColor;
    gl_Position = vec4(transform * vec3(aPosition, 1.0), 1.0);
}