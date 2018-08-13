using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProkisoMarker
{
	public class Answer : BindableBase
	{
		private int _no;
		public int No
		{
			get { return _no; }
			set { SetProperty(ref _no, value); }
		}
		private Result _result;
		public Result Result
		{
			get { return _result; }
			set { SetProperty(ref _result, value); }
		}
		private Evaluation _evaluation;
		public Evaluation Evaluation
		{
			get { return _evaluation; }
			set { SetProperty(ref _evaluation, value); }
		}
		public bool SourceModified
		{
			get { return _modifiedSourcePath != null; }
		}
		private string _originalSourcePath;
		public string OriginalSourcePath
		{
			get { return _originalSourcePath; }
			set { SetProperty(ref _originalSourcePath, value); }
		}
		private string _modifiedSourcePath;
		public string ModifiedSourcePath
		{
			get { return _modifiedSourcePath; }
			set {
				if ((_modifiedSourcePath == null) != (value == null)) {
					RaisePropertyChanged(nameof(SourceModified));
				}
				SetProperty(ref _modifiedSourcePath, value);
			}
		}
		private string _executionDirectory;
		public string ExecutionDirectory
		{
			get { return _executionDirectory; }
			set { SetProperty(ref _executionDirectory, value); }
		}
		private string _executableFilePath;
		public string ExecutableFilePath
		{
			get { return _executableFilePath; }
			set { SetProperty(ref _executableFilePath, value); }
		}
		public bool InputModified
		{
			get { return ModifiedInput != null; }
		}
		private string _modifiedInput;
		public string ModifiedInput
		{
			get { return _modifiedInput; }
			set {
				if((_modifiedInput == null) != (value == null)) {
					RaisePropertyChanged(nameof(InputModified));
				}
				SetProperty(ref _modifiedInput, value);
			}
		}
		private string _output;
		public string Output
		{
			get { return _output; }
			set { SetProperty(ref _output, value); }
		}
	}
}
