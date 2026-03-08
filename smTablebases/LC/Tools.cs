
using System.Numerics;



namespace LC
{
	public static class Tools
	{
		public static int ValueToBitCount(int v)
		{
			return Log2ForAnyNumber(v) + 1;
		}


		public static int Log2ForAnyNumber(int v)
		{
			return BitOperations.Log2((uint)v);	
		}

		
	}
}
