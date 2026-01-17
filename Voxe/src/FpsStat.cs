using System;

namespace Voxe;

public class FpsStat
{
	private readonly static TimeSpan MeanFpsUpdateTime = TimeSpan.FromSeconds(0.5);
	private readonly static TimeSpan MinMaxFpsUpdateTime = TimeSpan.FromSeconds(2f);

	private TimeSpan _lastUpdateMeanFpsTime = Time.CurrentTime;
	private TimeSpan _lastUpdateMinMaxFpsTime = Time.CurrentTime;
	private int _numFramesForMeanFps = 0;
	private double _lastMeanFps = 0f;
	private double _lastMinDt = 0f;
	private double _lastMaxDt = float.PositiveInfinity;
	private double _minDt = float.PositiveInfinity;
	private double _maxDt = 0;

	public double LastMeanFps => _lastMeanFps;
	public double LastMaxDt => _lastMaxDt;
	public double LastMinDt => _lastMinDt;

	public void Update(TimeSpan curTime, double dt)
	{
		if (curTime - _lastUpdateMeanFpsTime > MeanFpsUpdateTime)
		{
			_lastMeanFps = (curTime - _lastUpdateMeanFpsTime).TotalSeconds / int.Max(_numFramesForMeanFps, 1);
			_lastUpdateMeanFpsTime = curTime;
			_numFramesForMeanFps = 0;
		}

		if (curTime - _lastUpdateMinMaxFpsTime > MinMaxFpsUpdateTime)
		{
			_lastUpdateMinMaxFpsTime = curTime;
			_lastMinDt = _minDt;
			_lastMaxDt = _maxDt;
			_minDt = float.PositiveInfinity;
			_maxDt = 0;
		}

		_minDt = double.Min(_minDt, dt);
		_maxDt = double.Max(_maxDt, dt);

		_numFramesForMeanFps++;
	}
}