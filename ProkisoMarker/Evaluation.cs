using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProkisoMarker
{
	public enum Evaluation
	{
		Undefined,
		NotExecutable,
		Wrong,
		Correct,
	}

	public static class EvaluationExt
	{
		public static string ToDisplayName(this Evaluation self)
		{
			switch (self) {
			case Evaluation.Undefined:
				return "未評価";
			case Evaluation.NotExecutable:
				return "実行不可";
			case Evaluation.Wrong:
				return "不正解";
			case Evaluation.Correct:
				return "正解";
			}
			throw new ArgumentException($"self: {self} は無効な値");
		}
	}
}
