using System;

namespace ProkisoMarker
{
	public enum Result
	{
		Undefined,
		FileNotFound,
		CompileError,
		RuntimeError,
		Timeout,
		Success,
	}

	public static class ResultExt
	{
		public static string ToDisplayName(this Result self)
		{
			switch (self) {
			case Result.Undefined:
				return "未処理";
			case Result.FileNotFound:
				return "ファイル見つからない";
			case Result.CompileError:
				return "コンパイルエラー";
			case Result.RuntimeError:
				return "ランタイムエラー";
			case Result.Timeout:
				return "タイムアウト";
			case Result.Success:
				return "実行成功";
			}
			throw new ArgumentException($"self: {self} は無効な値");
		}
	}
}
