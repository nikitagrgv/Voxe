using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Voxe;

public static class Utils
{
	public static int GetSizeInBytes<T>(this List<T> array)
		where T : unmanaged
	{
		return array.Count * GetSizeOfType<T>();
	}

	public static int GetSizeInBytes<T>(this T[] array)
		where T : unmanaged
	{
		return array.Length * GetSizeOfType<T>();
	}

	public static int GetSizeInBytes<T>(this ReadOnlySpan<T> array)
		where T : unmanaged
	{
		return array.Length * GetSizeOfType<T>();
	}

	public static int GetSizeOfType<T>()
		where T : unmanaged
	{
		Type type = typeof(T);
		Debug.Assert(type.IsLayoutSequential || type.IsExplicitLayout,
			$"{type.Name} must be marked with [StructLayout(LayoutKind.Sequential)]");
		return Unsafe.SizeOf<T>();
	}

	public static Vector4 ToVector4(this Color color)
	{
		return new Vector4(
			color.R / 255f,
			color.G / 255f,
			color.B / 255f,
			color.A / 255f);
	}

	public static double Normalize360(this double angle)
	{
		angle %= 360.0;
		if (angle < 0)
			angle += 360.0;
		return angle;
	}

	public static float Normalize360(this float angle)
	{
		angle %= 360.0f;
		if (angle < 0f)
			angle += 360.0f;
		return angle;
	}

	public static double Normalize180(this double angle)
	{
		angle = Normalize360(angle);
		if (angle > 180)
			angle -= 360;
		return angle;
	}

	public static float Normalize180(this float angle)
	{
		angle = Normalize360(angle);
		if (angle > 180f)
			angle -= 360f;
		return angle;
	}

	public static PixelFormat ToOpenGLFormat(this Image.ImageFormat format)
	{
		return format switch
		{
			Image.ImageFormat.Rgb => PixelFormat.Rgb,
			Image.ImageFormat.Rgba => PixelFormat.Rgba,
			_ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
		};
	}
}