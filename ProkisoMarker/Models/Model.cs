using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
				using (var archive = ZipFile.Open(filePath, ZipArchiveMode.Read, Encoding.GetEncoding(932))) {
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

		public Task Compile(Answer answer)
		{
			return Task.Run(async () => {
				var psi = new ProcessStartInfo(Environment.GetEnvironmentVariable("comspec")) {
					RedirectStandardOutput = true,
					RedirectStandardInput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
					Arguments = VsDevCmdTable[Compiler],
				};
				Match m;
				var outputLines = new List<string>();
				var exePath = Path.Combine(answer.ExecutionDirectory, "a");
				using (var p = Process.Start(psi)) {
					if (!Directory.Exists(answer.ExecutionDirectory)) {
						Directory.CreateDirectory(answer.ExecutionDirectory);
					}
					p.StandardInput.WriteLine($"cl \"{answer.OriginalSourcePath}\" /Fe\"{exePath}\" /Fo\"{exePath}\" {CompilerOptions}");
					p.StandardInput.WriteLine("echo @@@errorlevel=%errorlevel%");
					p.StandardInput.WriteLine("exit");
					string l;
					while ((m = ReturnValueGetter.Match(l = await p.StandardOutput.ReadLineAsync())).Value == "") {
						outputLines.Add(l);
					}
				}
				var returnValue = int.Parse(m.Groups[1].Value);
				var output = outputLines.GetRange(6, outputLines.Count-6-2).Aggregate("", (ls, l) => ls + l + Environment.NewLine);
				answer.Output = (returnValue == 0 ? "コンパイル成功" : $"コンパイル失敗 戻り値: {returnValue}") + Environment.NewLine
					+ $"+++コンパイラ出力+++{Environment.NewLine}"
					+ output
					+ $"---コンパイラ出力---{Environment.NewLine}";
				if (returnValue == 0) {
					answer.ExecutableFilePath = exePath;
				} else {
					answer.Result = Result.CompileError;
				}
			});
		}

		public Task Run(Answer answer)
		{
			return Task.Run(async () => {
				foreach (var fp in Directory.EnumerateFiles(answer.ExecutionDirectory, "*", SearchOption.TopDirectoryOnly)
					.Where(p => !NecessaryFiles.Contains(Path.GetFileName(p)))) {
					File.Delete(fp);
				}
				if (GetProblemOf(answer).InputFiles != null) {
					foreach (var fp in (GetProblemOf(answer).InputFiles)
						.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
						.Select(l => l.Trim())
						.Where(l => l != "" && File.Exists(l))) {
						File.Copy(fp, Path.Combine(answer.ExecutionDirectory, Path.GetFileName(fp)));
					}
				}
				using (var p = Process.Start(new ProcessStartInfo(answer.ExecutableFilePath) {
					RedirectStandardOutput = true,
					RedirectStandardInput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				})) {
					answer.Output += "実行開始" + Environment.NewLine;
					p.StandardInput.Write(answer.InputModified ? answer.ModifiedInput : GetProblemOf(answer).Input);
					for (var i = 0; i < 10; ++i) p.StandardInput.WriteLine();
					for (var i = 0; i < ExecutionTime/20; ++i) {
						await Task.Delay(20);
						if (p.HasExited) break;
					}
					if (p.HasExited) {
						answer.Output
							+= "実行完了" + Environment.NewLine
							+ "+++出力+++" + Environment.NewLine
							+ await p.StandardOutput.ReadToEndAsync() + Environment.NewLine
							+ "---出力---" + Environment.NewLine;
						answer.Result = Result.Success;
					} else {
						answer.Output
							+= "時間切れ" + Environment.NewLine;
						p.Kill();
						answer.Result = Result.Timeout;
					}
				}
			});
		}

		public IObservable<Answer> CompileAll()
		{
			return Students.SelectMany(s => s.Answers)
				.Where(a => a.OriginalSourcePath != null)
				.Select(async a => { await Compile(a); return a; })
				.ToObservable()
				.Merge();
		}

		public IObservable<Answer> CompileAndRunAll()
		{
			return CompileAll().Where(a => a.ExecutableFilePath != null)
				.Select(async a => { await Run(a); return a; })
				.Merge();
		}

		public Task OutputScores()
		{
			return Task.Run(async () => {
				using (var s = new FileStream(Path.Combine(WorkingDirectory, "result.csv"), FileMode.Create))
				using (var sw = new StreamWriter(s, Encoding.GetEncoding(932))) {
					foreach (var item in Students) {
						await sw.WriteLineAsync($"{item.StudentNo},{item.Name},{item.Score},{item.AdvancedScore}");
					}
				}
			});
		}

		const string RelativeSubmissionsDirectory = @"submissions\";
		const string RelativeExecutionDirectory = @"execution\";
		static readonly Regex ZipNameSplitter = new Regex(@"^(\d{7}) (.+?)_.*\.zip$", RegexOptions.IgnoreCase);
		static readonly Regex ExerciseNoGetter = new Regex(@"第(\d+)回");
		static readonly ReadOnlyDictionary<Compiler, string> VsDevCmdTable
			= new ReadOnlyDictionary<Compiler, string>(new Dictionary<Compiler, string> {
				{
					Compiler.Vs2017Community,
					@"""/k """"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\Tools\VsDevCmd.bat"""""""
				},
				{
					Compiler.Vs2017Professional,
					@"""/k """"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\Tools\VsDevCmd.bat"""""""
				},
			});
		const string CompilerOptions = @"/GS /analyze- /W3 /Od /Zc:inline /fp:precise /RTC1 /Oy- /MDd /EHsc /nologo";
		static readonly Regex ReturnValueGetter = new Regex(@"^@@@errorlevel=(\d+)$", RegexOptions.Compiled);
		const int ExecutionTime = 5000;
		static readonly ReadOnlyCollection<string> NecessaryFiles
			= new ReadOnlyCollection<string>(new[] { "a.exe", "a.obj" });

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
