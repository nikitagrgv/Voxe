using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Voxe.Render;

public class ShaderCompileErrorException(
	string message) : Exception(message);

public class ShaderLinkErrorException(
	string message) : Exception(message);

public class Shader : IDisposable
{
	private int _id;

	public bool IsValid => Id != 0;
	public int Id => _id;

	public Shader(string vertexShader, string fragmentShader)
	{
		int fragmentShaderId = CompileShader(ShaderType.FragmentShader, fragmentShader);
		int vertexShaderId = CompileShader(ShaderType.VertexShader, vertexShader);

		int programId = GL.CreateProgram();
		GL.AttachShader(programId, fragmentShaderId);
		GL.AttachShader(programId, vertexShaderId);
		GL.LinkProgram(programId);

		GL.DeleteShader(fragmentShaderId);
		GL.DeleteShader(vertexShaderId);

		if (GL.GetProgrami(programId, ProgramProperty.LinkStatus) == 0)
		{
			GL.GetProgramInfoLog(programId, out string info);
			GL.DeleteProgram(programId);
			throw new ShaderLinkErrorException(info);
		}

		_id = programId;
	}

	public void Bind()
	{
		Debug.Assert(IsValid);
		GLState.BindProgram(Id);
	}

	public bool IsBound
	{
		get
		{
			Debug.Assert(IsValid);
			return GLState.CurrentProgram == Id;
		}
	}

	public int GetUniformLocation(string name)
	{
		Bind();
		int location = GL.GetUniformLocation(Id, name);
		return location;
	}

	public void SetUniform(int location, float value)
	{
		Bind();
		GL.Uniform1f(location, value);
	}

	public void SetUniform(int location, Vector2 value)
	{
		Bind();
		GL.Uniform2f(location, value.X, value.Y);
	}

	public void SetUniform(int location, Vector3 value)
	{
		Bind();
		GL.Uniform3f(location, value.X, value.Y, value.Z);
	}

	public void SetUniform(int location, Vector4 value)
	{
		Bind();
		GL.Uniform4f(location, value.X, value.Y, value.Z, value.W);
	}

	public void SetUniform(int location, int value)
	{
		Bind();
		GL.Uniform1i(location, value);
	}

	public void SetUniform(int location, Matrix4 matrix)
	{
		Bind();
		GL.UniformMatrix4f(location, 1, false, ref matrix);
	}

	public void SetUniform(int location, Matrix3 matrix)
	{
		Bind();
		GL.UniformMatrix3f(location, 1, false, ref matrix);
	}

	public void Destroy()
	{
		if (!IsValid)
		{
			return;
		}

		GLState.DeleteProgram(_id);
		_id = 0;
	}

	public void Dispose()
	{
		Destroy();
		GC.SuppressFinalize(this);
	}

	private static int CompileShader(ShaderType type, string source)
	{
		int id = GL.CreateShader(type);
		GL.ShaderSource(id, source);
		GL.CompileShader(id);

		if (GL.GetShaderi(id, ShaderParameterName.CompileStatus) == 0)
		{
			GL.GetShaderInfoLog(id, out string info);
			GL.DeleteShader(id);
			throw new ShaderCompileErrorException(info);
		}

		return id;
	}
}