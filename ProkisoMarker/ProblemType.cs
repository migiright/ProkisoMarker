using System;

namespace ProkisoMarker
{
	public enum ProblemType
	{
		Basic,
		Advanced,
	}

	public static class ProblemTypeExt
	{
		public static string ToDisplayName(this ProblemType self)
		{
			switch (self) {
			case ProblemType.Basic:
				return "基本";
			case ProblemType.Advanced:
				return "応用";
			}
			throw new ArgumentException($"self: {self.ToString()} は無効な値");
		}
	}
}
