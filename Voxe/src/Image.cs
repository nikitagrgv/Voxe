using System.Diagnostics;
using System.Drawing;
using StbImageSharp;
using StbImageWriteSharp;
using ColorComponents = StbImageSharp.ColorComponents;
using ColorComponentsWrite = StbImageWriteSharp.ColorComponents;
using Voxe;

public class Image
{
	private ImageResult? _data;
	private ImageFormat _format;

	public ImageFormat Format => _format;

	public enum ImageFormat
	{
		Rgb,
		Rgba
	}

	public bool IsValid => _data != null;

	public Image()
	{
	}

	public Image(string path, ImageFormat targetFormat, bool flipY = false)
	{
		Load(path, targetFormat, flipY);
	}

	public void Load(string path, ImageFormat targetFormat, bool flipY = false)
	{
		using Stream stream = FileSystem.ReadFileStream(path);

		StbImage.stbi_set_flip_vertically_on_load(flipY ? 1 : 0);
		ImageResult? result = ImageResult.FromStream(stream, ColorComponentsFromFormat(targetFormat));

		if (result == null)
			throw new Exception($"Cannot load image {path}");

		_data = result;
		_format = targetFormat;
	}

	public void Save(string path)
	{
		if (!IsValid)
			throw new Exception("Image is invalid");

		ImageWriter writer = new();
		using Stream stream = FileSystem.WriteFileStream(path);

		string extension = Path.GetExtension(path);
		if (string.IsNullOrEmpty(extension))
		{
			extension = "png";
			path += "." + extension;
		}

		ColorComponents components = ColorComponentsFromFormat(Format);
		ColorComponentsWrite componentsWrite = ColorComponentsWriteFromColorComponents(components);

		switch (extension)
		{
			case "png":
				writer.WritePng(_data!.Data, _data.Width, _data.Height, componentsWrite, stream);
				break;
			case "bmp":
				writer.WriteBmp(_data!.Data, _data.Width, _data.Height, componentsWrite, stream);
				break;
			case "jpg":
				writer.WriteJpg(_data!.Data, _data.Width, _data.Height, componentsWrite, stream, quality: 95);
				break;
			default:
				throw new Exception($"Unsupported file extension {extension}");
		}
	}

	public void SetPixel(int x, int y, Color color)
	{
		Debug.Assert(_data.Data != null);
		Debug.Assert(x >= 0 && x < _data.Width);
		Debug.Assert(y >= 0 && y < _data.Height);
		Debug.Assert(_format is ImageFormat.Rgba or ImageFormat.Rgb, "Not supported");

		int pixelSize = _format switch
		{
			ImageFormat.Rgb => 3,
			ImageFormat.Rgba => 4,
			_ => 0,
		};

		int pixelOffset = y * _data.Width + x;
		int bytesOffset = pixelOffset * pixelSize;

		_data.Data[bytesOffset + 0] = color.R;
		_data.Data[bytesOffset + 1] = color.G;
		_data.Data[bytesOffset + 2] = color.B;
		if (_format == ImageFormat.Rgba)
		{
			_data.Data[bytesOffset + 3] = color.A;
		}
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

	public byte[] RawData => _data.Data;
	public int Width => _data.Width;
	public int Height => _data.Height;
}