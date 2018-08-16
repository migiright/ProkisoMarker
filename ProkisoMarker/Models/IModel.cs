
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ProkisoMarker.Models
{
	public interface IModel
	{
		ProblemSet ProblemSet { get; }
		ObservableCollection<Student> Students { get; }
		string WorkingDirectory { get; set; }
		string SubmissionsDirectory { get; }
		Task LoadSubmissions(string filePath);
		Task Compile(Answer answer);
		Task Run(Answer answer);
		IObservable<Answer> CompileAll();
		IObservable<Answer> CompileAndRunAll();
		void OutputScores();
	}
}
