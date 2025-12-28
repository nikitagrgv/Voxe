namespace Voxe;

public struct TimePoint
{
	// private 
}

public static class Time
{
	private static double _deltaTime = 0;
	private static double _fps = 0;

	public static TimeSpan CurrentTime { get; set; }

	public static TimeSpan DeltaSpan
	{
		get;
		set
		{
			_deltaTime = value.TotalSeconds;
			_fps = 1 / _deltaTime;
			field = value;
		}
	}

	public static double DeltaTime => _deltaTime;
	public static double Fps => _fps;
}