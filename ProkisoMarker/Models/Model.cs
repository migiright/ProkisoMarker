using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;

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
		private Compiler _compiler;
		public Compiler Compiler
		{
			get { return _compiler; }
			set { SetProperty(ref _compiler, value); }
		}

		public Model()
		{
			ProblemSet.Problems.CollectionChanged += Problems_CollectionChanged;
			BindingOperations.EnableCollectionSynchronization(Students, new object());
		}

		public IObservable<Student> LoadSubmissions(string filePath)
		{
			string toHankaku(string str)
				=> string.Concat(str.Select(c => ('０' <= c && c <= '９') ? (char)('0' + c - '０') : c));
			var observable = Observable.Create<Student>(obs => {
				using (var archive = ZipFile.Open(filePath, ZipArchiveMode.Read)) {
					var excerciseNo = int.Parse(toHankaku(ExerciseNoGetter.Match(Path.GetFileName(filePath)).Groups[1].Value));
					var tuples = archive.Entries.Select(entry => (entry: entry, match: ZipNameSplitter.Match(entry.Name)))
						.Where(t => t.match.Success);
					Students.Clear();
					foreach (var (entry, match) in tuples) {
						var student = new Student(ProblemSet) {
							StudentNo = int.Parse(match.Groups[1].Value),
							Name = match.Groups[2].Value,
						};
						BindingOperations.EnableCollectionSynchronization(student.Answers, new object());
						var sd = Path.Combine(SubmissionsDirectory, GetRelativeStudentDirectory(student));
						using (var st = entry.Open())
						using (var ar = new ZipArchive(st)) {
							ar.ExtractToDirectory(sd);
						}
						student.Answers.AddRange(Enumerable.Range(1, ProblemSet.Problems.Count)
							.Select(i => {
								var sp = Directory.EnumerateFiles(sd, $"ex{excerciseNo}-{i}.cpp").FirstOrDefault();
								return new Answer {
									No = i,
									OriginalSourcePath = sp,
									ExecutionDirectory = Path.Combine(ExecutionDirectory, GetRelativeStudentDirectory(student), $"{i}"),
									Result = sp == null ? Result.FileNotFound : Result.Undefined,
								};
							}));
						Students.Add(student);
						obs.OnNext(student);
					}
				}
				obs.OnCompleted();
				return Disposable.Empty;
			}).SubscribeOn(Scheduler.Default)
				.Replay();
			observable.Connect();
			return observable;
		}

		public Problem GetProblemOf(Answer answer)
		{
			return ProblemSet.Problems.Where(p => p.No == answer.No).First();
		}

		const string RelativeSubmissionsDirectory = @"submissions\";
		const string RelativeExecutionDirectory = @"execution\";
		static readonly Regex ZipNameSplitter = new Regex(@"^(\d{7}) (.+?)_.*\.zip$", RegexOptions.IgnoreCase);
		static readonly Regex ExerciseNoGetter = new Regex(@"第(\d+)回");

		private string GetRelativeStudentDirectory(Student student)
		{
			return $"{student.StudentNo}_{ student.Name}\\";
		}

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
