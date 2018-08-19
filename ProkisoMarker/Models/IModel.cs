
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ProkisoMarker.Models
{
	public interface IModel
	{
		ProblemSet ProblemSet { get; }
		string WorkingDirectory { get; set; }
		ObservableCollection<Student> Students { get; }
		string SubmissionsDirectory { get; }
		string ExecutionDirectory { get; }
		Compiler Compiler { get; }
		IObservable<Student> LoadSubmissions(string filePath);
		Problem GetProblemOf(Answer answer);
		Task Compile(Answer answer);
		Task Run(Answer answer);
	}
}
