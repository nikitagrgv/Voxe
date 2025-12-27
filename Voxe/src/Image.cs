using StbImageSharp;
using Voxe;

public class Image
{
	private ImageResult _image;
	private ImageFormat _imageFormat;

	public ImageFormat Format => _imageFormat;

	public enum ImageFormat
	{
		Rgb,
		Rgba
	}

	public Image(string path, ImageFormat imageFormat, bool flipY = false)
	{
		Stream stream = FileSystem.ReadFileStream(path);

		StbImage.stbi_set_flip_vertically_on_load(flipY ? 1 : 0);
		ImageResult? result = ImageResult.FromStream(stream, ColorComponentsFromFormat(imageFormat));

		if (result == null)
			throw new Exception($"Cannot load image {path}");

		_image = result;
		_imageFormat = imageFormat;
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

	public byte[] RawData => _image.Data;
	public int Width => _image.Width;
	public int Height => _image.Height;
}