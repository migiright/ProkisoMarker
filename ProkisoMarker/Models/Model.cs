using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace ProkisoMarker.Models
{
	public class Model : BindableBase, IModel
	{
		public ProblemSet ProblemSet { get; } = new ProblemSet();
		private string _workingDirectory = "working";
		public string WorkingDirectory
		{
			get { return _workingDirectory; }
			set {
				var v = value ?? "";
				SetProperty(ref _workingDirectory, value);
			}
		}
		public ObservableCollection<Student> Students { get; } = new ObservableCollection<Student>();
		public string SubmissionsDirectory => Path.Combine(WorkingDirectory, RelativeSubmissionsDirectory);
		public string ExecutionDirectory => Path.Combine(WorkingDirectory, RelativeExecutionDirectory);

		public Model()
		{
			ProblemSet.Problems.CollectionChanged += Problems_CollectionChanged;
		}

		const string RelativeSubmissionsDirectory = @"submissions\";
		const string RelativeExecutionDirectory = @"execution\";

		private void Problems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null) {
				foreach (var (p, i) in e.NewItems.Cast<Problem>().Select((p, i) => (p, i))) {
					if (string.IsNullOrEmpty(p.Name)) {
						p.Name = "p" + (i + 1 + ProblemSet.Problems.Count - e.NewItems.Count);
					}
				}
			}
			foreach (var (p, i) in ProblemSet.Problems.Select((p, i) => (p, i))) {
				p.No = i + 1;
			}
		}
	}
}
