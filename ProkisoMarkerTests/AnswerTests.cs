using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProkisoMarker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace ProkisoMarker.Tests
{
	[TestClass]
	public class AnswerTests
	{
		[TestMethod]
		public void AnswerTest()
		{
			var answer = new Answer();
			Assert.IsFalse(answer.InputModified);
			Assert.IsFalse(answer.SourceModified);

			var NumInputModifiedChanged = 0;
			var SourceModifiedChanged = false;
			answer.PropertyChanged += (s, e) => {
				switch (e.PropertyName) {
				case nameof(Answer.InputModified):
					++NumInputModifiedChanged;
					break;
				case nameof(Answer.SourceModified):
					SourceModifiedChanged = true;
					break;
				}
			};
			answer.ModifiedInput = "aaa";
			Assert.IsTrue(answer.InputModified);
			Assert.AreEqual(1, NumInputModifiedChanged);

			answer.ModifiedSourcePath = "bbb";
			Assert.IsTrue(answer.SourceModified);
			Assert.IsTrue(SourceModifiedChanged);

			answer.ModifiedInput = "aaaa";
			Assert.AreEqual(1, NumInputModifiedChanged);
		}
	}
}
