using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace LC
{
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct LatestHistory
	{
		public static readonly LatestHistory Initial = new(1, 2, 3, 4);

		private readonly Vector128<int> _data;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public LatestHistory(int v0, int v1, int v2, int v3)
		{
			_data = Vector128.Create(v0, v1, v2, v3);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private LatestHistory(Vector128<int> data)
		{
			_data = data;
		}

		public int Val0
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _data.GetElement(0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Compare(LatestHistory lh1, LatestHistory lh2)
		{
			return lh1._data == lh2._data;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public LatestHistory Add(int value)
		{
			int v0 = _data.GetElement(0);
			int v1 = _data.GetElement(1);
			int v2 = _data.GetElement(2);

			if (value == v0)
				return this;
			if (value == v1)
				return new LatestHistory(Vector128.Create(value, v0, v2, _data.GetElement(3)));
			if (value == v2)
				return new LatestHistory(Vector128.Create(value, v0, v1, _data.GetElement(3)));

			return new LatestHistory(Vector128.Create(value, v0, v1, v2));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetRank(int dist)
		{
			Vector128<int> cmp = Vector128.Equals(_data, Vector128.Create(dist));

			uint mask = cmp.ExtractMostSignificantBits();

			return mask == 0 ? -1 : BitOperations.TrailingZeroCount(mask);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(int value)
		{
			Vector128<int> cmp = Vector128.Equals(_data, Vector128.Create(value));
			return cmp != Vector128<int>.Zero;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetVal(int index)
		{
			return _data.GetElement(index);
		}

		public override string ToString()
		{
			return string.Concat(
				_data.GetElement(0).ToString(), ", ",
				_data.GetElement(1).ToString(), ", ",
				_data.GetElement(2).ToString(), ", ",
				_data.GetElement(3).ToString());
		}
	}
}