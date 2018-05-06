using Prism.Mvvm;

namespace ProkisoMarker
{
	public class Problem : BindableBase
	{
		private int _no;
		public int No
		{
			get { return _no; }
			set { SetProperty(ref _no, value); }
		}
		private string _name;
		public string Name
		{
			get { return _name; }
			set { SetProperty(ref _name, value); }
		}
		private string _input;
		public string Input
		{
			get { return _input; }
			set { SetProperty(ref _input, value); }
		}
		private string _correctOutput;
		public string CorrectOutput
		{
			get { return _correctOutput; }
			set { SetProperty(ref _correctOutput, value); }
		}
		private ProblemType _type;
		public ProblemType Type
		{
			get { return _type; }
			set { SetProperty(ref _type, value); }
		}
	}
}
