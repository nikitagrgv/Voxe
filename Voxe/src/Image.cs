using System.Diagnostics;
using System.Drawing;
using StbImageSharp;
using StbImageWriteSharp;
using ColorComponents = StbImageSharp.ColorComponents;
using ColorComponentsWrite = StbImageWriteSharp.ColorComponents;
using Voxe;

public class Image
{
	private int _width = 0;
	private int _height = 0;
	private byte[] _data = [];

	private ImageFormat _format = ImageFormat.Rgba;

	public enum ImageFormat
	{
		Rgb,
		Rgba
	}

	public bool IsEmpty => _data.Length > 0;
	public byte[] RawData => _data;
	public int Width => _width;
	public int Height => _height;
	public ImageFormat Format => _format;

	public Image()
	{
	}

	public Image(int width, int height, ImageFormat format, Color color)
	{
		Create(width, height, format, color);
	}

	public Image(string path, ImageFormat targetFormat, bool flipY = false)
	{
		Load(path, targetFormat, flipY);
	}

	public Image(Image source)
	{
		CloneFrom(source);
	}

	public void CreateWithGarbage(int width, int height, ImageFormat format)
	{
		_width = width;
		_height = height;
		_format = format;

		int pixelSize = GetPixelSizeBytes(format);
		int totalSize = _width * _height * pixelSize;
		_data = GC.AllocateUninitializedArray<byte>(totalSize);
	}

	public void Create(int width, int height, ImageFormat format, Color color)
	{
		CreateWithGarbage(width, height, format);
		int pixelSize = GetPixelSizeBytes(format);
		int totalSize = _data.Length;

		switch (format)
		{
			case ImageFormat.Rgb:
			{
				for (int i = 0; i < totalSize; i += pixelSize)
				{
					_data[i] = color.R;
					_data[i + 1] = color.G;
					_data[i + 2] = color.B;
				}

				break;
			}
			case ImageFormat.Rgba:
			{
				for (int i = 0; i < totalSize; i += pixelSize)
				{
					_data[i] = color.R;
					_data[i + 1] = color.G;
					_data[i + 2] = color.B;
					_data[i + 3] = color.A;
				}

				break;
			}
			default: throw new Exception($"Unsupported format {format}");
		}
	}

	public void Load(string path, ImageFormat targetFormat, bool flipY = false)
	{
		using Stream stream = FileSystem.ReadFileStream(path);

		StbImage.stbi_set_flip_vertically_on_load(flipY ? 1 : 0);
		ImageResult? result = ImageResult.FromStream(stream, ColorComponentsFromFormat(targetFormat));

		if (result == null)
			throw new Exception($"Cannot load image {path}");

		_width = result.Width;
		_height = result.Height;
		_format = targetFormat;
		_data = result.Data;
	}

	public void CloneFrom(Image source)
	{
		_width = source._width;
		_height = source._height;
		_format = source._format;
		_data = GC.AllocateUninitializedArray<byte>(source._data.Length);
		Buffer.BlockCopy(source._data, 0, _data, 0, _data.Length);
	}

	public void Save(string path, bool flipY = false)
	{
		string extension = Path.GetExtension(path);
		if (string.IsNullOrEmpty(extension))
		{
			extension = ".png";
			path += extension;
		}

		ImageWriter writer = new();
		StbImageWrite.stbi_flip_vertically_on_write(flipY ? 1 : 0);
		using Stream stream = FileSystem.WriteFileStream(path);

		ColorComponents components = ColorComponentsFromFormat(Format);
		ColorComponentsWrite componentsWrite = ColorComponentsWriteFromColorComponents(components);

		switch (extension)
		{
			case ".png":
				writer.WritePng(_data, Width, Height, componentsWrite, stream);
				break;
			case ".bmp":
				writer.WriteBmp(_data, Width, Height, componentsWrite, stream);
				break;
			case ".jpg":
				writer.WriteJpg(_data, Width, Height, componentsWrite, stream, quality: 95);
				break;
			default:
				throw new Exception($"Unsupported file extension {extension}");
		}
	}

	public Image GenerateNextMipLevel()
	{
		if (Width < 2 || Height < 2)
			throw new Exception("Too small image");
		if (Width != Height)
			throw new NotSupportedException();
		if (Voxe.Math.Utils.IsPowerOfTwo(Width))
			throw new NotSupportedException();

		int newWidth = Math.Max(1, Width / 2);
		int newHeight = Math.Max(1, Height / 2);

		Image mip = new();
		mip.CreateWithGarbage(newWidth, newHeight, Format);

		for (int y = 0; y < newHeight; y++)
		{
			for (int x = 0; x < newWidth; x++)
			{
				int srcX = x * 2;
				int srcY = y * 2;

				Color p00 = GetPixel(srcX, srcY);
				Color p10 = GetPixel(srcX + 1, srcY);
				Color p01 = GetPixel(srcX, srcY + 1);
				Color p11 = GetPixel(srcX + 1, srcY + 1);

				Color averaged = AverageColors(p00, p10, p01, p11);
				mip.SetPixel(x, y, averaged);
			}
		}

		return mip;
	}

	private static Color AverageColors(params Color[] colors)
	{
		float rSum = 0, gSum = 0, bSum = 0, aSum = 0;
		float invCount = 1.0f / colors.Length;

		foreach (Color c in colors)
		{
			rSum += SrgbToLinear(c.R / 255.0f);
			gSum += SrgbToLinear(c.G / 255.0f);
			bSum += SrgbToLinear(c.B / 255.0f);
			aSum += c.A / 255.0f;
		}

		byte r = (byte)Math.Clamp(LinearToSrgb(rSum * invCount) * 255.0f, 0, 255);
		byte g = (byte)Math.Clamp(LinearToSrgb(gSum * invCount) * 255.0f, 0, 255);
		byte b = (byte)Math.Clamp(LinearToSrgb(bSum * invCount) * 255.0f, 0, 255);
		byte a = (byte)Math.Clamp((aSum * invCount) * 255.0f, 0, 255);

		return Color.FromArgb(a, r, g, b);
	}

	public static int GetPixelSizeBytes(ImageFormat format)
	{
		return format switch
		{
			ImageFormat.Rgb => 3,
			ImageFormat.Rgba => 4,
			_ => 0,
		};
	}

	public int GetPixelSizeBytes()
	{
		return _format switch
		{
			ImageFormat.Rgb => 3,
			ImageFormat.Rgba => 4,
			_ => 0,
		};
	}

	public int GetOffsetPixels(int x, int y)
	{
		return y * Width + x;
	}

	public int GetOffsetBytes(int x, int y)
	{
		return GetOffsetPixels(x, y) * GetPixelSizeBytes();
	}

	public void SetPixel(int x, int y, Color color)
	{
		Debug.Assert(x >= 0 && x < Width);
		Debug.Assert(y >= 0 && y < Height);
		Debug.Assert(_format is ImageFormat.Rgba or ImageFormat.Rgb, "Not supported");

		int bytesOffset = GetOffsetBytes(x, y);
		_data[bytesOffset + 0] = color.R;
		_data[bytesOffset + 1] = color.G;
		_data[bytesOffset + 2] = color.B;
		if (_format == ImageFormat.Rgba)
			_data[bytesOffset + 3] = color.A;
	}

	public Color GetPixel(int x, int y)
	{
		Debug.Assert(x >= 0 && x < Width);
		Debug.Assert(y >= 0 && y < Height);
		Debug.Assert(_format is ImageFormat.Rgba or ImageFormat.Rgb, "Not supported");

		int bytesOffset = GetOffsetBytes(x, y);
		byte r = _data[bytesOffset + 0];
		byte g = _data[bytesOffset + 1];
		byte b = _data[bytesOffset + 2];
		byte a = 255;
		if (_format == ImageFormat.Rgba)
			a = _data[bytesOffset + 3];

		return Color.FromArgb(a, r, g, b);
	}

	public void CopyFrom(Image source, int sourceX, int sourceY, int targetX, int targetY, int width, int height)
	{
		Debug.Assert(width > 0 && height > 0);

		Debug.Assert(source.IsValidPixel(sourceX, sourceY));
		Debug.Assert(source.IsValidPixel(sourceX + width - 1, sourceY + height - 1));

		Debug.Assert(IsValidPixel(targetX, targetY));
		Debug.Assert(IsValidPixel(targetX + width - 1, targetY + height - 1));

		// TODO: Optimize
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				Color pixel = source.GetPixel(sourceX + j, sourceY + i);
				SetPixel(targetX + j, targetY + i, pixel);
			}
		}
	}

	public bool IsValidPixel(int x, int y)
	{
		return x >= 0 && x < Width && y >= 0 && y < Height;
	}

	private static ColorComponentsWrite ColorComponentsWriteFromColorComponents(ColorComponents colorComponents)
	{
		return colorComponents switch
		{
			ColorComponents.Default => ColorComponentsWrite.RedGreenBlueAlpha,
			ColorComponents.Grey => ColorComponentsWrite.Grey,
			ColorComponents.GreyAlpha => ColorComponentsWrite.GreyAlpha,
			ColorComponents.RedGreenBlue => ColorComponentsWrite.RedGreenBlue,
			ColorComponents.RedGreenBlueAlpha => ColorComponentsWrite.RedGreenBlueAlpha,
			_ => throw new ArgumentOutOfRangeException(nameof(colorComponents), colorComponents, null)
		};
	}

	private static ColorComponents ColorComponentsFromFormat(ImageFormat imageFormat)
	{
		return imageFormat switch
		{
			ImageFormat.Rgb => ColorComponents.RedGreenBlue,
			ImageFormat.Rgba => ColorComponents.RedGreenBlueAlpha,
			_ => throw new ArgumentOutOfRangeException(nameof(imageFormat), imageFormat, null)
		};
	}

	private static float SrgbToLinear(float s) => (float)Math.Pow(s, 2.2);
	private static float LinearToSrgb(float l) => (float)Math.Pow(l, 1.0 / 2.2);
}