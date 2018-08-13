using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProkisoMarker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProkisoMarker.Tests
{
	[TestClass]
	public class StudentTests
	{
		[TestMethod]
		public void StudentTest()
		{
			var problemSet = new ProblemSet();
			problemSet.Problems.Add(new Problem { Type = ProblemType.Basic });
			problemSet.Problems.Add(new Problem { Type = ProblemType.Basic });
			problemSet.Problems.Add(new Problem { Type = ProblemType.Basic });
			problemSet.Problems.Add(new Problem { Type = ProblemType.Advanced });
			problemSet.Problems.Add(new Problem { Type = ProblemType.Advanced });
			var student = new Student(problemSet);
			student.Answers.Add(new Answer { Evaluation = Evaluation.NotExecutable });
			student.Answers.Add(new Answer { Evaluation = Evaluation.Wrong });
			student.Answers.Add(new Answer { Evaluation = Evaluation.Correct });
			student.Answers.Add(new Answer { Evaluation = Evaluation.NotExecutable });
			student.Answers.Add(new Answer { Evaluation = Evaluation.Correct });
			Assert.AreEqual(100, student.Score);
			Assert.AreEqual(2, student.AdvancedScore);

			student.Answers[4].Evaluation = Evaluation.Wrong;
			Assert.AreEqual(100, student.Score);
			Assert.AreEqual(0, student.AdvancedScore);

			student.Answers[2].Evaluation = Evaluation.Wrong;
			Assert.AreEqual(74, student.Score);
			Assert.AreEqual(0, student.AdvancedScore);

			student.Answers[1].Evaluation = Evaluation.Correct;
			Assert.AreEqual(94, student.Score);
			Assert.AreEqual(0, student.AdvancedScore);

			problemSet.Problems[1].Type = ProblemType.Advanced;
			Assert.AreEqual(90, student.Score);
			Assert.AreEqual(1, student.AdvancedScore);
		}
	}
}
