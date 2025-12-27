using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Voxe.Render;

public class VertexArrayBuilder
{
	private struct Attribute(
		int count,
		VertexAttribPointerType type,
		int sizeOfType,
		int offset)
	{
		public int Count { get; } = count;
		public VertexAttribPointerType Type { get; } = type;
		public int Offset { get; } = offset;
	}

	private readonly List<Attribute> _attributes = [];
	private int _stride;

	public static VertexArrayBuilder FromStruct<TVertex>() where TVertex : struct
	{
		VertexArrayBuilder b = new();
		b.AddAttributesFromStruct<TVertex>();
		return b;
	}

	public void Apply()
	{
		for (int i = 0; i < _attributes.Count; i++)
		{
			Attribute attribute = _attributes[i];

			GL.VertexAttribPointer(
				(uint)i,
				attribute.Count,
				attribute.Type,
				normalized: false,
				_stride,
				new IntPtr(attribute.Offset));

			GL.EnableVertexAttribArray((uint)i);
		}
	}

	private void AddAttributesFromStruct<TVertex>() where TVertex : struct
	{
		Type type = typeof(TVertex);

		Debug.Assert(type.IsValueType, "Type should be a struct");
		Debug.Assert(type.IsLayoutSequential || type.IsExplicitLayout,
			$"{type.Name} must be marked with [StructLayout(LayoutKind.Sequential)]");

		_stride = Marshal.SizeOf<TVertex>();

		FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

		foreach (FieldInfo field in fields)
		{
			int offset = (int)Marshal.OffsetOf<TVertex>(field.Name);

			if (field.FieldType == typeof(float))
			{
				AddAttribute(1, VertexAttribPointerType.Float, sizeof(float), offset);
			}
			else if (field.FieldType == typeof(Vector2))
			{
				AddAttribute(2, VertexAttribPointerType.Float, sizeof(float), offset);
			}
			else if (field.FieldType == typeof(Vector3))
			{
				AddAttribute(3, VertexAttribPointerType.Float, sizeof(float), offset);
			}
			else if (field.FieldType == typeof(Vector4))
			{
				AddAttribute(4, VertexAttribPointerType.Float, sizeof(float), offset);
			}
			else
			{
				throw new NotSupportedException($"Unsupported vertex field type: {field.FieldType}");
			}
		}

		_attributes.Sort((left, right) => left.Offset.CompareTo(right.Offset));
	}

	private void AddAttribute(
		int count,
		VertexAttribPointerType type,
		int sizeOfType,
		int offset)
	{
		_attributes.Add(new Attribute(count, type, sizeOfType, offset));
	}
}