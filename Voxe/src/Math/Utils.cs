using System.Numerics;

namespace Voxe.Math;

public static class Utils
{
	public static int RoundUpToPowerOfTwo(int value)
	{
		if (value < 1) return 1;
		return 1 << (BitOperations.Log2((uint)(value - 1)) + 1);
	}

	public static bool IsPowerOfTwo(int n)
	{
		return BitOperations.IsPow2(n);
	}
}