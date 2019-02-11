﻿using Xunit;

namespace DotNext
{
	public sealed class RangeTests: Assert
	{
		[Fact]
		public void ClampTest()
		{
			Equal(20M, 10M.Clamp(20M, 30M));
			Equal(25M, 25M.Clamp(20M, 30M));
			Equal(30M, 40M.Clamp(20M, 30M));
		}

		[Fact]
		public void MinMaxTest()
		{
			Equal(20M, 20M.Min(30M));
			Equal(30M, 10M.Max(30M));
		}

		[Fact]
		public void BetweenTest()
		{
			True(15M.Between(10M, 20M));
			False(10M.Between(10M, 20M, BoundType.Open));
			True(10M.Between(10M, 20M, BoundType.LeftClosed));
			False(15M.Between(10M, 12M));
		}
	}
}