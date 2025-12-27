using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Voxe.Math;

namespace Voxe.Render;

public static class Visualizer
{
	private struct Lines
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct Vertex
		{
			public Vector3 Position;
			public Vector4 Color;
		}

		public Shader? Shader = null;
		public int ViewProjLocation = 0;
		public int Vbo = 0;
		public int Vao = 0;
		public readonly List<Vertex> VerticesDepthTest = new();
		public readonly List<Vertex> VerticesNoDepthTest = new();

		public Lines()
		{
		}
	}

	private struct Texts
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct Vertex
		{
			public Vector3 Position;
			public Vector2 Uv;
			public Vector4 Color;
		}

		public Font? Font = null;
		public Shader? Shader = null;
		public int TransformLocation = 0;
		public int Vbo = 0;
		public int Vao = 0;
		public readonly List<Vertex> Vertices = new();

		public Texts()
		{
		}
	}

	private static Lines _lines = new();
	private static Texts _texts = new();

	public static void Initialize()
	{
		InitLines();
		InitTexts();
	}

	private static void InitLines()
	{
		string vertexShaderSource = FileSystem.ReadTextFile("visualizer_vertex.glsl");
		string fragmentShaderSource = FileSystem.ReadTextFile("visualizer_fragment.glsl");
		_lines.Shader = new Shader(vertexShaderSource, fragmentShaderSource);
		_lines.ViewProjLocation = _lines.Shader.GetUniformLocation("viewProj");
		_lines.Vbo = GL.GenBuffer();
		_lines.Vao = GL.GenVertexArray();

		GL.BindVertexArray(_lines.Vao);
		GL.BindBuffer(BufferTarget.ArrayBuffer, _lines.Vbo);
		VertexArrayBuilder.FromStruct<Lines.Vertex>().Apply();
	}

	private static void InitTexts()
	{
		string vertexShaderSource = FileSystem.ReadTextFile("visualizer_text_vertex.glsl");
		string fragmentShaderSource = FileSystem.ReadTextFile("visualizer_text_fragment.glsl");
		_texts.Shader = new Shader(vertexShaderSource, fragmentShaderSource);
		_texts.TransformLocation = _texts.Shader.GetUniformLocation("transform");
		_texts.Vbo = GL.GenBuffer();
		_texts.Vao = GL.GenVertexArray();

		GL.BindVertexArray(_texts.Vao);
		GL.BindBuffer(BufferTarget.ArrayBuffer, _texts.Vbo);
		VertexArrayBuilder.FromStruct<Texts.Vertex>().Apply();

		Image fontImage = new("default_font_12x8_20x32.png", Image.ImageFormat.Rgba, flipY: true);
		_texts.Font = new Font(fontImage, charWidth: 20, charHeight: 32, columns: 12, rows: 8, firstChar: ' ');
	}

	public static void AddLine(Vector3 start, Vector3 end, Color color, bool depthTest = true)
	{
		Vector4 colorVec = color.ToVector4();
		Lines.Vertex v0 = new()
		{
			Position = start,
			Color = colorVec,
		};
		Lines.Vertex v1 = new()
		{
			Position = end,
			Color = colorVec,
		};
		List<Lines.Vertex> list = depthTest ? _lines.VerticesDepthTest : _lines.VerticesNoDepthTest;
		list.Add(v0);
		list.Add(v1);
	}

	public static void AddText(string text, Vector2 position, float height, Color color)
	{
		Debug.Assert(_texts.Font != null);
		float lineSpacing = _texts.Font.GetLineSpacing(height);
		Vector2 cur = position;
		foreach (char c in text)
		{
			if (c == '\r')
				continue;
			if (c == '\n')
			{
				cur.Y += lineSpacing;
				cur.X = position.X;
				continue;
			}

			Rect uv = _texts.Font.GetCharUv(c, out bool found);
			if (!found)
			{
				continue;
			}

			float width = _texts.Font.GetCharWidth(c, height);

			Vector2 bottomRightPosition = new(cur.X + width, cur.Y + height);

			Vector4 vecColor = color.ToVector4();

			Texts.Vertex topLeft = new()
			{
				Position = new Vector3(cur.X, cur.Y, 0),
				Uv = uv.TopLeft,
				Color = vecColor,
			};
			Texts.Vertex topRight = new()
			{
				Position = new Vector3(bottomRightPosition.X, cur.Y, 0),
				Uv = uv.TopRight,
				Color = vecColor,
			};
			Texts.Vertex bottomLeft = new()
			{
				Position = new Vector3(cur.X, bottomRightPosition.Y, 0),
				Uv = uv.BottomLeft,
				Color = vecColor,
			};
			Texts.Vertex bottomRight = new()
			{
				Position = new Vector3(bottomRightPosition.X, bottomRightPosition.Y, 0),
				Uv = uv.BottomRight,
				Color = vecColor,
			};

			_texts.Vertices.Add(topLeft);
			_texts.Vertices.Add(bottomLeft);
			_texts.Vertices.Add(bottomRight);
			_texts.Vertices.Add(bottomRight);
			_texts.Vertices.Add(topRight);
			_texts.Vertices.Add(topLeft);

			cur.X += width;
		}
	}

	public static void RenderAndClear(Matrix4 viewProj, Vector2 screenSize)
	{
		GL.Enable(EnableCap.Blend);
		GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

		Debug.Assert(_lines.Shader != null);
		_lines.Shader.Bind();
		_lines.Shader.SetUniform(_lines.ViewProjLocation, viewProj);

		GL.Enable(EnableCap.DepthTest);
		RenderLines(_lines.VerticesDepthTest);

		GL.Disable(EnableCap.DepthTest);
		RenderLines(_lines.VerticesNoDepthTest);

		Matrix3 screenTransform = new(
			2 / screenSize.X, 0, 0,
			0, -2 / screenSize.Y, 0,
			-1, 1, 1
		);

		Debug.Assert(_texts.Shader != null && _texts.Font != null);
		_texts.Shader.Bind();
		_texts.Shader.SetUniform(_texts.TransformLocation, screenTransform);
		_texts.Font.Bind();
		RenderTexts(_texts.Vertices);
	}

	private static void RenderLines(List<Lines.Vertex> vertices)
	{
		if (vertices.Count == 0)
			return;

		GL.BindVertexArray(_lines.Vao);
		GL.BindBuffer(BufferTarget.ArrayBuffer, _lines.Vbo);

		GL.BufferData(BufferTarget.ArrayBuffer,
			vertices.GetSizeInBytes(),
			CollectionsMarshal.AsSpan(vertices),
			BufferUsage.DynamicDraw);

		GL.DrawArrays(PrimitiveType.Lines, 0, vertices.Count);

		vertices.Clear();
	}

	private static void RenderTexts(List<Texts.Vertex> vertices)
	{
		if (_texts.Vertices.Count == 0)
			return;

		GL.BindVertexArray(_texts.Vao);
		GL.BindBuffer(BufferTarget.ArrayBuffer, _texts.Vbo);

		GL.BufferData(BufferTarget.ArrayBuffer,
			vertices.GetSizeInBytes(),
			CollectionsMarshal.AsSpan(vertices),
			BufferUsage.DynamicDraw);

		GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);

		vertices.Clear();
	}
}