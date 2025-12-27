using OpenTK.Graphics.OpenGL;

namespace Voxe.Render;

public static class Renderer
{
	public static void DrawArrays(PrimitiveType type, int size, int offset = 0)
	{
		GL.DrawArrays(type, offset, size);
	}

	public static void DrawElements(PrimitiveType type, DrawElementsType elementsType, int size, int offset = 0)
	{
		GL.DrawElements(type, size, elementsType, offset);
	}
}