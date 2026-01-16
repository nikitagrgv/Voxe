using System.Diagnostics;
using System.Drawing;
using StbImageSharp;
using Voxe;

public class Image
{
	private ImageResult _data;
	private ImageFormat _format;

	public ImageFormat Format => _format;

	public enum ImageFormat
	{
		Rgb,
		Rgba
	}

	public Image(string path, ImageFormat targetFormat, bool flipY = false)
	{
		Stream stream = FileSystem.ReadFileStream(path);

		StbImage.stbi_set_flip_vertically_on_load(flipY ? 1 : 0);
		ImageResult? result = ImageResult.FromStream(stream, ColorComponentsFromFormat(targetFormat));

		if (result == null)
			throw new Exception($"Cannot load image {path}");

		_data = result;
		_format = targetFormat;
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