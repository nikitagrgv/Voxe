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

	public void Create(int width, int height, ImageFormat format, Color color)
	{
		_width = width;
		_height = height;
		_format = format;

		int pixelSize = GetPixelSizeBytes(format);
		int totalSize = _width * _height * pixelSize;
		_data = new byte[totalSize];

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
		_data = result.Data;
		_format = targetFormat;
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

	public void CopyFrom(Image image, int x, int y, int width, int height)
	{
		int endX = x + width;
		int endY = y + height;

		Debug.Assert(width > 0 && height > 0);
		Debug.Assert(x >= 0 && x < Width);
		Debug.Assert(y >= 0 && y < Height);
		Debug.Assert(endX >= 0 && endX < Width);
		Debug.Assert(endY >= 0 && endY < Height);

		int pixelSize = GetPixelSizeBytes();
		int offsetBytes = GetOffsetBytes(x, y);
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
}