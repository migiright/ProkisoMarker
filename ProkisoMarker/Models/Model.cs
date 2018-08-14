using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Linq;
//using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ProkisoMarker.Models
{
	public class Model : BindableBase, IModel
	{
		public ProblemSet ProblemSet { get; } = new ProblemSet();
		public ObservableCollection<Student> Students { get; } = new ObservableCollection<Student>();
		private string _workingDirectory;
		public string WorkingDirectory
		{
			get { return _workingDirectory; }
			set {
				var v = value ?? "";
				SetProperty(ref _workingDirectory, v);
			}
		}
		public string SubmissionsDirectory => Path.Combine(WorkingDirectory, RelativeSubmissionsDirectory);
		private Compiler _compiler = Compiler.Vs2017Professional;
		public Compiler Compiler
		{
			get { return _compiler; }
			set { SetProperty(ref _compiler, value); }
		}

		public Model()
		{
			BindingOperations.EnableCollectionSynchronization(Students, new object());
			ProblemSet.Problems.CollectionChanged += Problems_CollectionChanged;
		}

		public Task LoadSubmissions(string filePath)
		{
			return Task.Run(() => {
				string toHankaku(string str)
				{
					return string.Concat(str.Select(c => ('０' <= c && c <= '９') ? (char)('0' + c - '０') : c));
				}
				var archive = ZipFile.Open(filePath, ZipArchiveMode.Read);
				var excerciseNo = int.Parse(toHankaku(ExerciseNoGetter.Match(Path.GetFileName(filePath)).Groups[1].Value));
				Students.Clear();
				var tuple = archive.Entries.Select(e => (e: e, m: ZipNameSplitter.Match(e.Name)))
					.Where(t => t.m.Success)
					.Select(t => (e: t.e, s: new Student(ProblemSet) {
						StudentNo = int.Parse(t.m.Groups[1].Value),
						Name = t.m.Groups[2].Value,
					}));
				foreach (var (e, s) in tuple) {
					BindingOperations.EnableCollectionSynchronization(s.Answers, new object());
					var a = new ZipArchive(e.Open());
					var d = Path.Combine(SubmissionsDirectory, $"{s.StudentNo}_{s.Name}");
					a.ExtractToDirectory(d);
					for (var i = 1; i <= ProblemSet.Problems.Count; ++i) {
						var sp = Directory.EnumerateFiles(d, $"ex{excerciseNo}-{i}.cpp").FirstOrDefault();
						s.Answers.Add(new Answer {
							No = i,
							OriginalSourcePath = sp,
							ExecutingDirectory = Path.Combine(
								WorkingDirectory, RelativeExecutiongDirectory, $"{s.StudentNo}_{s.Name}", $"{i}"),
							Result = sp == null ? Result.FileNotFound : Result.Undefined,
						});
					}
					Students.Add(s);
				}
			});
		}

		public Task Compile(Answer answer)
		{
			return Task.Run(() => {
				var psi = new ProcessStartInfo(Environment.GetEnvironmentVariable("comspec")) {
					RedirectStandardOutput = true,
					RedirectStandardInput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
					Arguments = VsDevCmdTable[Compiler],
				};
				Match m;
				var co = new List<string>();
				string ef = Path.Combine(answer.ExecutingDirectory, "a");
				using (var p = Process.Start(psi)) {
					if (!Directory.Exists(answer.ExecutingDirectory)) {
						Directory.CreateDirectory(answer.ExecutingDirectory);
					}
					p.StandardInput.WriteLine($"cl {answer.OriginalSourcePath} /Fe\"{ef}\" /Fo\"{ef}\" {CompilerOptions}");
					p.StandardInput.WriteLine("echo @@@errorlevel=%errorlevel%");
					p.StandardInput.WriteLine("exit");
					string l;
					while ((m = ReturnValueGetter.Match(l = p.StandardOutput.ReadLine())).Value == "") {
						co.Add(l);
					}
				}
				var returnValue = int.Parse(m.Groups[1].Value);
				var output = co.GetRange(6, co.Count-6-2).Aggregate("", (ls, l) => ls + l + Environment.NewLine);
				answer.Output = (returnValue == 0 ? "コンパイル成功" : $"コンパイル失敗 戻り値: {returnValue}") + Environment.NewLine
					+ $"+++コンパイラ出力+++{Environment.NewLine}"
					+ output
					+ $"---コンパイラ出力---{Environment.NewLine}";
				if (returnValue == 0) {
					answer.ExecutableFilePath = ef;
				} else {
					answer.Result = Result.CompileError;
				}
			});
		}

		public async Task Run(Answer answer)
		{
			using (var p = Process.Start(new ProcessStartInfo(answer.ExecutableFilePath) {
				RedirectStandardOutput = true,
				RedirectStandardInput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			})) {
				answer.Output += "実行開始" + Environment.NewLine;
				p.StandardInput.Write(answer.InputModified ? answer.ModifiedInput : GetProblem(answer).Input);
				for (var i = 0; i < 10; ++i) p.StandardInput.WriteLine();
				for(var i = 0; i < ExecutionTime/20; ++i) {
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
					answer.Result = Result.RuntimeError;
				}
			}
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

		const string RelativeSubmissionsDirectory = @"submissions\";
		const string RelativeExecutiongDirectory = @"execution\";
		static readonly Regex ZipNameSplitter = new Regex(@"^(\d{7}) (.+?)_.*\.zip$", RegexOptions.IgnoreCase);
		static readonly Regex ExerciseNoGetter = new Regex(@"第(\d+)回");
		static readonly ReadOnlyDictionary<Compiler, string> VsDevCmdTable
			= new ReadOnlyDictionary<Compiler, string>(
				new Dictionary<Compiler, string> {
					{
						Compiler.Vs2017Community,
						@"""/k """"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\Tools\VsDevCmd.bat"""""""
					},
					{
						Compiler.Vs2017Professional,
						@"""/k """"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\Tools\VsDevCmd.bat"""""""
					},
				}
			);
		const string CompilerOptions = @"/GS /analyze- /W3 /Od /Zc:inline /fp:precise /RTC1 /Oy- /MDd /EHsc /nologo";
		static readonly Regex ReturnValueGetter = new Regex(@"^@@@errorlevel=(\d+)$", RegexOptions.Compiled);
		const int ExecutionTime = 5000;

		private void Problems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null) {
				foreach (var (p, i) in e.NewItems.Cast<Problem>().Select((p, i) => (p, i))) {
					if (string.IsNullOrEmpty(p.Name)) {
						p.Name = "p" + (i+ProblemSet.Problems.Count-e.NewItems.Count);
					}
				}
			}
			foreach (var (p, i) in ProblemSet.Problems.Select((p, i) => (p, i))) {
				p.No = i+1;
			}
		}

		private Problem GetProblem(Answer answer)
		{
			return ProblemSet.Problems.Where(p => p.No == answer.No).First();
		}
	}
}
