using System.Diagnostics;
using OpenTK.Graphics.OpenGLES2;

namespace Voxe.Render;

internal static class GLState
{
	private static int _currentProgram = 0;

	private struct TextureState
	{
	}

	public static void Init()
	{
		GL.UseProgram(0);
		_currentProgram = 0;
	}

	public static int CurrentProgram => _currentProgram;

	public static void BindProgram(int id)
	{
		Debug.Assert(GL.IsProgram(id));
		if (_currentProgram == id)
			return;

		_currentProgram = id;
		GL.UseProgram(id);
	}

	public static void UnbindProgram()
	{
		if (_currentProgram == 0)
			return;

		_currentProgram = 0;
		GL.UseProgram(0);
	}

	public static void DeleteProgram(int id)
	{
		Debug.Assert(GL.IsProgram(id));
		if (_currentProgram == id)
			UnbindProgram();
		GL.DeleteProgram(id);
	}
}