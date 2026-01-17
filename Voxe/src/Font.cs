using System;
using OpenTK.Graphics.OpenGL;
using Voxe.Math;

namespace Voxe;

public class Font : IDisposable
{
	private int _textureId;

	private int _firstChar;
	private int _charWidth = 0;
	private int _charHeight = 0;

	private Rect[] _uvs;

	public bool IsValid => _textureId != 0;

	public Font(Image image, int charWidth, int charHeight, int columns, int rows, char firstChar)
	{
		_textureId = GL.GenTexture();

		GL.BindTexture(TextureTarget.Texture2d, _textureId);
		GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS,
			(int)TextureWrapMode.ClampToEdge);
		GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT,
			(int)TextureWrapMode.ClampToEdge);
		GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter,
			(int)TextureMinFilter.LinearMipmapNearest);
		GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter,
			(int)TextureMagFilter.Nearest);
		GL.TexImage2D(TextureTarget.Texture2d,
			level: 0,
			InternalFormat.Rgba,
			image.Width,
			image.Height,
			border: 0,
			image.Format.ToOpenGLFormat(),
			PixelType.UnsignedByte,
			image.RawData);
		GL.GenerateMipmap(TextureTarget.Texture2d);

		_firstChar = firstChar;
		_charWidth = charWidth;
		_charHeight = charHeight;

		int imageWidth = image.Width;
		int imageHeight = image.Height;
		float charWidthUv = (float)charWidth / (float)imageWidth;
		float charHeightUv = (float)charHeight / (float)imageHeight;

		_uvs = new Rect[columns * rows];
		int index = 0;
		for (int row = 0; row < rows; row++)
		{
			for (int column = 0; column < columns; column++)
			{
				float top = 1f - (float)row * charHeightUv;
				float left = (float)column * charWidthUv;
				float bottom = 1f - (float)(row + 1) * charHeightUv;
				float right = (float)(column + 1) * charWidthUv;
				Rect rect = new(left, bottom, right, top);
				_uvs[index] = rect;
				index++;
			}
		}
	}

	public Rect GetCharUv(char c, out bool found)
	{
		int n = (int)c - _firstChar;
		if (n < 0 || n > _uvs.Length)
		{
			found = false;
			return new Rect();
		}

		found = true;
		return _uvs[n];
	}

	public float GetCharWidth(char c, float height)
	{
		return height * (float)_charWidth / (float)_charHeight;
	}

	public float GetLineSpacing(float height)
	{
		return height * 1.1f;
	}

	private int GetIndex(char c)
	{
		int n = (int)c - _firstChar;
		if (n < 0 || n > _uvs.Length)
			return -1;
		return n;
	}

	public void Destroy()
	{
		if (!IsValid)
			return;

		GL.DeleteTexture(_textureId);
		_textureId = 0;
	}

	public void Dispose()
	{
		Destroy();
		GC.SuppressFinalize(this);
	}

	public void Bind()
	{
		if (!IsValid)
			return;
		GL.BindTexture(TextureTarget.Texture2d, _textureId);
	}
}