using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProkisoMarker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProkisoMarker.Tests
{
	[TestClass()]
	public class ProblemSetTests
	{
		[TestMethod()]
		public void ProblemSetTest()
		{
			var problemSet = new ProblemSet();

			var problem0 = new Problem() { Type = ProblemType.Basic };
			problemSet.Problems.Add(problem0);
			problemSet.Problems.Add(new Problem() { Type = ProblemType.Basic });
			problemSet.Problems.Add(new Problem() { Type = ProblemType.Advanced });
			Assert.AreEqual(problemSet.NumBasicProblems, 2);
			Assert.AreEqual(problemSet.NumAdvancedPRoblems, 1);

			problem0.Type = ProblemType.Advanced;
			Assert.AreEqual(problemSet.NumBasicProblems, 1);
			Assert.AreEqual(problemSet.NumAdvancedPRoblems, 2);

			problemSet.Problems.Remove(problem0);
			Assert.AreEqual(problemSet.NumBasicProblems, 1);
			Assert.AreEqual(problemSet.NumAdvancedPRoblems, 1);

			problem0.Type = ProblemType.Basic;
			Assert.AreEqual(problemSet.NumBasicProblems, 1);
			Assert.AreEqual(problemSet.NumAdvancedPRoblems, 1);
		}
	}
}
