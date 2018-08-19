
using System;
using System.Collections.ObjectModel;

namespace ProkisoMarker.Models
{
	public interface IModel
	{
		ProblemSet ProblemSet { get; }
		string WorkingDirectory { get; set; }
		ObservableCollection<Student> Students { get; }
		string SubmissionsDirectory { get; }
		string ExecutionDirectory { get; }
		IObservable<Student> LoadSubmissions(string filePath);
		Problem GetProblemOf(Answer answer);
	}
}
