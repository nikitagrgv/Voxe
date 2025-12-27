#version 330 core
out vec4 FragColor;

in vec2 vUv;
in vec4 vColor;

uniform sampler2D uTexture;

void main()
{
    vec4 color = texture(uTexture, vUv);
    FragColor = color * vColor;
}