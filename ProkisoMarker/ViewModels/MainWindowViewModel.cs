using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using ProkisoMarker.Models;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Windows.Data;

namespace ProkisoMarker.ViewModels
{
	public class MainWindowViewModel : BindableBase
	{
		public IModel Model { get; }

		private string _title = "ProkisoMarker";
		public string Title
		{
			get { return _title; }
			set { SetProperty(ref _title, value); }
		}

		private Problem _selectedProblem;
		public Problem SelectedProblem
		{
			get { return _selectedProblem; }
			set { SetProperty(ref _selectedProblem, value); }
		}

		private string _submissionFilePath;
		public string SubmissionFilePath
		{
			get { return _submissionFilePath; }
			set { SetProperty(ref _submissionFilePath, value); }
		}

		private string _workingDirectory;
		public string WorkingDirectory
		{
			get { return _workingDirectory; }
			set { SetProperty(ref _workingDirectory, value); }
		}

		private Student _selectedStudet;
		public Student SelectedStudent
		{
			get { return _selectedStudet; }
			set { SetProperty(ref _selectedStudet, value); }
		}

		private Answer _selectedAnswer;
		public Answer SelectedAnswer
		{
			get { return _selectedAnswer; }
			set { SetProperty(ref _selectedAnswer, value); }
		}

		private string _source;
		public string Source
		{
			get { return _source; }
			set { SetProperty(ref _source, value); }
		}

		private string _input;
		public string Input
		{
			get { return _input; }
			set { SetProperty(ref _input, value); }
		}
		private bool inputChangedBySystem_ = false;

		private Answer previousSelectedAnswer;

		public MainWindowViewModel(IModel model)
		{
			Model = model;
			PropertyChanged += MainWindowViewModel_PropertyChanged;
		}

		private void MainWindowViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch(e.PropertyName){
			case nameof(SelectedAnswer):
				if(previousSelectedAnswer != null) {
					previousSelectedAnswer.PropertyChanged -= SelectedAnswer_PropertyChanged;
					previousSelectedAnswer = SelectedAnswer;
				}
				ChangeSource();
				if(SelectedAnswer != null){
					SelectedAnswer.PropertyChanged += SelectedAnswer_PropertyChanged;
				}

				if(SelectedAnswer != null){
					inputChangedBySystem_ = true;
					Input = SelectedAnswer.InputModified ? SelectedAnswer.ModifiedInput : GetProblemOfSelectedAnswer().Input;
					inputChangedBySystem_ = false;
				} else {
					Input = "";
				}
				ResetInput.RaiseCanExecuteChanged();
				break;
			case nameof(Input):
				if (!inputChangedBySystem_) {
					if(SelectedAnswer != null) {
						SelectedAnswer.ModifiedInput = Input;
					}
					ResetInput.RaiseCanExecuteChanged();
				}
				break;
			}
		}

		private void SelectedAnswer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if(e.PropertyName == nameof(Answer.OriginalSourcePath)) {
				ChangeSource();
			}
		}

		private DelegateCommand _addProblen;
		public DelegateCommand AddProblem =>
				_addProblen ?? (_addProblen = new DelegateCommand(ExecuteAddProblem));

		void ExecuteAddProblem()
		{
			var p = new Problem();
			Model.ProblemSet.Problems.Add(p);
			SelectedProblem = p;
		}

		private DelegateCommand _removeProblem;
		public DelegateCommand RemoveProblem =>
				_removeProblem ?? (_removeProblem = new DelegateCommand(ExecuteRemoveProblem));

		void ExecuteRemoveProblem()
		{
			Model.ProblemSet.Problems.Remove(SelectedProblem);
		}

		private DelegateCommand _upProblem;
		public DelegateCommand UpProblem =>
				_upProblem ?? (_upProblem = new DelegateCommand(ExecuteUpProblem));

		void ExecuteUpProblem()
		{
			var i = Model.ProblemSet.Problems.IndexOf(SelectedProblem);
			if (SelectedProblem != null && i >= 1) {
				Model.ProblemSet.Problems.Move(i, i-1);
			}
		}

		private DelegateCommand _downProblem;
		public DelegateCommand DownProblem =>
				_downProblem ?? (_downProblem = new DelegateCommand(ExecuteDownProblem));

		void ExecuteDownProblem()
		{
			var i = Model.ProblemSet.Problems.IndexOf(SelectedProblem);
			if (SelectedProblem != null && i < Model.ProblemSet.Problems.Count-1) {
				Model.ProblemSet.Problems.Move(i, i+1);
			}
		}

		private DelegateCommand _loadSubmissions;
		public DelegateCommand LoadSubmissions =>
				_loadSubmissions ?? (_loadSubmissions = new DelegateCommand(ExecuteLoadSubmissions));

		async void ExecuteLoadSubmissions()
		{
			Model.WorkingDirectory = WorkingDirectory;
			await Model.LoadSubmissions(SubmissionFilePath);
			await Model.CompileAndRunAll();
		}

		private DelegateCommand _resetInput;
		public DelegateCommand ResetInput =>
				_resetInput ?? (_resetInput = new DelegateCommand(ExecuteResetInput, CanExecuteResetInput));

		void ExecuteResetInput()
		{
			SelectedAnswer.ModifiedInput = null;
			Input = GetProblemOfSelectedAnswer().Input;
			ResetInput.RaiseCanExecuteChanged();
		}

		bool CanExecuteResetInput()
		{
			return SelectedAnswer != null && SelectedAnswer.InputModified;
		}

		private DelegateCommand _run;
		public DelegateCommand Run =>
				_run ?? (_run = new DelegateCommand(ExecuteRun, CanExecuteRun));

		async void ExecuteRun()
		{
			var a = SelectedAnswer;
			await Model.Compile(a);
			await Model.Run(a);
		}

		bool CanExecuteRun()
		{
			return Source != null;
		}

		private void ChangeSource()
		{
			if(SelectedAnswer?.OriginalSourcePath != null && File.Exists(SelectedAnswer.OriginalSourcePath)){
				using (var sr = new StreamReader(SelectedAnswer.OriginalSourcePath, Encoding.GetEncoding(932))) {
					Source = sr.ReadToEnd();
				}
			} else {
				Source = null;
			}
			Run.RaiseCanExecuteChanged();
		}

		private Problem GetProblemOfSelectedAnswer()
		{
			return Model.ProblemSet.Problems.Where(p => p.No == SelectedAnswer.No).First();
		}
	}
}

