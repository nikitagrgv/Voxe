using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Voxe.Render;

namespace Voxe;

public class ChunkMesh : IDisposable
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Vertex
	{
		public Vertex(Vector3 position, Vector2 uv)
		{
			Position = position;
			Uv = uv;
		}

		public Vector3 Position;
		public Vector2 Uv;
	}

	private static readonly VertexArrayBuilder VaoBuilder = VertexArrayBuilder.FromStruct<Vertex>();

	private int _vaoId;
	private int _vboId;
	private int _eboId;

	private int _numVertices;
	private int _numIndices;

	public bool IsValid
	{
		get
		{
			bool isValid = _vaoId != 0;
			Debug.Assert(isValid == (_vboId != 0) && isValid == (_eboId != 0));
			Debug.Assert(isValid || (_numIndices == 0 && _numVertices == 0));
			return isValid;
		}
	}

	public int BuffersMemoryUsage
	{
		get
		{
			int bytesVertices = Utils.GetSizeOfType<Vertex>() * _numVertices;
			int bytesIndices = sizeof(uint) * _numIndices;
			return bytesVertices + bytesIndices;
		}
	}

	public ChunkMesh()
	{
	}

	public void SetData(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices, bool dynamic = false)
	{
		EnsureInitialized();
		Bind();

		BufferUsage usage = dynamic ? BufferUsage.DynamicDraw : BufferUsage.StaticDraw;
		GL.BufferData(BufferTarget.ArrayBuffer, vertices.GetSizeInBytes(), vertices, usage);
		GL.BufferData(BufferTarget.ElementArrayBuffer, indices.GetSizeInBytes(), indices, usage);

		_numIndices = indices.Length;
		_numVertices = vertices.Length;
	}

	public void Render()
	{
		if (!IsValid || _numIndices == 0)
			return;
		Debug.Assert(_numVertices != 0);

		Bind();

		Renderer.DrawElements(PrimitiveType.Triangles, DrawElementsType.UnsignedInt, _numIndices);
		Stat.AddRenderedIndices((ulong)_numIndices);
	}

	public void Destroy()
	{
		if (!IsValid)
			return;

		GL.DeleteVertexArray(_vaoId);
		GL.DeleteBuffer(_vboId);
		GL.DeleteBuffer(_eboId);

		_vaoId = 0;
		_vboId = 0;
		_eboId = 0;
		_numIndices = 0;
	}

	private void EnsureInitialized()
	{
		if (IsValid)
			return;

		_vaoId = GL.GenVertexArray();
		_vboId = GL.GenBuffer();
		_eboId = GL.GenBuffer();

		Bind();
		VaoBuilder.Apply();
	}

	private void Bind()
	{
		Debug.Assert(IsValid);
		GL.BindVertexArray(_vaoId);
		GL.BindBuffer(BufferTarget.ArrayBuffer, _vboId);
		GL.BindBuffer(BufferTarget.ElementArrayBuffer, _eboId);
	}

	public void Dispose()
	{
		Destroy();
		GC.SuppressFinalize(this);
	}
}