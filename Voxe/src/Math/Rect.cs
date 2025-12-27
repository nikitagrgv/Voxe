using OpenTK.Mathematics;

namespace Voxe.Math;

public readonly struct Rect
{
	private readonly float _minX;
	private readonly float _minY;
	private readonly float _maxX;
	private readonly float _maxY;

	public float MinX => _minX;
	public float MinY => _minY;

	public float Width => _maxX - _minX;
	public float Height => _maxY - _minY;

	public float MaxX => _maxX;
	public float MaxY => _maxY;

	public Vector2 MinXMinY => new(MinX, MinY);
	public Vector2 MaxXMinY => new(MaxX, MinY);
	public Vector2 MinXMaxY => new(MinX, MaxY);
	public Vector2 MaxXMaxY => new(MaxX, MaxY);

	public Vector2 BottomLeft => new(MinX, MinY);
	public Vector2 BottomRight => new(MaxX, MinY);
	public Vector2 TopLeft => new(MinX, MaxY);
	public Vector2 TopRight => new(MaxX, MaxY);

	public Rect(float minX, float minY, float maxX, float maxY)
	{
		_minX = minX;
		_minY = minY;
		_maxX = maxX;
		_maxY = maxY;
	}
}